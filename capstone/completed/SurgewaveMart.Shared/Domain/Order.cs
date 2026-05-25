namespace SurgewaveMart.Shared.Domain;

public record Order(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal Total,
    OrderStatus Status,
    DateTime CreatedAt
);

public record OrderItem(
    string ProductId,
    int Quantity,
    decimal UnitPrice
);

public enum OrderStatus
{
    Pending,
    Confirmed,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}
