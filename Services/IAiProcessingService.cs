using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IAiProcessingService
    {
        Task<Invoice?> ExtractInvoiceFromFileAsync(Stream fileStream, string fileName);
        Task<Payment?> ExtractPaymentFromFileAsync(Stream fileStream, string fileName);
        Task<List<Invoice>> ProcessInvoiceBatchAsync(List<(Stream stream, string fileName)> files);
        Task<List<Payment>> ProcessPaymentBatchAsync(List<(Stream stream, string fileName)> files);
        Task<string> MatchPaymentToInvoiceAsync(Payment payment, List<Invoice> invoices);
    }
}

