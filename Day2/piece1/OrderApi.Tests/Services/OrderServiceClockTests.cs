using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderApi.Data;
using OrderApi.Models;
using OrderApi.Services;
using Xunit;

namespace OrderApi.Tests.Services;

// Inline fake — no mocking framework needed for a single-property interface.
sealed class FakeClock(DateTimeOffset fixedTime) : IClock
{
    public DateTimeOffset UtcNow => fixedTime;
}

public class OrderServiceClockTests
{
    private static OrderDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new OrderDbContext(opts);
        db.Products.Add(new Product { Id = 1, Name = "Widget", Price = 10m, StockQuantity = 100 });
        db.SaveChanges();
        return db;
    }

    private static OrderService Build(IClock clock)
    {
        return new OrderService(
            BuildDb(),
            NullLogger<OrderService>.Instance,
            new HttpClient(),
            clock,
            new TaxCalculator());
    }

    [Fact]
    public async Task EstimatedDelivery_IsThreeDaysAfterClockTime_NotSystemTime()
    {
        // Arrange — pin the clock to a known point far in the past so the assertion
        // can never accidentally match DateTime.UtcNow.
        var frozen = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sut = Build(new FakeClock(frozen));

        var request = new OrderRequest
        {
            CustomerName    = "Alice",
            CustomerEmail   = "alice@example.com",
            ShippingAddress = "123 Main St",
            CreditCardNumber = "1234567890123456",
            Items = [new OrderItemRequest { ProductId = 1, Quantity = 1 }]
        };

        // Act
        var response = await sut.CreateOrderAsync(request);

        // Assert
        Assert.True(response.Success, response.Message);
        Assert.Equal(frozen.AddDays(3).ToString("yyyy-MM-dd"), response.EstimatedDelivery);
    }

    [Fact]
    public async Task OrderDate_IsSetFromClockNotSystemTime()
    {
        var frozen = new DateTimeOffset(2000, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var db = BuildDb();
        var sut = new OrderService(
            db,
            NullLogger<OrderService>.Instance,
            new HttpClient(),
            new FakeClock(frozen),
            new TaxCalculator());

        var request = new OrderRequest
        {
            CustomerName    = "Bob",
            CustomerEmail   = "bob@example.com",
            ShippingAddress = "456 Oak Ave",
            CreditCardNumber = "9876543210987654",
            Items = [new OrderItemRequest { ProductId = 1, Quantity = 1 }]
        };

        var response = await sut.CreateOrderAsync(request);

        Assert.True(response.Success, response.Message);
        var savedOrder = db.Orders.Single(o => o.Id == response.OrderId);
        Assert.Equal(frozen.UtcDateTime, savedOrder.OrderDate);
    }
}
