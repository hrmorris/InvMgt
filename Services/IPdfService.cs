using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice);
        Task<byte[]> GeneratePaymentReceiptPdfAsync(Payment payment);
        Task<byte[]> GenerateInvoiceReportPdfAsync(IEnumerable<Invoice> invoices, DateTime? startDate, DateTime? endDate);
        Task<byte[]> GeneratePaymentReportPdfAsync(IEnumerable<Payment> payments, DateTime? startDate, DateTime? endDate);
        Task<byte[]> GenerateSupplierInvoiceListPdfAsync(IEnumerable<Invoice> invoices, DateTime? startDate, DateTime? endDate);
        Task<byte[]> GeneratePaymentsListPdfAsync(IEnumerable<Payment> payments, DateTime? startDate, DateTime? endDate);
        Task<byte[]> GenerateRequisitionPdfAsync(Requisition requisition);
        Task<byte[]> GeneratePurchaseOrderPdfAsync(PurchaseOrder purchaseOrder);
    }
}

