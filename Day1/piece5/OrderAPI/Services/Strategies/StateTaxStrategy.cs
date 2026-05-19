namespace OrderApi.Services.Strategies
{
    public class StateTaxStrategy : ITaxStrategy
    {
        private readonly string _addressKeyword;
        private readonly decimal _rate;

        public StateTaxStrategy(string addressKeyword, decimal rate)
        {
            _addressKeyword = addressKeyword;
            _rate = rate;
        }

        public bool CanApply(string shippingAddress) =>
            shippingAddress.Contains(_addressKeyword);

        public decimal GetRate(string shippingAddress) => _rate;
    }
}
