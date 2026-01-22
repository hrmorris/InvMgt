using InvoiceManagement.Models;
using InvoiceManagement.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceManagement.Services
{
    public class PdfService : IPdfService
    {
        private readonly IAdminService _adminService;
        private readonly ICurrencyService _currencyService;

        public PdfService(IAdminService adminService, ICurrencyService currencyService)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            _adminService = adminService;
            _currencyService = currencyService;
        }

        private async Task<CompanyInfo> GetCompanyInfoAsync()
        {
            return new CompanyInfo
            {
                Name = await _adminService.GetSettingValueAsync("CompanyName") ?? "Your Company Name",
                Address = await _adminService.GetSettingValueAsync("CompanyAddress") ?? "Address Line 1",
                Phone = await _adminService.GetSettingValueAsync("CompanyPhone") ?? "(123) 456-7890",
                Email = await _adminService.GetSettingValueAsync("CompanyEmail") ?? "info@company.com",
                Website = await _adminService.GetSettingValueAsync("CompanyWebsite") ?? "www.company.com"
            };
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice)
        {
            var companyInfo = await GetCompanyInfoAsync();
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, companyInfo));

                    page.Content().Element(content => ComposeInvoiceContent(content, invoice, currencySettings));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GeneratePaymentReceiptPdfAsync(Payment payment)
        {
            var companyInfo = await GetCompanyInfoAsync();
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, companyInfo));

                    page.Content().Element(content => ComposePaymentContent(content, payment, currencySettings));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateInvoiceReportPdfAsync(IEnumerable<Invoice> invoices, DateTime? startDate, DateTime? endDate)
        {
            var companyInfo = await GetCompanyInfoAsync();
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, companyInfo));

                    page.Content().Element(content => ComposeInvoiceReport(content, invoices, startDate, endDate, currencySettings));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GeneratePaymentReportPdfAsync(IEnumerable<Payment> payments, DateTime? startDate, DateTime? endDate)
        {
            var companyInfo = await GetCompanyInfoAsync();
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, companyInfo));

                    page.Content().Element(content => ComposePaymentReport(content, payments, startDate, endDate, currencySettings));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateSupplierInvoiceListPdfAsync(IEnumerable<Invoice> invoices, DateTime? startDate, DateTime? endDate)
        {
            var companyInfo = await GetCompanyInfoAsync();
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, companyInfo));

                    page.Content().Element(content => ComposeSupplierInvoiceListReport(content, invoices, startDate, endDate, currencySettings));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, CompanyInfo companyInfo)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Invoice Management System").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().Text(companyInfo.Name).FontSize(12);
                    column.Item().Text(companyInfo.Address).FontSize(9);
                    column.Item().Text($"Phone: {companyInfo.Phone} | Email: {companyInfo.Email}").FontSize(9);
                    if (!string.IsNullOrEmpty(companyInfo.Website))
                    {
                        column.Item().Text($"Website: {companyInfo.Website}").FontSize(9);
                    }
                });

                row.ConstantItem(100).Height(50).Placeholder();
            });
        }

        private string FormatCurrency(decimal amount, CurrencySettings settings)
        {
            return CurrencyHelper.FormatCurrency(amount, settings);
        }

        private void ComposeInvoiceContent(IContainer container, Invoice invoice, CurrencySettings currencySettings)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                // Determine if this is a supplier invoice (AP) or customer invoice (AR)
                bool isSupplierInvoice = invoice.InvoiceType == "Supplier" || invoice.SupplierId.HasValue;

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(innerColumn =>
                    {
                        innerColumn.Item().Text(isSupplierInvoice ? "BILL" : "INVOICE").FontSize(24).Bold();
                        innerColumn.Item().Text($"Invoice #: {invoice.InvoiceNumber}").FontSize(12);
                        innerColumn.Item().Text($"Date: {invoice.InvoiceDate:dd/MM/yyyy}").FontSize(12);
                        innerColumn.Item().Text($"Due Date: {invoice.DueDate:dd/MM/yyyy}").FontSize(12);
                        if (isSupplierInvoice)
                        {
                            innerColumn.Item().Text("Type: Supplier Invoice (Payable)").FontSize(10).Italic();
                        }
                    });

                    row.RelativeItem().Column(innerColumn =>
                    {
                        if (isSupplierInvoice)
                        {
                            // For supplier invoices - show supplier as "From" and company as "To"
                            innerColumn.Item().Text("Bill From (Supplier):").Bold().FontSize(12);
                            if (invoice.Supplier != null)
                            {
                                innerColumn.Item().Text(invoice.Supplier.SupplierName).FontSize(11);
                                if (!string.IsNullOrEmpty(invoice.Supplier.Address))
                                    innerColumn.Item().Text(invoice.Supplier.Address).FontSize(10);
                                if (!string.IsNullOrEmpty(invoice.Supplier.Email))
                                    innerColumn.Item().Text(invoice.Supplier.Email).FontSize(10);
                                if (!string.IsNullOrEmpty(invoice.Supplier.Phone))
                                    innerColumn.Item().Text(invoice.Supplier.Phone).FontSize(10);
                            }
                            else
                            {
                                innerColumn.Item().Text(invoice.CustomerName).FontSize(11);
                                if (!string.IsNullOrEmpty(invoice.CustomerAddress))
                                    innerColumn.Item().Text(invoice.CustomerAddress).FontSize(10);
                                if (!string.IsNullOrEmpty(invoice.CustomerEmail))
                                    innerColumn.Item().Text(invoice.CustomerEmail).FontSize(10);
                                if (!string.IsNullOrEmpty(invoice.CustomerPhone))
                                    innerColumn.Item().Text(invoice.CustomerPhone).FontSize(10);
                            }
                        }
                        else
                        {
                            // For customer invoices - show customer as "To"
                            innerColumn.Item().Text("Bill To (Customer):").Bold().FontSize(12);
                            innerColumn.Item().Text(invoice.CustomerName).FontSize(11);
                            if (!string.IsNullOrEmpty(invoice.CustomerAddress))
                                innerColumn.Item().Text(invoice.CustomerAddress).FontSize(10);
                            if (!string.IsNullOrEmpty(invoice.CustomerEmail))
                                innerColumn.Item().Text(invoice.CustomerEmail).FontSize(10);
                            if (!string.IsNullOrEmpty(invoice.CustomerPhone))
                                innerColumn.Item().Text(invoice.CustomerPhone).FontSize(10);
                        }
                    });
                });

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#").Bold();
                        header.Cell().Element(CellStyle).Text("Description").Bold();
                        header.Cell().Element(CellStyle).Text("Quantity").Bold();
                        header.Cell().Element(CellStyle).Text("Unit Price").Bold();
                        header.Cell().Element(CellStyle).Text("Total").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    var index = 1;
                    foreach (var item in invoice.InvoiceItems)
                    {
                        table.Cell().Element(CellStyle).Text(index++.ToString());
                        table.Cell().Element(CellStyle).Text(item.Description);
                        table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(item.UnitPrice, currencySettings));
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(item.TotalPrice, currencySettings));

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }
                });

                column.Item().PaddingTop(10).AlignRight().Column(summaryColumn =>
                {
                    summaryColumn.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Subtotal:");
                        row.ConstantItem(150).AlignRight().Text(FormatCurrency(invoice.SubTotal, currencySettings));
                    });
                    summaryColumn.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text($"GST ({invoice.GSTRate}%):");
                        row.ConstantItem(150).AlignRight().Text(FormatCurrency(invoice.GSTAmount, currencySettings));
                    });
                    summaryColumn.Item().PaddingTop(5).Row(row =>
                    {
                        row.ConstantItem(120).Text("Total Amount:").Bold();
                        row.ConstantItem(150).AlignRight().Text(FormatCurrency(invoice.TotalAmount, currencySettings)).Bold();
                    });
                    summaryColumn.Item().PaddingTop(10).Row(row =>
                    {
                        row.ConstantItem(120).Text("Paid Amount:");
                        row.ConstantItem(150).AlignRight().Text(FormatCurrency(invoice.PaidAmount, currencySettings));
                    });
                    summaryColumn.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Balance:").Bold().FontSize(14);
                        row.ConstantItem(150).AlignRight().Text(FormatCurrency(invoice.BalanceAmount, currencySettings)).Bold().FontSize(14).FontColor(Colors.Red.Medium);
                    });
                });

                if (!string.IsNullOrEmpty(invoice.Notes))
                {
                    column.Item().PaddingTop(20).Column(notesColumn =>
                    {
                        notesColumn.Item().Text("Notes:").Bold();
                        notesColumn.Item().Text(invoice.Notes);
                    });
                }

                // For supplier invoices, add company info as "Bill To"
                if (isSupplierInvoice)
                {
                    column.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(10);
                    column.Item().Column(companyColumn =>
                    {
                        companyColumn.Item().Text("Bill To (Our Company):").Bold().FontSize(12);
                        var companyInfo = GetCompanyInfoAsync().Result;
                        companyColumn.Item().Text(companyInfo.Name).FontSize(11);
                        companyColumn.Item().Text(companyInfo.Address).FontSize(10);
                        companyColumn.Item().Text(companyInfo.Email).FontSize(10);
                        companyColumn.Item().Text(companyInfo.Phone).FontSize(10);
                    });
                }

                column.Item().PaddingTop(20).Text($"Status: {invoice.Status}").Bold().FontSize(12);
            });
        }

        private void ComposePaymentContent(IContainer container, Payment payment, CurrencySettings currencySettings)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                column.Item().Text("PAYMENT RECEIPT").FontSize(24).Bold();

                column.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem().Column(innerColumn =>
                    {
                        innerColumn.Item().Text($"Payment #: {payment.PaymentNumber}").FontSize(12);
                        innerColumn.Item().Text($"Payment Date: {payment.PaymentDate:dd/MM/yyyy}").FontSize(12);
                        innerColumn.Item().Text($"Payment Method: {payment.PaymentMethod}").FontSize(12);
                        if (!string.IsNullOrEmpty(payment.ReferenceNumber))
                            innerColumn.Item().Text($"Reference: {payment.ReferenceNumber}").FontSize(12);
                    });

                    row.RelativeItem().Column(innerColumn =>
                    {
                        innerColumn.Item().Text("Invoice Information:").Bold().FontSize(12);
                        if (payment.Invoice != null)
                        {
                            innerColumn.Item().Text($"Invoice #: {payment.Invoice.InvoiceNumber}").FontSize(11);
                            innerColumn.Item().Text($"Customer: {payment.Invoice.CustomerName}").FontSize(11);
                        }
                    });
                });

                column.Item().PaddingTop(20).BorderTop(2).BorderColor(Colors.Blue.Medium).PaddingTop(10);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Amount Paid:").Bold().FontSize(16);
                    row.RelativeItem().AlignRight().Text(FormatCurrency(payment.Amount, currencySettings)).Bold().FontSize(18).FontColor(Colors.Green.Medium);
                });

                if (!string.IsNullOrEmpty(payment.Notes))
                {
                    column.Item().PaddingTop(20).Column(notesColumn =>
                    {
                        notesColumn.Item().Text("Notes:").Bold();
                        notesColumn.Item().Text(payment.Notes);
                    });
                }

                column.Item().PaddingTop(40).Text("Thank you for your payment!").FontSize(14).Italic();
            });
        }

        private void ComposeInvoiceReport(IContainer container, IEnumerable<Invoice> invoices, DateTime? startDate, DateTime? endDate, CurrencySettings currencySettings)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                column.Item().Text("INVOICE REPORT").FontSize(20).Bold();

                if (startDate.HasValue || endDate.HasValue)
                {
                    var dateRange = $"Period: {(startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : "Beginning")} to {(endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : "Present")}";
                    column.Item().Text(dateRange).FontSize(12);
                }

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Invoice #").Bold();
                        header.Cell().Element(CellStyle).Text("Supplier/Customer").Bold();
                        header.Cell().Element(CellStyle).Text("Date").Bold();
                        header.Cell().Element(CellStyle).Text("Due Date").Bold();
                        header.Cell().Element(CellStyle).Text("Total").Bold();
                        header.Cell().Element(CellStyle).Text("Paid").Bold();
                        header.Cell().Element(CellStyle).Text("Status").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var invoice in invoices)
                    {
                        table.Cell().Element(RowCellStyle).Text(invoice.InvoiceNumber);
                        table.Cell().Element(RowCellStyle).Text(invoice.CustomerName);
                        table.Cell().Element(RowCellStyle).Text(invoice.InvoiceDate.ToString("dd/MM/yyyy"));
                        table.Cell().Element(RowCellStyle).Text(invoice.DueDate.ToString("dd/MM/yyyy"));
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(invoice.TotalAmount, currencySettings));
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(invoice.PaidAmount, currencySettings));
                        table.Cell().Element(RowCellStyle).Text(invoice.Status);

                        static IContainer RowCellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }

                    // Summary row
                    var totalAmount = invoices.Sum(i => i.TotalAmount);
                    var totalPaid = invoices.Sum(i => i.PaidAmount);
                    var totalBalance = totalAmount - totalPaid;

                    table.Cell().ColumnSpan(4).Element(SummaryCellStyle).Text("TOTALS").Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalAmount, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalPaid, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalBalance, currencySettings)).Bold();

                    static IContainer SummaryCellStyle(IContainer container)
                    {
                        return container.BorderTop(2).BorderColor(Colors.Black).PaddingVertical(5);
                    }
                });

                column.Item().PaddingTop(10).Text($"Total Invoices: {invoices.Count()}").FontSize(11);
            });
        }

        private void ComposePaymentReport(IContainer container, IEnumerable<Payment> payments, DateTime? startDate, DateTime? endDate, CurrencySettings currencySettings)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                column.Item().Text("PAYMENT REPORT").FontSize(20).Bold();

                if (startDate.HasValue || endDate.HasValue)
                {
                    var dateRange = $"Period: {(startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : "Beginning")} to {(endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : "Present")}";
                    column.Item().Text(dateRange).FontSize(12);
                }

                column.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);      // Payment #
                        columns.RelativeColumn(0.8f);   // Date
                        columns.RelativeColumn(1.2f);   // Ref Number
                        columns.RelativeColumn(1.5f);   // Payee/Supplier
                        columns.RelativeColumn(1.2f);   // Bank Account
                        columns.RelativeColumn(0.8f);   // Method
                        columns.RelativeColumn(1);      // Amount
                        columns.RelativeColumn(0.7f);   // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Payment #").Bold();
                        header.Cell().Element(CellStyle).Text("Date").Bold();
                        header.Cell().Element(CellStyle).Text("Reference").Bold();
                        header.Cell().Element(CellStyle).Text("Payee/Supplier").Bold();
                        header.Cell().Element(CellStyle).Text("Bank Account").Bold();
                        header.Cell().Element(CellStyle).Text("Method").Bold();
                        header.Cell().Element(CellStyle).Text("Amount").Bold();
                        header.Cell().Element(CellStyle).Text("Status").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var payment in payments)
                    {
                        // Determine payee name: use TransferTo > PayeeName > Supplier from allocations
                        var payeeName = !string.IsNullOrEmpty(payment.TransferTo) 
                            ? payment.TransferTo 
                            : !string.IsNullOrEmpty(payment.PayeeName) 
                                ? payment.PayeeName 
                                : payment.PaymentAllocations?.FirstOrDefault()?.Invoice?.Supplier?.SupplierName ?? "-";
                        
                        // Display bank account (prefer parsed payee account number)
                        var bankAccount = !string.IsNullOrEmpty(payment.BankAccountNumber) 
                            ? payment.BankAccountNumber 
                            : !string.IsNullOrEmpty(payment.PayeeAccountNumber)
                                ? $"{payment.PayeeBranchNumber}-{payment.PayeeAccountNumber}"
                                : "-";

                        table.Cell().Element(RowCellStyle).Text(payment.PaymentNumber);
                        table.Cell().Element(RowCellStyle).Text(payment.PaymentDate.ToString("dd/MM/yyyy"));
                        table.Cell().Element(RowCellStyle).Text(payment.ReferenceNumber ?? "-");
                        table.Cell().Element(RowCellStyle).Text(payeeName.Length > 25 ? payeeName.Substring(0, 22) + "..." : payeeName);
                        table.Cell().Element(RowCellStyle).Text(bankAccount);
                        table.Cell().Element(RowCellStyle).Text(payment.PaymentMethod);
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(payment.Amount, currencySettings));
                        table.Cell().Element(RowCellStyle).Text(payment.Status);

                        static IContainer RowCellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }

                    // Summary row
                    var totalAmount = payments.Sum(p => p.Amount);

                    table.Cell().ColumnSpan(6).Element(SummaryCellStyle).Text("TOTAL").Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalAmount, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle);

                    static IContainer SummaryCellStyle(IContainer container)
                    {
                        return container.BorderTop(2).BorderColor(Colors.Black).PaddingVertical(5);
                    }
                });

                // Summary statistics
                column.Item().PaddingTop(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Total Payments: {payments.Count()}").FontSize(11);
                        col.Item().Text($"Bank Transfers: {payments.Count(p => p.PaymentMethod == "Bank Transfer")}").FontSize(10);
                        col.Item().Text($"Internal (BSP): {payments.Count(p => p.AccountType == "Internal")}").FontSize(10);
                        col.Item().Text($"Domestic: {payments.Count(p => p.AccountType == "Domestic")}").FontSize(10);
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Fully Allocated: {payments.Count(p => p.Status == "Fully Allocated")}").FontSize(10);
                        col.Item().Text($"Partially Allocated: {payments.Count(p => p.Status == "Partially Allocated")}").FontSize(10);
                        col.Item().Text($"Unallocated: {payments.Count(p => p.Status == "Unallocated")}").FontSize(10);
                    });
                });
            });
        }

        private void ComposeSupplierInvoiceListReport(IContainer container, IEnumerable<Invoice> invoices, DateTime? startDate, DateTime? endDate, CurrencySettings currencySettings)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                column.Item().Text("SUPPLIER INVOICE LIST").FontSize(20).Bold();

                if (startDate.HasValue || endDate.HasValue)
                {
                    var dateRange = $"Period: {(startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : "Beginning")} to {(endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : "Present")}";
                    column.Item().Text(dateRange).FontSize(12);
                }

                column.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);   // Invoice #
                        columns.RelativeColumn(1f);     // Date
                        columns.RelativeColumn(1f);     // Due Date
                        columns.RelativeColumn(1.5f);   // Supplier
                        columns.RelativeColumn(1f);     // Total
                        columns.RelativeColumn(1f);     // Paid
                        columns.RelativeColumn(1f);     // Balance
                        columns.RelativeColumn(0.8f);   // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Invoice #").Bold();
                        header.Cell().Element(CellStyle).Text("Date").Bold();
                        header.Cell().Element(CellStyle).Text("Due Date").Bold();
                        header.Cell().Element(CellStyle).Text("Supplier").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Paid").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Balance").Bold();
                        header.Cell().Element(CellStyle).Text("Status").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var invoice in invoices)
                    {
                        table.Cell().Element(RowCellStyle).Text(invoice.InvoiceNumber);
                        table.Cell().Element(RowCellStyle).Text(invoice.InvoiceDate.ToString("dd/MM/yyyy"));
                        table.Cell().Element(RowCellStyle).Text(invoice.DueDate.ToString("dd/MM/yyyy"));
                        table.Cell().Element(RowCellStyle).Text(invoice.Supplier?.SupplierName ?? "N/A");
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(invoice.TotalAmount, currencySettings));
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(invoice.PaidAmount, currencySettings));
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(invoice.BalanceAmount, currencySettings));
                        table.Cell().Element(RowCellStyle).Text(invoice.Status);

                        static IContainer RowCellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }

                    // Summary row
                    var totalAmount = invoices.Sum(i => i.TotalAmount);
                    var totalPaid = invoices.Sum(i => i.PaidAmount);
                    var totalBalance = invoices.Sum(i => i.BalanceAmount);

                    table.Cell().ColumnSpan(4).Element(SummaryCellStyle).Text("TOTALS").Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalAmount, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalPaid, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalBalance, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle);

                    static IContainer SummaryCellStyle(IContainer container)
                    {
                        return container.BorderTop(2).BorderColor(Colors.Black).PaddingVertical(5);
                    }
                });

                column.Item().PaddingTop(10).Text($"Total Invoices: {invoices.Count()}").FontSize(11);
            });
        }

        public async Task<byte[]> GeneratePaymentsListPdfAsync(IEnumerable<Payment> payments, DateTime? startDate, DateTime? endDate)
        {
            var companyInfo = await GetCompanyInfoAsync();
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, companyInfo));

                    page.Content().Element(content => ComposePaymentsListReport(content, payments, startDate, endDate, currencySettings));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposePaymentsListReport(IContainer container, IEnumerable<Payment> payments, DateTime? startDate, DateTime? endDate, CurrencySettings currencySettings)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                column.Item().Text("PAYMENTS LIST").FontSize(20).Bold();

                if (startDate.HasValue || endDate.HasValue)
                {
                    var dateRange = $"Period: {(startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : "Beginning")} to {(endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : "Present")}";
                    column.Item().Text(dateRange).FontSize(12);
                }

                column.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);   // Payment #
                        columns.RelativeColumn(1f);     // Date
                        columns.RelativeColumn(1f);     // Method
                        columns.RelativeColumn(1.5f);   // Reference
                        columns.RelativeColumn(1f);     // Amount
                        columns.RelativeColumn(1f);     // Allocated
                        columns.RelativeColumn(1f);     // Unallocated
                        columns.RelativeColumn(0.8f);   // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Payment #").Bold();
                        header.Cell().Element(CellStyle).Text("Date").Bold();
                        header.Cell().Element(CellStyle).Text("Method").Bold();
                        header.Cell().Element(CellStyle).Text("Reference").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Amount").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Allocated").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Unallocated").Bold();
                        header.Cell().Element(CellStyle).Text("Status").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var payment in payments)
                    {
                        table.Cell().Element(RowCellStyle).Text(payment.PaymentNumber);
                        table.Cell().Element(RowCellStyle).Text(payment.PaymentDate.ToString("dd/MM/yyyy"));
                        table.Cell().Element(RowCellStyle).Text(payment.PaymentMethod);
                        table.Cell().Element(RowCellStyle).Text(payment.ReferenceNumber ?? "-");
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(payment.Amount, currencySettings));
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(payment.AllocatedAmount, currencySettings));
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(payment.UnallocatedAmount, currencySettings));
                        table.Cell().Element(RowCellStyle).Text(payment.Status);

                        static IContainer RowCellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }

                    // Summary row
                    var totalAmount = payments.Sum(p => p.Amount);
                    var totalAllocated = payments.Sum(p => p.AllocatedAmount);
                    var totalUnallocated = payments.Sum(p => p.UnallocatedAmount);

                    table.Cell().ColumnSpan(4).Element(SummaryCellStyle).Text("TOTALS").Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalAmount, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalAllocated, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle).AlignRight().Text(FormatCurrency(totalUnallocated, currencySettings)).Bold();
                    table.Cell().Element(SummaryCellStyle);

                    static IContainer SummaryCellStyle(IContainer container)
                    {
                        return container.BorderTop(2).BorderColor(Colors.Black).PaddingVertical(5);
                    }
                });

                column.Item().PaddingTop(10).Text($"Total Payments: {payments.Count()}").FontSize(11);
            });
        }

        public async Task<byte[]> GenerateRequisitionPdfAsync(Requisition requisition)
        {
            var companyInfo = await GetCompanyInfoAsync();
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, companyInfo));

                    page.Content().Element(content => ComposeRequisitionContent(content, requisition, currencySettings));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GeneratePurchaseOrderPdfAsync(PurchaseOrder purchaseOrder)
        {
            var companyInfo = await GetCompanyInfoAsync();
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, companyInfo));

                    page.Content().Element(content => ComposePurchaseOrderContent(content, purchaseOrder, currencySettings));

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeRequisitionContent(IContainer container, Requisition requisition, CurrencySettings currencySettings)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(innerColumn =>
                    {
                        innerColumn.Item().Text("PURCHASE REQUISITION").FontSize(24).Bold().FontColor(Colors.Blue.Medium);
                        innerColumn.Item().Text($"Requisition #: {requisition.RequisitionNumber}").FontSize(12);
                        innerColumn.Item().Text($"Date: {requisition.RequisitionDate:dd/MM/yyyy}").FontSize(12);
                        innerColumn.Item().Text($"Status: {requisition.Status.Replace("_", " ")}").FontSize(12);
                    });

                    row.RelativeItem().Column(innerColumn =>
                    {
                        innerColumn.Item().Text("Requisition Details:").Bold().FontSize(12);
                        innerColumn.Item().Text($"Requested By: {requisition.RequestedBy}").FontSize(11);
                        innerColumn.Item().Text($"Department: {requisition.Department}").FontSize(11);
                        innerColumn.Item().Text($"Facility: {requisition.FacilityType}").FontSize(11);
                        innerColumn.Item().Text($"Purpose: {requisition.Purpose}").FontSize(11);
                    });
                });

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Cost Code: {requisition.CostCode}").FontSize(10);
                    row.RelativeItem().Text($"Budget Code: {requisition.BudgetCode}").FontSize(10);
                });

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#").Bold();
                        header.Cell().Element(CellStyle).Text("Item Description").Bold();
                        header.Cell().Element(CellStyle).Text("Code").Bold();
                        header.Cell().Element(CellStyle).Text("Qty").Bold();
                        header.Cell().Element(CellStyle).Text("Unit").Bold();
                        header.Cell().Element(CellStyle).Text("Est. Price").Bold();
                        header.Cell().Element(CellStyle).Text("Total").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    var index = 1;
                    foreach (var item in requisition.RequisitionItems)
                    {
                        table.Cell().Element(RowCellStyle).Text(index++.ToString());
                        table.Cell().Element(RowCellStyle).Text(item.ItemDescription);
                        table.Cell().Element(RowCellStyle).Text(item.ItemCode ?? "-");
                        table.Cell().Element(RowCellStyle).AlignRight().Text(item.QuantityRequested.ToString());
                        table.Cell().Element(RowCellStyle).Text(item.Unit);
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(item.EstimatedUnitPrice, currencySettings));
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(item.EstimatedTotal, currencySettings));

                        static IContainer RowCellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }
                });

                column.Item().PaddingTop(10).AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text("Total Estimated Amount:").Bold().FontSize(14);
                    row.ConstantItem(150).AlignRight().Text(FormatCurrency(requisition.EstimatedAmount, currencySettings)).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                });

                if (!string.IsNullOrEmpty(requisition.Notes))
                {
                    column.Item().PaddingTop(20).Column(notesColumn =>
                    {
                        notesColumn.Item().Text("Notes:").Bold();
                        notesColumn.Item().Text(requisition.Notes);
                    });
                }

                // Approval signatures section
                column.Item().PaddingTop(40).BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(20);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Supervisor Approval:").Bold();
                        if (!string.IsNullOrEmpty(requisition.SupervisorName))
                        {
                            col.Item().PaddingTop(5).Text($"Name: {requisition.SupervisorName}");
                            col.Item().Text($"Date: {requisition.SupervisorApprovalDate?.ToString("dd/MM/yyyy")}");
                        }
                        else
                        {
                            col.Item().PaddingTop(20).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(5).Text("Signature & Date");
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Finance Screening:").Bold();
                        if (!string.IsNullOrEmpty(requisition.FinanceOfficerName))
                        {
                            col.Item().PaddingTop(5).Text($"Name: {requisition.FinanceOfficerName}");
                            col.Item().Text($"Date: {requisition.FinanceApprovalDate?.ToString("dd/MM/yyyy")}");
                        }
                        else
                        {
                            col.Item().PaddingTop(20).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(5).Text("Signature & Date");
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Final Approval:").Bold();
                        if (!string.IsNullOrEmpty(requisition.FinalApproverName))
                        {
                            col.Item().PaddingTop(5).Text($"Name: {requisition.FinalApproverName}");
                            col.Item().Text($"Date: {requisition.FinalApprovalDate?.ToString("dd/MM/yyyy")}");
                        }
                        else
                        {
                            col.Item().PaddingTop(20).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(5).Text("Signature & Date");
                        }
                    });
                });
            });
        }

        private void ComposePurchaseOrderContent(IContainer container, PurchaseOrder purchaseOrder, CurrencySettings currencySettings)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                column.Item().Text("PURCHASE ORDER").FontSize(24).Bold().FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(innerColumn =>
                    {
                        innerColumn.Item().Text($"PO Number: {purchaseOrder.PONumber}").FontSize(12).Bold();
                        innerColumn.Item().Text($"PO Date: {purchaseOrder.PODate:dd/MM/yyyy}").FontSize(12);
                        innerColumn.Item().Text($"Expected Delivery: {purchaseOrder.ExpectedDeliveryDate:dd/MM/yyyy}").FontSize(12);
                        innerColumn.Item().Text($"Status: {purchaseOrder.Status.Replace("_", " ")}").FontSize(12);
                    });

                    row.RelativeItem().Column(innerColumn =>
                    {
                        innerColumn.Item().Text("Supplier:").Bold().FontSize(12);
                        if (purchaseOrder.Supplier != null)
                        {
                            innerColumn.Item().Text(purchaseOrder.Supplier.SupplierName).FontSize(11);
                            if (!string.IsNullOrEmpty(purchaseOrder.Supplier.Address))
                                innerColumn.Item().Text(purchaseOrder.Supplier.Address).FontSize(10);
                            if (!string.IsNullOrEmpty(purchaseOrder.Supplier.ContactPerson))
                                innerColumn.Item().Text($"Contact: {purchaseOrder.Supplier.ContactPerson}").FontSize(10);
                            if (!string.IsNullOrEmpty(purchaseOrder.Supplier.Phone))
                                innerColumn.Item().Text($"Phone: {purchaseOrder.Supplier.Phone}").FontSize(10);
                            if (!string.IsNullOrEmpty(purchaseOrder.Supplier.Email))
                                innerColumn.Item().Text($"Email: {purchaseOrder.Supplier.Email}").FontSize(10);
                        }
                    });
                });

                column.Item().PaddingTop(10).Text($"Delivery Address: {purchaseOrder.DeliveryAddress}").FontSize(10);
                column.Item().Text($"Prepared By: {purchaseOrder.PreparedBy}").FontSize(10);

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#").Bold();
                        header.Cell().Element(CellStyle).Text("Item Description").Bold();
                        header.Cell().Element(CellStyle).Text("Code").Bold();
                        header.Cell().Element(CellStyle).Text("Qty").Bold();
                        header.Cell().Element(CellStyle).Text("Unit").Bold();
                        header.Cell().Element(CellStyle).Text("Unit Price").Bold();
                        header.Cell().Element(CellStyle).Text("Total").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    var index = 1;
                    foreach (var item in purchaseOrder.PurchaseOrderItems)
                    {
                        table.Cell().Element(RowCellStyle).Text(index++.ToString());
                        table.Cell().Element(RowCellStyle).Text(item.ItemDescription);
                        table.Cell().Element(RowCellStyle).Text(item.ItemCode ?? "-");
                        table.Cell().Element(RowCellStyle).AlignRight().Text(item.QuantityOrdered.ToString());
                        table.Cell().Element(RowCellStyle).Text(item.Unit);
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(item.UnitPrice, currencySettings));
                        table.Cell().Element(RowCellStyle).AlignRight().Text(FormatCurrency(item.QuantityOrdered * item.UnitPrice, currencySettings));

                        static IContainer RowCellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }
                });

                column.Item().PaddingTop(10).AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text("Total Amount:").Bold().FontSize(14);
                    row.ConstantItem(150).AlignRight().Text(FormatCurrency(purchaseOrder.TotalAmount, currencySettings)).Bold().FontSize(14).FontColor(Colors.Green.Medium);
                });

                if (!string.IsNullOrEmpty(purchaseOrder.TermsAndConditions))
                {
                    column.Item().PaddingTop(20).Column(termsColumn =>
                    {
                        termsColumn.Item().Text("Terms & Conditions:").Bold();
                        termsColumn.Item().Text(purchaseOrder.TermsAndConditions);
                    });
                }

                if (!string.IsNullOrEmpty(purchaseOrder.Notes))
                {
                    column.Item().PaddingTop(10).Column(notesColumn =>
                    {
                        notesColumn.Item().Text("Notes:").Bold();
                        notesColumn.Item().Text(purchaseOrder.Notes);
                    });
                }

                // Approval signatures section
                column.Item().PaddingTop(40).BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(20);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Prepared By:").Bold();
                        col.Item().PaddingTop(5).Text(purchaseOrder.PreparedBy);
                        col.Item().Text($"Date: {purchaseOrder.CreatedDate:dd/MM/yyyy}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Approved By:").Bold();
                        if (!string.IsNullOrEmpty(purchaseOrder.ApprovedBy))
                        {
                            col.Item().PaddingTop(5).Text(purchaseOrder.ApprovedBy);
                            col.Item().Text($"Date: {purchaseOrder.ApprovalDate?.ToString("dd/MM/yyyy")}");
                        }
                        else
                        {
                            col.Item().PaddingTop(20).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(5).Text("Signature & Date");
                        }
                    });
                });

                column.Item().PaddingTop(20).Text("This is a computer-generated document.").FontSize(8).Italic().FontColor(Colors.Grey.Medium);
            });
        }
    }

    public class CompanyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
    }
}

