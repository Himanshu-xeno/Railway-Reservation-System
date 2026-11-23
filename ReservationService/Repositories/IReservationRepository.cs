using ReservationService.Models;

namespace ReservationService.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(int reservationId);
    Task<Reservation?> GetByPnrAsync(string pnr);
    Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Reservation>> GetAllAsync();
    Task<bool> ExistsPnrAsync(string pnr);
    Task<Reservation> CreateAsync(Reservation reservation);
    Task UpdateAsync(Reservation reservation);
    Task AddCancellationAsync(Cancellation cancellation);
}
