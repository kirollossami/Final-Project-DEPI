using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterStudentAsync(StudentRegisterRequest request);
    Task<AuthResponse> RegisterLandLordAsync(LandLordRegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(string token);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);
<<<<<<< HEAD
    Task<TwoFactorSetupResponse> SetupTwoFactorAsync(string email);
    Task<AuthResponse> EnableTwoFactorAsync(EnableTwoFactorRequest request);
    Task<AuthResponse> VerifyTwoFactorAsync(TwoFactorVerifyRequest request);
=======
>>>>>>> d373b1145cc825f184dd583507a557a4aaf9a1f0
}
