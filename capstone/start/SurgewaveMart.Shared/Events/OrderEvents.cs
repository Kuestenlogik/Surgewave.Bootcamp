using SurgewaveMart.Shared.Domain;

namespace SurgewaveMart.Shared.Events;

/// <summary>
/// Event raised when a new order is created.
/// </summary>
public record OrderCreated(
    string EventType,
    int Version,
    string OrderId,
    string CustomerId,
    List<OrderItemEvent> Items,
    decimal Total,
    DateTime CreatedAt
)
{
    public OrderCreated(string orderId, string customerId, List<OrderItemEvent> items, decimal total, DateTime createdAt)
        : this("OrderCreated", 1, orderId, customerId, items, total, createdAt) { }
}

/// <summary>
/// Event raised when an order is confirmed (inventory reserved).
/// </summary>
public record OrderConfirmed(
    string EventType,
    int Version,
    string OrderId,
    string ReservationId,
    DateTime ConfirmedAt
)
{
    public OrderConfirmed(string orderId, string reservationId, DateTime confirmedAt)
        : this("OrderConfirmed", 1, orderId, reservationId, confirmedAt) { }
}

/// <summary>
/// Event raised when an order is rejected.
/// </summary>
public record OrderRejected(
    string EventType,
    int Version,
    string OrderId,
    string Reason,
    DateTime RejectedAt
)
{
    public OrderRejected(string orderId, string reason, DateTime rejectedAt)
        : this("OrderRejected", 1, orderId, reason, rejectedAt) { }
}

/// <summary>
/// Event raised when an order is cancelled.
/// </summary>
public record OrderCancelled(
    string EventType,
    int Version,
    string OrderId,
    string Reason,
    DateTime CancelledAt
)
{
    public OrderCancelled(string orderId, string reason, DateTime cancelledAt)
        : this("OrderCancelled", 1, orderId, reason, cancelledAt) { }
}

/// <summary>
/// Order item representation for events.
/// </summary>
public record OrderItemEvent(
    string ProductId,
    int Quantity,
    decimal UnitPrice
);
