using Microsoft.AspNetCore.Mvc;
using InvoiceManagement.Services;

namespace InvoiceManagement.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPaymentService _paymentService;
        private readonly IPdfService _pdfService;
        private readonly ISupplierService _supplierService;

        public ReportsController(IInvoiceService invoiceService, IPaymentService paymentService, IPdfService pdfService, ISupplierService supplierService)
        {
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _pdfService = pdfService;
            _supplierService = supplierService;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/InvoiceReport
        public IActionResult InvoiceReport()
        {
            return View();
        }

        // POST: Reports/GenerateInvoiceReport
        [HttpPost]
        public async Task<IActionResult> GenerateInvoiceReport(DateTime? startDate, DateTime? endDate)
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();

            if (startDate.HasValue)
            {
                invoices = invoices.Where(i => i.InvoiceDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                invoices = invoices.Where(i => i.InvoiceDate <= endDate.Value);
            }

            var pdfBytes = await _pdfService.GenerateInvoiceReportPdfAsync(invoices, startDate, endDate);
            return File(pdfBytes, "application/pdf", $"InvoiceReport_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // GET: Reports/PaymentReport
        public IActionResult PaymentReport()
        {
            return View();
        }

        // POST: Reports/GeneratePaymentReport
        [HttpPost]
        public async Task<IActionResult> GeneratePaymentReport(DateTime? startDate, DateTime? endDate)
        {
            var payments = await _paymentService.GetAllPaymentsAsync();

            if (startDate.HasValue)
            {
                payments = payments.Where(p => p.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                payments = payments.Where(p => p.PaymentDate <= endDate.Value);
            }

            var pdfBytes = await _pdfService.GeneratePaymentReportPdfAsync(payments, startDate, endDate);
            return File(pdfBytes, "application/pdf", $"PaymentReport_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // GET: Reports/SupplierInvoiceList
        public async Task<IActionResult> SupplierInvoiceList(DateTime? startDate, DateTime? endDate, int? supplierId, string? status)
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();

            // Filter to only supplier invoices
            invoices = invoices.Where(i => i.InvoiceType == "Supplier" || i.SupplierId.HasValue);

            if (startDate.HasValue)
            {
                invoices = invoices.Where(i => i.InvoiceDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                invoices = invoices.Where(i => i.InvoiceDate <= endDate.Value);
            }

            if (supplierId.HasValue && supplierId > 0)
            {
                invoices = invoices.Where(i => i.SupplierId == supplierId.Value);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                invoices = invoices.Where(i => i.Status == status);
            }

            // Get suppliers for filter dropdown
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Suppliers = suppliers.OrderBy(s => s.SupplierName).ToList();
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SelectedSupplierId = supplierId;
            ViewBag.SelectedStatus = status;

            // Calculate summary
            var invoiceList = invoices.OrderByDescending(i => i.InvoiceDate).ToList();
            ViewBag.TotalInvoices = invoiceList.Count;
            ViewBag.TotalAmount = invoiceList.Sum(i => i.TotalAmount);
            ViewBag.TotalPaid = invoiceList.Sum(i => i.PaidAmount);
            ViewBag.TotalBalance = invoiceList.Sum(i => i.BalanceAmount);

            return View(invoiceList);
        }

        // POST: Reports/DownloadSupplierInvoiceListPdf
        [HttpPost]
        public async Task<IActionResult> DownloadSupplierInvoiceListPdf(DateTime? startDate, DateTime? endDate, int? supplierId, string? status)
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();

            // Filter to only supplier invoices
            invoices = invoices.Where(i => i.InvoiceType == "Supplier" || i.SupplierId.HasValue);

            if (startDate.HasValue)
            {
                invoices = invoices.Where(i => i.InvoiceDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                invoices = invoices.Where(i => i.InvoiceDate <= endDate.Value);
            }

            if (supplierId.HasValue && supplierId > 0)
            {
                invoices = invoices.Where(i => i.SupplierId == supplierId.Value);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                invoices = invoices.Where(i => i.Status == status);
            }

            var invoiceList = invoices.OrderByDescending(i => i.InvoiceDate).ToList();
            var pdfBytes = await _pdfService.GenerateSupplierInvoiceListPdfAsync(invoiceList, startDate, endDate);
            return File(pdfBytes, "application/pdf", $"SupplierInvoiceList_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // GET: Reports/PaymentsList
        public async Task<IActionResult> PaymentsList(DateTime? startDate, DateTime? endDate, string? paymentMethod, string? status)
        {
            var payments = await _paymentService.GetAllPaymentsAsync();

            if (startDate.HasValue)
            {
                payments = payments.Where(p => p.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                payments = payments.Where(p => p.PaymentDate <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod != "All")
            {
                payments = payments.Where(p => p.PaymentMethod == paymentMethod);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                payments = payments.Where(p => p.Status == status);
            }

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SelectedPaymentMethod = paymentMethod;
            ViewBag.SelectedStatus = status;

            // Calculate summary
            var paymentList = payments.OrderByDescending(p => p.PaymentDate).ToList();
            ViewBag.TotalPayments = paymentList.Count;
            ViewBag.TotalAmount = paymentList.Sum(p => p.Amount);
            ViewBag.TotalAllocated = paymentList.Sum(p => p.AllocatedAmount);
            ViewBag.TotalUnallocated = paymentList.Sum(p => p.UnallocatedAmount);

            return View(paymentList);
        }

        // POST: Reports/DownloadPaymentsListPdf
        [HttpPost]
        public async Task<IActionResult> DownloadPaymentsListPdf(DateTime? startDate, DateTime? endDate, string? paymentMethod, string? status)
        {
            var payments = await _paymentService.GetAllPaymentsAsync();

            if (startDate.HasValue)
            {
                payments = payments.Where(p => p.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                payments = payments.Where(p => p.PaymentDate <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod != "All")
            {
                payments = payments.Where(p => p.PaymentMethod == paymentMethod);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                payments = payments.Where(p => p.Status == status);
            }

            var paymentList = payments.OrderByDescending(p => p.PaymentDate).ToList();
            var pdfBytes = await _pdfService.GeneratePaymentsListPdfAsync(paymentList, startDate, endDate);
            return File(pdfBytes, "application/pdf", $"PaymentsList_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}

