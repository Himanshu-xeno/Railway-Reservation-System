using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs;

public class RefundRequestWithReservationDto
{
    [Required(ErrorMessage = "Reservation ID is required")]
    public int ReservationId { get; set; }

    [Required(ErrorMessage = "Refund amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than zero")]
    public decimal Amount { get; set; }
}
