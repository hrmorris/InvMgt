using InvoiceManagement.Models;
using InvoiceManagement.Models.ViewModels;

namespace InvoiceManagement.Services
{
    /// <summary>
    /// Service interface for the Smart PDF Splitter module.
    /// Physically splits large multi-page PDFs into individual invoice PDF files
    /// and matches them with invoices extracted via the Multi-Page PDF feature.
    /// </summary>
    public interface ISmartPdfSplitterService
    {
        /// <summary>
        /// Analyze a PDF document to detect invoice page boundaries using AI.
        /// Returns page boundary metadata without physically splitting.
        /// </summary>
        Task<SmartSplitAnalysisResult> AnalyzePdfBoundariesAsync(
            byte[] pdfBytes,
            string fileName,
            Func<int, string, Task>? progressCallback = null);

        /// <summary>
        /// Physically split a PDF into individual files based on provided page ranges.
        /// Uses PdfSharpCore to extract pages and create individual PDF files.
        /// </summary>
        Task<List<SplitPdfFile>> SplitPdfByPageRangesAsync(
            byte[] masterPdfBytes,
            List<PageBoundary> pageBoundaries);

        /// <summary>
        /// Match split PDF files against existing invoices in the system
        /// based on invoice numbers, dates, amounts, and page range metadata.
        /// </summary>
        Task<List<SplitPdfMatchResult>> MatchSplitFilesWithInvoicesAsync(
            int masterDocumentId,
            List<SplitPdfFile> splitFiles);

        /// <summary>
        /// Full pipeline: Analyze → Split → Match.
        /// Takes a master document ID, analyzes boundaries, physically splits,
        /// stores individual PDFs, and matches them with extracted invoices.
        /// </summary>
        Task<SmartSplitResult> ProcessSmartSplitAsync(
            int masterDocumentId,
            Func<int, string, Task>? progressCallback = null);

        /// <summary>
        /// Re-link split PDFs to invoices — update existing child documents
        /// with the physically-split page content instead of the full master PDF.
        /// </summary>
        Task<SmartSplitResult> RelinkSplitPdfsAsync(
            int masterDocumentId,
            Func<int, string, Task>? progressCallback = null);
    }

    /// <summary>
    /// Result of AI boundary analysis (no physical splitting yet).
    /// </summary>
    public class SmartSplitAnalysisResult
    {
        public int TotalPages { get; set; }
        public int TotalInvoicesDetected { get; set; }
        public List<PageBoundary> PageBoundaries { get; set; } = new();
        public string Confidence { get; set; } = "Medium";
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Success => !Errors.Any() && PageBoundaries.Any();
    }

    /// <summary>
    /// Represents the detected start/end pages and a brief description of a single invoice in the PDF.
    /// </summary>
    public class PageBoundary
    {
        public int Index { get; set; }
        public int StartPage { get; set; }
        public int EndPage { get; set; }
        public string PageRange => StartPage == EndPage ? $"{StartPage}" : $"{StartPage}-{EndPage}";
        public int PageCount => EndPage - StartPage + 1;
        public string? DetectedInvoiceNumber { get; set; }
        public string? DetectedCustomerName { get; set; }
        public decimal? DetectedAmount { get; set; }
        public string Confidence { get; set; } = "Medium";
    }

    /// <summary>
    /// A physically split PDF file — the actual bytes of pages extracted from the master.
    /// </summary>
    public class SplitPdfFile
    {
        public int Index { get; set; }
        public int StartPage { get; set; }
        public int EndPage { get; set; }
        public string PageRange => StartPage == EndPage ? $"{StartPage}" : $"{StartPage}-{EndPage}";
        public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
        public long FileSize => PdfBytes.Length;
        public string? DetectedInvoiceNumber { get; set; }
        public string? DetectedCustomerName { get; set; }
        public decimal? DetectedAmount { get; set; }
    }

    /// <summary>
    /// Result of matching a split PDF file against existing invoices.
    /// </summary>
    public class SplitPdfMatchResult
    {
        public SplitPdfFile SplitFile { get; set; } = new();

        /// <summary>Matched invoice ID (null if no match found)</summary>
        public int? MatchedInvoiceId { get; set; }

        /// <summary>Matched invoice number</summary>
        public string? MatchedInvoiceNumber { get; set; }

        /// <summary>Matched customer name from the invoice</summary>
        public string? MatchedCustomerName { get; set; }

        /// <summary>Matched total amount from the invoice</summary>
        public decimal? MatchedTotalAmount { get; set; }

        /// <summary>How the match was determined</summary>
        public string MatchMethod { get; set; } = "None"; // InvoiceNumber, PageRange, Amount+Customer, None

        /// <summary>Confidence of the match</summary>
        public string MatchConfidence { get; set; } = "None"; // High, Medium, Low, None

        /// <summary>The existing child document ID that was linked (if any)</summary>
        public int? LinkedDocumentId { get; set; }

        /// <summary>Whether the child document's content was updated with the split PDF</summary>
        public bool ContentUpdated { get; set; }

        /// <summary>New document ID created for this split (if no existing doc found)</summary>
        public int? NewDocumentId { get; set; }
    }

    /// <summary>
    /// Full result of the Smart Split pipeline.
    /// </summary>
    public class SmartSplitResult
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
        public List<SplitPdfMatchResult> Matches { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Success => !Errors.Any() || Matches.Any();
    }
}
