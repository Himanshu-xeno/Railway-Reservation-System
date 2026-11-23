using AuthService.Data;
using AuthService.DTOs;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shared.Exceptions;
using Shared.Settings;

namespace Tests.Auth;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private IOptions<JwtSettings> _jwtOptions = null!;
    private AuthService.Services.AuthService _authService = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();

        var jwtSettings = new JwtSettings
        {
            SecretKey      = "RailwayReservationSystemSuperSecretKey20261123!@#$",
            Issuer         = "RailwayReservationSystem",
            Audience       = "RailwayReservationSystemClients",
            ExpiryMinutes  = 120
        };
        _jwtOptions = Options.Create(jwtSettings);

        _authService = new AuthService.Services.AuthService(_userRepositoryMock.Object, _jwtOptions);
    }

    // ─── Register Tests ───────────────────────────────────────────────────────

    [Test]
    public async Task Register_WithNewEmail_ReturnsSuccessResponse()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<AuthService.Models.User>()))
            .ReturnsAsync((AuthService.Models.User u) => { u.UserId = 101; return u; });

        var request = new RegisterRequestDto
        {
            Name     = "Test User",
            Email    = "test@example.com",
            Password = "Test@123",
            Phone    = "9876543210",
            Role     = "User"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Message, Is.EqualTo("User registered successfully"));
        Assert.That(result.UserId, Is.EqualTo(101));
    }

    [Test]
    public void Register_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new RegisterRequestDto
        {
            Name     = "Test User",
            Email    = "duplicate@example.com",
            Password = "Test@123",
            Phone    = "9876543210"
        };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(request));
    }

    [Test]
    public async Task Register_AdminRole_CreatesAdminUser()
    {
        // Arrange
        AuthService.Models.User? capturedUser = null;

        _userRepositoryMock
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<AuthService.Models.User>()))
            .Callback<AuthService.Models.User>(u => capturedUser = u)
            .ReturnsAsync((AuthService.Models.User u) => { u.UserId = 1; return u; });

        var request = new RegisterRequestDto
        {
            Name  = "Admin",
            Email = "admin2@railway.com",
            Password = "Admin@123",
            Phone = "9999999999",
            Role  = "Admin"
        };

        // Act
        await _authService.RegisterAsync(request);

        // Assert
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.Role, Is.EqualTo("Admin"));
    }

    // ─── Login Tests ──────────────────────────────────────────────────────────

    [Test]
    public async Task Login_WithValidCredentials_ReturnsTokenAndRole()
    {
        // Arrange
        var user = new AuthService.Models.User
        {
            UserId       = 1,
            Name         = "Test User",
            Email        = "test@example.com",
            PasswordHash = AuthDbSeeder.HashPassword("Test@123"),
            Phone        = "9876543210",
            Role         = "User"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var request = new LoginRequestDto
        {
            Email    = "test@example.com",
            Password = "Test@123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.Not.Empty);
        Assert.That(result.UserId, Is.EqualTo(1));
        Assert.That(result.Role, Is.EqualTo("User"));
    }

    [Test]
    public void Login_WithWrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new AuthService.Models.User
        {
            UserId       = 1,
            Email        = "test@example.com",
            PasswordHash = AuthDbSeeder.HashPassword("CorrectPassword"),
            Role         = "User"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var request = new LoginRequestDto
        {
            Email    = "test@example.com",
            Password = "WrongPassword"
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
    }

    [Test]
    public void Login_WithNonExistentEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AuthService.Models.User?)null);

        var request = new LoginRequestDto
        {
            Email    = "nobody@example.com",
            Password = "Test@123"
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
    }

    // ─── JWT Validation Tests ─────────────────────────────────────────────────

    [Test]
    public async Task Login_GeneratedToken_ContainsExpectedClaims()
    {
        // Arrange
        var user = new AuthService.Models.User
        {
            UserId       = 42,
            Name         = "Claim Tester",
            Email        = "claims@example.com",
            PasswordHash = AuthDbSeeder.HashPassword("Pass@123"),
            Phone        = "9000000000",
            Role         = "Admin"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var request = new LoginRequestDto { Email = user.Email, Password = "Pass@123" };

        // Act
        var result = await _authService.LoginAsync(request);

        // Parse token and verify claims
        var handler     = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var parsedToken = handler.ReadJwtToken(result.Token);

        // Assert
        var userIdClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var roleClaim   = parsedToken.Claims.FirstOrDefault(c => c.Type == "Role")?.Value;
        var emailClaim  = parsedToken.Claims.FirstOrDefault(c => c.Type == "Email")?.Value;

        Assert.That(userIdClaim, Is.EqualTo("42"));
        Assert.That(roleClaim,   Is.EqualTo("Admin"));
        Assert.That(emailClaim,  Is.EqualTo("claims@example.com"));
    }
}
