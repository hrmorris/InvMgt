using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IAuthorizationService
    {
        // Permission checks
        Task<bool> HasPermissionAsync(int userId, string permission);
        Task<bool> HasAnyPermissionAsync(int userId, params string[] permissions);
        Task<bool> HasAllPermissionsAsync(int userId, params string[] permissions);
        Task<List<string>> GetUserPermissionsAsync(int userId);

        // Role checks
        Task<bool> HasRoleAsync(int userId, string roleName);
        Task<bool> HasAnyRoleAsync(int userId, params string[] roleNames);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<List<Role>> GetUserRoleObjectsAsync(int userId);

        // Role management
        Task AssignRoleToUserAsync(int userId, int roleId, string assignedBy);
        Task RemoveRoleFromUserAsync(int userId, int roleId);
        Task<List<UserRole>> GetUserRoleAssignmentsAsync(int userId);

        // Permission management for roles
        Task AssignPermissionToRoleAsync(int roleId, int permissionId);
        Task RemovePermissionFromRoleAsync(int roleId, int permissionId);
        Task<List<Permission>> GetRolePermissionsAsync(int roleId);

        // Initialize default data
        Task InitializeRolesAndPermissionsAsync();
    }
}

