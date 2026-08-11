using System.Globalization;
using OrderFlow.Domain;

namespace OrderFlow.Exhibits.Before.Ocp;

public static class BeforeOcpRunner
{
    public static void Run()
    {
        Console.WriteLine("=== Before: OCP Violation ===");

        var calculator = new PricingCalculator();
        var items = new[]
        {
            new OrderItem { Quantity = 2, UnitPriceAtOrder = 25.00m },
            new OrderItem { Quantity = 1, UnitPriceAtOrder = 30.00m }
        };

        // Invariant culture: this exhibit's whole point is a comparable console transcript
        // between Before/After runs, which a locale-dependent decimal separator would break.
        var noDiscount = calculator.CalculateTotal(items, DiscountType.None, 0);
        Console.WriteLine($"[Pricing] No discount: {noDiscount.ToString("0.00", CultureInfo.InvariantCulture)}");

        var percentageDiscount = calculator.CalculateTotal(items, DiscountType.Percentage, 10);
        Console.WriteLine($"[Pricing] Percentage discount (10%): {percentageDiscount.ToString("0.00", CultureInfo.InvariantCulture)}");

        var flatDiscount = calculator.CalculateTotal(items, DiscountType.FlatAmount, 5);
        Console.WriteLine($"[Pricing] Flat discount ($5): {flatDiscount.ToString("0.00", CultureInfo.InvariantCulture)}");
    }
}
