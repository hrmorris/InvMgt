namespace InvoiceManagement.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Admin Appearance & Branding page.
    /// Groups all customization settings into logical sections.
    /// </summary>
    public class AppearanceViewModel
    {
        // === Theme ===
        public string PrimaryColor { get; set; } = "#0d6efd";
        public string SecondaryColor { get; set; } = "#6c757d";
        public string AccentColor { get; set; } = "#198754";
        public string DangerColor { get; set; } = "#dc3545";
        public string WarningColor { get; set; } = "#ffc107";
        public string NavbarStyle { get; set; } = "bg-primary";  // bg-primary, bg-dark, bg-gradient-primary, etc.
        public string SidebarStyle { get; set; } = "light";      // light, dark
        public string CardStyle { get; set; } = "shadow";        // shadow, bordered, flat
        public string FontFamily { get; set; } = "Segoe UI";

        // === Branding ===
        public string ApplicationName { get; set; } = "Invoice Management System";
        public string LogoUrl { get; set; } = "";
        public string FaviconUrl { get; set; } = "";
        public string TagLine { get; set; } = "";

        // === Company Info (Header & Footer) ===
        public string CompanyAddress { get; set; } = "";
        public string CompanyPhone { get; set; } = "";
        public string CompanyEmail { get; set; } = "";
        public string CompanyWebsite { get; set; } = "";

        // === Social Media ===
        public string SocialFacebook { get; set; } = "";
        public string SocialTwitter { get; set; } = "";
        public string SocialLinkedIn { get; set; } = "";
        public string SocialInstagram { get; set; } = "";
        public string SocialYouTube { get; set; } = "";

        // === Header ===
        public bool ShowHeaderSearch { get; set; } = true;
        public bool ShowQuickAdd { get; set; } = true;
        public bool ShowHeaderNotifications { get; set; } = false;
        public bool ShowHeaderCompanyInfo { get; set; } = false;
        public string HeaderAnnouncementText { get; set; } = "";
        public string HeaderAnnouncementType { get; set; } = "info"; // info, warning, success, danger

        // === Footer ===
        public bool ShowFooter { get; set; } = true;
        public string FooterText { get; set; } = "© {year} Invoice Management System. All rights reserved.";
        public string FooterLeftLinks { get; set; } = "";   // JSON array of {text,url}
        public string FooterRightLinks { get; set; } = "";  // JSON array of {text,url}
        public bool ShowFooterVersion { get; set; } = true;
        public bool ShowFooterCompanyInfo { get; set; } = true;
        public bool ShowFooterSocialLinks { get; set; } = true;

        // === Login Screen ===
        public string LoginBackgroundType { get; set; } = "gradient";  // gradient, image, video
        public string LoginBackgroundUrl { get; set; } = "";
        public int LoginCardOpacity { get; set; } = 95;  // 0-100, percentage for card transparency
        public bool LoginShowFeatureBoxes { get; set; } = true;

        // === Dashboard Widgets ===
        public bool WidgetTotalInvoices { get; set; } = true;
        public bool WidgetUnpaidAmount { get; set; } = true;
        public bool WidgetRecentPayments { get; set; } = true;
        public bool WidgetOverdueInvoices { get; set; } = true;
        public bool WidgetProcurement { get; set; } = true;
        public bool WidgetAuditLog { get; set; } = true;
        public bool WidgetQuickActions { get; set; } = true;
        public bool WidgetCharts { get; set; } = true;
        public string DashboardLayout { get; set; } = "default"; // default, compact, expanded

        // === Maintenance (read-only display) ===
        public bool MaintenanceEnabled { get; set; } = false;
    }
}
