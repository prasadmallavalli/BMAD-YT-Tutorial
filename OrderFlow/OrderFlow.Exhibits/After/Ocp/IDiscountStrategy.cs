namespace OrderFlow.Exhibits.After.Ocp;

public interface IDiscountStrategy
{
    decimal Apply(decimal baseTotal);
}
