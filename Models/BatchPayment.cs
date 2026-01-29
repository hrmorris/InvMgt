using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    /// <summary>
    /// Represents a batch of invoices prepared for payment processing
    /// </summary>
    public class BatchPayment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Batch Reference")]
        public string BatchReference { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Batch Name")]
        public string? BatchName { get; set; }

        [Required]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Scheduled Payment Date")]
        public DateTime? ScheduledPaymentDate { get; set; }

        [Display(Name = "Processed Date")]
        public DateTime? ProcessedDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Ready, Processing, Completed, Cancelled

        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; } // Bank Transfer, Cheque, Cash, etc.

        [StringLength(100)]
        [Display(Name = "Bank Account")]
        public string? BankAccount { get; set; } // Source bank account for payment

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(100)]
        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        [StringLength(100)]
        [Display(Name = "Approved By")]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Approved Date")]
        public DateTime? ApprovedDate { get; set; }

        // Computed properties
        [Display(Name = "Total Amount")]
        public decimal TotalAmount => BatchItems?.Sum(bi => bi.AmountToPay) ?? 0;

        [Display(Name = "Invoice Count")]
        public int InvoiceCount => BatchItems?.Count ?? 0;

        [Display(Name = "Supplier Count")]
        public int SupplierCount => BatchItems?.Select(bi => bi.Invoice?.SupplierId ?? bi.Invoice?.CustomerId ?? 0).Distinct().Count() ?? 0;

        // Navigation properties
        public virtual ICollection<BatchPaymentItem> BatchItems { get; set; } = new List<BatchPaymentItem>();
    }

    /// <summary>
    /// Represents an invoice included in a batch payment
    /// </summary>
    public class BatchPaymentItem
    {
        public int Id { get; set; }

        [Required]
        public int BatchPaymentId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        /// <summary>
        /// Amount to pay for this invoice (can be partial payment)
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Amount to Pay")]
        public decimal AmountToPay { get; set; }

        /// <summary>
        /// Whether this item has been processed/paid
        /// </summary>
        public bool IsProcessed { get; set; } = false;

        /// <summary>
        /// Reference to the payment record created when processed
        /// </summary>
        public int? PaymentId { get; set; }

        [Display(Name = "Added Date")]
        public DateTime AddedDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual BatchPayment? BatchPayment { get; set; }
        public virtual Invoice? Invoice { get; set; }
        public virtual Payment? Payment { get; set; }
    }
}
