namespace SurgewaveMart.Shared.Domain;

/// <summary>
/// Represents a shipment for a completed order.
/// </summary>
public record Shipment(
    string ShipmentId,
    string OrderId,
    string TrackingNumber,
    ShipmentStatus Status,
    Address DeliveryAddress,
    DateTime EstimatedDelivery
);

/// <summary>
/// Delivery address information.
/// </summary>
public record Address(
    string Street,
    string City,
    string PostalCode,
    string Country
);

/// <summary>
/// Shipment tracking status values.
/// </summary>
public enum ShipmentStatus
{
    Created,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered
}
