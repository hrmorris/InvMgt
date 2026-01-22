using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;
using InvoiceManagement.Services;
using InvoiceManagement.ModelBinders;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Build cross-platform database path
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "InvoiceManagement.db");
var connectionString = $"Data Source={dbPath}";

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
});

// Configure Entity Framework with SQLite (cross-platform path)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Register custom services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddHttpClient<IAiProcessingService, AiProcessingService>();

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

// Apply database schema updates
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Add GSTEnabled column if it doesn't exist
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS __SchemaUpdates (UpdateName TEXT PRIMARY KEY);
            INSERT OR IGNORE INTO __SchemaUpdates VALUES ('AddGSTEnabled');
        ");

        var needsUpdate = db.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM __SchemaUpdates WHERE UpdateName = 'AddGSTEnabled_Applied'").ToList().FirstOrDefault() == 0;
        if (needsUpdate)
        {
            try
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Invoices ADD COLUMN GSTEnabled INTEGER NOT NULL DEFAULT 1");
            }
            catch { /* Column may already exist */ }

            db.Database.ExecuteSqlRaw("INSERT OR REPLACE INTO __SchemaUpdates VALUES ('AddGSTEnabled_Applied')");
        }

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

