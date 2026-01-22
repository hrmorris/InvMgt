using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;
using InvoiceManagement.Models;
using InvoiceManagement.Services;
using InvoiceManagement.Models.ViewModels;
using System.Text.RegularExpressions;

namespace InvoiceManagement.Controllers
{
    public class AiImportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiProcessingService _aiService;
        private readonly IDocumentStorageService _documentService;
        private readonly IEntityLookupService _entityLookupService;
        private readonly ILogger<AiImportController> _logger;
        private readonly string[] _allowedExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        private readonly long _maxFileSize = 500 * 1024 * 1024; // 500MB

        public AiImportController(
            ApplicationDbContext context,
            IAiProcessingService aiService,
            IDocumentStorageService documentService,
            IEntityLookupService entityLookupService,
            ILogger<AiImportController> logger)
        {
            _context = context;
            _aiService = aiService;
            _documentService = documentService;
            _entityLookupService = entityLookupService;
            _logger = logger;
        }

        // GET: AiImport
        public IActionResult Index()
        {
            return View();
        }

        // GET: AiImport/Invoice
        public async Task<IActionResult> Invoice()
        {
            ViewBag.Suppliers = await _context.Suppliers
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            ViewBag.Customers = await _context.Customers
                .Where(c => c.Status == "Active")
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            return View();
        }

