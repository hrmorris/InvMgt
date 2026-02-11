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

        /// <summary>
        /// Advanced multi-page PDF processing with page-boundary detection.
        /// Detects where individual invoices start/end within a large PDF (up to 100 pages),
        /// extracts each invoice separately with page-range metadata, and returns results
        /// suitable for storing each invoice as an individual document.
        /// </summary>
        /// <param name="fileStream">The PDF file stream</param>
        /// <param name="fileName">The file name</param>
        /// <param name="progressCallback">Optional callback to report progress (percentage, currentInvoice, message)</param>
        /// <returns>Result with page-aware extracted invoices</returns>
        Task<MultiPageExtractionResult> ExtractInvoicesWithPageDetectionAsync(
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

    /// <summary>
    /// Result of page-aware multi-invoice extraction from a large PDF.
    /// Each extracted invoice includes its source page range.
    /// </summary>
    public class MultiPageExtractionResult
    {
        public List<PageAwareInvoice> Invoices { get; set; } = new();
        public int TotalPages { get; set; }
        public int TotalInvoicesDetected { get; set; }
        public int SuccessfullyExtracted { get; set; }
        public int FailedExtractions { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public string ProcessingSummary { get; set; } = "";
    }

    /// <summary>
    /// An extracted invoice that knows which pages of the source PDF it came from.
    /// </summary>
    public class PageAwareInvoice
    {
        public Invoice Invoice { get; set; } = new();

        /// <summary>First page in the source PDF (1-based)</summary>
        public int StartPage { get; set; }

        /// <summary>Last page in the source PDF (1-based)</summary>
        public int EndPage { get; set; }

        /// <summary>Human-readable page range (e.g., "1-2", "3")</summary>
        public string PageRange => StartPage == EndPage ? $"{StartPage}" : $"{StartPage}-{EndPage}";

        /// <summary>AI confidence for this particular extraction</summary>
        public string Confidence { get; set; } = "Medium";
    }
}

