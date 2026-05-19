using System.Threading;
using System.Threading.Tasks;

namespace OrderApi.Services.Strategies
{
    public interface IDiscountStrategy
    {
        bool CanApply(string code);
        Task<decimal> ApplyAsync(decimal total, string code, CancellationToken cancellationToken = default);
    }
}
