using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Facility { get; set; } = string.Empty; // Hospital/Outstation name

        [Required]
        [StringLength(50)]
        public string FacilityType { get; set; } = "Hospital"; // Hospital/Outstation

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "User";
        // Admin, OIC, Supervisor, Finance_Officer, Health_Manager, Hospital_Executive, Finance_Manager, Procurement_Officer, User

        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Suspended

        [StringLength(256)]
        public string? PasswordHash { get; set; } // For future authentication

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties for many-to-many relationship with Roles
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        [StringLength(100)]
        public string? Username { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, Approved, Rejected, etc.

        [Required]
        [StringLength(100)]
        public string Entity { get; set; } = string.Empty; // Invoice, Payment, Requisition, PO, etc.

        public int? EntityId { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? IpAddress { get; set; }

        // Navigation property
        public virtual User? User { get; set; }
    }

    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SettingValue { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string Category { get; set; } = "General"; // General, Email, Procurement, Finance, etc.

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// Stores uploaded branding assets (logo, favicon, login background) in the database
    /// so they persist across Cloud Run container restarts (ephemeral filesystem).
    /// </summary>
    public class UploadedAsset
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string AssetKey { get; set; } = string.Empty; // "logo", "favicon", "login-bg"

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public byte[] FileContent { get; set; } = Array.Empty<byte>();

        public long FileSize { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UploadedBy { get; set; }
    }
}

