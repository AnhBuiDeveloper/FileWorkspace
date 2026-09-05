using FileWorkspace.Models;
using FileWorkspace.Services;

namespace FileWorkspace.Tests.Services;

public sealed class DownloadTicketServiceTests
{
    [Fact]
    public async Task Ticket_is_scoped_to_one_download_and_expires_after_one_hour()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero));
        var service = new DownloadTicketService(clock);
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        await File.WriteAllTextAsync(filePath, "ticket test");
        var download = new DownloadDescriptor(filePath, "proof.txt");

        try
        {
            var ticket = service.Create(download);

            Assert.Equal(clock.GetUtcNow().AddHours(1), ticket.ExpiresAtUtc);
            Assert.Equal(download, service.GetDownload(ticket.Value));

            clock.Advance(TimeSpan.FromHours(1));

            Assert.Null(service.GetDownload(ticket.Value));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
