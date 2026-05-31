namespace Application;

/// <summary>
/// Outcome returned by the payment gateway after a processing attempt.
/// <see cref="GatewayReference"/> is non-null on success and holds the gateway's transaction identifier.
/// <see cref="FailureReason"/> is non-null on failure and carries a gateway-supplied description.
/// </summary>
public sealed record PaymentResult(
    bool IsSuccess,
    string? GatewayReference,
    string? FailureReason)
{
    public static PaymentResult Success(string gatewayReference)
        => new(true, gatewayReference, null);

    public static PaymentResult Failure(string failureReason)
        => new(false, null, failureReason);
}
