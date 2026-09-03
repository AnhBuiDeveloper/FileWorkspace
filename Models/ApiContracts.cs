namespace FileUpload.Models;

public sealed record ErrorResponse(string Error);
public sealed record CreateFolderRequest(string ParentPath, string Name);
public sealed record FileManagerEntry(string Name, string Type, long? Bytes, DateTime ModifiedAtUtc);
public sealed record FileListing(string Path, IReadOnlyList<FileManagerEntry> Entries);
public sealed record UploadStartResponse(string UploadId, int ChunkSize, bool Completed, long UploadedBytes, string? FileName = null);
public sealed record UploadChunkResponse(long UploadedBytes, bool Completed, string? FileName = null);
public sealed record DownloadDescriptor(string AbsolutePath, string FileName);

public static class UploadProtocol
{
    public const int ChunkSize = 8 * 1024 * 1024;
    public const int CopyBufferSize = 1024 * 1024;
}
