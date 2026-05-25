namespace SurgewaveMart.Shared.Events;

/// <summary>
/// Event raised when inventory is reserved for an order.
/// </summary>
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

/// <summary>
/// Event raised when reserved inventory is released (cancelled/timeout).
/// </summary>
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

/// <summary>
/// Event raised when inventory reservation is committed (payment successful).
/// </summary>
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

/// <summary>
/// Event raised when stock level becomes low.
/// </summary>
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

/// <summary>
/// Item representation for reservation events.
/// </summary>
public record ReservationItem(
    string ProductId,
    int Quantity
);
