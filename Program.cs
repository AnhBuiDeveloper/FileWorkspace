// Required Notice: Copyright (c) 2026 Anh Bui (https://github.com/AnhBuiDeveloper/FileUpload)
using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.Features;

const int ChunkSize = 8 * 1024 * 1024;
const int CopyBufferSize = 1024 * 1024;

var builder = WebApplication.CreateBuilder(args);

var localEnvPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(localEnvPath))
{
    var localEnvValues = File.ReadLines(localEnvPath)
        .Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith('#'))
        .Select(line => line.Split('=', 2))
        .Where(parts => parts.Length == 2)
        .Select(parts => new KeyValuePair<string, string?>(parts[0].Trim(), parts[1].Trim()));

    builder.Configuration.AddInMemoryCollection(localEnvValues);
}

// Environment variables take priority over the local development .env file.
builder.Configuration.AddEnvironmentVariables();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = null);

var app = builder.Build();
var uploadDirectory = Path.Combine(app.Environment.ContentRootPath, "Upload");
var uploadAccessToken = builder.Configuration["UPLOAD_ACCESS_TOKEN"]
    ?? throw new InvalidOperationException("Thiếu biến môi trường UPLOAD_ACCESS_TOKEN.");
var uploadSessions = new ConcurrentDictionary<string, UploadSession>();
var reservedNames = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
var fileNameLock = new object();

Directory.CreateDirectory(uploadDirectory);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/uploads", async (HttpContext context) =>
{
    if (!TokenMatches(uploadAccessToken, context.Request.Headers["X-Upload-Token"].ToString()))
        return Results.Unauthorized();

    var fileName = GetSafeFileName(context.Request.Headers["X-File-Name"].ToString());
    if (fileName is null)
        return Results.BadRequest(new { error = "Tên file không hợp lệ." });

    if (!long.TryParse(context.Request.Headers["X-File-Size"], out var totalBytes) || totalBytes < 0)
        return Results.BadRequest(new { error = "Dung lượng file không hợp lệ." });

    var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    var storedName = ReserveUniqueFileName(uploadDirectory, fileName, reservedNames, fileNameLock);
    var temporaryPath = Path.Combine(uploadDirectory, $".{sessionId}.uploading");
    var totalChunks = totalBytes == 0 ? 0 : checked((int)((totalBytes + ChunkSize - 1) / ChunkSize));

    try
    {
        await using (var temporaryFile = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            CopyBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await temporaryFile.FlushAsync(context.RequestAborted);
        }

        var session = new UploadSession(sessionId, storedName, temporaryPath, totalBytes, totalChunks);
        if (totalChunks == 0)
        {
            File.Move(temporaryPath, Path.Combine(uploadDirectory, storedName));
            reservedNames.TryRemove(storedName, out _);
            return Results.Ok(new { uploadId = sessionId, chunkSize = ChunkSize, completed = true, fileName = storedName, uploadedBytes = 0L });
        }

        if (!uploadSessions.TryAdd(sessionId, session))
            throw new InvalidOperationException("Không thể tạo phiên upload.");

        return Results.Ok(new { uploadId = sessionId, chunkSize = ChunkSize, completed = false, uploadedBytes = 0L });
    }
    catch
    {
        reservedNames.TryRemove(storedName, out _);
        if (File.Exists(temporaryPath))
            File.Delete(temporaryPath);
        throw;
    }
});

