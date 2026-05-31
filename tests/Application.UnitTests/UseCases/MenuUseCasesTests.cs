using Domain;
using FluentAssertions;

namespace Application.UnitTests.UseCases;

public sealed class MenuUseCasesTests
{
    private static readonly Guid ItemId = new("bbbbbbbb-0000-0000-0000-000000000001");

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsArgumentException()
    {
        var sut = new CreateMenuItemUseCase(new FakeMenuRepository());

        await sut.Invoking(s => s.ExecuteAsync(new CreateMenuItemRequest("", SnackType.Cookie, new Money(3.50m, "USD"))))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_ValidInput_SavesAndReturnsMenuItem()
    {
        var repo = new FakeMenuRepository();
        var sut = new CreateMenuItemUseCase(repo);

        var item = await sut.ExecuteAsync(new CreateMenuItemRequest("Croissant", SnackType.Pastry, new Money(4.00m, "USD")));

        item.Name.Should().Be("Croissant");
        item.Price.Amount.Should().Be(4.00m);
        repo.All.Should().ContainSingle(i => i.Id == item.Id);
    }

    // ── Get ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsItem()
    {
        var existing = SampleItem();
        var sut = new GetMenuItemUseCase(new FakeMenuRepository(existing));

        var result = await sut.ExecuteAsync(ItemId);

        result.Should().Be(existing);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        var sut = new GetMenuItemUseCase(new FakeMenuRepository());

        var result = await sut.ExecuteAsync(ItemId);

        result.Should().BeNull();
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NewName_RenamesItem()
    {
        var item = SampleItem();
        var sut = new UpdateMenuItemUseCase(new FakeMenuRepository(item));

        await sut.ExecuteAsync(new UpdateMenuItemRequest(ItemId, NewName: "Almond Croissant", NewPrice: null));

        item.Name.Should().Be("Almond Croissant");
    }

    [Fact]
    public async Task UpdateAsync_NewPrice_RepricesItem()
    {
        var item = SampleItem();
        var sut = new UpdateMenuItemUseCase(new FakeMenuRepository(item));

        await sut.ExecuteAsync(new UpdateMenuItemRequest(ItemId, NewName: null, NewPrice: new Money(5.00m, "USD")));

        item.Price.Amount.Should().Be(5.00m);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var sut = new UpdateMenuItemUseCase(new FakeMenuRepository());

        await sut.Invoking(s => s.ExecuteAsync(new UpdateMenuItemRequest(ItemId, "New Name", null)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Remove ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_ExistingItem_SetsIsRemovedTrue()
    {
        var item = SampleItem();
        var sut = new RemoveMenuItemUseCase(new FakeMenuRepository(item));

        await sut.ExecuteAsync(ItemId);

        item.IsRemoved.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var sut = new RemoveMenuItemUseCase(new FakeMenuRepository());

        await sut.Invoking(s => s.ExecuteAsync(ItemId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    // ── List ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_ReturnsOnlyActiveItems()
    {
        var active = SampleItem();
        var removed = new MenuItem(new Guid("cccccccc-0000-0000-0000-000000000001"), "Old Bread", SnackType.Bread, new Money(2.00m, "USD"));
        removed.Remove();
        var sut = new ListMenuItemsUseCase(new FakeMenuRepository(active, removed));

        var result = await sut.ExecuteAsync();

        result.Should().ContainSingle().Which.Should().Be(active);
    }

    private static MenuItem SampleItem() =>
        new(ItemId, "Chocolate Chip Cookie", SnackType.Cookie, new Money(3.50m, "USD"));

    private sealed class FakeMenuRepository(params MenuItem[] items) : IMenuRepository
    {
        public List<MenuItem> All { get; } = [.. items];

        public Task<IReadOnlyList<MenuItem>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MenuItem>>(All);

        public Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(All.FirstOrDefault(i => i.Id == id));

        public Task AddAsync(MenuItem item, CancellationToken ct = default)
        {
            All.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MenuItem item, CancellationToken ct = default) => Task.CompletedTask;
    }
}
