using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using InvoiceManagement.Data;
using InvoiceManagement.Services;
using InvoiceManagement.ModelBinders;
using System.Globalization;

// Enable legacy timestamp behavior for PostgreSQL to handle DateTime.Now without UTC conversion
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Get database configuration
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SQLite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// For SQLite in development, build cross-platform database path
if (databaseProvider == "SQLite" && (string.IsNullOrEmpty(connectionString) || connectionString.Contains("InvoiceManagement.db")))
{
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "InvoiceManagement.db");
    connectionString = $"Data Source={dbPath}";
}

// Set default culture to support DD/MM/YYYY date format
var cultureInfo = new CultureInfo("en-GB"); // British English uses DD/MM/YYYY
cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Add services to the container with custom model binders
builder.Services.AddControllersWithViews(options =>
{
    // Add custom date time model binder for DD/MM/YYYY format
    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());

    // Add currency view data filter to make currency settings available in all views
    options.Filters.Add<InvoiceManagement.Filters.CurrencyViewDataFilter>();
})
.AddSessionStateTempDataProvider(); // Use session-based TempData to avoid HTTP 431 cookie size errors

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".InvMgt.Session"; // Unique cookie name
});

// For PostgreSQL, ensure database schema exists BEFORE configuring DataProtection
// This prevents "relation does not exist" errors on fresh databases
if (databaseProvider == "PostgreSQL")
{
    // Create a temporary service provider to run migrations first
    var tempServices = new ServiceCollection();
    tempServices.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    using (var tempProvider = tempServices.BuildServiceProvider())
    using (var scope = tempProvider.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            Console.WriteLine("Ensuring database schema exists...");
            db.Database.Migrate();
            Console.WriteLine("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration note: {ex.Message}");
            // Try to create essential tables manually if migration fails
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""DataProtectionKeys"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""FriendlyName"" TEXT,
                        ""Xml"" TEXT
                    );
                ");
                Console.WriteLine("DataProtectionKeys table ensured.");
            }
            catch (Exception tableEx)
            {
                Console.WriteLine($"Table creation note: {tableEx.Message}");
            }
        }
    }
}

// Configure Data Protection to persist keys for containerized environments
// This ensures anti-forgery tokens and session cookies work properly across Cloud Run instances
if (databaseProvider == "PostgreSQL")
{
    // For production, use database-backed key storage
    builder.Services.AddDataProtection()
        .SetApplicationName("InvoiceManagement")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
        .PersistKeysToDbContext<ApplicationDbContext>();
}
else
{
    // For development, use file system
    builder.Services.AddDataProtection()
        .SetApplicationName("InvoiceManagement")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
}

// Configure anti-forgery to work with Cloud Run's load balancing
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Name = ".InvMgt.Antiforgery"; // Unique cookie name
    options.SuppressXFrameOptionsHeader = false;
});

// Configure Entity Framework with appropriate database provider
if (databaseProvider == "PostgreSQL")
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}

// Register custom services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IPdfService, PdfService>();

// Configure HttpClient for AI Processing with extended timeout for large documents
builder.Services.AddHttpClient<IAiProcessingService, AiProcessingService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(10); // 10 minute timeout for large PDF processing
});

// Register procurement services
builder.Services.AddScoped<IRequisitionService, RequisitionService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// Register admin services
builder.Services.AddScoped<IAdminService, AdminService>();

// Register authentication service
builder.Services.AddScoped<IAuthService, AuthService>();

// Register authorization service (role-based access control)
builder.Services.AddScoped<InvoiceManagement.Services.IAuthorizationService, InvoiceManagement.Services.AuthorizationService>();

// Register currency service
builder.Services.AddScoped<ICurrencyService, CurrencyService>();

// Register document storage and entity lookup services
builder.Services.AddScoped<IDocumentStorageService, DocumentStorageService>();
builder.Services.AddScoped<IEntityLookupService, EntityLookupService>();

var app = builder.Build();

// Apply database migrations and schema updates
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
    var isPostgres = config["DatabaseProvider"] == "PostgreSQL";

    try
    {
        // Ensure database is created and migrations applied
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Schema update note: {ex.Message}");
    }

    // Fix ProcessingNotes column type for PostgreSQL (must be TEXT for bulk invoice data)
    // This runs AFTER migrations, outside the try-catch so it always executes
    if (isPostgres)
    {
        try
        {
            Console.WriteLine("Updating ProcessingNotes column to TEXT...");
            db.Database.ExecuteSqlRaw(@"ALTER TABLE ""ImportedDocuments"" ALTER COLUMN ""ProcessingNotes"" TYPE TEXT;");
            Console.WriteLine("ProcessingNotes column updated to TEXT successfully");
        }
        catch (Exception colEx)
        {
            Console.WriteLine($"ProcessingNotes column update note: {colEx.Message}");
        }
    }

    try
    {
        // Initialize default system settings (including GoogleAIApiKey)
        await adminService.InitializeDefaultSettingsAsync();
        Console.WriteLine("System settings initialized successfully");

        // Set default password for users who have no password set
        var usersWithoutPassword = db.Users.Where(u => string.IsNullOrEmpty(u.PasswordHash)).ToList();
        if (usersWithoutPassword.Any())
        {
            var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");
            foreach (var user in usersWithoutPassword)
            {
                user.PasswordHash = defaultPasswordHash;
            }
            db.SaveChanges();
            Console.WriteLine($"Set default password for {usersWithoutPassword.Count} user(s). Default password is: Password123!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Schema update note: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configure forwarded headers for Cloud Run (HTTPS termination at load balancer)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Only use HTTPS redirection in non-Cloud Run environments
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

// Middleware to clear corrupted session cookies (happens after deployment with new data protection keys)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex) when (ex.Message.Contains("key") && ex.Message.Contains("not found"))
    {
        // Clear all cookies and redirect to start fresh
        foreach (var cookie in context.Request.Cookies.Keys)
        {
            context.Response.Cookies.Delete(cookie);
        }
        context.Response.Redirect("/");
    }
});

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

