using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SupplierCode { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? Mobile { get; set; }

        [StringLength(200)]
        public string? ContactPerson { get; set; }

        [StringLength(100)]
        public string? TIN { get; set; } // Tax Identification Number

        [StringLength(100)]
        public string? RegistrationNumber { get; set; }

        [StringLength(200)]
        public string? BankName { get; set; }

        [StringLength(50)]
        public string? BankAccountNumber { get; set; }

        [StringLength(500)]
        public string? ProductsServices { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Blacklisted

        public int PaymentTermsDays { get; set; } = 30; // Default 30 days

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}

