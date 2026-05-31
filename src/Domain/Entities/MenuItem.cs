namespace Domain;

public sealed class MenuItem
{
    public MenuItem(Guid id, string name, SnackType snackType, Money price)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Menu item id cannot be empty.", nameof(id)) : id;
        Name = NormalizeName(name);
        SnackType = snackType;
        Price = price;
    }

    // Parameterless constructor required for EF Core materialization.
    private MenuItem() { }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public SnackType SnackType { get; private set; }

    public Money Price { get; private set; }

    public bool IsRemoved { get; private set; }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void Reprice(Money price)
    {
        Price = price;
    }

    public void Remove()
    {
        IsRemoved = true;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Menu item name is required.", nameof(name));
        }

        return name.Trim();
    }
}
