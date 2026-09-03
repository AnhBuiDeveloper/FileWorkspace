using System.Text;
using FileUpload.Models;
using FileUpload.Services;
using FileUpload.Tests.TestSupport;

namespace FileUpload.Tests.Services;

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
}
