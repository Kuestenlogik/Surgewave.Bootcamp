using SurgewaveMart.Shared.Domain;

namespace SurgewaveMart.Shared.Events;

/// <summary>
/// Event raised when payment is successfully completed.
/// </summary>
public record PaymentCompleted(
    string EventType,
    int Version,
    string PaymentId,
    string OrderId,
    decimal Amount,
    PaymentMethod Method,
    string TransactionId,
    DateTime CompletedAt
)
{
    public PaymentCompleted(string paymentId, string orderId, decimal amount, PaymentMethod method, string transactionId, DateTime completedAt)
        : this("PaymentCompleted", 1, paymentId, orderId, amount, method, transactionId, completedAt) { }
}

/// <summary>
/// Event raised when payment fails after all retries.
/// </summary>
public record PaymentFailed(
    string EventType,
    int Version,
    string PaymentId,
    string OrderId,
    decimal Amount,
    string Reason,
    int RetryCount,
    DateTime FailedAt
)
{
    public PaymentFailed(string paymentId, string orderId, decimal amount, string reason, int retryCount, DateTime failedAt)
        : this("PaymentFailed", 1, paymentId, orderId, amount, reason, retryCount, failedAt) { }
}
