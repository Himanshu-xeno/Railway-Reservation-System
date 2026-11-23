namespace ReservationService.DTOs;

public class BookingResponseDto
{
    public int ReservationId { get; set; }
    public string PNR { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int TrainId { get; set; }
    public DateTime JourneyDate { get; set; }
    public int NoOfPassengers { get; set; }
    public decimal TotalFare { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public List<PassengerDto> Passengers { get; set; } = new();
}
