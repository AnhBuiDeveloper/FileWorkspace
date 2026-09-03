// Required Notice: Copyright (c) 2026 Anh Bui (https://github.com/AnhBuiDeveloper/FileUpload)
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.Features;

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

// File body is streamed directly to disk. Do not cap uploads at Kestrel's default limit.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = null);

var app = builder.Build();
var uploadDirectory = Path.Combine(app.Environment.ContentRootPath, "Upload");
var uploadAccessToken = builder.Configuration["UPLOAD_ACCESS_TOKEN"]
    ?? throw new InvalidOperationException("Thiếu biến môi trường UPLOAD_ACCESS_TOKEN.");
Directory.CreateDirectory(uploadDirectory);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/upload", async (HttpContext context) =>
{
    var suppliedToken = context.Request.Headers["X-Upload-Token"].ToString();
    if (!TokenMatches(uploadAccessToken, suppliedToken))
        return Results.Unauthorized();

    var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (sizeFeature is { IsReadOnly: false })
        sizeFeature.MaxRequestBodySize = null;

    var encodedName = context.Request.Headers["X-File-Name"].ToString();
    var requestedName = Uri.UnescapeDataString(encodedName);
    var fileName = Path.GetFileName(requestedName);

    if (string.IsNullOrWhiteSpace(fileName))
        return Results.BadRequest(new { error = "Tên file không hợp lệ." });

    // Reserve a unique name before copying; this avoids overwriting existing uploads.
    var storedName = CreateUniqueFileName(uploadDirectory, fileName);
    var targetPath = Path.Combine(uploadDirectory, storedName);
    var temporaryPath = Path.Combine(uploadDirectory, $".{Guid.NewGuid():N}.uploading");
    var completed = false;

    try
    {
        await using (var output = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await context.Request.Body.CopyToAsync(output, 1024 * 1024, context.RequestAborted);
        }

        File.Move(temporaryPath, targetPath);
        completed = true;
        return Results.Ok(new { fileName = storedName, bytes = new FileInfo(targetPath).Length });
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
    }
    finally
    {
        if (!completed && File.Exists(temporaryPath))
            File.Delete(temporaryPath);
    }
});

app.Run();

static string CreateUniqueFileName(string directory, string fileName)
{
    var baseName = Path.GetFileNameWithoutExtension(fileName);
    var extension = Path.GetExtension(fileName);
    var candidate = fileName;
    var counter = 1;

    while (File.Exists(Path.Combine(directory, candidate)))
        candidate = $"{baseName} ({counter++}){extension}";

    return candidate;
}

static bool TokenMatches(string expectedToken, string suppliedToken)
{
    if (suppliedToken.Length != expectedToken.Length)
        return false;

    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(expectedToken),
        Encoding.UTF8.GetBytes(suppliedToken));
}
