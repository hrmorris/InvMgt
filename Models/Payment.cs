using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Direct invoice link (legacy support). Use PaymentAllocations for flexible allocation.
        /// </summary>
        public int? InvoiceId { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Check, Credit Card, Bank Transfer

        /// <summary>
        /// Bank statement transaction reference number (unique identifier from bank)
        /// </summary>
        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Purpose/reason for payment (often empty, can be updated after saving)
        /// </summary>
        [StringLength(500)]
        public string? Purpose { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Unallocated"; // Unallocated, Partially Allocated, Fully Allocated

        // Supplier/Customer Reference (for AI Import matching)
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }

        // ═══════════════════════════════════════════════════════════════════
        // BSP BANK STATEMENT FIELDS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Supplier/Payee bank name (e.g., "BSP" for Bank South Pacific)
        /// </summary>
        [StringLength(200)]
        public string? BankName { get; set; }

        /// <summary>
        /// Account Type: "Internal" (BSP account) or "Domestic" (other bank)
        /// </summary>
        [StringLength(50)]
        public string? AccountType { get; set; }

        /// <summary>
        /// Supplier/Payee full account number (Branch + Account: first 3 digits = branch, next 10 = account)
        /// </summary>
        [StringLength(50)]
        public string? BankAccountNumber { get; set; }

        /// <summary>
        /// Supplier/Payee branch number (first 3 digits of account)
        /// </summary>
        [StringLength(10)]
        public string? PayeeBranchNumber { get; set; }

        /// <summary>
        /// Supplier/Payee actual account number (last 10 digits)
        /// </summary>
        [StringLength(20)]
        public string? PayeeAccountNumber { get; set; }

        /// <summary>
        /// Payer (our company) full account number (Branch + Account)
        /// </summary>
        [StringLength(50)]
        public string? PayerAccountNumber { get; set; }

        /// <summary>
        /// Payer (our company) branch number (first 3 digits)
        /// </summary>
        [StringLength(10)]
        public string? PayerBranchNumber { get; set; }

        /// <summary>
        /// Payer (our company) actual bank account number (last 10 digits)
        /// </summary>
        [StringLength(20)]
        public string? PayerBankAccountNumber { get; set; }

        /// <summary>
        /// Payer name (our company name from bank statement header)
        /// </summary>
        [StringLength(200)]
        public string? PayerName { get; set; }

        /// <summary>
        /// Payee/Supplier name (Account Name from bank statement, excluding bank suffix)
        /// </summary>
        [StringLength(200)]
        public string? PayeeName { get; set; }

        /// <summary>
        /// Transfer To field from bank statement (includes bank suffix like "BSP")
        /// </summary>
        [StringLength(200)]
        public string? TransferTo { get; set; }

        /// <summary>
        /// Currency code (e.g., "PGK", "USD", "AUD")
        /// </summary>
        [StringLength(10)]
        public string? Currency { get; set; }

        // Navigation properties
        public virtual Invoice? Invoice { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();
        public virtual ICollection<ImportedDocument> ImportedDocuments { get; set; } = new List<ImportedDocument>();

        // Computed properties
        /// <summary>
        /// Total amount allocated to invoices
        /// </summary>
        public decimal AllocatedAmount => PaymentAllocations?.Sum(pa => pa.AllocatedAmount) ?? 0;

        /// <summary>
        /// Remaining amount not yet allocated to any invoice
        /// </summary>
        public decimal UnallocatedAmount => Amount - AllocatedAmount;

        /// <summary>
        /// Whether the payment is fully allocated
        /// </summary>
        public bool IsFullyAllocated => UnallocatedAmount <= 0;

        /// <summary>
        /// Whether the payment has any allocations
        /// </summary>
        public bool HasAllocations => PaymentAllocations?.Any() ?? false;
    }
}

