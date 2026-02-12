using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;

namespace InvoiceManagement.Filters
{
    /// <summary>
    /// Global action filter that checks if the system is in maintenance mode.
    /// When enabled, non-admin users are redirected to a maintenance page.
    /// Admins can continue using the system normally.
    /// </summary>
    public class MaintenanceModeFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceModeFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Get the current controller and action names
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
            var actionName = context.RouteData.Values["action"]?.ToString() ?? "";

            // Always allow these routes (login, logout, access denied, maintenance page itself)
            var bypassRoutes = new[]
            {
                ("Account", "Login"),
                ("Account", "Logout"),
                ("Account", "AccessDenied"),
                ("Account", "Maintenance")
            };

            foreach (var (ctrl, act) in bypassRoutes)
            {
                if (string.Equals(controllerName, ctrl, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(actionName, act, StringComparison.OrdinalIgnoreCase))
                {
                    await next();
                    return;
                }
            }

            // Always allow Admin controller (admins need full access during maintenance)
            if (string.Equals(controllerName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // Check if the current user is an admin — admins bypass maintenance mode
            var userRole = context.HttpContext.Session.GetString("Role") ??
                           context.HttpContext.Session.GetString("UserRole") ?? "";
            if (userRole == "Admin" || userRole == "SystemAdmin")
            {
                // Inject maintenance mode flag into ViewData so admins see the banner
                if (context.Controller is Controller mvcController)
                {
                    try
                    {
                        var isEnabled = await _context.SystemSettings
                            .Where(s => s.SettingKey == "MaintenanceMode_Enabled")
                            .Select(s => s.SettingValue)
                            .FirstOrDefaultAsync();

                        mvcController.ViewData["MaintenanceModeActive"] = isEnabled == "true";
                    }
                    catch
                    {
                        // If DB query fails, don't block admins
                    }
                }

                await next();
                return;
            }

            // For non-admin users, check if maintenance mode is enabled
            try
            {
                var maintenanceSetting = await _context.SystemSettings
                    .Where(s => s.SettingKey == "MaintenanceMode_Enabled")
                    .Select(s => s.SettingValue)
                    .FirstOrDefaultAsync();

                if (maintenanceSetting == "true")
                {
                    // Redirect to the maintenance page
                    context.Result = new RedirectToActionResult("Maintenance", "Account", null);
                    return;
                }
            }
            catch
            {
                // If the DB query fails, don't block users — let them through
            }

            await next();
        }
    }
}
