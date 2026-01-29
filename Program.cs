using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using InvoiceManagement.Data;
using InvoiceManagement.Services;
using InvoiceManagement.ModelBinders;
using System.Globalization;

// Check for migration command
if (args.Length > 0 && args[0] == "--migrate")
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    await DataMigrator.RunMigrationAsync();
    return;
}

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
            // Check if tables exist first
            var tableExists = false;
            try
            {
                db.Database.ExecuteSqlRaw(@"SELECT 1 FROM ""SystemSettings"" LIMIT 1");
                tableExists = true;
                Console.WriteLine("Database tables already exist.");
            }
            catch
            {
                tableExists = false;
            }

            if (!tableExists)
            {
                Console.WriteLine("Running database migrations...");
                db.Database.Migrate();
                Console.WriteLine("Database migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration error: {ex.Message}");
            Console.WriteLine("Attempting to create schema manually...");

            // Create all essential tables manually if migration fails
            try
            {
                // Create tables in order (no dependencies first)
                var schemaStatements = new[]
                {
                    @"CREATE TABLE IF NOT EXISTS ""DataProtectionKeys"" (""Id"" SERIAL PRIMARY KEY, ""FriendlyName"" TEXT, ""Xml"" TEXT);",
                    @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (""MigrationId"" VARCHAR(150) PRIMARY KEY, ""ProductVersion"" VARCHAR(32) NOT NULL);",
                    @"CREATE TABLE IF NOT EXISTS ""Customers"" (""Id"" SERIAL PRIMARY KEY, ""CustomerName"" VARCHAR(200) NOT NULL, ""CustomerCode"" VARCHAR(50), ""Address"" VARCHAR(500), ""Email"" VARCHAR(100), ""Phone"" VARCHAR(20), ""Mobile"" VARCHAR(20), ""ContactPerson"" VARCHAR(200), ""TIN"" VARCHAR(100), ""RegistrationNumber"" VARCHAR(100), ""BankName"" VARCHAR(200), ""BankAccountNumber"" VARCHAR(50), ""Industry"" VARCHAR(500), ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Active', ""PaymentTermsDays"" INT NOT NULL DEFAULT 30, ""CreditLimit"" DECIMAL NOT NULL DEFAULT 0, ""Notes"" VARCHAR(1000), ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ModifiedDate"" TIMESTAMP);",
                    @"CREATE TABLE IF NOT EXISTS ""Suppliers"" (""Id"" SERIAL PRIMARY KEY, ""SupplierName"" VARCHAR(200) NOT NULL, ""SupplierCode"" VARCHAR(50), ""Address"" VARCHAR(500), ""Email"" VARCHAR(100), ""Phone"" VARCHAR(20), ""Mobile"" VARCHAR(20), ""ContactPerson"" VARCHAR(200), ""TIN"" VARCHAR(100), ""RegistrationNumber"" VARCHAR(100), ""BankName"" VARCHAR(200), ""BankAccountNumber"" VARCHAR(50), ""ProductsServices"" VARCHAR(500), ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Active', ""PaymentTermsDays"" INT NOT NULL DEFAULT 30, ""Notes"" VARCHAR(1000), ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ModifiedDate"" TIMESTAMP);",
                    @"CREATE TABLE IF NOT EXISTS ""SystemSettings"" (""Id"" SERIAL PRIMARY KEY, ""SettingKey"" VARCHAR(100) NOT NULL UNIQUE, ""SettingValue"" VARCHAR(500), ""Description"" VARCHAR(200), ""Category"" VARCHAR(50) NOT NULL, ""ModifiedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ModifiedBy"" VARCHAR(100));",
                    @"CREATE TABLE IF NOT EXISTS ""Roles"" (""Id"" SERIAL PRIMARY KEY, ""Name"" VARCHAR(50) NOT NULL UNIQUE, ""DisplayName"" VARCHAR(200), ""Description"" VARCHAR(500), ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE, ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);",
                    @"CREATE TABLE IF NOT EXISTS ""Permissions"" (""Id"" SERIAL PRIMARY KEY, ""Name"" VARCHAR(100) NOT NULL UNIQUE, ""DisplayName"" VARCHAR(200), ""Description"" VARCHAR(500), ""Module"" VARCHAR(50) NOT NULL, ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE, ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);",
                    @"CREATE TABLE IF NOT EXISTS ""Users"" (""Id"" SERIAL PRIMARY KEY, ""Username"" VARCHAR(100) NOT NULL UNIQUE, ""FullName"" VARCHAR(200) NOT NULL, ""Email"" VARCHAR(100) NOT NULL UNIQUE, ""Phone"" VARCHAR(20), ""Department"" VARCHAR(100) NOT NULL, ""Facility"" VARCHAR(100) NOT NULL, ""FacilityType"" VARCHAR(50) NOT NULL, ""Role"" VARCHAR(50) NOT NULL, ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Active', ""PasswordHash"" VARCHAR(256), ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ModifiedDate"" TIMESTAMP, ""LastLoginDate"" TIMESTAMP, ""Notes"" VARCHAR(500));",
                    @"CREATE TABLE IF NOT EXISTS ""Requisitions"" (""Id"" SERIAL PRIMARY KEY, ""RequisitionNumber"" VARCHAR(50) NOT NULL UNIQUE, ""RequisitionDate"" TIMESTAMP NOT NULL, ""RequestedBy"" VARCHAR(200) NOT NULL, ""Department"" VARCHAR(200) NOT NULL, ""FacilityType"" VARCHAR(100) NOT NULL, ""Purpose"" VARCHAR(500) NOT NULL, ""EstimatedAmount"" DECIMAL(18,2) NOT NULL, ""CostCode"" VARCHAR(50) NOT NULL, ""BudgetCode"" VARCHAR(50) NOT NULL, ""Status"" VARCHAR(50) NOT NULL, ""SupervisorName"" VARCHAR(200), ""SupervisorApprovalDate"" TIMESTAMP, ""SupervisorComments"" VARCHAR(500), ""FinanceOfficerName"" VARCHAR(200), ""FinanceApprovalDate"" TIMESTAMP, ""FinanceComments"" VARCHAR(500), ""BudgetApproved"" BOOLEAN, ""NeedApproved"" BOOLEAN, ""CostCodeApproved"" BOOLEAN, ""FinalApproverName"" VARCHAR(200), ""FinalApprovalDate"" TIMESTAMP, ""FinalApproverComments"" VARCHAR(500), ""RejectionReason"" VARCHAR(500), ""Notes"" VARCHAR(1000), ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ModifiedDate"" TIMESTAMP);",
                    @"CREATE TABLE IF NOT EXISTS ""PurchaseOrders"" (""Id"" SERIAL PRIMARY KEY, ""PONumber"" VARCHAR(50) NOT NULL UNIQUE, ""PODate"" TIMESTAMP NOT NULL, ""RequisitionId"" INT REFERENCES ""Requisitions""(""Id"") ON DELETE RESTRICT, ""SupplierId"" INT NOT NULL REFERENCES ""Suppliers""(""Id"") ON DELETE RESTRICT, ""ExpectedDeliveryDate"" TIMESTAMP NOT NULL, ""DeliveryAddress"" VARCHAR(200) NOT NULL, ""TotalAmount"" DECIMAL(18,2) NOT NULL, ""Status"" VARCHAR(50) NOT NULL, ""PreparedBy"" VARCHAR(200) NOT NULL, ""ApprovedBy"" VARCHAR(200), ""ApprovalDate"" TIMESTAMP, ""TermsAndConditions"" VARCHAR(500), ""Notes"" VARCHAR(1000), ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ModifiedDate"" TIMESTAMP);",
                    @"CREATE TABLE IF NOT EXISTS ""Invoices"" (""Id"" SERIAL PRIMARY KEY, ""InvoiceNumber"" VARCHAR(50) NOT NULL UNIQUE, ""InvoiceDate"" TIMESTAMP NOT NULL, ""DueDate"" TIMESTAMP NOT NULL, ""PurchaseOrderId"" INT REFERENCES ""PurchaseOrders""(""Id""), ""SupplierId"" INT REFERENCES ""Suppliers""(""Id""), ""CustomerName"" VARCHAR(200) NOT NULL, ""CustomerAddress"" VARCHAR(500), ""CustomerEmail"" VARCHAR(100), ""CustomerPhone"" VARCHAR(20), ""SubTotal"" DECIMAL NOT NULL, ""GSTEnabled"" BOOLEAN NOT NULL DEFAULT TRUE, ""GSTRate"" DECIMAL NOT NULL DEFAULT 10, ""GSTAmount"" DECIMAL NOT NULL, ""TotalAmount"" DECIMAL(18,2) NOT NULL, ""PaidAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0, ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Unpaid', ""InvoiceType"" VARCHAR(50) NOT NULL DEFAULT 'Receivable', ""Notes"" VARCHAR(1000), ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ModifiedDate"" TIMESTAMP, ""CustomerId"" INT REFERENCES ""Customers""(""Id"") ON DELETE SET NULL);",
                    @"CREATE TABLE IF NOT EXISTS ""Payments"" (""Id"" SERIAL PRIMARY KEY, ""PaymentNumber"" VARCHAR(50) NOT NULL UNIQUE, ""InvoiceId"" INT REFERENCES ""Invoices""(""Id"") ON DELETE RESTRICT, ""PaymentDate"" TIMESTAMP NOT NULL, ""Amount"" DECIMAL(18,2) NOT NULL, ""PaymentMethod"" VARCHAR(50) NOT NULL, ""ReferenceNumber"" VARCHAR(100), ""Notes"" VARCHAR(500), ""Purpose"" VARCHAR(500), ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ModifiedDate"" TIMESTAMP, ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Unallocated', ""SupplierId"" INT REFERENCES ""Suppliers""(""Id"") ON DELETE SET NULL, ""CustomerId"" INT REFERENCES ""Customers""(""Id"") ON DELETE SET NULL, ""BankName"" VARCHAR(200), ""AccountType"" VARCHAR(50), ""BankAccountNumber"" VARCHAR(50), ""PayeeBranchNumber"" VARCHAR(10), ""PayeeAccountNumber"" VARCHAR(20), ""PayerAccountNumber"" VARCHAR(50), ""PayerBranchNumber"" VARCHAR(10), ""PayerBankAccountNumber"" VARCHAR(20), ""PayerName"" VARCHAR(200), ""PayeeName"" VARCHAR(200), ""TransferTo"" VARCHAR(200), ""Currency"" VARCHAR(10));",
                    @"CREATE TABLE IF NOT EXISTS ""InvoiceItems"" (""Id"" SERIAL PRIMARY KEY, ""InvoiceId"" INT NOT NULL REFERENCES ""Invoices""(""Id"") ON DELETE CASCADE, ""Description"" VARCHAR(200) NOT NULL, ""Quantity"" INT NOT NULL, ""UnitPrice"" DECIMAL(18,2) NOT NULL);",
                    @"CREATE TABLE IF NOT EXISTS ""ImportedDocuments"" (""Id"" SERIAL PRIMARY KEY, ""FileName"" VARCHAR(255) NOT NULL, ""OriginalFileName"" VARCHAR(255) NOT NULL, ""ContentType"" VARCHAR(100), ""FileSize"" BIGINT NOT NULL, ""FileContent"" BYTEA NOT NULL, ""DocumentType"" VARCHAR(50) NOT NULL, ""InvoiceId"" INT REFERENCES ""Invoices""(""Id"") ON DELETE SET NULL, ""PaymentId"" INT REFERENCES ""Payments""(""Id"") ON DELETE SET NULL, ""ExtractedText"" VARCHAR(2000), ""ExtractedAccountNumber"" VARCHAR(500), ""ExtractedBankName"" VARCHAR(500), ""ExtractedSupplierName"" VARCHAR(500), ""ExtractedCustomerName"" VARCHAR(500), ""ProcessingStatus"" VARCHAR(50) NOT NULL DEFAULT 'Pending', ""ProcessingNotes"" TEXT, ""UploadDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""ProcessedDate"" TIMESTAMP, ""UploadedBy"" VARCHAR(100));",
                    @"CREATE TABLE IF NOT EXISTS ""PaymentAllocations"" (""Id"" SERIAL PRIMARY KEY, ""PaymentId"" INT NOT NULL REFERENCES ""Payments""(""Id"") ON DELETE CASCADE, ""InvoiceId"" INT NOT NULL REFERENCES ""Invoices""(""Id"") ON DELETE RESTRICT, ""AllocatedAmount"" DECIMAL(18,2) NOT NULL, ""AllocationDate"" TIMESTAMP NOT NULL, ""Notes"" VARCHAR(500));",
                    @"CREATE TABLE IF NOT EXISTS ""RequisitionItems"" (""Id"" SERIAL PRIMARY KEY, ""RequisitionId"" INT NOT NULL REFERENCES ""Requisitions""(""Id"") ON DELETE CASCADE, ""ItemDescription"" VARCHAR(200) NOT NULL, ""ItemCode"" VARCHAR(100), ""QuantityRequested"" INT NOT NULL, ""Unit"" VARCHAR(50) NOT NULL, ""EstimatedUnitPrice"" DECIMAL(18,2) NOT NULL, ""Justification"" VARCHAR(500));",
                    @"CREATE TABLE IF NOT EXISTS ""PurchaseOrderItems"" (""Id"" SERIAL PRIMARY KEY, ""PurchaseOrderId"" INT NOT NULL REFERENCES ""PurchaseOrders""(""Id"") ON DELETE CASCADE, ""ItemDescription"" VARCHAR(200) NOT NULL, ""ItemCode"" VARCHAR(100), ""QuantityOrdered"" INT NOT NULL, ""Unit"" VARCHAR(50) NOT NULL, ""UnitPrice"" DECIMAL(18,2) NOT NULL, ""QuantityReceived"" INT NOT NULL DEFAULT 0, ""ReceivedDate"" TIMESTAMP);",
                    @"CREATE TABLE IF NOT EXISTS ""RolePermissions"" (""Id"" SERIAL PRIMARY KEY, ""RoleId"" INT NOT NULL REFERENCES ""Roles""(""Id"") ON DELETE CASCADE, ""PermissionId"" INT NOT NULL REFERENCES ""Permissions""(""Id"") ON DELETE CASCADE, ""AssignedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);",
                    @"CREATE TABLE IF NOT EXISTS ""UserRoles"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" INT NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE, ""RoleId"" INT NOT NULL REFERENCES ""Roles""(""Id"") ON DELETE CASCADE, ""AssignedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""AssignedBy"" VARCHAR(100));",
                    @"CREATE TABLE IF NOT EXISTS ""AuditLogs"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" INT REFERENCES ""Users""(""Id"") ON DELETE SET NULL, ""Username"" VARCHAR(100), ""Action"" VARCHAR(100) NOT NULL, ""Entity"" VARCHAR(100) NOT NULL, ""EntityId"" INT, ""Details"" VARCHAR(1000), ""ActionDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, ""IpAddress"" VARCHAR(50));"
                };

                foreach (var stmt in schemaStatements)
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw(stmt);
                    }
                    catch (Exception stmtEx)
                    {
                        Console.WriteLine($"Statement note: {stmtEx.Message}");
                    }
                }
                Console.WriteLine("Schema tables created manually.");
            }
            catch (Exception schemaEx)
            {
                Console.WriteLine($"Schema creation error: {schemaEx.Message}");
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
builder.Services.AddScoped<IBatchPaymentService, BatchPaymentService>();
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

