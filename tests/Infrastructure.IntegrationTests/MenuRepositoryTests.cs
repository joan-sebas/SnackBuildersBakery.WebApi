using Domain;
using FluentAssertions;

namespace Infrastructure.IntegrationTests;

[Collection("Database")]
public sealed class MenuRepositoryTests(DatabaseFixture db)
{
    [Fact]
    public async Task AddAsync_ThenListAsync_ReturnsItem()
    {
        await using var ctx = db.CreateContext();
        var repo = new MenuRepository(ctx);

        var item = new MenuItem(Guid.NewGuid(), "Test Cookie", SnackType.Cookie, new Money(3.50m, "USD"));
        await repo.AddAsync(item);

        var list = await repo.ListAsync();

        list.Should().Contain(m => m.Id == item.Id);
    }

    [Fact]
    public async Task ListAsync_ExcludesRemovedItems()
    {
        await using var ctx = db.CreateContext();
        var repo = new MenuRepository(ctx);

        var item = new MenuItem(Guid.NewGuid(), "Removed Pastry", SnackType.Pastry, new Money(4.00m, "USD"));
        await repo.AddAsync(item);

        item.Remove();
        await repo.UpdateAsync(item);

        var list = await repo.ListAsync();

        list.Should().NotContain(m => m.Id == item.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var ctx = db.CreateContext();
        var repo = new MenuRepository(ctx);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsRename()
    {
        await using var ctx = db.CreateContext();
        var repo = new MenuRepository(ctx);

        var item = new MenuItem(Guid.NewGuid(), "Old Name", SnackType.Bread, new Money(5.00m, "USD"));
        await repo.AddAsync(item);

        item.Rename("New Name");
        await repo.UpdateAsync(item);

        await using var ctx2 = db.CreateContext();
        var loaded = await new MenuRepository(ctx2).GetByIdAsync(item.Id);
        loaded!.Name.Should().Be("New Name");
    }
}
