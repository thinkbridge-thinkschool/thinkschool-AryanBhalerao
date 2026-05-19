using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderApi.Data;
using OrderApi.Services;
using OrderApi.Services.Strategies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("OrderDb"));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IOrderService, OrderService>();

// Discount strategies — evaluated in registration order; ExternalApi is the fallback (CanApply = true).
builder.Services.AddSingleton<IDiscountStrategy>(_ => new PercentageDiscountStrategy("SAVE10", 0.9m));
builder.Services.AddSingleton<IDiscountStrategy>(_ => new PercentageDiscountStrategy("SAVE20", 0.8m));
builder.Services.AddScoped<IDiscountStrategy, ExternalApiDiscountStrategy>();

// Tax strategies — evaluated in registration order; Default is the fallback (CanApply = true).
builder.Services.AddSingleton<ITaxStrategy>(_ => new StateTaxStrategy("NY", 0.08875m));
builder.Services.AddSingleton<ITaxStrategy>(_ => new StateTaxStrategy("CA", 0.0725m));
builder.Services.AddSingleton<ITaxStrategy>(_ => new DefaultTaxStrategy(0.08m));

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
