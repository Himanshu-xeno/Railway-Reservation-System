using ReservationService.DTOs;

namespace ReservationService.Services;

public interface IReservationService
{
    Task<FareEstimateResponseDto> EstimateFareAsync(FareEstimateRequestDto request);
    Task<BookingResponseDto> BookTicketAsync(int userId, string idempotencyKey, BookTicketRequestDto request);
    Task<BookingResponseDto> GetBookingByIdAsync(int reservationId, int currentUserId, string currentUserRole);
    Task<BookingResponseDto> GetBookingByPnrAsync(string pnr, int currentUserId, string currentUserRole);
    Task<IEnumerable<BookingResponseDto>> GetBookingHistoryAsync(int userId);
    Task<IEnumerable<BookingResponseDto>> GetAllBookingsAsync();
    Task<CancelReservationResponseDto> CancelBookingAsync(int reservationId, int currentUserId);
}
