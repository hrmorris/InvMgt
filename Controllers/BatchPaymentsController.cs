using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InvoiceManagement.Models;
using InvoiceManagement.Services;
using InvoiceManagement.Authorization;

namespace InvoiceManagement.Controllers
{
    [Authorize]
    public class BatchPaymentsController : Controller
    {
        private readonly IBatchPaymentService _batchPaymentService;
        private readonly IInvoiceService _invoiceService;

        public BatchPaymentsController(IBatchPaymentService batchPaymentService, IInvoiceService invoiceService)
        {
            _batchPaymentService = batchPaymentService;
            _invoiceService = invoiceService;
        }

        // GET: BatchPayments
        public async Task<IActionResult> Index()
        {
            var batches = await _batchPaymentService.GetAllBatchPaymentsAsync();
            return View(batches);
        }

        // GET: BatchPayments/Create
        public async Task<IActionResult> Create()
        {
            var batch = new BatchPayment
            {
                BatchReference = await _batchPaymentService.GenerateBatchReferenceAsync(),
                ScheduledPaymentDate = DateTime.Now.AddDays(7)
            };

            ViewBag.PaymentMethods = new SelectList(new[] { "Bank Transfer", "Cheque", "Cash", "EFT", "Direct Debit" });
            return View(batch);
        }

        // POST: BatchPayments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BatchPayment batch)
        {
            if (ModelState.IsValid)
            {
                batch.CreatedBy = User.Identity?.Name ?? "System";
                await _batchPaymentService.CreateBatchPaymentAsync(batch);
                TempData["SuccessMessage"] = $"Batch {batch.BatchReference} created successfully!";
                return RedirectToAction(nameof(Details), new { id = batch.Id });
            }

            ViewBag.PaymentMethods = new SelectList(new[] { "Bank Transfer", "Cheque", "Cash", "EFT", "Direct Debit" });
            return View(batch);
        }

        // GET: BatchPayments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var batch = await _batchPaymentService.GetBatchPaymentWithItemsAsync(id);
            if (batch == null)
            {
                return NotFound();
            }

            // Get available invoices not in this batch
            var availableInvoices = await _batchPaymentService.GetInvoicesNotInBatchAsync(id);
            ViewBag.AvailableInvoices = availableInvoices;
            ViewBag.Suppliers = availableInvoices
                .Where(i => i.Supplier != null)
                .Select(i => i.Supplier)
                .DistinctBy(s => s!.Id)
                .OrderBy(s => s!.SupplierName)
                .ToList();

            return View(batch);
        }

        // GET: BatchPayments/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var batch = await _batchPaymentService.GetBatchPaymentByIdAsync(id);
            if (batch == null)
            {
                return NotFound();
            }

            if (batch.Status != "Draft" && batch.Status != "Ready")
            {
                TempData["ErrorMessage"] = "Cannot edit a batch that has been processed or cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.PaymentMethods = new SelectList(new[] { "Bank Transfer", "Cheque", "Cash", "EFT", "Direct Debit" }, batch.PaymentMethod);
            return View(batch);
        }

        // POST: BatchPayments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BatchPayment batch)
        {
            if (id != batch.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _batchPaymentService.UpdateBatchPaymentAsync(batch);
                    TempData["SuccessMessage"] = "Batch updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (InvalidOperationException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            ViewBag.PaymentMethods = new SelectList(new[] { "Bank Transfer", "Cheque", "Cash", "EFT", "Direct Debit" }, batch.PaymentMethod);
            return View(batch);
        }

        // POST: BatchPayments/AddInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddInvoice(int batchId, int invoiceId, decimal? amountToPay)
        {
            try
            {
                await _batchPaymentService.AddInvoiceToBatchAsync(batchId, invoiceId, amountToPay);
                TempData["SuccessMessage"] = "Invoice added to batch successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = batchId });
        }

        // POST: BatchPayments/RemoveInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveInvoice(int batchId, int invoiceId)
        {
            try
            {
                await _batchPaymentService.RemoveInvoiceFromBatchAsync(batchId, invoiceId);
                TempData["SuccessMessage"] = "Invoice removed from batch.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = batchId });
        }

        // POST: BatchPayments/UpdateItemAmount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItemAmount(int batchId, int itemId, decimal amountToPay)
        {
            try
            {
                await _batchPaymentService.UpdateBatchItemAmountAsync(itemId, amountToPay);
                TempData["SuccessMessage"] = "Amount updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = batchId });
        }

        // POST: BatchPayments/MarkReady/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReady(int id)
        {
            try
            {
                await _batchPaymentService.MarkBatchAsReadyAsync(id);
                TempData["SuccessMessage"] = "Batch marked as ready for processing.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: BatchPayments/Process/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int id, string paymentMethod, string? referenceNumber)
        {
            try
            {
                var batch = await _batchPaymentService.GetBatchPaymentByIdAsync(id);
                var method = paymentMethod ?? batch?.PaymentMethod ?? "Bank Transfer";

                await _batchPaymentService.ProcessBatchAsync(id, method, referenceNumber);
                TempData["SuccessMessage"] = "Batch processed successfully! All payments have been created.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error processing batch: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: BatchPayments/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _batchPaymentService.CancelBatchAsync(id);
                TempData["SuccessMessage"] = "Batch cancelled.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: BatchPayments/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var batch = await _batchPaymentService.GetBatchPaymentWithItemsAsync(id);
            if (batch == null)
            {
                return NotFound();
            }

            return View(batch);
        }

        // POST: BatchPayments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _batchPaymentService.DeleteBatchPaymentAsync(id);
                TempData["SuccessMessage"] = "Batch deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // API: Get available invoices for a batch (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetAvailableInvoices(int batchId, int? supplierId)
        {
            var invoices = await _batchPaymentService.GetInvoicesNotInBatchAsync(batchId, supplierId);

            var result = invoices.Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                SupplierName = i.Supplier?.SupplierName ?? i.Customer?.CustomerName ?? i.CustomerName,
                i.InvoiceDate,
                i.DueDate,
                i.TotalAmount,
                i.BalanceAmount,
                i.Status,
                IsOverdue = i.DueDate < DateTime.Now && i.Status != "Paid"
            });

            return Json(result);
        }

        // API: Add multiple invoices to batch (AJAX)
        [HttpPost]
        public async Task<IActionResult> AddMultipleInvoices(int batchId, [FromBody] int[] invoiceIds)
        {
            var added = 0;
            var errors = new List<string>();

            foreach (var invoiceId in invoiceIds)
            {
                try
                {
                    await _batchPaymentService.AddInvoiceToBatchAsync(batchId, invoiceId);
                    added++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Invoice {invoiceId}: {ex.Message}");
                }
            }

            return Json(new { success = added > 0, added, errors });
        }
    }
}
