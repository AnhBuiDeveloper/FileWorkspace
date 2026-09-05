namespace FileWorkspace.Services;

public sealed class UploadCleanupHostedService(FileManagerService files) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken)) files.CleanupExpiredUploads();
    }
}
