namespace Application;

public sealed record PayOrderResult(bool IsSuccess, string? FailureReason = null)
{
    public static PayOrderResult Success() => new(true);
    public static PayOrderResult Failure(string reason) => new(false, reason);
}
