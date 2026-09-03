using FileUpload.Configuration;
using FileUpload.Endpoints;
using FileUpload.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddProjectEnvironmentFile(builder.Environment.ContentRootPath);
builder.Configuration.AddEnvironmentVariables();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = null);

builder.Services.AddSingleton<UploadTokenValidator>();
builder.Services.AddSingleton<FileManagerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFileUploadEndpoints();

app.Run();

public partial class Program
{
}
