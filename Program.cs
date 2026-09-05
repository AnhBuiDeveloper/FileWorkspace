using FileWorkspace.Configuration;
using FileWorkspace.Endpoints;
using FileWorkspace.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddProjectEnvironmentFile(builder.Environment.ContentRootPath);
builder.Configuration.AddEnvironmentVariables();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = null);

builder.Services.AddSingleton<UploadTokenValidator>();
builder.Services.AddSingleton<FileManagerService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DownloadTicketService>();
builder.Services.AddHostedService<UploadCleanupHostedService>();
builder.Services.AddFileWorkspaceRateLimiting();

var app = builder.Build();

// Honor X-Forwarded-For/-Proto from a reverse proxy so the rate limiter partitions by the
// real client IP (not the proxy's) and HSTS reflects the actual original scheme.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.MapFileWorkspaceEndpoints();

app.Run();

public partial class Program
{
}
