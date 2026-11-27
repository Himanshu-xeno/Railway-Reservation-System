using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Repositories;
using Shared.Constants;
using Shared.Exceptions;
using System.Security.Cryptography;

namespace PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(ProcessPaymentRequestDto request)
    {
        // 0. Reservation ID validation
        if (request.ReservationId <= 0)
        {
            throw new BadRequestException("Reservation ID must be a valid positive integer.");
        }

        // 1. Amount validation
        if (request.Amount <= 0)
        {
            throw new BadRequestException("Payment amount must be greater than zero.");
        }

        // 2. Duplicate payment check (TC-32)
        var hasPaid = await _paymentRepository.HasSuccessfulPaymentAsync(request.ReservationId);
        if (hasPaid)
        {
            throw new ConflictException("Duplicate payment detected for this reservation.");
        }

        // 3. Generate unique transaction ID
        var transactionId = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{RandomNumberGenerator.GetInt32(1000, 9999)}";

        // 4. Create and persist simulated payment record
        var payment = new Payment
        {
            ReservationId = request.ReservationId,
            Amount = request.Amount,
            PaymentType = PaymentTypeConstants.Payment,
            PaymentDate = DateTime.UtcNow,
            PaymentStatus = PaymentStatusConstants.Success,
            TransactionId = transactionId
        };

        var created = await _paymentRepository.CreateAsync(payment);

        return MapToDto(created);
    }

    public async Task<PaymentResponseDto> RefundPaymentAsync(int reservationId, RefundPaymentRequestDto request)
    {
        if (reservationId <= 0)
        {
            throw new BadRequestException("Reservation ID must be a valid positive integer.");
        }

        if (request.Amount <= 0)
        {
            throw new BadRequestException("Refund amount must be greater than zero.");
        }

        // 1. Verify original payment exists
        var payments = await _paymentRepository.GetByReservationIdAsync(reservationId);
        var originalPayment = payments.FirstOrDefault(p => p.PaymentType == PaymentTypeConstants.Payment && p.PaymentStatus == PaymentStatusConstants.Success);
        if (originalPayment == null)
        {
            throw new BadRequestException("No successful payment found for this reservation to refund.");
        }

        // Validate refund amount against original payment
        if (request.Amount > originalPayment.Amount)
        {
            throw new BadRequestException($"Refund amount ({request.Amount}) cannot exceed original payment amount ({originalPayment.Amount}).");
        }

        // 2. Prevent duplicate refund
        var hasRefunded = await _paymentRepository.HasSuccessfulRefundAsync(reservationId);
        if (hasRefunded)
        {
            throw new ConflictException("Refund has already been processed for this reservation.");
        }

        // 3. Generate unique refund transaction ID
        var transactionId = $"REF{DateTime.UtcNow:yyyyMMddHHmmss}{RandomNumberGenerator.GetInt32(1000, 9999)}";

        var refund = new Payment
        {
            ReservationId = reservationId,
            Amount = request.Amount,
            PaymentType = PaymentTypeConstants.Refund,
            PaymentDate = DateTime.UtcNow,
            PaymentStatus = PaymentStatusConstants.Success,
            TransactionId = transactionId
        };

        var created = await _paymentRepository.CreateAsync(refund);

        return MapToDto(created);
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetPaymentsByReservationIdAsync(int reservationId)
    {
        var payments = await _paymentRepository.GetByReservationIdAsync(reservationId);
        return payments.Select(MapToDto);
    }

    private static PaymentResponseDto MapToDto(Payment payment)
    {
        return new PaymentResponseDto
        {
            PaymentId = payment.PaymentId,
            ReservationId = payment.ReservationId,
            Amount = payment.Amount,
            PaymentType = payment.PaymentType,
            PaymentDate = payment.PaymentDate,
            PaymentStatus = payment.PaymentStatus,
            TransactionId = payment.TransactionId
        };
    }
}
