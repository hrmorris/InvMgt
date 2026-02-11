using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;

        public PaymentService(ApplicationDbContext context, IInvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
        {
            return await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Supplier)
                .Include(p => p.PaymentAllocations)
                    .ThenInclude(pa => pa.Invoice)
                        .ThenInclude(i => i.Supplier)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Supplier)
                .Include(p => p.PaymentAllocations)
                    .ThenInclude(pa => pa.Invoice)
                        .ThenInclude(i => i.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment?> GetPaymentWithAllocationsAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Supplier)
                .Include(p => p.Supplier)
                .Include(p => p.PaymentAllocations)
                    .ThenInclude(pa => pa.Invoice)
                        .ThenInclude(i => i.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByInvoiceIdAsync(int invoiceId)
        {
            return await _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetUnallocatedPaymentsAsync()
        {
            return await _context.Payments
                .Include(p => p.PaymentAllocations)
                .Where(p => p.Status == "Unallocated" || !p.PaymentAllocations.Any())
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPartiallyAllocatedPaymentsAsync()
        {
            return await _context.Payments
                .Include(p => p.PaymentAllocations)
                .Where(p => p.Status == "Partially Allocated")
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            payment.CreatedDate = DateTime.Now;
            payment.Status = payment.InvoiceId.HasValue ? "Fully Allocated" : "Unallocated";

            // Add payment
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Update invoice paid amount if directly linked (legacy support)
            if (payment.InvoiceId.HasValue)
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(payment.InvoiceId.Value);
                if (invoice != null)
                {
                    await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(payment.InvoiceId.Value, invoice.PaidAmount + payment.Amount);
                }
            }

            return payment;
        }

        public async Task UpdatePaymentAsync(Payment payment)
        {
            var existingPayment = await GetPaymentByIdAsync(payment.Id);
            if (existingPayment != null)
            {
                // Handle invoice change for legacy direct link
                if (existingPayment.InvoiceId != payment.InvoiceId)
                {
                    // Remove from old invoice
                    if (existingPayment.InvoiceId.HasValue)
                    {
                        var oldInvoice = await _invoiceService.GetInvoiceByIdAsync(existingPayment.InvoiceId.Value);
                        if (oldInvoice != null)
                        {
                            await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(existingPayment.InvoiceId.Value, oldInvoice.PaidAmount - existingPayment.Amount);
                        }
                    }

                    // Add to new invoice
                    if (payment.InvoiceId.HasValue)
                    {
                        var newInvoice = await _invoiceService.GetInvoiceByIdAsync(payment.InvoiceId.Value);
                        if (newInvoice != null)
                        {
                            await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(payment.InvoiceId.Value, newInvoice.PaidAmount + payment.Amount);
                        }
                    }
                }
                else if (payment.InvoiceId.HasValue && existingPayment.Amount != payment.Amount)
                {
                    // Amount changed, adjust invoice
                    var invoice = await _invoiceService.GetInvoiceByIdAsync(payment.InvoiceId.Value);
                    if (invoice != null)
                    {
                        await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(payment.InvoiceId.Value, invoice.PaidAmount - existingPayment.Amount + payment.Amount);
                    }
                }

                payment.ModifiedDate = DateTime.Now;
                _context.Entry(existingPayment).CurrentValues.SetValues(payment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeletePaymentAsync(int id)
        {
            var payment = await GetPaymentWithAllocationsAsync(id);
            if (payment != null)
            {
                // Remove all allocations and update invoices
                foreach (var allocation in payment.PaymentAllocations.ToList())
                {
                    var invoice = await _invoiceService.GetInvoiceByIdAsync(allocation.InvoiceId);
                    if (invoice != null)
                    {
                        await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(allocation.InvoiceId, invoice.PaidAmount - allocation.AllocatedAmount);
                    }
                }

                // Update invoice paid amount for legacy direct link
                if (payment.InvoiceId.HasValue)
                {
                    var invoice = await _invoiceService.GetInvoiceByIdAsync(payment.InvoiceId.Value);
                    if (invoice != null)
                    {
                        await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(payment.InvoiceId.Value, invoice.PaidAmount - payment.Amount);
                    }
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }
        }

        // Payment Allocation Methods
        public async Task<PaymentAllocation> AllocatePaymentToInvoiceAsync(int paymentId, int invoiceId, decimal amount, string? notes = null)
        {
            var payment = await GetPaymentWithAllocationsAsync(paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            // Calculate unallocated amount
            var unallocatedAmount = payment.Amount - payment.PaymentAllocations.Sum(pa => pa.AllocatedAmount);

            if (unallocatedAmount <= 0)
                throw new InvalidOperationException($"This payment is fully allocated. To allocate to another invoice, please remove existing allocations first to free up funds.");

            if (amount > unallocatedAmount)
                throw new InvalidOperationException($"Allocation amount ({amount:N2}) exceeds available unallocated amount ({unallocatedAmount:N2}). Please reduce the allocation amount or remove existing allocations to free up more funds.");

            if (amount <= 0)
                throw new InvalidOperationException("Allocation amount must be greater than zero");

            // Round values to 2 decimal places to avoid floating-point precision issues
            var roundedAmount = Math.Round(amount, 2);
            var roundedBalance = Math.Round(invoice.BalanceAmount, 2);

            // Check if allocation would exceed the invoice's balance (prevent over-allocation to invoice)
            // Use small tolerance for floating-point comparison
            if (roundedAmount > roundedBalance + 0.01m && roundedBalance > 0)
                throw new InvalidOperationException($"Allocation amount ({amount:N2}) exceeds invoice balance ({invoice.BalanceAmount:N2}). The invoice only needs {invoice.BalanceAmount:N2} to be fully paid.");

            // Warn if trying to allocate to an already fully paid invoice
            if (roundedBalance <= 0)
                throw new InvalidOperationException($"Invoice {invoice.InvoiceNumber} is already fully paid (Balance: {invoice.BalanceAmount:N2}). Allocating more would over-pay this invoice.");

            // Check if allocation already exists for this invoice
            var existingAllocation = payment.PaymentAllocations.FirstOrDefault(pa => pa.InvoiceId == invoiceId);
            if (existingAllocation != null)
                throw new InvalidOperationException("Payment is already allocated to this invoice. Use update to change the amount.");

            // Create allocation
            var allocation = new PaymentAllocation
            {
                PaymentId = paymentId,
                InvoiceId = invoiceId,
                AllocatedAmount = amount,
                AllocationDate = DateTime.Now,
                Notes = notes
            };

            _context.PaymentAllocations.Add(allocation);
            await _context.SaveChangesAsync();

            // Update invoice paid amount and status
            await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(invoiceId, invoice.PaidAmount + amount);

            // Update payment status
            await UpdatePaymentStatusAsync(paymentId);

            return allocation;
        }

        public async Task<IEnumerable<PaymentAllocation>> GetPaymentAllocationsAsync(int paymentId)
        {
            return await _context.PaymentAllocations
                .Where(pa => pa.PaymentId == paymentId)
                .Include(pa => pa.Invoice)
                .OrderBy(pa => pa.AllocationDate)
                .ToListAsync();
        }

        public async Task<PaymentAllocation?> GetAllocationByIdAsync(int allocationId)
        {
            return await _context.PaymentAllocations
                .Include(pa => pa.Payment)
                .Include(pa => pa.Invoice)
                .FirstOrDefaultAsync(pa => pa.Id == allocationId);
        }

        public async Task UpdateAllocationAsync(PaymentAllocation allocation)
        {
            var existingAllocation = await _context.PaymentAllocations
                .Include(pa => pa.Payment)
                .Include(pa => pa.Invoice)
                .FirstOrDefaultAsync(pa => pa.Id == allocation.Id);

            if (existingAllocation == null)
                throw new InvalidOperationException("Allocation not found");

            var payment = await GetPaymentWithAllocationsAsync(existingAllocation.PaymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            // Calculate available amount (excluding current allocation)
            var otherAllocations = payment.PaymentAllocations.Where(pa => pa.Id != allocation.Id).Sum(pa => pa.AllocatedAmount);
            var availableAmount = payment.Amount - otherAllocations;

            if (allocation.AllocatedAmount > availableAmount)
                throw new InvalidOperationException($"New allocation amount ({allocation.AllocatedAmount:C}) exceeds available payment amount ({availableAmount:C})");

            // Update allocation
            var oldAmount = existingAllocation.AllocatedAmount;
            existingAllocation.AllocatedAmount = allocation.AllocatedAmount;
            existingAllocation.Notes = allocation.Notes;
            await _context.SaveChangesAsync();

            // Update invoice paid amount and status
            var invoice = await _invoiceService.GetInvoiceByIdAsync(existingAllocation.InvoiceId);
            if (invoice != null)
            {
                await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(existingAllocation.InvoiceId, invoice.PaidAmount - oldAmount + allocation.AllocatedAmount);
            }

            // Update payment status
            await UpdatePaymentStatusAsync(existingAllocation.PaymentId);
        }

        public async Task DeleteAllocationAsync(int allocationId)
        {
            var allocation = await _context.PaymentAllocations
                .Include(pa => pa.Invoice)
                .FirstOrDefaultAsync(pa => pa.Id == allocationId);

            if (allocation != null)
            {
                var invoiceId = allocation.InvoiceId;
                var allocatedAmount = allocation.AllocatedAmount;
                var paymentId = allocation.PaymentId;

                _context.PaymentAllocations.Remove(allocation);
                await _context.SaveChangesAsync();

                // Update invoice paid amount and status
                var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
                if (invoice != null)
                {
                    await _invoiceService.UpdateInvoicePaidAmountAndStatusAsync(invoiceId, invoice.PaidAmount - allocatedAmount);
                }

                // Update payment status
                await UpdatePaymentStatusAsync(paymentId);
            }
        }

        public async Task<decimal> GetUnallocatedAmountAsync(int paymentId)
        {
            var payment = await GetPaymentWithAllocationsAsync(paymentId);
            if (payment == null)
                return 0;

            return payment.Amount - payment.PaymentAllocations.Sum(pa => pa.AllocatedAmount);
        }

        public async Task UpdatePaymentStatusAsync(int paymentId)
        {
            var payment = await GetPaymentWithAllocationsAsync(paymentId);
            if (payment == null)
                return;

            var allocatedAmount = payment.PaymentAllocations.Sum(pa => pa.AllocatedAmount);
            var unallocatedAmount = payment.Amount - allocatedAmount;

            if (allocatedAmount == 0)
            {
                payment.Status = "Unallocated";
            }
            else if (unallocatedAmount > 0.01m) // Allow for rounding
            {
                payment.Status = "Partially Allocated";
            }
            else
            {
                payment.Status = "Fully Allocated";
            }

            await _context.SaveChangesAsync();
        }
    }
}

