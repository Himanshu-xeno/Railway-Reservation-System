using Shared.Exceptions;
using System.Net.Http.Json;

namespace ReservationService.Services;

public class TrainClient : ITrainClient
{
    private readonly HttpClient _httpClient;

    public TrainClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TrainInfoDto> GetTrainByIdAsync(int trainId)
    {
        var response = await _httpClient.GetAsync($"/api/trains/{trainId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException($"Train with ID {trainId} not found.");
        }

        response.EnsureSuccessStatusCode();
        var train = await response.Content.ReadFromJsonAsync<TrainInfoDto>();
        if (train == null)
        {
            throw new NotFoundException($"Train with ID {trainId} not found.");
        }

        return train;
    }

    public async Task<bool> ReserveSeatsAsync(int trainId, int seatCount)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/internal/seats/reserve", new
        {
            TrainId = trainId,
            SeatCount = seatCount
        });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new ConflictException("Seats unavailable for the requested train.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException($"Train with ID {trainId} not found.");
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<bool> ReleaseSeatsAsync(int trainId, int seatCount)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/internal/seats/release", new
        {
            TrainId = trainId,
            SeatCount = seatCount
        });

        return response.IsSuccessStatusCode;
    }
}
