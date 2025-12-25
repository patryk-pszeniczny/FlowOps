using FlowOps.Domain.Events;
using FlowOps.Domain.Subscriptions.Events;
using FlowOps.Events;

namespace FlowOps.Domain.Subscriptions
{
    public sealed class Subscription : AggregateRoot
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public string PlanCode { get; private set; } = string.Empty;

        public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.None;
        public DateTime? ActivatedAt { get; private set; }
        public DateTime? ExpiresAt { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public DateTime? SuspendedAt { get; private set; }
        public DateTime? ResumedAt { get; private set; }

        private Subscription() { }

        public Subscription(Guid id, Guid customerId, string planCode)
        {
            Id = id;
            CustomerId = customerId;
            PlanCode = planCode;
            Status = SubscriptionStatus.None;
        }
        public static Subscription Create(Guid customerId, string planCode)
        {
            if(customerId == Guid.Empty)
                throw new ArgumentException("CustomerId cannot be empty.", nameof(customerId));
            if(string.IsNullOrWhiteSpace(planCode))
                throw new ArgumentException("PlanCode cannot be null or whitespace.", nameof(planCode));
            return new Subscription(Guid.NewGuid(), customerId, planCode.Trim().ToUpperInvariant());
        }
        public void Activate(DateTime utcNow)
        {
            if(Status == SubscriptionStatus.Active)
                throw new InvalidOperationException("Subscription is already active.");
            if(Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
                throw new InvalidOperationException("Cannot activate a canceled or expired subscription.");

            Status = SubscriptionStatus.Active;
            ActivatedAt = utcNow;

            SuspendedAt = null;
            ResumedAt = null;
            CancelledAt = null;
            ExpiresAt = utcNow.AddMonths(1);

            AddDomainEvent(new SubscriptionActivatedDomainEvent(Id, CustomerId, PlanCode));
        }
        public void Cancel(DateTime utcNow)
        {
            if(Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Only active subscriptions can be canceled.");

            Status = SubscriptionStatus.Cancelled;
            CancelledAt = utcNow;

            AddDomainEvent(new SubscriptionCancelledDomainEvent(Id, CustomerId, PlanCode));
        }
        public void Expire(DateTime utcNow)
        {
            if(Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Only active subscriptions can expire.");
           
            if(ExpiresAt.HasValue && utcNow < ExpiresAt.Value)
                throw new InvalidOperationException("Subscription cannot expire before its expiration date.");

            Status = SubscriptionStatus.Expired;
        }

        public bool IsActiveAt(DateTime utcNow)
            => Status == SubscriptionStatus.Active && (!ExpiresAt.HasValue || ExpiresAt.Value > utcNow);
        public void Suspend(DateTime utcNow)
        {
            if(Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Only active subscriptions can be suspended.");
            Status = SubscriptionStatus.Suspended;
            SuspendedAt = utcNow;

            AddDomainEvent(new SubscriptionSuspendedDomainEvent(Id, CustomerId, PlanCode));
        }
        public void Resume(DateTime utcNow)
        {
            if(Status != SubscriptionStatus.Suspended)
                throw new InvalidOperationException("Only suspended subscriptions can be resumed.");
            Status = SubscriptionStatus.Active;
            ResumedAt = utcNow;
            SuspendedAt = null;

            AddDomainEvent(new SubscriptionResumedDomainEvent(Id, CustomerId, PlanCode));
        }

    }
}
