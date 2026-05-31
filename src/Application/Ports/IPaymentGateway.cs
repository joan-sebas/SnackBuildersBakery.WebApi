using Domain;

namespace Application;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessAsync(
        Money amount,
        PaymentMethod method,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);
}
