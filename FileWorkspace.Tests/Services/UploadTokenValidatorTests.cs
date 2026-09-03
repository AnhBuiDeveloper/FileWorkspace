using FileWorkspace.Services;
using Microsoft.Extensions.Configuration;

namespace FileWorkspace.Tests.Services;

public sealed class UploadTokenValidatorTests
{
    [Fact]
    public void Matches_returns_true_only_for_the_exact_token()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["UPLOAD_ACCESS_TOKEN"] = "correct-token" })
            .Build();
        var validator = new UploadTokenValidator(configuration);

        Assert.True(validator.Matches("correct-token"));
        Assert.False(validator.Matches("wrong-token"));
        Assert.False(validator.Matches("correct-token-with-extra-text"));
    }

    [Fact]
    public void Constructor_requires_an_access_token()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => new UploadTokenValidator(configuration));
    }
}
