using Microsoft.AspNetCore.Mvc;
using InvoiceManagement.Models;
using InvoiceManagement.Models.ViewModels;
using InvoiceManagement.Services;
using InvoiceManagement.Data;
using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Authorization;

namespace InvoiceManagement.Controllers
{
    [AuthorizeRoles(Roles.SystemAdmin, Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ISupplierService _supplierService;
        private readonly ICustomerService _customerService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(IAdminService adminService, ISupplierService supplierService, ICustomerService customerService, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _adminService = adminService;
            _supplierService = supplierService;
            _customerService = customerService;
            _context = context;
            _environment = environment;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Index()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            var recentLogs = await _adminService.GetRecentAuditLogsAsync(10);

            ViewBag.RecentLogs = recentLogs;
            return View(stats);
        }

        // ====================  USERS MANAGEMENT ====================

        // GET: Admin/Users
        public async Task<IActionResult> Users(string? role, string? status)
        {
            IEnumerable<User> users;

            if (!string.IsNullOrEmpty(role))
                users = await _adminService.GetUsersByRoleAsync(role);
            else
                users = await _adminService.GetAllUsersAsync();

            if (!string.IsNullOrEmpty(status))
                users = users.Where(u => u.Status == status);

            ViewBag.Role = role;
            ViewBag.Status = status;
            return View(users);
        }

        // GET: Admin/CreateUser
        public IActionResult CreateUser()
        {
            return View(new User());
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User user)
        {
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                await _adminService.CreateUserAsync(user);
                TempData["SuccessMessage"] = $"User {user.FullName} created successfully!";
                return RedirectToAction(nameof(Users));
            }
            return View(user);
        }

        // GET: Admin/EditUser/5
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Admin/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User user)
        {
            if (id != user.Id)
                return NotFound();

            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                await _adminService.UpdateUserAsync(user);
                TempData["SuccessMessage"] = $"User {user.FullName} updated successfully!";
                return RedirectToAction(nameof(Users));
            }
            return View(user);
        }

        // GET: Admin/DeleteUser/5
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            await _adminService.DeleteUserAsync(id);
            TempData["SuccessMessage"] = "User deleted successfully!";
            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/UserProfile/5
        public async Task<IActionResult> UserProfile(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            var activityStats = await _adminService.GetUserActivityStatsAsync(id);
            var auditLogs = await _adminService.GetAuditLogsByUserAsync(id);

            ViewBag.ActivityStats = activityStats;
            ViewBag.AuditLogs = auditLogs.Take(50);
            return View(user);
        }

        // GET: Admin/ResetPassword/5
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Admin/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Password must be at least 6 characters long.";
                return RedirectToAction(nameof(ResetPassword), new { id });
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction(nameof(ResetPassword), new { id });
            }

            await _adminService.ResetPasswordAsync(id, newPassword);
            TempData["SuccessMessage"] = "Password has been reset successfully!";
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/ToggleUserStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            var newStatus = user.Status == "Active" ? "Inactive" : "Active";
            await _adminService.UpdateUserStatusAsync(id, newStatus);
            TempData["SuccessMessage"] = $"User {user.FullName} is now {newStatus}.";
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/SuspendUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            await _adminService.UpdateUserStatusAsync(id, "Suspended");
            TempData["SuccessMessage"] = $"User {user.FullName} has been suspended.";
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/ActivateUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            await _adminService.UpdateUserStatusAsync(id, "Active");
            TempData["SuccessMessage"] = $"User {user.FullName} has been activated.";
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/BulkUpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateStatus(int[] userIds, string status)
        {
            if (userIds == null || userIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            await _adminService.BulkUpdateStatusAsync(userIds, status);
            TempData["SuccessMessage"] = $"{userIds.Length} user(s) updated to {status}.";
            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/ExportUsers
        public async Task<IActionResult> ExportUsers(string? role, string? status)
        {
            IEnumerable<User> users;

            if (!string.IsNullOrEmpty(role))
                users = await _adminService.GetUsersByRoleAsync(role);
            else
                users = await _adminService.GetAllUsersAsync();

            if (!string.IsNullOrEmpty(status))
                users = users.Where(u => u.Status == status);

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,Username,FullName,Email,Phone,Department,Facility,FacilityType,Role,Status,CreatedDate,LastLoginDate");

            foreach (var user in users)
            {
                csv.AppendLine($"{user.Id},\"{user.Username}\",\"{user.FullName}\",\"{user.Email}\",\"{user.Phone ?? ""}\",\"{user.Department}\",\"{user.Facility}\",\"{user.FacilityType}\",\"{user.Role}\",\"{user.Status}\",\"{user.CreatedDate:yyyy-MM-dd}\",\"{user.LastLoginDate?.ToString("yyyy-MM-dd") ?? "Never"}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // GET: Admin/SearchUsers
        public async Task<IActionResult> SearchUsers(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction(nameof(Users));

            var users = await _adminService.SearchUsersAsync(q);
            ViewBag.SearchTerm = q;
            return View("Users", users);
        }

        // ==================== SUPPLIERS MANAGEMENT ====================

        // GET: Admin/Suppliers
        public async Task<IActionResult> Suppliers()
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            return View(suppliers);
        }

        // GET: Admin/CreateSupplier
        public IActionResult CreateSupplier()
        {
            return View(new Supplier());
        }

        // POST: Admin/CreateSupplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                await _supplierService.CreateSupplierAsync(supplier);
                TempData["SuccessMessage"] = $"Supplier {supplier.SupplierName} created successfully!";
                return RedirectToAction(nameof(Suppliers));
            }
            return View(supplier);
        }

        // GET: Admin/ViewSupplier/5
        public async Task<IActionResult> ViewSupplier(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        // GET: Admin/EditSupplier/5
        public async Task<IActionResult> EditSupplier(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        // POST: Admin/EditSupplier/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(int id, Supplier supplier)
        {
            if (id != supplier.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                await _supplierService.UpdateSupplierAsync(supplier);
                TempData["SuccessMessage"] = $"Supplier {supplier.SupplierName} updated successfully!";
                return RedirectToAction(nameof(Suppliers));
            }
            return View(supplier);
        }

        // GET: Admin/SupplierDetails/5
        public async Task<IActionResult> SupplierDetails(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        // GET: Admin/DeleteSupplier/5
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        // POST: Admin/DeleteSupplier/5
        [HttpPost, ActionName("DeleteSupplier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplierConfirmed(int id)
        {
            await _supplierService.DeleteSupplierAsync(id);
            TempData["SuccessMessage"] = "Supplier deleted successfully!";
            return RedirectToAction(nameof(Suppliers));
        }

        // ==================== CUSTOMERS MANAGEMENT ====================

        // GET: Admin/Customers
        public async Task<IActionResult> Customers()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return View(customers);
        }

        // GET: Admin/CreateCustomer
        public IActionResult CreateCustomer()
        {
            return View(new Customer());
        }

        // POST: Admin/CreateCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerService.CreateCustomerAsync(customer);
                TempData["SuccessMessage"] = $"Customer {customer.CustomerName} created successfully!";
                return RedirectToAction(nameof(Customers));
            }
            return View(customer);
        }

        // GET: Admin/ViewCustomer/5
        public async Task<IActionResult> ViewCustomer(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // GET: Admin/EditCustomer/5
        public async Task<IActionResult> EditCustomer(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Admin/EditCustomer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(int id, Customer customer)
        {
            if (id != customer.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                await _customerService.UpdateCustomerAsync(customer);
                TempData["SuccessMessage"] = $"Customer {customer.CustomerName} updated successfully!";
                return RedirectToAction(nameof(Customers));
            }
            return View(customer);
        }

        // GET: Admin/DeleteCustomer/5
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Admin/DeleteCustomer/5
        [HttpPost, ActionName("DeleteCustomer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomerConfirmed(int id)
        {
            await _customerService.DeleteCustomerAsync(id);
            TempData["SuccessMessage"] = "Customer deleted successfully!";
            return RedirectToAction(nameof(Customers));
        }

        // ==================== AUDIT LOGS ====================

        // GET: Admin/AuditLogs
        public async Task<IActionResult> AuditLogs(string? entity, int? userId)
        {
            IEnumerable<AuditLog> logs;

            if (!string.IsNullOrEmpty(entity))
                logs = await _adminService.GetAuditLogsByEntityAsync(entity, null);
            else if (userId.HasValue)
                logs = await _adminService.GetAuditLogsByUserAsync(userId.Value);
            else
                logs = await _adminService.GetRecentAuditLogsAsync(200);

            ViewBag.Entity = entity;
            ViewBag.UserId = userId;
            return View(logs);
        }

        // ==================== SYSTEM SETTINGS ====================

        // GET: Admin/Settings
        public async Task<IActionResult> Settings()
        {
            var settings = await _adminService.GetAllSettingsAsync();
            return View(settings);
        }

        // GET: Admin/EditSetting/id
        public async Task<IActionResult> EditSetting(int id)
        {
            var setting = await _adminService.GetSettingByIdAsync(id);
            if (setting == null)
                return NotFound();

            return View(setting);
        }

        // POST: Admin/EditSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSetting(string key, string value)
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";

            // If CurrencyCode is changed, automatically update CurrencySymbol and CurrencyName
            if (key == "CurrencyCode")
            {
                var currency = Currency.GetByCode(value);
                if (currency != null)
                {
                    await _adminService.UpdateSettingAsync("CurrencyCode", currency.Code, username);
                    await _adminService.UpdateSettingAsync("CurrencySymbol", currency.Symbol, username);
                    await _adminService.UpdateSettingAsync("CurrencyName", currency.Name, username);
                    TempData["SuccessMessage"] = $"Currency updated to {currency.Name} ({currency.Code}) with symbol {currency.Symbol}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid currency code";
                }
            }
            else
            {
                await _adminService.UpdateSettingAsync(key, value, username);
                TempData["SuccessMessage"] = "Setting updated successfully!";
            }

            return RedirectToAction(nameof(Settings));
        }

        // GET: Admin/CreateSetting
        public IActionResult CreateSetting()
        {
            return View(new SystemSetting());
        }

        // POST: Admin/CreateSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSetting(SystemSetting setting)
        {
            if (ModelState.IsValid)
            {
                setting.ModifiedBy = User.Identity?.Name ?? "Admin";
                setting.ModifiedDate = DateTime.Now;
                await _adminService.CreateSettingAsync(setting);
                TempData["SuccessMessage"] = $"Setting '{setting.SettingKey}' created successfully!";
                return RedirectToAction(nameof(Settings));
            }
            return View(setting);
        }

        // POST: Admin/DeleteSetting/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSetting(int id)
        {
            var setting = await _adminService.GetSettingByIdAsync(id);
            if (setting != null)
            {
                await _adminService.DeleteSettingAsync(id);
                TempData["SuccessMessage"] = $"Setting '{setting.SettingKey}' deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Setting not found.";
            }
            return RedirectToAction(nameof(Settings));
        }

        // POST: Admin/InitializeDefaultSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitializeDefaultSettings()
        {
            try
            {
                await _adminService.InitializeDefaultSettingsAsync();
                TempData["SuccessMessage"] = "Default settings initialized successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error initializing settings: {ex.Message}";
            }
            return RedirectToAction(nameof(Settings));
        }

        // ====================  CURRENCY SETTINGS ====================

        // GET: Admin/CurrencySettings
        public async Task<IActionResult> CurrencySettings()
        {
            var settings = await _adminService.GetAllSettingsAsync();

            // Get currency-related settings
            ViewBag.CurrencyCode = settings.FirstOrDefault(s => s.SettingKey == "CurrencyCode")?.SettingValue ?? "PGK";
            ViewBag.CurrencySymbol = settings.FirstOrDefault(s => s.SettingKey == "CurrencySymbol")?.SettingValue ?? "K";
            ViewBag.CurrencyName = settings.FirstOrDefault(s => s.SettingKey == "CurrencyName")?.SettingValue ?? "Papua New Guinean Kina";
            ViewBag.CurrencyPosition = settings.FirstOrDefault(s => s.SettingKey == "CurrencyPosition")?.SettingValue ?? "before";
            ViewBag.DecimalPlaces = settings.FirstOrDefault(s => s.SettingKey == "DecimalPlaces")?.SettingValue ?? "2";
            ViewBag.ThousandsSeparator = settings.FirstOrDefault(s => s.SettingKey == "ThousandsSeparator")?.SettingValue ?? ",";
            ViewBag.DecimalSeparator = settings.FirstOrDefault(s => s.SettingKey == "DecimalSeparator")?.SettingValue ?? ".";

            // Get all available currencies
            ViewBag.AllCurrencies = Currency.GetAllCurrencies();
            ViewBag.AllRegions = Currency.GetAllRegions();

            return View();
        }

        // POST: Admin/UpdateCurrency
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCurrency(string currencyCode)
        {
            try
            {
                var currency = Currency.GetByCode(currencyCode);
                if (currency == null)
                {
                    TempData["ErrorMessage"] = "Invalid currency code";
                    return RedirectToAction(nameof(CurrencySettings));
                }

                var settings = await _adminService.GetAllSettingsAsync();
                var username = HttpContext.Session.GetString("Username") ?? "System";

                // Update CurrencyCode
                await _adminService.UpdateSettingAsync("CurrencyCode", currency.Code, username);

                // Update CurrencySymbol
                await _adminService.UpdateSettingAsync("CurrencySymbol", currency.Symbol, username);

                // Update CurrencyName
                await _adminService.UpdateSettingAsync("CurrencyName", currency.Name, username);

                TempData["SuccessMessage"] = $"Currency updated to {currency.Name} ({currency.Code})";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating currency: {ex.Message}";
            }

            return RedirectToAction(nameof(CurrencySettings));
        }

        // POST: Admin/UpdateCurrencyFormat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCurrencyFormat(
            string currencyPosition,
            string decimalPlaces,
            string thousandsSeparator,
            string decimalSeparator)
        {
            try
            {
                var settings = await _adminService.GetAllSettingsAsync();
                var username = HttpContext.Session.GetString("Username") ?? "System";

                // Update formatting settings
                var updates = new Dictionary<string, string>
                {
                    { "CurrencyPosition", currencyPosition },
                    { "DecimalPlaces", decimalPlaces },
                    { "ThousandsSeparator", thousandsSeparator },
                    { "DecimalSeparator", decimalSeparator }
                };

                foreach (var update in updates)
                {
                    await _adminService.UpdateSettingAsync(update.Key, update.Value, username);
                }

                TempData["SuccessMessage"] = "Currency formatting updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating format: {ex.Message}";
            }

            return RedirectToAction(nameof(CurrencySettings));
        }

        // ==================== SYSTEM HEALTH & DIAGNOSTICS ====================

        // GET: Admin/SystemHealth
        public async Task<IActionResult> SystemHealth()
        {
            var healthData = new SystemHealthData
            {
                DatabaseStatus = "Connected",
                DatabaseSize = await GetDatabaseSizeAsync(),
                TotalInvoices = await _context.Invoices.CountAsync(),
                TotalPayments = await _context.Payments.CountAsync(),
                TotalSuppliers = await _context.Suppliers.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                TotalAuditLogs = await _context.AuditLogs.CountAsync(),
                TotalDocuments = await _context.ImportedDocuments.CountAsync(),
                ServerTime = DateTime.Now,
                Uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime,
                MemoryUsage = GC.GetTotalMemory(false) / (1024 * 1024), // MB
                LastBackup = null, // Can be tracked in settings
                OldestUnpaidInvoice = await _context.Invoices.Where(i => i.Status != "Paid").OrderBy(i => i.InvoiceDate).FirstOrDefaultAsync(),
                RecentErrors = new List<string>()
            };

            // Check for potential issues
            healthData.Warnings = new List<string>();

            var overdueCount = await _context.Invoices.CountAsync(i => i.DueDate < DateTime.Today && i.Status != "Paid");
            if (overdueCount > 0)
                healthData.Warnings.Add($"{overdueCount} overdue invoices need attention");

            var unallocatedPayments = await _context.Payments.CountAsync(p => p.Status == "Unallocated");
            if (unallocatedPayments > 0)
                healthData.Warnings.Add($"{unallocatedPayments} unallocated payments");

            var pendingDocuments = await _context.ImportedDocuments.CountAsync(d => d.ProcessingStatus == "Pending");
            if (pendingDocuments > 0)
                healthData.Warnings.Add($"{pendingDocuments} documents pending processing");

            return View(healthData);
        }

        private Task<string> GetDatabaseSizeAsync()
        {
            try
            {
                var dbPath = Path.Combine(_environment.ContentRootPath, "Data", "InvoiceManagement.db");
                if (System.IO.File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                    return Task.FromResult($"{sizeInMB:F2} MB");
                }
                return Task.FromResult("Unknown");
            }
            catch
            {
                return Task.FromResult("Unable to determine");
            }
        }

        // ==================== DATABASE BACKUP & MAINTENANCE ====================

        // GET: Admin/Backup
        public IActionResult Backup()
        {
            var backupPath = Path.Combine(_environment.ContentRootPath, "Backups");
            var backups = new List<BackupInfo>();

            if (Directory.Exists(backupPath))
            {
                var files = Directory.GetFiles(backupPath, "*.db")
                    .Union(Directory.GetFiles(backupPath, "*.zip"));

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    backups.Add(new BackupInfo
                    {
                        FileName = fileInfo.Name,
                        FilePath = file,
                        Size = fileInfo.Length,
                        CreatedDate = fileInfo.CreationTime
                    });
                }
            }

            ViewBag.Backups = backups.OrderByDescending(b => b.CreatedDate).ToList();
            return View();
        }

        // POST: Admin/CreateBackup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                var backupPath = Path.Combine(_environment.ContentRootPath, "Backups");
                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"InvoiceManagement_Backup_{timestamp}.db";
                var backupFilePath = Path.Combine(backupPath, backupFileName);

                var sourcePath = Path.Combine(_environment.ContentRootPath, "Data", "InvoiceManagement.db");

                if (System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Copy(sourcePath, backupFilePath, true);
                    TempData["SuccessMessage"] = $"Backup created successfully: {backupFileName}";

                    // Log the backup action
                    var username = HttpContext.Session.GetString("Username") ?? "System";
                    await _adminService.LogActionAsync(null, username, "Create", "Backup", null, $"Created backup: {backupFileName}");
                }
                else
                {
                    TempData["ErrorMessage"] = "Source database not found";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Backup failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Backup));
        }

        // GET: Admin/DownloadBackup
        public IActionResult DownloadBackup(string fileName)
        {
            var backupPath = Path.Combine(_environment.ContentRootPath, "Backups", fileName);
            if (System.IO.File.Exists(backupPath))
            {
                var bytes = System.IO.File.ReadAllBytes(backupPath);
                return File(bytes, "application/octet-stream", fileName);
            }

            TempData["ErrorMessage"] = "Backup file not found";
            return RedirectToAction(nameof(Backup));
        }

        // POST: Admin/DeleteBackup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBackup(string fileName)
        {
            try
            {
                var backupPath = Path.Combine(_environment.ContentRootPath, "Backups", fileName);
                if (System.IO.File.Exists(backupPath))
                {
                    System.IO.File.Delete(backupPath);
                    TempData["SuccessMessage"] = $"Backup deleted: {fileName}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Backup file not found";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Delete failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Backup));
        }

        // ==================== DATA CLEANUP & ARCHIVE ====================

        // GET: Admin/DataCleanup
        public async Task<IActionResult> DataCleanup()
        {
            var cleanupStats = new DataCleanupStats
            {
                OldAuditLogsCount = await _context.AuditLogs.CountAsync(l => l.ActionDate < DateTime.Now.AddMonths(-6)),
                ProcessedDocumentsCount = await _context.ImportedDocuments.CountAsync(d => d.ProcessingStatus == "Processed" && d.ProcessedDate < DateTime.Now.AddMonths(-3)),
                PaidInvoicesOlderThan1Year = await _context.Invoices.CountAsync(i => i.Status == "Paid" && i.InvoiceDate < DateTime.Now.AddYears(-1)),
                FullyAllocatedPaymentsOlderThan1Year = await _context.Payments.CountAsync(p => p.Status == "Fully Allocated" && p.PaymentDate < DateTime.Now.AddYears(-1)),
                TotalDocumentStorageBytes = await _context.ImportedDocuments.SumAsync(d => d.FileSize)
            };

            return View(cleanupStats);
        }

        // POST: Admin/CleanupAuditLogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupAuditLogs(int monthsOld = 6)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddMonths(-monthsOld);
                var oldLogs = await _context.AuditLogs.Where(l => l.ActionDate < cutoffDate).ToListAsync();
                var count = oldLogs.Count;

                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Deleted {count} audit logs older than {monthsOld} months";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Cleanup failed: {ex.Message}";
            }

            return RedirectToAction(nameof(DataCleanup));
        }

        // ==================== ROLE PERMISSIONS ====================

        // GET: Admin/RolePermissions
        public IActionResult RolePermissions()
        {
            var roles = new List<RolePermissionInfo>
            {
                new RolePermissionInfo
                {
                    RoleName = "Admin",
                    Description = "Full system access - can manage all aspects of the system",
                    Permissions = new List<string> { "All Permissions", "User Management", "System Settings", "Backup/Restore", "Audit Logs", "All CRUD Operations" }
                },
                new RolePermissionInfo
                {
                    RoleName = "Finance_Manager",
                    Description = "Finance department manager - can approve payments and view reports",
                    Permissions = new List<string> { "View Invoices", "Create/Edit Invoices", "View Payments", "Create/Edit Payments", "Approve Requisitions", "View Reports", "Export Data" }
                },
                new RolePermissionInfo
                {
                    RoleName = "Finance_Officer",
                    Description = "Finance staff - can process invoices and payments",
                    Permissions = new List<string> { "View Invoices", "Create/Edit Invoices", "View Payments", "Create/Edit Payments", "View Reports" }
                },
                new RolePermissionInfo
                {
                    RoleName = "Health_Manager",
                    Description = "Health facility manager - can create requisitions and view reports",
                    Permissions = new List<string> { "View Invoices", "Create Requisitions", "Approve Requisitions (Level 1)", "View Reports", "View Purchase Orders" }
                },
                new RolePermissionInfo
                {
                    RoleName = "Supervisor",
                    Description = "Department supervisor - can approve requisitions at first level",
                    Permissions = new List<string> { "View Invoices", "Create Requisitions", "Approve Requisitions (Level 1)", "View Reports" }
                },
                new RolePermissionInfo
                {
                    RoleName = "Staff",
                    Description = "General staff - basic view and create access",
                    Permissions = new List<string> { "View Own Invoices", "Create Requisitions", "View Own Reports" }
                },
                new RolePermissionInfo
                {
                    RoleName = "Viewer",
                    Description = "Read-only access - can only view data",
                    Permissions = new List<string> { "View Invoices", "View Payments", "View Reports" }
                }
            };

            return View(roles);
        }

        // ==================== EMAIL CONFIGURATION ====================

        // GET: Admin/EmailSettings
        public async Task<IActionResult> EmailSettings()
        {
            var settings = await _adminService.GetAllSettingsAsync();

            ViewBag.SmtpHost = settings.FirstOrDefault(s => s.SettingKey == "SmtpHost")?.SettingValue ?? "";
            ViewBag.SmtpPort = settings.FirstOrDefault(s => s.SettingKey == "SmtpPort")?.SettingValue ?? "587";
            ViewBag.SmtpUsername = settings.FirstOrDefault(s => s.SettingKey == "SmtpUsername")?.SettingValue ?? "";
            ViewBag.SmtpPassword = settings.FirstOrDefault(s => s.SettingKey == "SmtpPassword")?.SettingValue ?? "";
            ViewBag.SmtpFromEmail = settings.FirstOrDefault(s => s.SettingKey == "SmtpFromEmail")?.SettingValue ?? "";
            ViewBag.SmtpFromName = settings.FirstOrDefault(s => s.SettingKey == "SmtpFromName")?.SettingValue ?? "Invoice Management System";
            ViewBag.SmtpEnableSsl = settings.FirstOrDefault(s => s.SettingKey == "SmtpEnableSsl")?.SettingValue ?? "true";

            return View();
        }

        // POST: Admin/UpdateEmailSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmailSettings(string smtpHost, string smtpPort, string smtpUsername, string smtpPassword, string smtpFromEmail, string smtpFromName, bool smtpEnableSsl)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username") ?? "System";

                await _adminService.UpdateSettingAsync("SmtpHost", smtpHost, username);
                await _adminService.UpdateSettingAsync("SmtpPort", smtpPort, username);
                await _adminService.UpdateSettingAsync("SmtpUsername", smtpUsername, username);
                if (!string.IsNullOrEmpty(smtpPassword))
                    await _adminService.UpdateSettingAsync("SmtpPassword", smtpPassword, username);
                await _adminService.UpdateSettingAsync("SmtpFromEmail", smtpFromEmail, username);
                await _adminService.UpdateSettingAsync("SmtpFromName", smtpFromName, username);
                await _adminService.UpdateSettingAsync("SmtpEnableSsl", smtpEnableSsl.ToString().ToLower(), username);

                TempData["SuccessMessage"] = "Email settings updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating email settings: {ex.Message}";
            }

            return RedirectToAction(nameof(EmailSettings));
        }

        // POST: Admin/TestEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestEmail(string testEmailAddress)
        {
            try
            {
                // In a real implementation, this would send a test email
                // For now, just validate settings exist
                var settings = await _adminService.GetAllSettingsAsync();
                var smtpHost = settings.FirstOrDefault(s => s.SettingKey == "SmtpHost")?.SettingValue;

                if (string.IsNullOrEmpty(smtpHost))
                {
                    TempData["ErrorMessage"] = "SMTP host is not configured";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Test email would be sent to {testEmailAddress} (Email sending not yet implemented)";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Test failed: {ex.Message}";
            }

            return RedirectToAction(nameof(EmailSettings));
        }

        // ==================== ANALYTICS DASHBOARD ====================

        // GET: Admin/Analytics
        public async Task<IActionResult> Analytics()
        {
            var startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var analytics = new AdminAnalytics
            {
                // Invoice Analytics
                TotalInvoicesThisYear = await _context.Invoices.CountAsync(i => i.InvoiceDate >= startOfYear),
                TotalInvoicesThisMonth = await _context.Invoices.CountAsync(i => i.InvoiceDate >= startOfMonth),
                TotalInvoiceAmountThisYear = await _context.Invoices.Where(i => i.InvoiceDate >= startOfYear).SumAsync(i => i.TotalAmount),
                TotalInvoiceAmountThisMonth = await _context.Invoices.Where(i => i.InvoiceDate >= startOfMonth).SumAsync(i => i.TotalAmount),

                // Payment Analytics
                TotalPaymentsThisYear = await _context.Payments.CountAsync(p => p.PaymentDate >= startOfYear),
                TotalPaymentsThisMonth = await _context.Payments.CountAsync(p => p.PaymentDate >= startOfMonth),
                TotalPaymentAmountThisYear = await _context.Payments.Where(p => p.PaymentDate >= startOfYear).SumAsync(p => p.Amount),
                TotalPaymentAmountThisMonth = await _context.Payments.Where(p => p.PaymentDate >= startOfMonth).SumAsync(p => p.Amount),

                // User Activity
                ActiveUsersThisMonth = await _context.Users.CountAsync(u => u.LastLoginDate >= startOfMonth),
                NewUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedDate >= startOfMonth),

                // Monthly Trends (last 12 months)
                MonthlyInvoiceTrends = await GetMonthlyInvoiceTrendsAsync(),
                MonthlyPaymentTrends = await GetMonthlyPaymentTrendsAsync(),

                // Top Suppliers by Invoice Amount
                TopSuppliers = await GetTopSuppliersAsync(),

                // Invoice Status Distribution
                InvoiceStatusDistribution = await GetInvoiceStatusDistributionAsync(),

                // Payment Method Distribution
                PaymentMethodDistribution = await GetPaymentMethodDistributionAsync()
            };

            return View(analytics);
        }

        private async Task<List<MonthlyTrend>> GetMonthlyInvoiceTrendsAsync()
        {
            var trends = new List<MonthlyTrend>();
            for (int i = 11; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                var count = await _context.Invoices.CountAsync(inv => inv.InvoiceDate >= startOfMonth && inv.InvoiceDate < endOfMonth);
                var amount = await _context.Invoices.Where(inv => inv.InvoiceDate >= startOfMonth && inv.InvoiceDate < endOfMonth).SumAsync(inv => inv.TotalAmount);

                trends.Add(new MonthlyTrend
                {
                    Month = startOfMonth.ToString("MMM yyyy"),
                    Count = count,
                    Amount = amount
                });
            }
            return trends;
        }

        private async Task<List<MonthlyTrend>> GetMonthlyPaymentTrendsAsync()
        {
            var trends = new List<MonthlyTrend>();
            for (int i = 11; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                var count = await _context.Payments.CountAsync(p => p.PaymentDate >= startOfMonth && p.PaymentDate < endOfMonth);
                var amount = await _context.Payments.Where(p => p.PaymentDate >= startOfMonth && p.PaymentDate < endOfMonth).SumAsync(p => p.Amount);

                trends.Add(new MonthlyTrend
                {
                    Month = startOfMonth.ToString("MMM yyyy"),
                    Count = count,
                    Amount = amount
                });
            }
            return trends;
        }

        private async Task<List<TopSupplierInfo>> GetTopSuppliersAsync()
        {
            return await _context.Invoices
                .Where(i => i.SupplierId.HasValue)
                .GroupBy(i => new { i.SupplierId, i.Supplier!.SupplierName })
                .Select(g => new TopSupplierInfo
                {
                    SupplierId = g.Key.SupplierId ?? 0,
                    SupplierName = g.Key.SupplierName ?? "Unknown",
                    InvoiceCount = g.Count(),
                    TotalAmount = g.Sum(i => i.TotalAmount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .Take(10)
                .ToListAsync();
        }

        private async Task<Dictionary<string, int>> GetInvoiceStatusDistributionAsync()
        {
            return await _context.Invoices
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        private async Task<Dictionary<string, decimal>> GetPaymentMethodDistributionAsync()
        {
            return await _context.Payments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(x => x.Method, x => x.Total);
        }

        // ==================== DATA MIGRATION ====================

        // GET: Admin/MigrateData
        [HttpGet]
        public IActionResult MigrateData()
        {
            return View();
        }

        // POST: Admin/RunMigration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunMigration()
        {
            try
            {
                // Get paths
                var sqlitePath = Path.Combine(_environment.ContentRootPath, "Data", "InvoiceManagement.db");

                if (!System.IO.File.Exists(sqlitePath))
                {
                    return Json(new { success = false, message = $"SQLite database not found at: {sqlitePath}" });
                }

                var postgresConnection = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(postgresConnection))
                {
                    return Json(new { success = false, message = "PostgreSQL connection string not found" });
                }

                var migrator = new DataMigrator(sqlitePath, postgresConnection);
                var result = await migrator.MigrateAllDataAsync();

                return Json(new { success = true, message = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Migration failed: {ex.Message}" });
            }
        }

        // GET: Admin/CheckDocuments - Temporary diagnostic endpoint
        [HttpGet]
        public async Task<IActionResult> CheckDocuments()
        {
            var totalDocs = await _context.ImportedDocuments.CountAsync();
            var withContent = await _context.ImportedDocuments.CountAsync(d => d.FileContent != null && d.FileContent.Length > 0);
            var emptyContent = totalDocs - withContent;

            // Get sample of last 10 docs
            var sampleDocs = await _context.ImportedDocuments
                .OrderByDescending(d => d.Id)
                .Take(10)
                .Select(d => new { d.Id, d.OriginalFileName, d.FileSize, ContentLength = d.FileContent.Length, d.ProcessingStatus, d.DocumentType, d.InvoiceId, d.UploadDate })
                .ToListAsync();

            return Json(new
            {
                totalDocuments = totalDocs,
                withContent,
                emptyContent,
                sampleDocuments = sampleDocs
            });
        }

        // ==================== APPEARANCE & BRANDING ====================

        // GET: Admin/Appearance
        public async Task<IActionResult> Appearance()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            var vm = BuildAppearanceViewModel(settings);
            return View(vm);
        }

        // POST: Admin/SaveTheme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTheme(AppearanceViewModel model)
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            var themeSettings = new Dictionary<string, string>
            {
                ["Theme_PrimaryColor"] = model.PrimaryColor ?? "#0d6efd",
                ["Theme_SecondaryColor"] = model.SecondaryColor ?? "#6c757d",
                ["Theme_AccentColor"] = model.AccentColor ?? "#198754",
                ["Theme_DangerColor"] = model.DangerColor ?? "#dc3545",
                ["Theme_WarningColor"] = model.WarningColor ?? "#ffc107",
                ["Theme_NavbarStyle"] = model.NavbarStyle ?? "bg-primary",
                ["Theme_SidebarStyle"] = model.SidebarStyle ?? "light",
                ["Theme_CardStyle"] = model.CardStyle ?? "shadow",
                ["Theme_FontFamily"] = model.FontFamily ?? "Segoe UI"
            };

            await SaveSettingsBatchAsync(themeSettings, "Appearance", username);
            TempData["SuccessMessage"] = "Theme settings saved successfully!";
            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/SaveBranding
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBranding(AppearanceViewModel model)
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            var brandingSettings = new Dictionary<string, string>
            {
                ["ApplicationName"] = model.ApplicationName ?? "Invoice Management System",
                ["Branding_TagLine"] = model.TagLine ?? ""
            };

            await SaveSettingsBatchAsync(brandingSettings, "Appearance", username);

            // Also update the General category ApplicationName
            await _adminService.UpdateSettingAsync("ApplicationName", model.ApplicationName ?? "Invoice Management System", username);

            TempData["SuccessMessage"] = "Branding saved successfully!";
            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/UploadLogo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLogo(IFormFile logoFile)
        {
            if (logoFile == null || logoFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a logo file to upload.";
                return RedirectToAction(nameof(Appearance));
            }

            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".webp", ".gif" };
            var ext = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "Invalid file type. Allowed: PNG, JPG, SVG, WebP, GIF.";
                return RedirectToAction(nameof(Appearance));
            }

            if (logoFile.Length > 2 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Logo file must be under 2 MB.";
                return RedirectToAction(nameof(Appearance));
            }

            try
            {
                // Save to database for persistence across Cloud Run restarts
                var username = HttpContext.Session.GetString("Username") ?? "Admin";
                await SaveAssetToDatabaseAsync("logo", $"logo{ext}", logoFile, username);

                var servePath = "/Assets/Get/logo";
                await SaveSettingsBatchAsync(new Dictionary<string, string> { ["Branding_LogoUrl"] = servePath }, "Appearance", username);

                TempData["SuccessMessage"] = "Logo uploaded successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading logo: {ex.Message}";
            }

            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/RemoveLogo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogo()
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            await SaveSettingsBatchAsync(new Dictionary<string, string> { ["Branding_LogoUrl"] = "" }, "Appearance", username);
            await DeleteAssetFromDatabaseAsync("logo");

            TempData["SuccessMessage"] = "Logo removed.";
            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/UploadFavicon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFavicon(IFormFile faviconFile)
        {
            if (faviconFile == null || faviconFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a favicon file to upload.";
                return RedirectToAction(nameof(Appearance));
            }

            var allowedExtensions = new[] { ".ico", ".png", ".svg" };
            var ext = Path.GetExtension(faviconFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "Invalid file type. Allowed: ICO, PNG, SVG.";
                return RedirectToAction(nameof(Appearance));
            }

            if (faviconFile.Length > 512 * 1024)
            {
                TempData["ErrorMessage"] = "Favicon must be under 512 KB.";
                return RedirectToAction(nameof(Appearance));
            }

            try
            {
                // Save to database for persistence across Cloud Run restarts
                var username = HttpContext.Session.GetString("Username") ?? "Admin";
                await SaveAssetToDatabaseAsync("favicon", $"favicon{ext}", faviconFile, username);

                var servePath = "/Assets/Get/favicon";
                await SaveSettingsBatchAsync(new Dictionary<string, string> { ["Branding_FaviconUrl"] = servePath }, "Appearance", username);

                TempData["SuccessMessage"] = "Favicon uploaded successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading favicon: {ex.Message}";
            }

            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/RemoveFavicon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavicon()
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            await SaveSettingsBatchAsync(new Dictionary<string, string> { ["Branding_FaviconUrl"] = "" }, "Appearance", username);
            await DeleteAssetFromDatabaseAsync("favicon");

            TempData["SuccessMessage"] = "Favicon removed.";
            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/SaveHeaderFooter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveHeaderFooter(AppearanceViewModel model)
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            var settings = new Dictionary<string, string>
            {
                ["Header_ShowSearch"] = model.ShowHeaderSearch.ToString().ToLower(),
                ["Header_ShowQuickAdd"] = model.ShowQuickAdd.ToString().ToLower(),
                ["Header_ShowNotifications"] = model.ShowHeaderNotifications.ToString().ToLower(),
                ["Header_ShowCompanyInfo"] = model.ShowHeaderCompanyInfo.ToString().ToLower(),
                ["Header_AnnouncementText"] = model.HeaderAnnouncementText ?? "",
                ["Header_AnnouncementType"] = model.HeaderAnnouncementType ?? "info",
                ["Footer_Show"] = model.ShowFooter.ToString().ToLower(),
                ["Footer_Text"] = model.FooterText ?? "",
                ["Footer_LeftLinks"] = model.FooterLeftLinks ?? "",
                ["Footer_RightLinks"] = model.FooterRightLinks ?? "",
                ["Footer_ShowVersion"] = model.ShowFooterVersion.ToString().ToLower(),
                ["Footer_ShowCompanyInfo"] = model.ShowFooterCompanyInfo.ToString().ToLower(),
                ["Footer_ShowSocialLinks"] = model.ShowFooterSocialLinks.ToString().ToLower(),
                ["Company_Address"] = model.CompanyAddress ?? "",
                ["Company_Phone"] = model.CompanyPhone ?? "",
                ["Company_Email"] = model.CompanyEmail ?? "",
                ["Company_Website"] = model.CompanyWebsite ?? "",
                ["Social_Facebook"] = model.SocialFacebook ?? "",
                ["Social_Twitter"] = model.SocialTwitter ?? "",
                ["Social_LinkedIn"] = model.SocialLinkedIn ?? "",
                ["Social_Instagram"] = model.SocialInstagram ?? "",
                ["Social_YouTube"] = model.SocialYouTube ?? ""
            };

            await SaveSettingsBatchAsync(settings, "Appearance", username);

            // Also sync company info to the legacy Company category keys used by PDF reports
            var companySync = new Dictionary<string, string>
            {
                ["CompanyAddress"] = model.CompanyAddress ?? "",
                ["CompanyPhone"] = model.CompanyPhone ?? "",
                ["CompanyEmail"] = model.CompanyEmail ?? "",
                ["CompanyWebsite"] = model.CompanyWebsite ?? ""
            };
            await SaveSettingsBatchAsync(companySync, "Company", username);

            TempData["SuccessMessage"] = "Header & footer settings saved!";
            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/SaveLoginScreen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLoginScreen(AppearanceViewModel model)
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            var settings = new Dictionary<string, string>
            {
                ["Login_BackgroundType"] = model.LoginBackgroundType ?? "gradient",
                ["Login_CardOpacity"] = Math.Clamp(model.LoginCardOpacity, 10, 100).ToString(),
                ["Login_ShowFeatureBoxes"] = model.LoginShowFeatureBoxes.ToString().ToLower()
            };

            await SaveSettingsBatchAsync(settings, "Appearance", username);
            TempData["SuccessMessage"] = "Login screen settings saved!";
            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/UploadLoginBackground
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200_000_000)] // 200 MB for video uploads
        public async Task<IActionResult> UploadLoginBackground(IFormFile loginBgFile)
        {
            if (loginBgFile == null || loginBgFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a background file to upload.";
                return RedirectToAction(nameof(Appearance));
            }

            var imageExts = new[] { ".png", ".jpg", ".jpeg", ".svg", ".webp", ".gif", ".bmp", ".tiff" };
            var videoExts = new[] { ".mp4", ".webm", ".ogg", ".mov" };
            var ext = Path.GetExtension(loginBgFile.FileName).ToLowerInvariant();
            var allAllowed = imageExts.Concat(videoExts).ToArray();

            if (!allAllowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Invalid file type. Allowed: PNG, JPG, SVG, WebP, GIF, BMP, TIFF (images) or MP4, WebM, OGG, MOV (videos).";
                return RedirectToAction(nameof(Appearance));
            }

            bool isVideo = videoExts.Contains(ext);

            // Image max 10 MB, Video max 200 MB (5 min clips)
            long maxSize = isVideo ? 200L * 1024 * 1024 : 10L * 1024 * 1024;
            if (loginBgFile.Length > maxSize)
            {
                TempData["ErrorMessage"] = isVideo
                    ? "Video file must be under 200 MB (approximately 5 minutes)."
                    : "Image file must be under 10 MB.";
                return RedirectToAction(nameof(Appearance));
            }

            try
            {
                // Save to database for persistence across Cloud Run restarts
                var username = HttpContext.Session.GetString("Username") ?? "Admin";
                await SaveAssetToDatabaseAsync("login-bg", $"login-bg{ext}", loginBgFile, username);

                var servePath = "/Assets/Get/login-bg";
                var bgType = isVideo ? "video" : "image";
                await SaveSettingsBatchAsync(new Dictionary<string, string>
                {
                    ["Login_BackgroundUrl"] = servePath,
                    ["Login_BackgroundType"] = bgType
                }, "Appearance", username);

                TempData["SuccessMessage"] = $"Login background {(isVideo ? "video" : "image")} uploaded successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading login background: {ex.Message}";
            }

            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/RemoveLoginBackground
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLoginBackground()
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            await SaveSettingsBatchAsync(new Dictionary<string, string>
            {
                ["Login_BackgroundUrl"] = "",
                ["Login_BackgroundType"] = "gradient"
            }, "Appearance", username);
            await DeleteAssetFromDatabaseAsync("login-bg");

            TempData["SuccessMessage"] = "Login background removed.";
            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/SaveWidgets
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveWidgets(AppearanceViewModel model)
        {
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            var settings = new Dictionary<string, string>
            {
                ["Widget_TotalInvoices"] = model.WidgetTotalInvoices.ToString().ToLower(),
                ["Widget_UnpaidAmount"] = model.WidgetUnpaidAmount.ToString().ToLower(),
                ["Widget_RecentPayments"] = model.WidgetRecentPayments.ToString().ToLower(),
                ["Widget_OverdueInvoices"] = model.WidgetOverdueInvoices.ToString().ToLower(),
                ["Widget_Procurement"] = model.WidgetProcurement.ToString().ToLower(),
                ["Widget_AuditLog"] = model.WidgetAuditLog.ToString().ToLower(),
                ["Widget_QuickActions"] = model.WidgetQuickActions.ToString().ToLower(),
                ["Widget_Charts"] = model.WidgetCharts.ToString().ToLower(),
                ["Widget_DashboardLayout"] = model.DashboardLayout ?? "default"
            };

            await SaveSettingsBatchAsync(settings, "Appearance", username);
            TempData["SuccessMessage"] = "Widget configuration saved!";
            return RedirectToAction(nameof(Appearance));
        }

        // POST: Admin/ResetAppearance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAppearance()
        {
            var appearanceKeys = await _context.SystemSettings
                .Where(s => s.Category == "Appearance")
                .ToListAsync();

            _context.SystemSettings.RemoveRange(appearanceKeys);
            await _context.SaveChangesAsync();

            // Delete uploaded branding files
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "branding");
            if (Directory.Exists(uploadsDir))
            {
                foreach (var file in Directory.GetFiles(uploadsDir))
                    System.IO.File.Delete(file);
            }

            TempData["SuccessMessage"] = "All appearance settings reset to defaults.";
            return RedirectToAction(nameof(Appearance));
        }

        // Helper: Save multiple settings in one batch
        private async Task SaveSettingsBatchAsync(Dictionary<string, string> settings, string category, string username)
        {
            foreach (var kvp in settings)
            {
                var existing = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == kvp.Key);
                if (existing != null)
                {
                    existing.SettingValue = kvp.Value;
                    existing.ModifiedBy = username;
                    existing.ModifiedDate = DateTime.Now;
                }
                else
                {
                    _context.SystemSettings.Add(new SystemSetting
                    {
                        Category = category,
                        SettingKey = kvp.Key,
                        SettingValue = kvp.Value,
                        Description = $"Appearance setting: {kvp.Key}",
                        ModifiedBy = username,
                        ModifiedDate = DateTime.Now
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        // Helper: Save uploaded file to UploadedAssets table (persists across Cloud Run restarts)
        private async Task SaveAssetToDatabaseAsync(string assetKey, string fileName, IFormFile file, string username)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var existing = await _context.UploadedAssets.FirstOrDefaultAsync(a => a.AssetKey == assetKey);
            if (existing != null)
            {
                existing.FileName = fileName;
                existing.ContentType = file.ContentType;
                existing.FileContent = fileBytes;
                existing.FileSize = file.Length;
                existing.UploadedDate = DateTime.Now;
                existing.UploadedBy = username;
            }
            else
            {
                _context.UploadedAssets.Add(new UploadedAsset
                {
                    AssetKey = assetKey,
                    FileName = fileName,
                    ContentType = file.ContentType,
                    FileContent = fileBytes,
                    FileSize = file.Length,
                    UploadedDate = DateTime.Now,
                    UploadedBy = username
                });
            }
            await _context.SaveChangesAsync();
        }

        // Helper: Delete uploaded asset from UploadedAssets table
        private async Task DeleteAssetFromDatabaseAsync(string assetKey)
        {
            var existing = await _context.UploadedAssets.FirstOrDefaultAsync(a => a.AssetKey == assetKey);
            if (existing != null)
            {
                _context.UploadedAssets.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }

        // Helper: Build AppearanceViewModel from settings list
        private AppearanceViewModel BuildAppearanceViewModel(List<SystemSetting> settings)
        {
            string Get(string key, string fallback = "") =>
                settings.FirstOrDefault(s => s.SettingKey == key)?.SettingValue ?? fallback;

            bool GetBool(string key, bool fallback = true) =>
                Get(key, fallback.ToString().ToLower()) == "true";

            return new AppearanceViewModel
            {
                PrimaryColor = Get("Theme_PrimaryColor", "#0d6efd"),
                SecondaryColor = Get("Theme_SecondaryColor", "#6c757d"),
                AccentColor = Get("Theme_AccentColor", "#198754"),
                DangerColor = Get("Theme_DangerColor", "#dc3545"),
                WarningColor = Get("Theme_WarningColor", "#ffc107"),
                NavbarStyle = Get("Theme_NavbarStyle", "bg-primary"),
                SidebarStyle = Get("Theme_SidebarStyle", "light"),
                CardStyle = Get("Theme_CardStyle", "shadow"),
                FontFamily = Get("Theme_FontFamily", "Segoe UI"),

                ApplicationName = Get("ApplicationName", "Invoice Management System"),
                LogoUrl = Get("Branding_LogoUrl"),
                FaviconUrl = Get("Branding_FaviconUrl"),
                TagLine = Get("Branding_TagLine"),

                CompanyAddress = Get("Company_Address"),
                CompanyPhone = Get("Company_Phone"),
                CompanyEmail = Get("Company_Email"),
                CompanyWebsite = Get("Company_Website"),

                SocialFacebook = Get("Social_Facebook"),
                SocialTwitter = Get("Social_Twitter"),
                SocialLinkedIn = Get("Social_LinkedIn"),
                SocialInstagram = Get("Social_Instagram"),
                SocialYouTube = Get("Social_YouTube"),

                ShowHeaderSearch = GetBool("Header_ShowSearch"),
                ShowQuickAdd = GetBool("Header_ShowQuickAdd"),
                ShowHeaderNotifications = GetBool("Header_ShowNotifications", false),
                ShowHeaderCompanyInfo = GetBool("Header_ShowCompanyInfo", false),
                HeaderAnnouncementText = Get("Header_AnnouncementText"),
                HeaderAnnouncementType = Get("Header_AnnouncementType", "info"),

                ShowFooter = GetBool("Footer_Show"),
                FooterText = Get("Footer_Text", "© {year} Invoice Management System. All rights reserved."),
                FooterLeftLinks = Get("Footer_LeftLinks"),
                FooterRightLinks = Get("Footer_RightLinks"),
                ShowFooterVersion = GetBool("Footer_ShowVersion"),
                ShowFooterCompanyInfo = GetBool("Footer_ShowCompanyInfo"),
                ShowFooterSocialLinks = GetBool("Footer_ShowSocialLinks"),

                LoginBackgroundType = Get("Login_BackgroundType", "gradient"),
                LoginBackgroundUrl = Get("Login_BackgroundUrl"),
                LoginCardOpacity = int.TryParse(Get("Login_CardOpacity", "95"), out var op) ? op : 95,
                LoginShowFeatureBoxes = GetBool("Login_ShowFeatureBoxes"),

                WidgetTotalInvoices = GetBool("Widget_TotalInvoices"),
                WidgetUnpaidAmount = GetBool("Widget_UnpaidAmount"),
                WidgetRecentPayments = GetBool("Widget_RecentPayments"),
                WidgetOverdueInvoices = GetBool("Widget_OverdueInvoices"),
                WidgetProcurement = GetBool("Widget_Procurement"),
                WidgetAuditLog = GetBool("Widget_AuditLog"),
                WidgetQuickActions = GetBool("Widget_QuickActions"),
                WidgetCharts = GetBool("Widget_Charts"),
                DashboardLayout = Get("Widget_DashboardLayout", "default"),

                MaintenanceEnabled = GetBool("MaintenanceMode_Enabled", false)
            };
        }
    }

    // ==================== ADMIN VIEW MODELS ====================

    public class SystemHealthData
    {
        public string DatabaseStatus { get; set; } = "Unknown";
        public string DatabaseSize { get; set; } = "Unknown";
        public int TotalInvoices { get; set; }
        public int TotalPayments { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalAuditLogs { get; set; }
        public int TotalDocuments { get; set; }
        public DateTime ServerTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public long MemoryUsage { get; set; }
        public DateTime? LastBackup { get; set; }
        public Invoice? OldestUnpaidInvoice { get; set; }
        public List<string> RecentErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class DataCleanupStats
    {
        public int OldAuditLogsCount { get; set; }
        public int ProcessedDocumentsCount { get; set; }
        public int PaidInvoicesOlderThan1Year { get; set; }
        public int FullyAllocatedPaymentsOlderThan1Year { get; set; }
        public long TotalDocumentStorageBytes { get; set; }
    }

    public class RolePermissionInfo
    {
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }

    public class AdminAnalytics
    {
        public int TotalInvoicesThisYear { get; set; }
        public int TotalInvoicesThisMonth { get; set; }
        public decimal TotalInvoiceAmountThisYear { get; set; }
        public decimal TotalInvoiceAmountThisMonth { get; set; }
        public int TotalPaymentsThisYear { get; set; }
        public int TotalPaymentsThisMonth { get; set; }
        public decimal TotalPaymentAmountThisYear { get; set; }
        public decimal TotalPaymentAmountThisMonth { get; set; }
        public int ActiveUsersThisMonth { get; set; }
        public int NewUsersThisMonth { get; set; }
        public List<MonthlyTrend> MonthlyInvoiceTrends { get; set; } = new();
        public List<MonthlyTrend> MonthlyPaymentTrends { get; set; } = new();
        public List<TopSupplierInfo> TopSuppliers { get; set; } = new();
        public Dictionary<string, int> InvoiceStatusDistribution { get; set; } = new();
        public Dictionary<string, decimal> PaymentMethodDistribution { get; set; } = new();
    }

    public class MonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class TopSupplierInfo
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

// Add API endpoint for enabling RLS - accessible without auth for one-time setup
[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SetupController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("enable-rls")]
    public async Task<IActionResult> EnableRLS([FromQuery] string key)
    {
        // Simple security check
        if (key != "setup-rls-2026")
            return Unauthorized("Invalid setup key");

        var results = new List<string>();
        var tables = new[]
        {
            "BatchPaymentItems", "BatchPayments", "AuditLogs", "UserRoles",
            "Permissions", "RolePermissions", "PurchaseOrderItems", "RequisitionItems",
            "PaymentAllocations", "ImportedDocuments", "InvoiceItems", "Payments",
            "Invoices", "Suppliers", "PurchaseOrders", "Requisitions", "Users",
            "Roles", "SystemSettings", "Customers", "__EFMigrationsHistory", "DataProtectionKeys"
        };

        foreach (var table in tables)
        {
            try
            {
                // Enable RLS
                await _context.Database.ExecuteSqlRawAsync($"ALTER TABLE public.\"{table}\" ENABLE ROW LEVEL SECURITY");
                results.Add($"✓ RLS enabled on {table}");

                // Check if policy exists using raw SQL
                var sql = $"SELECT COUNT(*) FROM pg_policies WHERE tablename = '{table}' AND policyname = 'Allow all for authenticated service'";
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                await _context.Database.OpenConnectionAsync();
                var result = await command.ExecuteScalarAsync();
                var policyCount = Convert.ToInt64(result ?? 0);

                if (policyCount == 0)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        $"CREATE POLICY \"Allow all for authenticated service\" ON public.\"{table}\" FOR ALL USING (true) WITH CHECK (true)");
                    results.Add($"  ✓ Policy created for {table}");
                }
                else
                {
                    results.Add($"  ○ Policy already exists for {table}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"✗ Error on {table}: {ex.Message}");
            }
        }

        // Update AI settings
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE \"SystemSettings\" SET \"SettingValue\" = 'gpt-5.2', \"Description\" = 'OpenAI model for document processing', \"ModifiedDate\" = NOW() WHERE \"SettingKey\" = 'AIModel'");
            results.Add("✓ AIModel updated to gpt-5.2");

            var openAiExists = await _context.SystemSettings.AnyAsync(s => s.SettingKey == "OpenAIApiKey");
            if (!openAiExists)
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    Category = "API",
                    SettingKey = "OpenAIApiKey",
                    SettingValue = "",
                    Description = "OpenAI API key for AI-powered invoice/payment import. Get from https://platform.openai.com/api-keys",
                    ModifiedBy = "System",
                    ModifiedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
                results.Add("✓ OpenAIApiKey setting created");
            }
        }
        catch (Exception ex)
        {
            results.Add($"✗ Error updating AI settings: {ex.Message}");
        }

        return Ok(new { success = true, results });
    }

    [HttpPost("set-apikey")]
    public async Task<IActionResult> SetApiKey([FromQuery] string key, [FromQuery] string apiKey)
    {
        if (key != "setup-rls-2026")
            return Unauthorized("Invalid setup key");

        if (string.IsNullOrEmpty(apiKey))
            return BadRequest("API key is required");

        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == "OpenAIApiKey");
        if (setting != null)
        {
            setting.SettingValue = apiKey;
            setting.ModifiedDate = DateTime.Now;
            setting.ModifiedBy = "Setup";
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "OpenAI API key updated successfully" });
        }
        else
        {
            _context.SystemSettings.Add(new SystemSetting
            {
                Category = "API",
                SettingKey = "OpenAIApiKey",
                SettingValue = apiKey,
                Description = "OpenAI API key for AI-powered invoice/payment import",
                ModifiedBy = "Setup",
                ModifiedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "OpenAI API key created successfully" });
        }
    }
}