using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderApi.Services.Strategies
{
    public class PercentageDiscountStrategy : IDiscountStrategy
    {
        private readonly string _code;
        private readonly decimal _multiplier;

        public PercentageDiscountStrategy(string code, decimal multiplier)
        {
            _code = code;
            _multiplier = multiplier;
        }

        public bool CanApply(string code) =>
            code.Equals(_code, StringComparison.OrdinalIgnoreCase);

        public Task<decimal> ApplyAsync(decimal total, string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(total * _multiplier);
    }
}
