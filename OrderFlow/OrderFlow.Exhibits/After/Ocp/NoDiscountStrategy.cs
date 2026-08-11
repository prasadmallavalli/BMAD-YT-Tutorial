namespace OrderFlow.Exhibits.After.Ocp;

public class NoDiscountStrategy : IDiscountStrategy
{
    public decimal Apply(decimal baseTotal) => baseTotal;
}
