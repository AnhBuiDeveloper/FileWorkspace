using System.Net;
using System.Net.Http.Json;
using System.Text;
using FileUpload.Models;
using FileUpload.Tests.TestSupport;

namespace FileUpload.Tests.Endpoints;

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
}
