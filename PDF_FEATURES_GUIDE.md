# 📄 PDF Document Generation & Download Guide

## Overview
Your Invoice Management System has **professional PDF generation** built-in using QuestPDF. You can generate and download beautiful, formatted PDFs for invoices, payments, and reports!

---

## 🎯 Available PDF Features

### 1. **Invoice PDF Documents**
Generate professional invoice PDFs with:
- ✅ Company header and branding
- ✅ Invoice number, dates (invoice & due date)
- ✅ Customer information (name, address, email, phone)
- ✅ Itemized line items table
- ✅ Totals breakdown (Total, Paid, Balance)
- ✅ Invoice status badge
- ✅ Notes section
- ✅ Page numbering

### 2. **Payment Receipt PDFs**
Generate payment receipts with:
- ✅ Payment number and date
- ✅ Payment method
- ✅ Related invoice information
- ✅ Amount paid (highlighted)
- ✅ Reference numbers
- ✅ Thank you message

### 3. **Invoice Reports**
Generate comprehensive reports with:
- ✅ All invoices in date range
- ✅ Summary statistics
- ✅ Total amounts, paid, balance
- ✅ Landscape format for better visibility
- ✅ Professional table layout

### 4. **Payment Reports**
Generate payment reports with:
- ✅ All payments in date range
- ✅ Total payment amounts
- ✅ Payment methods breakdown
- ✅ Reference tracking

---

## 📥 How to Download Invoice PDFs

### Method 1: From Invoice Details Page
1. Go to **Invoices** in the sidebar
2. Click on any invoice number to view details
3. Click the green **"Download PDF"** button at the top
4. PDF will download immediately with filename: `Invoice_[InvoiceNumber].pdf`

### Method 2: From Invoice List
1. Go to **Invoices** in the sidebar
2. In the Actions column, click the green PDF icon (<i class="bi bi-file-pdf"></i>)
3. PDF downloads instantly!

### Method 3: Direct URL
Navigate to: `http://localhost:5000/Invoices/GeneratePdf/{InvoiceId}`

---

## 📋 PDF Document Structure

### Invoice PDF Layout:

```
┌─────────────────────────────────────────────────┐
│  INVOICE MANAGEMENT SYSTEM                      │
│  Your Company Name                              │
│  Address | Phone | Email                        │
├─────────────────────────────────────────────────┤
│                                                 │
│  INVOICE                         BILL TO:       │
│  Invoice #: INV-001             Customer Name   │
│  Date: 2025-11-07               Address         │
│  Due Date: 2025-12-07           Email & Phone   │
│                                                 │
├─────────────────────────────────────────────────┤
│  LINE ITEMS TABLE                               │
│  # | Description | Qty | Unit Price | Total    │
│  1 | Service A   |  2  | $100.00    | $200.00 │
│  2 | Service B   |  1  | $150.00    | $150.00 │
├─────────────────────────────────────────────────┤
│                          Total Amount: $350.00  │
│                          Paid Amount:  $100.00  │
│                          Balance:      $250.00  │
├─────────────────────────────────────────────────┤
│  Notes: Any special notes here                  │
│  Status: Partial                                │
│                                                 │
│              Page 1 / 1                         │
└─────────────────────────────────────────────────┘
```

---

## 🎨 PDF Styling Features

### Professional Design Elements:
- **Blue header** with company branding
- **Bold invoice number** for easy identification
- **Table borders** for clear line item separation
- **Color-coded amounts**:
  - Total: Bold black
  - Paid: Green
  - Balance: Red (attention-grabbing)
- **Status badges** matching web interface
- **Page numbers** on footer

### Typography:
- Header: 20pt bold
- Invoice #: 24pt bold
- Body text: 11pt
- Summary totals: 14pt bold
- Professional font (default system font)

---

## 🔧 Technical Details

### PDF Generation Engine
- **Library**: QuestPDF (Community License)
- **Format**: A4 page size
- **Margins**: 2cm all sides
- **Orientation**: Portrait (invoices/payments), Landscape (reports)
- **File Size**: ~50-100 KB per invoice
- **Quality**: High-resolution, print-ready

### File Naming Convention
- Invoices: `Invoice_[InvoiceNumber].pdf`
- Payments: `Payment_[PaymentNumber].pdf`
- Reports: `InvoiceReport_[StartDate]_[EndDate].pdf`

