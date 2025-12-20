using FlowOps.Domain.Customers.Events;
using FlowOps.Domain.Events;

namespace FlowOps.Domain.Customers
{
    public sealed class Customer : AggregateRoot
    {
        private Customer()
        {
        }

        private Customer(Guid id, string name, string? taxId, string? email, DateTime createdAt)
        {
            Id = id;
            Name = name;
            TaxId = taxId;
            Email = email;
            CreatedAt = createdAt;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? TaxId { get; private set; }
        public string? Email { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public static Customer Create(string name, string? taxId, string? email, DateTime createdAtUtc)
        {
            var trimmedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                throw new ArgumentException("Customer name is required.", nameof(name));
            }

            var trimmedTaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId!.Trim();
            var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email!.Trim();

            var customer = new Customer(Guid.NewGuid(), trimmedName, trimmedTaxId, trimmedEmail, createdAtUtc);
            customer.AddDomainEvent(new CustomerCreatedDomainEvent(
                customer.Id,
                customer.Name,
                customer.TaxId,
                customer.Email,
                customer.CreatedAt));
            return customer;
        }

        public static Customer FromExisting(Guid id, string name, string? taxId, string? email, DateTime createdAtUtc)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Customer ID is required.", nameof(id));
            }
            var trimmedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                throw new ArgumentException("Customer name is required.", nameof(name));
            }

            return new Customer(id, trimmedName, string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim(), string.IsNullOrWhiteSpace(email) ? null : email.Trim(), createdAtUtc);
        }
    }
}