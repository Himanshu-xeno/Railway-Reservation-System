namespace TrainService.DTOs;

public class TrainResponseDto
{
    public int TrainId { get; set; }
    public string TrainName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal Fare { get; set; }
    public string Status { get; set; } = string.Empty;
}
