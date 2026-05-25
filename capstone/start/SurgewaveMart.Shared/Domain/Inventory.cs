namespace SurgewaveMart.Shared.Domain;

/// <summary>
/// Represents an inventory item with stock tracking.
/// </summary>
public record InventoryItem(
    string ProductId,
    string ProductName,
    int QuantityAvailable,
    int QuantityReserved,
    decimal Price
);

/// <summary>
/// Represents a temporary stock reservation for an order.
/// </summary>
public record StockReservation(
    string ReservationId,
    string OrderId,
    string ProductId,
    int Quantity,
    DateTime ExpiresAt
);
