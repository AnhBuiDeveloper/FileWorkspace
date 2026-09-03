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
var uploadRoot = Path.GetFullPath(uploadDirectory);

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

    var relativeFolder = GetSafeRelativePath(context.Request.Headers["X-Target-Folder"].ToString());
    if (relativeFolder is null)
        return Results.BadRequest(new { error = "Thư mục đích không hợp lệ." });

    var destinationDirectory = GetAbsoluteUploadPath(uploadRoot, relativeFolder);
    if (destinationDirectory is null)
        return Results.BadRequest(new { error = "Thư mục đích không hợp lệ." });

    Directory.CreateDirectory(destinationDirectory);

    var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    var storedName = ReserveUniqueFileName(destinationDirectory, fileName, reservedNames, fileNameLock, out var reservationKey);
    var temporaryPath = Path.Combine(destinationDirectory, $".{sessionId}.uploading");
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

        var session = new UploadSession(sessionId, storedName, destinationDirectory, reservationKey, temporaryPath, totalBytes, totalChunks);
        if (totalChunks == 0)
        {
            File.Move(temporaryPath, Path.Combine(destinationDirectory, storedName));
            reservedNames.TryRemove(reservationKey, out _);
            return Results.Ok(new { uploadId = sessionId, chunkSize = ChunkSize, completed = true, fileName = storedName, uploadedBytes = 0L });
        }

        if (!uploadSessions.TryAdd(sessionId, session))
            throw new InvalidOperationException("Không thể tạo phiên upload.");

        return Results.Ok(new { uploadId = sessionId, chunkSize = ChunkSize, completed = false, uploadedBytes = 0L });
    }
    catch
    {
        reservedNames.TryRemove(reservationKey, out _);
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

            var targetPath = Path.Combine(session.DestinationDirectory, session.StoredName);
            File.Move(session.TemporaryPath, targetPath);
            uploadSessions.TryRemove(session.Id, out _);
            reservedNames.TryRemove(session.ReservationKey, out _);

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
        reservedNames.TryRemove(session.ReservationKey, out _);
    }
    finally
    {
        session.Gate.Release();
    }

    return Results.NoContent();
});

app.MapGet("/api/files", (HttpContext context) =>
{
    if (!TokenMatches(uploadAccessToken, context.Request.Headers["X-Upload-Token"].ToString()))
        return Results.Unauthorized();

    var relativePath = GetSafeRelativePath(context.Request.Query["path"].ToString());
    if (relativePath is null)
        return Results.BadRequest(new { error = "Đường dẫn thư mục không hợp lệ." });

    var currentDirectory = GetAbsoluteUploadPath(uploadRoot, relativePath);
    if (currentDirectory is null || !Directory.Exists(currentDirectory))
        return Results.NotFound(new { error = "Thư mục không tồn tại." });

    var folders = Directory.EnumerateDirectories(currentDirectory)
        .Select(path => new DirectoryInfo(path))
        .Select(folder => new
        {
            name = folder.Name,
            type = "folder",
            bytes = (long?)null,
            modifiedAtUtc = folder.LastWriteTimeUtc
        });
    var files = Directory.EnumerateFiles(currentDirectory)
        .Select(path => new FileInfo(path))
        .Where(file => !IsTemporaryUpload(file.Name))
        .Select(file => new
        {
            name = file.Name,
            type = "file",
            bytes = (long?)file.Length,
            modifiedAtUtc = file.LastWriteTimeUtc
        });

    var entries = folders.Concat(files)
        .OrderBy(entry => entry.type == "folder" ? 0 : 1)
        .ThenBy(entry => entry.name, StringComparer.OrdinalIgnoreCase);

    return Results.Ok(new { path = relativePath, entries });
});

app.MapPost("/api/files/download", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    if (!TokenMatches(uploadAccessToken, form["token"].ToString()))
        return Results.Unauthorized();

    var relativePath = GetSafeRelativePath(form["path"].ToString());
    if (relativePath is null || IsTemporaryUpload(Path.GetFileName(relativePath)))
        return Results.NotFound();

    var filePath = GetAbsoluteUploadPath(uploadRoot, relativePath);
    if (filePath is null)
        return Results.NotFound();

    if (!File.Exists(filePath))
        return Results.NotFound();

    return Results.File(filePath, "application/octet-stream", fileDownloadName: Path.GetFileName(filePath), enableRangeProcessing: true);
});

