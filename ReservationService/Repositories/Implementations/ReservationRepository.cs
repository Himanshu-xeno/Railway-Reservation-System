using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;

namespace ReservationService.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly ReservationDbContext _context;

    public ReservationRepository(ReservationDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(int reservationId)
    {
        return await _context.Reservations
            .Include(r => r.Passengers)
            .Include(r => r.Cancellation)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId);
    }

    public async Task<Reservation?> GetByPnrAsync(string pnr)
    {
        return await _context.Reservations
            .Include(r => r.Passengers)
            .Include(r => r.Cancellation)
            .FirstOrDefaultAsync(r => r.PNR.ToUpper() == pnr.ToUpper());
    }

    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId)
    {
        return await _context.Reservations
            .Include(r => r.Passengers)
            .Include(r => r.Cancellation)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetAllAsync()
    {
        return await _context.Reservations
            .Include(r => r.Passengers)
            .Include(r => r.Cancellation)
            .OrderByDescending(r => r.BookingDate)
            .ToListAsync();
    }

    public async Task<bool> ExistsPnrAsync(string pnr)
    {
        return await _context.Reservations.AnyAsync(r => r.PNR.ToUpper() == pnr.ToUpper());
    }

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    public async Task UpdateAsync(Reservation reservation)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync();
    }

    public async Task AddCancellationAsync(Cancellation cancellation)
    {
        _context.Cancellations.Add(cancellation);
        await _context.SaveChangesAsync();
    }
}
