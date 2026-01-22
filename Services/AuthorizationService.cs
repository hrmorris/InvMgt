using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(ApplicationDbContext context, ILogger<AuthorizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Permission checks
        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            // Admin users have all permissions
            var userRoles = await GetUserRolesAsync(userId);
            if (userRoles.Contains("Admin") || userRoles.Contains("SystemAdmin"))
            {
                return true;
            }

            var userPermissions = await GetUserPermissionsAsync(userId);
            return userPermissions.Contains(permission);
        }

        public async Task<bool> HasAnyPermissionAsync(int userId, params string[] permissions)
        {
            // Admin users have all permissions
            var userRoles = await GetUserRolesAsync(userId);
            if (userRoles.Contains("Admin") || userRoles.Contains("SystemAdmin"))
            {
                return true;
            }

            var userPermissions = await GetUserPermissionsAsync(userId);
            return permissions.Any(p => userPermissions.Contains(p));
        }

        public async Task<bool> HasAllPermissionsAsync(int userId, params string[] permissions)
        {
            // Admin users have all permissions
            var userRoles = await GetUserRolesAsync(userId);
            if (userRoles.Contains("Admin") || userRoles.Contains("SystemAdmin"))
            {
                return true;
            }

            var userPermissions = await GetUserPermissionsAsync(userId);
            return permissions.All(p => userPermissions.Contains(p));
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            var permissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(ur => ur.Role.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Where(rp => rp.Permission.IsActive)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        // Role checks
        public async Task<bool> HasRoleAsync(int userId, string roleName)
        {
            // Admin users have all roles
            var userRoles = await GetUserRolesAsync(userId);
            if (userRoles.Contains("Admin") || userRoles.Contains("SystemAdmin"))
            {
                return true;
            }

            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName && ur.Role.IsActive);
        }

        public async Task<bool> HasAnyRoleAsync(int userId, params string[] roleNames)
        {
            // Admin users have all roles
            var userRoles = await GetUserRolesAsync(userId);
            if (userRoles.Contains("Admin") || userRoles.Contains("SystemAdmin"))
            {
                return true;
            }

            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && roleNames.Contains(ur.Role.Name) && ur.Role.IsActive);
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.IsActive)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }

        public async Task<List<Role>> GetUserRoleObjectsAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.IsActive)
                .Select(ur => ur.Role)
                .ToListAsync();
        }

        // Role management
        public async Task AssignRoleToUserAsync(int userId, int roleId, string assignedBy)
        {
            // Check if assignment already exists
            var exists = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (!exists)
            {
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedBy = assignedBy,
                    AssignedDate = DateTime.Now
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Role {roleId} assigned to user {userId} by {assignedBy}");
            }
        }

        public async Task RemoveRoleFromUserAsync(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Role {roleId} removed from user {userId}");
            }
        }

        public async Task<List<UserRole>> GetUserRoleAssignmentsAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .ToListAsync();
        }

        // Permission management for roles
        public async Task AssignPermissionToRoleAsync(int roleId, int permissionId)
        {
            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (!exists)
            {
                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    AssignedDate = DateTime.Now
                };

                _context.RolePermissions.Add(rolePermission);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Permission {permissionId} assigned to role {roleId}");
            }
        }

        public async Task RemovePermissionFromRoleAsync(int roleId, int permissionId)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission != null)
            {
                _context.RolePermissions.Remove(rolePermission);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Permission {permissionId} removed from role {roleId}");
            }
        }

        public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
        {
            return await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission.IsActive)
                .Select(rp => rp.Permission)
                .ToListAsync();
        }

        // Initialize default roles and permissions
        public async Task InitializeRolesAndPermissionsAsync()
        {
            // Check if already initialized
            if (await _context.Roles.AnyAsync())
            {
                _logger.LogInformation("Roles already initialized");
                return;
            }

            _logger.LogInformation("Initializing roles and permissions...");

            // Create permissions
            var permissions = new List<Permission>
            {
                // Dashboard
                new Permission { Name = Permissions.ViewDashboard, DisplayName = "View Dashboard", Module = "Dashboard", Description = "Access to main dashboard" },

                // Invoices
                new Permission { Name = Permissions.ViewInvoices, DisplayName = "View Invoices", Module = "Invoices", Description = "View invoice list and details" },
                new Permission { Name = Permissions.CreateInvoices, DisplayName = "Create Invoices", Module = "Invoices", Description = "Create new invoices" },
                new Permission { Name = Permissions.EditInvoices, DisplayName = "Edit Invoices", Module = "Invoices", Description = "Edit existing invoices" },
                new Permission { Name = Permissions.DeleteInvoices, DisplayName = "Delete Invoices", Module = "Invoices", Description = "Delete invoices" },
                new Permission { Name = Permissions.DownloadInvoicePDF, DisplayName = "Download Invoice PDF", Module = "Invoices", Description = "Download invoice PDFs" },

                // Payments
                new Permission { Name = Permissions.ViewPayments, DisplayName = "View Payments", Module = "Payments", Description = "View payment list and details" },
                new Permission { Name = Permissions.CreatePayments, DisplayName = "Create Payments", Module = "Payments", Description = "Create new payments" },
                new Permission { Name = Permissions.EditPayments, DisplayName = "Edit Payments", Module = "Payments", Description = "Edit existing payments" },
                new Permission { Name = Permissions.DeletePayments, DisplayName = "Delete Payments", Module = "Payments", Description = "Delete payments" },

                // Reports
                new Permission { Name = Permissions.ViewReports, DisplayName = "View Reports", Module = "Reports", Description = "Access reports section" },
                new Permission { Name = Permissions.GenerateReports, DisplayName = "Generate Reports", Module = "Reports", Description = "Generate custom reports" },
                new Permission { Name = Permissions.ExportReports, DisplayName = "Export Reports", Module = "Reports", Description = "Export reports to PDF/Excel" },

                // AI Import
                new Permission { Name = Permissions.UseAIImport, DisplayName = "Use AI Import", Module = "AI Import", Description = "Access AI-powered import features" },
                new Permission { Name = Permissions.ProcessDocuments, DisplayName = "Process Documents", Module = "AI Import", Description = "Process documents with AI" },

                // Requisitions
                new Permission { Name = Permissions.ViewRequisitions, DisplayName = "View Requisitions", Module = "Procurement", Description = "View requisition list and details" },
                new Permission { Name = Permissions.CreateRequisitions, DisplayName = "Create Requisitions", Module = "Procurement", Description = "Create new requisitions" },
                new Permission { Name = Permissions.EditRequisitions, DisplayName = "Edit Requisitions", Module = "Procurement", Description = "Edit existing requisitions" },
                new Permission { Name = Permissions.DeleteRequisitions, DisplayName = "Delete Requisitions", Module = "Procurement", Description = "Delete requisitions" },
                new Permission { Name = Permissions.SubmitRequisitions, DisplayName = "Submit Requisitions", Module = "Procurement", Description = "Submit requisitions for approval" },
                new Permission { Name = Permissions.ApproveRequisitions, DisplayName = "Approve Requisitions", Module = "Procurement", Description = "Approve or reject requisitions" },
                new Permission { Name = Permissions.RejectRequisitions, DisplayName = "Reject Requisitions", Module = "Procurement", Description = "Reject requisitions" },

                // Purchase Orders
                new Permission { Name = Permissions.ViewPurchaseOrders, DisplayName = "View Purchase Orders", Module = "Procurement", Description = "View purchase order list and details" },
                new Permission { Name = Permissions.CreatePurchaseOrders, DisplayName = "Create Purchase Orders", Module = "Procurement", Description = "Create new purchase orders" },
                new Permission { Name = Permissions.EditPurchaseOrders, DisplayName = "Edit Purchase Orders", Module = "Procurement", Description = "Edit existing purchase orders" },
                new Permission { Name = Permissions.DeletePurchaseOrders, DisplayName = "Delete Purchase Orders", Module = "Procurement", Description = "Delete purchase orders" },
                new Permission { Name = Permissions.ApprovePurchaseOrders, DisplayName = "Approve Purchase Orders", Module = "Procurement", Description = "Approve purchase orders" },
                new Permission { Name = Permissions.ReceiveGoods, DisplayName = "Receive Goods", Module = "Procurement", Description = "Mark goods as received" },

                // Suppliers
                new Permission { Name = Permissions.ViewSuppliers, DisplayName = "View Suppliers", Module = "Suppliers", Description = "View supplier list and details" },
                new Permission { Name = Permissions.CreateSuppliers, DisplayName = "Create Suppliers", Module = "Suppliers", Description = "Create new suppliers" },
                new Permission { Name = Permissions.EditSuppliers, DisplayName = "Edit Suppliers", Module = "Suppliers", Description = "Edit existing suppliers" },
                new Permission { Name = Permissions.DeleteSuppliers, DisplayName = "Delete Suppliers", Module = "Suppliers", Description = "Delete suppliers" },

                // Admin - User Management
                new Permission { Name = Permissions.ViewUsers, DisplayName = "View Users", Module = "Admin", Description = "View user list and details" },
                new Permission { Name = Permissions.CreateUsers, DisplayName = "Create Users", Module = "Admin", Description = "Create new users" },
                new Permission { Name = Permissions.EditUsers, DisplayName = "Edit Users", Module = "Admin", Description = "Edit existing users" },
                new Permission { Name = Permissions.DeleteUsers, DisplayName = "Delete Users", Module = "Admin", Description = "Delete users" },
                new Permission { Name = Permissions.AssignRoles, DisplayName = "Assign Roles", Module = "Admin", Description = "Assign roles to users" },

                // Admin - Role Management
                new Permission { Name = Permissions.ViewRoles, DisplayName = "View Roles", Module = "Admin", Description = "View role list and details" },
                new Permission { Name = Permissions.CreateRoles, DisplayName = "Create Roles", Module = "Admin", Description = "Create new roles" },
                new Permission { Name = Permissions.EditRoles, DisplayName = "Edit Roles", Module = "Admin", Description = "Edit existing roles" },
                new Permission { Name = Permissions.DeleteRoles, DisplayName = "Delete Roles", Module = "Admin", Description = "Delete roles" },
                new Permission { Name = Permissions.ManagePermissions, DisplayName = "Manage Permissions", Module = "Admin", Description = "Assign permissions to roles" },

                // Admin - System Settings
                new Permission { Name = Permissions.ViewSettings, DisplayName = "View Settings", Module = "Admin", Description = "View system settings" },
                new Permission { Name = Permissions.EditSettings, DisplayName = "Edit Settings", Module = "Admin", Description = "Edit system settings" },

                // Admin - Audit Logs
                new Permission { Name = Permissions.ViewAuditLogs, DisplayName = "View Audit Logs", Module = "Admin", Description = "View audit trail" }
            };

            _context.Permissions.AddRange(permissions);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created {permissions.Count} permissions");

            // Create roles
            var roles = new List<Role>
            {
                new Role
                {
                    Name = Roles.SystemAdmin,
                    DisplayName = "System Administrator",
                    Description = "Full system access - can do everything"
                },
                new Role
                {
                    Name = Roles.Admin,
                    DisplayName = "Administrator",
                    Description = "Administrative access with some restrictions"
                },
                new Role
                {
                    Name = Roles.FinanceOfficer,
                    DisplayName = "Finance Officer",
                    Description = "Manages invoices, payments, and financial reports"
                },
                new Role
                {
                    Name = Roles.HealthManager,
                    DisplayName = "Health Manager",
                    Description = "Approves requisitions for outstation facilities"
                },
                new Role
                {
                    Name = Roles.HospitalExecutive,
                    DisplayName = "Hospital Executive Officer",
                    Description = "Approves requisitions for hospital units"
                },
                new Role
                {
                    Name = Roles.FinanceManager,
                    DisplayName = "Finance Manager",
                    Description = "Final approval for financial matters"
                },
                new Role
                {
                    Name = Roles.ProcurementOfficer,
                    DisplayName = "Procurement Officer",
                    Description = "Manages procurement process and purchase orders"
                },
                new Role
                {
                    Name = Roles.OIC,
                    DisplayName = "Officer in Charge (OIC)",
                    Description = "Creates requisitions for facility needs"
                },
                new Role
                {
                    Name = Roles.Supervisor,
                    DisplayName = "Supervisor",
                    Description = "Reviews and forwards requisitions"
                },
                new Role
                {
                    Name = Roles.User,
                    DisplayName = "Standard User",
                    Description = "Basic access to view information"
                }
            };

            _context.Roles.AddRange(roles);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created {roles.Count} roles");

            // Assign permissions to roles
            await AssignRolePermissionsAsync(roles, permissions);

            _logger.LogInformation("Roles and permissions initialization completed");
        }

        private async Task AssignRolePermissionsAsync(List<Role> roles, List<Permission> permissions)
        {
            var rolePermissions = new List<RolePermission>();

            // System Admin - ALL permissions
            var systemAdminRole = roles.First(r => r.Name == Roles.SystemAdmin);
            rolePermissions.AddRange(permissions.Select(p => new RolePermission
            {
                RoleId = systemAdminRole.Id,
                PermissionId = p.Id
            }));

            // Admin - Most permissions except some system-level ones
            var adminRole = roles.First(r => r.Name == Roles.Admin);
            var adminPermissions = permissions.Where(p =>
                !p.Name.Contains("Delete") || p.Module != "Admin"
            ).ToList();
            rolePermissions.AddRange(adminPermissions.Select(p => new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = p.Id
            }));

            // Finance Officer - Invoices, Payments, Reports, AI Import
            var financeOfficerRole = roles.First(r => r.Name == Roles.FinanceOfficer);
            var financePermissions = permissions.Where(p =>
                p.Module == "Dashboard" ||
                p.Module == "Invoices" ||
                p.Module == "Payments" ||
                p.Module == "Reports" ||
                p.Module == "AI Import" ||
                (p.Module == "Suppliers" && !p.Name.Contains("Delete"))
            ).ToList();
            rolePermissions.AddRange(financePermissions.Select(p => new RolePermission
            {
                RoleId = financeOfficerRole.Id,
                PermissionId = p.Id
            }));

            // Health Manager - Approve requisitions, view purchase orders
            var healthManagerRole = roles.First(r => r.Name == Roles.HealthManager);
            var healthManagerPermissions = permissions.Where(p =>
                p.Module == "Dashboard" ||
                p.Name == Permissions.ViewRequisitions ||
                p.Name == Permissions.ApproveRequisitions ||
                p.Name == Permissions.RejectRequisitions ||
                p.Name == Permissions.ViewPurchaseOrders ||
                p.Name == Permissions.ViewSuppliers ||
                p.Name == Permissions.ViewReports
            ).ToList();
            rolePermissions.AddRange(healthManagerPermissions.Select(p => new RolePermission
            {
                RoleId = healthManagerRole.Id,
                PermissionId = p.Id
            }));

            // Hospital Executive - Similar to Health Manager
            var hospitalExecutiveRole = roles.First(r => r.Name == Roles.HospitalExecutive);
            rolePermissions.AddRange(healthManagerPermissions.Select(p => new RolePermission
            {
                RoleId = hospitalExecutiveRole.Id,
                PermissionId = p.Id
            }));

            // Finance Manager - Similar to Health Manager plus financial approvals
            var financeManagerRole = roles.First(r => r.Name == Roles.FinanceManager);
            var financeManagerPermissions = permissions.Where(p =>
                p.Module == "Dashboard" ||
                p.Module == "Reports" ||
                p.Name == Permissions.ViewRequisitions ||
                p.Name == Permissions.ApproveRequisitions ||
                p.Name == Permissions.RejectRequisitions ||
                p.Name == Permissions.ViewPurchaseOrders ||
                p.Name == Permissions.ApprovePurchaseOrders ||
                p.Name == Permissions.ViewInvoices ||
                p.Name == Permissions.ViewPayments ||
                p.Name == Permissions.ViewSuppliers
            ).ToList();
            rolePermissions.AddRange(financeManagerPermissions.Select(p => new RolePermission
            {
                RoleId = financeManagerRole.Id,
                PermissionId = p.Id
            }));

            // Procurement Officer - Full procurement access
            var procurementOfficerRole = roles.First(r => r.Name == Roles.ProcurementOfficer);
            var procurementPermissions = permissions.Where(p =>
                p.Module == "Dashboard" ||
                p.Module == "Procurement" ||
                p.Module == "Suppliers" ||
                p.Module == "Reports" ||
                p.Name == Permissions.ViewInvoices
            ).ToList();
            rolePermissions.AddRange(procurementPermissions.Select(p => new RolePermission
            {
                RoleId = procurementOfficerRole.Id,
                PermissionId = p.Id
            }));

            // OIC - Create and submit requisitions
            var oicRole = roles.First(r => r.Name == Roles.OIC);
            var oicPermissions = permissions.Where(p =>
                p.Module == "Dashboard" ||
                p.Name == Permissions.ViewRequisitions ||
                p.Name == Permissions.CreateRequisitions ||
                p.Name == Permissions.EditRequisitions ||
                p.Name == Permissions.SubmitRequisitions ||
                p.Name == Permissions.ViewPurchaseOrders ||
                p.Name == Permissions.ViewSuppliers
            ).ToList();
            rolePermissions.AddRange(oicPermissions.Select(p => new RolePermission
            {
                RoleId = oicRole.Id,
                PermissionId = p.Id
            }));

            // Supervisor - Review requisitions
            var supervisorRole = roles.First(r => r.Name == Roles.Supervisor);
            var supervisorPermissions = permissions.Where(p =>
                p.Module == "Dashboard" ||
                p.Name == Permissions.ViewRequisitions ||
                p.Name == Permissions.ApproveRequisitions ||
                p.Name == Permissions.RejectRequisitions ||
                p.Name == Permissions.ViewPurchaseOrders ||
                p.Name == Permissions.ViewSuppliers
            ).ToList();
            rolePermissions.AddRange(supervisorPermissions.Select(p => new RolePermission
            {
                RoleId = supervisorRole.Id,
                PermissionId = p.Id
            }));

            // User - Basic read access
            var userRole = roles.First(r => r.Name == Roles.User);
            var userPermissions = permissions.Where(p =>
                p.Module == "Dashboard" ||
                p.Name == Permissions.ViewInvoices ||
                p.Name == Permissions.ViewPayments ||
                p.Name == Permissions.ViewReports ||
                p.Name == Permissions.ViewRequisitions ||
                p.Name == Permissions.ViewPurchaseOrders ||
                p.Name == Permissions.ViewSuppliers
            ).ToList();
            rolePermissions.AddRange(userPermissions.Select(p => new RolePermission
            {
                RoleId = userRole.Id,
                PermissionId = p.Id
            }));

            _context.RolePermissions.AddRange(rolePermissions);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Assigned {rolePermissions.Count} role-permission mappings");
        }
    }
}

