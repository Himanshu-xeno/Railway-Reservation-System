using PaymentService.Models;

namespace PaymentService.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int paymentId);
    Task<IEnumerable<Payment>> GetByReservationIdAsync(int reservationId);
    Task<bool> HasSuccessfulPaymentAsync(int reservationId);
    Task<bool> HasSuccessfulRefundAsync(int reservationId);
    Task<Payment> CreateAsync(Payment payment);
}
