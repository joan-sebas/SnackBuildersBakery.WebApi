using FluentAssertions;

namespace Domain.UnitTests.Entities;

public sealed class MenuItemTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_ShouldCreateActiveMenuItem()
    {
        var id = Guid.NewGuid();
        var price = new Money(4.50m, "USD");

        var menuItem = new MenuItem(id, "Chocolate cookie", SnackType.Cookie, price);

        menuItem.Id.Should().Be(id);
        menuItem.Name.Should().Be("Chocolate cookie");
        menuItem.SnackType.Should().Be(SnackType.Cookie);
        menuItem.Price.Should().Be(price);
        menuItem.IsRemoved.Should().BeFalse();
    }

    [Fact]
    public void Rename_WhenNameIsValid_ShouldUpdateName()
    {
        var menuItem = CreateMenuItem();

        menuItem.Rename("Butter pastry");

        menuItem.Name.Should().Be("Butter pastry");
    }

    [Fact]
    public void Reprice_WhenPriceIsValid_ShouldUpdatePrice()
    {
        var menuItem = CreateMenuItem();
        var newPrice = new Money(6.25m, "USD");

        menuItem.Reprice(newPrice);

        menuItem.Price.Should().Be(newPrice);
    }

    [Fact]
    public void Remove_WhenCalledRepeatedly_ShouldRemainRemoved()
    {
        var menuItem = CreateMenuItem();

        menuItem.Remove();
        menuItem.Remove();

        menuItem.IsRemoved.Should().BeTrue();
    }

    [Fact]
    public void Rename_WhenNameIsBlank_ShouldThrow()
    {
        var menuItem = CreateMenuItem();

        var act = () => menuItem.Rename(" ");

        act.Should().Throw<ArgumentException>();
    }

    private static MenuItem CreateMenuItem()
    {
        return new MenuItem(Guid.NewGuid(), "Seed bread", SnackType.Bread, new Money(5.00m, "USD"));
    }
}
