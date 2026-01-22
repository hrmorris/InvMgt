using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using InvoiceManagement.Models;
using OfficeOpenXml;

namespace InvoiceManagement.Services
{
    public class ImportService : IImportService
    {
        public Task<List<Invoice>> ImportInvoicesFromCsvAsync(Stream fileStream)
        {
            var invoices = new List<Invoice>();

            using (var reader = new StreamReader(fileStream))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Context.RegisterClassMap<InvoiceCsvMap>();
                var records = csv.GetRecords<InvoiceCsvRecord>().ToList();

                foreach (var record in records)
                {
                    invoices.Add(new Invoice
                    {
                        InvoiceNumber = record.InvoiceNumber,
                        InvoiceDate = record.InvoiceDate,
                        DueDate = record.DueDate,
                        CustomerName = record.CustomerName,
                        CustomerAddress = record.CustomerAddress,
                        CustomerEmail = record.CustomerEmail,
                        CustomerPhone = record.CustomerPhone,
                        TotalAmount = record.TotalAmount,
                        Notes = record.Notes
                    });
                }
            }

            return Task.FromResult(invoices);
        }

        public Task<List<Payment>> ImportPaymentsFromCsvAsync(Stream fileStream)
        {
            var payments = new List<Payment>();

            using (var reader = new StreamReader(fileStream))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Context.RegisterClassMap<PaymentCsvMap>();
                var records = csv.GetRecords<PaymentCsvRecord>().ToList();

                foreach (var record in records)
                {
                    payments.Add(new Payment
                    {
                        PaymentNumber = record.PaymentNumber,
                        InvoiceId = record.InvoiceId,
                        PaymentDate = record.PaymentDate,
                        Amount = record.Amount,
                        PaymentMethod = record.PaymentMethod,
                        ReferenceNumber = record.ReferenceNumber,
                        Notes = record.Notes
                    });
                }
            }

            return Task.FromResult(payments);
        }

        public Task<List<Invoice>> ImportInvoicesFromExcelAsync(Stream fileStream)
        {
            var invoices = new List<Invoice>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++) // Skip header row
                {
                    invoices.Add(new Invoice
                    {
                        InvoiceNumber = worksheet.Cells[row, 1].Value?.ToString() ?? string.Empty,
                        InvoiceDate = DateTime.Parse(worksheet.Cells[row, 2].Value?.ToString() ?? DateTime.Now.ToString()),
                        DueDate = DateTime.Parse(worksheet.Cells[row, 3].Value?.ToString() ?? DateTime.Now.ToString()),
                        CustomerName = worksheet.Cells[row, 4].Value?.ToString() ?? string.Empty,
                        CustomerAddress = worksheet.Cells[row, 5].Value?.ToString(),
                        CustomerEmail = worksheet.Cells[row, 6].Value?.ToString(),
                        CustomerPhone = worksheet.Cells[row, 7].Value?.ToString(),
                        TotalAmount = decimal.Parse(worksheet.Cells[row, 8].Value?.ToString() ?? "0"),
                        Notes = worksheet.Cells[row, 9].Value?.ToString()
                    });
                }
            }

            return Task.FromResult(invoices);
        }

        public Task<List<Payment>> ImportPaymentsFromExcelAsync(Stream fileStream)
        {
            var payments = new List<Payment>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++) // Skip header row
                {
                    payments.Add(new Payment
                    {
                        PaymentNumber = worksheet.Cells[row, 1].Value?.ToString() ?? string.Empty,
                        InvoiceId = int.Parse(worksheet.Cells[row, 2].Value?.ToString() ?? "0"),
                        PaymentDate = DateTime.Parse(worksheet.Cells[row, 3].Value?.ToString() ?? DateTime.Now.ToString()),
                        Amount = decimal.Parse(worksheet.Cells[row, 4].Value?.ToString() ?? "0"),
                        PaymentMethod = worksheet.Cells[row, 5].Value?.ToString() ?? string.Empty,
                        ReferenceNumber = worksheet.Cells[row, 6].Value?.ToString(),
                        Notes = worksheet.Cells[row, 7].Value?.ToString()
                    });
                }
            }

            return Task.FromResult(payments);
        }
    }

    // CSV Mapping Classes
    public class InvoiceCsvRecord
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
    }

    public class PaymentCsvRecord
    {
        public string PaymentNumber { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class InvoiceCsvMap : ClassMap<InvoiceCsvRecord>
    {
        public InvoiceCsvMap()
        {
            Map(m => m.InvoiceNumber).Name("InvoiceNumber");
            Map(m => m.InvoiceDate).Name("InvoiceDate");
            Map(m => m.DueDate).Name("DueDate");
            Map(m => m.CustomerName).Name("CustomerName");
            Map(m => m.CustomerAddress).Name("CustomerAddress").Optional();
            Map(m => m.CustomerEmail).Name("CustomerEmail").Optional();
            Map(m => m.CustomerPhone).Name("CustomerPhone").Optional();
            Map(m => m.TotalAmount).Name("TotalAmount");
            Map(m => m.Notes).Name("Notes").Optional();
        }
    }

    public sealed class PaymentCsvMap : ClassMap<PaymentCsvRecord>
    {
        public PaymentCsvMap()
        {
            Map(m => m.PaymentNumber).Name("PaymentNumber");
            Map(m => m.InvoiceId).Name("InvoiceId");
            Map(m => m.PaymentDate).Name("PaymentDate");
            Map(m => m.Amount).Name("Amount");
            Map(m => m.PaymentMethod).Name("PaymentMethod");
            Map(m => m.ReferenceNumber).Name("ReferenceNumber").Optional();
            Map(m => m.Notes).Name("Notes").Optional();
        }
    }
}

