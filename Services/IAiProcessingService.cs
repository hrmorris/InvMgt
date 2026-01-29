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

        /// <summary>
        /// Extract multiple invoices from a single PDF file containing many invoices.
        /// Supports processing 500+ invoices from a single document by chunking and batching.
        /// </summary>
        /// <param name="fileStream">The PDF file stream</param>
        /// <param name="fileName">The file name</param>
        /// <param name="progressCallback">Optional callback to report progress (percentage, currentInvoice, message)</param>
        /// <returns>List of extracted invoices with batch processing metadata</returns>
        Task<MultiInvoiceExtractionResult> ExtractMultipleInvoicesFromPdfAsync(
            Stream fileStream,
            string fileName,
            Func<int, int, string, Task>? progressCallback = null);
    }

    /// <summary>
    /// Result of extracting multiple invoices from a single PDF
    /// </summary>
    public class MultiInvoiceExtractionResult
    {
        public List<Invoice> Invoices { get; set; } = new();
        public int TotalInvoicesDetected { get; set; }
        public int SuccessfullyExtracted { get; set; }
        public int FailedExtractions { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public string ProcessingSummary { get; set; } = "";
    }
}

