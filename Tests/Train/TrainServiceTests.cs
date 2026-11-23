using Moq;
using NUnit.Framework;
using Shared.Exceptions;
using TrainService.DTOs;
using TrainService.Models;
using TrainService.Repositories;
using TrainService.Services;

namespace Tests.Train;

[TestFixture]
public class TrainServiceTests
{
    private Mock<ITrainRepository> _trainRepositoryMock = null!;
    private TrainService.Services.TrainService _trainService = null!;

    [SetUp]
    public void SetUp()
    {
        _trainRepositoryMock = new Mock<ITrainRepository>();
        _trainService = new TrainService.Services.TrainService(_trainRepositoryMock.Object);
    }

    // ─── Create Train ─────────────────────────────────────────────────────────

    [Test]
    public async Task CreateTrain_WithValidData_ReturnsTrainResponse()
    {
        // Arrange
        _trainRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<TrainService.Models.Train>()))
            .ReturnsAsync((TrainService.Models.Train t) => { t.TrainId = 1; return t; });

        var request = new CreateTrainRequestDto
        {
            TrainName     = "Test Express",
            Source        = "Mumbai",
            Destination   = "Delhi",
            JourneyDate   = DateTime.UtcNow.AddDays(5),
            DepartureTime = new TimeSpan(10, 0, 0),
            ArrivalTime   = new TimeSpan(22, 0, 0),
            TotalSeats    = 100,
            Fare          = 1500m
        };

        // Act
        var result = await _trainService.CreateTrainAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TrainName,       Is.EqualTo("Test Express"));
        Assert.That(result.AvailableSeats,  Is.EqualTo(100)); // Initialized to TotalSeats
        Assert.That(result.Status,          Is.EqualTo("Active"));
    }

    [Test]
    public void CreateTrain_SameSourceAndDestination_ThrowsBadRequestException()
    {
        // Arrange
        var request = new CreateTrainRequestDto
        {
            TrainName     = "Same Station Train",
            Source        = "Mumbai",
            Destination   = "Mumbai",
            JourneyDate   = DateTime.UtcNow.AddDays(5),
            DepartureTime = new TimeSpan(10, 0, 0),
            ArrivalTime   = new TimeSpan(12, 0, 0),
            TotalSeats    = 100,
            Fare          = 500m
        };

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(() => _trainService.CreateTrainAsync(request));
    }

    [Test]
    public void CreateTrain_PastJourneyDate_ThrowsBadRequestException()
    {
        // Arrange
        var request = new CreateTrainRequestDto
        {
            TrainName     = "Past Train",
            Source        = "Mumbai",
            Destination   = "Delhi",
            JourneyDate   = DateTime.UtcNow.AddDays(-1), // Past date
            DepartureTime = new TimeSpan(10, 0, 0),
            ArrivalTime   = new TimeSpan(22, 0, 0),
            TotalSeats    = 100,
            Fare          = 1500m
        };

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(() => _trainService.CreateTrainAsync(request));
    }

    // ─── Search Train ─────────────────────────────────────────────────────────

    [Test]
    public async Task SearchTrains_WithMatchingCriteria_ReturnsTrains()
    {
        // Arrange
        var journeyDate = new DateTime(2026, 12, 1);
        var trains = new List<TrainService.Models.Train>
        {
            new() { TrainId = 1, TrainName = "Express 1", Source = "Mumbai",
                    Destination = "Delhi", JourneyDate = journeyDate,
                    TotalSeats = 100, AvailableSeats = 60, Fare = 1500m, Status = "Active",
                    DepartureTime = TimeSpan.FromHours(10), ArrivalTime = TimeSpan.FromHours(22) },
            new() { TrainId = 2, TrainName = "Express 2", Source = "Mumbai",
                    Destination = "Delhi", JourneyDate = journeyDate,
                    TotalSeats = 80, AvailableSeats = 20, Fare = 1200m, Status = "Active",
                    DepartureTime = TimeSpan.FromHours(14), ArrivalTime = TimeSpan.FromHours(2) }
        };

        _trainRepositoryMock
            .Setup(r => r.SearchTrainsAsync("Mumbai", "Delhi", journeyDate))
            .ReturnsAsync(trains);

        // Act
        var results = (await _trainService.SearchTrainsAsync("Mumbai", "Delhi", journeyDate)).ToList();

        // Assert
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].TrainName, Is.EqualTo("Express 1"));
    }

    [Test]
    public async Task SearchTrains_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        _trainRepositoryMock
            .Setup(r => r.SearchTrainsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TrainService.Models.Train>());

        // Act
        var results = await _trainService.SearchTrainsAsync("Kolkata", "Goa", DateTime.Today);

        // Assert
        Assert.That(results, Is.Empty);
    }

    // ─── Seat Availability ────────────────────────────────────────────────────

    [Test]
    public async Task CheckAvailability_ExistingTrain_ReturnsSeatInfo()
    {
        // Arrange
        var train = new TrainService.Models.Train
        {
            TrainId = 5, TrainName = "Avail Train",
            TotalSeats = 100, AvailableSeats = 42,
            Fare = 800m, JourneyDate = DateTime.Today.AddDays(3),
            Status = "Active", Source = "A", Destination = "B",
            DepartureTime = TimeSpan.Zero, ArrivalTime = TimeSpan.FromHours(5)
        };

        _trainRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(train);

        // Act
        var result = await _trainService.CheckSeatAvailabilityAsync(5);

        // Assert
        Assert.That(result.TotalSeats,     Is.EqualTo(100));
        Assert.That(result.AvailableSeats, Is.EqualTo(42));
    }

    // ─── Overbooking Prevention ───────────────────────────────────────────────

    [Test]
    public void ReserveSeats_WhenNotEnoughSeats_ThrowsConflictException()
    {
        // Arrange
        var train = new TrainService.Models.Train
        {
            TrainId = 1, Status = "Active",
            TotalSeats = 10, AvailableSeats = 2, // only 2 available
            TrainName = "Full Train", Source = "A", Destination = "B",
            JourneyDate = DateTime.Today.AddDays(2), Fare = 500m,
            DepartureTime = TimeSpan.Zero, ArrivalTime = TimeSpan.FromHours(4)
        };

        _trainRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(train);

        var request = new ReserveSeatsRequestDto { TrainId = 1, SeatCount = 5 }; // Requesting 5 > 2

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(() => _trainService.ReserveSeatsAsync(request));
    }

    [Test]
    public async Task ReserveSeats_WithAvailableSeats_DeductsSeatsCorrectly()
    {
        // Arrange
        var train = new TrainService.Models.Train
        {
            TrainId = 2, Status = "Active",
            TotalSeats = 100, AvailableSeats = 50,
            TrainName = "Available Train", Source = "X", Destination = "Y",
            JourneyDate = DateTime.Today.AddDays(2), Fare = 1000m,
            DepartureTime = TimeSpan.Zero, ArrivalTime = TimeSpan.FromHours(6)
        };

        _trainRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(train);
        _trainRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TrainService.Models.Train>())).Returns(Task.CompletedTask);

        var request = new ReserveSeatsRequestDto { TrainId = 2, SeatCount = 3 };

        // Act
        var result = await _trainService.ReserveSeatsAsync(request);

        // Assert
        Assert.That(result.Success,        Is.True);
        Assert.That(result.ReservedSeats,  Is.EqualTo(3));
        Assert.That(result.RemainingSeats, Is.EqualTo(47));
    }

    [Test]
    public void ReserveSeats_NonExistentTrain_ThrowsNotFoundException()
    {
        // Arrange
        _trainRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((TrainService.Models.Train?)null);

        var request = new ReserveSeatsRequestDto { TrainId = 999, SeatCount = 1 };

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => _trainService.ReserveSeatsAsync(request));
    }

    [Test]
    public async Task ReserveSeats_ExactlyAvailableCount_Succeeds()
    {
        // Arrange
        var train = new TrainService.Models.Train
        {
            TrainId = 3, Status = "Active",
            TotalSeats = 10, AvailableSeats = 3,
            TrainName = "Exact Train", Source = "A", Destination = "B",
            JourneyDate = DateTime.Today.AddDays(1), Fare = 300m,
            DepartureTime = TimeSpan.Zero, ArrivalTime = TimeSpan.FromHours(2)
        };

        _trainRepositoryMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(train);
        _trainRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TrainService.Models.Train>())).Returns(Task.CompletedTask);

        var request = new ReserveSeatsRequestDto { TrainId = 3, SeatCount = 3 };

        // Act
        var result = await _trainService.ReserveSeatsAsync(request);

        // Assert
        Assert.That(result.Success,        Is.True);
        Assert.That(result.RemainingSeats, Is.EqualTo(0));
    }
}
