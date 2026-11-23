using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(int paymentId)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
    }

    public async Task<IEnumerable<Payment>> GetByReservationIdAsync(int reservationId)
    {
        return await _context.Payments
            .Where(p => p.ReservationId == reservationId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<bool> HasSuccessfulPaymentAsync(int reservationId)
    {
        return await _context.Payments.AnyAsync(p =>
            p.ReservationId == reservationId &&
            p.PaymentType == "Payment" &&
            p.PaymentStatus == "Success");
    }

    public async Task<bool> HasSuccessfulRefundAsync(int reservationId)
    {
        return await _context.Payments.AnyAsync(p =>
            p.ReservationId == reservationId &&
            p.PaymentType == "Refund" &&
            p.PaymentStatus == "Success");
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}
