using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs;

public class RefundPaymentRequestDto
{
    [Required(ErrorMessage = "Refund amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than zero")]
    public decimal Amount { get; set; }
}
