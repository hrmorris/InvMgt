using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;
using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public class EntityLookupService : IEntityLookupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EntityLookupService> _logger;

        public EntityLookupService(ApplicationDbContext context, ILogger<EntityLookupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Supplier?> FindSupplierByAccountNumberAsync(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) return null;
            
            return await _context.Suppliers
                .FirstOrDefaultAsync(s => s.BankAccountNumber != null && 
                    s.BankAccountNumber.Replace(" ", "").Replace("-", "") == accountNumber.Replace(" ", "").Replace("-", ""));
        }

        public async Task<Supplier?> FindSupplierByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            
            // First try exact match
            var exactMatch = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierName.ToLower() == name.ToLower());
            
            if (exactMatch != null) return exactMatch;
            
            // Try partial match
            return await _context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierName.ToLower().Contains(name.ToLower()) ||
                    name.ToLower().Contains(s.SupplierName.ToLower()));
        }

        public async Task<Supplier?> FindSupplierByBankDetailsAsync(string? bankName, string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) return null;
            
            var normalizedAccount = accountNumber.Replace(" ", "").Replace("-", "");
            
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.BankAccountNumber != null && 
                    s.BankAccountNumber.Replace(" ", "").Replace("-", "") == normalizedAccount);
            
            if (supplier != null) return supplier;
            
            // If bank name provided, try to find by both
            if (!string.IsNullOrWhiteSpace(bankName))
            {
                supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.BankName != null && 
                        s.BankName.ToLower().Contains(bankName.ToLower()) &&
                        s.BankAccountNumber != null && 
                        s.BankAccountNumber.Replace(" ", "").Replace("-", "") == normalizedAccount);
            }
            
            return supplier;
        }

        public async Task<Customer?> FindCustomerByAccountNumberAsync(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) return null;
            
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.BankAccountNumber != null && 
                    c.BankAccountNumber.Replace(" ", "").Replace("-", "") == accountNumber.Replace(" ", "").Replace("-", ""));
        }

        public async Task<Customer?> FindCustomerByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            
            // First try exact match
            var exactMatch = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerName.ToLower() == name.ToLower());
            
            if (exactMatch != null) return exactMatch;
            
            // Try partial match
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerName.ToLower().Contains(name.ToLower()) ||
                    name.ToLower().Contains(c.CustomerName.ToLower()));
        }

        public async Task<Customer?> FindCustomerByBankDetailsAsync(string? bankName, string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) return null;
            
            var normalizedAccount = accountNumber.Replace(" ", "").Replace("-", "");
            
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.BankAccountNumber != null && 
                    c.BankAccountNumber.Replace(" ", "").Replace("-", "") == normalizedAccount);
            
            if (customer != null) return customer;
            
            // If bank name provided, try to find by both
            if (!string.IsNullOrWhiteSpace(bankName))
            {
                customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.BankName != null && 
                        c.BankName.ToLower().Contains(bankName.ToLower()) &&
                        c.BankAccountNumber != null && 
                        c.BankAccountNumber.Replace(" ", "").Replace("-", "") == normalizedAccount);
            }
            
            return customer;
        }

        public async Task<Supplier> CreateSupplierFromPaymentDataAsync(string? name, string? bankName, string? accountNumber)
        {
            var supplierName = string.IsNullOrWhiteSpace(name) ? $"Supplier (Acct: {accountNumber ?? "Unknown"})" : name;
            
            var supplier = new Supplier
            {
                SupplierName = supplierName,
                SupplierCode = GenerateSupplierCode(),
                BankName = bankName,
                BankAccountNumber = accountNumber,
                Status = "Active",
                Notes = "Auto-created from AI Import on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                CreatedDate = DateTime.Now
            };
            
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created new supplier from payment data: {SupplierName} (ID: {SupplierId})", 
                supplier.SupplierName, supplier.Id);
            
            return supplier;
        }

        public async Task<Customer> CreateCustomerFromPaymentDataAsync(string? name, string? bankName, string? accountNumber)
        {
            var customerName = string.IsNullOrWhiteSpace(name) ? $"Customer (Acct: {accountNumber ?? "Unknown"})" : name;
            
            var customer = new Customer
            {
                CustomerName = customerName,
                CustomerCode = GenerateCustomerCode(),
                BankName = bankName,
                BankAccountNumber = accountNumber,
                Status = "Active",
                Notes = "Auto-created from AI Import on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                CreatedDate = DateTime.Now
            };
            
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created new customer from payment data: {CustomerName} (ID: {CustomerId})", 
                customer.CustomerName, customer.Id);
            
            return customer;
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .ToListAsync();
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers
                .Where(c => c.Status == "Active")
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        /// <summary>
        /// Resolves the supplier or customer for a payment based on extracted data.
        /// For outgoing payments (to suppliers): payeeName is the supplier
        /// For incoming payments (from customers): payerName is the customer
        /// </summary>
        public async Task<(Supplier? supplier, Customer? customer, bool isNew)> ResolvePaymentEntityAsync(
            string? payerName, string? payeeName, string? bankName, string? accountNumber, string paymentType)
        {
            bool isNew = false;
            Supplier? supplier = null;
            Customer? customer = null;

            // Determine if this is a payment to supplier (outgoing) or from customer (incoming)
            var isOutgoingPayment = paymentType?.ToLower() == "outgoing" || 
                                    paymentType?.ToLower() == "supplier" ||
                                    !string.IsNullOrWhiteSpace(payeeName);

            if (isOutgoingPayment)
            {
                // This is a payment to a supplier (payee is the supplier)
                // First try by account number
                supplier = await FindSupplierByBankDetailsAsync(bankName, accountNumber);
                
                if (supplier == null && !string.IsNullOrWhiteSpace(payeeName))
                {
                    supplier = await FindSupplierByNameAsync(payeeName);
                }
                
                // Return without auto-creating - let the controller handle that decision
            }
            else
            {
                // This is a payment from a customer (payer is the customer)
                customer = await FindCustomerByBankDetailsAsync(bankName, accountNumber);
                
                if (customer == null && !string.IsNullOrWhiteSpace(payerName))
                {
                    customer = await FindCustomerByNameAsync(payerName);
                }
            }

            return (supplier, customer, isNew);
        }

        private string GenerateSupplierCode()
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            return $"SUP{timestamp}";
        }

        private string GenerateCustomerCode()
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            return $"CUS{timestamp}";
        }
    }
}
