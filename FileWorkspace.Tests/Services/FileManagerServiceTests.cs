using System.Text;
using System.IO.Compression;
using FileWorkspace.Models;
using FileWorkspace.Services;
using FileWorkspace.Tests.TestSupport;

namespace FileWorkspace.Tests.Services;

public sealed class FileManagerServiceTests
{
    [Fact]
    public async Task Upload_completion_makes_file_visible_and_downloadable()
    {
        using var workspace = new TemporaryWorkspace();
        var service = new FileManagerService(workspace.Environment);
        var bytes = Encoding.UTF8.GetBytes("hello workspace");

        service.CreateFolder(new CreateFolderRequest("", "documents"));
        var start = await service.StartUploadAsync("note.txt", "documents", bytes.Length, CancellationToken.None);
        var result = await service.WriteChunkAsync(start.UploadId, 0, bytes.Length, new MemoryStream(bytes), CancellationToken.None);

        Assert.True(result.Completed);
        Assert.Equal("note.txt", result.FileName);
        var listing = service.List("documents");
        var entry = Assert.Single(listing.Entries);
        Assert.Equal("note.txt", entry.Name);
        Assert.Equal("file", entry.Type);
        Assert.Equal(bytes.Length, entry.Bytes);

        var download = service.GetDownload("documents/note.txt");
        Assert.Equal(bytes, await File.ReadAllBytesAsync(download.AbsolutePath));
    }

    [Fact]
    public async Task Incomplete_uploads_are_hidden_and_cancel_removes_the_temporary_file()
    {
        using var workspace = new TemporaryWorkspace();
        var service = new FileManagerService(workspace.Environment);

        var start = await service.StartUploadAsync("movie.mp4", "", UploadProtocol.ChunkSize + 1L, CancellationToken.None);

        Assert.Empty(service.List("").Entries);
        Assert.Single(Directory.EnumerateFiles(workspace.UploadRoot, "*.uploading", SearchOption.TopDirectoryOnly));

        await service.CancelUploadAsync(start.UploadId);

        Assert.Empty(Directory.EnumerateFiles(workspace.UploadRoot, "*.uploading", SearchOption.TopDirectoryOnly));
        Assert.Empty(service.List("").Entries);
    }

