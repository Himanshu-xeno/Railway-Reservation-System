using System.ComponentModel.DataAnnotations;

namespace TrainService.DTOs;

public class CreateTrainRequestDto
{
    [Required(ErrorMessage = "Train name is required")]
    [MaxLength(100)]
    public string TrainName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Source station is required")]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;

    [Required(ErrorMessage = "Destination station is required")]
    [MaxLength(100)]
    public string Destination { get; set; } = string.Empty;

    [Required(ErrorMessage = "Journey date is required")]
    public DateTime JourneyDate { get; set; }

    [Required(ErrorMessage = "Departure time is required")]
    public TimeSpan DepartureTime { get; set; }

    [Required(ErrorMessage = "Arrival time is required")]
    public TimeSpan ArrivalTime { get; set; }

    [Required(ErrorMessage = "Total seats is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Total seats must be greater than 0")]
    public int TotalSeats { get; set; }

    [Required(ErrorMessage = "Fare is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Fare must be greater than 0")]
    public decimal Fare { get; set; }
}
