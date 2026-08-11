namespace OrderFlow.Domain;

// OrderType's full set is pinned (Story 2.1). OrderStatus's full lifecycle is pinned here
// (Story 3.1) — the per-OrderType allowed-transition table lives in OrderStatusService (AD-4).
public enum OrderStatus
{
    Unspecified = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
