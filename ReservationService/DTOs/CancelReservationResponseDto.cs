namespace ReservationService.DTOs;

public class CancelReservationResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string PNR { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
}
