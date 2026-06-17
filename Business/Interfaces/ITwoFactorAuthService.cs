namespace Business.Interfaces;

public interface ITwoFactorAuthService
{
    string GenerateSecret();
    string GenerateQrCodeUri(string email, string secret);
    bool VerifyCode(string secret, string code);
}
