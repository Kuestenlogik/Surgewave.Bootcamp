namespace SurgewaveMart.Shared.Events;

public record InventoryReserved(
    string EventType,
    int Version,
    string ReservationId,
    string OrderId,
    List<ReservationItem> Items,
    DateTime ExpiresAt
)
{
    public InventoryReserved(string reservationId, string orderId, List<ReservationItem> items, DateTime expiresAt)
        : this("InventoryReserved", 1, reservationId, orderId, items, expiresAt) { }
}

public record InventoryReleased(
    string EventType,
    int Version,
    string ReservationId,
    string OrderId,
    string Reason,
    DateTime ReleasedAt
)
{
    public InventoryReleased(string reservationId, string orderId, string reason, DateTime releasedAt)
        : this("InventoryReleased", 1, reservationId, orderId, reason, releasedAt) { }
}

public record InventoryCommitted(
    string EventType,
    int Version,
    string ReservationId,
    string OrderId,
    DateTime CommittedAt
)
{
    public InventoryCommitted(string reservationId, string orderId, DateTime committedAt)
        : this("InventoryCommitted", 1, reservationId, orderId, committedAt) { }
}

public record LowStockAlert(
    string EventType,
    int Version,
    string ProductId,
    string ProductName,
    int CurrentQuantity,
    int Threshold,
    DateTime AlertedAt
)
{
    public LowStockAlert(string productId, string productName, int currentQuantity, int threshold, DateTime alertedAt)
        : this("LowStockAlert", 1, productId, productName, currentQuantity, threshold, alertedAt) { }
}

public record ReservationItem(
    string ProductId,
    int Quantity
);
