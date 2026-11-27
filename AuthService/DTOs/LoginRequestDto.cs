using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs; //stores DTO (client<-->API)

//use for Login request
//Represents the JSON coming from frontend.
//ASP.Net convert JSON into LoginRequestDto obj (Model Binding)
public class LoginRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}
