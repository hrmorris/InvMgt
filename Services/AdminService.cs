using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        // User Management
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> CreateUserAsync(User user, string? password = null)
        {
            user.CreatedDate = DateTime.Now;

            // Hash password using BCrypt if provided
            if (!string.IsNullOrEmpty(password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await LogActionAsync(null, "System", "Created", "User", user.Id, $"Created user: {user.FullName}");

            return user;
        }

        public async Task UpdateUserAsync(User user)
        {
            user.ModifiedDate = DateTime.Now;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await LogActionAsync(null, "System", "Updated", "User", user.Id, $"Updated user: {user.FullName}");
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await LogActionAsync(null, "System", "Deleted", "User", id, $"Deleted user: {user.FullName}");
            }
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByFacilityAsync(string facility)
        {
            return await _context.Users
                .Where(u => u.Facility == facility)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllUsersAsync();

            var term = searchTerm.ToLower();
            return await _context.Users
                .Where(u => u.Username.ToLower().Contains(term) ||
                            u.FullName.ToLower().Contains(term) ||
                            u.Email.ToLower().Contains(term) ||
                            u.Department.ToLower().Contains(term) ||
                            u.Facility.ToLower().Contains(term) ||
                            u.Role.ToLower().Contains(term))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogActionAsync(null, "System", "PasswordReset", "User", userId, $"Password reset for user: {user.FullName}");
            }
        }

        public async Task UpdateUserStatusAsync(int userId, string status)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                var oldStatus = user.Status;
                user.Status = status;
                user.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogActionAsync(null, "System", "StatusChanged", "User", userId, $"User {user.FullName} status changed from {oldStatus} to {status}");
            }
        }

        public async Task BulkUpdateStatusAsync(IEnumerable<int> userIds, string status)
        {
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in users)
            {
                user.Status = status;
                user.ModifiedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            await LogActionAsync(null, "System", "BulkStatusUpdate", "User", null, $"Bulk status update to {status} for {users.Count} users");
        }

        public async Task<UserActivityStats> GetUserActivityStatsAsync(int userId)
        {
            var stats = new UserActivityStats();

            var logs = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ActionDate)
                .ToListAsync();

            stats.TotalActions = logs.Count;
            stats.TotalLogins = logs.Count(l => l.Action == "Login");
            stats.FirstLogin = logs.Where(l => l.Action == "Login").LastOrDefault()?.ActionDate;
            stats.LastLogin = logs.Where(l => l.Action == "Login").FirstOrDefault()?.ActionDate;
            stats.InvoicesCreated = logs.Count(l => l.Entity == "Invoice" && l.Action == "Created");
            stats.PaymentsRecorded = logs.Count(l => l.Entity == "Payment" && l.Action == "Created");
            stats.RequisitionsCreated = logs.Count(l => l.Entity == "Requisition" && l.Action == "Created");
            stats.PurchaseOrdersCreated = logs.Count(l => l.Entity == "PurchaseOrder" && l.Action == "Created");

            stats.ActionsByType = logs
                .GroupBy(l => l.Action)
                .ToDictionary(g => g.Key, g => g.Count());

            stats.RecentActivity = logs.Take(20).ToList();

            return stats;
        }

        // Audit Logs
        public async Task LogActionAsync(int? userId, string username, string action, string entity, int? entityId, string? details)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Username = username,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Details = details,
                ActionDate = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetRecentAuditLogsAsync(int count = 100)
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.ActionDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(int userId)
        {
            return await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ActionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entity, int? entityId)
        {
            var query = _context.AuditLogs.Where(a => a.Entity == entity);

            if (entityId.HasValue)
                query = query.Where(a => a.EntityId == entityId);

            return await query
                .OrderByDescending(a => a.ActionDate)
                .ToListAsync();
        }

        // System Settings
        public async Task<IEnumerable<SystemSetting>> GetAllSettingsAsync()
        {
            return await _context.SystemSettings
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SettingKey)
                .ToListAsync();
        }

        public async Task<SystemSetting?> GetSettingByKeyAsync(string key)
        {
            return await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);
        }

        public async Task<string?> GetSettingValueAsync(string key)
        {
            var setting = await GetSettingByKeyAsync(key);
            return setting?.SettingValue;
        }

        public async Task UpdateSettingAsync(string key, string value, string modifiedBy)
        {
            var setting = await GetSettingByKeyAsync(key);
            if (setting != null)
            {
                setting.SettingValue = value;
                setting.ModifiedBy = modifiedBy;
                setting.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<SystemSetting?> GetSettingByIdAsync(int id)
        {
            return await _context.SystemSettings.FindAsync(id);
        }

        public async Task CreateSettingAsync(SystemSetting setting)
        {
            setting.ModifiedDate = DateTime.Now;
            _context.SystemSettings.Add(setting);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSettingAsync(int id)
        {
            var setting = await _context.SystemSettings.FindAsync(id);
            if (setting != null)
            {
                _context.SystemSettings.Remove(setting);
                await _context.SaveChangesAsync();
            }
        }

        public async Task InitializeDefaultSettingsAsync()
        {
            var defaultSettings = new List<SystemSetting>
            {
                // General Settings
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "ApplicationName",
                    SettingValue = "Invoice Management System",
                    Description = "Name of the application displayed in the header and reports",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "DateFormat",
                    SettingValue = "dd/MM/yyyy",
                    Description = "Default date format for the entire application",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "CurrencyCode",
                    SettingValue = "PGK",
                    Description = "Currency code (e.g., USD, EUR, GBP, PGK for Papua New Guinea Kina)",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "CurrencySymbol",
                    SettingValue = "K",
                    Description = "Currency symbol to display (e.g., $, £, €, K)",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "CurrencyName",
                    SettingValue = "Papua New Guinean Kina",
                    Description = "Full currency name",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "CurrencyPosition",
                    SettingValue = "before",
                    Description = "Currency symbol position (before or after amount)",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "DecimalPlaces",
                    SettingValue = "2",
                    Description = "Number of decimal places for currency display",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "ThousandsSeparator",
                    SettingValue = ",",
                    Description = "Thousands separator character (e.g., comma, period, space)",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "DecimalSeparator",
                    SettingValue = ".",
                    Description = "Decimal separator character (e.g., period, comma)",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "General",
                    SettingKey = "TimeZone",
                    SettingValue = "Pacific/Port_Moresby",
                    Description = "Default timezone for the application",
                    ModifiedBy = "System"
                },

                // Company Settings
                new SystemSetting
                {
                    Category = "Company",
                    SettingKey = "CompanyName",
                    SettingValue = "Your Company Name",
                    Description = "Company name for invoices and reports",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Company",
                    SettingKey = "CompanyAddress",
                    SettingValue = "123 Main Street, City, Country",
                    Description = "Company address for invoices and reports",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Company",
                    SettingKey = "CompanyPhone",
                    SettingValue = "+1234567890",
                    Description = "Company phone number",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Company",
                    SettingKey = "CompanyEmail",
                    SettingValue = "info@company.com",
                    Description = "Company email address",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Company",
                    SettingKey = "CompanyWebsite",
                    SettingValue = "www.company.com",
                    Description = "Company website URL",
                    ModifiedBy = "System"
                },

                // Invoice Settings
                new SystemSetting
                {
                    Category = "Invoice",
                    SettingKey = "InvoiceDueDays",
                    SettingValue = "30",
                    Description = "Default number of days until invoice is due",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Invoice",
                    SettingKey = "InvoicePrefix",
                    SettingValue = "INV-",
                    Description = "Prefix for invoice numbers (e.g., INV-0001)",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Invoice",
                    SettingKey = "TaxRate",
                    SettingValue = "15",
                    Description = "Default tax rate percentage",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Invoice",
                    SettingKey = "EnableAutoInvoiceNumbers",
                    SettingValue = "true",
                    Description = "Automatically generate sequential invoice numbers",
                    ModifiedBy = "System"
                },

                // Payment Settings
                new SystemSetting
                {
                    Category = "Payment",
                    SettingKey = "PaymentPrefix",
                    SettingValue = "PAY-",
                    Description = "Prefix for payment numbers (e.g., PAY-0001)",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Payment",
                    SettingKey = "EnablePaymentReminders",
                    SettingValue = "true",
                    Description = "Send automatic payment reminders for overdue invoices",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Payment",
                    SettingKey = "PaymentReminderDays",
                    SettingValue = "7",
                    Description = "Days before due date to send payment reminders",
                    ModifiedBy = "System"
                },

                // Procurement Settings
                new SystemSetting
                {
                    Category = "Procurement",
                    SettingKey = "RequisitionPrefix",
                    SettingValue = "REQ-",
                    Description = "Prefix for requisition numbers",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Procurement",
                    SettingKey = "PurchaseOrderPrefix",
                    SettingValue = "PO-",
                    Description = "Prefix for purchase order numbers",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Procurement",
                    SettingKey = "RequireApproval",
                    SettingValue = "true",
                    Description = "Require approval for requisitions and purchase orders",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Procurement",
                    SettingKey = "ApprovalThreshold",
                    SettingValue = "1000",
                    Description = "Amount threshold requiring additional approval",
                    ModifiedBy = "System"
                },

                // Email Settings
                new SystemSetting
                {
                    Category = "Email",
                    SettingKey = "SMTPServer",
                    SettingValue = "smtp.gmail.com",
                    Description = "SMTP server for sending emails",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Email",
                    SettingKey = "SMTPPort",
                    SettingValue = "587",
                    Description = "SMTP server port",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Email",
                    SettingKey = "EmailFrom",
                    SettingValue = "noreply@company.com",
                    Description = "Default sender email address",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Email",
                    SettingKey = "EnableEmailNotifications",
                    SettingValue = "true",
                    Description = "Send email notifications for important events",
                    ModifiedBy = "System"
                },

                // Security Settings
                new SystemSetting
                {
                    Category = "Security",
                    SettingKey = "SessionTimeout",
                    SettingValue = "120",
                    Description = "Session timeout in minutes",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Security",
                    SettingKey = "PasswordMinLength",
                    SettingValue = "8",
                    Description = "Minimum password length",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Security",
                    SettingKey = "EnableAuditLogging",
                    SettingValue = "true",
                    Description = "Log all user actions for audit trail",
                    ModifiedBy = "System"
                },

                // API Settings
                new SystemSetting
                {
                    Category = "API",
                    SettingKey = "MaxUploadSize",
                    SettingValue = "7",
                    Description = "Maximum file upload size in MB for AI processing",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "API",
                    SettingKey = "AIModel",
                    SettingValue = "gpt-5.2",
                    Description = "OpenAI model for document processing",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "API",
                    SettingKey = "OpenAIApiKey",
                    SettingValue = "",
                    Description = "OpenAI API key for AI-powered invoice/payment import. Get from https://platform.openai.com/api-keys",
                    ModifiedBy = "System"
                },

                // Maintenance Settings
                new SystemSetting
                {
                    Category = "Maintenance",
                    SettingKey = "MaintenanceMode_Enabled",
                    SettingValue = "false",
                    Description = "Enable maintenance mode to block non-admin users from accessing the system",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Maintenance",
                    SettingKey = "MaintenanceMode_Message",
                    SettingValue = "The system is currently undergoing scheduled maintenance. We'll be back shortly.",
                    Description = "Message displayed to users during maintenance",
                    ModifiedBy = "System"
                },
                new SystemSetting
                {
                    Category = "Maintenance",
                    SettingKey = "MaintenanceMode_EndTime",
                    SettingValue = "",
                    Description = "Estimated end time for maintenance (e.g., 2025-01-15T14:00). Leave empty if unknown.",
                    ModifiedBy = "System"
                }
            };

            // Only add settings that don't already exist
            foreach (var setting in defaultSettings)
            {
                var exists = await _context.SystemSettings
                    .AnyAsync(s => s.SettingKey == setting.SettingKey);

                if (!exists)
                {
                    setting.ModifiedDate = DateTime.Now;
                    _context.SystemSettings.Add(setting);
                }
            }

            await _context.SaveChangesAsync();
        }

        // Dashboard Statistics
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var today = DateTime.Today;

            // Get unpaid invoices to calculate balance
            var unpaidInvoices = await _context.Invoices
                .Where(i => i.Status != "Paid")
                .Select(i => new { i.TotalAmount, i.PaidAmount })
                .ToListAsync();

            var totalUnpaidAmount = unpaidInvoices.Sum(i => i.TotalAmount - i.PaidAmount);

            return new DashboardStats
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.Status == "Active"),
                TotalSuppliers = await _context.Suppliers.CountAsync(),
                ActiveSuppliers = await _context.Suppliers.CountAsync(s => s.Status == "Active"),
                TotalRequisitions = await _context.Requisitions.CountAsync(),
                PendingRequisitions = await _context.Requisitions.CountAsync(r =>
                    r.Status == "Pending_Supervisor" ||
                    r.Status == "Pending_Finance" ||
                    r.Status == "Pending_Approval"),
                TotalPurchaseOrders = await _context.PurchaseOrders.CountAsync(),
                OpenPurchaseOrders = await _context.PurchaseOrders.CountAsync(po =>
                    po.Status != "Fully_Received" && po.Status != "Cancelled"),
                TotalInvoices = await _context.Invoices.CountAsync(),
                UnpaidInvoices = unpaidInvoices.Count,
                TotalUnpaidAmount = totalUnpaidAmount,
                TodayActions = await _context.AuditLogs.CountAsync(a => a.ActionDate >= today)
            };
        }
    }
}

