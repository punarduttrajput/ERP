namespace ERP.Auth.Application.Services;

public interface ITotpService
{
    string GenerateSecret();
    string GetQrCodeUri(string secret, string email, string issuer);
    bool Verify(string secret, string code);
}
