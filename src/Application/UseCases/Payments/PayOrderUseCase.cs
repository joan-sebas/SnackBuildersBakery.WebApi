using Domain;

namespace Application;

/// <summary>
/// An already-paid order throws <see cref="OrderAlreadyPaidError"/> before the gateway
/// is called, preventing a double-charge. Endpoint and gateway idempotency handle
/// duplicate payment attempts before re-executing side effects.
/// </summary>
public sealed class PayOrderUseCase(
    IOrderRepository orders,
    IPaymentGateway gateway,
    ISchedulerCoordinator scheduler,
    IDomainEventDispatcher dispatcher,
    TimeProvider timeProvider)
{
    public async Task<PayOrderResult> ExecuteAsync(
        PayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order not found. OrderId: {request.OrderId}");

        if (order.Status != OrderStatus.AwaitingPayment)
        {
            throw new OrderAlreadyPaidError(order.Id);
        }

        var paymentResult = await gateway.ProcessAsync(
            request.Amount,
            request.PaymentMethod,
            request.IdempotencyKey,
            cancellationToken);

        if (!paymentResult.IsSuccess)
        {
            return PayOrderResult.Failure(paymentResult.FailureReason!);
        }

        var now = timeProvider.GetUtcNow();
        order.Pay(Guid.NewGuid(), now);

        foreach (var item in order.Items)
        {
            await scheduler.EnqueueAsync(
                new EnqueuedItem(item.Id, item.PriorityLevel, item.SnackType, item.UnitPrice),
                cancellationToken);
        }

        await orders.UpdateAsync(order, cancellationToken);

        await dispatcher.DispatchAllAsync(order.DomainEvents, cancellationToken);
        order.ClearDomainEvents();

        return PayOrderResult.Success();
    }
}
