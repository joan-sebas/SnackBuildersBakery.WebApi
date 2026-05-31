using Domain;
using FluentAssertions;

namespace Infrastructure.IntegrationTests;

[Collection("Database")]
public sealed class OrderRepositoryTests(DatabaseFixture db)
{
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsOrderWithItems()
    {
        await using var ctx = db.CreateContext();
        var repo = new OrderRepository(ctx);

        var menuItem = new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(3.50m, "USD"));
        var order = OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(), Guid.NewGuid(), PriorityLevel.WalkIn,
            [new OrderFactoryRequestedItem(menuItem.Id, 1)],
            [menuItem], DateTimeOffset.UtcNow).Order;

        await repo.AddAsync(order);

        await using var ctx2 = db.CreateContext();
        var repo2 = new OrderRepository(ctx2);
        var loaded = await repo2.GetByIdAsync(order.Id);

        loaded.Should().NotBeNull();
        loaded!.Items.Should().HaveCount(1);
        loaded.Items[0].SnackType.Should().Be(SnackType.Cookie);
    }

    [Fact]
    public async Task GetQueuedAndBakingItemsAsync_ReturnsOnlyActiveItems()
    {
        await using var ctx = db.CreateContext();
        var repo = new OrderRepository(ctx);

        var menuItem = new MenuItem(Guid.NewGuid(), "Pastry", SnackType.Pastry, new Money(4.50m, "USD"));
        var order = OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(), Guid.NewGuid(), PriorityLevel.WalkIn,
            [new OrderFactoryRequestedItem(menuItem.Id, 1)],
            [menuItem], DateTimeOffset.UtcNow).Order;

        await repo.AddAsync(order);

        var items = await repo.GetQueuedAndBakingItemsAsync();

        items.Should().Contain(i => i.Id == order.Items[0].Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        await using var ctx = db.CreateContext();
        var repo = new OrderRepository(ctx);

        var menuItem = new MenuItem(Guid.NewGuid(), "Bread", SnackType.Bread, new Money(6.00m, "USD"));
        var result = OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(), Guid.NewGuid(), PriorityLevel.Vip,
            [new OrderFactoryRequestedItem(menuItem.Id, 1)],
            [menuItem], DateTimeOffset.UtcNow);
        var order = result.Order;
        await repo.AddAsync(order);

        order.Pay(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await repo.UpdateAsync(order);

        await using var ctx2 = db.CreateContext();
        var loaded = await new OrderRepository(ctx2).GetByIdAsync(order.Id);
        loaded!.Status.Should().Be(OrderStatus.Paid);
    }
}
