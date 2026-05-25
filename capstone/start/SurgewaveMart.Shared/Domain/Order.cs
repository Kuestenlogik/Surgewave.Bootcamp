namespace SurgewaveMart.Shared.Domain;

/// <summary>
/// Represents a customer order in the system.
/// </summary>
public record Order(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal Total,
    OrderStatus Status,
    DateTime CreatedAt
);

/// <summary>
/// Represents a single item within an order.
/// </summary>
public record OrderItem(
    string ProductId,
    int Quantity,
    decimal UnitPrice
);

/// <summary>
/// Order lifecycle status values.
/// </summary>
public enum OrderStatus
{
    Pending,
    Confirmed,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}
