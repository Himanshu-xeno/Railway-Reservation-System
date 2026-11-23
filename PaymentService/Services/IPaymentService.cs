using PaymentService.DTOs;

namespace PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentResponseDto> ProcessPaymentAsync(ProcessPaymentRequestDto request);
    Task<PaymentResponseDto> RefundPaymentAsync(int reservationId, RefundPaymentRequestDto request);
    Task<IEnumerable<PaymentResponseDto>> GetPaymentsByReservationIdAsync(int reservationId);
}
