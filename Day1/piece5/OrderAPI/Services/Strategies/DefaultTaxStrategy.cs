namespace OrderApi.Services.Strategies
{
    // Fallback rate — must be registered last so specific states match first.
    public class DefaultTaxStrategy : ITaxStrategy
    {
        private readonly decimal _rate;

        public DefaultTaxStrategy(decimal rate = 0.08m) => _rate = rate;

        public bool CanApply(string shippingAddress) => true;
        public decimal GetRate(string shippingAddress) => _rate;
    }
}
