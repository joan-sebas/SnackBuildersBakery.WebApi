using FluentAssertions;

namespace Domain.UnitTests.Entities;

public sealed class OvenSlotTests
{
    [Fact]
    public void StartBaking_ThenTurnover_ShouldKeepSlotUnavailableUntilAvailableAt()
    {
        var slot = new OvenSlot(Guid.NewGuid(), new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero));
        var startedAt = new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero);
        var bakingEndsAt = new DateTimeOffset(2026, 5, 31, 10, 8, 0, TimeSpan.Zero);
        var turnover = TimeSpan.FromMinutes(2);

        slot.StartBaking(startedAt, bakingEndsAt);
        slot.BeginTurnover(turnover);
        slot.ReleaseIfAvailable(new DateTimeOffset(2026, 5, 31, 10, 9, 0, TimeSpan.Zero));

        slot.Status.Should().Be(OvenSlotStatus.Turnover);
        slot.ReleaseIfAvailable(new DateTimeOffset(2026, 5, 31, 10, 10, 0, TimeSpan.Zero));
        slot.Status.Should().Be(OvenSlotStatus.Free);
    }

    [Fact]
    public void BeginTurnover_WhenSlotIsNotBaking_ShouldThrowDomainError()
    {
        var slot = new OvenSlot(Guid.NewGuid(), new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero));

        var act = () => slot.BeginTurnover(TimeSpan.FromMinutes(1));

        act.Should().Throw<InvalidOvenSlotTransitionError>();
    }

    [Fact]
    public void ExtendTurnover_WhenCalled_ShouldMoveAvailableAtForwardOnly()
    {
        var slot = new OvenSlot(Guid.NewGuid(), new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero));
        slot.StartBaking(
            new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 10, 10, 0, TimeSpan.Zero));
        slot.BeginTurnover(TimeSpan.FromMinutes(2));

        var previousAvailableAt = slot.AvailableAt;
        slot.ExtendTurnover(TimeSpan.FromMinutes(3));

        slot.AvailableAt.Should().Be(previousAvailableAt + TimeSpan.FromMinutes(3));

        var act = () => slot.ExtendTurnover(TimeSpan.FromMinutes(-1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Oven_WhenCapacityIsProvided_ShouldCreateSlotsFromInput()
    {
        var oven = new Oven(Guid.NewGuid(), 4, new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero));

        oven.Slots.Should().HaveCount(4);
    }

    [Fact]
    public void Oven_WhenCapacityIsInvalid_ShouldThrowDomainError()
    {
        var act = () => new Oven(Guid.NewGuid(), 0, new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero));

        act.Should().Throw<InvalidCapacityError>();
    }
}
