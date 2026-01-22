# Invoice Management System

A comprehensive .NET 8 web application for managing invoices, payments, and generating PDF reports.

## Features

- **Invoice Management**
  - Create, edit, and delete invoices
  - Add multiple line items to invoices
  - Track invoice status (Unpaid, Partial, Paid, Overdue)
  - Search and filter invoices
  - View overdue invoices

- **Payment Management**
  - Record payments against invoices
  - Multiple payment methods supported (Cash, Check, Credit Card, Bank Transfer, etc.)
  - Automatic invoice balance calculation
  - Payment history tracking

- **Import/Export**
  - Import invoices from CSV or Excel files
  - Import payments from CSV or Excel files
  - Sample templates provided

- **PDF Reports**
  - Generate individual invoice PDFs
  - Generate payment receipt PDFs
  - Generate comprehensive invoice reports
  - Generate payment reports with date filtering

- **Dashboard**
  - Overview of all invoices and payments
  - Quick statistics
  - Recent activity
  - Alerts for overdue invoices

## Technology Stack

- **Framework**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server with Entity Framework Core
- **PDF Generation**: QuestPDF
- **Excel/CSV Import**: EPPlus, CsvHelper
- **UI**: Bootstrap 5 with custom styling

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or full installation)
- Visual Studio 2022 or Visual Studio Code

## Installation

1. **Clone or download the project**

2. **Restore NuGet packages**
```bash
dotnet restore
```

3. **Update database connection string** (if needed)
   
   Edit `appsettings.json` and modify the connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InvoiceManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```

4. **Create the database**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

5. **Run the application**
```bash
dotnet run
```

6. **Access the application**
   
   Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## Usage

### Creating an Invoice

1. Navigate to **Invoices** > **Create New Invoice**
2. Fill in invoice details (number, dates, customer information)
3. Add line items with description, quantity, and unit price
4. Click **Create Invoice**

### Recording a Payment

1. Navigate to **Payments** > **Record New Payment**
2. Select the invoice from the dropdown
3. Enter payment details (amount, method, reference)
4. Click **Record Payment**

### Importing Data

#### Invoice Import Format (CSV/Excel)
```csv
InvoiceNumber,InvoiceDate,DueDate,CustomerName,TotalAmount,CustomerAddress,CustomerEmail,CustomerPhone,Notes
INV-001,2025-01-01,2025-01-31,Acme Corporation,1000.00,123 Main St,acme@example.com,(555) 123-4567,Sample invoice
```

#### Payment Import Format (CSV/Excel)
```csv
PaymentNumber,InvoiceId,PaymentDate,Amount,PaymentMethod,ReferenceNumber,Notes
PAY-001,1,2025-01-15,500.00,Bank Transfer,TXN123456,Partial payment
```

### Generating Reports

1. Navigate to **Reports**
2. Choose either Invoice Report or Payment Report
3. Optionally set date range filters
4. Click **Generate PDF Report**
5. The PDF will download automatically

## Project Structure

```
InvMgt/
├── Controllers/          # MVC Controllers
├── Models/              # Data models
├── Services/            # Business logic services
├── Data/                # Database context
├── Views/               # Razor views
├── appsettings.json     # Configuration
├── Program.cs           # Application entry point
└── README.md           # This file
```

## Key Features Explained

### Invoice Status Management
The system automatically updates invoice status based on payments:
- **Unpaid**: No payments received
- **Partial**: Some payment received but balance remains
- **Paid**: Full payment received
- **Overdue**: Due date passed and not fully paid

### PDF Generation
All PDF documents include:
- Professional formatting
- Company branding area (customizable)
- Detailed line items
- Payment information
- Status indicators

### Data Import
- Supports both CSV and Excel formats
- Validates data before import
- Provides helpful error messages
- Sample templates included in documentation

## Customization

### Company Information
Edit the header section in `Services/PdfService.cs` to customize:
- Company name
- Address
- Phone and email
- Logo (add using QuestPDF image methods)

### Database Provider
To use a different database provider:
1. Update NuGet packages
2. Modify `Program.cs` to use different provider
3. Update connection string in `appsettings.json`

## Troubleshooting

### Database Connection Issues
- Ensure SQL Server is running
- Verify connection string is correct
- Run migrations: `dotnet ef database update`

### Import Errors
- Check file format matches templates
- Ensure all required columns are present
- Verify date formats (yyyy-MM-dd)
- Check for duplicate invoice/payment numbers

### PDF Generation Issues
- Ensure QuestPDF license is set correctly
- Check for null references in data
- Verify all required fields are populated

## Future Enhancements

Potential features to add:
- User authentication and authorization
- Email invoice/receipt functionality
- Recurring invoices
- Multi-currency support
- Custom report templates
- API endpoints for integration
- Mobile responsive improvements
- Dark mode

## License

This project uses QuestPDF Community License (for non-commercial use).
For commercial use, obtain appropriate QuestPDF license.

EPPlus is used under the Polyform Noncommercial License.
For commercial use, obtain EPPlus commercial license.

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review error messages carefully
3. Ensure all prerequisites are installed
4. Verify database is properly configured

## Version History

- **1.0.0** (2025-11-07)
  - Initial release
  - Invoice and payment management
  - Import functionality
  - PDF report generation
  - Dashboard and statistics

