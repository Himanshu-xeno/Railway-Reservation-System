using AuthService.DTOs;

namespace AuthService.Services;

//Any authService must provide me these 3 methods 

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserDto> GetCurrentUserAsync(int userId);
}
