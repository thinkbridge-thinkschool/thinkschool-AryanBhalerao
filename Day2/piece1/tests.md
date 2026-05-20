# Fake Clock Test

## FakeClock (inline test double)

```csharp
sealed class FakeClock(DateTimeOffset fixedTime) : IClock
{
    public DateTimeOffset UtcNow => fixedTime;
}
```

## Test

```csharp
[Fact]
public async Task EstimatedDelivery_IsThreeDaysAfterClockTime_NotSystemTime()
{
    // Pin the clock to year 2000 — far enough in the past that the assertion
    // can never accidentally match DateTime.UtcNow.
    var frozen = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    var opts = new DbContextOptionsBuilder<OrderDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    using var db = new OrderDbContext(opts);
    db.Products.Add(new Product { Id = 1, Name = "Widget", Price = 10m, StockQuantity = 100 });
    db.SaveChanges();

    var sut = new OrderService(
        db,
        NullLogger<OrderService>.Instance,
        new HttpClient(),
        new FakeClock(frozen),
        new TaxCalculator());

    var request = new OrderRequest
    {
        CustomerName     = "Alice",
        CustomerEmail    = "alice@example.com",
        ShippingAddress  = "123 Main St",
        CreditCardNumber = "1234567890123456",
        Items = [new OrderItemRequest { ProductId = 1, Quantity = 1 }]
    };

    var response = await sut.CreateOrderAsync(request);

    Assert.True(response.Success, response.Message);
    Assert.Equal(frozen.AddDays(3).ToString("yyyy-MM-dd"), response.EstimatedDelivery);
    // "2000-01-04" — deterministic, not dependent on when the test runs
}
```
