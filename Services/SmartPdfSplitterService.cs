using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Text.Json;

namespace InvoiceManagement.Services
{
    /// <summary>
    /// Smart PDF Splitter implementation.
    /// Uses PdfSharpCore to physically split PDFs and AI boundary detection
    /// to determine where each invoice starts/ends, then matches split files
    /// with invoices extracted via the Multi-Page PDF feature.
    /// </summary>
    public class SmartPdfSplitterService : ISmartPdfSplitterService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiProcessingService _aiService;
        private readonly ILogger<SmartPdfSplitterService> _logger;

        public SmartPdfSplitterService(
            ApplicationDbContext context,
            IAiProcessingService aiService,
            ILogger<SmartPdfSplitterService> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 1. ANALYZE PDF BOUNDARIES (AI)
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<SmartSplitAnalysisResult> AnalyzePdfBoundariesAsync(
            byte[] pdfBytes,
            string fileName,
            Func<int, string, Task>? progressCallback = null)
        {
            var result = new SmartSplitAnalysisResult();

            try
            {
                await (progressCallback?.Invoke(5, "Reading PDF structure...") ?? Task.CompletedTask);

                // Get total page count from PdfSharpCore
                int totalPages;
                using (var ms = new MemoryStream(pdfBytes))
                {
                    using var pdfDoc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                    totalPages = pdfDoc.PageCount;
                }
                result.TotalPages = totalPages;

                _logger.LogInformation("SmartSplit: PDF {File} has {Pages} pages", fileName, totalPages);
                await (progressCallback?.Invoke(10, $"PDF has {totalPages} pages. Running AI boundary detection...") ?? Task.CompletedTask);

                // Use the existing AI boundary-detection from AiProcessingService
                using var stream = new MemoryStream(pdfBytes);
                var extractionResult = await _aiService.ExtractInvoicesWithPageDetectionAsync(
                    stream, fileName,
                    async (percent, count, message) =>
                    {
                        // Map AI service progress (0-100) to our range (10-60)
                        var mappedPercent = 10 + (int)(percent * 0.5);
                        await (progressCallback?.Invoke(mappedPercent, message) ?? Task.CompletedTask);
                    });

                result.TotalInvoicesDetected = extractionResult.TotalInvoicesDetected;
                result.Warnings.AddRange(extractionResult.Warnings);
                result.Errors.AddRange(extractionResult.Errors);

                // Convert AI extraction results to PageBoundary objects
                int idx = 1;
                foreach (var inv in extractionResult.Invoices)
                {
                    result.PageBoundaries.Add(new PageBoundary
                    {
                        Index = idx++,
                        StartPage = inv.StartPage,
                        EndPage = inv.EndPage,
                        DetectedInvoiceNumber = inv.Invoice.InvoiceNumber,
                        DetectedCustomerName = inv.Invoice.CustomerName,
                        DetectedAmount = inv.Invoice.TotalAmount,
                        Confidence = inv.Confidence
                    });
                }

                result.Confidence = result.PageBoundaries.All(b => b.Confidence == "High") ? "High" :
                                    result.PageBoundaries.Any(b => b.Confidence == "Low") ? "Low" : "Medium";

                _logger.LogInformation("SmartSplit: Detected {Count} invoices in {Pages} pages",
                    result.TotalInvoicesDetected, totalPages);

                await (progressCallback?.Invoke(65, $"Detected {result.PageBoundaries.Count} invoice boundaries") ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing PDF boundaries for {File}", fileName);
                result.Errors.Add($"Analysis error: {ex.Message}");
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 2. PHYSICALLY SPLIT PDF BY PAGE RANGES
        // ═══════════════════════════════════════════════════════════════════════

        public Task<List<SplitPdfFile>> SplitPdfByPageRangesAsync(
            byte[] masterPdfBytes,
            List<PageBoundary> pageBoundaries)
        {
            var splitFiles = new List<SplitPdfFile>();

            try
            {
                foreach (var boundary in pageBoundaries)
                {
                    try
                    {
                        var pdfBytes = ExtractPages(masterPdfBytes, boundary.StartPage, boundary.EndPage);

                        splitFiles.Add(new SplitPdfFile
                        {
                            Index = boundary.Index,
                            StartPage = boundary.StartPage,
                            EndPage = boundary.EndPage,
                            PdfBytes = pdfBytes,
                            DetectedInvoiceNumber = boundary.DetectedInvoiceNumber,
                            DetectedCustomerName = boundary.DetectedCustomerName,
                            DetectedAmount = boundary.DetectedAmount
                        });

                        _logger.LogInformation("SmartSplit: Extracted pages {Start}-{End} ({Size:N0} bytes)",
                            boundary.StartPage, boundary.EndPage, pdfBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error splitting pages {Start}-{End}", boundary.StartPage, boundary.EndPage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SplitPdfByPageRangesAsync");
            }

            return Task.FromResult(splitFiles);
        }

        /// <summary>
        /// Extract specific pages from a PDF using PdfSharpCore.
        /// Returns the bytes of a new PDF containing only the specified pages.
        /// </summary>
        private byte[] ExtractPages(byte[] masterPdfBytes, int startPage, int endPage)
        {
            using var inputStream = new MemoryStream(masterPdfBytes);
            using var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

            using var outputDoc = new PdfDocument();
            outputDoc.Version = inputDoc.Version;

            // PdfSharpCore uses 0-based indexing, our page ranges are 1-based
            var actualStart = Math.Max(0, startPage - 1);
            var actualEnd = Math.Min(inputDoc.PageCount - 1, endPage - 1);

            for (int i = actualStart; i <= actualEnd; i++)
            {
                outputDoc.AddPage(inputDoc.Pages[i]);
            }

            using var outputStream = new MemoryStream();
            outputDoc.Save(outputStream, false);
            return outputStream.ToArray();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 3. MATCH SPLIT FILES WITH EXISTING INVOICES
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<List<SplitPdfMatchResult>> MatchSplitFilesWithInvoicesAsync(
            int masterDocumentId,
            List<SplitPdfFile> splitFiles)
        {
            var results = new List<SplitPdfMatchResult>();

            // Find all child documents linked to the master document
            var masterDoc = await _context.ImportedDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == masterDocumentId);

            if (masterDoc == null) return results;

            // Find child documents that reference this master
            // Convention: ProcessingNotes contains "Master document ID: {masterDocumentId}"
            var childDocs = await _context.ImportedDocuments
                .Where(d => d.ProcessingNotes != null &&
                            d.ProcessingNotes.Contains($"Master document ID: {masterDocumentId}") &&
                            d.InvoiceId != null)
                .Select(d => new
                {
                    d.Id,
                    d.InvoiceId,
                    d.OriginalFileName,
                    d.ProcessingNotes,
                    d.FileSize
                })
                .ToListAsync();

            // Load the invoices for those child documents
            var invoiceIds = childDocs.Where(d => d.InvoiceId.HasValue).Select(d => d.InvoiceId!.Value).Distinct().ToList();
            var invoices = await _context.Invoices
                .Where(i => invoiceIds.Contains(i.Id))
                .Select(i => new { i.Id, i.InvoiceNumber, i.CustomerName, i.TotalAmount, i.Notes })
                .ToListAsync();

            // Also look for invoices with page-range metadata in Notes
            var allPageRangeInvoices = await _context.Invoices
                .Where(i => i.Notes != null && i.Notes.Contains($"from: {masterDocumentId}"))
                .Select(i => new { i.Id, i.InvoiceNumber, i.CustomerName, i.TotalAmount, i.Notes })
                .ToListAsync();

            // Merge both sets
            var candidateInvoices = invoices.Union(allPageRangeInvoices).DistinctBy(i => i.Id).ToList();

            foreach (var splitFile in splitFiles)
            {
                var match = new SplitPdfMatchResult { SplitFile = splitFile };

                // Strategy 1: Match by page range in child document filename or ProcessingNotes
                var pageRangeMatch = childDocs.FirstOrDefault(d =>
                    d.ProcessingNotes?.Contains($"pages {splitFile.PageRange}") == true ||
                    d.OriginalFileName?.Contains($"Pages_{splitFile.PageRange}") == true);

                if (pageRangeMatch != null && pageRangeMatch.InvoiceId.HasValue)
                {
                    var inv = candidateInvoices.FirstOrDefault(i => i.Id == pageRangeMatch.InvoiceId.Value);
                    if (inv != null)
                    {
                        match.MatchedInvoiceId = inv.Id;
                        match.MatchedInvoiceNumber = inv.InvoiceNumber;
                        match.MatchedCustomerName = inv.CustomerName;
                        match.MatchedTotalAmount = inv.TotalAmount;
                        match.MatchMethod = "PageRange";
                        match.MatchConfidence = "High";
                        match.LinkedDocumentId = pageRangeMatch.Id;
                    }
                }

                // Strategy 2: Match by invoice number
                if (match.MatchedInvoiceId == null && !string.IsNullOrEmpty(splitFile.DetectedInvoiceNumber))
                {
                    var invNumMatch = candidateInvoices.FirstOrDefault(i =>
                        i.InvoiceNumber?.Equals(splitFile.DetectedInvoiceNumber, StringComparison.OrdinalIgnoreCase) == true);

                    if (invNumMatch != null)
                    {
                        match.MatchedInvoiceId = invNumMatch.Id;
                        match.MatchedInvoiceNumber = invNumMatch.InvoiceNumber;
                        match.MatchedCustomerName = invNumMatch.CustomerName;
                        match.MatchedTotalAmount = invNumMatch.TotalAmount;
                        match.MatchMethod = "InvoiceNumber";
                        match.MatchConfidence = "High";

                        // Find the child doc linked to this invoice
                        var linkedDoc = childDocs.FirstOrDefault(d => d.InvoiceId == invNumMatch.Id);
                        match.LinkedDocumentId = linkedDoc?.Id;
                    }
                }

                // Strategy 3: Match by amount + customer name (fuzzy)
                if (match.MatchedInvoiceId == null && splitFile.DetectedAmount.HasValue && splitFile.DetectedAmount > 0)
                {
                    var amountMatches = candidateInvoices.Where(i =>
                        Math.Abs(i.TotalAmount - splitFile.DetectedAmount.Value) < 0.01m).ToList();

                    if (amountMatches.Count == 1)
                    {
                        var inv = amountMatches[0];
                        match.MatchedInvoiceId = inv.Id;
                        match.MatchedInvoiceNumber = inv.InvoiceNumber;
                        match.MatchedCustomerName = inv.CustomerName;
                        match.MatchedTotalAmount = inv.TotalAmount;
                        match.MatchMethod = "Amount";
                        match.MatchConfidence = "Medium";

                        var linkedDoc = childDocs.FirstOrDefault(d => d.InvoiceId == inv.Id);
                        match.LinkedDocumentId = linkedDoc?.Id;
                    }
                    else if (amountMatches.Count > 1 && !string.IsNullOrEmpty(splitFile.DetectedCustomerName))
                    {
                        // Multiple amount matches — narrow by customer name
                        var custMatch = amountMatches.FirstOrDefault(i =>
                            i.CustomerName?.Contains(splitFile.DetectedCustomerName, StringComparison.OrdinalIgnoreCase) == true);

                        if (custMatch != null)
                        {
                            match.MatchedInvoiceId = custMatch.Id;
                            match.MatchedInvoiceNumber = custMatch.InvoiceNumber;
                            match.MatchedCustomerName = custMatch.CustomerName;
                            match.MatchedTotalAmount = custMatch.TotalAmount;
                            match.MatchMethod = "Amount+Customer";
                            match.MatchConfidence = "Medium";

                            var linkedDoc = childDocs.FirstOrDefault(d => d.InvoiceId == custMatch.Id);
                            match.LinkedDocumentId = linkedDoc?.Id;
                        }
                    }
                }

                // Strategy 4: Positional match (index-based fallback)
                if (match.MatchedInvoiceId == null)
                {
                    // Try matching by Notes containing the page range
                    var noteMatch = candidateInvoices.FirstOrDefault(i =>
                        i.Notes?.Contains($"pages {splitFile.PageRange}") == true);

                    if (noteMatch != null)
                    {
                        match.MatchedInvoiceId = noteMatch.Id;
                        match.MatchedInvoiceNumber = noteMatch.InvoiceNumber;
                        match.MatchedCustomerName = noteMatch.CustomerName;
                        match.MatchedTotalAmount = noteMatch.TotalAmount;
                        match.MatchMethod = "NotesPageRange";
                        match.MatchConfidence = "Medium";

                        var linkedDoc = childDocs.FirstOrDefault(d => d.InvoiceId == noteMatch.Id);
                        match.LinkedDocumentId = linkedDoc?.Id;
                    }
                }

                results.Add(match);
            }

            return results;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 4. FULL PIPELINE: ANALYZE → SPLIT → MATCH → STORE
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<SmartSplitResult> ProcessSmartSplitAsync(
            int masterDocumentId,
            Func<int, string, Task>? progressCallback = null)
        {
            var startTime = DateTime.UtcNow;
            var result = new SmartSplitResult { MasterDocumentId = masterDocumentId };

            try
            {
                // Load master document
                await (progressCallback?.Invoke(2, "Loading master PDF document...") ?? Task.CompletedTask);

                var masterDoc = await _context.ImportedDocuments.FindAsync(masterDocumentId);
                if (masterDoc == null)
                {
                    result.Errors.Add("Master document not found.");
                    return result;
                }

                if (masterDoc.FileContent == null || masterDoc.FileContent.Length == 0)
                {
                    result.Errors.Add("Master document has no file content.");
                    return result;
                }

                result.OriginalFileName = masterDoc.OriginalFileName;
                var pdfBytes = masterDoc.FileContent;

                // Detach to avoid holding stale EF context during long AI processing
                _context.Entry(masterDoc).State = EntityState.Detached;

                // STEP 1: Analyze boundaries
                await (progressCallback?.Invoke(5, "Analyzing PDF structure with AI...") ?? Task.CompletedTask);

                var analysis = await AnalyzePdfBoundariesAsync(pdfBytes, result.OriginalFileName,
                    async (percent, msg) =>
                    {
                        var mapped = 5 + (int)(percent * 0.45); // Maps 0-100 to 5-50
                        await (progressCallback?.Invoke(mapped, msg) ?? Task.CompletedTask);
                    });

                result.TotalPages = analysis.TotalPages;
                result.TotalInvoicesDetected = analysis.TotalInvoicesDetected;
                result.Warnings.AddRange(analysis.Warnings);

                if (!analysis.Success)
                {
                    result.Errors.AddRange(analysis.Errors);
                    return result;
                }

                // STEP 2: Physically split
                await (progressCallback?.Invoke(55, $"Physically splitting PDF into {analysis.PageBoundaries.Count} individual files...") ?? Task.CompletedTask);

                var splitFiles = await SplitPdfByPageRangesAsync(pdfBytes, analysis.PageBoundaries);
                result.TotalSplitFiles = splitFiles.Count;

                _logger.LogInformation("SmartSplit: Created {Count} split files from {Pages} pages",
                    splitFiles.Count, analysis.TotalPages);

                await (progressCallback?.Invoke(70, $"Split into {splitFiles.Count} individual PDFs. Matching with invoices...") ?? Task.CompletedTask);

                // STEP 3: Match with existing invoices
                var matches = await MatchSplitFilesWithInvoicesAsync(masterDocumentId, splitFiles);
                result.Matches = matches;
                result.MatchedCount = matches.Count(m => m.MatchedInvoiceId.HasValue);
                result.UnmatchedCount = matches.Count(m => !m.MatchedInvoiceId.HasValue);

                await (progressCallback?.Invoke(80, $"Matched {result.MatchedCount} of {splitFiles.Count} split files. Updating documents...") ?? Task.CompletedTask);

                // STEP 4: Store/update split documents
                int updatedCount = 0;
                int newCount = 0;

                foreach (var match in matches)
                {
                    try
                    {
                        if (match.LinkedDocumentId.HasValue)
                        {
                            // Update existing child document with physically split content
                            var childDoc = await _context.ImportedDocuments.FindAsync(match.LinkedDocumentId.Value);
                            if (childDoc != null)
                            {
                                childDoc.FileContent = match.SplitFile.PdfBytes;
                                childDoc.FileSize = match.SplitFile.PdfBytes.Length;
                                childDoc.ProcessingNotes = $"Smart-split pages {match.SplitFile.PageRange} from master {masterDocumentId}. " +
                                                          $"Match: {match.MatchMethod} ({match.MatchConfidence})";
                                childDoc.ProcessingStatus = "Split-Complete";
                                childDoc.ProcessedDate = DateTime.Now;
                                match.ContentUpdated = true;
                                updatedCount++;
                            }
                        }
                        else if (match.MatchedInvoiceId.HasValue)
                        {
                            // Create a new child document linked to the matched invoice
                            var cleanNum = string.Join("", (match.MatchedInvoiceNumber ?? "unknown")
                                .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                            var newDoc = new ImportedDocument
                            {
                                FileName = $"{Guid.NewGuid()}_SmartSplit_{cleanNum}.pdf",
                                OriginalFileName = $"SmartSplit_{cleanNum}_Pages_{match.SplitFile.PageRange}.pdf",
                                ContentType = "application/pdf",
                                FileSize = match.SplitFile.PdfBytes.Length,
                                FileContent = match.SplitFile.PdfBytes,
                                DocumentType = "Invoice",
                                ProcessingStatus = "Split-Complete",
                                ProcessingNotes = $"Smart-split pages {match.SplitFile.PageRange} from master {masterDocumentId}. " +
                                                 $"Match: {match.MatchMethod} ({match.MatchConfidence}). Master document ID: {masterDocumentId}",
                                InvoiceId = match.MatchedInvoiceId,
                                UploadDate = DateTime.Now,
                                ProcessedDate = DateTime.Now,
                                UploadedBy = "SmartSplit"
                            };
                            _context.ImportedDocuments.Add(newDoc);
                            await _context.SaveChangesAsync();
                            match.NewDocumentId = newDoc.Id;
                            newCount++;
                        }
                        else
                        {
                            // No invoice match — store as unmatched split document
                            var newDoc = new ImportedDocument
                            {
                                FileName = $"{Guid.NewGuid()}_SmartSplit_Unmatched_{match.SplitFile.Index}.pdf",
                                OriginalFileName = $"SmartSplit_Pages_{match.SplitFile.PageRange}.pdf",
                                ContentType = "application/pdf",
                                FileSize = match.SplitFile.PdfBytes.Length,
                                FileContent = match.SplitFile.PdfBytes,
                                DocumentType = "Invoice",
                                ProcessingStatus = "Split-Unmatched",
                                ProcessingNotes = $"Smart-split pages {match.SplitFile.PageRange} from master {masterDocumentId}. " +
                                                 $"No invoice match found. Detected: #{match.SplitFile.DetectedInvoiceNumber ?? "?"}, " +
                                                 $"Amount: {match.SplitFile.DetectedAmount?.ToString("C") ?? "?"}, " +
                                                 $"Customer: {match.SplitFile.DetectedCustomerName ?? "?"}. Master document ID: {masterDocumentId}",
                                ExtractedSupplierName = match.SplitFile.DetectedCustomerName,
                                UploadDate = DateTime.Now,
                                ProcessedDate = DateTime.Now,
                                UploadedBy = "SmartSplit"
                            };
                            _context.ImportedDocuments.Add(newDoc);
                            await _context.SaveChangesAsync();
                            match.NewDocumentId = newDoc.Id;
                            newCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error storing split document for pages {Range}", match.SplitFile.PageRange);
                        result.Warnings.Add($"Failed to store split for pages {match.SplitFile.PageRange}: {ex.Message}");

                        // Clear failed entities
                        foreach (var entry in _context.ChangeTracker.Entries()
                            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
                        {
                            entry.State = EntityState.Detached;
                        }
                    }
                }

                if (updatedCount > 0 || newCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                result.UpdatedDocumentsCount = updatedCount;
                result.NewDocumentsCount = newCount;

                // Update master document
                var freshMaster = await _context.ImportedDocuments.FindAsync(masterDocumentId);
                if (freshMaster != null)
                {
                    freshMaster.ProcessingStatus = "Smart-Split";
                    freshMaster.ProcessedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                result.ProcessingTimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                result.Summary = $"Smart Split complete: {splitFiles.Count} PDFs created from {result.TotalPages} pages. " +
                                $"{result.MatchedCount} matched, {result.UnmatchedCount} unmatched. " +
                                $"{updatedCount} updated, {newCount} new. " +
                                $"Processed in {result.ProcessingTimeSeconds:F1}s.";

                await (progressCallback?.Invoke(100, result.Summary) ?? Task.CompletedTask);

                _logger.LogInformation("SmartSplit complete: {Summary}", result.Summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessSmartSplitAsync for document {Id}", masterDocumentId);
                result.Errors.Add($"Processing error: {ex.Message}");
                result.ProcessingTimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 5. RE-LINK: Update existing child docs with physically-split content
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<SmartSplitResult> RelinkSplitPdfsAsync(
            int masterDocumentId,
            Func<int, string, Task>? progressCallback = null)
        {
            var startTime = DateTime.UtcNow;
            var result = new SmartSplitResult { MasterDocumentId = masterDocumentId };

            try
            {
                await (progressCallback?.Invoke(5, "Loading master document...") ?? Task.CompletedTask);

                var masterDoc = await _context.ImportedDocuments.FindAsync(masterDocumentId);
                if (masterDoc == null)
                {
                    result.Errors.Add("Master document not found.");
                    return result;
                }

                result.OriginalFileName = masterDoc.OriginalFileName;
                var pdfBytes = masterDoc.FileContent;

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    result.Errors.Add("Master document has no content.");
                    return result;
                }

                // Find existing child documents
                await (progressCallback?.Invoke(10, "Finding child documents...") ?? Task.CompletedTask);

                var childDocs = await _context.ImportedDocuments
                    .Where(d => d.ProcessingNotes != null &&
                               d.ProcessingNotes.Contains($"Master document ID: {masterDocumentId}") &&
                               d.InvoiceId != null)
                    .ToListAsync();

                if (childDocs.Count == 0)
                {
                    result.Errors.Add("No child documents found linked to this master document. Process with Multi-Page PDF first.");
                    return result;
                }

                _logger.LogInformation("SmartSplit Relink: Found {Count} child documents for master {Id}",
                    childDocs.Count, masterDocumentId);

                // Parse page ranges from child document metadata
                var pageBoundaries = new List<PageBoundary>();
                int idx = 1;

                foreach (var child in childDocs)
                {
                    // Extract page range from ProcessingNotes or OriginalFileName
                    var pageRange = ExtractPageRangeFromMetadata(child.ProcessingNotes, child.OriginalFileName);
                    if (pageRange.HasValue)
                    {
                        pageBoundaries.Add(new PageBoundary
                        {
                            Index = idx++,
                            StartPage = pageRange.Value.start,
                            EndPage = pageRange.Value.end
                        });
                    }
                }

                if (pageBoundaries.Count == 0)
                {
                    result.Errors.Add("Could not determine page ranges from child document metadata.");
                    return result;
                }

                await (progressCallback?.Invoke(25, $"Splitting PDF into {pageBoundaries.Count} files...") ?? Task.CompletedTask);

                // Get total pages
                using (var ms = new MemoryStream(pdfBytes))
                {
                    using var pdfDoc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                    result.TotalPages = pdfDoc.PageCount;
                }

                // Physically split
                var splitFiles = await SplitPdfByPageRangesAsync(pdfBytes, pageBoundaries);
                result.TotalSplitFiles = splitFiles.Count;

                // Detach master doc to free memory
                _context.Entry(masterDoc).State = EntityState.Detached;

                // Update child documents with split content
                await (progressCallback?.Invoke(50, "Updating child documents with split PDF content...") ?? Task.CompletedTask);

                int updatedCount = 0;
                foreach (var splitFile in splitFiles)
                {
                    // Find the child doc for this page range
                    var matchingChild = childDocs.FirstOrDefault(d =>
                    {
                        var range = ExtractPageRangeFromMetadata(d.ProcessingNotes, d.OriginalFileName);
                        return range.HasValue &&
                               range.Value.start == splitFile.StartPage &&
                               range.Value.end == splitFile.EndPage;
                    });

                    if (matchingChild != null)
                    {
                        // Re-attach if detached
                        var entry = _context.Entry(matchingChild);
                        if (entry.State == EntityState.Detached)
                        {
                            _context.Attach(matchingChild);
                        }

                        matchingChild.FileContent = splitFile.PdfBytes;
                        matchingChild.FileSize = splitFile.PdfBytes.Length;
                        matchingChild.ProcessingStatus = "Split-Complete";
                        matchingChild.ProcessedDate = DateTime.Now;

                        // Get the invoice for match result
                        var invoice = matchingChild.InvoiceId.HasValue
                            ? await _context.Invoices.AsNoTracking()
                                .Where(i => i.Id == matchingChild.InvoiceId.Value)
                                .Select(i => new { i.Id, i.InvoiceNumber, i.CustomerName, i.TotalAmount })
                                .FirstOrDefaultAsync()
                            : null;

                        result.Matches.Add(new SplitPdfMatchResult
                        {
                            SplitFile = splitFile,
                            MatchedInvoiceId = matchingChild.InvoiceId,
                            MatchedInvoiceNumber = invoice?.InvoiceNumber,
                            MatchedCustomerName = invoice?.CustomerName,
                            MatchedTotalAmount = invoice?.TotalAmount,
                            MatchMethod = "Relink-PageRange",
                            MatchConfidence = "High",
                            LinkedDocumentId = matchingChild.Id,
                            ContentUpdated = true
                        });

                        updatedCount++;

                        var progressPct = 50 + (int)((double)updatedCount / splitFiles.Count * 40);
                        await (progressCallback?.Invoke(progressPct,
                            $"Updated {updatedCount} of {splitFiles.Count} documents...") ?? Task.CompletedTask);
                    }
                }

                await _context.SaveChangesAsync();

                result.MatchedCount = updatedCount;
                result.UpdatedDocumentsCount = updatedCount;
                result.UnmatchedCount = splitFiles.Count - updatedCount;
                result.TotalInvoicesDetected = childDocs.Count;
                result.ProcessingTimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds;

                result.Summary = $"Re-link complete: Updated {updatedCount} of {splitFiles.Count} documents with physically-split PDF content " +
                                $"in {result.ProcessingTimeSeconds:F1}s.";

                await (progressCallback?.Invoke(100, result.Summary) ?? Task.CompletedTask);

                _logger.LogInformation("SmartSplit Relink: {Summary}", result.Summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RelinkSplitPdfsAsync for document {Id}", masterDocumentId);
                result.Errors.Add($"Relink error: {ex.Message}");
                result.ProcessingTimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Extract page range from metadata strings (ProcessingNotes, OriginalFileName).
        /// Looks for patterns like "pages 1-3", "Pages_1-3", "pages 5".
        /// </summary>
        private (int start, int end)? ExtractPageRangeFromMetadata(string? notes, string? fileName)
        {
            // Try ProcessingNotes first: "pages 1-3" or "pages 5"
            if (!string.IsNullOrEmpty(notes))
            {
                var match = System.Text.RegularExpressions.Regex.Match(notes, @"pages?\s+(\d+)(?:\s*-\s*(\d+))?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var start = int.Parse(match.Groups[1].Value);
                    var end = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : start;
                    return (start, end);
                }
            }

            // Try OriginalFileName: "Pages_1-3" or "Pages_5"
            if (!string.IsNullOrEmpty(fileName))
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, @"Pages?_(\d+)(?:-(\d+))?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var start = int.Parse(match.Groups[1].Value);
                    var end = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : start;
                    return (start, end);
                }
            }

            return null;
        }
    }
}
