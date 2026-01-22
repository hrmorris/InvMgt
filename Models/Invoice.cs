using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        // Link to Purchase Order (for supplier invoices)
        public int? PurchaseOrderId { get; set; }

        // Link to Supplier (for supplier invoices)
        public int? SupplierId { get; set; }

        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? CustomerAddress { get; set; }

        [StringLength(100)]
        public string? CustomerEmail { get; set; }

        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal SubTotal { get; set; }

        // GST Settings
        public bool GSTEnabled { get; set; } = true; // Toggle GST on/off

        [Range(0, 100)]
        public decimal GSTRate { get; set; } = 10; // Default 10% GST, can be customized per invoice

        public decimal GSTAmount { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        public decimal BalanceAmount => TotalAmount - PaidAmount;

        [StringLength(50)]
        public string Status { get; set; } = "Unpaid"; // Unpaid, Partial, Paid, Overdue

        [StringLength(50)]
        public string InvoiceType { get; set; } = "Customer"; // Customer (AR), Supplier (AP)

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Link to Customer (for customer invoices)
        public int? CustomerId { get; set; }

        // Navigation properties
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();
        public virtual ICollection<ImportedDocument> ImportedDocuments { get; set; } = new List<ImportedDocument>();

        // Computed property for allocated payment amount
        public decimal AllocatedPaymentAmount => PaymentAllocations?.Sum(pa => pa.AllocatedAmount) ?? 0;
    }

    public class InvoiceItem
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;

        // Navigation property
        public virtual Invoice? Invoice { get; set; }
    }
}