    [Fact]
    public async Task Upload_can_resume_after_the_service_restarts()
    {
        using var workspace = new TemporaryWorkspace();
        var firstService = new FileManagerService(workspace.Environment);
        var bytes = Enumerable.Range(0, UploadProtocol.ChunkSize + 3).Select(index => (byte)(index % 251)).ToArray();
        var start = await firstService.StartUploadAsync("resume.bin", "", bytes.Length, CancellationToken.None);
        await firstService.WriteChunkAsync(start.UploadId, 0, UploadProtocol.ChunkSize, new MemoryStream(bytes[..UploadProtocol.ChunkSize]), CancellationToken.None);

        var restartedService = new FileManagerService(workspace.Environment);
        var resumed = await restartedService.ResumeUploadAsync(start.UploadId, "resume.bin", "", bytes.Length, CancellationToken.None);

        Assert.Equal(UploadProtocol.ChunkSize, resumed.UploadedBytes);
        Assert.Equal(1, resumed.NextChunk);
        var completed = await restartedService.WriteChunkAsync(start.UploadId, 1, 3, new MemoryStream(bytes[UploadProtocol.ChunkSize..]), CancellationToken.None);
        Assert.True(completed.Completed);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(workspace.UploadRoot, "resume.bin")));
        Assert.Empty(Directory.EnumerateFiles(workspace.UploadRoot, "*.uploading", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(workspace.UploadRoot, ".upload-session-*.json", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public async Task Expired_incomplete_uploads_are_cleaned_after_seven_days()
    {
        using var workspace = new TemporaryWorkspace();
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero));
        var service = new FileManagerService(workspace.Environment, clock);
        await service.StartUploadAsync("stale.bin", "", UploadProtocol.ChunkSize + 1L, CancellationToken.None);

        clock.Advance(TimeSpan.FromDays(7).Add(TimeSpan.FromSeconds(1)));
        _ = new FileManagerService(workspace.Environment, clock);

        Assert.Empty(Directory.EnumerateFiles(workspace.UploadRoot, "*.uploading", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(workspace.UploadRoot, ".upload-session-*.json", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public async Task Uploads_with_the_same_name_receive_distinct_names()
    {
        using var workspace = new TemporaryWorkspace();
        var service = new FileManagerService(workspace.Environment);
        var bytes = new byte[] { 1 };

        var first = await service.StartUploadAsync("report.txt", "", bytes.Length, CancellationToken.None);
        var second = await service.StartUploadAsync("report.txt", "", bytes.Length, CancellationToken.None);
        var firstResult = await service.WriteChunkAsync(first.UploadId, 0, bytes.Length, new MemoryStream(bytes), CancellationToken.None);
        var secondResult = await service.WriteChunkAsync(second.UploadId, 0, bytes.Length, new MemoryStream(bytes), CancellationToken.None);

        Assert.Equal("report.txt", firstResult.FileName);
        Assert.Equal("report (1).txt", secondResult.FileName);
    }

    [Fact]
    public async Task Archive_preserves_selected_file_and_folder_contents()
    {
        using var workspace = new TemporaryWorkspace();
        var service = new FileManagerService(workspace.Environment);
        service.CreateFolder(new CreateFolderRequest("", "documents"));
        var bytes = Encoding.UTF8.GetBytes("archive content");
        var upload = await service.StartUploadAsync("note.txt", "documents", bytes.Length, CancellationToken.None);
        await service.WriteChunkAsync(upload.UploadId, 0, bytes.Length, new MemoryStream(bytes), CancellationToken.None);

        var archive = service.GetArchive(["documents", "documents/note.txt"]);
        await using var output = new MemoryStream();
        await service.WriteArchiveAsync(archive, output, CancellationToken.None);
        output.Position = 0;
        using var zip = new ZipArchive(output, ZipArchiveMode.Read);

        var entry = zip.GetEntry("documents/note.txt");
        Assert.NotNull(entry);
        await using var input = entry!.Open();
        using var reader = new StreamReader(input);
        Assert.Equal("archive content", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task DeleteFile_removes_only_completed_file_inside_workspace()
    {
        using var workspace = new TemporaryWorkspace();
        var service = new FileManagerService(workspace.Environment);
        var bytes = new byte[] { 1 };
        var upload = await service.StartUploadAsync("remove.txt", "", bytes.Length, CancellationToken.None);
        await service.WriteChunkAsync(upload.UploadId, 0, bytes.Length, new MemoryStream(bytes), CancellationToken.None);

        service.DeleteFile("remove.txt");

        Assert.Empty(service.List("").Entries);
        var exception = Assert.Throws<FileManagerException>(() => service.GetDownload("remove.txt"));
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task DeleteEntries_removes_selected_folders_recursively_and_deduplicates_descendants()
    {
        using var workspace = new TemporaryWorkspace();
        var service = new FileManagerService(workspace.Environment);
        service.CreateFolder(new CreateFolderRequest("", "documents"));
        var bytes = new byte[] { 1 };
        var upload = await service.StartUploadAsync("note.txt", "documents", bytes.Length, CancellationToken.None);
        await service.WriteChunkAsync(upload.UploadId, 0, bytes.Length, new MemoryStream(bytes), CancellationToken.None);

        service.DeleteEntries(["documents", "documents/note.txt"]);

        Assert.Empty(service.List("").Entries);
        var exception = Assert.Throws<FileManagerException>(() => service.GetDownload("documents/note.txt"));
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task DeleteEntries_rejects_folder_with_an_incomplete_upload()
    {
        using var workspace = new TemporaryWorkspace();
        var service = new FileManagerService(workspace.Environment);
        service.CreateFolder(new CreateFolderRequest("", "documents"));
        await service.StartUploadAsync("note.txt", "documents", 1, CancellationToken.None);

        var exception = Assert.Throws<FileManagerException>(() => service.DeleteEntries(["documents"]));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Single(service.List("").Entries);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../outside")]
    [InlineData("%2E%2E%2Foutside")]
    public async Task Upload_rejects_unsafe_target_paths(string path)
    {
        using var workspace = new TemporaryWorkspace();
        var service = new FileManagerService(workspace.Environment);

        var exception = await Assert.ThrowsAsync<FileManagerException>(() =>
            service.StartUploadAsync("safe.txt", path, 1, CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
