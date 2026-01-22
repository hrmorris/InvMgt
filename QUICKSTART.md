# Quick Start Guide - Invoice Management System

## 🚀 Getting Started in 5 Minutes

### Step 1: Restore Packages
Open terminal in the project directory and run:
```bash
dotnet restore
```

### Step 2: Create the Database
Run these commands to create and setup your database:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

If you don't have the EF Core tools installed, run this first:
```bash
dotnet tool install --global dotnet-ef
```

### Step 3: Run the Application
```bash
dotnet run
```

The application will start and be available at:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

### Step 4: Start Using the App
Open your browser and navigate to one of the URLs above. You'll see the dashboard.

---

## 📋 Common Tasks

### Creating Your First Invoice
1. Click **"Invoices"** in the sidebar
2. Click **"Create New Invoice"**
3. Fill in the form:
   - Invoice number (e.g., INV-001)
   - Dates (invoice date and due date)
   - Customer information
   - Add line items
4. Click **"Create Invoice"**

### Recording a Payment
1. Click **"Payments"** in the sidebar
2. Click **"Record New Payment"**
3. Select the invoice from the dropdown
4. Enter payment details
5. Click **"Record Payment"**

### Importing Data
We've included sample CSV files in the `SampleData` folder:
- `sample-invoices.csv` - 5 sample invoices
- `sample-payments.csv` - 5 sample payments

To import:
1. Click **"Import Invoices"** or **"Import Payments"** in the sidebar
2. Select the CSV file
3. Click **"Import"**

### Generating PDF Reports
1. Click **"Reports"** in the sidebar
2. Choose **"Invoice Report"** or **"Payment Report"**
3. Optionally set date filters
4. Click **"Generate PDF Report"**

---

## 🗂️ Import File Formats

### Invoice CSV Format
```csv
InvoiceNumber,InvoiceDate,DueDate,CustomerName,TotalAmount,CustomerAddress,CustomerEmail,CustomerPhone,Notes
INV-001,2025-01-01,2025-01-31,Acme Corp,1000.00,123 Main St,acme@example.com,(555) 123-4567,Sample note
```

### Payment CSV Format
```csv
PaymentNumber,InvoiceId,PaymentDate,Amount,PaymentMethod,ReferenceNumber,Notes
PAY-001,1,2025-01-15,500.00,Bank Transfer,TXN123456,Payment note
```

**Note:** For payments, you need to know the InvoiceId (which is assigned by the database). Create invoices first, then check their IDs before importing payments.

---

## ⚙️ Configuration

### Database Connection
The default connection uses SQL Server LocalDB. To change it:

Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-Connection-String-Here"
  }
}
```

### Common Connection Strings

**LocalDB (default):**
```
Server=(localdb)\\mssqllocaldb;Database=InvoiceManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server with Windows Auth:**
```
Server=localhost;Database=InvoiceManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server with Username/Password:**
```
Server=localhost;Database=InvoiceManagementDb;User Id=yourusername;Password=yourpassword;MultipleActiveResultSets=true
```

---

## 🔧 Troubleshooting

### "Unable to connect to database"
**Solution:** Ensure SQL Server or LocalDB is running. If using LocalDB, it should start automatically.

### "A network-related or instance-specific error"
**Solution:** 
1. Check if SQL Server service is running
2. Verify the connection string in `appsettings.json`
3. Try using `(localdb)\\mssqllocaldb` for LocalDB

### "The term 'dotnet-ef' is not recognized"
**Solution:** Install EF Core tools:
```bash
dotnet tool install --global dotnet-ef
```

### Import fails with "InvoiceId not found"
**Solution:** When importing payments, make sure:
1. Invoices are created first
2. You're using the correct InvoiceId from the database
3. The InvoiceId exists in your Invoices table

### PDF Generation shows error
**Solution:** QuestPDF requires a license setting. The app uses Community license (non-commercial). For commercial use, update the license type in `Services/PdfService.cs`.

---

## 📊 Sample Data

Want to quickly test the system? Use our sample data:

1. **Import Sample Invoices:**
   - Go to Invoices → Import Invoices
   - Select `SampleData/sample-invoices.csv`
   - Click Import

2. **Check Invoice IDs:**
   - Go to Invoices
   - Note the database IDs (they'll be 1, 2, 3, 4, 5 if starting fresh)

3. **Import Sample Payments:**
   - Go to Payments → Import Payments
   - Select `SampleData/sample-payments.csv`
   - Click Import

---

## 🎯 Next Steps

1. **Customize Company Info:** Edit `Services/PdfService.cs` to add your company details
2. **Add Logo:** Modify the PDF header section to include your company logo
3. **Customize Styling:** Edit the CSS in `Views/Shared/_Layout.cshtml`
4. **Add Authentication:** Implement ASP.NET Core Identity for user management
5. **Deploy:** Publish to Azure, IIS, or your preferred hosting platform

---

## 📚 Learn More

- [Full README](README.md) - Complete documentation
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [QuestPDF Documentation](https://www.questpdf.com)

---

## 💡 Tips

- **Invoice Numbers:** Use a consistent format (e.g., INV-YYYY-001)
- **Payment Numbers:** Follow similar pattern (e.g., PAY-YYYY-001)
- **Backup:** Regularly backup your database
- **Testing:** Test import files with small datasets first
- **Reports:** Generate reports periodically for record-keeping

---

## ✅ Checklist

- [ ] .NET 8 SDK installed
- [ ] SQL Server or LocalDB available
- [ ] Packages restored (`dotnet restore`)
- [ ] Database created (`dotnet ef database update`)
- [ ] Application running (`dotnet run`)
- [ ] Created first invoice
- [ ] Recorded first payment
- [ ] Generated first PDF report

---

**Need Help?** Check the troubleshooting section or review the full README.md file.

**Ready to customize?** The codebase is well-structured and commented for easy modifications.

Happy invoicing! 🎉

