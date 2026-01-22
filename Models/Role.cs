using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    /// <summary>
    /// Represents a role in the system (e.g., Admin, Finance Officer, Procurement Officer)
    /// </summary>
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // Admin, Finance_Officer, Health_Manager, etc.

        [StringLength(200)]
        public string? DisplayName { get; set; } // User-friendly name

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    /// <summary>
    /// Represents a permission in the system (e.g., ViewInvoices, CreateInvoices, ApproveRequisitions)
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // ViewInvoices, CreateInvoices, etc.

        [StringLength(200)]
        public string? DisplayName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty; // Invoices, Payments, Procurement, Admin, etc.

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    /// <summary>
    /// Join table for many-to-many relationship between Users and Roles
    /// </summary>
    public class UserRole
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? AssignedBy { get; set; }
    }

    /// <summary>
    /// Join table for many-to-many relationship between Roles and Permissions
    /// </summary>
    public class RolePermission
    {
        public int Id { get; set; }

        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;

        public DateTime AssignedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Static class containing all permission constants for easy reference
    /// </summary>
    public static class Permissions
    {
        // Dashboard
        public const string ViewDashboard = "ViewDashboard";

        // Invoices
        public const string ViewInvoices = "ViewInvoices";
        public const string CreateInvoices = "CreateInvoices";
        public const string EditInvoices = "EditInvoices";
        public const string DeleteInvoices = "DeleteInvoices";
        public const string DownloadInvoicePDF = "DownloadInvoicePDF";

        // Payments
        public const string ViewPayments = "ViewPayments";
        public const string CreatePayments = "CreatePayments";
        public const string EditPayments = "EditPayments";
        public const string DeletePayments = "DeletePayments";

        // Reports
        public const string ViewReports = "ViewReports";
        public const string GenerateReports = "GenerateReports";
        public const string ExportReports = "ExportReports";

        // AI Import
        public const string UseAIImport = "UseAIImport";
        public const string ProcessDocuments = "ProcessDocuments";

        // Requisitions
        public const string ViewRequisitions = "ViewRequisitions";
        public const string CreateRequisitions = "CreateRequisitions";
        public const string EditRequisitions = "EditRequisitions";
        public const string DeleteRequisitions = "DeleteRequisitions";
        public const string SubmitRequisitions = "SubmitRequisitions";
        public const string ApproveRequisitions = "ApproveRequisitions";
        public const string RejectRequisitions = "RejectRequisitions";

        // Purchase Orders
        public const string ViewPurchaseOrders = "ViewPurchaseOrders";
        public const string CreatePurchaseOrders = "CreatePurchaseOrders";
        public const string EditPurchaseOrders = "EditPurchaseOrders";
        public const string DeletePurchaseOrders = "DeletePurchaseOrders";
        public const string ApprovePurchaseOrders = "ApprovePurchaseOrders";
        public const string ReceiveGoods = "ReceiveGoods";

        // Suppliers
        public const string ViewSuppliers = "ViewSuppliers";
        public const string CreateSuppliers = "CreateSuppliers";
        public const string EditSuppliers = "EditSuppliers";
        public const string DeleteSuppliers = "DeleteSuppliers";

        // Admin - User Management
        public const string ViewUsers = "ViewUsers";
        public const string CreateUsers = "CreateUsers";
        public const string EditUsers = "EditUsers";
        public const string DeleteUsers = "DeleteUsers";
        public const string AssignRoles = "AssignRoles";

        // Admin - Role Management
        public const string ViewRoles = "ViewRoles";
        public const string CreateRoles = "CreateRoles";
        public const string EditRoles = "EditRoles";
        public const string DeleteRoles = "DeleteRoles";
        public const string ManagePermissions = "ManagePermissions";

        // Admin - System Settings
        public const string ViewSettings = "ViewSettings";
        public const string EditSettings = "EditSettings";

        // Admin - Audit Logs
        public const string ViewAuditLogs = "ViewAuditLogs";
    }

    /// <summary>
    /// Static class containing all role name constants
    /// </summary>
    public static class Roles
    {
        public const string SystemAdmin = "SystemAdmin";
        public const string Admin = "Admin";
        public const string FinanceOfficer = "FinanceOfficer";
        public const string HealthManager = "HealthManager";
        public const string HospitalExecutive = "HospitalExecutive";
        public const string FinanceManager = "FinanceManager";
        public const string ProcurementOfficer = "ProcurementOfficer";
        public const string OIC = "OIC"; // Officer in Charge (Facility)
        public const string Supervisor = "Supervisor";
        public const string User = "User";
    }
}