        // GET: AiImport/ReviewInvoice - Review an existing imported document
        public async Task<IActionResult> ReviewInvoice(int documentId)
        {
            var document = await _context.ImportedDocuments
                .Include(d => d.Invoice)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(Documents));
            }

            // If already linked to an invoice, redirect to that invoice
            if (document.InvoiceId.HasValue)
            {
                TempData["Info"] = "This document is already linked to an invoice.";
                return RedirectToAction("Details", "Invoices", new { id = document.InvoiceId });
            }

            try
            {
                // Re-process the document with AI
                Invoice? extractedInvoice = null;
                using (var stream = new MemoryStream(document.FileContent))
                {
                    extractedInvoice = await _aiService.ExtractInvoiceFromFileAsync(stream, document.OriginalFileName);
                }

                if (extractedInvoice == null)
                {
                    TempData["Error"] = "Could not extract invoice data from the document.";
                    return RedirectToAction(nameof(Documents));
                }

                // Try to match supplier by name
                Supplier? matchedSupplier = null;
                if (extractedInvoice.Supplier != null && !string.IsNullOrEmpty(extractedInvoice.Supplier.SupplierName))
                {
                    matchedSupplier = await _entityLookupService.FindSupplierByNameAsync(extractedInvoice.Supplier.SupplierName);
                }

                // Try to match customer by name
                Customer? matchedCustomer = null;
                if (!string.IsNullOrEmpty(extractedInvoice.CustomerName))
                {
                    matchedCustomer = await _entityLookupService.FindCustomerByNameAsync(extractedInvoice.CustomerName);
                }

                // Get all suppliers and customers for dropdown
                var suppliers = await _entityLookupService.GetAllSuppliersAsync();
                var customers = await _entityLookupService.GetAllCustomersAsync();

                var viewModel = new AiImportInvoiceViewModel
                {
                    DocumentId = document.Id,
                    OriginalFileName = document.OriginalFileName,
                    InvoiceNumber = extractedInvoice.InvoiceNumber ?? "",
                    InvoiceDate = extractedInvoice.InvoiceDate,
                    DueDate = extractedInvoice.DueDate,
                    CustomerName = extractedInvoice.CustomerName ?? "",
                    CustomerAddress = extractedInvoice.CustomerAddress ?? "",
                    CustomerEmail = extractedInvoice.CustomerEmail ?? "",
                    CustomerPhone = extractedInvoice.CustomerPhone ?? "",
                    SubTotal = extractedInvoice.SubTotal,
                    GSTAmount = extractedInvoice.GSTAmount,
                    TotalAmount = extractedInvoice.TotalAmount,
                    Notes = extractedInvoice.Notes ?? "",
                    InvoiceType = extractedInvoice.InvoiceType ?? "Payable",
                    Items = extractedInvoice.InvoiceItems?.Select(i => new AiImportInvoiceItemViewModel
                    {
                        Description = i.Description ?? "",
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList() ?? new List<AiImportInvoiceItemViewModel>(),
                    MatchedSupplierId = matchedSupplier?.Id,
                    MatchedSupplierName = matchedSupplier?.SupplierName,
                    ExtractedSupplierName = extractedInvoice.Supplier?.SupplierName ?? document.ExtractedSupplierName ?? "",
                    MatchedCustomerId = matchedCustomer?.Id,
                    MatchedCustomerName = matchedCustomer?.CustomerName,
                    ExtractedCustomerName = extractedInvoice.CustomerName ?? document.ExtractedCustomerName ?? "",
                    AvailableSuppliers = suppliers.ToList(),
                    AvailableCustomers = customers.ToList(),
                    MatchConfidence = matchedSupplier != null || matchedCustomer != null ? "High" : "None"
                };

                TempData["Success"] = "Invoice data extracted successfully. Please review and confirm.";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-processing document with AI");
                TempData["Error"] = $"Error processing document: {ex.Message}";
                return RedirectToAction(nameof(Documents));
            }
        }

        // GET: AiImport/ReviewPayment - Review an existing imported payment document
        public async Task<IActionResult> ReviewPayment(int documentId)
        {
            var document = await _context.ImportedDocuments
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(Documents));
            }

            // If already linked to a payment, redirect to that payment
            if (document.PaymentId.HasValue)
            {
                TempData["Info"] = "This document is already linked to a payment.";
                return RedirectToAction("Details", "Payments", new { id = document.PaymentId });
            }

            try
            {
                // Re-process the document with AI
                Payment? extractedPayment = null;
                using (var stream = new MemoryStream(document.FileContent))
                {
                    extractedPayment = await _aiService.ExtractPaymentFromFileAsync(stream, document.OriginalFileName);
                }

                if (extractedPayment == null)
                {
                    TempData["Error"] = "Could not extract payment data from the document.";
                    return RedirectToAction(nameof(Documents));
                }

                // Try to match supplier by name
                Supplier? matchedSupplier = null;
                if (!string.IsNullOrEmpty(extractedPayment.PayeeName))
                {
                    matchedSupplier = await _entityLookupService.FindSupplierByNameAsync(extractedPayment.PayeeName);
                }

                // Try to match customer by name
                Customer? matchedCustomer = null;
                if (!string.IsNullOrEmpty(extractedPayment.PayerName))
                {
                    matchedCustomer = await _entityLookupService.FindCustomerByNameAsync(extractedPayment.PayerName);
                }

                // Get all suppliers and customers for dropdown
                var suppliers = await _entityLookupService.GetAllSuppliersAsync();
                var customers = await _entityLookupService.GetAllCustomersAsync();
                var availableInvoices = await _context.Invoices
                    .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                var viewModel = new AiImportPaymentViewModel
                {
                    DocumentId = document.Id,
                    OriginalFileName = document.OriginalFileName,
                    PaymentDate = extractedPayment.PaymentDate != default ? extractedPayment.PaymentDate : DateTime.Now,
                    Amount = extractedPayment.Amount,
                    PaymentMethod = extractedPayment.PaymentMethod ?? "Bank Transfer",
                    ReferenceNumber = extractedPayment.ReferenceNumber ?? "",
                    Notes = extractedPayment.Notes ?? "",
                    ExtractedBankName = extractedPayment.BankName ?? "",
                    ExtractedBankAccountNumber = extractedPayment.BankAccountNumber ?? "",
                    ExtractedPayerName = extractedPayment.PayerName ?? "",
                    ExtractedPayeeName = extractedPayment.PayeeName ?? "",
                    MatchedSupplierId = matchedSupplier?.Id,
                    MatchedSupplierName = matchedSupplier?.SupplierName,
                    MatchedCustomerId = matchedCustomer?.Id,
                    MatchedCustomerName = matchedCustomer?.CustomerName,
                    PaymentDirection = "Outgoing", // Default for supplier payments
                    MatchConfidence = matchedSupplier != null || matchedCustomer != null ? "High" : "None",
                    AvailableSuppliers = suppliers.ToList(),
                    AvailableCustomers = customers.ToList(),
                    AvailableInvoices = availableInvoices
                };

                TempData["Success"] = "Payment data extracted successfully. Please review and confirm.";
                return View("ReviewPayment", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-processing payment document with AI");
                TempData["Error"] = $"Error processing document: {ex.Message}";
                return RedirectToAction(nameof(Documents));
            }
        }

        // GET: AiImport/RescanPayment - Re-scan a payment document with AI (alias with specific feedback)
        public async Task<IActionResult> RescanPayment(int documentId)
        {
            var document = await _context.ImportedDocuments
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(Documents));
            }

            // If already linked to a payment, still allow rescan but warn
            if (document.PaymentId.HasValue)
            {
                TempData["Warning"] = "This document is already linked to a payment. Rescan will show new extraction but won't affect the existing payment.";
            }

            try
            {
                // Re-process the document with AI
                Payment? extractedPayment = null;
                using (var stream = new MemoryStream(document.FileContent))
                {
                    extractedPayment = await _aiService.ExtractPaymentFromFileAsync(stream, document.OriginalFileName);
                }

                if (extractedPayment == null)
                {
                    TempData["Error"] = "AI could not extract payment data from this document. Please try a different document or enter data manually.";
                    return RedirectToAction(nameof(Documents));
                }

                // Try to match supplier by name or bank account
                Supplier? matchedSupplier = null;
                string? matchedByField = null;

                // First try matching by payee name
                if (!string.IsNullOrEmpty(extractedPayment.PayeeName))
                {
                    matchedSupplier = await _entityLookupService.FindSupplierByNameAsync(extractedPayment.PayeeName);
                    if (matchedSupplier != null) matchedByField = "PayeeName";
                }

                // If no match by name, try matching by bank account number
                if (matchedSupplier == null && !string.IsNullOrEmpty(extractedPayment.PayeeAccountNumber))
                {
                    matchedSupplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.BankAccountNumber != null &&
                            s.BankAccountNumber.Contains(extractedPayment.PayeeAccountNumber));
                    if (matchedSupplier != null) matchedByField = "BankAccount";
                }

                // Try to match customer by name
                Customer? matchedCustomer = null;
                if (!string.IsNullOrEmpty(extractedPayment.PayerName))
                {
                    matchedCustomer = await _entityLookupService.FindCustomerByNameAsync(extractedPayment.PayerName);
                }

                // Get all suppliers and customers for dropdown
                var suppliers = await _entityLookupService.GetAllSuppliersAsync();
                var customers = await _entityLookupService.GetAllCustomersAsync();
                var availableInvoices = await _context.Invoices
                    .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                var viewModel = new AiImportPaymentViewModel
                {
                    DocumentId = document.Id,
                    OriginalFileName = document.OriginalFileName,
                    PaymentDate = extractedPayment.PaymentDate != default ? extractedPayment.PaymentDate : DateTime.Now,
                    Amount = extractedPayment.Amount,
                    PaymentMethod = extractedPayment.PaymentMethod ?? "Bank Transfer",
                    ReferenceNumber = extractedPayment.ReferenceNumber ?? "",
                    Notes = extractedPayment.Notes ?? "",
                    ExtractedBankName = extractedPayment.BankName ?? "",
                    ExtractedBankAccountNumber = extractedPayment.BankAccountNumber ?? "",
                    ExtractedPayerName = extractedPayment.PayerName ?? "",
                    ExtractedPayeeName = extractedPayment.PayeeName ?? "",
                    TransferTo = extractedPayment.TransferTo ?? "",
                    AccountType = extractedPayment.AccountType ?? "",
                    PayeeBranchNumber = extractedPayment.PayeeBranchNumber ?? "",
                    PayeeAccountNumber = extractedPayment.PayeeAccountNumber ?? "",
                    PayerAccountFull = extractedPayment.PayerBankAccountNumber ?? "",
                    PayerBranchNumber = extractedPayment.PayerBranchNumber ?? "",
                    PayerAccountNumber = extractedPayment.PayerAccountNumber ?? "",
                    Currency = extractedPayment.Currency ?? "PGK",
                    Purpose = extractedPayment.Purpose ?? "",
                    MatchedSupplierId = matchedSupplier?.Id,
                    MatchedSupplierName = matchedSupplier?.SupplierName,
                    MatchedCustomerId = matchedCustomer?.Id,
                    MatchedCustomerName = matchedCustomer?.CustomerName,
                    PaymentDirection = "Outgoing",
                    MatchConfidence = matchedSupplier != null ? "High" : "None",
                    MatchedByField = matchedByField,
                    AvailableSuppliers = suppliers.ToList(),
                    AvailableCustomers = customers.ToList(),
                    AvailableInvoices = availableInvoices
                };

                TempData["Success"] = "Document re-scanned with AI successfully. Extracted data has been refreshed.";
                return View("ReviewPayment", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescanning payment document with AI");
                TempData["Error"] = $"AI rescan failed: {ex.Message}";
                return RedirectToAction("ReviewPayment", new { documentId });
            }
        }

        // POST: AiImport/ProcessInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessInvoice(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Invoice));
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                TempData["Error"] = $"Invalid file type. Allowed types: {string.Join(", ", _allowedExtensions)}";
                return RedirectToAction(nameof(Invoice));
            }

            // Validate file size
            if (file.Length > _maxFileSize)
            {
                TempData["Error"] = "File size exceeds 500MB limit.";
                return RedirectToAction(nameof(Invoice));
            }

            try
            {
                // Store the document first
                var importedDocument = await _documentService.StoreDocumentAsync(
                    file,
                    "Invoice",
                    User.Identity?.Name ?? "System"
                );

                // Process with AI using the file stream
                Invoice? extractedInvoice;
                using (var stream = file.OpenReadStream())
                {
                    extractedInvoice = await _aiService.ExtractInvoiceFromFileAsync(stream, file.FileName);
                }

                if (extractedInvoice == null)
                {
                    // Update document status
                    await _documentService.UpdateDocumentExtractedDataAsync(
                        importedDocument.Id, null, null, null, null, null
                    );

                    TempData["Error"] = "Could not extract invoice data from the document.";
                    return RedirectToAction(nameof(Invoice));
                }

                // Update document with extracted data
                await _documentService.UpdateDocumentExtractedDataAsync(
                    importedDocument.Id,
                    null, // extractedText
                    null, // accountNumber
                    null, // bankName
                    extractedInvoice.Supplier?.SupplierName,
                    extractedInvoice.CustomerName
                );

                // Try to match supplier by name
                Supplier? matchedSupplier = null;
                if (extractedInvoice.Supplier != null && !string.IsNullOrEmpty(extractedInvoice.Supplier.SupplierName))
                {
                    matchedSupplier = await _entityLookupService.FindSupplierByNameAsync(extractedInvoice.Supplier.SupplierName);
                }

                // Try to match customer by name
                Customer? matchedCustomer = null;
                if (!string.IsNullOrEmpty(extractedInvoice.CustomerName))
                {
                    matchedCustomer = await _entityLookupService.FindCustomerByNameAsync(extractedInvoice.CustomerName);
                }

                // Get all suppliers and customers for dropdown
                var suppliers = await _entityLookupService.GetAllSuppliersAsync();
                var customers = await _entityLookupService.GetAllCustomersAsync();

                var viewModel = new AiImportInvoiceViewModel
                {
                    DocumentId = importedDocument.Id,
                    OriginalFileName = file.FileName,
                    InvoiceNumber = extractedInvoice.InvoiceNumber ?? "",
                    InvoiceDate = extractedInvoice.InvoiceDate,
                    DueDate = extractedInvoice.DueDate,
                    CustomerName = extractedInvoice.CustomerName ?? "",
                    CustomerAddress = extractedInvoice.CustomerAddress ?? "",
                    CustomerEmail = extractedInvoice.CustomerEmail ?? "",
                    CustomerPhone = extractedInvoice.CustomerPhone ?? "",
                    SubTotal = extractedInvoice.SubTotal,
                    GSTAmount = extractedInvoice.GSTAmount,
                    TotalAmount = extractedInvoice.TotalAmount,
                    Notes = extractedInvoice.Notes ?? "",
                    InvoiceType = extractedInvoice.InvoiceType ?? "Payable",
                    Items = extractedInvoice.InvoiceItems?.Select(i => new AiImportInvoiceItemViewModel
                    {
                        Description = i.Description ?? "",
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList() ?? new List<AiImportInvoiceItemViewModel>(),
                    MatchedSupplierId = matchedSupplier?.Id,
                    MatchedSupplierName = matchedSupplier?.SupplierName,
                    ExtractedSupplierName = extractedInvoice.Supplier?.SupplierName ?? "",
                    MatchedCustomerId = matchedCustomer?.Id,
                    MatchedCustomerName = matchedCustomer?.CustomerName,
                    ExtractedCustomerName = extractedInvoice.CustomerName ?? "",
                    AvailableSuppliers = suppliers.ToList(),
                    AvailableCustomers = customers.ToList(),
                    MatchConfidence = matchedSupplier != null || matchedCustomer != null ? "High" : "None"
                };

                TempData["Success"] = "Invoice data extracted successfully. Please review and confirm.";
                return View("ReviewInvoice", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing invoice with AI");
                TempData["Error"] = $"Error processing document: {ex.Message}";
                return RedirectToAction(nameof(Invoice));
            }
        }

        // POST: AiImport/SaveInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveInvoice(AiImportInvoiceViewModel model)
        {
            try
            {
                // Create the invoice
                var invoice = new Invoice
                {
                    InvoiceNumber = model.InvoiceNumber,
                    InvoiceDate = model.InvoiceDate ?? DateTime.Now,
                    DueDate = model.DueDate ?? DateTime.Now.AddDays(30),
                    CustomerName = model.CustomerName,
                    CustomerAddress = model.CustomerAddress,
                    CustomerEmail = model.CustomerEmail,
                    CustomerPhone = model.CustomerPhone,
                    SubTotal = model.SubTotal,
                    GSTAmount = model.GSTAmount,
                    GSTEnabled = model.GSTAmount > 0,
                    GSTRate = model.GSTAmount > 0 && model.SubTotal > 0 ? (model.GSTAmount / model.SubTotal) * 100 : 0,
                    TotalAmount = model.TotalAmount,
                    Notes = model.Notes,
                    InvoiceType = model.InvoiceType,
                    Status = "Draft",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                // Set supplier if selected
                if (model.SelectedSupplierId.HasValue)
                {
                    invoice.SupplierId = model.SelectedSupplierId.Value;
                }
                else if (model.MatchedSupplierId.HasValue)
                {
                    invoice.SupplierId = model.MatchedSupplierId.Value;
                }

                // Set customer if selected
                if (model.SelectedCustomerId.HasValue)
                {
                    invoice.CustomerId = model.SelectedCustomerId.Value;
                }
                else if (model.MatchedCustomerId.HasValue)
                {
                    invoice.CustomerId = model.MatchedCustomerId.Value;
                }

                // Create new supplier if requested
                if (model.CreateNewSupplier && !string.IsNullOrEmpty(model.ExtractedSupplierName))
                {
                    var newSupplier = await _entityLookupService.CreateSupplierFromPaymentDataAsync(
                        model.ExtractedSupplierName, null, null
                    );
                    invoice.SupplierId = newSupplier.Id;
                }

                // Create new customer if requested
                if (model.CreateNewCustomer && !string.IsNullOrEmpty(model.ExtractedCustomerName))
                {
                    var newCustomer = await _entityLookupService.CreateCustomerFromPaymentDataAsync(
                        model.ExtractedCustomerName, null, null
                    );
                    invoice.CustomerId = newCustomer.Id;
                }

                // Add invoice items
                if (model.Items != null && model.Items.Any())
                {
                    invoice.InvoiceItems = model.Items.Select(i => new InvoiceItem
                    {
                        Description = i.Description,
                        Quantity = (int)Math.Round(i.Quantity),
                        UnitPrice = i.UnitPrice
                    }).ToList();
                }

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Link the document to the invoice
                if (model.DocumentId > 0)
                {
                    await _documentService.LinkDocumentToInvoiceAsync(model.DocumentId, invoice.Id);
                }

                TempData["Success"] = $"Invoice {invoice.InvoiceNumber} imported successfully!";
                return RedirectToAction("Details", "Invoices", new { id = invoice.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving imported invoice");
                TempData["Error"] = $"Error saving invoice: {ex.Message}";
                return View("ReviewInvoice", model);
            }
        }

        // GET: AiImport/Payment
        public async Task<IActionResult> Payment()
        {
            ViewBag.Suppliers = await _context.Suppliers
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            ViewBag.Customers = await _context.Customers
                .Where(c => c.Status == "Active")
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            ViewBag.Invoices = await _context.Invoices
                .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return View();
        }

        // POST: AiImport/ProcessPayment
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ProcessPayment(IFormFile file)
        {
            // Check if this is an AJAX request
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers["Accept"].ToString().Contains("application/json") ||
                          Request.ContentType?.Contains("multipart/form-data") == true;

            if (file == null || file.Length == 0)
            {
                if (isAjax)
                    return Json(new { success = false, message = "Please select a file to upload." });
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Payment));
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                if (isAjax)
                    return Json(new { success = false, message = $"Invalid file type. Allowed types: {string.Join(", ", _allowedExtensions)}" });
                TempData["Error"] = $"Invalid file type. Allowed types: {string.Join(", ", _allowedExtensions)}";
                return RedirectToAction(nameof(Payment));
            }

            // Validate file size
            if (file.Length > _maxFileSize)
            {
                if (isAjax)
                    return Json(new { success = false, message = "File size exceeds 500MB limit." });
                TempData["Error"] = "File size exceeds 500MB limit.";
                return RedirectToAction(nameof(Payment));
            }

            try
            {
                // Store the document first
                var importedDocument = await _documentService.StoreDocumentAsync(
                    file,
                    "Payment",
                    User.Identity?.Name ?? "System"
                );

                // Process with AI using file stream
                Payment? extractedPayment;
                using (var stream = file.OpenReadStream())
                {
                    extractedPayment = await _aiService.ExtractPaymentFromFileAsync(stream, file.FileName);
                }

                if (extractedPayment == null)
                {
                    // Update document status
                    await _documentService.UpdateDocumentExtractedDataAsync(
                        importedDocument.Id, null, null, null, null, null
                    );

                    if (isAjax)
                        return Json(new { success = false, message = "Could not extract payment data from the document. Please try a clearer image or PDF." });
                    TempData["Error"] = "Could not extract payment data from the document.";
                    return RedirectToAction(nameof(Payment));
                }

                // Get bank details from the payment if available
                var bankAccountNumber = extractedPayment.BankAccountNumber;
                var bankName = extractedPayment.BankName;
                var payerName = extractedPayment.PayerName;
                var payeeName = extractedPayment.PayeeName;

                // Update document with extracted data
                await _documentService.UpdateDocumentExtractedDataAsync(
                    importedDocument.Id,
                    null, // extractedText
                    bankAccountNumber,
                    bankName,
                    payeeName,
                    payerName
                );

                // Try to match supplier/customer by bank account number first
                Supplier? matchedSupplier = null;
                Customer? matchedCustomer = null;
                string paymentDirection = "Unknown";
                string? matchedByField = null;

                // First try bank account lookup
                if (!string.IsNullOrEmpty(bankAccountNumber))
                {
                    // Try supplier first (outgoing payment)
                    matchedSupplier = await _entityLookupService.FindSupplierByAccountNumberAsync(bankAccountNumber);
                    if (matchedSupplier != null)
                    {
                        paymentDirection = "Outgoing";
                        matchedByField = "Bank Account";
                    }
                    else
                    {
                        // Try customer (incoming payment)
                        matchedCustomer = await _entityLookupService.FindCustomerByAccountNumberAsync(bankAccountNumber);
                        if (matchedCustomer != null)
                        {
                            paymentDirection = "Incoming";
                            matchedByField = "Bank Account";
                        }
                    }
                }

                // If no match by account number, try bank details
                if (matchedSupplier == null && matchedCustomer == null &&
                    !string.IsNullOrEmpty(bankAccountNumber) &&
                    !string.IsNullOrEmpty(bankName))
                {
                    matchedSupplier = await _entityLookupService.FindSupplierByBankDetailsAsync(
                        bankAccountNumber, bankName
                    );
                    if (matchedSupplier != null)
                    {
                        paymentDirection = "Outgoing";
                        matchedByField = "Bank Account";
                    }
                    else
                    {
                        matchedCustomer = await _entityLookupService.FindCustomerByBankDetailsAsync(
                            bankAccountNumber, bankName
                        );
                        if (matchedCustomer != null)
                        {
                            paymentDirection = "Incoming";
                            matchedByField = "Bank Account";
                        }
                    }
                }

                // If still no match, try by name
                if (matchedSupplier == null && matchedCustomer == null)
                {
                    if (!string.IsNullOrEmpty(payeeName))
                    {
                        matchedSupplier = await _entityLookupService.FindSupplierByNameAsync(payeeName);
                        if (matchedSupplier != null)
                        {
                            paymentDirection = "Outgoing";
                            matchedByField = "Name";
                        }
                    }

                    if (matchedSupplier == null && !string.IsNullOrEmpty(payerName))
                    {
                        matchedCustomer = await _entityLookupService.FindCustomerByNameAsync(payerName);
                        if (matchedCustomer != null)
                        {
                            paymentDirection = "Incoming";
                            matchedByField = "Name";
                        }
                    }
                }

                // Get all suppliers and customers for dropdown
                var suppliers = await _entityLookupService.GetAllSuppliersAsync();
                var customers = await _entityLookupService.GetAllCustomersAsync();

                // Get unpaid invoices
                var invoices = await _context.Invoices
                    .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                // Extract BSP-specific fields from the payment
                var transferTo = !string.IsNullOrEmpty(extractedPayment.TransferTo)
                    ? extractedPayment.TransferTo
                    : (!string.IsNullOrEmpty(payeeName) && !string.IsNullOrEmpty(bankName)
                        ? $"{payeeName} {bankName}"
                        : payeeName ?? "");

                var accountType = extractedPayment.AccountType ??
                    (bankName?.ToUpper().Contains("BSP") == true ? "Internal" : "Domestic");

                var viewModel = new AiImportPaymentViewModel
                {
                    DocumentId = importedDocument.Id,
                    OriginalFileName = file.FileName,
                    PaymentDate = extractedPayment.PaymentDate,
                    Amount = extractedPayment.Amount,
                    PaymentMethod = extractedPayment.PaymentMethod ?? "Bank Transfer",
                    ReferenceNumber = extractedPayment.ReferenceNumber ?? "",
                    Notes = extractedPayment.Notes ?? "",
                    ExtractedBankName = bankName ?? "",
                    ExtractedBankAccountNumber = bankAccountNumber ?? "",
                    ExtractedPayerName = payerName ?? "",
                    ExtractedPayeeName = payeeName ?? "",
                    MatchedSupplierId = matchedSupplier?.Id,
                    MatchedSupplierName = matchedSupplier?.SupplierName,
                    MatchedCustomerId = matchedCustomer?.Id,
                    MatchedCustomerName = matchedCustomer?.CustomerName,
                    PaymentDirection = paymentDirection,
                    AvailableSuppliers = suppliers.ToList(),
                    AvailableCustomers = customers.ToList(),
                    AvailableInvoices = invoices,
                    MatchConfidence = matchedSupplier != null || matchedCustomer != null ? "High" : "None",
                    MatchedByField = matchedByField,
                    // BSP-specific fields
                    TransferTo = transferTo,
                    AccountType = accountType,
                    PayeeBranchNumber = extractedPayment.PayeeBranchNumber ?? "",
                    PayeeAccountNumber = extractedPayment.PayeeAccountNumber ?? "",
                    PayerAccountFull = extractedPayment.PayerBankAccountNumber ?? "",
                    PayerBranchNumber = extractedPayment.PayerBranchNumber ?? "",
                    PayerAccountNumber = extractedPayment.PayerAccountNumber ?? "",
                    Currency = extractedPayment.Currency ?? "PGK",
                    Purpose = extractedPayment.Purpose ?? "",
                    RelatedInvoiceNumber = ExtractInvoiceNumberFromNotes(extractedPayment.Notes ?? "")
                };

                // For AJAX requests, redirect to review page with document ID only (no TempData to avoid cookie size issues)
                if (isAjax)
                {
                    // Store extracted payment data in the ImportedDocument's ProcessingNotes field as JSON
                    var extractedJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        PaymentDate = viewModel.PaymentDate,
                        Amount = viewModel.Amount,
                        PaymentMethod = viewModel.PaymentMethod,
                        ReferenceNumber = viewModel.ReferenceNumber,
                        Notes = viewModel.Notes,
                        ExtractedBankName = viewModel.ExtractedBankName,
                        ExtractedBankAccountNumber = viewModel.ExtractedBankAccountNumber,
                        ExtractedPayerName = viewModel.ExtractedPayerName,
                        ExtractedPayeeName = viewModel.ExtractedPayeeName,
                        MatchedSupplierId = viewModel.MatchedSupplierId,
                        MatchedSupplierName = viewModel.MatchedSupplierName,
                        MatchedCustomerId = viewModel.MatchedCustomerId,
                        MatchedCustomerName = viewModel.MatchedCustomerName,
                        PaymentDirection = viewModel.PaymentDirection,
                        MatchConfidence = viewModel.MatchConfidence,
                        MatchedByField = viewModel.MatchedByField,
                        // BSP-specific fields
                        TransferTo = viewModel.TransferTo,
                        AccountType = viewModel.AccountType,
                        PayeeBranchNumber = viewModel.PayeeBranchNumber,
                        PayeeAccountNumber = viewModel.PayeeAccountNumber,
                        PayerAccountFull = viewModel.PayerAccountFull,
                        PayerBranchNumber = viewModel.PayerBranchNumber,
                        PayerAccountNumber = viewModel.PayerAccountNumber,
                        Currency = viewModel.Currency,
                        Purpose = viewModel.Purpose,
                        RelatedInvoiceNumber = viewModel.RelatedInvoiceNumber
                    });

                    // Update the document with extracted data
                    var doc = await _context.ImportedDocuments.FindAsync(viewModel.DocumentId);
                    if (doc != null)
                    {
                        doc.ProcessingNotes = extractedJson;
                        doc.ProcessingStatus = "Extracted";
                        await _context.SaveChangesAsync();
                    }

                    return Json(new
                    {
                        success = true,
                        redirectUrl = Url.Action("ReviewPaymentById", "AiImport", new { id = viewModel.DocumentId })
                    });
                }

                TempData["Success"] = "Payment data extracted successfully. Please review and confirm.";
                return View("ReviewPayment", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment with AI");
                if (isAjax)
                    return Json(new { success = false, message = $"Error processing document: {ex.Message}" });
                TempData["Error"] = $"Error processing document: {ex.Message}";
                return RedirectToAction(nameof(Payment));
            }
        }

        // GET: AiImport/ReviewPaymentFromJson - Review page for AJAX processing
        public async Task<IActionResult> ReviewPaymentFromJson()
        {
            if (!TempData.ContainsKey("PaymentData"))
            {
                TempData["Error"] = "No payment data found. Please upload a document first.";
                return RedirectToAction(nameof(Payment));
            }

            try
            {
                var json = TempData["PaymentData"]?.ToString();
                if (string.IsNullOrEmpty(json))
                {
                    TempData["Error"] = "No payment data found. Please upload a document first.";
                    return RedirectToAction(nameof(Payment));
                }

                // Parse the essential data
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                var viewModel = new AiImportPaymentViewModel
                {
                    DocumentId = root.GetProperty("DocumentId").GetInt32(),
                    OriginalFileName = root.GetProperty("OriginalFileName").GetString() ?? "",
                    PaymentDate = root.TryGetProperty("PaymentDate", out var pd) && pd.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? pd.GetDateTime() : DateTime.Now,
                    Amount = root.GetProperty("Amount").GetDecimal(),
                    PaymentMethod = root.GetProperty("PaymentMethod").GetString() ?? "Bank Transfer",
                    ReferenceNumber = root.GetProperty("ReferenceNumber").GetString() ?? "",
                    Notes = root.GetProperty("Notes").GetString() ?? "",
                    ExtractedBankName = root.GetProperty("ExtractedBankName").GetString() ?? "",
                    ExtractedBankAccountNumber = root.GetProperty("ExtractedBankAccountNumber").GetString() ?? "",
                    ExtractedPayerName = root.GetProperty("ExtractedPayerName").GetString() ?? "",
                    ExtractedPayeeName = root.GetProperty("ExtractedPayeeName").GetString() ?? "",
                    MatchedSupplierId = root.TryGetProperty("MatchedSupplierId", out var msi) && msi.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? msi.GetInt32() : null,
                    MatchedSupplierName = root.TryGetProperty("MatchedSupplierName", out var msn) && msn.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? msn.GetString() : null,
                    MatchedCustomerId = root.TryGetProperty("MatchedCustomerId", out var mci) && mci.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? mci.GetInt32() : null,
                    MatchedCustomerName = root.TryGetProperty("MatchedCustomerName", out var mcn) && mcn.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? mcn.GetString() : null,
                    PaymentDirection = root.GetProperty("PaymentDirection").GetString() ?? "Unknown",
                    MatchConfidence = root.GetProperty("MatchConfidence").GetString() ?? "None",
                    MatchedByField = root.TryGetProperty("MatchedByField", out var mbf) && mbf.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? mbf.GetString() : null
                };

                // Load the dropdown lists fresh from the database
                viewModel.AvailableSuppliers = (await _entityLookupService.GetAllSuppliersAsync()).ToList();
                viewModel.AvailableCustomers = (await _entityLookupService.GetAllCustomersAsync()).ToList();
                viewModel.AvailableInvoices = await _context.Invoices
                    .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                return View("ReviewPayment", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment review from JSON");
                TempData["Error"] = $"Error loading payment data: {ex.Message}";
                return RedirectToAction(nameof(Payment));
            }
        }

        // GET: AiImport/ReviewPaymentById/{id} - Review page loading from database
        public async Task<IActionResult> ReviewPaymentById(int id)
        {
            try
            {
                var document = await _context.ImportedDocuments.FindAsync(id);
                if (document == null)
                {
                    TempData["Error"] = "Document not found. Please upload a document first.";
                    return RedirectToAction(nameof(Payment));
                }

                if (string.IsNullOrEmpty(document.ProcessingNotes))
                {
                    TempData["Error"] = "No extracted data found for this document. Please re-upload.";
                    return RedirectToAction(nameof(Payment));
                }

                // Parse the JSON from ProcessingNotes
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(document.ProcessingNotes);
                var root = jsonDoc.RootElement;

                var viewModel = new AiImportPaymentViewModel
                {
                    DocumentId = document.Id,
                    OriginalFileName = document.OriginalFileName ?? "",
                    PaymentDate = root.TryGetProperty("PaymentDate", out var pd) && pd.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? pd.GetDateTime() : DateTime.Now,
                    Amount = root.TryGetProperty("Amount", out var amt) ? amt.GetDecimal() : 0,
                    PaymentMethod = root.TryGetProperty("PaymentMethod", out var pm) ? pm.GetString() ?? "Bank Transfer" : "Bank Transfer",
                    ReferenceNumber = root.TryGetProperty("ReferenceNumber", out var rn) ? rn.GetString() ?? "" : "",
                    Notes = root.TryGetProperty("Notes", out var notes) ? notes.GetString() ?? "" : "",
                    ExtractedBankName = root.TryGetProperty("ExtractedBankName", out var bn) ? bn.GetString() ?? "" : "",
                    ExtractedBankAccountNumber = root.TryGetProperty("ExtractedBankAccountNumber", out var ba) ? ba.GetString() ?? "" : "",
                    ExtractedPayerName = root.TryGetProperty("ExtractedPayerName", out var payer) ? payer.GetString() ?? "" : "",
                    ExtractedPayeeName = root.TryGetProperty("ExtractedPayeeName", out var payee) ? payee.GetString() ?? "" : "",
                    MatchedSupplierId = root.TryGetProperty("MatchedSupplierId", out var msi) && msi.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? msi.GetInt32() : null,
                    MatchedSupplierName = root.TryGetProperty("MatchedSupplierName", out var msn) && msn.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? msn.GetString() : null,
                    MatchedCustomerId = root.TryGetProperty("MatchedCustomerId", out var mci) && mci.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? mci.GetInt32() : null,
                    MatchedCustomerName = root.TryGetProperty("MatchedCustomerName", out var mcn) && mcn.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? mcn.GetString() : null,
                    PaymentDirection = root.TryGetProperty("PaymentDirection", out var dir) ? dir.GetString() ?? "Unknown" : "Unknown",
                    MatchConfidence = root.TryGetProperty("MatchConfidence", out var conf) ? conf.GetString() ?? "None" : "None",
                    MatchedByField = root.TryGetProperty("MatchedByField", out var mbf) && mbf.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? mbf.GetString() : null,
                    // BSP-specific fields
                    TransferTo = root.TryGetProperty("TransferTo", out var tt) ? tt.GetString() ?? "" : "",
                    AccountType = root.TryGetProperty("AccountType", out var at) ? at.GetString() ?? "" : "",
                    PayeeBranchNumber = root.TryGetProperty("PayeeBranchNumber", out var pbn) ? pbn.GetString() ?? "" : "",
                    PayeeAccountNumber = root.TryGetProperty("PayeeAccountNumber", out var pan) ? pan.GetString() ?? "" : "",
                    PayerAccountFull = root.TryGetProperty("PayerAccountFull", out var paf) ? paf.GetString() ?? "" : "",
                    PayerBranchNumber = root.TryGetProperty("PayerBranchNumber", out var prbn) ? prbn.GetString() ?? "" : "",
                    PayerAccountNumber = root.TryGetProperty("PayerAccountNumber", out var pran) ? pran.GetString() ?? "" : "",
                    Currency = root.TryGetProperty("Currency", out var cur) ? cur.GetString() ?? "PGK" : "PGK",
                    Purpose = root.TryGetProperty("Purpose", out var purp) ? purp.GetString() ?? "" : "",
                    RelatedInvoiceNumber = root.TryGetProperty("RelatedInvoiceNumber", out var rin) ? rin.GetString() ?? "" : ""
                };

                // Load the dropdown lists fresh from the database
                viewModel.AvailableSuppliers = (await _entityLookupService.GetAllSuppliersAsync()).ToList();
                viewModel.AvailableCustomers = (await _entityLookupService.GetAllCustomersAsync()).ToList();
                viewModel.AvailableInvoices = await _context.Invoices
                    .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                return View("ReviewPayment", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment review by ID {Id}", id);
                TempData["Error"] = $"Error loading payment data: {ex.Message}";
                return RedirectToAction(nameof(Payment));
            }
        }

        // POST: AiImport/SavePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePayment(AiImportPaymentViewModel model)
        {
            _logger.LogInformation("SavePayment called - DocumentId: {DocumentId}, Amount: {Amount}, Reference: {Reference}",
                model.DocumentId, model.Amount, model.ReferenceNumber);

            try
            {
                // Generate payment number based on count of existing payments
                var paymentCount = await _context.Payments.CountAsync();
                int nextNumber = paymentCount + 1;

                // Create the payment
                var payment = new Payment
                {
                    PaymentNumber = $"PAY-{nextNumber:D5}",
                    PaymentDate = model.PaymentDate ?? DateTime.Now,
                    Amount = model.Amount,
                    PaymentMethod = model.PaymentMethod,
                    ReferenceNumber = model.ReferenceNumber,
                    Notes = model.Notes,
                    BankName = model.ExtractedBankName,
                    BankAccountNumber = model.ExtractedBankAccountNumber,
                    PayerName = model.ExtractedPayerName,
                    PayeeName = model.ExtractedPayeeName,
                    Status = "Unallocated",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    // BSP-specific fields
                    TransferTo = model.TransferTo,
                    AccountType = model.AccountType,
                    PayeeBranchNumber = model.PayeeBranchNumber,
                    PayeeAccountNumber = model.PayeeAccountNumber,
                    PayerBranchNumber = model.PayerBranchNumber,
                    PayerAccountNumber = model.PayerAccountNumber,
                    PayerBankAccountNumber = model.PayerAccountFull,
                    Currency = model.Currency,
                    Purpose = model.Purpose
                };

                // Set supplier if selected
                if (model.SelectedSupplierId.HasValue)
                {
                    payment.SupplierId = model.SelectedSupplierId.Value;
                }
                else if (model.MatchedSupplierId.HasValue)
                {
                    payment.SupplierId = model.MatchedSupplierId.Value;
                }

                // Set customer if selected
                if (model.SelectedCustomerId.HasValue)
                {
                    payment.CustomerId = model.SelectedCustomerId.Value;
                }
                else if (model.MatchedCustomerId.HasValue)
                {
                    payment.CustomerId = model.MatchedCustomerId.Value;
                }

                // Create new supplier if requested
                if (model.CreateNewSupplier && !string.IsNullOrEmpty(model.ExtractedPayeeName))
                {
                    var newSupplier = await _entityLookupService.CreateSupplierFromPaymentDataAsync(
                        model.ExtractedPayeeName,
                        model.ExtractedBankAccountNumber,
                        model.ExtractedBankName
                    );
                    payment.SupplierId = newSupplier.Id;
                }

                // Create new customer if requested
                if (model.CreateNewCustomer && !string.IsNullOrEmpty(model.ExtractedPayerName))
                {
                    var newCustomer = await _entityLookupService.CreateCustomerFromPaymentDataAsync(
                        model.ExtractedPayerName,
                        model.ExtractedBankAccountNumber,
                        model.ExtractedBankName
                    );
                    payment.CustomerId = newCustomer.Id;
                }

                // Link to invoice if selected
                if (model.SelectedInvoiceId.HasValue)
                {
                    payment.InvoiceId = model.SelectedInvoiceId.Value;
                }

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment {PaymentId} ({PaymentNumber}) saved successfully", payment.Id, payment.PaymentNumber);

                // Link the document to the payment and update its notes with payment reference
                if (model.DocumentId > 0)
                {
                    _logger.LogInformation("Linking document {DocumentId} to payment {PaymentId}", model.DocumentId, payment.Id);
                    await _documentService.LinkDocumentToPaymentAsync(model.DocumentId, payment.Id);

                    // Update document processing notes with payment reference
                    var doc = await _context.ImportedDocuments.FindAsync(model.DocumentId);
                    if (doc != null)
                    {
                        doc.ProcessingNotes = $"AI Imported - Payment: {payment.PaymentNumber}, Reference: {payment.ReferenceNumber}, Amount: {payment.Amount:N2}";
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Linked document {DocumentId} to payment {PaymentId} ({PaymentNumber})",
                        model.DocumentId, payment.Id, payment.PaymentNumber);
                }
                else
                {
                    _logger.LogWarning("No document to link - DocumentId was 0 for payment {PaymentNumber}", payment.PaymentNumber);
                }

                TempData["Success"] = $"Payment {payment.PaymentNumber} imported successfully!";
                _logger.LogInformation("Redirecting to Payment Details for payment {PaymentId}", payment.Id);
                return RedirectToAction("Details", "Payments", new { id = payment.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving imported payment. Model: DocumentId={DocumentId}, Amount={Amount}",
                    model.DocumentId, model.Amount);
                TempData["Error"] = $"Error saving payment: {ex.Message}";

                // Reload dropdown lists for the view
                model.AvailableSuppliers = (await _entityLookupService.GetAllSuppliersAsync()).ToList();
                model.AvailableCustomers = (await _entityLookupService.GetAllCustomersAsync()).ToList();
                model.AvailableInvoices = await _context.Invoices
                    .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                return View("ReviewPayment", model);
            }
        }

        // GET: AiImport/Batch
        public IActionResult Batch()
        {
            return View();
        }

        // POST: AiImport/ProcessBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBatch(List<IFormFile> files, string documentType)
        {
            if (files == null || files.Count == 0)
            {
                TempData["Error"] = "Please select files to upload.";
                return RedirectToAction(nameof(Batch));
            }

            if (string.IsNullOrEmpty(documentType) || (documentType != "Invoice" && documentType != "Payment"))
            {
                TempData["Error"] = "Please select a valid document type.";
                return RedirectToAction(nameof(Batch));
            }

            var results = new AiImportBatchViewModel
            {
                DocumentType = documentType,
                TotalFiles = files.Count,
                AvailableSuppliers = (await _entityLookupService.GetAllSuppliersAsync()).ToList(),
                AvailableCustomers = (await _entityLookupService.GetAllCustomersAsync()).ToList()
            };

            foreach (var file in files)
            {
                try
                {
                    // Validate file
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!_allowedExtensions.Contains(extension))
                    {
                        results.FailedCount++;
                        results.FailedFiles.Add(file.FileName);
                        continue;
                    }

                    if (file.Length > _maxFileSize)
                    {
                        results.FailedCount++;
                        results.FailedFiles.Add(file.FileName);
                        continue;
                    }

                    // Store the document
                    var importedDocument = await _documentService.StoreDocumentAsync(
                        file,
                        documentType,
                        User.Identity?.Name ?? "System"
                    );

                    if (documentType == "Invoice")
                    {
                        Invoice? extractedData;
                        using (var stream = file.OpenReadStream())
                        {
                            extractedData = await _aiService.ExtractInvoiceFromFileAsync(stream, file.FileName);
                        }

                        if (extractedData != null)
                        {
                            // Try to match entities
                            Supplier? matchedSupplier = null;
                            Customer? matchedCustomer = null;

                            if (extractedData.Supplier != null && !string.IsNullOrEmpty(extractedData.Supplier.SupplierName))
                            {
                                matchedSupplier = await _entityLookupService.FindSupplierByNameAsync(extractedData.Supplier.SupplierName);
                            }
                            if (!string.IsNullOrEmpty(extractedData.CustomerName))
                            {
                                matchedCustomer = await _entityLookupService.FindCustomerByNameAsync(extractedData.CustomerName);
                            }

                            results.ExtractedInvoices.Add(new AiImportInvoiceViewModel
                            {
                                DocumentId = importedDocument.Id,
                                OriginalFileName = file.FileName,
                                InvoiceNumber = extractedData.InvoiceNumber ?? "",
                                InvoiceDate = extractedData.InvoiceDate,
                                DueDate = extractedData.DueDate,
                                CustomerName = extractedData.CustomerName ?? "",
                                CustomerAddress = extractedData.CustomerAddress ?? "",
                                TotalAmount = extractedData.TotalAmount,
                                InvoiceType = extractedData.InvoiceType ?? "Payable",
                                MatchedSupplierId = matchedSupplier?.Id,
                                MatchedSupplierName = matchedSupplier?.SupplierName,
                                ExtractedSupplierName = extractedData.Supplier?.SupplierName ?? "",
                                MatchedCustomerId = matchedCustomer?.Id,
                                MatchedCustomerName = matchedCustomer?.CustomerName,
                                ExtractedCustomerName = extractedData.CustomerName ?? "",
                                MatchConfidence = matchedSupplier != null || matchedCustomer != null ? "High" : "None"
                            });

                            results.SuccessCount++;
                        }
                        else
                        {
                            await _documentService.UpdateDocumentExtractedDataAsync(
                                importedDocument.Id, null, null, null, null, null
                            );
                            results.FailedCount++;
                            results.FailedFiles.Add(file.FileName);
                        }
                    }
                    else // Payment
                    {
                        Payment? extractedData;
                        using (var stream = file.OpenReadStream())
                        {
                            extractedData = await _aiService.ExtractPaymentFromFileAsync(stream, file.FileName);
                        }

                        if (extractedData != null)
                        {
                            // Try to match by bank account
                            Supplier? matchedSupplier = null;
                            Customer? matchedCustomer = null;
                            string paymentDirection = "Unknown";
                            string? matchedByField = null;

                            if (!string.IsNullOrEmpty(extractedData.BankAccountNumber))
                            {
                                matchedSupplier = await _entityLookupService.FindSupplierByAccountNumberAsync(extractedData.BankAccountNumber);
                                if (matchedSupplier != null)
                                {
                                    paymentDirection = "Outgoing";
                                    matchedByField = "Bank Account";
                                }
                                else
                                {
                                    matchedCustomer = await _entityLookupService.FindCustomerByAccountNumberAsync(extractedData.BankAccountNumber);
                                    if (matchedCustomer != null)
                                    {
                                        paymentDirection = "Incoming";
                                        matchedByField = "Bank Account";
                                    }
                                }
                            }

                            results.ExtractedPayments.Add(new AiImportPaymentViewModel
                            {
                                DocumentId = importedDocument.Id,
                                OriginalFileName = file.FileName,
                                PaymentDate = extractedData.PaymentDate,
                                Amount = extractedData.Amount,
                                PaymentMethod = extractedData.PaymentMethod ?? "Bank Transfer",
                                ReferenceNumber = extractedData.ReferenceNumber ?? "",
                                ExtractedBankName = extractedData.BankName ?? "",
                                ExtractedBankAccountNumber = extractedData.BankAccountNumber ?? "",
                                ExtractedPayerName = extractedData.PayerName ?? "",
                                ExtractedPayeeName = extractedData.PayeeName ?? "",
                                MatchedSupplierId = matchedSupplier?.Id,
                                MatchedSupplierName = matchedSupplier?.SupplierName,
                                MatchedCustomerId = matchedCustomer?.Id,
                                MatchedCustomerName = matchedCustomer?.CustomerName,
                                PaymentDirection = paymentDirection,
                                MatchConfidence = matchedSupplier != null || matchedCustomer != null ? "High" : "None",
                                MatchedByField = matchedByField
                            });

                            results.SuccessCount++;
                        }
                        else
                        {
                            await _documentService.UpdateDocumentExtractedDataAsync(
                                importedDocument.Id, null, null, null, null, null
                            );
                            results.FailedCount++;
                            results.FailedFiles.Add(file.FileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {FileName} in batch", file.FileName);
                    results.FailedCount++;
                    results.FailedFiles.Add(file.FileName);
                }
            }

            if (results.SuccessCount == 0)
            {
                TempData["Error"] = "No files could be processed successfully.";
                return RedirectToAction(nameof(Batch));
            }

            TempData["Success"] = $"Processed {results.SuccessCount} of {results.TotalFiles} files successfully.";
            return View("ReviewBatch", results);
        }

        // GET: AiImport/Documents
        public async Task<IActionResult> Documents()
        {
            var documents = await _context.ImportedDocuments
                .Include(d => d.Invoice)
                .Include(d => d.Payment)
                .OrderByDescending(d => d.UploadDate)
                .Take(100)
                .ToListAsync();

            // Load suppliers for dropdown in edit modal
            ViewBag.Suppliers = await _context.Suppliers
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            return View(documents);
        }

        // GET: AiImport/DownloadDocument/5
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var content = await _documentService.GetDocumentContentAsync(id);

            if (content == null)
            {
                return NotFound();
            }

            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            return File(content, document.ContentType ?? "application/octet-stream", document.OriginalFileName ?? "document");
        }

        // GET: AiImport/PreviewDocument/5
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var content = await _documentService.GetDocumentContentAsync(id);

            if (content == null)
            {
                return NotFound();
            }

            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            // Return file inline for preview (Content-Disposition: inline)
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{document.OriginalFileName ?? "document"}\"";
            return File(content, document.ContentType ?? "application/octet-stream");
        }

        // GET: AiImport/ViewDocument/5
        public async Task<IActionResult> ViewDocument(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }

        // POST: AiImport/UpdateDocumentSupplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDocumentSupplier(string documentIds, string supplierName)
        {
            if (string.IsNullOrWhiteSpace(documentIds) || string.IsNullOrWhiteSpace(supplierName))
            {
                TempData["Error"] = "Document IDs and supplier name are required.";
                return RedirectToAction(nameof(Documents));
            }

            try
            {
                var ids = documentIds.Split(',').Select(int.Parse).ToList();
                var documents = await _context.ImportedDocuments
                    .Where(d => ids.Contains(d.Id))
                    .ToListAsync();

                foreach (var doc in documents)
                {
                    doc.ExtractedSupplierName = supplierName.Trim();
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Supplier name updated to '{supplierName}' for {documents.Count} document(s).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier name for documents: {DocumentIds}", documentIds);
                TempData["Error"] = "An error occurred while updating the supplier name.";
            }

            return RedirectToAction(nameof(Documents));
        }

        // POST: AiImport/DeleteDocument/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, string? returnUrl)
        {
            await _documentService.DeleteDocumentAsync(id);
            TempData["Success"] = "Document deleted successfully.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Documents));
        }

        // POST: AiImport/UploadDocumentToInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocumentToInvoice(int invoiceId, IFormFile file, string? notes)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("Details", "Invoices", new { id = invoiceId });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                TempData["Error"] = $"File type {extension} is not supported. Allowed types: PDF, PNG, JPG, JPEG, GIF, WEBP";
                return RedirectToAction("Details", "Invoices", new { id = invoiceId });
            }

            if (file.Length > _maxFileSize)
            {
                TempData["Error"] = "File size exceeds the 500MB limit.";
                return RedirectToAction("Details", "Invoices", new { id = invoiceId });
            }

            try
            {
                var document = await _documentService.StoreDocumentAsync(file, "Invoice", null);

                // Link to invoice
                document.InvoiceId = invoiceId;
                document.ProcessingStatus = "Uploaded";
                document.ProcessingNotes = notes;
                document.ProcessedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Document '{file.FileName}' uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document to invoice {InvoiceId}", invoiceId);
                TempData["Error"] = "Failed to upload document. Please try again.";
            }

            return RedirectToAction("Details", "Invoices", new { id = invoiceId });
        }

        // POST: AiImport/UploadDocumentToPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocumentToPayment(int paymentId, IFormFile file, string? notes)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("Details", "Payments", new { id = paymentId });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                TempData["Error"] = $"File type {extension} is not supported. Allowed types: PDF, PNG, JPG, JPEG, GIF, WEBP";
                return RedirectToAction("Details", "Payments", new { id = paymentId });
            }

            if (file.Length > _maxFileSize)
            {
                TempData["Error"] = "File size exceeds the 500MB limit.";
                return RedirectToAction("Details", "Payments", new { id = paymentId });
            }

            try
            {
                var document = await _documentService.StoreDocumentAsync(file, "Payment", null);

                // Link to payment
                document.PaymentId = paymentId;
                document.ProcessingStatus = "Uploaded";
                document.ProcessingNotes = notes;
                document.ProcessedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Document '{file.FileName}' uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document to payment {PaymentId}", paymentId);
                TempData["Error"] = "Failed to upload document. Please try again.";
            }

            return RedirectToAction("Details", "Payments", new { id = paymentId });
        }

        /// <summary>
        /// Extracts invoice numbers from the payment notes field.
        /// Looks for common patterns like "INV-12345", "Invoice 12345", etc.
        /// </summary>
        private static string ExtractInvoiceNumberFromNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return "";

            // Common invoice number patterns
            var patterns = new[]
            {
                @"INV[-#]?\d+",                   // INV-12345, INV#12345, INV12345
                @"Invoice\s*[-#]?\s*\d+",         // Invoice 12345, Invoice-12345
                @"INVOICE\s*[-#]?\s*\d+",         // INVOICE 12345
                @"Inv\s*[-#]?\s*\d+",             // Inv 12345
                @"#\s*\d{4,}",                    // #12345
                @"\b\d{6,}\b"                     // 6+ digit number (common invoice number)
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(notes, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Value.Trim();
                }
            }

            return "";
        }
    }
}
