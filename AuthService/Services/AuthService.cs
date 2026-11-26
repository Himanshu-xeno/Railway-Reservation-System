using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Constants;
using Shared.Exceptions;
using Shared.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IOptions<JwtSettings> jwtOptions)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<RegisterResponseDto> RegisterAsync(
        RegisterRequestDto request)
    {
        // 1. Check if email already exists
        var emailExists =
            await _userRepository.ExistsByEmailAsync(request.Email);

        if (emailExists)
        {
            throw new ConflictException(
                "Email is already registered.");
        }

        // 2. Password validation
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException(
                "Password is required.");
        }

        if (request.Password.Length < 8)
        {
            throw new BadRequestException(
                "Password must be at least 8 characters.");
        }

        if (!Regex.IsMatch(
                request.Password,
                @"^(?=.*[A-Za-z])(?=.*\d).{8,}$"))
        {
            throw new BadRequestException(
                "Password must be at least 8 characters and contain at least one letter and one number.");
        }

        // 3. Hash password
        var passwordHash =
            AuthDbSeeder.HashPassword(request.Password);

        // 4. Determine role (default to User)
        var role = string.Equals(
            request.Role,
            Roles.Admin,
            StringComparison.OrdinalIgnoreCase)
            ? Roles.Admin
            : Roles.User;

        // 5. Create user
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = passwordHash,
            Phone = request.Phone.Trim(),
            Role = role
        };

        var createdUser =
            await _userRepository.CreateAsync(user);

        return new RegisterResponseDto
        {
            Message = "User registered successfully",
            UserId = createdUser.UserId
        };
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request)
    {
        // 1. Fetch user by email
        var user =
            await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            throw new UnauthorizedException(
                "Invalid email or password.");
        }

        // 2. Verify password
        var isPasswordValid =
            AuthDbSeeder.VerifyPassword(
                request.Password,
                user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new UnauthorizedException(
                "Invalid email or password.");
        }

        // 3. Generate JWT
        var token = GenerateJwtToken(user);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.UserId,
            Role = user.Role
        };
    }

    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException(
                "User not found.");
        }

        return new UserDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role
        };
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.UTF8.GetBytes(
            _jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new Claim("UserId", user.UserId.ToString()),
            new Claim(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),
            new Claim(
                ClaimTypes.Name,
                user.Name),
            new Claim(
                ClaimTypes.Email,
                user.Email),
            new Claim(
                "Email",
                user.Email),
            new Claim(
                ClaimTypes.Role,
                user.Role),
            new Claim(
                "Role",
                user.Role)
        };

        var tokenDescriptor =
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(
                    _jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials =
                    new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
            };

        var token =
            tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}