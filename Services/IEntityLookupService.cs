using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;
using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IEntityLookupService
    {
        Task<Supplier?> FindSupplierByAccountNumberAsync(string accountNumber);
        Task<Supplier?> FindSupplierByNameAsync(string name);
        Task<Supplier?> FindSupplierByBankDetailsAsync(string? bankName, string? accountNumber);
        Task<Customer?> FindCustomerByAccountNumberAsync(string accountNumber);
        Task<Customer?> FindCustomerByNameAsync(string name);
        Task<Customer?> FindCustomerByBankDetailsAsync(string? bankName, string? accountNumber);
        Task<Supplier> CreateSupplierFromPaymentDataAsync(string? name, string? bankName, string? accountNumber);
        Task<Customer> CreateCustomerFromPaymentDataAsync(string? name, string? bankName, string? accountNumber);
        Task<List<Supplier>> GetAllSuppliersAsync();
        Task<List<Customer>> GetAllCustomersAsync();
        Task<(Supplier? supplier, Customer? customer, bool isNew)> ResolvePaymentEntityAsync(
            string? payerName, string? payeeName, string? bankName, string? accountNumber, string paymentType);
    }
}
