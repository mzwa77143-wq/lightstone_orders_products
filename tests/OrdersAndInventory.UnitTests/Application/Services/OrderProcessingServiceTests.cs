using System.Data;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersAndInventory.Application.Common.Interfaces;
using OrdersAndInventory.Application.DTOs;
using OrdersAndInventory.Application.Services;
using OrdersAndInventory.Application.Validators;
using OrdersAndInventory.Domain.Entities;
using OrdersAndInventory.Domain.Enums;
using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.UnitTests.Application.Services;

public class OrderProcessingServiceTests
{
    private readonly Mock<IApplicationDbContext> _mockContext = new();
    private readonly Mock<IInventoryRepository> _mockInventoryRepo = new();
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider = new();
    private readonly IValidator<SubmitOrderRequest> _validator = new SubmitOrderRequestValidator();
    private readonly Mock<ILogger<OrderProcessingService>> _mockLogger = new();
    private readonly Mock<IDbContextTransaction> _mockTransaction = new();

    public OrderProcessingServiceTests()
    {
        _mockDateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc));
        _mockContext
            .Setup(c => c.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockTransaction.Object);
    }

    [Fact]
    public async Task ProcessOrderAsync_WhenValidationFails_ShouldThrowDomainValidationException()
    {
        // Arrange
        var service = new OrderProcessingService(
            _mockContext.Object,
            _mockInventoryRepo.Object,
            _mockDateTimeProvider.Object,
            _validator,
            _mockLogger.Object);

        var invalidRequest = new SubmitOrderRequest("", DateTime.UtcNow, new List<SubmitOrderItemRequest>());

        // Act
        var act = () => service.ProcessOrderAsync(invalidRequest);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>();
    }
}
