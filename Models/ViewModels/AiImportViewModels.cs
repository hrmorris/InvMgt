using InvoiceManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models.ViewModels
{
    public class AiImportInvoiceItemViewModel
    {
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class AiImportInvoiceViewModel
    {
        public int DocumentId { get; set; }
        public string? OriginalFileName { get; set; }

        // Selection for bulk operations
        public bool Selected { get; set; } = true;

        // Invoice fields
        public string InvoiceNumber { get; set; } = "";
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public decimal SubTotal { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; } = "";
        public string InvoiceType { get; set; } = "Payable";
        public List<AiImportInvoiceItemViewModel>? Items { get; set; }

        // Extracted supplier info
        public string? ExtractedSupplierName { get; set; }
        public string? ExtractedBankAccount { get; set; }

        // Extracted customer info  
        public string? ExtractedCustomerName { get; set; }

        // Matched entities
        public int? MatchedSupplierId { get; set; }
        public string? MatchedSupplierName { get; set; }
        public int? MatchedCustomerId { get; set; }
        public string? MatchedCustomerName { get; set; }

        // For dropdown selection
        public int? SelectedSupplierId { get; set; }
        public int? SelectedCustomerId { get; set; }

        // Available entities for dropdowns
        public List<Supplier> AvailableSuppliers { get; set; } = new List<Supplier>();
        public List<Customer> AvailableCustomers { get; set; } = new List<Customer>();

        // Entity creation flags
        public bool CreateNewSupplier { get; set; }
        public bool CreateNewCustomer { get; set; }

        // Processing info
        public string? MatchConfidence { get; set; } // "High", "Low", "None"
    }

    public class AiImportPaymentViewModel
    {
        public int DocumentId { get; set; }
        public string? OriginalFileName { get; set; }

        // ═══════════════════════════════════════════════════════════════════
        // CORE PAYMENT FIELDS
        // ═══════════════════════════════════════════════════════════════════

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? PaymentDate { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PGK";
        public string PaymentMethod { get; set; } = "Bank Transfer";

        /// <summary>
        /// Bank transaction reference number (unique identifier from bank statement)
        /// </summary>
        public string ReferenceNumber { get; set; } = "";

        public string Notes { get; set; } = "";

        /// <summary>
        /// Payment purpose/reason (often empty, can be updated after saving)
        /// </summary>
        public string Purpose { get; set; } = "";

        // ═══════════════════════════════════════════════════════════════════
        // BSP BANK STATEMENT FIELDS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Transfer To field (Supplier name with bank suffix e.g., "Roselaw Limited BSP")
        /// </summary>
        public string TransferTo { get; set; } = "";

        /// <summary>
        /// Account Type: "Internal" (BSP to BSP) or "Domestic" (BSP to other bank)
        /// </summary>
        public string AccountType { get; set; } = "";

        // ─────────────────────────────────────────────────────────────────────
        // PAYEE (SUPPLIER) BANK DETAILS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Payee/Supplier name without bank suffix (Account Name field)
        /// </summary>
        public string ExtractedPayeeName { get; set; } = "";

        /// <summary>
        /// Payee bank name (extracted from Transfer To suffix: BSP, Westpac, ANZ, Kina)
        /// </summary>
        public string ExtractedBankName { get; set; } = "";

        /// <summary>
        /// Full account number (Branch + Account combined)
        /// </summary>
        public string ExtractedBankAccountNumber { get; set; } = "";

        /// <summary>
        /// Payee branch number (first 3 digits of account)
        /// </summary>
        public string PayeeBranchNumber { get; set; } = "";

        /// <summary>
        /// Payee account number (remaining digits after branch)
        /// </summary>
        public string PayeeAccountNumber { get; set; } = "";

        // ─────────────────────────────────────────────────────────────────────
        // PAYER (OUR COMPANY) BANK DETAILS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Payer/Our company name (from bank statement header)
        /// </summary>
        public string ExtractedPayerName { get; set; } = "";

        /// <summary>
        /// Our company's full account number (Transfer From field)
        /// </summary>
        public string PayerAccountFull { get; set; } = "";

        /// <summary>
        /// Our company's branch number (first 3 digits)
        /// </summary>
        public string PayerBranchNumber { get; set; } = "";

        /// <summary>
        /// Our company's actual account number (remaining digits)
        /// </summary>
        public string PayerAccountNumber { get; set; } = "";

        // ═══════════════════════════════════════════════════════════════════
        // INVOICE REFERENCE (Extracted from Note field)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Invoice number extracted from Note field (e.g., "InvNo1956" → "1956")
        /// </summary>
        public string? RelatedInvoiceNumber { get; set; }

        // ═══════════════════════════════════════════════════════════════════
        // ENTITY MATCHING
        // ═══════════════════════════════════════════════════════════════════

        // Matched entities (auto-detected)
        public int? MatchedSupplierId { get; set; }
        public string? MatchedSupplierName { get; set; }
        public int? MatchedCustomerId { get; set; }
        public string? MatchedCustomerName { get; set; }

        // For dropdown selection (user-selected)
        public int? SelectedSupplierId { get; set; }
        public int? SelectedCustomerId { get; set; }
        public int? SelectedInvoiceId { get; set; }

        // Available entities for dropdowns
        public List<Supplier> AvailableSuppliers { get; set; } = new List<Supplier>();
        public List<Customer> AvailableCustomers { get; set; } = new List<Customer>();
        public List<Invoice> AvailableInvoices { get; set; } = new List<Invoice>();

        // Entity creation flags
        public bool CreateNewSupplier { get; set; }
        public bool CreateNewCustomer { get; set; }

        // Processing info
        public string? PaymentDirection { get; set; } // "Incoming" (from customer), "Outgoing" (to supplier), "Unknown"
        public string? MatchConfidence { get; set; } // "High", "Medium", "Low", "None"
        public string? MatchedByField { get; set; } // "Bank Account", "Name", "Account Number"
    }

    public class AiImportBatchViewModel
    {
        public string DocumentType { get; set; } = "Invoice";
        public List<AiImportInvoiceViewModel> ExtractedInvoices { get; set; } = new List<AiImportInvoiceViewModel>();
        public List<AiImportPaymentViewModel> ExtractedPayments { get; set; } = new List<AiImportPaymentViewModel>();

        // Available entities for dropdown selection
        public List<Supplier> AvailableSuppliers { get; set; } = new List<Supplier>();
        public List<Customer> AvailableCustomers { get; set; } = new List<Customer>();

        // Summary
        public int TotalFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> FailedFiles { get; set; } = new List<string>();
    }
}
