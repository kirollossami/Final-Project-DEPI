using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Helpers;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IStudentRepository _studentRepository;
    private readonly ILandLordRepository _landLordRepository;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        IStudentRepository studentRepository,
        ILandLordRepository landLordRepository)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _studentRepository = studentRepository;
        _landLordRepository = landLordRepository;
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

        var student = await _studentRepository.GetAll().FirstOrDefaultAsync(s => s.UserId == user.Id);
        var landlord = await _landLordRepository.GetAll().FirstOrDefaultAsync(l => l.UserId == user.Id);

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.LoginSuccess,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToArray(),
                StudentId = student?.StudentId,
                LandLordId = landlord?.LandLordId
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
            NationalId = request.NationalId
        };

        await _studentRepository.Insert(student);
        await _studentRepository.CommitAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.RegistrationSuccess,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
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

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.RegistrationSuccess,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
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

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = ErrorMessageHelper.UserNotFound
            };
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthResponse
        {
            Success = true,
            Message = ErrorMessageHelper.TokenRefreshed,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            }
        };
    }

    public async Task<bool> LogoutAsync(string token)
    {
        // In a real implementation, you would add the token to a blacklist
        // For now, we'll just return true
        await Task.CompletedTask;
        return true;
    }
}
