using FileWorkspace.Models;
using FileWorkspace.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;

namespace FileWorkspace.Endpoints;

public static class FileWorkspaceEndpointExtensions
{
    public static IEndpointRouteBuilder MapFileWorkspaceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/uploads", async (HttpContext context, FileManagerService files, UploadTokenValidator token) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            if (!long.TryParse(context.Request.Headers["X-File-Size"], out var size)) return Error("Dung lượng file không hợp lệ.", StatusCodes.Status400BadRequest);
            try
            {
                var result = await files.StartUploadAsync(context.Request.Headers["X-File-Name"].ToString(), context.Request.Headers["X-Target-Folder"].ToString(), size, context.RequestAborted);
                return Results.Ok(result);
            }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api-uploads");

        endpoints.MapPut("/api/uploads/{uploadId}/chunks/{chunkIndex:int}", async (HttpContext context, string uploadId, int chunkIndex, FileManagerService files, UploadTokenValidator token) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            try
            {
                var result = await files.WriteChunkAsync(uploadId, chunkIndex, context.Request.ContentLength, context.Request.Body, context.RequestAborted);
                return Results.Ok(result);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { return Results.StatusCode(StatusCodes.Status499ClientClosedRequest); }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api-uploads");

        endpoints.MapPost("/api/uploads/{uploadId}/resume", async (HttpContext context, string uploadId, FileManagerService files, UploadTokenValidator token) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            if (!long.TryParse(context.Request.Headers["X-File-Size"], out var size)) return Error("Dung lượng file không hợp lệ.", StatusCodes.Status400BadRequest);
            try
            {
                var result = await files.ResumeUploadAsync(uploadId, context.Request.Headers["X-File-Name"].ToString(), context.Request.Headers["X-Target-Folder"].ToString(), size, context.RequestAborted);
                return Results.Ok(result);
            }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api-uploads");

        endpoints.MapDelete("/api/uploads/{uploadId}", async (HttpContext context, string uploadId, FileManagerService files, UploadTokenValidator token) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            await files.CancelUploadAsync(uploadId);
            return Results.NoContent();
        }).RequireRateLimiting("api-uploads");

        endpoints.MapGet("/api/files", (HttpContext context, FileManagerService files, UploadTokenValidator token) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            try { return Results.Ok(files.List(context.Request.Query["path"].ToString())); }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api");

        endpoints.MapPost("/api/files/download", async (HttpContext context, FileManagerService files, UploadTokenValidator token, ILogger<Program> logger) =>
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            if (!token.Matches(form["token"].ToString())) return Results.Unauthorized();
            try
            {
                var download = files.GetDownload(form["path"].ToString());
                LogAudit(logger, context, "download", download.FileName);
                return Results.File(download.AbsolutePath, "application/octet-stream", fileDownloadName: download.FileName, enableRangeProcessing: true);
            }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api");

        endpoints.MapPost("/api/files/download-tickets", async (HttpContext context, FileManagerService files, DownloadTicketService tickets, UploadTokenValidator token, ILogger<Program> logger) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            var request = await context.Request.ReadFromJsonAsync<DownloadTicketRequest>(cancellationToken: context.RequestAborted);
            if (request is null) return Error("File không tồn tại.", StatusCodes.Status400BadRequest);
            try
            {
                var download = files.GetDownload(request.Path);
                var ticket = tickets.Create(download);
                LogAudit(logger, context, "download-ticket-issued", download.FileName);
                return Results.Ok(new DownloadTicketResponse($"/api/downloads/{ticket.Value}", ticket.ExpiresAtUtc));
            }
            catch (FileNotFoundException) { return Error("File không tồn tại.", StatusCodes.Status404NotFound); }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api");

        endpoints.MapGet("/api/downloads/{ticket}", (HttpContext context, string ticket, DownloadTicketService tickets, ILogger<Program> logger) =>
        {
            var download = tickets.GetDownload(ticket);
            if (download is null || !File.Exists(download.AbsolutePath)) return Results.NotFound();
            LogAudit(logger, context, "download", download.FileName);
            return Results.File(download.AbsolutePath, "application/octet-stream", fileDownloadName: download.FileName, enableRangeProcessing: true);
        }).RequireRateLimiting("api");

        endpoints.MapPost("/api/files/archive", async (HttpContext context, FileManagerService files, UploadTokenValidator token, ILogger<Program> logger) =>
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            if (!token.Matches(form["token"].ToString())) return Results.Unauthorized();
            try
            {
                var archive = files.GetArchive(form["path"]);
                LogAudit(logger, context, "archive-download", string.Join(", ", form["path"].ToArray()));
                var bodyControl = context.Features.Get<IHttpBodyControlFeature>();
                if (bodyControl is not null) bodyControl.AllowSynchronousIO = true;
                return Results.Stream(stream => files.WriteArchiveAsync(archive, stream, context.RequestAborted), "application/zip", fileDownloadName: "FileWorkspace-download.zip");
            }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api");

        endpoints.MapPost("/api/files/delete", async (HttpContext context, FileManagerService files, UploadTokenValidator token, ILogger<Program> logger) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            var request = await context.Request.ReadFromJsonAsync<DeleteEntriesRequest>(cancellationToken: context.RequestAborted);
            if (request?.Paths is null) return Error("Cần chọn ít nhất một file hoặc folder.", StatusCodes.Status400BadRequest);
            try
            {
                files.DeleteEntries(request.Paths);
                LogAudit(logger, context, "delete", string.Join(", ", request.Paths));
                return Results.NoContent();
            }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api");

        endpoints.MapDelete("/api/files", (HttpContext context, FileManagerService files, UploadTokenValidator token, ILogger<Program> logger) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            try
            {
                var path = context.Request.Query["path"].ToString();
                files.DeleteFile(path);
                LogAudit(logger, context, "delete", path);
                return Results.NoContent();
            }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api");

        endpoints.MapPost("/api/folders", async (HttpContext context, FileManagerService files, UploadTokenValidator token) =>
        {
            if (!IsAuthorized(context, token)) return Results.Unauthorized();
            var request = await context.Request.ReadFromJsonAsync<CreateFolderRequest>(cancellationToken: context.RequestAborted);
            if (request is null) return Error("Tên hoặc đường dẫn thư mục không hợp lệ.", StatusCodes.Status400BadRequest);
            try
            {
                files.CreateFolder(request);
                var path = string.IsNullOrEmpty(request.ParentPath) ? request.Name : $"{request.ParentPath}/{request.Name}";
                return Results.Created($"/api/files?path={Uri.EscapeDataString(path)}", new { path, name = request.Name });
            }
            catch (FileManagerException exception) { return Error(exception); }
        }).RequireRateLimiting("api");

        return endpoints;
    }

    private static bool IsAuthorized(HttpContext context, UploadTokenValidator token) =>
        token.Matches(context.Request.Headers["X-Upload-Token"].ToString());

    // Minimal security audit trail: action, path, and client IP only — never the upload token or a ticket value.
    private static void LogAudit(ILogger logger, HttpContext context, string action, string path) =>
        logger.LogInformation("audit action={Action} path={Path} clientIp={ClientIp}", action, path, context.Connection.RemoteIpAddress);

    private static IResult Error(FileManagerException exception) => Error(exception.Message, exception.StatusCode);
    private static IResult Error(string message, int statusCode) => Results.Json(new ErrorResponse(message), statusCode: statusCode);
}
