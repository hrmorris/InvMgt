namespace InvoiceManagement.Models.ViewModels
{
    public class SupplierOutstandingViewModel
    {
        public List<SupplierOutstandingSummary> Suppliers { get; set; } = new();
        public decimal TotalOutstanding { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalSuppliers { get; set; }
        public decimal TotalOverdue { get; set; }
        public int OverdueInvoiceCount { get; set; }
    }

    public class SupplierOutstandingSummary
    {
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int InvoiceCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal OverdueAmount { get; set; }
        public DateTime? OldestInvoiceDate { get; set; }
        public int MaxDaysOverdue { get; set; }
        public List<Invoice> Invoices { get; set; } = new();
    }
}
