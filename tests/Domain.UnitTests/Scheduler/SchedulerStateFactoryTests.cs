using FluentAssertions;

namespace Domain.UnitTests.Scheduler;

public sealed class SchedulerStateFactoryTests
{
    [Fact]
    public void CreateInitial_WhenLayoutProvided_ShouldCreateFreeSlotForEveryTray()
    {
        var initialAvailableAt = new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);
        var settings = CreateSettings(ovensCount: 2, traysPerOven: 3);

        var state = SchedulerStateFactory.CreateInitial(settings, initialAvailableAt);

        state.Items.Should().BeEmpty();
        state.Slots.Should().HaveCount(6);
        state.Slots.Should().OnlyContain(slot =>
            slot.Status == OvenSlotStatus.Free
            && slot.AvailableAt == initialAvailableAt
            && slot.OrderItemId == null
            && slot.BakingEndsAt == null);
    }

    [Fact]
    public void CreateInitial_WhenLayoutProvided_ShouldGroupTraysUnderDistinctOvens()
    {
        var settings = CreateSettings(ovensCount: 2, traysPerOven: 3);

        var state = SchedulerStateFactory.CreateInitial(settings, default);

        var ovenGroups = state.Slots.GroupBy(slot => slot.OvenId).ToList();
        ovenGroups.Should().HaveCount(2);
        ovenGroups.Should().OnlyContain(group => group.Count() == 3);
        state.Slots.Select(slot => slot.SlotId).Distinct().Should().HaveCount(6);
    }

    [Fact]
    public void CreateInitial_WhenOvensCountIsNotPositive_ShouldThrowInvalidCapacityError()
    {
        var settings = CreateSettings(ovensCount: 0, traysPerOven: 3);

        var act = () => SchedulerStateFactory.CreateInitial(settings, default);

        act.Should().Throw<InvalidCapacityError>();
    }

    [Fact]
    public void CreateInitial_WhenTraysPerOvenIsNotPositive_ShouldThrowInvalidCapacityError()
    {
        var settings = CreateSettings(ovensCount: 2, traysPerOven: 0);

        var act = () => SchedulerStateFactory.CreateInitial(settings, default);

        act.Should().Throw<InvalidCapacityError>();
    }

    private static SchedulerSettings CreateSettings(int ovensCount, int traysPerOven)
    {
        return new SchedulerSettings(
            new Dictionary<PriorityLevel, int>(),
            AgingFactor: 1m,
            OvensCount: ovensCount,
            TraysPerOven: traysPerOven,
            Turnover: TimeSpan.FromMinutes(2),
            new Dictionary<SnackType, TimeSpan>());
    }
}
