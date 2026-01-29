using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Models;
using InvoiceManagement.Services;
using InvoiceManagement.Data;
using InvoiceManagement.Authorization;

namespace InvoiceManagement.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IImportService _importService;
        private readonly IPdfService _pdfService;
        private readonly IPaymentService _paymentService;
        private readonly ISupplierService _supplierService;
        private readonly ICustomerService _customerService;
        private readonly ILogger<InvoicesController> _logger;
        private readonly ApplicationDbContext _context;

        public InvoicesController(IInvoiceService invoiceService, IImportService importService, IPdfService pdfService, IPaymentService paymentService, ISupplierService supplierService, ICustomerService customerService, ILogger<InvoicesController> logger, ApplicationDbContext context)
        {
            _invoiceService = invoiceService;
            _importService = importService;
            _pdfService = pdfService;
            _paymentService = paymentService;
            _supplierService = supplierService;
            _customerService = customerService;
            _logger = logger;
            _context = context;
        }

        // GET: Invoices
        public async Task<IActionResult> Index(string searchTerm)
        {
            IEnumerable<Invoice> invoices;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                invoices = await _invoiceService.SearchInvoicesAsync(searchTerm);
                ViewBag.SearchTerm = searchTerm;
            }
            else
            {
                invoices = await _invoiceService.GetAllInvoicesAsync();
            }

            return View(invoices);
        }

        // GET: Invoices/SupplierOutstanding
        public async Task<IActionResult> SupplierOutstanding()
        {
            var viewModel = await _invoiceService.GetSupplierOutstandingAsync();
            return View(viewModel);
        }

        // GET: Invoices/OverAllocated
        public async Task<IActionResult> OverAllocated()
        {
            var invoices = await _invoiceService.GetOverAllocatedInvoicesAsync();
            return View(invoices);
        }

        // POST: Invoices/RecalculatePaidAmount/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculatePaidAmount(int id)
        {
            await _invoiceService.RecalculateInvoicePaidAmountAsync(id);
            TempData["SuccessMessage"] = "Invoice paid amount has been recalculated from actual allocations.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Invoices/RecalculateAllPaidAmounts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculateAllPaidAmounts()
        {
            await _invoiceService.RecalculateAllInvoicePaidAmountsAsync();
            TempData["SuccessMessage"] = "All invoice paid amounts have been recalculated from actual allocations.";
            return RedirectToAction(nameof(OverAllocated));
        }

        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Invoices/Create
        public async Task<IActionResult> Create()
        {
            var invoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(30),
                InvoiceType = "Supplier" // Default to Supplier invoice (most common use case)
            };

            // Get suppliers for dropdown
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Suppliers = suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToList();

            return View(invoice);
        }

        // POST: Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invoice invoice, List<InvoiceItem> items)
        {
            // Remove validation errors for fields we calculate server-side
            ModelState.Remove("InvoiceItems");
            ModelState.Remove("TotalAmount");
            ModelState.Remove("PaidAmount");
            ModelState.Remove("Status");

            // Ensure items list is not null and remove any empty items
            if (items != null)
            {
                items = items.Where(i => !string.IsNullOrWhiteSpace(i.Description)).ToList();
            }
            else
            {
                items = new List<InvoiceItem>();
            }

            // Validate that we have at least one item
            if (!items.Any())
            {
                ModelState.AddModelError("", "Please add at least one invoice item.");
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                ViewBag.Suppliers = suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToList();
                return View(invoice);
            }

            if (ModelState.IsValid)
            {
                invoice.InvoiceItems = items;
                invoice.SubTotal = invoice.InvoiceItems.Sum(i => i.TotalPrice);
                // Only calculate GST if GSTEnabled is true
                invoice.GSTAmount = invoice.GSTEnabled ? invoice.SubTotal * (invoice.GSTRate / 100) : 0;
                invoice.TotalAmount = invoice.SubTotal + invoice.GSTAmount;
                invoice.Status = "Unpaid";
                invoice.PaidAmount = 0;
                invoice.CreatedDate = DateTime.Now;

                await _invoiceService.CreateInvoiceAsync(invoice);
                TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} created successfully!";
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }

            // If we got this far, something failed, redisplay form
            var suppliersForView = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Suppliers = suppliersForView.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToList();
            return View(invoice);
        }

        // GET: Invoices/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            // Get available payments (unallocated or partially allocated)
            var allPayments = await _paymentService.GetAllPaymentsAsync();
            var availablePayments = allPayments
                .Where(p => p.Status == "Unallocated" || p.Status == "Partially Allocated")
                .Where(p => p.UnallocatedAmount > 0)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            ViewBag.AvailablePayments = availablePayments;

            // Get suppliers for dropdown
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Suppliers = suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToList();

            // Get customers for dropdown
            var customers = await _customerService.GetAllCustomersAsync();
            ViewBag.Customers = customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToList();

            return View(invoice);
        }

        // POST: Invoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Invoice invoice, List<InvoiceItem> items)
        {
            if (id != invoice.Id)
            {
                return NotFound();
            }

            // Log what was received
            _logger.LogInformation($"Edit POST - Invoice {id}, Items received: {items?.Count ?? 0}");
            if (items != null && items.Any())
            {
                foreach (var item in items)
                {
                    _logger.LogInformation($"  Item: {item.Description}, Qty: {item.Quantity}, Price: {item.UnitPrice}");
                }
            }

            // Remove validation errors for fields we calculate server-side
            ModelState.Remove("InvoiceItems");
            ModelState.Remove("TotalAmount");
            ModelState.Remove("SubTotal");
            ModelState.Remove("GSTAmount");
            ModelState.Remove("Supplier"); // Navigation property, not posted
            ModelState.Remove("Customer"); // Navigation property, not posted

            // Normalize InvoiceType - treat "Payable" as "Supplier"
            if (invoice.InvoiceType == "Payable")
            {
                invoice.InvoiceType = "Supplier";
            }
            else if (invoice.InvoiceType == "Receivable")
            {
                invoice.InvoiceType = "Customer";
            }

            // For Supplier invoices, CustomerName is not required - use supplier info instead
            if (invoice.InvoiceType == "Supplier")
            {
                ModelState.Remove("CustomerName");

                if (invoice.SupplierId.HasValue)
                {
                    // ALWAYS get supplier name to use as CustomerName when supplier is selected
                    var supplier = await _context.Suppliers.FindAsync(invoice.SupplierId.Value);
                    if (supplier != null)
                    {
                        invoice.CustomerName = supplier.SupplierName;
                        invoice.CustomerAddress = supplier.Address;
                        invoice.CustomerEmail = supplier.Email;
                        invoice.CustomerPhone = supplier.Phone;
                    }
                }
                else if (string.IsNullOrWhiteSpace(invoice.CustomerName))
                {
                    // No supplier selected, use a default name
                    invoice.CustomerName = "Unknown Supplier";
                }
            }

            // For Customer invoices, always populate from customer if selected
            if (invoice.InvoiceType == "Customer" && invoice.CustomerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(invoice.CustomerId.Value);
                if (customer != null)
                {
                    invoice.CustomerName = customer.CustomerName;
                    invoice.CustomerAddress = customer.Address;
                    invoice.CustomerEmail = customer.Email;
                    invoice.CustomerPhone = customer.Phone;
                }
            }

            // Ensure items list is not null and remove any empty items
            if (items != null)
            {
                items = items.Where(i => !string.IsNullOrWhiteSpace(i.Description)).ToList();
            }
            else
            {
                items = new List<InvoiceItem>();
            }

            _logger.LogInformation($"After filtering, items count: {items.Count}");

            // Validate that we have at least one item
            if (!items.Any())
            {
                _logger.LogWarning($"No items found for invoice {id}. Returning validation error.");
                ModelState.AddModelError("", "Please add at least one invoice item.");

                // Reload the invoice with its items from database to display existing data
                var existingInvoice = await _invoiceService.GetInvoiceByIdAsync(invoice.Id);
                if (existingInvoice == null)
                {
                    return NotFound();
                }

                // Get available payments for the view
                var allPayments = await _paymentService.GetAllPaymentsAsync();
                var availablePayments = allPayments
                    .Where(p => p.Status == "Unallocated" || p.Status == "Partially Allocated")
                    .Where(p => p.UnallocatedAmount > 0)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();
                ViewBag.AvailablePayments = availablePayments;

                // Get suppliers for dropdown
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                ViewBag.Suppliers = suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToList();

                // Get customers for dropdown
                var customers = await _customerService.GetAllCustomersAsync();
                ViewBag.Customers = customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToList();

                return View(existingInvoice);
            }

            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"ModelState is invalid for invoice {id}");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning($"  Field: {state.Key}, Error: {error.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                invoice.InvoiceItems = items;
                invoice.SubTotal = invoice.InvoiceItems.Sum(i => i.TotalPrice);
                // Only calculate GST if GSTEnabled is true
                invoice.GSTAmount = invoice.GSTEnabled ? invoice.SubTotal * (invoice.GSTRate / 100) : 0;
                invoice.TotalAmount = invoice.SubTotal + invoice.GSTAmount;
                invoice.ModifiedDate = DateTime.Now;

                await _invoiceService.UpdateInvoiceAsync(invoice);

                // Update linked documents with the supplier/customer name
                var linkedDocuments = await _context.ImportedDocuments
                    .Where(d => d.InvoiceId == invoice.Id)
                    .ToListAsync();

                if (linkedDocuments.Any())
                {
                    var supplierName = invoice.InvoiceType == "Sales"
                        ? invoice.CustomerName
                        : (invoice.Supplier?.SupplierName ?? invoice.CustomerName);

                    // If supplier is set, get the supplier name from database
                    if (invoice.SupplierId.HasValue)
                    {
                        var supplier = await _context.Suppliers.FindAsync(invoice.SupplierId.Value);
                        if (supplier != null)
                        {
                            supplierName = supplier.SupplierName;
                        }
                    }

                    foreach (var doc in linkedDocuments)
                    {
                        doc.ExtractedSupplierName = supplierName;
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} updated successfully!";
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }

            // If validation failed for other reasons, reload the full invoice with items
            var invoiceToReturn = await _invoiceService.GetInvoiceByIdAsync(invoice.Id);
            if (invoiceToReturn == null)
            {
                return NotFound();
            }

            // Get available payments for the view
            var paymentsForView = await _paymentService.GetAllPaymentsAsync();
            var availablePaymentsForView = paymentsForView
                .Where(p => p.Status == "Unallocated" || p.Status == "Partially Allocated")
                .Where(p => p.UnallocatedAmount > 0)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();
            ViewBag.AvailablePayments = availablePaymentsForView;

            // Get suppliers for dropdown
            var suppliersForView = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Suppliers = suppliersForView.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToList();

            // Get customers for dropdown
            var customersForView = await _customerService.GetAllCustomersAsync();
            ViewBag.Customers = customersForView.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToList();

            return View(invoiceToReturn);
        }

        // GET: Invoices/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _invoiceService.DeleteInvoiceAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Invoices/Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: Invoices/Import
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
                List<Invoice> invoices;
                var extension = Path.GetExtension(file.FileName).ToLower();

                using (var stream = file.OpenReadStream())
                {
                    if (extension == ".csv")
                    {
                        invoices = await _importService.ImportInvoicesFromCsvAsync(stream);
                    }
                    else if (extension == ".xlsx" || extension == ".xls")
                    {
                        invoices = await _importService.ImportInvoicesFromExcelAsync(stream);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unsupported file format. Please use CSV or Excel files.");
                        return View();
                    }
                }

                foreach (var invoice in invoices)
                {
                    await _invoiceService.CreateInvoiceAsync(invoice);
                }

                TempData["SuccessMessage"] = $"Successfully imported {invoices.Count} invoice(s).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error importing file: {ex.Message}");
                return View();
            }
        }

        // GET: Invoices/GeneratePdf/5
        public async Task<IActionResult> GeneratePdf(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoice);
            return File(pdfBytes, "application/pdf", $"Invoice_{invoice.InvoiceNumber}.pdf");
        }

        // GET: Invoices/Overdue
        public async Task<IActionResult> Overdue()
        {
            var invoices = await _invoiceService.GetOverdueInvoicesAsync();
            return View(invoices);
        }

        // POST: Invoices/AllocatePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocatePayment(int invoiceId, int paymentId, decimal amount, string? notes)
        {
            _logger.LogInformation($"AllocatePayment called: invoiceId={invoiceId}, paymentId={paymentId}, amount={amount}");

            try
            {
                await _paymentService.AllocatePaymentToInvoiceAsync(paymentId, invoiceId, amount, notes);
                TempData["SuccessMessage"] = $"Successfully allocated ${amount:N2} from payment to this invoice.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AllocatePayment failed: {ex.Message}");

                // If the error is due to stale PaidAmount data, try to auto-fix
                if (ex.Message.Contains("already fully paid") || ex.Message.Contains("Balance:"))
                {
                    // Recalculate invoice paid amount from actual allocations
                    await _invoiceService.RecalculateInvoicePaidAmountAsync(invoiceId);
                    TempData["ErrorMessage"] = $"⚠️ {ex.Message} The invoice data was recalculated. Please try again.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        // POST: Invoices/RemovePaymentAllocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePaymentAllocation(int allocationId, int invoiceId)
        {
            try
            {
                // Get allocation details before removing for better feedback
                var allocation = await _paymentService.GetAllocationByIdAsync(allocationId);
                decimal amount = 0;
                string paymentNumber = "";

                if (allocation != null)
                {
                    amount = allocation.AllocatedAmount;
                    paymentNumber = allocation.Payment?.PaymentNumber ?? "";
                }

                await _paymentService.DeleteAllocationAsync(allocationId);

                TempData["SuccessMessage"] = $"✓ Payment unallocated successfully! " +
                    $"${amount:N2} from payment {paymentNumber} is now available for other invoices. " +
                    $"Invoice balance has been updated.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error unallocating payment: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        // POST: Invoices/MarkUnpaid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnpaid(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.PaymentAllocations)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    TempData["ErrorMessage"] = "Invoice not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Remove all payment allocations for this invoice
                if (invoice.PaymentAllocations != null && invoice.PaymentAllocations.Any())
                {
                    var allocationsToRemove = invoice.PaymentAllocations.ToList();
                    foreach (var allocation in allocationsToRemove)
                    {
                        // Update the payment status
                        var payment = await _context.Payments.FindAsync(allocation.PaymentId);
                        if (payment != null)
                        {
                            // Recalculate payment allocations
                            var remainingAllocations = await _context.PaymentAllocations
                                .Where(pa => pa.PaymentId == payment.Id && pa.Id != allocation.Id)
                                .SumAsync(pa => pa.AllocatedAmount);

                            if (remainingAllocations == 0)
                            {
                                payment.Status = "Unallocated";
                            }
                            else if (remainingAllocations < payment.Amount)
                            {
                                payment.Status = "Partially Allocated";
                            }
                        }

                        _context.PaymentAllocations.Remove(allocation);
                    }
                }

                // Reset the invoice status
                invoice.PaidAmount = 0;
                invoice.Status = "Unpaid";
                invoice.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} has been marked as Unpaid. All payment allocations have been removed and are now available for reallocation.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice as unpaid");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Edit), new { id = id });
        }

        // POST: Invoices/CreateSupplierFromInvoice
        [HttpPost]
        public async Task<IActionResult> CreateSupplierFromInvoice([FromBody] CreateSupplierRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SupplierName))
                {
                    return Json(new { success = false, message = "Supplier name is required" });
                }

                // Check if supplier already exists
                var existingSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.SupplierName.ToLower() == request.SupplierName.ToLower());

                if (existingSupplier != null)
                {
                    return Json(new { success = false, message = $"Supplier '{request.SupplierName}' already exists", existingId = existingSupplier.Id });
                }

                var supplier = new Supplier
                {
                    SupplierName = request.SupplierName,
                    Address = request.Address,
                    Email = request.Email,
                    Phone = request.Phone,
                    Status = "Active",
                    CreatedDate = DateTime.Now
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    supplierId = supplier.Id,
                    supplierName = supplier.SupplierName,
                    message = $"Supplier '{supplier.SupplierName}' created successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier from invoice");
                return Json(new { success = false, message = $"Error creating supplier: {ex.Message}" });
            }
        }

        // POST: Invoices/CreateCustomerFromInvoice
        [HttpPost]
        public async Task<IActionResult> CreateCustomerFromInvoice([FromBody] CreateCustomerRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CustomerName))
                {
                    return Json(new { success = false, message = "Customer name is required" });
                }

                // Check if customer already exists
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerName.ToLower() == request.CustomerName.ToLower());

                if (existingCustomer != null)
                {
                    return Json(new { success = false, message = $"Customer '{request.CustomerName}' already exists", existingId = existingCustomer.Id });
                }

                var customer = new Customer
                {
                    CustomerName = request.CustomerName,
                    Address = request.Address,
                    Email = request.Email,
                    Phone = request.Phone,
                    Status = "Active",
                    CreatedDate = DateTime.Now
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    customerId = customer.Id,
                    customerName = customer.CustomerName,
                    message = $"Customer '{customer.CustomerName}' created successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer from invoice");
                return Json(new { success = false, message = $"Error creating customer: {ex.Message}" });
            }
        }
    }

    public class CreateSupplierRequest
    {
        public string SupplierName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class CreateCustomerRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}

