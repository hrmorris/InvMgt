using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;
using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public class DocumentStorageService : IDocumentStorageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentStorageService> _logger;

        public DocumentStorageService(ApplicationDbContext context, ILogger<DocumentStorageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ImportedDocument> StoreDocumentAsync(IFormFile file, string documentType, string? uploadedBy = null)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            var document = new ImportedDocument
            {
                FileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}",
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileContent = fileContent,
                DocumentType = documentType,
                ProcessingStatus = "Pending",
                UploadDate = DateTime.Now,
                UploadedBy = uploadedBy
            };

            _context.ImportedDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Stored document: {FileName}, Type: {DocumentType}, Size: {FileSize} bytes", 
                document.OriginalFileName, documentType, document.FileSize);

            return document;
        }

        public async Task<ImportedDocument?> GetDocumentByIdAsync(int id)
        {
            return await _context.ImportedDocuments
                .Include(d => d.Invoice)
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<ImportedDocument>> GetDocumentsByInvoiceIdAsync(int invoiceId)
        {
            return await _context.ImportedDocuments
                .Where(d => d.InvoiceId == invoiceId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ImportedDocument>> GetDocumentsByPaymentIdAsync(int paymentId)
        {
            return await _context.ImportedDocuments
                .Where(d => d.PaymentId == paymentId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task LinkDocumentToInvoiceAsync(int documentId, int invoiceId)
        {
            var document = await _context.ImportedDocuments.FindAsync(documentId);
            if (document != null)
            {
                document.InvoiceId = invoiceId;
                document.ProcessingStatus = "Processed";
                document.ProcessedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Linked document {DocumentId} to invoice {InvoiceId}", documentId, invoiceId);
            }
        }

        public async Task LinkDocumentToPaymentAsync(int documentId, int paymentId)
        {
            var document = await _context.ImportedDocuments.FindAsync(documentId);
            if (document != null)
            {
                document.PaymentId = paymentId;
                document.ProcessingStatus = "Processed";
                document.ProcessedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Linked document {DocumentId} to payment {PaymentId}", documentId, paymentId);
            }
        }

        public async Task UpdateDocumentExtractedDataAsync(int documentId, string? extractedText, string? accountNumber, string? bankName, string? supplierName, string? customerName)
        {
            var document = await _context.ImportedDocuments.FindAsync(documentId);
            if (document != null)
            {
                document.ExtractedText = extractedText?.Length > 2000 ? extractedText.Substring(0, 2000) : extractedText;
                document.ExtractedAccountNumber = accountNumber;
                document.ExtractedBankName = bankName;
                document.ExtractedSupplierName = supplierName;
                document.ExtractedCustomerName = customerName;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteDocumentAsync(int id)
        {
            var document = await _context.ImportedDocuments.FindAsync(id);
            if (document != null)
            {
                _context.ImportedDocuments.Remove(document);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted document {DocumentId}: {FileName}", id, document.OriginalFileName);
            }
        }

        public async Task<byte[]?> GetDocumentContentAsync(int id)
        {
            var document = await _context.ImportedDocuments.FindAsync(id);
            return document?.FileContent;
        }
    }
}
