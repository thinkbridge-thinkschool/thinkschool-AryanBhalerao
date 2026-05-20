using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Transient: new instance every resolution — safe for stateless services that may have
// non-thread-safe internals (e.g., a calculator that accumulates state per call).
builder.Services.AddTransient<ITaxCalculator, TaxCalculator>();

// Scoped: one instance per HTTP request — correct for anything that wraps DbContext,
// since DbContext itself is scoped and must not be shared across requests.
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("OrderDb"));
builder.Services.AddScoped<IOrderService, OrderService>();

// Singleton: one instance for the app's lifetime — safe only for genuinely stateless
// cross-cutting concerns. IClock has no mutable state; the same instance is fine everywhere.
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
