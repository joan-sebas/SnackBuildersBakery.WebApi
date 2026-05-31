using Domain;

namespace Application;

public sealed record PayOrderRequest(
    Guid OrderId,
    PaymentMethod PaymentMethod,
    Money Amount,
    Guid IdempotencyKey);
