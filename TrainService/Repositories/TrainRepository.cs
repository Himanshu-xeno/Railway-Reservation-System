using Microsoft.EntityFrameworkCore;
using TrainService.Data;
using TrainService.Models;

namespace TrainService.Repositories;

public class TrainRepository : ITrainRepository
{
    private readonly TrainDbContext _context;

    public TrainRepository(TrainDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Train>> GetAllAsync()
    {
        return await _context.Trains.AsNoTracking().ToListAsync();
    }

    public async Task<Train?> GetByIdAsync(int trainId)
    {
        return await _context.Trains.FirstOrDefaultAsync(t => t.TrainId == trainId);
    }

    public async Task<IEnumerable<Train>> SearchTrainsAsync(string source, string destination, DateTime journeyDate)
    {
        var targetDate = journeyDate.Date;
        return await _context.Trains
            .AsNoTracking()
            .Where(t => t.Source.ToLower() == source.ToLower()
                     && t.Destination.ToLower() == destination.ToLower()
                     && t.JourneyDate.Date == targetDate
                     && t.Status == "Active")
            .ToListAsync();
    }

    public async Task<Train> CreateAsync(Train train)
    {
        _context.Trains.Add(train);
        await _context.SaveChangesAsync();
        return train;
    }

    public async Task UpdateAsync(Train train)
    {
        _context.Trains.Update(train);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int trainId)
    {
        var train = await _context.Trains.FirstOrDefaultAsync(t => t.TrainId == trainId);
        if (train != null)
        {
            train.Status = "Inactive";
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
