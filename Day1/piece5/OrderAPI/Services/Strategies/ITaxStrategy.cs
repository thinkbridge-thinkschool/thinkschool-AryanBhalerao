namespace OrderApi.Services.Strategies
{
    public interface ITaxStrategy
    {
        bool CanApply(string shippingAddress);
        decimal GetRate(string shippingAddress);
    }
}
