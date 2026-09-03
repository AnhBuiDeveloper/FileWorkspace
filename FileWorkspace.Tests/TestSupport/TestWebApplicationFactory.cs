using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FileWorkspace.Tests.TestSupport;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "file-workspace-api-tests", Guid.NewGuid().ToString("N"));

    public string UploadRoot => Path.Combine(_contentRoot, "Upload");
    public const string AccessToken = "integration-test-token";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_contentRoot);
        builder.UseContentRoot(_contentRoot);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UPLOAD_ACCESS_TOKEN"] = AccessToken
            }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_contentRoot))
            Directory.Delete(_contentRoot, recursive: true);
    }
}
