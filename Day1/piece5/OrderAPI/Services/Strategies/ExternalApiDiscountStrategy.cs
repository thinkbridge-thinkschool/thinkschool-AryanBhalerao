using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OrderApi.Services.Strategies
{
    // Fallback: validates unknown codes against an external API.
    // CanApply always returns true so it must be registered last.
    public class ExternalApiDiscountStrategy : IDiscountStrategy
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalApiDiscountStrategy> _logger;

        public ExternalApiDiscountStrategy(HttpClient httpClient, ILogger<ExternalApiDiscountStrategy> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool CanApply(string code) => true;

        public async Task<decimal> ApplyAsync(decimal total, string code, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"https://api.discount-checker.com/validate?code={code}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (content.Contains("valid"))
                        return total - 5.0m;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to validate discount code {Code}. Ignoring discount.", code);
            }

            return total;
        }
    }
}
