namespace OrderFlow.Exhibits.Before.Ocp;

// Exhibit-only vocabulary — Domain has no discount concept (Epic 2 locked
// StandardPricingStrategy as a no-discount, sum-only calculation), so this isn't a toy
// redefinition of an existing Domain type (AD-8's rule targets those, e.g. Order/OrderItem).
public enum DiscountType
{
    None,
    Percentage,
    FlatAmount
}
