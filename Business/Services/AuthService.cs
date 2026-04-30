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
<<<<<<< HEAD
    private readonly ITwoFactorAuthService _twoFactorAuthService;
=======
>>>>>>> d373b1145cc825f184dd583507a557a4aaf9a1f0

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        IStudentRepository studentRepository,
        ILandLordRepository landLordRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenBlacklistService tokenBlacklistService,
<<<<<<< HEAD
        IConfiguration configuration,
        ITwoFactorAuthService twoFactorAuthService)
=======
        IConfiguration configuration)
>>>>>>> d373b1145cc825f184dd583507a557a4aaf9a1f0
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _studentRepository = studentRepository;
        _landLordRepository = landLordRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenBlacklistService = tokenBlacklistService;
        _configuration = configuration;
<<<<<<< HEAD
        _twoFactorAuthService = twoFactorAuthService;
=======
>>>>>>> d373b1145cc825f184dd583507a557a4aaf9a1f0
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

        if (roles.Contains("Student"))
        {
            studentId = (await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id))?.StudentId;
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
                LandLordId = landlordId
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
                StudentId = student.StudentId
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
            PropertyOwnerShipProof = request.PropertyOwnerShipProof,
            VerificationStatus = "Pending"
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
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            
            if (string.IsNullOrEmpty(googleClientId) || googleClientId == "your-google-client-id.apps.googleusercontent.com")
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Google OAuth is not configured properly. Please set ClientId in appsettings.json"
                };
            }

            if (string.IsNullOrEmpty(request.IdToken))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Google ID token is required."
                };
            }

            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);

            // Use Google Subject (user ID) for identification - more reliable than email
            var googleId = payload.Subject;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (user == null)
            {
                // Check if user exists with same email but no GoogleId (merge accounts)
                user = await _userManager.FindByEmailAsync(payload.Email);
                
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
                        UserName = payload.Email,
                        Email = payload.Email,
                        GoogleId = googleId,
                        IsGoogleUser = true,
                        IsActive = true
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
                    // User will need to complete profile later
                    var studentEntity = new Domain.Entities.Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = user.Id,
                        DateOfBirth = DateTime.UtcNow.AddYears(-18), // Default to 18 years ago
                        Gender = Domain.Enums.Gender.Male, // Default gender
                        Address = null,
                        City = null,
                        PreferredArea = null,
                        NationalId = "00000000000000", // Placeholder, will be updated during onboarding
                        IsOnboardingComplete = false // Flag to track onboarding status
                    };

                    await _studentRepository.Insert(studentEntity);
                    await _studentRepository.CommitAsync();
                }
            }

<<<<<<< HEAD
            // Check if user has 2FA enabled
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

=======
>>>>>>> d373b1145cc825f184dd583507a557a4aaf9a1f0
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Optimize: Only query based on user's role
            var studentId = (Guid?)null;
            var landlordId = (Guid?)null;

            if (roles.Contains("Student"))
            {
                studentId = (await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id))?.StudentId;
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
                    LandLordId = landlordId
                }
            };
        }
        catch (InvalidJwtException ex)
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Invalid Google token: {ex.Message}"
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
<<<<<<< HEAD

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

        if (roles.Contains("Student"))
        {
            studentId = (await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id))?.StudentId;
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
                LandLordId = landlordId
            }
        };
    }
=======
>>>>>>> d373b1145cc825f184dd583507a557a4aaf9a1f0
}
