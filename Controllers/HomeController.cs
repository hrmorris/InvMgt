using Microsoft.AspNetCore.Mvc;
using InvoiceManagement.Services;
using InvoiceManagement.Models;

namespace InvoiceManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPaymentService _paymentService;
        private readonly IRequisitionService _requisitionService;
        private readonly IPurchaseOrderService _purchaseOrderService;

        public HomeController(
            IInvoiceService invoiceService, 
            IPaymentService paymentService,
            IRequisitionService requisitionService,
            IPurchaseOrderService purchaseOrderService)
        {
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _requisitionService = requisitionService;
            _purchaseOrderService = purchaseOrderService;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get user info from session
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("Role");

            // Get invoices and payments data
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var payments = await _paymentService.GetAllPaymentsAsync();

            // Invoice statistics
            ViewBag.TotalInvoices = invoices.Count();
            ViewBag.TotalAmount = invoices.Sum(i => i.TotalAmount);
            ViewBag.TotalPaid = invoices.Sum(i => i.PaidAmount);
            ViewBag.TotalBalance = invoices.Sum(i => i.BalanceAmount);
            ViewBag.UnpaidInvoices = invoices.Count(i => i.Status == "Unpaid");
            ViewBag.PartiallyPaidInvoices = invoices.Count(i => i.Status == "Partially Paid");
            ViewBag.PaidInvoices = invoices.Count(i => i.Status == "Paid");
            ViewBag.OverdueInvoices = invoices.Count(i => i.Status == "Overdue");

            // Payment statistics
            ViewBag.TotalPayments = payments.Count();
            ViewBag.TotalPaymentAmount = payments.Sum(p => p.Amount);
            ViewBag.UnallocatedPayments = payments.Count(p => p.Status == "Unallocated");
            ViewBag.UnallocatedAmount = payments.Where(p => p.Status == "Unallocated").Sum(p => p.Amount);

            // Recent data
            ViewBag.RecentInvoices = invoices.OrderByDescending(i => i.InvoiceDate).Take(5);
            ViewBag.RecentPayments = payments.OrderByDescending(p => p.PaymentDate).Take(5);

            // Get procurement data
            var requisitions = await _requisitionService.GetAllRequisitionsAsync();
            var purchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();

            ViewBag.TotalRequisitions = requisitions.Count();
            ViewBag.PendingRequisitions = requisitions.Count(r => r.Status.Contains("Pending"));
            ViewBag.TotalPurchaseOrders = purchaseOrders.Count();
            ViewBag.PendingPurchaseOrders = purchaseOrders.Count(po => po.Status == "Pending");

            return View();
        }
    }
}

