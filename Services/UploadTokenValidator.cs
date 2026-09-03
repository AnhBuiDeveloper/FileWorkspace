using System.Security.Cryptography;
using System.Text;

namespace FileUpload.Services;

public sealed class UploadTokenValidator
{
    private readonly string _expectedToken;

    public UploadTokenValidator(IConfiguration configuration)
    {
        _expectedToken = configuration["UPLOAD_ACCESS_TOKEN"]
            ?? throw new InvalidOperationException("Thiếu biến môi trường UPLOAD_ACCESS_TOKEN.");
    }

    public bool Matches(string suppliedToken)
    {
        if (suppliedToken.Length != _expectedToken.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(_expectedToken),
            Encoding.UTF8.GetBytes(suppliedToken));
    }
}
