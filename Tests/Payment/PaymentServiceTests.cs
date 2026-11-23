using Moq;
using NUnit.Framework;
using PaymentService.DTOs;
using PaymentService.Repositories;
using PaymentService.Services;
using Shared.Exceptions;
using PayModel = PaymentService.Models.Payment;

namespace Tests.PaymentTests;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IPaymentRepository> _paymentRepoMock = null!;
    private PaymentService.Services.PaymentService _paymentService = null!;

    [SetUp]
    public void SetUp()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _paymentService  = new PaymentService.Services.PaymentService(_paymentRepoMock.Object);
    }

    // ─── Process Payment Tests ────────────────────────────────────────────────

    [Test]
    public async Task ProcessPayment_WithValidRequest_ReturnsSuccessPayment()
    {
        // Arrange
        _paymentRepoMock
            .Setup(r => r.HasSuccessfulPaymentAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        _paymentRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<PayModel>()))
            .ReturnsAsync((PayModel p) => { p.PaymentId = 1; return p; });

        var request = new ProcessPaymentRequestDto { ReservationId = 101, Amount = 1500m };

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PaymentStatus, Is.EqualTo("Success"));
        Assert.That(result.PaymentType,   Is.EqualTo("Payment"));
        Assert.That(result.Amount,        Is.EqualTo(1500m));
        Assert.That(result.TransactionId, Does.StartWith("TXN"));
    }

    [Test]
    public void ProcessPayment_DuplicatePayment_ThrowsConflictException()
    {
        // Arrange – already paid
        _paymentRepoMock
            .Setup(r => r.HasSuccessfulPaymentAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        var request = new ProcessPaymentRequestDto { ReservationId = 102, Amount = 800m };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(() => _paymentService.ProcessPaymentAsync(request));
    }

    [Test]
    public void ProcessPayment_ZeroAmount_ThrowsBadRequestException()
    {
        // Arrange
        var request = new ProcessPaymentRequestDto { ReservationId = 103, Amount = 0m };

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(() => _paymentService.ProcessPaymentAsync(request));
    }

    [Test]
    public async Task ProcessPayment_GeneratesUniqueTransactionId()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.HasSuccessfulPaymentAsync(It.IsAny<int>())).ReturnsAsync(false);

        var transactionIds = new List<string>();
        _paymentRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<PayModel>()))
            .Callback<PayModel>(p => transactionIds.Add(p.TransactionId))
            .ReturnsAsync((PayModel p) => { p.PaymentId = 1; return p; });

        var req1 = new ProcessPaymentRequestDto { ReservationId = 201, Amount = 500m };
        var req2 = new ProcessPaymentRequestDto { ReservationId = 202, Amount = 700m };

        // Act
        await _paymentService.ProcessPaymentAsync(req1);
        await _paymentService.ProcessPaymentAsync(req2);

        // Assert – Transaction IDs must differ
        Assert.That(transactionIds[0], Is.Not.EqualTo(transactionIds[1]));
    }

    // ─── Refund Payment Tests ─────────────────────────────────────────────────

    [Test]
    public async Task RefundPayment_WithValidRefundRequest_ReturnsRefundRecord()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.HasSuccessfulPaymentAsync(301)).ReturnsAsync(true);
        _paymentRepoMock.Setup(r => r.HasSuccessfulRefundAsync(301)).ReturnsAsync(false);

        _paymentRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<PayModel>()))
            .ReturnsAsync((PayModel p) => { p.PaymentId = 10; return p; });

        var request = new RefundPaymentRequestDto { Amount = 900m };

        // Act
        var result = await _paymentService.RefundPaymentAsync(301, request);

        // Assert
        Assert.That(result.PaymentType,   Is.EqualTo("Refund"));
        Assert.That(result.PaymentStatus, Is.EqualTo("Success"));
        Assert.That(result.Amount,        Is.EqualTo(900m));
        Assert.That(result.TransactionId, Does.StartWith("REF"));
    }

    [Test]
    public void RefundPayment_NoOriginalPayment_ThrowsBadRequestException()
    {
        // Arrange – no original payment
        _paymentRepoMock.Setup(r => r.HasSuccessfulPaymentAsync(302)).ReturnsAsync(false);

        var request = new RefundPaymentRequestDto { Amount = 500m };

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(() => _paymentService.RefundPaymentAsync(302, request));
    }

    [Test]
    public void RefundPayment_AlreadyRefunded_ThrowsConflictException()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.HasSuccessfulPaymentAsync(303)).ReturnsAsync(true);
        _paymentRepoMock.Setup(r => r.HasSuccessfulRefundAsync(303)).ReturnsAsync(true); // already refunded

        var request = new RefundPaymentRequestDto { Amount = 450m };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(() => _paymentService.RefundPaymentAsync(303, request));
    }
}
