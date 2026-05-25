using SurgewaveMart.Shared.Domain;

namespace SurgewaveMart.Shared.Events;

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
