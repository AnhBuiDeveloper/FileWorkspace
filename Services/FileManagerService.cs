using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using FileWorkspace.Models;

namespace FileWorkspace.Services;

public sealed class FileManagerService
{
    private readonly string _rootPath;
    private readonly ConcurrentDictionary<string, UploadSession> _sessions = new();
    private readonly ConcurrentDictionary<string, byte> _reservedPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _fileNameLock = new();

    public FileManagerService(IHostEnvironment environment)
    {
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Upload"));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<UploadStartResponse> StartUploadAsync(string encodedFileName, string encodedTargetFolder, long totalBytes, CancellationToken cancellationToken)
    {
        if (totalBytes < 0) throw BadRequest("Dung lượng file không hợp lệ.");
        var fileName = GetSafeFileName(encodedFileName) ?? throw BadRequest("Tên file không hợp lệ.");
        var targetFolder = NormalizeRelativePath(encodedTargetFolder) ?? throw BadRequest("Thư mục đích không hợp lệ.");
        var destinationDirectory = ResolvePath(targetFolder) ?? throw BadRequest("Thư mục đích không hợp lệ.");
        Directory.CreateDirectory(destinationDirectory);

        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var storedName = ReserveUniqueFileName(destinationDirectory, fileName, out var reservationKey);
        var temporaryPath = Path.Combine(destinationDirectory, $".{sessionId}.uploading");
        var totalChunks = totalBytes == 0 ? 0 : checked((int)((totalBytes + UploadProtocol.ChunkSize - 1) / UploadProtocol.ChunkSize));

        try
        {
            await using (var temporaryFile = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, UploadProtocol.CopyBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await temporaryFile.FlushAsync(cancellationToken);

            if (totalChunks == 0)
            {
                File.Move(temporaryPath, Path.Combine(destinationDirectory, storedName));
                _reservedPaths.TryRemove(reservationKey, out _);
                return new UploadStartResponse(sessionId, UploadProtocol.ChunkSize, true, 0, storedName);
            }

            var session = new UploadSession(sessionId, storedName, destinationDirectory, reservationKey, temporaryPath, totalBytes, totalChunks);
            if (!_sessions.TryAdd(sessionId, session)) throw new InvalidOperationException("Không thể tạo phiên upload.");
            return new UploadStartResponse(sessionId, UploadProtocol.ChunkSize, false, 0);
        }
        catch
        {
            _reservedPaths.TryRemove(reservationKey, out _);
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            throw;
        }
    }

    public async Task<UploadChunkResponse> WriteChunkAsync(string uploadId, int chunkIndex, long? contentLength, Stream requestBody, CancellationToken cancellationToken)
    {
        var session = GetSession(uploadId);
        if (chunkIndex < 0 || chunkIndex >= session.TotalChunks) throw BadRequest("Chunk không hợp lệ.");
        var expectedLength = session.GetChunkLength(chunkIndex);
        if (contentLength != expectedLength) throw BadRequest("Kích thước chunk không hợp lệ.");

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (session.ReceivedChunks[chunkIndex]) return new UploadChunkResponse(session.UploadedBytes, false);
            long copied;
            await using (var temporaryFile = new FileStream(session.TemporaryPath, FileMode.Open, FileAccess.Write, FileShare.None, UploadProtocol.CopyBufferSize, FileOptions.Asynchronous | FileOptions.RandomAccess))
            {
                temporaryFile.Position = (long)chunkIndex * UploadProtocol.ChunkSize;
                copied = await CopyExactAsync(requestBody, temporaryFile, expectedLength, cancellationToken);
            }
            if (copied != expectedLength) throw BadRequest("Dữ liệu chunk chưa hoàn chỉnh.");

            session.ReceivedChunks[chunkIndex] = true;
            session.UploadedBytes += expectedLength;
            if (!session.ReceivedChunks.All(received => received)) return new UploadChunkResponse(session.UploadedBytes, false);

            File.Move(session.TemporaryPath, Path.Combine(session.DestinationDirectory, session.StoredName));
            _sessions.TryRemove(session.Id, out _);
            _reservedPaths.TryRemove(session.ReservationKey, out _);
            return new UploadChunkResponse(session.UploadedBytes, true, session.StoredName);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task CancelUploadAsync(string uploadId)
    {
        if (!_sessions.TryRemove(uploadId, out var session)) return;
        await session.Gate.WaitAsync();
        try
        {
            if (File.Exists(session.TemporaryPath)) File.Delete(session.TemporaryPath);
            _reservedPaths.TryRemove(session.ReservationKey, out _);
        }
        finally { session.Gate.Release(); }
    }

    public FileListing List(string encodedPath)
    {
        var relativePath = NormalizeRelativePath(encodedPath) ?? throw BadRequest("Đường dẫn thư mục không hợp lệ.");
        var directory = ResolvePath(relativePath);
        if (directory is null || !Directory.Exists(directory)) throw NotFound("Thư mục không tồn tại.");

        var folders = Directory.EnumerateDirectories(directory)
            .Select(path => new DirectoryInfo(path))
            .Select(folder => new FileManagerEntry(folder.Name, "folder", null, folder.LastWriteTimeUtc));
        var files = Directory.EnumerateFiles(directory)
            .Select(path => new FileInfo(path))
            .Where(file => !IsTemporaryUpload(file.Name))
            .Select(file => new FileManagerEntry(file.Name, "file", file.Length, file.LastWriteTimeUtc));
        var entries = folders.Concat(files)
            .OrderBy(entry => entry.Type == "folder" ? 0 : 1)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new FileListing(relativePath, entries);
    }

    public void CreateFolder(CreateFolderRequest request)
    {
        var parentPath = NormalizeRelativePath(request.ParentPath) ?? throw BadRequest("Tên hoặc đường dẫn thư mục không hợp lệ.");
        var folderName = GetSafeFolderName(request.Name) ?? throw BadRequest("Tên hoặc đường dẫn thư mục không hợp lệ.");
        var folderPath = ResolvePath(JoinPath(parentPath, folderName)) ?? throw BadRequest("Đường dẫn thư mục không hợp lệ.");
        if (Directory.Exists(folderPath) || File.Exists(folderPath)) throw new FileManagerException("Tên thư mục đã tồn tại.", StatusCodes.Status409Conflict);
        Directory.CreateDirectory(folderPath);
    }

    public DownloadDescriptor GetDownload(string encodedPath)
    {
        var relativePath = NormalizeRelativePath(encodedPath);
        if (string.IsNullOrEmpty(relativePath) || IsTemporaryUpload(Path.GetFileName(relativePath))) throw NotFound("File không tồn tại.");
        var filePath = ResolvePath(relativePath);
        if (filePath is null || !File.Exists(filePath)) throw NotFound("File không tồn tại.");
        return new DownloadDescriptor(filePath, Path.GetFileName(filePath));
    }

    private UploadSession GetSession(string uploadId) => _sessions.TryGetValue(uploadId, out var session) ? session : throw NotFound("Phiên upload không tồn tại hoặc đã kết thúc.");

    private string ReserveUniqueFileName(string directory, string requestedName, out string reservationKey)
    {
        lock (_fileNameLock)
        {
            var baseName = Path.GetFileNameWithoutExtension(requestedName);
            var extension = Path.GetExtension(requestedName);
            var candidate = requestedName;
            var counter = 1;
            while (File.Exists(Path.Combine(directory, candidate)) || _reservedPaths.ContainsKey(Path.Combine(directory, candidate)))
                candidate = $"{baseName} ({counter++}){extension}";
            reservationKey = Path.Combine(directory, candidate);
            _reservedPaths.TryAdd(reservationKey, 0);
            return candidate;
        }
    }

    private string? ResolvePath(string relativePath)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar) ? _rootPath : _rootPath + Path.DirectorySeparatorChar;
        return absolutePath.Equals(_rootPath, StringComparison.OrdinalIgnoreCase) || absolutePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? absolutePath : null;
    }

    private static string? GetSafeFileName(string encodedName)
    {
        try
        {
            var fileName = Path.GetFileName(Uri.UnescapeDataString(encodedName));
            return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
        }
        catch (UriFormatException) { return null; }
    }

    private static string? NormalizeRelativePath(string encodedPath)
    {
        try
        {
            var decodedPath = Uri.UnescapeDataString(encodedPath).Replace("\\", "/").Trim('/');
            if (string.IsNullOrEmpty(decodedPath)) return string.Empty;
            var segments = decodedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.All(segment => GetSafeFolderName(segment) is not null) ? string.Join('/', segments) : null;
        }
        catch (UriFormatException) { return null; }
    }

    private static string? GetSafeFolderName(string name)
    {
        var trimmedName = name.Trim();
        return string.IsNullOrWhiteSpace(trimmedName) || trimmedName is "." or ".." || trimmedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || trimmedName.Contains('/') || trimmedName.Contains("\\") ? null : trimmedName;
    }

    private static bool IsTemporaryUpload(string fileName) => fileName.StartsWith(".", StringComparison.Ordinal) && fileName.EndsWith(".uploading", StringComparison.OrdinalIgnoreCase);
    private static string JoinPath(string left, string right) => string.IsNullOrEmpty(left) ? right : $"{left}/{right}";
    private static FileManagerException BadRequest(string message) => new(message, StatusCodes.Status400BadRequest);
    private static FileManagerException NotFound(string message) => new(message, StatusCodes.Status404NotFound);

    private static async Task<long> CopyExactAsync(Stream source, Stream destination, long expectedLength, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(UploadProtocol.CopyBufferSize);
        try
        {
            long copied = 0;
            while (copied < expectedLength)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, expectedLength - copied)), cancellationToken);
                if (read == 0) break;
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                copied += read;
            }
            return copied;
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }

    private sealed class UploadSession(string id, string storedName, string destinationDirectory, string reservationKey, string temporaryPath, long totalBytes, int totalChunks)
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
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public long GetChunkLength(int chunkIndex) => Math.Min(UploadProtocol.ChunkSize, TotalBytes - (long)chunkIndex * UploadProtocol.ChunkSize);
    }
}
