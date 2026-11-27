namespace ReservationService.Services;

public class PaymentResultDto
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public interface IPaymentClient
{
    Task<PaymentResultDto> ProcessPaymentAsync(int reservationId, decimal amount);
    Task<PaymentResultDto> RefundPaymentAsync(int reservationId, decimal amount);
}
