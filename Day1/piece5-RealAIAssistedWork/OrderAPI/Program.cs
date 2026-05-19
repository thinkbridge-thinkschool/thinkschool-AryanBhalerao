using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefactoringExercise.OriginalCode;

// ── DI registration ─────────────────────────────────────────────────────────
// To add a new rule: implement IOrderRule and add one line below.
// The controller and all existing rules stay untouched.

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("OrdersDb"));

builder.Services.AddScoped<IOrderRule, RequestValidationRule>();
builder.Services.AddScoped<IOrderRule, PendingOrderLimitRule>();
builder.Services.AddScoped<IOrderRule, StockReservationRule>();
builder.Services.AddScoped<IOrderRule, DiscountRule>();
builder.Services.AddScoped<IOrderRule, TaxRule>();
builder.Services.AddScoped<IOrderRule, PaymentRule>();
builder.Services.AddScoped<IOrderRule, OrderPersistenceRule>();
builder.Services.AddScoped<IOrderRule, EmailNotificationRule>();

var app = builder.Build();
app.MapControllers();
app.Run();

// ── Namespace ────────────────────────────────────────────────────────────────
namespace RefactoringExercise.OriginalCode
{
    // ── DTOs ─────────────────────────────────────────────────────────────────

    public class OrderRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string CreditCardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public List<OrderItemRequest> Items { get; set; } = new();
        public string? DiscountCode { get; set; }
        public string? Notes { get; set; }
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // ── Entities ──────────────────────────────────────────────────────────────

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // ── DbContext ─────────────────────────────────────────────────────────────

    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }

    // ── Strategy Pattern Infrastructure ───────────────────────────────────────

    public sealed class OrderRuleResult
    {
        public bool IsSuccess { get; private init; }
        public string? ErrorMessage { get; private init; }

        public static OrderRuleResult Ok() => new() { IsSuccess = true };
        public static OrderRuleResult Fail(string message) => new() { IsSuccess = false, ErrorMessage = message };
    }

    // Shared state threaded through the rule pipeline
    public class OrderContext
    {
        public OrderRequest Request { get; init; } = null!;
        public List<(Product Product, OrderItemRequest Item)> ResolvedItems { get; } = new();
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public Order? CreatedOrder { get; set; }
    }

    public interface IOrderRule
    {
        Task<OrderRuleResult> ApplyAsync(OrderContext context);
    }

    // ── Concrete Rules ────────────────────────────────────────────────────────

    public class RequestValidationRule : IOrderRule
    {
        public Task<OrderRuleResult> ApplyAsync(OrderContext context)
        {
            var r = context.Request;

            if (string.IsNullOrWhiteSpace(r.CustomerName))
                return Task.FromResult(OrderRuleResult.Fail("CustomerName is required"));
            if (string.IsNullOrWhiteSpace(r.CustomerEmail) || !r.CustomerEmail.Contains('@'))
                return Task.FromResult(OrderRuleResult.Fail("Invalid email"));
            if (r.Items is not { Count: > 0 })
                return Task.FromResult(OrderRuleResult.Fail("No items in order"));
            if (string.IsNullOrWhiteSpace(r.CreditCardNumber) || r.CreditCardNumber.Length < 16)
                return Task.FromResult(OrderRuleResult.Fail("Invalid credit card"));

            return Task.FromResult(OrderRuleResult.Ok());
        }
    }

    public class PendingOrderLimitRule : IOrderRule
    {
        private readonly AppDbContext _db;
        public PendingOrderLimitRule(AppDbContext db) => _db = db;

        public async Task<OrderRuleResult> ApplyAsync(OrderContext context)
        {
            var pending = await _db.Orders
                .CountAsync(o => o.CustomerEmail == context.Request.CustomerEmail && o.Status == "Pending");

            return pending > 3
                ? OrderRuleResult.Fail("Too many pending orders")
                : OrderRuleResult.Ok();
        }
    }

    public class StockReservationRule : IOrderRule
    {
        private readonly AppDbContext _db;
        public StockReservationRule(AppDbContext db) => _db = db;

        public async Task<OrderRuleResult> ApplyAsync(OrderContext context)
        {
            var ids = context.Request.Items.Select(i => i.ProductId).ToList();
            var products = await _db.Products
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            decimal sub = 0;
            foreach (var item in context.Request.Items)
            {
                if (!products.ContainsKey(item.ProductId))
                    return OrderRuleResult.Fail($"Product {item.ProductId} not found");
                var product = products[item.ProductId];

                if (product.StockQuantity < item.Quantity)
                    return OrderRuleResult.Fail($"Insufficient stock for {product.Name}");

                product.StockQuantity -= item.Quantity;
                context.ResolvedItems.Add((product, item));
                sub += product.Price * item.Quantity;
            }

            context.SubTotal = sub;
            context.Total = sub;
            return OrderRuleResult.Ok();
        }
    }

    public class DiscountRule : IOrderRule
    {
        private readonly IHttpClientFactory _http;
        public DiscountRule(IHttpClientFactory http) => _http = http;

        public async Task<OrderRuleResult> ApplyAsync(OrderContext context)
        {
            var code = context.Request.DiscountCode;
            if (string.IsNullOrWhiteSpace(code)) return OrderRuleResult.Ok();

            if (code == "SAVE10") { context.Total *= 0.9m;  return OrderRuleResult.Ok(); }
            if (code == "SAVE20") { context.Total *= 0.8m;  return OrderRuleResult.Ok(); }

            try
            {
                var response = await _http.CreateClient()
                    .GetAsync($"https://api.discount-checker.com/validate?code={code}");
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (body.Contains("valid")) context.Total -= 5.0m;
                }
            }
            catch (HttpRequestException) { /* external service unavailable — skip discount */ }

            return OrderRuleResult.Ok();
        }
    }

    public class TaxRule : IOrderRule
    {
        private static readonly Dictionary<string, decimal> StateRates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["NY"]         = 0.08875m,
                ["New York"]   = 0.08875m,
                ["CA"]         = 0.0725m,
                ["California"] = 0.0725m,
            };
        private const decimal DefaultRate = 0.08m;

        public Task<OrderRuleResult> ApplyAsync(OrderContext context)
        {
            var addr = context.Request.ShippingAddress;
            var rate = StateRates.FirstOrDefault(kv => addr.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;
            if (rate == 0) rate = DefaultRate;

            context.Total += context.Total * rate;
            return Task.FromResult(OrderRuleResult.Ok());
        }
    }

    public class PaymentRule : IOrderRule
    {
        private readonly IHttpClientFactory _http;
        public PaymentRule(IHttpClientFactory http) => _http = http;

        public async Task<OrderRuleResult> ApplyAsync(OrderContext context)
        {
            try
            {
                var payload = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        cc     = context.Request.CreditCardNumber,
                        exp    = context.Request.ExpiryDate,
                        cvv    = context.Request.Cvv,
                        amount = context.Total
                    }),
                    Encoding.UTF8, "application/json");

                var response = await _http.CreateClient()
                    .PostAsync("https://fake-payment-gateway.com/charge", payload);

                return response.IsSuccessStatusCode
                    ? OrderRuleResult.Ok()
                    : OrderRuleResult.Fail("Payment declined");
            }
            catch (HttpRequestException)
            {
                return OrderRuleResult.Fail("Payment gateway unreachable");
            }
        }
    }

    public class OrderPersistenceRule : IOrderRule
    {
        private readonly AppDbContext _db;
        public OrderPersistenceRule(AppDbContext db) => _db = db;

        public async Task<OrderRuleResult> ApplyAsync(OrderContext context)
        {
            var order = new Order
            {
                CustomerName  = context.Request.CustomerName,
                CustomerEmail = context.Request.CustomerEmail,
                TotalAmount   = context.Total,
                OrderDate     = DateTime.UtcNow,
                Status        = "Paid",
                Items         = context.ResolvedItems
                    .Select(r => new OrderItem
                    {
                        ProductId = r.Product.Id,
                        Quantity  = r.Item.Quantity,
                        UnitPrice = r.Product.Price
                    }).ToList()
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            context.CreatedOrder = order;
            return OrderRuleResult.Ok();
        }
    }

    public class EmailNotificationRule : IOrderRule
    {
        public Task<OrderRuleResult> ApplyAsync(OrderContext context)
        {
            try
            {
                using var smtp = new System.Net.Mail.SmtpClient("smtp.fake-mail-server.com") { Port = 587 };
                smtp.Send(
                    "noreply@ourstore.com",
                    context.Request.CustomerEmail,
                    $"Order Confirmation #{context.CreatedOrder!.Id}",
                    $"Thank you! Total: {context.Total:C}");
            }
            catch (Exception) { /* non-critical — order is already persisted */ }

            return Task.FromResult(OrderRuleResult.Ok());
        }
    }

    // ── Controller ────────────────────────────────────────────────────────────
    // Adding a new rule = implement IOrderRule + one AddScoped line above.
    // This class never needs to change.

    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IEnumerable<IOrderRule> _rules;

        public OrdersController(IEnumerable<IOrderRule> rules) => _rules = rules;

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
        {
            var context = new OrderContext { Request = request };

            foreach (var rule in _rules)
            {
                var result = await rule.ApplyAsync(context);
                if (!result.IsSuccess)
                    return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Ok(new
            {
                success          = true,
                message          = "Order placed successfully",
                orderId          = context.CreatedOrder!.Id,
                totalPaid        = context.Total,
                estimatedDelivery = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd")
            });
        }
    }
}
