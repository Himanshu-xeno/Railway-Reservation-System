using ReservationService.DTOs;
using ReservationService.Models;
using ReservationService.Repositories;
using Shared.Constants;
using Shared.Exceptions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReservationService.Services;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly ITrainClient _trainClient;
    private readonly IPaymentClient _paymentClient;

    public ReservationService(
        IReservationRepository reservationRepository,
        IIdempotencyRepository idempotencyRepository,
        ITrainClient trainClient,
        IPaymentClient paymentClient)
    {
        _reservationRepository = reservationRepository;
        _idempotencyRepository = idempotencyRepository;
        _trainClient = trainClient;
        _paymentClient = paymentClient;
    }

    public async Task<FareEstimateResponseDto> EstimateFareAsync(FareEstimateRequestDto request)
    {
        var train = await _trainClient.GetTrainByIdAsync(request.TrainId);

        if (train.Status != TrainStatusConstants.Active)
        {
            throw new BadRequestException("This train is not currently active.");
        }

        var totalFare = train.Fare * request.PassengerCount;

        return new FareEstimateResponseDto
        {
            TrainId = train.TrainId,
            TrainName = train.TrainName,
            PassengerCount = request.PassengerCount,
            FarePerPassenger = train.Fare,
            TotalFare = totalFare
        };
    }

    public async Task<BookingResponseDto> BookTicketAsync(int userId, string idempotencyKey, BookTicketRequestDto request)
    {
        // 1. Validate train exists and is active
        var train = await _trainClient.GetTrainByIdAsync(request.TrainId);
        if (train.Status != TrainStatusConstants.Active)
        {
            throw new BadRequestException("Train is not active for new bookings.");
        }

        if (train.JourneyDate.Date < DateTime.UtcNow.Date)
        {
            throw new BadRequestException("Cannot book a ticket for a past journey date.");
        }

        // 2. Compute idempotency hash from entire request body
        var requestPayload = JsonSerializer.Serialize(new { userId, request.TrainId, request.Passengers });
        var payloadHash = ComputeHash(requestPayload);

        // 3. Idempotency check
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingRecord = await _idempotencyRepository.GetAsync(userId, idempotencyKey);

            if (existingRecord != null)
            {
                // Same key + different payload → 409 Conflict
                if (existingRecord.PayloadHash != payloadHash)
                {
                    throw new ConflictException("Idempotency key used with a different request payload.");
                }

                // Same key + still processing → 409 Conflict
                if (existingRecord.Status == "Processing")
                {
                    throw new ConflictException("A request with this idempotency key is already in progress.");
                }

                // Same key + same payload + completed → return cached response
                if (existingRecord.Status == "Completed" && existingRecord.ReservationId.HasValue)
                {
                    var cached = await _reservationRepository.GetByIdAsync(existingRecord.ReservationId.Value);
                    if (cached != null)
                    {
                        return MapToResponseDto(cached);
                    }
                }
            }

            // Create a "Processing" idempotency record
            await _idempotencyRepository.CreateAsync(new IdempotencyRecord
            {
                UserId = userId,
                IdempotencyKey = idempotencyKey,
                PayloadHash = payloadHash,
                Status = "Processing",
                CreatedAt = DateTime.UtcNow
            });
        }

        // 4. Reserve seats in TrainService (overbooking prevention)
        int seatCount = request.Passengers.Count;
        await _trainClient.ReserveSeatsAsync(request.TrainId, seatCount);

        // 5. Calculate total fare
        var totalFare = train.Fare * seatCount;

        // 6. Generate unique PNR
        var pnr = await GenerateUniquePnrAsync();

        // 7. Create reservation in Pending state
        var reservation = new Reservation
        {
            PNR = pnr,
            UserId = userId,
            TrainId = request.TrainId,
            JourneyDate = train.JourneyDate.Date,
            NoOfPassengers = seatCount,
            TotalFare = totalFare,
            BookingStatus = BookingStatusConstants.Pending,
            BookingDate = DateTime.UtcNow,
            Passengers = request.Passengers.Select(p => new Passenger
            {
                PassengerName = p.PassengerName,
                Age = p.Age,
                Gender = p.Gender
            }).ToList()
        };

        var created = await _reservationRepository.CreateAsync(reservation);

        // 8. Process payment
        var paymentResult = await _paymentClient.ProcessPaymentAsync(created.ReservationId, totalFare);

        if (!paymentResult.Success)
        {
            // Payment failed: release seats and mark reservation failed
            await _trainClient.ReleaseSeatsAsync(request.TrainId, seatCount);
            created.BookingStatus = BookingStatusConstants.Failed;
            await _reservationRepository.UpdateAsync(created);

            // Clean up idempotency record if one was created
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var rec = await _idempotencyRepository.GetAsync(userId, idempotencyKey);
                if (rec != null) await _idempotencyRepository.DeleteAsync(rec.Id);
            }

            throw new ConflictException("Payment processing failed. Reservation has been cancelled.");
        }

        // 9. Mark reservation as Confirmed
        created.BookingStatus = BookingStatusConstants.Confirmed;
        await _reservationRepository.UpdateAsync(created);

        // 10. Update idempotency record to Completed
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var rec = await _idempotencyRepository.GetAsync(userId, idempotencyKey);
            if (rec != null)
            {
                rec.Status = "Completed";
                rec.ReservationId = created.ReservationId;
                await _idempotencyRepository.UpdateAsync(rec);
            }
        }

        return MapToResponseDto(created);
    }

    public async Task<BookingResponseDto> GetBookingByIdAsync(int reservationId, int currentUserId, string currentUserRole)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
        {
            throw new NotFoundException($"Reservation with ID {reservationId} not found.");
        }

        // Non-admin users can only see their own bookings
        if (currentUserRole != Roles.Admin && reservation.UserId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to view this reservation.");
        }

        return MapToResponseDto(reservation);
    }

    public async Task<BookingResponseDto> GetBookingByPnrAsync(string pnr, int currentUserId, string currentUserRole)
    {
        var reservation = await _reservationRepository.GetByPnrAsync(pnr);
        if (reservation == null)
        {
            throw new NotFoundException($"Reservation with PNR '{pnr}' not found.");
        }

        if (currentUserRole != Roles.Admin && reservation.UserId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to view this reservation.");
        }

        return MapToResponseDto(reservation);
    }

    public async Task<IEnumerable<BookingResponseDto>> GetBookingHistoryAsync(int userId)
    {
        var reservations = await _reservationRepository.GetByUserIdAsync(userId);
        return reservations.Select(MapToResponseDto);
    }

    public async Task<IEnumerable<BookingResponseDto>> GetAllBookingsAsync()
    {
        var reservations = await _reservationRepository.GetAllAsync();
        return reservations.Select(MapToResponseDto);
    }

    public async Task<CancelReservationResponseDto> CancelBookingAsync(int reservationId, int currentUserId)
    {
        // 1. Find reservation
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
        {
            throw new NotFoundException($"Reservation with ID {reservationId} not found.");
        }

        // 2. Only the booking owner can cancel
        if (reservation.UserId != currentUserId)
        {
            throw new ForbiddenException("Only the booking owner can cancel this reservation.");
        }

        // 3. Only confirmed bookings can be cancelled
        if (reservation.BookingStatus != BookingStatusConstants.Confirmed)
        {
            throw new ConflictException($"Only confirmed bookings can be cancelled. Current status: {reservation.BookingStatus}.");
        }

        // 4. Journey date must be in the future
        if (reservation.JourneyDate.Date <= DateTime.UtcNow.Date)
        {
            throw new ConflictException("Cannot cancel a booking for a past or today's journey date.");
        }

        // 5. Calculate refund: 90% of total fare (10% cancellation charge)
        var cancellationCharge = reservation.TotalFare * 0.10m;
        var refundAmount = reservation.TotalFare - cancellationCharge;

        // 6. Process refund via PaymentService
        var refundResult = await _paymentClient.RefundPaymentAsync(reservationId, refundAmount);
        if (!refundResult.Success)
        {
            throw new ConflictException("Refund processing failed. Please try again.");
        }

        // 7. Release seats back to TrainService
        await _trainClient.ReleaseSeatsAsync(reservation.TrainId, reservation.NoOfPassengers);

        // 8. Create cancellation record
        await _reservationRepository.AddCancellationAsync(new Cancellation
        {
            ReservationId = reservationId,
            CancellationDate = DateTime.UtcNow,
            RefundAmount = refundAmount
        });

        // 9. Update reservation status to Cancelled
        reservation.BookingStatus = BookingStatusConstants.Cancelled;
        await _reservationRepository.UpdateAsync(reservation);

        return new CancelReservationResponseDto
        {
            Message = "Booking cancelled successfully. Refund is being processed.",
            PNR = reservation.PNR,
            Status = BookingStatusConstants.Cancelled,
            RefundAmount = refundAmount
        };
    }

    private async Task<string> GenerateUniquePnrAsync()
    {
        string pnr;
        bool exists;
        int sequence = 1;

        // Keep incrementing until we get a unique PNR
        do
        {
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
            pnr = $"PNR{yearMonth}{sequence:D4}";
            exists = await _reservationRepository.ExistsPnrAsync(pnr);
            sequence++;
        }
        while (exists);

        return pnr;
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower()[..64];
    }

    private static BookingResponseDto MapToResponseDto(Reservation reservation)
    {
        return new BookingResponseDto
        {
            ReservationId = reservation.ReservationId,
            PNR = reservation.PNR,
            UserId = reservation.UserId,
            TrainId = reservation.TrainId,
            JourneyDate = reservation.JourneyDate,
            NoOfPassengers = reservation.NoOfPassengers,
            TotalFare = reservation.TotalFare,
            BookingStatus = reservation.BookingStatus,
            BookingDate = reservation.BookingDate,
            Passengers = reservation.Passengers.Select(p => new PassengerDto
            {
                PassengerName = p.PassengerName,
                Age = p.Age,
                Gender = p.Gender
            }).ToList()
        };
    }
}
