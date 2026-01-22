using InvoiceManagement.Models;
using InvoiceManagement.Models.ViewModels;

namespace InvoiceManagement.Services
{
    public interface IInvoiceService
    {
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(int id);
        Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
        Task<IEnumerable<Invoice>> GetOverAllocatedInvoicesAsync();
        Task<IEnumerable<Invoice>> SearchInvoicesAsync(string searchTerm);
        Task UpdateInvoiceStatusAsync(int invoiceId);
        Task UpdateInvoicePaidAmountAndStatusAsync(int invoiceId, decimal paidAmount);
        Task RecalculateInvoicePaidAmountAsync(int invoiceId);
        Task RecalculateAllInvoicePaidAmountsAsync();
        Task<SupplierOutstandingViewModel> GetSupplierOutstandingAsync();
    }
}

