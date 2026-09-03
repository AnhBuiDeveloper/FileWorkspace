using Microsoft.Extensions.FileProviders;

namespace FileWorkspace.Tests.TestSupport;

public sealed class TemporaryWorkspace : IDisposable
{
    public TemporaryWorkspace()
    {
        ContentRoot = Path.Combine(Path.GetTempPath(), "file-workspace-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ContentRoot);
        Environment = new TestHostEnvironment(ContentRoot);
    }

    public string ContentRoot { get; }
    public string UploadRoot => Path.Combine(ContentRoot, "Upload");
    public TestHostEnvironment Environment { get; }

    public void Dispose()
    {
        if (Directory.Exists(ContentRoot))
            Directory.Delete(ContentRoot, recursive: true);
    }
}

public sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "FileWorkspace.Tests";
    public string ContentRootPath { get; set; } = contentRoot;
    public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRoot);
}
