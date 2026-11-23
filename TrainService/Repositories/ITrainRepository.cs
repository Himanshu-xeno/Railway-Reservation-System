using TrainService.Models;

namespace TrainService.Repositories;

public interface ITrainRepository
{
    Task<IEnumerable<Train>> GetAllAsync();
    Task<Train?> GetByIdAsync(int trainId);
    Task<IEnumerable<Train>> SearchTrainsAsync(string source, string destination, DateTime journeyDate);
    Task<Train> CreateAsync(Train train);
    Task UpdateAsync(Train train);
    Task DeleteAsync(int trainId);
    Task<bool> SaveChangesAsync();
}
