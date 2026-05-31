namespace Domain;

public sealed class Oven
{
    private readonly List<OvenSlot> _slots;

    public Oven(Guid id, int slotCount, DateTimeOffset initialAvailableAt)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Oven id cannot be empty.", nameof(id)) : id;

        if (slotCount <= 0)
        {
            throw new InvalidCapacityError(slotCount);
        }

        _slots = new List<OvenSlot>(slotCount);

        for (var index = 0; index < slotCount; index++)
        {
            _slots.Add(new OvenSlot(Guid.NewGuid(), initialAvailableAt));
        }
    }

    public Guid Id { get; }

    public IReadOnlyList<OvenSlot> Slots => _slots;
}
