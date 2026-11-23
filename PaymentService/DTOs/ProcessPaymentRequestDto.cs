using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs;

public class ProcessPaymentRequestDto
{
    [Required(ErrorMessage = "Reservation ID is required")]
    public int ReservationId { get; set; }

    [Required(ErrorMessage = "Payment amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }
}
