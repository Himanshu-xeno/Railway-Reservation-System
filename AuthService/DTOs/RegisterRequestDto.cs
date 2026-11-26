using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
[MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
[RegularExpression(
    @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$",
    ErrorMessage = "Password must be at least 8 characters and contain at least one letter, one number, and one special character."
)]
public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    public string? Role { get; set; } = "User";
}
