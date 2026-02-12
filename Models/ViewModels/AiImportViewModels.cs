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

    /// <summary>
    /// ViewModel for the Multi-Page PDF Import feature.
    /// Supports PDFs with up to 100 pages containing multiple invoices,
    /// with intelligent page-boundary detection and individual document storage.
    /// </summary>
    public class MultiPagePdfViewModel
    {
        /// <summary>Master document ID (the uploaded multi-page PDF)</summary>
        public int MasterDocumentId { get; set; }

        /// <summary>Original file name of the uploaded PDF</summary>
        public string OriginalFileName { get; set; } = "";

        /// <summary>Total number of pages detected in the PDF</summary>
        public int TotalPages { get; set; }

        /// <summary>Total number of individual invoices detected</summary>
        public int TotalInvoicesDetected { get; set; }

        /// <summary>Number of invoices successfully extracted</summary>
        public int SuccessfullyExtracted { get; set; }

        /// <summary>Number of extraction failures</summary>
        public int FailedExtractions { get; set; }

        /// <summary>Processing time in seconds</summary>
        public double ProcessingTimeSeconds { get; set; }

        /// <summary>Processing summary message</summary>
        public string ProcessingSummary { get; set; } = "";

        /// <summary>Warnings generated during processing</summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>Errors generated during processing</summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>List of split invoice entries with page-level metadata</summary>
        public List<SplitInvoiceEntry> SplitInvoices { get; set; } = new();

        /// <summary>Available suppliers for entity matching dropdowns</summary>
        public List<Supplier> AvailableSuppliers { get; set; } = new();

        /// <summary>Available customers for entity matching dropdowns</summary>
        public List<Customer> AvailableCustomers { get; set; } = new();

        /// <summary>Number of duplicate invoice numbers found in the database</summary>
        public int DuplicateCount { get; set; }

        /// <summary>List of duplicate invoice numbers (first 10)</summary>
        public List<string> DuplicateNumbers { get; set; } = new();
    }

    /// <summary>
    /// Represents a single invoice split from a multi-page PDF.
    /// Each entry tracks its source page range and has its own stored document.
    /// </summary>
    public class SplitInvoiceEntry
    {
        /// <summary>Sequential index within the split (1-based)</summary>
        public int Index { get; set; }

        /// <summary>Whether this invoice is selected for saving</summary>
        public bool Selected { get; set; } = true;

        /// <summary>The document ID for this individual split page/document (stored separately)</summary>
        public int? SplitDocumentId { get; set; }

        /// <summary>Page range in the source PDF (e.g., "1-2", "3")</summary>
        public string PageRange { get; set; } = "";

        /// <summary>Starting page in the source PDF (1-based)</summary>
        public int StartPage { get; set; }

        /// <summary>Ending page in the source PDF (1-based)</summary>
        public int EndPage { get; set; }

        /// <summary>AI confidence for this extraction</summary>
        public string Confidence { get; set; } = "Medium";

        // ─── Invoice Data ───────────────────────────────────────────────
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

        // ─── Supplier / Customer matching ────────────────────────────────
        public string ExtractedSupplierName { get; set; } = "";
        public string ExtractedCustomerName { get; set; } = "";
        public int? MatchedSupplierId { get; set; }
        public string? MatchedSupplierName { get; set; }
        public int? MatchedCustomerId { get; set; }
        public string? MatchedCustomerName { get; set; }
        public int? SelectedSupplierId { get; set; }
        public int? SelectedCustomerId { get; set; }
        public string MatchConfidence { get; set; } = "None";

        // ─── Line Items ──────────────────────────────────────────────────
        public List<AiImportInvoiceItemViewModel> Items { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Smart PDF Splitter ViewModels
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel for the Smart PDF Splitter results page.
    /// Displays the analysis and matching results after AI-powered splitting.
    /// </summary>
    public class SmartSplitViewModel
    {
        public int MasterDocumentId { get; set; }
        public string OriginalFileName { get; set; } = "";
        public int TotalPages { get; set; }
        public int TotalInvoicesDetected { get; set; }
        public int TotalSplitFiles { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public int UpdatedDocumentsCount { get; set; }
        public int NewDocumentsCount { get; set; }
        public double ProcessingTimeSeconds { get; set; }
        public string Summary { get; set; } = "";
        public List<SmartSplitMatchEntry> Matches { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// A single split-file entry with its match information.
    /// </summary>
    public class SmartSplitMatchEntry
    {
        public int Index { get; set; }
        public int StartPage { get; set; }
        public int EndPage { get; set; }
        public string PageRange => StartPage == EndPage ? $"{StartPage}" : $"{StartPage}-{EndPage}";
        public int PageCount => EndPage - StartPage + 1;
        public long SplitFileSize { get; set; }

        // Detected from AI
        public string? DetectedInvoiceNumber { get; set; }
        public string? DetectedCustomerName { get; set; }
        public decimal? DetectedAmount { get; set; }

        // Match result
        public int? MatchedInvoiceId { get; set; }
        public string? MatchedInvoiceNumber { get; set; }
        public string? MatchedCustomerName { get; set; }
        public decimal? MatchedTotalAmount { get; set; }
        public string MatchMethod { get; set; } = "None";
        public string MatchConfidence { get; set; } = "None";

        // Document storage
        public int? DocumentId { get; set; }
        public bool ContentUpdated { get; set; }
    }

    /// <summary>
    /// ViewModel for listing all completed smart-split jobs.
    /// </summary>
    public class SmartSplitHistoryViewModel
    {
        public List<SmartSplitHistoryEntry> Jobs { get; set; } = new();
    }

    public class SmartSplitHistoryEntry
    {
        public int MasterDocumentId { get; set; }
        public string OriginalFileName { get; set; } = "";
        public DateTime? ProcessedDate { get; set; }
        public string ProcessingStatus { get; set; } = "";
        public int ChildDocumentCount { get; set; }
        public int SplitCompleteCount { get; set; }
        public long MasterFileSize { get; set; }
    }
}
