using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IImportService
    {
        Task<List<Invoice>> ImportInvoicesFromCsvAsync(Stream fileStream);
        Task<List<Payment>> ImportPaymentsFromCsvAsync(Stream fileStream);
        Task<List<Invoice>> ImportInvoicesFromExcelAsync(Stream fileStream);
        Task<List<Payment>> ImportPaymentsFromExcelAsync(Stream fileStream);
    }
}

