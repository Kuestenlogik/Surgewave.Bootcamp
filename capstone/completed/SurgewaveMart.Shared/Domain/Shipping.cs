namespace SurgewaveMart.Shared.Domain;

public record Shipment(
    string ShipmentId,
    string OrderId,
    string TrackingNumber,
    ShipmentStatus Status,
    Address DeliveryAddress,
    DateTime EstimatedDelivery
);

public record Address(
    string Street,
    string City,
    string PostalCode,
    string Country
);

public enum ShipmentStatus
{
    Created,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered
}
