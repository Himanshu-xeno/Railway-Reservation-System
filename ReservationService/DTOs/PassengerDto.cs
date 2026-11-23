using System.ComponentModel.DataAnnotations;

namespace ReservationService.DTOs;

public class PassengerDto
{
    [Required(ErrorMessage = "Passenger name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Passenger name must be between 2 and 100 characters")]
    public string PassengerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Age is required")]
    [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
    public string Gender { get; set; } = string.Empty;
}
