using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using TrainService.DTOs;
using TrainService.Services;

namespace TrainService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/trains")]
public class TrainController : ControllerBase
{
    private readonly ITrainService _trainService;

    public TrainController(ITrainService trainService)
    {
        _trainService = trainService;
    }

    /// <summary>
    /// Get all trains
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TrainResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var trains = await _trainService.GetAllTrainsAsync();
        return Ok(trains);
    }

    /// <summary>
    /// Search trains by source, destination, and journey date
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TrainResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string source, [FromQuery] string destination, [FromQuery] DateTime journeyDate)
    {
        var results = await _trainService.SearchTrainsAsync(source, destination, journeyDate);
        return Ok(results);
    }

    /// <summary>
    /// Get train details by train ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TrainResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var train = await _trainService.GetTrainByIdAsync(id);
        return Ok(train);
    }

    /// <summary>
    /// Check available seats for a train
    /// </summary>
    [HttpGet("{id}/availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SeatAvailabilityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckAvailability(int id)
    {
        var availability = await _trainService.CheckSeatAvailabilityAsync(id);
        return Ok(availability);
    }

    /// <summary>
    /// Add a new train (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(TrainResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateTrainRequestDto request)
    {
        var train = await _trainService.CreateTrainAsync(request);
        return StatusCode(StatusCodes.Status201Created, train);
    }

    /// <summary>
    /// Update train details, seats, and fare (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(TrainResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTrainRequestDto request)
    {
        var updatedTrain = await _trainService.UpdateTrainAsync(id, request);
        return Ok(updatedTrain);
    }

    /// <summary>
    /// Deactivate a train (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _trainService.DeactivateTrainAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Reserve seats for a booking (Supports both internal and direct route)
    /// </summary>
    [HttpPost("reserve-seats")]
    [HttpPost("/api/internal/seats/reserve")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReserveSeatsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReserveSeats([FromBody] ReserveSeatsRequestDto request)
    {
        var result = await _trainService.ReserveSeatsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Release reserved seats upon cancellation or booking failure
    /// </summary>
    [HttpPost("release-seats")]
    [HttpPost("/api/internal/seats/release")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReleaseSeatsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseSeats([FromBody] ReleaseSeatsRequestDto request)
    {
        var result = await _trainService.ReleaseSeatsAsync(request);
        return Ok(result);
    }
}
