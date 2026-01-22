using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? CustomerCode { get; set; }

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
        public string? Industry { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, OnHold

        public int PaymentTermsDays { get; set; } = 30; // Default 30 days

        public decimal CreditLimit { get; set; } = 0; // Credit limit for the customer

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties - for future linking invoices to customers
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
