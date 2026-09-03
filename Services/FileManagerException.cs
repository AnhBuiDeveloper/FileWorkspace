namespace FileUpload.Services;

public sealed class FileManagerException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
