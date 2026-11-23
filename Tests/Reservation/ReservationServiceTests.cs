using Moq;
using NUnit.Framework;
using ReservationService.DTOs;
using ReservationService.Models;
using ReservationService.Repositories;
using ReservationService.Services;
using Shared.Exceptions;
using ResModel  = ReservationService.Models.Reservation;
using CancelModel = ReservationService.Models.Cancellation;
using IdempModel  = ReservationService.Models.IdempotencyRecord;
using PassModel   = ReservationService.Models.Passenger;

namespace Tests.ReservationTests;

[TestFixture]
public class ReservationServiceTests
{
    private Mock<IReservationRepository> _reservationRepoMock  = null!;
    private Mock<IIdempotencyRepository> _idempotencyRepoMock  = null!;
    private Mock<ITrainClient>           _trainClientMock      = null!;
    private Mock<IPaymentClient>         _paymentClientMock    = null!;
    private ReservationService.Services.ReservationService _svc = null!;

    [SetUp]
    public void SetUp()
    {
        _reservationRepoMock = new Mock<IReservationRepository>();
        _idempotencyRepoMock = new Mock<IIdempotencyRepository>();
        _trainClientMock     = new Mock<ITrainClient>();
        _paymentClientMock   = new Mock<IPaymentClient>();

        _svc = new ReservationService.Services.ReservationService(
            _reservationRepoMock.Object,
            _idempotencyRepoMock.Object,
            _trainClientMock.Object,
            _paymentClientMock.Object);
    }

    // ─── Booking Tests ────────────────────────────────────────────────────────

    [Test]
    public async Task BookTicket_WithValidRequest_ReturnsConfirmedBooking()
    {
        // Arrange
        var trainInfo = new TrainInfoDto
        {
            TrainId = 1, TrainName = "Express",
            Status  = "Active",
            JourneyDate    = DateTime.UtcNow.AddDays(5),
            Fare           = 1000m,
            AvailableSeats = 50
        };

        _trainClientMock.Setup(c => c.GetTrainByIdAsync(1)).ReturnsAsync(trainInfo);
        _trainClientMock.Setup(c => c.ReserveSeatsAsync(1, 2)).ReturnsAsync(true);

        _idempotencyRepoMock
            .Setup(r => r.GetAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((IdempModel?)null);
        _idempotencyRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<IdempModel>()))
            .ReturnsAsync(new IdempModel { Id = 1 });