app.MapPut("/api/uploads/{uploadId}/chunks/{chunkIndex:int}", async (HttpContext context, string uploadId, int chunkIndex) =>
{
    if (!TokenMatches(uploadAccessToken, context.Request.Headers["X-Upload-Token"].ToString()))
        return Results.Unauthorized();

    if (!uploadSessions.TryGetValue(uploadId, out var session))
        return Results.NotFound(new { error = "Phiên upload không tồn tại hoặc đã kết thúc." });

    if (chunkIndex < 0 || chunkIndex >= session.TotalChunks)
        return Results.BadRequest(new { error = "Chunk không hợp lệ." });

    var expectedLength = session.GetChunkLength(chunkIndex, ChunkSize);
    if (context.Request.ContentLength != expectedLength)
        return Results.BadRequest(new { error = "Kích thước chunk không hợp lệ." });

    var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (sizeFeature is { IsReadOnly: false })
        sizeFeature.MaxRequestBodySize = expectedLength;

    try
    {
        await session.Gate.WaitAsync(context.RequestAborted);
        try
        {
            if (session.ReceivedChunks[chunkIndex])
                return Results.Ok(new { uploadedBytes = session.UploadedBytes, completed = false });

            long copied;
            await using (var temporaryFile = new FileStream(
                session.TemporaryPath,
                FileMode.Open,
                FileAccess.Write,
                FileShare.None,
                CopyBufferSize,
                FileOptions.Asynchronous | FileOptions.RandomAccess))
            {
                temporaryFile.Position = (long)chunkIndex * ChunkSize;
                copied = await CopyExactAsync(context.Request.Body, temporaryFile, expectedLength, context.RequestAborted);
            }

            if (copied != expectedLength)
                return Results.BadRequest(new { error = "Dữ liệu chunk chưa hoàn chỉnh." });

            session.ReceivedChunks[chunkIndex] = true;
            session.UploadedBytes += expectedLength;
            session.LastActivityUtc = DateTimeOffset.UtcNow;

            if (!session.ReceivedChunks.All(received => received))
                return Results.Ok(new { uploadedBytes = session.UploadedBytes, completed = false });

            var targetPath = Path.Combine(uploadDirectory, session.StoredName);
            File.Move(session.TemporaryPath, targetPath);
            uploadSessions.TryRemove(session.Id, out _);
            reservedNames.TryRemove(session.StoredName, out _);

            return Results.Ok(new
            {
                uploadedBytes = session.UploadedBytes,
                completed = true,
                fileName = session.StoredName
            });
        }
        finally
        {
            session.Gate.Release();
        }
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
    }
});

app.MapDelete("/api/uploads/{uploadId}", async (HttpContext context, string uploadId) =>
{
    if (!TokenMatches(uploadAccessToken, context.Request.Headers["X-Upload-Token"].ToString()))
        return Results.Unauthorized();

    if (!uploadSessions.TryRemove(uploadId, out var session))
        return Results.NoContent();

    await session.Gate.WaitAsync();
    try
    {
        if (File.Exists(session.TemporaryPath))
            File.Delete(session.TemporaryPath);
        reservedNames.TryRemove(session.StoredName, out _);
    }
    finally
    {
        session.Gate.Release();
    }

    return Results.NoContent();
});

app.Run();

static string? GetSafeFileName(string encodedName)
{
    try
    {
        var fileName = Path.GetFileName(Uri.UnescapeDataString(encodedName));
        return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
    }
    catch (UriFormatException)
    {
        return null;
    }
}

static string ReserveUniqueFileName(string directory, string requestedName, ConcurrentDictionary<string, byte> reservedNames, object fileNameLock)
{
    lock (fileNameLock)
    {
        var baseName = Path.GetFileNameWithoutExtension(requestedName);
        var extension = Path.GetExtension(requestedName);
        var candidate = requestedName;
        var counter = 1;

        while (File.Exists(Path.Combine(directory, candidate)) || reservedNames.ContainsKey(candidate))
            candidate = $"{baseName} ({counter++}){extension}";

        reservedNames.TryAdd(candidate, 0);
        return candidate;
    }
}

static async Task<long> CopyExactAsync(Stream source, Stream destination, long expectedLength, CancellationToken cancellationToken)
{
    var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
    try
    {
        long copied = 0;
        while (copied < expectedLength)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, expectedLength - copied)), cancellationToken);
            if (read == 0)
                break;

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            copied += read;
        }

        return copied;
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}

static bool TokenMatches(string expectedToken, string suppliedToken)
{
    if (suppliedToken.Length != expectedToken.Length)
        return false;

    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(expectedToken),
        Encoding.UTF8.GetBytes(suppliedToken));
}

sealed class UploadSession(string id, string storedName, string temporaryPath, long totalBytes, int totalChunks)
{
    public string Id { get; } = id;
    public string StoredName { get; } = storedName;
    public string TemporaryPath { get; } = temporaryPath;
    public long TotalBytes { get; } = totalBytes;
    public int TotalChunks { get; } = totalChunks;
    public bool[] ReceivedChunks { get; } = new bool[totalChunks];
    public long UploadedBytes { get; set; }
    public DateTimeOffset LastActivityUtc { get; set; } = DateTimeOffset.UtcNow;
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public long GetChunkLength(int chunkIndex, int chunkSize)
    {
        var offset = (long)chunkIndex * chunkSize;
        return Math.Min(chunkSize, TotalBytes - offset);
    }
}
