using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IDocumentStorageService
    {
        Task<ImportedDocument> StoreDocumentAsync(IFormFile file, string documentType, string? uploadedBy = null);
        Task<ImportedDocument?> GetDocumentByIdAsync(int id);
        Task<IEnumerable<ImportedDocument>> GetDocumentsByInvoiceIdAsync(int invoiceId);
        Task<IEnumerable<ImportedDocument>> GetDocumentsByPaymentIdAsync(int paymentId);
        Task LinkDocumentToInvoiceAsync(int documentId, int invoiceId);
        Task LinkDocumentToPaymentAsync(int documentId, int paymentId);
        Task UpdateDocumentExtractedDataAsync(int documentId, string? extractedText, string? accountNumber, string? bankName, string? supplierName, string? customerName);
        Task UpdateDocumentFilenameWithInvoiceNumberAsync(int documentId, string invoiceNumber);
        Task DeleteDocumentAsync(int id);
        Task<byte[]?> GetDocumentContentAsync(int id);
    }
}
