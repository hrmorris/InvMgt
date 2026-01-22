using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InvoiceManagement.Models;
using InvoiceManagement.Services;
using InvoiceManagement.Authorization;

namespace InvoiceManagement.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IInvoiceService _invoiceService;
        private readonly IImportService _importService;
        private readonly IPdfService _pdfService;

        public PaymentsController(IPaymentService paymentService, IInvoiceService invoiceService, IImportService importService, IPdfService pdfService)
        {
            _paymentService = paymentService;
            _invoiceService = invoiceService;
            _importService = importService;
            _pdfService = pdfService;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var payments = await _paymentService.GetAllPaymentsAsync();
            return View(payments);
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentService.GetPaymentWithAllocationsAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        public async Task<IActionResult> Create(int? invoiceId)
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            ViewBag.Invoices = new SelectList(invoices.Where(i => i.Status != "Paid"), "Id", "InvoiceNumber", invoiceId);

            var payment = new Payment
            {
                PaymentDate = DateTime.Now
            };

            if (invoiceId.HasValue)
            {
                payment.InvoiceId = invoiceId.Value;
                var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId.Value);
                if (invoice != null)
                {
                    payment.Amount = invoice.BalanceAmount;
                }
            }

            return View(payment);
        }

        // POST: Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            // Allow saving without invoice allocation - user can allocate later from Details view
            if (!payment.InvoiceId.HasValue || payment.InvoiceId == 0)
            {
                payment.InvoiceId = null;
                payment.Status = "Unallocated";
            }

            if (ModelState.IsValid)
            {
                await _paymentService.CreatePaymentAsync(payment);
                TempData["SuccessMessage"] = $"Payment {payment.PaymentNumber} recorded successfully!";
                return RedirectToAction(nameof(Details), new { id = payment.Id });
            }

            var invoices = await _invoiceService.GetAllInvoicesAsync();
            ViewBag.Invoices = new SelectList(invoices.Where(i => i.Status != "Paid"), "Id", "InvoiceNumber", payment.InvoiceId);
            return View(payment);
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _paymentService.GetPaymentWithAllocationsAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            // Get available invoices (unpaid or partially paid)
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var availableInvoices = invoices.Where(i => i.Status != "Paid").ToList();
            ViewBag.Invoices = new SelectList(availableInvoices, "Id", "InvoiceNumber", payment.InvoiceId);

            // Get all invoices for allocation dropdown
            ViewBag.AllInvoices = availableInvoices;

            return View(payment);
        }

        // POST: Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment payment)
        {
            if (id != payment.Id)
            {
                return NotFound();
            }

            // Allow saving without invoice (for unallocated payments)
            ModelState.Remove("InvoiceId");

            if (ModelState.IsValid)
            {
                // If InvoiceId is null or 0, set status to Unallocated
                if (!payment.InvoiceId.HasValue || payment.InvoiceId == 0)
                {
                    payment.InvoiceId = null;
                    payment.Status = "Unallocated";
                }

                await _paymentService.UpdatePaymentAsync(payment);
                TempData["SuccessMessage"] = "Payment updated successfully!";
                return RedirectToAction(nameof(Details), new { id = payment.Id });
            }

            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var availableInvoices = invoices.Where(i => i.Status != "Paid").ToList();
            ViewBag.Invoices = new SelectList(availableInvoices, "Id", "InvoiceNumber", payment.InvoiceId);
            ViewBag.AllInvoices = availableInvoices;
            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _paymentService.DeletePaymentAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Payments/Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: Payments/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to import.");
                return View();
            }

            try
            {
                List<Payment> payments;
                var extension = Path.GetExtension(file.FileName).ToLower();

                using (var stream = file.OpenReadStream())
                {
                    if (extension == ".csv")
                    {
                        payments = await _importService.ImportPaymentsFromCsvAsync(stream);
                    }
                    else if (extension == ".xlsx" || extension == ".xls")
                    {
                        payments = await _importService.ImportPaymentsFromExcelAsync(stream);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unsupported file format. Please use CSV or Excel files.");
                        return View();
                    }
                }

                foreach (var payment in payments)
                {
                    await _paymentService.CreatePaymentAsync(payment);
                }

                TempData["SuccessMessage"] = $"Successfully imported {payments.Count} payment(s).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error importing file: {ex.Message}");
                return View();
            }
        }

        // GET: Payments/GenerateReceipt/5
        public async Task<IActionResult> GenerateReceipt(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            var pdfBytes = await _pdfService.GeneratePaymentReceiptPdfAsync(payment);
            return File(pdfBytes, "application/pdf", $"Receipt_{payment.PaymentNumber}.pdf");
        }

        // GET: Payments/ManageAllocations/5
        public async Task<IActionResult> ManageAllocations(int id)
        {
            var payment = await _paymentService.GetPaymentWithAllocationsAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            // Get available invoices (unpaid or partially paid)
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var availableInvoices = invoices
                .Where(i => i.Status != "Paid" && i.BalanceAmount > 0)
                .OrderByDescending(i => i.InvoiceDate)
                .ToList();

            ViewBag.AvailableInvoices = availableInvoices;
            return View(payment);
        }

        // POST: Payments/AddAllocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAllocation(int paymentId, int invoiceId, decimal amount, string? notes)
        {
            try
            {
                await _paymentService.AllocatePaymentToInvoiceAsync(paymentId, invoiceId, amount, notes);
                TempData["SuccessMessage"] = $"Successfully allocated ${amount:N2} to invoice.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageAllocations), new { id = paymentId });
        }

        // POST: Payments/RemoveAllocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAllocation(int allocationId, int paymentId)
        {
            try
            {
                await _paymentService.DeleteAllocationAsync(allocationId);
                TempData["SuccessMessage"] = "Allocation removed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageAllocations), new { id = paymentId });
        }

        // GET: Payments/Unallocated
        public async Task<IActionResult> Unallocated()
        {
            var payments = await _paymentService.GetUnallocatedPaymentsAsync();
            return View(payments);
        }

        // GET: Payments/PartiallyAllocated
        public async Task<IActionResult> PartiallyAllocated()
        {
            var payments = await _paymentService.GetPartiallyAllocatedPaymentsAsync();
            return View(payments);
        }
    }
}

