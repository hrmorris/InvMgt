using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    public interface IBatchPaymentService
    {
        Task<IEnumerable<BatchPayment>> GetAllBatchPaymentsAsync();
        Task<BatchPayment?> GetBatchPaymentByIdAsync(int id);
        Task<BatchPayment?> GetBatchPaymentWithItemsAsync(int id);
        Task<BatchPayment> CreateBatchPaymentAsync(BatchPayment batch);
        Task UpdateBatchPaymentAsync(BatchPayment batch);
        Task DeleteBatchPaymentAsync(int id);
        Task<string> GenerateBatchReferenceAsync();

        // Batch item operations
        Task<BatchPaymentItem> AddInvoiceToBatchAsync(int batchId, int invoiceId, decimal? amountToPay = null);
        Task RemoveInvoiceFromBatchAsync(int batchId, int invoiceId);
        Task UpdateBatchItemAmountAsync(int itemId, decimal amountToPay);

        // Batch processing
        Task<bool> MarkBatchAsReadyAsync(int batchId);
        Task<bool> ProcessBatchAsync(int batchId, string paymentMethod, string? referenceNumber = null);
        Task<bool> CancelBatchAsync(int batchId);

        // Query helpers
        Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync(int? supplierId = null);
        Task<IEnumerable<Invoice>> GetInvoicesNotInBatchAsync(int batchId, int? supplierId = null);
    }

    public class BatchPaymentService : IBatchPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IInvoiceService _invoiceService;

        public BatchPaymentService(ApplicationDbContext context, IPaymentService paymentService, IInvoiceService invoiceService)
        {
            _context = context;
            _paymentService = paymentService;
            _invoiceService = invoiceService;
        }

        public async Task<IEnumerable<BatchPayment>> GetAllBatchPaymentsAsync()
        {
            return await _context.BatchPayments
                .Include(b => b.BatchItems)
                    .ThenInclude(bi => bi.Invoice)
                        .ThenInclude(i => i!.Supplier)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
        }

        public async Task<BatchPayment?> GetBatchPaymentByIdAsync(int id)
        {
            return await _context.BatchPayments
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<BatchPayment?> GetBatchPaymentWithItemsAsync(int id)
        {
            return await _context.BatchPayments
                .Include(b => b.BatchItems)
                    .ThenInclude(bi => bi.Invoice)
                        .ThenInclude(i => i!.Supplier)
                .Include(b => b.BatchItems)
                    .ThenInclude(bi => bi.Invoice)
                        .ThenInclude(i => i!.Customer)
                .Include(b => b.BatchItems)
                    .ThenInclude(bi => bi.Payment)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<BatchPayment> CreateBatchPaymentAsync(BatchPayment batch)
        {
            if (string.IsNullOrEmpty(batch.BatchReference))
            {
                batch.BatchReference = await GenerateBatchReferenceAsync();
            }

            batch.CreatedDate = DateTime.Now;
            batch.Status = "Draft";

            _context.BatchPayments.Add(batch);
            await _context.SaveChangesAsync();

            return batch;
        }

        public async Task UpdateBatchPaymentAsync(BatchPayment batch)
        {
            var existing = await _context.BatchPayments.FindAsync(batch.Id);
            if (existing == null) return;

            // Only allow updates if batch is still in Draft status
            if (existing.Status != "Draft" && existing.Status != "Ready")
            {
                throw new InvalidOperationException("Cannot modify a batch that has been processed or cancelled.");
            }

            existing.BatchName = batch.BatchName;
            existing.ScheduledPaymentDate = batch.ScheduledPaymentDate;
            existing.PaymentMethod = batch.PaymentMethod;
            existing.BankAccount = batch.BankAccount;
            existing.Notes = batch.Notes;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteBatchPaymentAsync(int id)
        {
            var batch = await _context.BatchPayments
                .Include(b => b.BatchItems)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null) return;

            // Only allow deletion if batch is in Draft status
            if (batch.Status != "Draft")
            {
                throw new InvalidOperationException("Cannot delete a batch that is not in Draft status.");
            }

            _context.BatchPayments.Remove(batch);
            await _context.SaveChangesAsync();
        }

        public async Task<string> GenerateBatchReferenceAsync()
        {
            var today = DateTime.Now;
            var prefix = $"BATCH-{today:yyyyMMdd}-";

            var lastBatch = await _context.BatchPayments
                .Where(b => b.BatchReference.StartsWith(prefix))
                .OrderByDescending(b => b.BatchReference)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastBatch != null)
            {
                var lastNumberStr = lastBatch.BatchReference.Replace(prefix, "");
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D3}";
        }

        public async Task<BatchPaymentItem> AddInvoiceToBatchAsync(int batchId, int invoiceId, decimal? amountToPay = null)
        {
            var batch = await _context.BatchPayments.FindAsync(batchId);
            if (batch == null)
                throw new ArgumentException("Batch not found");

            if (batch.Status != "Draft")
                throw new InvalidOperationException("Cannot add invoices to a batch that is not in Draft status.");

            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                throw new ArgumentException("Invoice not found");

            // Check if invoice is already in this batch
            var existingItem = await _context.BatchPaymentItems
                .FirstOrDefaultAsync(bi => bi.BatchPaymentId == batchId && bi.InvoiceId == invoiceId);

            if (existingItem != null)
                throw new InvalidOperationException("Invoice is already in this batch.");

            // Default to the invoice balance if no amount specified
            var amount = amountToPay ?? invoice.BalanceAmount;

            // Validate amount doesn't exceed balance
            if (amount > invoice.BalanceAmount)
                throw new ArgumentException($"Amount to pay ({amount:C}) exceeds invoice balance ({invoice.BalanceAmount:C})");

            var item = new BatchPaymentItem
            {
                BatchPaymentId = batchId,
                InvoiceId = invoiceId,
                AmountToPay = amount,
                AddedDate = DateTime.Now
            };

            _context.BatchPaymentItems.Add(item);
            await _context.SaveChangesAsync();

            return item;
        }

        public async Task RemoveInvoiceFromBatchAsync(int batchId, int invoiceId)
        {
            var batch = await _context.BatchPayments.FindAsync(batchId);
            if (batch == null) return;

            if (batch.Status != "Draft")
                throw new InvalidOperationException("Cannot remove invoices from a batch that is not in Draft status.");

            var item = await _context.BatchPaymentItems
                .FirstOrDefaultAsync(bi => bi.BatchPaymentId == batchId && bi.InvoiceId == invoiceId);

            if (item != null)
            {
                _context.BatchPaymentItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateBatchItemAmountAsync(int itemId, decimal amountToPay)
        {
            var item = await _context.BatchPaymentItems
                .Include(bi => bi.Invoice)
                .Include(bi => bi.BatchPayment)
                .FirstOrDefaultAsync(bi => bi.Id == itemId);

            if (item == null) return;

            if (item.BatchPayment?.Status != "Draft")
                throw new InvalidOperationException("Cannot modify items in a batch that is not in Draft status.");

            if (item.Invoice != null && amountToPay > item.Invoice.BalanceAmount)
                throw new ArgumentException($"Amount to pay ({amountToPay:C}) exceeds invoice balance ({item.Invoice.BalanceAmount:C})");

            item.AmountToPay = amountToPay;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> MarkBatchAsReadyAsync(int batchId)
        {
            var batch = await _context.BatchPayments
                .Include(b => b.BatchItems)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null) return false;

            if (batch.Status != "Draft")
                throw new InvalidOperationException("Only Draft batches can be marked as Ready.");

            if (!batch.BatchItems.Any())
                throw new InvalidOperationException("Cannot mark an empty batch as Ready.");

            batch.Status = "Ready";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ProcessBatchAsync(int batchId, string paymentMethod, string? referenceNumber = null)
        {
            var batch = await _context.BatchPayments
                .Include(b => b.BatchItems)
                    .ThenInclude(bi => bi.Invoice)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null) return false;

            if (batch.Status != "Ready" && batch.Status != "Draft")
                throw new InvalidOperationException("Only Ready or Draft batches can be processed.");

            if (!batch.BatchItems.Any())
                throw new InvalidOperationException("Cannot process an empty batch.");

            batch.Status = "Processing";
            await _context.SaveChangesAsync();

            try
            {
                foreach (var item in batch.BatchItems.Where(bi => !bi.IsProcessed))
                {
                    // Generate payment number
                    var paymentNumber = await GeneratePaymentNumberAsync();

                    // Create a payment for each invoice in the batch
                    var payment = new Payment
                    {
                        PaymentNumber = paymentNumber,
                        PaymentDate = DateTime.Now,
                        Amount = item.AmountToPay,
                        PaymentMethod = paymentMethod,
                        ReferenceNumber = referenceNumber,
                        Notes = $"Batch Payment: {batch.BatchReference}",
                        SupplierId = item.Invoice?.SupplierId,
                        CustomerId = item.Invoice?.CustomerId,
                        InvoiceId = item.InvoiceId, // Direct link for legacy support
                        Status = "Fully Allocated"
                    };

                    await _paymentService.CreatePaymentAsync(payment);

                    // Create allocation record
                    await _paymentService.AllocatePaymentToInvoiceAsync(payment.Id, item.InvoiceId, item.AmountToPay, $"Batch: {batch.BatchReference}");

                    item.IsProcessed = true;
                    item.PaymentId = payment.Id;
                }

                batch.Status = "Completed";
                batch.ProcessedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                batch.Status = "Ready"; // Revert status on failure
                await _context.SaveChangesAsync();
                throw;
            }
        }

        public async Task<bool> CancelBatchAsync(int batchId)
        {
            var batch = await _context.BatchPayments.FindAsync(batchId);
            if (batch == null) return false;

            if (batch.Status == "Completed")
                throw new InvalidOperationException("Cannot cancel a completed batch.");

            batch.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync(int? supplierId = null)
        {
            var query = _context.Invoices
                .Include(i => i.Supplier)
                .Include(i => i.Customer)
                .Where(i => i.Status != "Paid" && i.BalanceAmount > 0);

            if (supplierId.HasValue)
            {
                query = query.Where(i => i.SupplierId == supplierId.Value);
            }

            return await query.OrderBy(i => i.DueDate).ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesNotInBatchAsync(int batchId, int? supplierId = null)
        {
            // Get invoice IDs already in the batch
            var invoiceIdsInBatch = await _context.BatchPaymentItems
                .Where(bi => bi.BatchPaymentId == batchId)
                .Select(bi => bi.InvoiceId)
                .ToListAsync();

            var query = _context.Invoices
                .Include(i => i.Supplier)
                .Include(i => i.Customer)
                .Where(i => i.Status != "Paid" && i.BalanceAmount > 0 && !invoiceIdsInBatch.Contains(i.Id));

            if (supplierId.HasValue)
            {
                query = query.Where(i => i.SupplierId == supplierId.Value);
            }

            return await query.OrderBy(i => i.DueDate).ToListAsync();
        }

        private async Task<string> GeneratePaymentNumberAsync()
        {
            var today = DateTime.Now;
            var prefix = $"PAY-{today:yyyyMMdd}-";

            var lastPayment = await _context.Payments
                .Where(p => p.PaymentNumber.StartsWith(prefix))
                .OrderByDescending(p => p.PaymentNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastPayment != null)
            {
                var lastNumberStr = lastPayment.PaymentNumber.Replace(prefix, "");
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }
    }
}
