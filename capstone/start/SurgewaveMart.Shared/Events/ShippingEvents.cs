using SurgewaveMart.Shared.Domain;

namespace SurgewaveMart.Shared.Events;

/// <summary>
/// Event raised when a shipment is created.
/// </summary>
public record ShipmentCreated(
    string EventType,
    int Version,
    string ShipmentId,
    string OrderId,
    string TrackingNumber,
    Address DeliveryAddress,
    DateTime EstimatedDelivery,
    DateTime CreatedAt
)
{
    public ShipmentCreated(string shipmentId, string orderId, string trackingNumber, Address address, DateTime estimated, DateTime createdAt)
        : this("ShipmentCreated", 1, shipmentId, orderId, trackingNumber, address, estimated, createdAt) { }
}

/// <summary>
/// Event raised when shipment status is updated.
/// </summary>
public record ShipmentStatusUpdated(
    string EventType,
    int Version,
    string ShipmentId,
    string OrderId,
    ShipmentStatus PreviousStatus,
    ShipmentStatus NewStatus,
    string? Location,
    DateTime UpdatedAt
)
{
    public ShipmentStatusUpdated(string shipmentId, string orderId, ShipmentStatus previous, ShipmentStatus newStatus, string? location, DateTime updatedAt)
        : this("ShipmentStatusUpdated", 1, shipmentId, orderId, previous, newStatus, location, updatedAt) { }
}

/// <summary>
/// Event raised when shipment is delivered.
/// </summary>
public record ShipmentDelivered(
    string EventType,
    int Version,
    string ShipmentId,
    string OrderId,
    DateTime DeliveredAt,
    string? SignedBy
)
{
    public ShipmentDelivered(string shipmentId, string orderId, DateTime deliveredAt, string? signedBy)
        : this("ShipmentDelivered", 1, shipmentId, orderId, deliveredAt, signedBy) { }
}