### Browser Behavior
- **Download**: PDF automatically downloads to your Downloads folder
- **Filename**: Descriptive name based on document type
- **Format**: Standard PDF (readable by all PDF readers)

---

## 🚀 Quick Start - Test PDF Generation

### Create and Download Your First Invoice PDF:

1. **Create a test invoice**:
   - Go to **Invoices** → **Create New Invoice**
   - Fill in:
     - Invoice Number: `TEST-PDF-001`
     - Customer: `Test Customer`
     - Add at least one line item
   - Click **Create Invoice**

2. **Download the PDF**:
   - Click on the invoice number `TEST-PDF-001`
   - Click green **"Download PDF"** button
   - Check your Downloads folder!

3. **Open the PDF**:
   - PDF will be named `Invoice_TEST-PDF-001.pdf`
   - Open with any PDF reader (Preview, Adobe, Chrome, etc.)
   - Professional invoice ready to email or print!

---

## 📊 Report Generation

### Generate Invoice Report:
1. Go to **Reports** in sidebar
2. Select date range (optional)
3. Click **"Generate Invoice Report PDF"**
4. Download comprehensive PDF with all invoices!

### Generate Payment Report:
1. Go to **Reports** in sidebar
2. Select date range (optional)
3. Click **"Generate Payment Report PDF"**
4. Download PDF with all payments!

---

## 💡 Use Cases

### For Accounting:
- ✅ Download invoice PDFs to email to customers
- ✅ Print invoices for physical mailing
- ✅ Archive invoices as PDF for records
- ✅ Generate monthly reports

### For Customers:
- ✅ Professional invoices they can save
- ✅ Payment receipts for their records
- ✅ Clear itemization of charges
- ✅ Easy to forward to accounting department

### For Management:
- ✅ Quick overview reports
- ✅ Financial summaries
- ✅ Period-based analysis
- ✅ Printable documents for meetings

---

## 🎯 PDF Quality & Features

| Feature | Status |
|---------|--------|
| Professional Layout | ✅ |
| Company Branding | ✅ |
| Line Items Table | ✅ |
| Color Coding | ✅ |
| Page Numbering | ✅ |
| Print Ready | ✅ |
| Email Friendly | ✅ |
| Small File Size | ✅ |
| Standards Compliant | ✅ |
| Cross-Platform | ✅ |

---

## 🔐 Customization Options

### Want to Customize?
Edit `/Services/PdfService.cs` to change:
- **Company name** in header (line 126)
- **Company address** (line 127)
- **Contact info** (line 128)
- **Colors and styling**
- **Logo placement** (line 131 - placeholder for logo)
- **Font sizes and styles**

### Example Customization:
```csharp
// In ComposeHeader method:
column.Item().Text("Your Company Name Here").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
column.Item().Text("123 Main Street, City, State 12345").FontSize(12);
column.Item().Text("Phone: (555) 123-4567 | Email: billing@yourcompany.com").FontSize(9);
```

---

## 📝 Current Server Setup

**Server**: http://localhost:5000  
**PDF Endpoints**:
- Invoice PDF: `/Invoices/GeneratePdf/{id}`
- Payment PDF: `/Payments/GenerateReceipt/{id}`
- Invoice Report: `/Reports/InvoiceReport`
- Payment Report: `/Reports/PaymentReport`

---

## ✅ Testing Checklist

Test your PDF generation:
- [ ] Create an invoice with multiple line items
- [ ] Download PDF from Details page
- [ ] Download PDF from Index page (PDF icon)
- [ ] Verify PDF opens correctly
- [ ] Check all information is accurate
- [ ] Verify totals calculate correctly
- [ ] Test with different invoice statuses
- [ ] Generate a payment receipt PDF
- [ ] Generate an invoice report PDF

---

## 🎉 Result

You have **enterprise-grade PDF generation** built into your system:
- Professional documents
- One-click download
- Print-ready quality
- Customer-ready invoices
- Complete reporting

**Start generating PDFs now!** 📄✨

---

## 💼 Pro Tips

1. **Email Integration**: You can easily add email functionality to automatically send invoice PDFs
2. **Bulk Download**: Generate PDFs for multiple invoices at once
3. **Custom Templates**: Create different PDF templates for different invoice types
4. **Watermarks**: Add "PAID" or "OVERDUE" watermarks based on status
5. **Multi-Language**: Easily translate PDF text for international customers

---

**Your PDF system is ready to use!** 🚀

