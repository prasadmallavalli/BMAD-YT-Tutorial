namespace OrderFlow.Exhibits.After.Ocp;

public class PercentageDiscountStrategy : IDiscountStrategy
{
    private readonly decimal _percent;

    public PercentageDiscountStrategy(decimal percent)
    {
        _percent = percent;
    }

    public decimal Apply(decimal baseTotal) => baseTotal - (baseTotal * _percent / 100m);
}
