using FlowOps.Domain.Subscriptions;
using FlowOps.Domain.Subscriptions.Events;
using FluentAssertions;
using Xunit;


namespace FlowOps.Tests.Domain
{
    public class SubscriptionTests
    {
        [Fact]
        public void Activate_sets_state_and_raises_event()
        {
            var subscription = Subscription.Create(Guid.NewGuid(), "basic");

            var now = DateTime.UtcNow;
            subscription.Activate(now);

            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.ActivatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            subscription.ExpiresAt.Should().BeCloseTo(now.AddMonths(1), TimeSpan.FromSeconds(1));
            subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionActivatedDomainEvent);
        }

        [Fact]
        public void Suspend_and_resume_cycle_updates_timestamps()
        {
            var subscription = Subscription.Create(Guid.NewGuid(), "basic");
            var now = DateTime.UtcNow;
            subscription.Activate(now);

            var suspendedAt = now.AddMinutes(1);
            subscription.Suspend(suspendedAt);

            subscription.Status.Should().Be(SubscriptionStatus.Suspended);
            subscription.SuspendedAt.Should().BeCloseTo(suspendedAt, TimeSpan.FromSeconds(1));

            var resumedAt = suspendedAt.AddMinutes(2);
            subscription.Resume(resumedAt);

            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.ResumedAt.Should().BeCloseTo(resumedAt, TimeSpan.FromSeconds(1));
            subscription.SuspendedAt.Should().BeNull();
        }

        [Fact]
        public void Cancel_only_allowed_when_active()
        {
            var subscription = Subscription.Create(Guid.NewGuid(), "basic");
            var now = DateTime.UtcNow;

            Action act = () => subscription.Cancel(now);
            act.Should().Throw<InvalidOperationException>();

            subscription.Activate(now);

            act = () => subscription.Cancel(now);
            act.Should().NotThrow();
            subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
            subscription.CancelledAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        }
    }
}
