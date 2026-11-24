using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.DTOs;
using ReservationService.Services;
using Shared.Constants;
using System.Security.Claims;

namespace ReservationService.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private int GetCurrentUserId()
    {
        var value = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }

    private string GetCurrentUserRole()
        => User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("Role")?.Value ?? string.Empty;

    // ─── Endpoints ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Estimate total fare before booking
    /// </summary>
    [HttpPost("fare-estimate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FareEstimateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EstimateFare([FromBody] FareEstimateRequestDto request)
    {
        var estimate = await _reservationService.EstimateFareAsync(request);
        return Ok(estimate);
    }

    /// <summary>
    /// Book a train ticket. Optionally send Idempotency-Key header to prevent duplicate bookings.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookTicket(
        [FromBody] BookTicketRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        var userId = GetCurrentUserId();
        var booking = await _reservationService.BookTicketAsync(userId, idempotencyKey ?? string.Empty, request);
        return StatusCode(StatusCodes.Status201Created, booking);
    }

    /// <summary>
    /// Get a booking by its reservation ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var role   = GetCurrentUserRole();
        var booking = await _reservationService.GetBookingByIdAsync(id, userId, role);
        return Ok(booking);
    }

    /// <summary>
    /// Look up a booking by its PNR number (e.g., PNR202612001)
    /// </summary>
    [HttpGet("pnr/{pnr}")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPnr(string pnr)
    {
        var userId = GetCurrentUserId();
        var role   = GetCurrentUserRole();
        var booking = await _reservationService.GetBookingByPnrAsync(pnr, userId, role);
        return Ok(booking);
    }

    /// <summary>
    /// Get the booking history of the currently logged-in user
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetCurrentUserId();
        var history = await _reservationService.GetBookingHistoryAsync(userId);
        return Ok(history);
    }

    /// <summary>
    /// View all bookings (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var bookings = await _reservationService.GetAllBookingsAsync();
        return Ok(bookings);
    }

    /// <summary>
    /// Cancel a confirmed booking (booking owner only)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(CancelReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _reservationService.CancelBookingAsync(id, userId);
        return Ok(result);
    }
}
