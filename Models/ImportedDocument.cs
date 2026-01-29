using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    public class ImportedDocument
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        // Store file content as Base64 or binary
        public byte[] FileContent { get; set; } = Array.Empty<byte>();

        [StringLength(50)]
        public string DocumentType { get; set; } = "Invoice"; // Invoice, Payment, Receipt

        // Reference to the entity this document belongs to
        public int? InvoiceId { get; set; }
        public int? PaymentId { get; set; }

        // Extracted data stored for reference
        [StringLength(2000)]
        public string? ExtractedText { get; set; }

        [StringLength(500)]
        public string? ExtractedAccountNumber { get; set; }

        [StringLength(500)]
        public string? ExtractedBankName { get; set; }

        [StringLength(500)]
        public string? ExtractedSupplierName { get; set; }

        [StringLength(500)]
        public string? ExtractedCustomerName { get; set; }

        // Processing status
        [StringLength(50)]
        public string ProcessingStatus { get; set; } = "Pending"; // Pending, Processed, Error

        // No length limit for ProcessingNotes to support bulk invoice JSON data
        public string? ProcessingNotes { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public DateTime? ProcessedDate { get; set; }

        [StringLength(100)]
        public string? UploadedBy { get; set; }

        // Navigation properties
        public virtual Invoice? Invoice { get; set; }
        public virtual Payment? Payment { get; set; }
    }
}
