using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Helpers;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Google.Apis.Auth;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Business.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IStudentRepository _studentRepository;
    private readonly ILandLordRepository _landLordRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IConfiguration _configuration;
    private readonly ITwoFactorAuthService _twoFactorAuthService;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        IStudentRepository studentRepository,
        ILandLordRepository landLordRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenBlacklistService tokenBlacklistService,
        IConfiguration configuration,
        ITwoFactorAuthService twoFactorAuthService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _studentRepository = studentRepository;
        _landLordRepository = landLordRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenBlacklistService = tokenBlacklistService;
        _configuration = configuration;
        _twoFactorAuthService = twoFactorAuthService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.InvalidCredentials
            };
        }

        if (!user.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.AccountInactive
            };
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Optimize: Only query based on user's role
        var studentId = (Guid?)null;
        var landlordId = (Guid?)null;
        var universityVerificationStatus = (int?)null;

        if (roles.Contains("Student"))
        {
            var student = await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id);
            studentId = student?.StudentId;
            universityVerificationStatus = (int?)student?.UniversityVerificationStatus;
        }
        else if (roles.Contains("LandLord"))
        {
            landlordId = (await _landLordRepository.GetAll().FirstOrDefaultAsync(l => l.UserId == user.Id))?.LandLordId;
        }

        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            IsRevoked = false,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.Insert(refreshTokenEntity);
        await _refreshTokenRepository.CommitAsync();

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.LoginSuccess,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiryMinutes * 60 // Convert to seconds
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToArray(),
                StudentId = studentId,
                LandLordId = landlordId,
                UniversityVerificationStatus = universityVerificationStatus
            }
        };
    }

    public async Task<AuthResponse> RegisterStudentAsync(StudentRegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.EmailAlreadyExists
            };
        }

        // Check if student already exists with this email (via User)
        var existingStudent = await _studentRepository.GetAll()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.User != null && s.User.Email == request.Email);
        
        if (existingStudent != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Student with this email already exists"
            };
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        await _userManager.AddToRoleAsync(user, "Student");

        var student = new Domain.Entities.Student
        {
            StudentId = Guid.NewGuid(),
            UserId = user.Id,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            City = request.City,
            PreferredArea = request.PreferredArea,
            NationalId = request.NationalId,
            IsOnboardingComplete = true // Manual registration is complete
        };

        await _studentRepository.Insert(student);
        await _studentRepository.CommitAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            IsRevoked = false,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.Insert(refreshTokenEntity);
        await _refreshTokenRepository.CommitAsync();

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.RegistrationSuccess,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiryMinutes * 60 // Convert to seconds
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToArray(),
                StudentId = student.StudentId,
                UniversityVerificationStatus = (int)student.UniversityVerificationStatus
            }
        };
    }

    public async Task<AuthResponse> RegisterLandLordAsync(LandLordRegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.EmailAlreadyExists
            };
        }

        // Check if landlord already exists with this email (via User)
        var existingLandlord = await _landLordRepository.GetAll()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.User != null && l.User.Email == request.Email);
        
        if (existingLandlord != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Landlord with this email already exists"
            };
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        await _userManager.AddToRoleAsync(user, "LandLord");

        var landlord = new Domain.Entities.LandLord
        {
            LandLordId = Guid.NewGuid(),
            UserId = user.Id,
            CompanyName = request.CompanyName,
            NationalId = request.NationalId,
            NationalIdImageUrl = string.Empty,
            PropertyOwnerShipProof = request.PropertyOwnerShipProof,
            HousingUnitDocumentationUrl = string.Empty,
            VerificationStatus = "Pending",
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _landLordRepository.Insert(landlord);
        await _landLordRepository.CommitAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            IsRevoked = false,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.Insert(refreshTokenEntity);
        await _refreshTokenRepository.CommitAsync();

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.RegistrationSuccess,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiryMinutes * 60 // Convert to seconds
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToArray(),
                LandLordId = landlord.LandLordId
            }
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Refresh token is required"
            };
        }

        var storedRefreshToken = await _refreshTokenRepository.GetValidTokenAsync(request.RefreshToken);
        if (storedRefreshToken == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            };
        }

        var principal = _tokenService.GetPrincipalFromExpiredToken(request.Token);
        var userId = principal?.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.InvalidToken
            };
        }

        if (storedRefreshToken.UserId != userId)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Refresh token does not belong to this user"
            };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.UserNotFound
            };
        }

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.Update(storedRefreshToken);
        await _refreshTokenRepository.CommitAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var newRefreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            IsRevoked = false,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.Insert(newRefreshTokenEntity);
        await _refreshTokenRepository.CommitAsync();

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.TokenRefreshed,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = expiryMinutes * 60 // Convert to seconds
            }
        };
    }

    public async Task<bool> LogoutAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        if (_tokenBlacklistService.IsTokenBlacklisted(token))
        {
            return false;
        }

        try
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(token);
            var userId = principal?.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            _tokenBlacklistService.BlacklistToken(token);

            await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
            await _refreshTokenRepository.CommitAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        try
        {
            // Use provider claims (Provider and ProviderKey) instead of validating an id_token here.
            if (request == null || string.IsNullOrEmpty(request.Provider) || string.IsNullOrEmpty(request.ProviderKey))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid external login request. Provider and ProviderKey are required."
                };
            }

            if (!string.Equals(request.Provider, "Google", StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Unsupported external provider"
                };
            }

            var googleId = request.ProviderKey;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (user == null)
            {
                // Check if user exists with same email but no GoogleId (merge accounts)
                user = await _userManager.FindByEmailAsync(request.Email);

                if (user != null)
                {
                    // Link existing account with Google
                    user.GoogleId = googleId;
                    user.IsGoogleUser = true;
                    await _userManager.UpdateAsync(user);
                }
                else
                {
                    // Create new user with Google account
                    user = new User
                    {
                        UserName = request.Email,
                        Email = request.Email,
                        GoogleId = googleId,
                        IsGoogleUser = true,
                        IsActive = true,
                        ProfileImage = null
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return new AuthResponse
                        {
                            Success = false,
                            Message = $"Failed to create user from Google account: {string.Join(", ", createResult.Errors.Select(e => e.Description))}"
                        };
                    }

                    await _userManager.AddToRoleAsync(user, "Student");

                    // Create Student entity with default values for Google users
                    var studentEntity = new Domain.Entities.Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = user.Id,
                        DateOfBirth = DateTime.UtcNow.AddYears(-18),
                        Gender = Domain.Enums.Gender.Male,
                        Address = null,
                        City = null,
                        PreferredArea = null,
                        NationalId = null,
                        IsOnboardingComplete = false
                    };

                    await _studentRepository.Insert(studentEntity);
                    await _studentRepository.CommitAsync();
                }
            }

            // Reject login if account has been deactivated
            if (!user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Your account has been deactivated. Please contact support."
                };
            }
            if (user.TwoFactorEnabled)
            {
                return new AuthResponse
                {
                    Success = true,
                    Message = "Two-factor authentication required. Please verify your code.",
                    RequiresTwoFactor = true,
                    User = new UserResponse
                    {
                        Id = user.Id,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Roles = (await _userManager.GetRolesAsync(user)).ToArray()
                    }
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Optimize: Only query based on user's role
            var studentId = (Guid?)null;
            var landlordId = (Guid?)null;
            var universityVerificationStatus = (int?)null;

            if (roles.Contains("Student"))
            {
                var student = await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id);
                studentId = student?.StudentId;
                universityVerificationStatus = (int?)student?.UniversityVerificationStatus;
            }
            else if (roles.Contains("LandLord"))
            {
                landlordId = (await _landLordRepository.GetAll().FirstOrDefaultAsync(l => l.UserId == user.Id))?.LandLordId;
            }

            var refreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                IsRevoked = false,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.Insert(refreshTokenEntity);
            await _refreshTokenRepository.CommitAsync();

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            return new AuthResponse
            {
                Success = true,
                Message = ErrorMessageHelper.LoginSuccess,
                Token = new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = expiryMinutes * 60 // Convert to seconds
                },
                User = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToArray(),
                    StudentId = studentId,
                    LandLordId = landlordId,
                    UniversityVerificationStatus = universityVerificationStatus
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Google login failed: {ex.Message}"
            };
        }
    }

    public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return new TwoFactorSetupResponse
            {
                Success = false,
                Message = ErrorMessageHelper.UserNotFound
            };
        }

        if (user.TwoFactorEnabled)
        {
            return new TwoFactorSetupResponse
            {
                Success = false,
                Message = "Two-factor authentication is already enabled for this account"
            };
        }

        var secret = _twoFactorAuthService.GenerateSecret();
        var qrCodeUri = _twoFactorAuthService.GenerateQrCodeUri(user.Email, secret);

        user.TwoFactorSecret = secret;
        await _userManager.UpdateAsync(user);

        return new TwoFactorSetupResponse
        {
            Success = true,
            Message = "Two-factor authentication setup initiated. Please verify the code to enable 2FA.",
            Secret = secret,
            QrCodeUri = qrCodeUri
        };
    }

    public async Task<AuthResponse> EnableTwoFactorAsync(EnableTwoFactorRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.UserNotFound
            };
        }

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Two-factor authentication setup not initiated. Please call SetupTwoFactorAsync first."
            };
        }

        if (!_twoFactorAuthService.VerifyCode(user.TwoFactorSecret, request.Code))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid verification code"
            };
        }

        user.TwoFactorEnabled = true;
        await _userManager.UpdateAsync(user);

        return new AuthResponse
        {
            Success = true,
            Message = "Two-factor authentication enabled successfully"
        };
    }

    public async Task<AuthResponse> VerifyTwoFactorAsync(TwoFactorVerifyRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.UserNotFound
            };
        }

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Two-factor authentication is not enabled for this account"
            };
        }

        if (!_twoFactorAuthService.VerifyCode(user.TwoFactorSecret, request.Code))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid verification code"
            };
        }

        if (!user.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.AccountInactive
            };
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var studentId = (Guid?)null;
        var landlordId = (Guid?)null;
        var universityVerificationStatus = (int?)null;

        if (roles.Contains("Student"))
        {
            var student = await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id);
            studentId = student?.StudentId;
            universityVerificationStatus = (int?)student?.UniversityVerificationStatus;
        }
        else if (roles.Contains("LandLord"))
        {
            landlordId = (await _landLordRepository.GetAll().FirstOrDefaultAsync(l => l.UserId == user.Id))?.LandLordId;
        }

        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            IsRevoked = false,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.Insert(refreshTokenEntity);
        await _refreshTokenRepository.CommitAsync();

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.LoginSuccess,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiryMinutes * 60
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToArray(),
                StudentId = studentId,
                LandLordId = landlordId,
                UniversityVerificationStatus = universityVerificationStatus
            }
        };
    }

    public async Task<AuthResponse> SendEmailConfirmationAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "UserId is required."
            };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.UserNotFound
            };
        }

        if (user.EmailConfirmed)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.EmailAlreadyConfirmed
            };
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);

        return new AuthResponse
        {
            Success = true,
            Message = $"{ErrorMessageHelper.EmailConfirmationSent} Use the token as a query parameter: /api/v1/Account/confirm-email?userId={Uri.EscapeDataString(userId)}&token={encodedToken}",
            Token = new TokenResponse
            {
                AccessToken = encodedToken,
                ExpiresIn = 86400
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponse> ConfirmEmailAsync(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "UserId and token are required."
            };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.UserNotFound
            };
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.EmailConfirmationFailed
            };
        }

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.EmailConfirmationSuccess
        };
    }

    public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email is required."
            };
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "User not found."
            };
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.ForgotPasswordGeneric,
            Token = new TokenResponse
            {
                AccessToken = encodedToken,
                ExpiresIn = 86400
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email, token, and new password are required."
            };
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.PasswordsDoNotMatch
            };
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.PasswordResetFailed
            };
        }

        var decodedToken = Uri.UnescapeDataString(request.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.PasswordResetFailed
            };
        }

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.PasswordResetSuccess
        };
    }
}
