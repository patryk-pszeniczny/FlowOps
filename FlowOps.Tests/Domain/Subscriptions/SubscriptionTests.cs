using FlowOps.Domain.Subscriptions;
using FluentAssertions;
using Xunit;

namespace FlowOps.Tests.Domain.Subscriptions;

public class SubscriptionTests
{
    [Fact]
    public void Activate_sets_state_and_returns_event()
    {
        var now = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var subscription = Subscription.Create(Guid.NewGuid(), "basic");

        var ev = subscription.Activate(now);

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.ActivatedAt.Should().Be(now);
        subscription.ExpiresAt.Should().BeAfter(now);
        ev.Should().NotBeNull();
        ev.SubscriptionId.Should().Be(subscription.Id);
    }

    [Fact]
    public void Cancel_requires_active_state()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), "basic");
        var now = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        subscription.Activate(now);

        subscription.Invoking(s => s.Cancel(now)).Should().NotThrow();
        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
    }

    [Fact]
    public void Resume_only_after_suspend()
    {
        var now = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var subscription = Subscription.Create(Guid.NewGuid(), "basic");
        subscription.Activate(now);
        subscription.Suspend(now.AddHours(1));

        subscription.Invoking(s => s.Resume(now.AddHours(2))).Should().NotThrow();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }
}