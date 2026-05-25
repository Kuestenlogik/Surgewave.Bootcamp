namespace SurgewaveMart.Shared;

/// <summary>
/// Topic name constants for SurgewaveMart.
/// </summary>
public static class Topics
{
    /// <summary>New orders (key: orderId, partitions: 6)</summary>
    public const string Orders = "orders";

    /// <summary>Validated/confirmed orders (key: orderId, partitions: 3)</summary>
    public const string OrdersConfirmed = "orders-confirmed";

    /// <summary>Payment events (key: orderId, partitions: 3)</summary>
    public const string Payments = "payments";

    /// <summary>Inventory/stock changes (key: productId, partitions: 6)</summary>
    public const string InventoryUpdates = "inventory-updates";

    /// <summary>Shipping events (key: orderId, partitions: 3)</summary>
    public const string Shipments = "shipments";

    /// <summary>Aggregated analytics metrics (partitions: 1)</summary>
    public const string Analytics = "analytics";

    /// <summary>Dead letter queue for failed messages (key: original-topic, partitions: 1)</summary>
    public const string DeadLetterQueue = "dlq";
}
