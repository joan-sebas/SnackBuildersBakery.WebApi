using Domain;

namespace Api.Contracts;

public sealed record PayOrderBody(PaymentMethod Method, decimal Amount, string Currency);

public sealed record PayOrderResponse(bool IsSuccess, string? FailureReason);
