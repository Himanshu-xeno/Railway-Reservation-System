namespace ReservationService.Services;

public class TrainInfoDto
{
    public int TrainId { get; set; }
    public string TrainName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public int AvailableSeats { get; set; }
    public decimal Fare { get; set; }
    public string Status { get; set; } = string.Empty;
}

public interface ITrainClient
{
    Task<TrainInfoDto> GetTrainByIdAsync(int trainId);
    Task<bool> ReserveSeatsAsync(int trainId, int seatCount);
    Task<bool> ReleaseSeatsAsync(int trainId, int seatCount);
}