app.MapPost("/api/folders", async (HttpContext context) =>
{
    if (!TokenMatches(uploadAccessToken, context.Request.Headers["X-Upload-Token"].ToString()))
        return Results.Unauthorized();

    var request = await context.Request.ReadFromJsonAsync<CreateFolderRequest>(cancellationToken: context.RequestAborted);
    var parentPath = GetSafeRelativePath(request?.ParentPath ?? string.Empty);
    var folderName = GetSafeFolderName(request?.Name ?? string.Empty);
    if (parentPath is null || folderName is null)
        return Results.BadRequest(new { error = "Tên hoặc đường dẫn thư mục không hợp lệ." });

    var relativeFolderPath = string.IsNullOrEmpty(parentPath) ? folderName : $"{parentPath}/{folderName}";
    var folderPath = GetAbsoluteUploadPath(uploadRoot, relativeFolderPath);
    if (folderPath is null)
        return Results.BadRequest(new { error = "Đường dẫn thư mục không hợp lệ." });
    if (Directory.Exists(folderPath) || File.Exists(folderPath))
        return Results.Conflict(new { error = "Tên thư mục đã tồn tại." });

    Directory.CreateDirectory(folderPath);
    return Results.Created($"/api/files?path={Uri.EscapeDataString(relativeFolderPath)}", new { path = relativeFolderPath, name = folderName });
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

static string? GetSafeFolderName(string name)
{
    var trimmedName = name.Trim();
    return string.IsNullOrWhiteSpace(trimmedName) ||
           trimmedName is "." or ".." ||
           trimmedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
           trimmedName.Contains('/') ||
           trimmedName.Contains('\\')
        ? null
        : trimmedName;
}

static string? GetSafeRelativePath(string encodedPath)
{
    try
    {
        var decodedPath = Uri.UnescapeDataString(encodedPath).Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(decodedPath))
            return string.Empty;

        var segments = decodedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.All(segment => GetSafeFolderName(segment) is not null)
            ? string.Join('/', segments)
            : null;
    }
    catch (UriFormatException)
    {
        return null;
    }
}

static string? GetAbsoluteUploadPath(string uploadRoot, string relativePath)
{
    var absolutePath = Path.GetFullPath(Path.Combine(uploadRoot, relativePath));
    var rootWithSeparator = uploadRoot.EndsWith(Path.DirectorySeparatorChar)
        ? uploadRoot
        : uploadRoot + Path.DirectorySeparatorChar;

    return absolutePath.Equals(uploadRoot, StringComparison.OrdinalIgnoreCase) ||
           absolutePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
        ? absolutePath
        : null;
}

static bool IsTemporaryUpload(string fileName) =>
    fileName.StartsWith(".", StringComparison.Ordinal) &&
    fileName.EndsWith(".uploading", StringComparison.OrdinalIgnoreCase);

static string ReserveUniqueFileName(string directory, string requestedName, ConcurrentDictionary<string, byte> reservedNames, object fileNameLock, out string reservationKey)
{
    lock (fileNameLock)
    {
        var baseName = Path.GetFileNameWithoutExtension(requestedName);
        var extension = Path.GetExtension(requestedName);
        var candidate = requestedName;
        var counter = 1;

        while (File.Exists(Path.Combine(directory, candidate)) || reservedNames.ContainsKey(Path.Combine(directory, candidate)))
            candidate = $"{baseName} ({counter++}){extension}";

        reservationKey = Path.Combine(directory, candidate);
        reservedNames.TryAdd(reservationKey, 0);
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

sealed class UploadSession(string id, string storedName, string destinationDirectory, string reservationKey, string temporaryPath, long totalBytes, int totalChunks)
{
    public string Id { get; } = id;
    public string StoredName { get; } = storedName;
    public string DestinationDirectory { get; } = destinationDirectory;
    public string ReservationKey { get; } = reservationKey;
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

sealed record CreateFolderRequest(string ParentPath, string Name);
