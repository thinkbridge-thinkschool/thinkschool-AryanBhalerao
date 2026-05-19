using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderApi.Data;
using OrderApi.Models;
using OrderApi.Services.Strategies;

namespace OrderApi.Services
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);
    }

    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _dbContext;
        private readonly ILogger<OrderService> _logger;
        private readonly IEnumerable<IDiscountStrategy> _discountStrategies;
        private readonly IEnumerable<ITaxStrategy> _taxStrategies;

        public OrderService(
            OrderDbContext dbContext,
            ILogger<OrderService> logger,
            IEnumerable<IDiscountStrategy> discountStrategies,
            IEnumerable<ITaxStrategy> taxStrategies)
        {
            _dbContext = dbContext;
            _logger = logger;
            _discountStrategies = discountStrategies;
            _taxStrategies = taxStrategies;
        }

        public async Task<OrderResponse> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Order started for {CustomerName}", request.CustomerName);

            if (request == null || string.IsNullOrEmpty(request.CustomerName) || string.IsNullOrEmpty(request.CustomerEmail))
                return new OrderResponse { Success = false, Message = "Invalid request or missing customer details." };

            if (request.Items == null || !request.Items.Any())
                return new OrderResponse { Success = false, Message = "No items in order." };

            var pendingOrdersCount = await _dbContext.Orders
                .CountAsync(o => o.CustomerEmail == request.CustomerEmail && o.Status == "Pending", cancellationToken);

            if (pendingOrdersCount > 3)
                return new OrderResponse { Success = false, Message = "Too many pending orders." };

            decimal totalAmount = 0;
            var orderItemsToSave = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

                if (product == null)
                    return new OrderResponse { Success = false, Message = $"Product {item.ProductId} not found." };

                if (product.StockQuantity < item.Quantity)
                    return new OrderResponse { Success = false, Message = $"Insufficient stock for product {product.Name}." };

                product.StockQuantity -= item.Quantity;

                orderItemsToSave.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });

                totalAmount += product.Price * item.Quantity;
            }

            if (!string.IsNullOrEmpty(request.DiscountCode))
            {
                var strategy = _discountStrategies.FirstOrDefault(s => s.CanApply(request.DiscountCode));
                if (strategy != null)
                    totalAmount = await strategy.ApplyAsync(totalAmount, request.DiscountCode, cancellationToken);
            }

            var taxStrategy = _taxStrategies.First(s => s.CanApply(request.ShippingAddress));
            decimal taxRate = taxStrategy.GetRate(request.ShippingAddress);
            decimal totalWithTax = totalAmount + (totalAmount * taxRate);

            if (!SimulatePayment(request.CreditCardNumber))
                return new OrderResponse { Success = false, Message = "Payment failed." };

            var newOrder = new Order
            {
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                TotalAmount = totalWithTax,
                OrderDate = DateTime.UtcNow,
                Status = "Paid",
                Items = orderItemsToSave
            };

            _dbContext.Orders.Add(newOrder);
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await SendConfirmationEmailAsync(newOrder.CustomerEmail, newOrder.Id, totalWithTax);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send confirmation email to {Email}", newOrder.CustomerEmail);
            }

            return new OrderResponse
            {
                Success = true,
                Message = "Order placed successfully",
                OrderId = newOrder.Id,
                TotalPaid = totalWithTax,
                EstimatedDelivery = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd")
            };
        }

        private static bool SimulatePayment(string cc) => cc.Length >= 16;

        private Task SendConfirmationEmailAsync(string email, int orderId, decimal amount)
        {
            _logger.LogInformation("Sending email for order {OrderId} to {Email}", orderId, email);
            return Task.CompletedTask;
        }
    }
}
