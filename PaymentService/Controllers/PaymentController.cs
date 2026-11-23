using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Process simulated payment (Internal and direct routes supported)
    /// </summary>
    [HttpPost("process")]
    [HttpPost("/api/internal/payments")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequestDto request)
    {
        var result = await _paymentService.ProcessPaymentAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Process refund for a cancelled booking using path parameter
    /// </summary>
    [HttpPost("{reservationId}/refund")]
    [HttpPost("/api/internal/payments/{reservationId}/refund")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RefundPayment(int reservationId, [FromBody] RefundPaymentRequestDto request)
    {
        var result = await _paymentService.RefundPaymentAsync(reservationId, request);
        return Ok(result);
    }

    /// <summary>
    /// Process refund for a cancelled booking using request body
    /// </summary>
    [HttpPost("refund")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Refund([FromBody] RefundRequestWithReservationDto request)
    {
        var result = await _paymentService.RefundPaymentAsync(request.ReservationId, new RefundPaymentRequestDto
        {
            Amount = request.Amount
        });
        return Ok(result);
    }

    /// <summary>
    /// Retrieve payment records for a reservation
    /// </summary>
    [HttpGet("{reservationId}")]
    [HttpGet("/api/internal/payments/{reservationId}")]
    [ProducesResponseType(typeof(IEnumerable<PaymentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByReservationId(int reservationId)
    {
        var result = await _paymentService.GetPaymentsByReservationIdAsync(reservationId);
        return Ok(result);
    }
}
