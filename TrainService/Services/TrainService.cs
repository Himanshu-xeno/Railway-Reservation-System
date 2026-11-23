using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Exceptions;
using TrainService.DTOs;
using TrainService.Models;
using TrainService.Repositories;

namespace TrainService.Services;

public class TrainService : ITrainService
{
    private readonly ITrainRepository _trainRepository;

    public TrainService(ITrainRepository trainRepository)
    {
        _trainRepository = trainRepository;
    }

    public async Task<IEnumerable<TrainResponseDto>> GetAllTrainsAsync()
    {
        var trains = await _trainRepository.GetAllAsync();
        return trains.Select(MapToResponseDto);
    }

    public async Task<TrainResponseDto> GetTrainByIdAsync(int trainId)
    {
        var train = await _trainRepository.GetByIdAsync(trainId);
        if (train == null)
        {
            throw new NotFoundException($"Train with ID {trainId} not found.");
        }

        return MapToResponseDto(train);
    }

    public async Task<IEnumerable<TrainResponseDto>> SearchTrainsAsync(string source, string destination, DateTime journeyDate)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
        {
            throw new BadRequestException("Source and destination must not be empty.");
        }

        var trains = await _trainRepository.SearchTrainsAsync(source.Trim(), destination.Trim(), journeyDate);
        return trains.Select(MapToResponseDto);
    }

    public async Task<SeatAvailabilityResponseDto> CheckSeatAvailabilityAsync(int trainId)
    {
        var train = await _trainRepository.GetByIdAsync(trainId);
        if (train == null)
        {
            throw new NotFoundException($"Train with ID {trainId} not found.");
        }

        return new SeatAvailabilityResponseDto
        {
            TrainId = train.TrainId,
            TrainName = train.TrainName,
            JourneyDate = train.JourneyDate,
            TotalSeats = train.TotalSeats,
            AvailableSeats = train.AvailableSeats,
            Fare = train.Fare,
            Status = train.Status
        };
    }

    public async Task<TrainResponseDto> CreateTrainAsync(CreateTrainRequestDto request)
    {
        // 1. Destination must differ from source
        if (string.Equals(request.Source.Trim(), request.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Destination station must differ from source station.");
        }

        // 2. JourneyDate must be today or a future date
        if (request.JourneyDate.Date < DateTime.UtcNow.Date)
        {
            throw new BadRequestException("Journey date must be today or a future date.");
        }

        // 3. TotalSeats and Fare validations
        if (request.TotalSeats <= 0)
        {
            throw new BadRequestException("Total seats must be greater than zero.");
        }

        if (request.Fare <= 0)
        {
            throw new BadRequestException("Fare must be greater than zero.");
        }

        var train = new Train
        {
            TrainName = request.TrainName.Trim(),
            Source = request.Source.Trim(),
            Destination = request.Destination.Trim(),
            JourneyDate = request.JourneyDate.Date,
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
            TotalSeats = request.TotalSeats,
            AvailableSeats = request.TotalSeats, // Initialized to TotalSeats
            Fare = request.Fare,
            Status = TrainStatusConstants.Active
        };

        var createdTrain = await _trainRepository.CreateAsync(train);
        return MapToResponseDto(createdTrain);
    }

    public async Task<TrainResponseDto> UpdateTrainAsync(int trainId, UpdateTrainRequestDto request)
    {
        var train = await _trainRepository.GetByIdAsync(trainId);
        if (train == null)
        {
            throw new NotFoundException($"Train with ID {trainId} not found.");
        }

        if (string.Equals(request.Source.Trim(), request.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Destination station must differ from source station.");
        }

        if (request.AvailableSeats > request.TotalSeats || request.AvailableSeats < 0)
        {
            throw new BadRequestException("Available seats must be between 0 and total seats.");
        }

        train.TrainName = request.TrainName.Trim();
        train.Source = request.Source.Trim();
        train.Destination = request.Destination.Trim();
        train.JourneyDate = request.JourneyDate.Date;
        train.DepartureTime = request.DepartureTime;
        train.ArrivalTime = request.ArrivalTime;
        train.TotalSeats = request.TotalSeats;
        train.AvailableSeats = request.AvailableSeats;
        train.Fare = request.Fare;
        train.Status = request.Status.Trim();

        await _trainRepository.UpdateAsync(train);
        return MapToResponseDto(train);
    }

    public async Task DeactivateTrainAsync(int trainId)
    {
        var train = await _trainRepository.GetByIdAsync(trainId);
        if (train == null)
        {
            throw new NotFoundException($"Train with ID {trainId} not found.");
        }

        await _trainRepository.DeleteAsync(trainId);
    }

    public async Task<ReserveSeatsResponseDto> ReserveSeatsAsync(ReserveSeatsRequestDto request)
    {
        var train = await _trainRepository.GetByIdAsync(request.TrainId);
        if (train == null)
        {
            throw new NotFoundException($"Train with ID {request.TrainId} not found.");
        }

        if (train.Status != TrainStatusConstants.Active)
        {
            throw new ConflictException("Train is not active for new reservations.");
        }

        // Overbooking prevention: check available seats
        if (train.AvailableSeats < request.SeatCount)
        {
            throw new ConflictException($"Only {train.AvailableSeats} seat(s) available. Requested: {request.SeatCount}.");
        }

        // Deduct seats atomically
        train.AvailableSeats -= request.SeatCount;

        try
        {
            await _trainRepository.UpdateAsync(train);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Concurrent booking conflict occurred. Please retry seat reservation.");
        }

        return new ReserveSeatsResponseDto
        {
            Success = true,
            Message = $"{request.SeatCount} seat(s) reserved successfully.",
            TrainId = train.TrainId,
            ReservedSeats = request.SeatCount,
            RemainingSeats = train.AvailableSeats
        };
    }

    public async Task<ReleaseSeatsResponseDto> ReleaseSeatsAsync(ReleaseSeatsRequestDto request)
    {
        var train = await _trainRepository.GetByIdAsync(request.TrainId);
        if (train == null)
        {
            throw new NotFoundException($"Train with ID {request.TrainId} not found.");
        }

        // Increase available seats, not exceeding total seats
        train.AvailableSeats = Math.Min(train.TotalSeats, train.AvailableSeats + request.SeatCount);

        try
        {
            await _trainRepository.UpdateAsync(train);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Concurrent seat release conflict occurred. Please retry.");
        }

        return new ReleaseSeatsResponseDto
        {
            Success = true,
            Message = $"{request.SeatCount} seat(s) released successfully.",
            TrainId = train.TrainId,
            ReleasedSeats = request.SeatCount,
            RemainingSeats = train.AvailableSeats
        };
    }

    private static TrainResponseDto MapToResponseDto(Train train)
    {
        return new TrainResponseDto
        {
            TrainId = train.TrainId,
            TrainName = train.TrainName,
            Source = train.Source,
            Destination = train.Destination,
            JourneyDate = train.JourneyDate,
            DepartureTime = train.DepartureTime,
            ArrivalTime = train.ArrivalTime,
            TotalSeats = train.TotalSeats,
            AvailableSeats = train.AvailableSeats,
            Fare = train.Fare,
            Status = train.Status
        };
    }
}
