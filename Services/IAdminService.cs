using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IAdminService
    {
        // User Management
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User> CreateUserAsync(User user, string? password = null);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
        Task<IEnumerable<User>> GetUsersByFacilityAsync(string facility);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task ResetPasswordAsync(int userId, string newPassword);
        Task UpdateUserStatusAsync(int userId, string status);
        Task BulkUpdateStatusAsync(IEnumerable<int> userIds, string status);
        Task<UserActivityStats> GetUserActivityStatsAsync(int userId);

        // Audit Logs
        Task LogActionAsync(int? userId, string username, string action, string entity, int? entityId, string? details);
        Task<IEnumerable<AuditLog>> GetRecentAuditLogsAsync(int count = 100);
        Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(int userId);
        Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entity, int? entityId);

        // System Settings
        Task<IEnumerable<SystemSetting>> GetAllSettingsAsync();
        Task<SystemSetting?> GetSettingByKeyAsync(string key);
        Task<SystemSetting?> GetSettingByIdAsync(int id);
        Task<string?> GetSettingValueAsync(string key);
        Task UpdateSettingAsync(string key, string value, string modifiedBy);
        Task CreateSettingAsync(SystemSetting setting);
        Task DeleteSettingAsync(int id);
        Task InitializeDefaultSettingsAsync();

        // Dashboard Statistics
        Task<DashboardStats> GetDashboardStatsAsync();
    }

    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int TotalRequisitions { get; set; }
        public int PendingRequisitions { get; set; }
        public int TotalPurchaseOrders { get; set; }
        public int OpenPurchaseOrders { get; set; }
        public int TotalInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
        public decimal TotalUnpaidAmount { get; set; }
        public int TodayActions { get; set; }
    }

    public class UserActivityStats
    {
        public int TotalLogins { get; set; }
        public int TotalActions { get; set; }
        public DateTime? FirstLogin { get; set; }
        public DateTime? LastLogin { get; set; }
        public int InvoicesCreated { get; set; }
        public int PaymentsRecorded { get; set; }
        public int RequisitionsCreated { get; set; }
        public int PurchaseOrdersCreated { get; set; }
        public Dictionary<string, int> ActionsByType { get; set; } = new();
        public List<AuditLog> RecentActivity { get; set; } = new();
    }
}

