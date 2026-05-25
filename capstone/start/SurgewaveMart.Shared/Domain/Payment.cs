namespace SurgewaveMart.Shared.Domain;

/// <summary>
/// Represents a payment transaction.
/// </summary>
public record Payment(
    string PaymentId,
    string OrderId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime ProcessedAt
);

/// <summary>
/// Supported payment methods.
/// </summary>
public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    BankTransfer,
    Wallet
}

/// <summary>
/// Payment processing status values.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded
}
