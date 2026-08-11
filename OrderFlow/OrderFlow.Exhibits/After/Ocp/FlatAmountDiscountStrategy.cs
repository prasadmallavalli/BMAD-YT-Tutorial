namespace OrderFlow.Exhibits.After.Ocp;

public class FlatAmountDiscountStrategy : IDiscountStrategy
{
    private readonly decimal _amount;

    public FlatAmountDiscountStrategy(decimal amount)
    {
        _amount = amount;
    }

    public decimal Apply(decimal baseTotal) => baseTotal - _amount;
}
