using System.ComponentModel.DataAnnotations;

namespace FlowOps.Contracts.Request
{
    public sealed class PayInvoiceRequest
    {
        [Required]
        public Guid InvoiceId { get; init; }

        [Required]
        public Guid CustomerId { get; init; }

        [Required]
        public Guid SubscriptionId { get; init; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be > 0")]
        public decimal Amount { get; init; }

        [Required]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be 3 uppercase letters, e.g. PLN, USD")]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; init; } = "PLN";

        [StringLength(32)]
        public string? PaymentMethod { get; init; }

        [StringLength(64)]
        public string? TransactionId { get; init; }
    }
}
