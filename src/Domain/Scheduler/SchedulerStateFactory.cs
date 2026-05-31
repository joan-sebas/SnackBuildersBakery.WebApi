namespace Domain;

public static class SchedulerStateFactory
{
    public static SchedulerState CreateInitial(SchedulerSettings settings, DateTimeOffset initialAvailableAt)
    {
        if (settings.OvensCount <= 0)
        {
            throw new InvalidCapacityError(settings.OvensCount);
        }

        if (settings.TraysPerOven <= 0)
        {
            throw new InvalidCapacityError(settings.TraysPerOven);
        }

        var slots = new List<SchedulerSlotState>(settings.Capacity);

        for (var oven = 0; oven < settings.OvensCount; oven++)
        {
            var ovenId = Guid.NewGuid();

            for (var tray = 0; tray < settings.TraysPerOven; tray++)
            {
                slots.Add(new SchedulerSlotState(
                    SlotId: Guid.NewGuid(),
                    OvenId: ovenId,
                    Status: OvenSlotStatus.Free,
                    AvailableAt: initialAvailableAt,
                    OrderItemId: null,
                    BakingEndsAt: null));
            }
        }

        return new SchedulerState(Array.Empty<SchedulerItemState>(), slots);
    }
}
