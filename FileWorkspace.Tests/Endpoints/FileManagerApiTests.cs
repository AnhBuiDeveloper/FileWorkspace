using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Text;
using FileWorkspace.Models;
using FileWorkspace.Tests.TestSupport;

namespace FileWorkspace.Tests.Endpoints;

public sealed class FileManagerApiTests
{
    [Fact]
    public async Task Protected_routes_reject_missing_or_incorrect_tokens()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var missing = await client.GetAsync("/api/files");
        var incorrectRequest = new HttpRequestMessage(HttpMethod.Get, "/api/files");
        incorrectRequest.Headers.Add("X-Upload-Token", "incorrect");
        var incorrect = await client.SendAsync(incorrectRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, incorrect.StatusCode);
    }

    [Fact]
    public async Task Authenticated_user_can_create_folder_upload_list_and_download()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        const string folder = "evidence";
        var bytes = Encoding.UTF8.GetBytes("api integration content");

        var createFolder = new HttpRequestMessage(HttpMethod.Post, "/api/folders")
        {
            Content = JsonContent.Create(new CreateFolderRequest("", folder))
        };
        createFolder.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        var folderResponse = await client.SendAsync(createFolder);
        Assert.Equal(HttpStatusCode.Created, folderResponse.StatusCode);

        var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/uploads");
        startRequest.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        startRequest.Headers.Add("X-File-Name", Uri.EscapeDataString("proof.txt"));
        startRequest.Headers.Add("X-File-Size", bytes.Length.ToString());
        startRequest.Headers.Add("X-Target-Folder", Uri.EscapeDataString(folder));
        var startResponse = await client.SendAsync(startRequest);
        startResponse.EnsureSuccessStatusCode();
        var start = await startResponse.Content.ReadFromJsonAsync<UploadStartResponse>();
        Assert.NotNull(start);

        var chunkRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/uploads/{start!.UploadId}/chunks/0")
        {
            Content = new ByteArrayContent(bytes)
        };
        chunkRequest.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        var chunkResponse = await client.SendAsync(chunkRequest);
        chunkResponse.EnsureSuccessStatusCode();
        var chunk = await chunkResponse.Content.ReadFromJsonAsync<UploadChunkResponse>();
        Assert.NotNull(chunk);
        Assert.True(chunk!.Completed);

        var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/files?path={folder}");
        listRequest.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        var listResponse = await client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();
        var listing = await listResponse.Content.ReadFromJsonAsync<FileListing>();
        var entry = Assert.Single(listing!.Entries);
        Assert.Equal("proof.txt", entry.Name);

        var download = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = TestWebApplicationFactory.AccessToken,
            ["path"] = $"{folder}/proof.txt"
        });
        var downloadResponse = await client.PostAsync("/api/files/download", download);
        downloadResponse.EnsureSuccessStatusCode();
        Assert.Equal(bytes, await downloadResponse.Content.ReadAsByteArrayAsync());

        var archive = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", TestWebApplicationFactory.AccessToken),
            new KeyValuePair<string, string>("path", folder)
        });
        var archiveResponse = await client.PostAsync("/api/files/archive", archive);
        archiveResponse.EnsureSuccessStatusCode();
        await using var archiveContent = new MemoryStream(await archiveResponse.Content.ReadAsByteArrayAsync());
        using var zip = new ZipArchive(archiveContent, ZipArchiveMode.Read);
        var archiveEntry = zip.GetEntry($"{folder}/proof.txt");
        Assert.NotNull(archiveEntry);

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/files?path={folder}/proof.txt");
        deleteRequest.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var deletedListRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/files?path={folder}");
        deletedListRequest.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        var deletedListResponse = await client.SendAsync(deletedListRequest);
        var deletedListing = await deletedListResponse.Content.ReadFromJsonAsync<FileListing>();
        Assert.Empty(deletedListing!.Entries);

        var bulkDeleteRequest = new HttpRequestMessage(HttpMethod.Post, "/api/files/delete")
        {
            Content = JsonContent.Create(new DeleteEntriesRequest([folder]))
        };
        bulkDeleteRequest.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        var bulkDeleteResponse = await client.SendAsync(bulkDeleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, bulkDeleteResponse.StatusCode);

        var rootListRequest = new HttpRequestMessage(HttpMethod.Get, "/api/files");
        rootListRequest.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        var rootListResponse = await client.SendAsync(rootListRequest);
        var rootListing = await rootListResponse.Content.ReadFromJsonAsync<FileListing>();
        Assert.Empty(rootListing!.Entries);
    }

    [Fact]
    public async Task Unsafe_folder_request_returns_a_client_error()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/folders")
        {
            Content = JsonContent.Create(new CreateFolderRequest("", "../outside"))
        };
        request.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
    }

    [Fact]
    public async Task Download_ticket_allows_ranged_file_download_without_the_access_token_in_the_url()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var bytes = Encoding.UTF8.GetBytes("ticketed file content");
        Directory.CreateDirectory(factory.UploadRoot);
        await File.WriteAllBytesAsync(Path.Combine(factory.UploadRoot, "proof.txt"), bytes);

        var createTicket = new HttpRequestMessage(HttpMethod.Post, "/api/files/download-tickets")
        {
            Content = JsonContent.Create(new DownloadTicketRequest("proof.txt"))
        };
        createTicket.Headers.Add("X-Upload-Token", TestWebApplicationFactory.AccessToken);
        var ticketResponse = await client.SendAsync(createTicket);
        ticketResponse.EnsureSuccessStatusCode();
        var ticket = await ticketResponse.Content.ReadFromJsonAsync<DownloadTicketResponse>();

        Assert.NotNull(ticket);
        Assert.StartsWith("/api/downloads/", ticket!.Url, StringComparison.Ordinal);
        Assert.DoesNotContain(TestWebApplicationFactory.AccessToken, ticket.Url, StringComparison.Ordinal);
        Assert.Equal(DateTimeOffset.UtcNow.AddHours(1), ticket.ExpiresAtUtc, TimeSpan.FromSeconds(5));

        var downloadRequest = new HttpRequestMessage(HttpMethod.Get, ticket.Url);
        downloadRequest.Headers.Range = new RangeHeaderValue(0, 5);
        var downloadResponse = await client.SendAsync(downloadRequest);

        Assert.Equal(HttpStatusCode.PartialContent, downloadResponse.StatusCode);
        Assert.Equal(bytes[..6], await downloadResponse.Content.ReadAsByteArrayAsync());

        var unknownResponse = await client.GetAsync("/api/downloads/not-a-ticket");
        Assert.Equal(HttpStatusCode.NotFound, unknownResponse.StatusCode);
    }
}
