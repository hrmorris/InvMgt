using InvoiceManagement.Data;
using InvoiceManagement.Models;
using InvoiceManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .Include(i => i.Supplier)
                .Include(i => i.PaymentAllocations)
                    .ThenInclude(pa => pa.Payment)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            invoice.CreatedDate = DateTime.Now;
            invoice.PaidAmount = 0;
            await UpdateInvoiceStatusAsync(invoice);

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            invoice.ModifiedDate = DateTime.Now;
            await UpdateInvoiceStatusAsync(invoice);

            // Get the existing invoice with its items
            var existingInvoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);

            if (existingInvoice != null)
            {
                // Only update items if they are explicitly provided (not null)
                // This prevents accidentally deleting items when updating only invoice properties
                if (invoice.InvoiceItems != null)
                {
                    // Remove old items first (use ToList to avoid collection modification issues)
                    var oldItems = await _context.InvoiceItems
                        .Where(i => i.InvoiceId == invoice.Id)
                        .ToListAsync();

                    _context.InvoiceItems.RemoveRange(oldItems);
                    await _context.SaveChangesAsync();

                    // Add new items if any
                    if (invoice.InvoiceItems.Any())
                    {
                        foreach (var item in invoice.InvoiceItems)
                        {
                            item.InvoiceId = invoice.Id;
                            item.Id = 0; // Ensure new items get new IDs
                        }
                        _context.InvoiceItems.AddRange(invoice.InvoiceItems);
                    }
                }

                // Update invoice properties
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.PaymentAllocations)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice != null)
            {
                // Remove payment allocations first
                if (invoice.PaymentAllocations != null && invoice.PaymentAllocations.Any())
                {
                    _context.PaymentAllocations.RemoveRange(invoice.PaymentAllocations);
                }

                // Remove invoice items
                if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
                {
                    _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);
                }

                // Remove direct payments (payments linked directly to invoice)
                if (invoice.Payments != null && invoice.Payments.Any())
                {
                    _context.Payments.RemoveRange(invoice.Payments);
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .Include(i => i.Supplier)
                .Where(i => i.DueDate < DateTime.Now && i.Status != "Paid")
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetOverAllocatedInvoicesAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .Include(i => i.Supplier)
                .Include(i => i.PaymentAllocations)
                .ToListAsync();

            // Filter on client side with tolerance for floating-point precision
            var overAllocated = invoices.Where(i => i.PaidAmount > i.TotalAmount + 0.01m).ToList();

            // Order on client side since SQLite doesn't support ordering by computed decimal expressions
            return overAllocated.OrderByDescending(i => i.PaidAmount - i.TotalAmount);
        }

        public async Task<IEnumerable<Invoice>> SearchInvoicesAsync(string searchTerm)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .Where(i => i.InvoiceNumber.Contains(searchTerm) ||
                           i.CustomerName.Contains(searchTerm) ||
                           (i.CustomerEmail != null && i.CustomerEmail.Contains(searchTerm)))
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task UpdateInvoiceStatusAsync(int invoiceId)
        {
            var invoice = await GetInvoiceByIdAsync(invoiceId);
            if (invoice != null)
            {
                await UpdateInvoiceStatusAsync(invoice);
                await _context.SaveChangesAsync();
            }
        }

        private Task UpdateInvoiceStatusAsync(Invoice invoice)
        {
            if (invoice.PaidAmount >= invoice.TotalAmount)
            {
                invoice.Status = "Paid";
            }
            else if (invoice.PaidAmount > 0)
            {
                invoice.Status = "Partial";
            }
            else if (invoice.DueDate < DateTime.Now)
            {
                invoice.Status = "Overdue";
            }
            else
            {
                invoice.Status = "Unpaid";
            }

            return Task.CompletedTask;
        }

        public async Task UpdateInvoicePaidAmountAndStatusAsync(int invoiceId, decimal paidAmount)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice != null)
            {
                invoice.PaidAmount = paidAmount;
                invoice.ModifiedDate = DateTime.Now;
                await UpdateInvoiceStatusAsync(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RecalculateInvoicePaidAmountAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.PaymentAllocations)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice != null)
            {
                // Calculate actual paid amount from allocations
                var actualPaidAmount = invoice.PaymentAllocations?.Sum(pa => pa.AllocatedAmount) ?? 0;
                invoice.PaidAmount = actualPaidAmount;
                invoice.ModifiedDate = DateTime.Now;
                await UpdateInvoiceStatusAsync(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RecalculateAllInvoicePaidAmountsAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.PaymentAllocations)
                .ToListAsync();

            foreach (var invoice in invoices)
            {
                var actualPaidAmount = invoice.PaymentAllocations?.Sum(pa => pa.AllocatedAmount) ?? 0;
                if (invoice.PaidAmount != actualPaidAmount)
                {
                    invoice.PaidAmount = actualPaidAmount;
                    invoice.ModifiedDate = DateTime.Now;
                    await UpdateInvoiceStatusAsync(invoice);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<SupplierOutstandingViewModel> GetSupplierOutstandingAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Supplier)
                .Include(i => i.InvoiceItems)
                .Where(i => i.Status != "Paid" && i.InvoiceType == "Supplier")
                .OrderBy(i => i.DueDate)
                .ToListAsync();

            var supplierGroups = invoices
                .GroupBy(i => new { i.SupplierId, SupplierName = i.Supplier?.SupplierName ?? i.CustomerName })
                .Select(g => new SupplierOutstandingSummary
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierName = g.Key.SupplierName,
                    SupplierCode = g.FirstOrDefault()?.Supplier?.SupplierCode,
                    ContactPerson = g.FirstOrDefault()?.Supplier?.ContactPerson,
                    Phone = g.FirstOrDefault()?.Supplier?.Phone ?? g.FirstOrDefault()?.Supplier?.Mobile,
                    Email = g.FirstOrDefault()?.Supplier?.Email,
                    InvoiceCount = g.Count(),
                    OverdueCount = g.Count(i => i.DueDate < DateTime.Now),
                    TotalAmount = g.Sum(i => i.TotalAmount),
                    PaidAmount = g.Sum(i => i.PaidAmount),
                    OutstandingAmount = g.Sum(i => i.BalanceAmount),
                    OverdueAmount = g.Where(i => i.DueDate < DateTime.Now).Sum(i => i.BalanceAmount),
                    OldestInvoiceDate = g.Min(i => i.InvoiceDate),
                    MaxDaysOverdue = g.Where(i => i.DueDate < DateTime.Now).Any()
                        ? g.Where(i => i.DueDate < DateTime.Now).Max(i => (DateTime.Now - i.DueDate).Days)
                        : 0,
                    Invoices = g.OrderBy(i => i.DueDate).ToList()
                })
                .OrderByDescending(s => s.OutstandingAmount)
                .ToList();

            return new SupplierOutstandingViewModel
            {
                Suppliers = supplierGroups,
                TotalOutstanding = supplierGroups.Sum(s => s.OutstandingAmount),
                TotalInvoices = supplierGroups.Sum(s => s.InvoiceCount),
                TotalSuppliers = supplierGroups.Count,
                TotalOverdue = supplierGroups.Sum(s => s.OverdueAmount),
                OverdueInvoiceCount = supplierGroups.Sum(s => s.OverdueCount)
            };
        }
    }
}

