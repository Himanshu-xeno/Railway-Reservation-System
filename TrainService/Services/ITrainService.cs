using TrainService.DTOs;

namespace TrainService.Services;

public interface ITrainService
{
    Task<IEnumerable<TrainResponseDto>> GetAllTrainsAsync();
    Task<TrainResponseDto> GetTrainByIdAsync(int trainId);
    Task<IEnumerable<TrainResponseDto>> SearchTrainsAsync(string source, string destination, DateTime journeyDate);
    Task<SeatAvailabilityResponseDto> CheckSeatAvailabilityAsync(int trainId);
    Task<TrainResponseDto> CreateTrainAsync(CreateTrainRequestDto request);
    Task<TrainResponseDto> UpdateTrainAsync(int trainId, UpdateTrainRequestDto request);
    Task DeactivateTrainAsync(int trainId);
    Task<ReserveSeatsResponseDto> ReserveSeatsAsync(ReserveSeatsRequestDto request);
    Task<ReleaseSeatsResponseDto> ReleaseSeatsAsync(ReleaseSeatsRequestDto request);
}
