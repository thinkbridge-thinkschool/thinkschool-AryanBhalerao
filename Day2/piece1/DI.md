# DI Registrations — Three Lifetimes

```csharp
// Transient
builder.Services.AddTransient<ITaxCalculator, TaxCalculator>();

// Scoped
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("OrderDb"));
builder.Services.AddScoped<IOrderService, OrderService>();

// Singleton
builder.Services.AddSingleton<IClock, SystemClock>();
```
