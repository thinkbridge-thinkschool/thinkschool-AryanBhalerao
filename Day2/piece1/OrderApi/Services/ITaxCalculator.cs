namespace OrderApi.Services
{
    public interface ITaxCalculator
    {
        decimal Calculate(decimal subtotal, string shippingAddress);
    }

    // Registered as Transient: stateless pure computation, but transient guards against
    // non-thread-safe internals if this ever accumulates intermediate state (e.g., a StringBuilder).
    public class TaxCalculator : ITaxCalculator
    {
        public decimal Calculate(decimal subtotal, string shippingAddress)
        {
            decimal rate = shippingAddress.Contains("NY") ? 0.08875m
                         : shippingAddress.Contains("CA") ? 0.0725m
                         : 0.08m;
            return subtotal * rate;
        }
    }
}
