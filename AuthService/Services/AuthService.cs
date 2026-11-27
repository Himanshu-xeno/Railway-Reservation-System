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
    //stores repository object
    private readonly IUserRepository _userRepository;
    //stores jwt config(settings)
    private readonly JwtSettings _jwtSettings;



    //DI provides us userRepo & JwtSettings automatic
    public AuthService(
        IUserRepository userRepository,
        IOptions<JwtSettings> jwtOptions)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtOptions.Value;
    }


    //1.Create new user(registration)
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

        // 3. Hash password - storing the HashPassword
        var passwordHash = AuthDbSeeder.HashPassword(request.Password);

        // 4. Determine role (default to User)
        var role = string.Equals(
            request.Role,
            Roles.Admin,
            StringComparison.OrdinalIgnoreCase)
            ? Roles.Admin
            : Roles.User;

        // 5. Create user object(user entity)
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = passwordHash,
            Phone = request.Phone.Trim(),
            Role = role
        };

        //save the user (repository inserts record)
        var createdUser = await _userRepository.CreateAsync(user);

        //we return response 
        return new RegisterResponseDto
        {
            Message = "User registered successfully",
            UserId = createdUser.UserId
        };
    }

    
    //2.Login user
    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request)
    {
        // 1. Fetch user by email
        var user = await _userRepository.GetByEmailAsync(request.Email);

        //if user not found
        if (user == null)
        {
            throw new UnauthorizedException(
                "Invalid email or password.");
        }

        // 2. Verify password
        var isPasswordValid = AuthDbSeeder.VerifyPassword(request.Password, user.PasswordHash);

        //if wrong password
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        // 3. Generating JWT
        var token = GenerateJwtToken(user);

        //return Login response
        return new LoginResponseDto
        {
            Token = token,
            UserId = user.UserId,
            Role = user.Role
        };
    }

    
    //3.Get current user 
    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        //find user by id
        var user = await _userRepository.GetByIdAsync(userId);

        //if not found
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        //return user details by creating user-dto
        return new UserDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role
        };
    }

    
    //Generating JWT token
    private string GenerateJwtToken(User user)
    {
        //create token handler(jwt utility object)
        var tokenHandler = new JwtSecurityTokenHandler();

        //converting secret key to bytes 
        var key = Encoding.UTF8.GetBytes( _jwtSettings.SecretKey);

        //claims : Information stored inside token
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

        //creating token descriptor contain : claims, expiry, issuer, audience, secret key
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

        //creation of token
        var token = tokenHandler.CreateToken(tokenDescriptor);

        //convert to string for returning
        return tokenHandler.WriteToken(token);
    }
}