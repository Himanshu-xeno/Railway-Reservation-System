using System.ComponentModel.DataAnnotations;

namespace ReservationService.DTOs;

public class FareEstimateRequestDto
{
    [Required(ErrorMessage = "Train ID is required")]
    public int TrainId { get; set; }

    [Required(ErrorMessage = "Passenger count is required")]
    [Range(1, 6, ErrorMessage = "Passenger count must be between 1 and 6")]
    public int PassengerCount { get; set; }
}

public class FareEstimateResponseDto
{
    public int TrainId { get; set; }
    public string TrainName { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public decimal FarePerPassenger { get; set; }
    public decimal TotalFare { get; set; }
}
