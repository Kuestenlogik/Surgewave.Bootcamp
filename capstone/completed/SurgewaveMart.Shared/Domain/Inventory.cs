namespace SurgewaveMart.Shared.Domain;

public record InventoryItem(
    string ProductId,
    string ProductName,
    int QuantityAvailable,
    int QuantityReserved,
    decimal Price
);

public record StockReservation(
    string ReservationId,
    string OrderId,
    string ProductId,
    int Quantity,
    DateTime ExpiresAt
);
