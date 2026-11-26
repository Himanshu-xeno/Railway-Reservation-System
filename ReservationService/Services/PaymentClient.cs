using Shared.Exceptions;
using System.Net.Http.Json;

namespace ReservationService.Services;

public class PaymentClient : IPaymentClient
{
    private readonly HttpClient _httpClient;

    public PaymentClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(int reservationId, decimal amount)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/payments/process", new
        {
            ReservationId = reservationId,
            Amount = amount
        });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new ConflictException("Duplicate payment detected for this reservation.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Payment processing failed."
            };
        }

        return new PaymentResultDto
        {
            Success = true,
            Message = "Payment processed successfully."
        };
    }

    public async Task<PaymentResultDto> RefundPaymentAsync(int reservationId, decimal amount)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/payments/{reservationId}/refund", new
        {
            Amount = amount
        });

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new ConflictException("Refund has already been processed for this reservation.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Refund processing failed."
            };
        }

        return new PaymentResultDto
        {
            Success = true,
            Message = "Refund processed successfully."
        };
    }
}
