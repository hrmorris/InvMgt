using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    /// <summary>
    /// Represents an allocation of a payment amount to a specific invoice.
    /// Supports partial payments and splitting one payment across multiple invoices.
    /// </summary>
    public class PaymentAllocation
    {
        public int Id { get; set; }

        public int PaymentId { get; set; }
        
        public int InvoiceId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Allocated amount must be greater than zero")]
        public decimal AllocatedAmount { get; set; }

        public DateTime AllocationDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual Payment Payment { get; set; } = null!;
        public virtual Invoice Invoice { get; set; } = null!;
    }
}

