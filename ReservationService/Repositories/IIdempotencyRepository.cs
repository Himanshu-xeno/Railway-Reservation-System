using ReservationService.Models;

namespace ReservationService.Repositories;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetAsync(int userId, string idempotencyKey);
    Task<IdempotencyRecord> CreateAsync(IdempotencyRecord record);
    Task UpdateAsync(IdempotencyRecord record);
    Task DeleteAsync(int id);
}