        _reservationRepoMock
            .Setup(r => r.ExistsPnrAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ResModel>()))
            .ReturnsAsync((ResModel res) => { res.ReservationId = 10; return res; });
        _reservationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ResModel>()))
            .Returns(Task.CompletedTask);

        _paymentClientMock
            .Setup(c => c.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()))
            .ReturnsAsync(new PaymentResultDto { Success = true });

        _idempotencyRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<IdempModel>()))
            .Returns(Task.CompletedTask);

        var request = new BookTicketRequestDto
        {
            TrainId    = 1,
            Passengers = new List<PassengerDto>
            {
                new() { PassengerName = "Alice", Age = 30, Gender = "Female" },
                new() { PassengerName = "Bob",   Age = 35, Gender = "Male"   }
            }
        };

        // Act
        var result = await _svc.BookTicketAsync(1, "key-001", request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.BookingStatus,  Is.EqualTo("Confirmed"));
        Assert.That(result.NoOfPassengers, Is.EqualTo(2));
        Assert.That(result.TotalFare,      Is.EqualTo(2000m));
        Assert.That(result.PNR,            Does.StartWith("PNR"));
    }

    [Test]
    public void BookTicket_WhenPaymentFails_ThrowsConflictAndReleasesSeats()
    {
        // Arrange
        var trainInfo = new TrainInfoDto
        {
            TrainId = 1, Status = "Active",
            JourneyDate = DateTime.UtcNow.AddDays(3),
            Fare = 500m, AvailableSeats = 20, TrainName = "PayFail Train"
        };

        _trainClientMock.Setup(c => c.GetTrainByIdAsync(1)).ReturnsAsync(trainInfo);
        _trainClientMock.Setup(c => c.ReserveSeatsAsync(1, 1)).ReturnsAsync(true);
        _trainClientMock.Setup(c => c.ReleaseSeatsAsync(1, 1)).ReturnsAsync(true);

        _idempotencyRepoMock.Setup(r => r.GetAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync((IdempModel?)null);
        _idempotencyRepoMock.Setup(r => r.CreateAsync(It.IsAny<IdempModel>())).ReturnsAsync(new IdempModel { Id = 1 });
        _idempotencyRepoMock.Setup(r => r.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        _reservationRepoMock.Setup(r => r.ExistsPnrAsync(It.IsAny<string>())).ReturnsAsync(false);
        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ResModel>()))
            .ReturnsAsync((ResModel res) => { res.ReservationId = 20; return res; });
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResModel>())).Returns(Task.CompletedTask);

        _paymentClientMock
            .Setup(c => c.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()))
            .ReturnsAsync(new PaymentResultDto { Success = false });

        var request = new BookTicketRequestDto
        {
            TrainId    = 1,
            Passengers = new List<PassengerDto>
            {
                new() { PassengerName = "Charlie", Age = 25, Gender = "Male" }
            }
        };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(() => _svc.BookTicketAsync(1, "key-002", request));
    }

    // ─── Cancellation Tests ───────────────────────────────────────────────────

    [Test]
    public async Task CancelBooking_ValidOwnerAndConfirmed_ReturnsRefund()
    {
        // Arrange
        var reservation = new ResModel
        {
            ReservationId = 5, UserId = 1, TrainId = 2,
            BookingStatus = "Confirmed",
            JourneyDate   = DateTime.UtcNow.AddDays(10).Date,
            TotalFare     = 1000m, NoOfPassengers = 2,
            PNR           = "PNR20261200001",
            Passengers    = new List<PassModel>()
        };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(reservation);
        _paymentClientMock
            .Setup(c => c.RefundPaymentAsync(5, 900m))
            .ReturnsAsync(new PaymentResultDto { Success = true });
        _trainClientMock.Setup(c => c.ReleaseSeatsAsync(2, 2)).ReturnsAsync(true);
        _reservationRepoMock.Setup(r => r.AddCancellationAsync(It.IsAny<CancelModel>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ResModel>())).Returns(Task.CompletedTask);

        // Act
        var result = await _svc.CancelBookingAsync(5, 1);

        // Assert
        Assert.That(result.Status,       Is.EqualTo("Cancelled"));
        Assert.That(result.RefundAmount, Is.EqualTo(900m));
    }

    [Test]
    public void CancelBooking_ByNonOwner_ThrowsForbiddenException()
    {
        // Arrange
        var reservation = new ResModel
        {
            ReservationId = 6, UserId = 10,
            BookingStatus = "Confirmed",
            JourneyDate   = DateTime.UtcNow.AddDays(5).Date,
            TotalFare     = 800m, Passengers = new List<PassModel>()
        };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(reservation);

        // Act & Assert
        Assert.ThrowsAsync<ForbiddenException>(() => _svc.CancelBookingAsync(6, 99));
    }

    [Test]
    public void CancelBooking_AlreadyCancelled_ThrowsConflictException()
    {
        // Arrange
        var reservation = new ResModel
        {
            ReservationId = 7, UserId = 1,
            BookingStatus = "Cancelled",
            JourneyDate   = DateTime.UtcNow.AddDays(5).Date,
            TotalFare     = 600m, Passengers = new List<PassModel>()
        };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(reservation);

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(() => _svc.CancelBookingAsync(7, 1));
    }

    [Test]
    public void CancelBooking_PastJourneyDate_ThrowsConflictException()
    {
        // Arrange
        var reservation = new ResModel
        {
            ReservationId = 8, UserId = 1,
            BookingStatus = "Confirmed",
            JourneyDate   = DateTime.UtcNow.AddDays(-1).Date,
            TotalFare     = 500m, Passengers = new List<PassModel>()
        };

        _reservationRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(reservation);

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(() => _svc.CancelBookingAsync(8, 1));
    }

    // ─── PNR Search Tests ─────────────────────────────────────────────────────

    [Test]
    public async Task GetBookingByPnr_ValidPnrAndOwner_ReturnsBooking()
    {
        // Arrange
        var reservation = new ResModel
        {
            ReservationId = 9, PNR = "PNR202612001",
            UserId        = 1, TrainId = 3,
            BookingStatus = "Confirmed",
            JourneyDate   = DateTime.UtcNow.AddDays(7).Date,
            TotalFare     = 750m, NoOfPassengers = 1,
            BookingDate   = DateTime.UtcNow,
            Passengers    = new List<PassModel>
            {
                new() { PassengerName = "Dave", Age = 40, Gender = "Male" }
            }
        };

        _reservationRepoMock.Setup(r => r.GetByPnrAsync("PNR202612001")).ReturnsAsync(reservation);

        // Act
        var result = await _svc.GetBookingByPnrAsync("PNR202612001", 1, "User");

        // Assert
        Assert.That(result.PNR,           Is.EqualTo("PNR202612001"));
        Assert.That(result.BookingStatus, Is.EqualTo("Confirmed"));
    }

    [Test]
    public void GetBookingByPnr_NonExistentPnr_ThrowsNotFoundException()
    {
        // Arrange
        _reservationRepoMock.Setup(r => r.GetByPnrAsync(It.IsAny<string>())).ReturnsAsync((ResModel?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => _svc.GetBookingByPnrAsync("PNRXXX", 1, "User"));
    }

    // ─── Idempotency Tests ────────────────────────────────────────────────────

    [Test]
    public async Task BookTicket_SameKeyAndSamePayload_ReturnsCachedResponse()
    {
        // Arrange
        var request = new BookTicketRequestDto
        {
            TrainId    = 1,
            Passengers = new List<PassengerDto>
            {
                new() { PassengerName = "Alice", Age = 28, Gender = "Female" }
            }
        };

        // Compute same hash the service uses
        var payload = System.Text.Json.JsonSerializer.Serialize(new { userId = 1, request.TrainId, request.Passengers });
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var hash = Convert.ToHexString(hashBytes).ToLower()[..64];

        var existingRecord = new IdempModel
        {
            Id = 1, UserId = 1, IdempotencyKey = "dup-key",
            PayloadHash = hash, Status = "Completed", ReservationId = 50
        };

        var existingReservation = new ResModel
        {
            ReservationId = 50, PNR = "PNR202612050",
            UserId = 1, TrainId = 1,
            BookingStatus = "Confirmed",
            JourneyDate   = DateTime.UtcNow.AddDays(7),
            TotalFare     = 1000m, NoOfPassengers = 1,
            BookingDate   = DateTime.UtcNow,
            Passengers    = new List<PassModel>
            {
                new() { PassengerName = "Alice", Age = 28, Gender = "Female" }
            }
        };

        _trainClientMock.Setup(c => c.GetTrainByIdAsync(1)).ReturnsAsync(new TrainInfoDto
        {
            TrainId = 1, Status = "Active",
            JourneyDate = DateTime.UtcNow.AddDays(7),
            Fare = 1000m, AvailableSeats = 50, TrainName = "T1"
        });

        _idempotencyRepoMock.Setup(r => r.GetAsync(1, "dup-key")).ReturnsAsync(existingRecord);
        _reservationRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(existingReservation);

        // Act
        var result = await _svc.BookTicketAsync(1, "dup-key", request);

        // Assert – cached response returned, no new reservation created
        Assert.That(result.ReservationId, Is.EqualTo(50));
        _reservationRepoMock.Verify(r => r.CreateAsync(It.IsAny<ResModel>()), Times.Never);
    }

    [Test]
    public void BookTicket_SameKeyDifferentPayload_ThrowsConflictException()
    {
        // Arrange
        var existingRecord = new IdempModel
        {
            Id = 2, UserId = 1, IdempotencyKey = "conflict-key",
            // A hash that will NOT match the computed hash for TrainId=2 request
            PayloadHash = "totallydifferenthash12345678901234567890123456789012345678901234",
            Status = "Completed"
        };

        _idempotencyRepoMock.Setup(r => r.GetAsync(1, "conflict-key")).ReturnsAsync(existingRecord);

        _trainClientMock.Setup(c => c.GetTrainByIdAsync(2)).ReturnsAsync(new TrainInfoDto
        {
            TrainId = 2, Status = "Active",
            JourneyDate = DateTime.UtcNow.AddDays(5),
            Fare = 800m, AvailableSeats = 30, TrainName = "T2"
        });

        var request = new BookTicketRequestDto
        {
            TrainId    = 2,
            Passengers = new List<PassengerDto>
            {
                new() { PassengerName = "Bob", Age = 22, Gender = "Male" }
            }
        };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(() => _svc.BookTicketAsync(1, "conflict-key", request));
    }
}
