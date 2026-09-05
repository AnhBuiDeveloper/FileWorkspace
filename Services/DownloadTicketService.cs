using System.Collections.Concurrent;
using System.Security.Cryptography;
using FileWorkspace.Models;

namespace FileWorkspace.Services;

public sealed class DownloadTicketService(TimeProvider timeProvider)
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromHours(1);
    private readonly ConcurrentDictionary<string, TicketedDownload> _tickets = new(StringComparer.Ordinal);

    public DownloadTicket Create(DownloadDescriptor download)
    {
        RemoveExpiredTickets();
        var file = new FileInfo(download.AbsolutePath);
        if (!file.Exists) throw new FileNotFoundException("The ticketed file no longer exists.", download.AbsolutePath);

        string value;
        do
        {
            value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
        while (_tickets.ContainsKey(value));

        var expiresAtUtc = timeProvider.GetUtcNow().Add(TicketLifetime);
        _tickets[value] = new TicketedDownload(download, file.Length, file.LastWriteTimeUtc, expiresAtUtc);
        return new DownloadTicket(value, expiresAtUtc);
    }

    public DownloadDescriptor? GetDownload(string ticket)
    {
        if (!_tickets.TryGetValue(ticket, out var ticketedDownload)) return null;
        if (ticketedDownload.ExpiresAtUtc > timeProvider.GetUtcNow() && ticketedDownload.MatchesCurrentFile()) return ticketedDownload.Download;

        _tickets.TryRemove(ticket, out _);
        return null;
    }

    private void RemoveExpiredTickets()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var ticket in _tickets)
            if (ticket.Value.ExpiresAtUtc <= now) _tickets.TryRemove(ticket.Key, out _);
    }

    private sealed record TicketedDownload(DownloadDescriptor Download, long Length, DateTime LastWriteTimeUtc, DateTimeOffset ExpiresAtUtc)
    {
        public bool MatchesCurrentFile()
        {
            var file = new FileInfo(Download.AbsolutePath);
            return file.Exists && file.Length == Length && file.LastWriteTimeUtc == LastWriteTimeUtc;
        }
    }
}
