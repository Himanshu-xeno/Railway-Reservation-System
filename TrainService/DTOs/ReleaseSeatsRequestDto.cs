using System.ComponentModel.DataAnnotations;

namespace TrainService.DTOs;

public class ReleaseSeatsRequestDto
{
    [Required]
    public int TrainId { get; set; }

    [Required]
    [Range(1, 6, ErrorMessage = "Seat count must be between 1 and 6")]
    public int SeatCount { get; set; }
}

public class ReleaseSeatsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TrainId { get; set; }
    public int ReleasedSeats { get; set; }
    public int RemainingSeats { get; set; }
}
