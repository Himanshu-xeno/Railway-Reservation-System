namespace AuthService.DTOs;

//This object is returned after login succeeds.
public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}
