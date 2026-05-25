namespace SurgewaveMart.Shared.Domain;

public record Payment(
    string PaymentId,
    string OrderId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime ProcessedAt
);

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    BankTransfer,
    Wallet
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded
}
