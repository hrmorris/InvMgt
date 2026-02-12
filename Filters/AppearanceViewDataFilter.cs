using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;

namespace InvoiceManagement.Filters
{
    /// <summary>
    /// Global action filter that loads appearance/branding settings from the database
    /// and injects them into ViewData so they're available in _Layout.cshtml.
    /// </summary>
    public class AppearanceViewDataFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public AppearanceViewDataFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller)
            {
                try
                {
                    var settings = await _context.SystemSettings
                        .Where(s => s.Category == "Appearance" || s.SettingKey == "ApplicationName")
                        .AsNoTracking()
                        .ToListAsync();

                    string Get(string key, string fallback = "") =>
                        settings.FirstOrDefault(s => s.SettingKey == key)?.SettingValue ?? fallback;

                    bool GetBool(string key, bool fallback = true) =>
                        Get(key, fallback.ToString().ToLower()) == "true";

                    // Theme
                    controller.ViewData["Theme_PrimaryColor"] = Get("Theme_PrimaryColor", "#667eea");
                    controller.ViewData["Theme_SecondaryColor"] = Get("Theme_SecondaryColor", "#6c757d");
                    controller.ViewData["Theme_AccentColor"] = Get("Theme_AccentColor", "#764ba2");
                    controller.ViewData["Theme_DangerColor"] = Get("Theme_DangerColor", "#dc3545");
                    controller.ViewData["Theme_WarningColor"] = Get("Theme_WarningColor", "#ffc107");
                    controller.ViewData["Theme_NavbarStyle"] = Get("Theme_NavbarStyle", "bg-gradient-primary");
                    controller.ViewData["Theme_SidebarStyle"] = Get("Theme_SidebarStyle", "dark");
                    controller.ViewData["Theme_FontFamily"] = Get("Theme_FontFamily", "Inter");

                    // Branding
                    controller.ViewData["AppName"] = Get("ApplicationName", "Invoice Management System");
                    controller.ViewData["Branding_LogoUrl"] = Get("Branding_LogoUrl");
                    controller.ViewData["Branding_FaviconUrl"] = Get("Branding_FaviconUrl");
                    controller.ViewData["Branding_TagLine"] = Get("Branding_TagLine");

                    // Header
                    controller.ViewData["Header_ShowSearch"] = GetBool("Header_ShowSearch");
                    controller.ViewData["Header_ShowQuickAdd"] = GetBool("Header_ShowQuickAdd");
                    controller.ViewData["Header_ShowNotifications"] = GetBool("Header_ShowNotifications", false);
                    controller.ViewData["Header_AnnouncementText"] = Get("Header_AnnouncementText");
                    controller.ViewData["Header_AnnouncementType"] = Get("Header_AnnouncementType", "info");

                    // Footer
                    controller.ViewData["Footer_Show"] = GetBool("Footer_Show");
                    controller.ViewData["Footer_Text"] = Get("Footer_Text", "© {year} Invoice Management System. All rights reserved.");
                    controller.ViewData["Footer_LeftLinks"] = Get("Footer_LeftLinks");
                    controller.ViewData["Footer_RightLinks"] = Get("Footer_RightLinks");
                    controller.ViewData["Footer_ShowVersion"] = GetBool("Footer_ShowVersion");
                }
                catch
                {
                    // If DB query fails on startup, use defaults — don't crash
                }
            }

            await next();
        }
    }
}
