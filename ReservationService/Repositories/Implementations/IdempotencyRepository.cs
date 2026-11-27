using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;

namespace ReservationService.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly ReservationDbContext _context;

    public IdempotencyRepository(ReservationDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyRecord?> GetAsync(int userId, string idempotencyKey)
    {
        return await _context.IdempotencyRecords
            .FirstOrDefaultAsync(i => i.UserId == userId && i.IdempotencyKey == idempotencyKey);
    }

    public async Task<IdempotencyRecord> CreateAsync(IdempotencyRecord record)
    {
        _context.IdempotencyRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task UpdateAsync(IdempotencyRecord record)
    {
        _context.IdempotencyRecords.Update(record);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var record = await _context.IdempotencyRecords.FindAsync(id);
        if (record != null)
        {
            _context.IdempotencyRecords.Remove(record);
            await _context.SaveChangesAsync();
        }
    }
}
