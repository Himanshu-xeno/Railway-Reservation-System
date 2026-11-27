//send user information to the client without exposing sensitive data.
namespace AuthService.DTOs;

//We send this data to client
//No use of entity as we don't send passwords
public class UserDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
