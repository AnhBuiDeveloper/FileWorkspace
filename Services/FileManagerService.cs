using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using FileWorkspace.Models;

namespace FileWorkspace.Services;

public sealed class FileManagerService
{
    private static readonly TimeSpan IncompleteUploadRetention = TimeSpan.FromDays(7);
    private readonly string _rootPath;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, UploadSession> _sessions = new();
    private readonly ConcurrentDictionary<string, byte> _reservedPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _fileNameLock = new();

    public FileManagerService(IHostEnvironment environment)
        : this(environment, TimeProvider.System)
    {
    }

    public FileManagerService(IHostEnvironment environment, TimeProvider timeProvider)
    {
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Upload"));
        _timeProvider = timeProvider;
        Directory.CreateDirectory(_rootPath);
        CleanupExpiredUploads();
        RestoreActiveReservations();
    }

    public async Task<UploadStartResponse> StartUploadAsync(string encodedFileName, string encodedTargetFolder, long totalBytes, CancellationToken cancellationToken)
    {
        CleanupExpiredUploads();
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

            var session = new UploadSession(sessionId, storedName, targetFolder, destinationDirectory, reservationKey, temporaryPath, GetSessionManifestPath(sessionId), totalBytes, totalChunks, lastUpdatedUtc: _timeProvider.GetUtcNow().UtcDateTime);
            await PersistSessionAsync(session, cancellationToken);
            if (!_sessions.TryAdd(sessionId, session)) throw new InvalidOperationException("Không thể tạo phiên upload.");
            return new UploadStartResponse(sessionId, UploadProtocol.ChunkSize, false, 0);
        }
        catch
        {
            _reservedPaths.TryRemove(reservationKey, out _);
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            DeleteSessionManifest(sessionId);
            throw;
        }
    }

    public async Task<UploadStartResponse> ResumeUploadAsync(string uploadId, string encodedFileName, string encodedTargetFolder, long totalBytes, CancellationToken cancellationToken)
    {
        var session = GetSession(uploadId);
        var fileName = GetSafeFileName(encodedFileName) ?? throw BadRequest("Tên file không hợp lệ.");
        var targetFolder = NormalizeRelativePath(encodedTargetFolder) ?? throw BadRequest("Thư mục đích không hợp lệ.");
        if (session.StoredName != fileName || session.TargetFolder != targetFolder || session.TotalBytes != totalBytes)
            throw Conflict("Thông tin file khôi phục không khớp.");

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(session.TemporaryPath))
            {
                DeleteSession(session);
                throw NotFound("Phiên upload không tồn tại hoặc đã kết thúc.");
            }

            session.Touch(_timeProvider.GetUtcNow().UtcDateTime);
            await PersistSessionAsync(session, cancellationToken);
            return new UploadStartResponse(session.Id, UploadProtocol.ChunkSize, false, session.UploadedBytes, null, session.NextChunk);
        }
        finally { session.Gate.Release(); }
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
            session.Touch(_timeProvider.GetUtcNow().UtcDateTime);
            if (!session.ReceivedChunks.All(received => received))
            {
                await PersistSessionAsync(session, cancellationToken);
                return new UploadChunkResponse(session.UploadedBytes, false);
            }

            File.Move(session.TemporaryPath, Path.Combine(session.DestinationDirectory, session.StoredName));
            DeleteSession(session);
            return new UploadChunkResponse(session.UploadedBytes, true, session.StoredName);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task CancelUploadAsync(string uploadId)
    {
        if (!TryGetSession(uploadId, out var session)) return;
        _sessions.TryRemove(uploadId, out _);
        await session.Gate.WaitAsync();
        try
        {
            if (File.Exists(session.TemporaryPath)) File.Delete(session.TemporaryPath);
            DeleteSession(session);
        }
        finally { session.Gate.Release(); }
    }

    public FileListing List(string encodedPath)
    {
        var relativePath = NormalizeRelativePath(encodedPath) ?? throw BadRequest("Đường dẫn thư mục không hợp lệ.");
        var directory = ResolvePath(relativePath);
        if (directory is null || !Directory.Exists(directory)) throw NotFound("Thư mục không tồn tại.");

        var folders = Directory.EnumerateDirectories(directory)
            .Where(path => !IsUploadInternalFile(Path.GetFileName(path)))
            .Select(path => new DirectoryInfo(path))
            .Select(folder => new FileManagerEntry(folder.Name, "folder", null, folder.LastWriteTimeUtc));
        var files = Directory.EnumerateFiles(directory)
            .Select(path => new FileInfo(path))
            .Where(file => !IsUploadInternalFile(file.Name))
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
        if (string.IsNullOrEmpty(relativePath) || IsUploadInternalFile(Path.GetFileName(relativePath))) throw NotFound("File không tồn tại.");
        var filePath = ResolvePath(relativePath);
        if (filePath is null || !File.Exists(filePath)) throw NotFound("File không tồn tại.");
        return new DownloadDescriptor(filePath, Path.GetFileName(filePath));
    }

    public ArchiveDownload GetArchive(IEnumerable<string> encodedPaths)
    {
        var paths = encodedPaths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        if (paths.Length == 0) throw BadRequest("Cần chọn ít nhất một file hoặc folder.");

        var sources = new List<ArchiveSource>();
        var entryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var encodedPath in paths)
        {
            var relativePath = NormalizeRelativePath(encodedPath);
            if (string.IsNullOrEmpty(relativePath) || IsUploadInternalFile(Path.GetFileName(relativePath))) throw NotFound("File hoặc folder không tồn tại.");
            var absolutePath = ResolvePath(relativePath);
            if (absolutePath is null) throw BadRequest("Đường dẫn file hoặc folder không hợp lệ.");

            if (File.Exists(absolutePath)) AddArchiveSource(absolutePath, false, sources, entryNames);
            else if (Directory.Exists(absolutePath)) AddDirectorySources(absolutePath, sources, entryNames);
            else throw NotFound("File hoặc folder không tồn tại.");
        }

        return new ArchiveDownload(sources);
    }

    public async Task WriteArchiveAsync(ArchiveDownload archive, Stream destination, CancellationToken cancellationToken)
    {
        using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var source in archive.Sources)
        {
            var entry = zip.CreateEntry(source.EntryName, source.IsDirectory ? CompressionLevel.NoCompression : CompressionLevel.Fastest);
            if (source.IsDirectory) continue;

            await using var input = new FileStream(source.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, UploadProtocol.CopyBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var output = entry.Open();
            await input.CopyToAsync(output, UploadProtocol.CopyBufferSize, cancellationToken);
        }
    }

    public void DeleteFile(string encodedPath)
    {
        var relativePath = NormalizeRelativePath(encodedPath);
        if (string.IsNullOrEmpty(relativePath) || IsUploadInternalFile(Path.GetFileName(relativePath))) throw NotFound("File không tồn tại.");
        var filePath = ResolvePath(relativePath);
        if (filePath is null || !File.Exists(filePath)) throw NotFound("File không tồn tại.");
        File.Delete(filePath);
    }

    public void DeleteEntries(IEnumerable<string> encodedPaths)
    {
        var targets = GetDeletionTargets(encodedPaths);
        foreach (var target in targets.OrderByDescending(target => target.AbsolutePath.Length))
        {
            if (target.IsDirectory) Directory.Delete(target.AbsolutePath, recursive: true);
            else File.Delete(target.AbsolutePath);
        }
    }

    public void CleanupExpiredUploads()
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime - IncompleteUploadRetention;
        foreach (var manifestPath in Directory.EnumerateFiles(_rootPath, ".upload-session-*.json", SearchOption.TopDirectoryOnly))
        {
            var session = ReadPersistedSession(manifestPath);
            if (session is null || session.LastUpdatedUtc <= cutoff || !File.Exists(GetTemporaryPath(session)))
            {
                if (session is not null)
                {
                    _sessions.TryRemove(session.Id, out _);
                    DeletePersistedFiles(session);
                }
                else if (File.GetLastWriteTimeUtc(manifestPath) <= cutoff) File.Delete(manifestPath);
            }
        }

        foreach (var temporaryPath in Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories).Where(path => IsTemporaryUpload(Path.GetFileName(path))))
            if (File.GetLastWriteTimeUtc(temporaryPath) <= cutoff) File.Delete(temporaryPath);

        foreach (var temporaryManifestPath in Directory.EnumerateFiles(_rootPath, ".upload-session-*.tmp", SearchOption.TopDirectoryOnly))
            if (File.GetLastWriteTimeUtc(temporaryManifestPath) <= cutoff) File.Delete(temporaryManifestPath);
    }

    private UploadSession GetSession(string uploadId)
    {
        if (TryGetSession(uploadId, out var session)) return session;
        throw NotFound("Phiên upload không tồn tại hoặc đã kết thúc.");
    }

    private bool TryGetSession(string uploadId, out UploadSession session)
    {
        if (!IsValidUploadId(uploadId))
        {
            session = null!;
            return false;
        }
        if (_sessions.TryGetValue(uploadId, out session!)) return true;

        var persisted = ReadPersistedSession(GetSessionManifestPath(uploadId));
        if (persisted is null || persisted.Id != uploadId || IsExpired(persisted) || !File.Exists(GetTemporaryPath(persisted)))
        {
            if (persisted is not null) DeletePersistedFiles(persisted);
            session = null!;
            return false;
        }

        var restored = CreateSession(persisted);
        if (restored is null)
        {
            DeletePersistedFiles(persisted);
            session = null!;
            return false;
        }

        _reservedPaths.TryAdd(restored.ReservationKey, 0);
        session = _sessions.GetOrAdd(uploadId, restored);
        return true;
    }

    private void RestoreActiveReservations()
    {
        foreach (var manifestPath in Directory.EnumerateFiles(_rootPath, ".upload-session-*.json", SearchOption.TopDirectoryOnly))
        {
            var persisted = ReadPersistedSession(manifestPath);
            var session = persisted is null || IsExpired(persisted) ? null : CreateSession(persisted);
            if (session is null)
            {
                if (persisted is not null) DeletePersistedFiles(persisted);
                continue;
            }
            _reservedPaths.TryAdd(session.ReservationKey, 0);
        }
    }

    private UploadSession? CreateSession(PersistedUploadSession persisted)
    {
        if (!IsValidUploadId(persisted.Id) || GetSafeFileName(persisted.StoredName) != persisted.StoredName || NormalizeRelativePath(persisted.TargetFolder) != persisted.TargetFolder || persisted.TotalBytes < 0 || persisted.TotalChunks < 1 || persisted.ReceivedChunks.Length != persisted.TotalChunks)
            return null;
        var destinationDirectory = ResolvePath(persisted.TargetFolder);
        var temporaryPath = GetTemporaryPath(persisted);
        if (destinationDirectory is null || temporaryPath is null || !File.Exists(temporaryPath)) return null;
        var expectedChunks = checked((int)((persisted.TotalBytes + UploadProtocol.ChunkSize - 1) / UploadProtocol.ChunkSize));
        if (expectedChunks != persisted.TotalChunks) return null;
        var reservationKey = Path.Combine(destinationDirectory, persisted.StoredName);
        return new UploadSession(persisted.Id, persisted.StoredName, persisted.TargetFolder, destinationDirectory, reservationKey, temporaryPath, GetSessionManifestPath(persisted.Id), persisted.TotalBytes, persisted.TotalChunks, persisted.ReceivedChunks, persisted.LastUpdatedUtc);
    }

    private async Task PersistSessionAsync(UploadSession session, CancellationToken cancellationToken)
    {
        var temporaryManifestPath = Path.Combine(_rootPath, $".upload-session-{session.Id}.{Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant()}.tmp");
        var persisted = new PersistedUploadSession(session.Id, session.StoredName, session.TargetFolder, session.TotalBytes, session.TotalChunks, session.ReceivedChunks, session.LastUpdatedUtc);
        try
        {
            await File.WriteAllTextAsync(temporaryManifestPath, JsonSerializer.Serialize(persisted), cancellationToken);
            File.Move(temporaryManifestPath, session.ManifestPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryManifestPath)) File.Delete(temporaryManifestPath);
        }
    }

    private PersistedUploadSession? ReadPersistedSession(string manifestPath)
    {
        try { return File.Exists(manifestPath) ? JsonSerializer.Deserialize<PersistedUploadSession>(File.ReadAllText(manifestPath)) : null; }
        catch (JsonException) { return null; }
        catch (IOException) { return null; }
    }

    private void DeleteSession(UploadSession session)
    {
        _sessions.TryRemove(session.Id, out _);
        _reservedPaths.TryRemove(session.ReservationKey, out _);
        DeleteSessionManifest(session.Id);
    }

    private void DeletePersistedFiles(PersistedUploadSession session)
    {
        var temporaryPath = GetTemporaryPath(session);
        if (temporaryPath is not null && File.Exists(temporaryPath)) File.Delete(temporaryPath);
        var destinationDirectory = ResolvePath(session.TargetFolder);
        if (destinationDirectory is not null) _reservedPaths.TryRemove(Path.Combine(destinationDirectory, session.StoredName), out _);
        DeleteSessionManifest(session.Id);
    }

    private void DeleteSessionManifest(string uploadId)
    {
        if (!IsValidUploadId(uploadId)) return;
        var manifestPath = GetSessionManifestPath(uploadId);
        if (File.Exists(manifestPath)) File.Delete(manifestPath);
    }

    private bool IsExpired(PersistedUploadSession session) => session.LastUpdatedUtc <= _timeProvider.GetUtcNow().UtcDateTime - IncompleteUploadRetention;
    private string GetSessionManifestPath(string uploadId) => Path.Combine(_rootPath, $".upload-session-{uploadId}.json");
    private string? GetTemporaryPath(PersistedUploadSession session)
    {
        var destinationDirectory = ResolvePath(session.TargetFolder);
        return destinationDirectory is null ? null : Path.Combine(destinationDirectory, $".{session.Id}.uploading");
    }
    private static bool IsValidUploadId(string uploadId) => uploadId.Length == 32 && uploadId.All(Uri.IsHexDigit);

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

    private void AddDirectorySources(string directoryPath, List<ArchiveSource> sources, HashSet<string> entryNames)
    {
        AddArchiveSource(directoryPath, true, sources, entryNames);
        foreach (var directory in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories))
            AddArchiveSource(directory, true, sources, entryNames);
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            if (!IsUploadInternalFile(Path.GetFileName(file))) AddArchiveSource(file, false, sources, entryNames);
    }

    private void AddArchiveSource(string absolutePath, bool isDirectory, List<ArchiveSource> sources, HashSet<string> entryNames)
    {
        var entryName = Path.GetRelativePath(_rootPath, absolutePath).Replace(Path.DirectorySeparatorChar, '/');
        if (isDirectory) entryName += "/";
        if (entryNames.Add(entryName)) sources.Add(new ArchiveSource(absolutePath, entryName, isDirectory));
    }

    private IReadOnlyList<DeletionTarget> GetDeletionTargets(IEnumerable<string> encodedPaths)
    {
        var targets = new List<DeletionTarget>();
        var relativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var encodedPath in encodedPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var relativePath = NormalizeRelativePath(encodedPath);
            if (string.IsNullOrEmpty(relativePath) || IsUploadInternalFile(Path.GetFileName(relativePath))) throw NotFound("File hoặc folder không tồn tại.");
            if (!relativePaths.Add(relativePath)) continue;

            var absolutePath = ResolvePath(relativePath);
            if (absolutePath is null) throw BadRequest("Đường dẫn file hoặc folder không hợp lệ.");
            if (File.Exists(absolutePath)) targets.Add(new DeletionTarget(absolutePath, false));
            else if (Directory.Exists(absolutePath))
            {
                if (Directory.EnumerateFiles(absolutePath, "*", SearchOption.AllDirectories).Any(path => IsUploadInternalFile(Path.GetFileName(path))))
                    throw Conflict("Không thể xóa folder đang có upload chưa hoàn tất.");
                targets.Add(new DeletionTarget(absolutePath, true));
            }
            else throw NotFound("File hoặc folder không tồn tại.");
        }

        if (targets.Count == 0) throw BadRequest("Cần chọn ít nhất một file hoặc folder.");
        return targets.Where(target => !targets.Any(parent => parent.IsDirectory && parent.AbsolutePath != target.AbsolutePath && IsPathInside(target.AbsolutePath, parent.AbsolutePath))).ToArray();
    }

    private static bool IsPathInside(string path, string directory)
    {
        var directoryWithSeparator = directory.EndsWith(Path.DirectorySeparatorChar) ? directory : directory + Path.DirectorySeparatorChar;
        return path.StartsWith(directoryWithSeparator, StringComparison.OrdinalIgnoreCase);
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
    private static bool IsUploadInternalFile(string fileName) => IsTemporaryUpload(fileName) || fileName.StartsWith(".upload-session-", StringComparison.Ordinal);
    private static string JoinPath(string left, string right) => string.IsNullOrEmpty(left) ? right : $"{left}/{right}";
    private static FileManagerException BadRequest(string message) => new(message, StatusCodes.Status400BadRequest);
    private static FileManagerException NotFound(string message) => new(message, StatusCodes.Status404NotFound);
    private static FileManagerException Conflict(string message) => new(message, StatusCodes.Status409Conflict);

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

    private sealed class UploadSession(string id, string storedName, string targetFolder, string destinationDirectory, string reservationKey, string temporaryPath, string manifestPath, long totalBytes, int totalChunks, bool[]? receivedChunks = null, DateTime? lastUpdatedUtc = null)
    {
        public string Id { get; } = id;
        public string StoredName { get; } = storedName;
        public string TargetFolder { get; } = targetFolder;
        public string DestinationDirectory { get; } = destinationDirectory;
        public string ReservationKey { get; } = reservationKey;
        public string TemporaryPath { get; } = temporaryPath;
        public string ManifestPath { get; } = manifestPath;
        public long TotalBytes { get; } = totalBytes;
        public int TotalChunks { get; } = totalChunks;
        public bool[] ReceivedChunks { get; } = receivedChunks ?? new bool[totalChunks];
        public long UploadedBytes { get; set; } = CalculateUploadedBytes(receivedChunks, totalBytes);
        public DateTime LastUpdatedUtc { get; private set; } = lastUpdatedUtc ?? DateTime.UtcNow;
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public long GetChunkLength(int chunkIndex) => Math.Min(UploadProtocol.ChunkSize, TotalBytes - (long)chunkIndex * UploadProtocol.ChunkSize);
        public int NextChunk
        {
            get
            {
                var index = Array.FindIndex(ReceivedChunks, received => !received);
                return index >= 0 ? index : TotalChunks;
            }
        }
        public void Touch(DateTime updatedAtUtc) => LastUpdatedUtc = updatedAtUtc;

        private static long CalculateUploadedBytes(bool[]? receivedChunks, long totalBytes)
        {
            if (receivedChunks is null) return 0;
            return receivedChunks.Select((received, index) => received ? Math.Min(UploadProtocol.ChunkSize, totalBytes - (long)index * UploadProtocol.ChunkSize) : 0).Sum();
        }
    }

    private sealed record PersistedUploadSession(string Id, string StoredName, string TargetFolder, long TotalBytes, int TotalChunks, bool[] ReceivedChunks, DateTime LastUpdatedUtc);
    private sealed record DeletionTarget(string AbsolutePath, bool IsDirectory);
}
