using FileWorkspace.Configuration;
using FileWorkspace.Endpoints;
using FileWorkspace.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddProjectEnvironmentFile(builder.Environment.ContentRootPath);
builder.Configuration.AddEnvironmentVariables();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = null);

builder.Services.AddSingleton<UploadTokenValidator>();
builder.Services.AddSingleton<FileManagerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFileWorkspaceEndpoints();

app.Run();

public partial class Program
{
}
