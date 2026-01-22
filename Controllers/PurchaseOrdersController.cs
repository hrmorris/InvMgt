using Microsoft.AspNetCore.Mvc;
using InvoiceManagement.Models;
using InvoiceManagement.Services;
using InvoiceManagement.Authorization;

namespace InvoiceManagement.Controllers
{
    [Authorize]
    public class PurchaseOrdersController : Controller
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly ISupplierService _supplierService;
        private readonly IRequisitionService _requisitionService;
        private readonly IPdfService _pdfService;

        public PurchaseOrdersController(
            IPurchaseOrderService purchaseOrderService,
            ISupplierService supplierService,
            IRequisitionService requisitionService,
            IPdfService pdfService)
        {
            _purchaseOrderService = purchaseOrderService;
            _supplierService = supplierService;
            _requisitionService = requisitionService;
            _pdfService = pdfService;
        }

        // GET: PurchaseOrders
        public async Task<IActionResult> Index(string? status)
        {
            IEnumerable<PurchaseOrder> purchaseOrders;

            if (!string.IsNullOrEmpty(status))
                purchaseOrders = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(status);
            else
                purchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();

            ViewBag.Status = status;
            return View(purchaseOrders);
        }

        // GET: PurchaseOrders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var po = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            if (po == null)
                return NotFound();

            return View(po);
        }

        // GET: PurchaseOrders/Create
        public async Task<IActionResult> Create()
        {
            var suppliers = await _supplierService.GetActiveSuppliersAsync();
            ViewBag.Suppliers = suppliers;

            var po = new PurchaseOrder
            {
                PODate = DateTime.Now,
                PONumber = $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                Status = "Draft"
            };

            return View(po);
        }

        // POST: PurchaseOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrder purchaseOrder, List<PurchaseOrderItem> items)
        {
            ModelState.Remove("PurchaseOrderItems");
            ModelState.Remove("Supplier");
            ModelState.Remove("Requisition");

            if (items != null)
            {
                items = items.Where(i => !string.IsNullOrWhiteSpace(i.ItemDescription)).ToList();
            }
            else
            {
                items = new List<PurchaseOrderItem>();
            }

            if (!items.Any())
            {
                ModelState.AddModelError("", "Please add at least one item to the purchase order.");
                var suppliers = await _supplierService.GetActiveSuppliersAsync();
                ViewBag.Suppliers = suppliers;
                return View(purchaseOrder);
            }

            if (ModelState.IsValid)
            {
                purchaseOrder.PurchaseOrderItems = items;
                purchaseOrder.TotalAmount = items.Sum(i => i.TotalPrice);
                purchaseOrder.CreatedDate = DateTime.Now;
                purchaseOrder.Status = "Draft";

                await _purchaseOrderService.CreatePurchaseOrderAsync(purchaseOrder);
                TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.PONumber} created successfully!";
                return RedirectToAction(nameof(Details), new { id = purchaseOrder.Id });
            }

            var supplierList = await _supplierService.GetActiveSuppliersAsync();
            ViewBag.Suppliers = supplierList;
            return View(purchaseOrder);
        }

        // GET: PurchaseOrders/CreateFromRequisition/5
        public async Task<IActionResult> CreateFromRequisition(int requisitionId)
        {
            var requisition = await _requisitionService.GetRequisitionByIdAsync(requisitionId);
            if (requisition == null || requisition.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Requisition must be approved before creating a Purchase Order.";
                return RedirectToAction("Index", "Requisitions");
            }

            var suppliers = await _supplierService.GetActiveSuppliersAsync();
            ViewBag.Suppliers = suppliers;
            ViewBag.Requisition = requisition;

            return View();
        }

        // POST: PurchaseOrders/CreateFromRequisition
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromRequisition(int requisitionId, int supplierId)
        {
            try
            {
                var po = await _purchaseOrderService.CreateFromRequisitionAsync(requisitionId, supplierId);
                TempData["SuccessMessage"] = $"Purchase Order {po.PONumber} created successfully!";
                return RedirectToAction(nameof(Details), new { id = po.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CreateFromRequisition), new { requisitionId });
            }
        }

        // GET: PurchaseOrders/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var po = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            if (po == null)
                return NotFound();

            // Admin users can edit any purchase order, others can only edit draft/pending
            var userRole = HttpContext.Session.GetString("UserRole");
            var isAdmin = userRole == "Admin" || userRole == "SystemAdmin";

            if (!isAdmin && po.Status != "Draft" && po.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only draft or pending purchase orders can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var suppliers = await _supplierService.GetActiveSuppliersAsync();
            ViewBag.Suppliers = suppliers;
            return View(po);
        }

        // POST: PurchaseOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrder purchaseOrder)
        {
            if (id != purchaseOrder.Id)
                return NotFound();

            ModelState.Remove("PurchaseOrderItems");
            ModelState.Remove("Supplier");
            ModelState.Remove("Requisition");

            if (ModelState.IsValid)
            {
                purchaseOrder.ModifiedDate = DateTime.Now;
                await _purchaseOrderService.UpdatePurchaseOrderAsync(purchaseOrder);
                TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.PONumber} updated successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }

            var suppliers = await _supplierService.GetActiveSuppliersAsync();
            ViewBag.Suppliers = suppliers;
            return View(purchaseOrder);
        }

        // GET: PurchaseOrders/ReceiveGoods/5
        public async Task<IActionResult> ReceiveGoods(int id)
        {
            var po = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            if (po == null)
                return NotFound();

            return View(po);
        }

        // POST: PurchaseOrders/ReceiveGoods
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveGoods(int id, Dictionary<int, int> quantities)
        {
            await _purchaseOrderService.MarkItemsReceivedAsync(id, quantities);
            TempData["SuccessMessage"] = "Goods receipt recorded successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: PurchaseOrders/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var po = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            if (po == null)
                return NotFound();

            // Admin users can delete any purchase order, others can only delete draft/pending
            var userRole = HttpContext.Session.GetString("UserRole");
            var isAdmin = userRole == "Admin" || userRole == "SystemAdmin";

            if (!isAdmin && po.Status != "Draft" && po.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only draft or pending purchase orders can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(po);
        }

        // POST: PurchaseOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var po = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            if (po == null)
                return NotFound();

            await _purchaseOrderService.DeletePurchaseOrderAsync(id);
            TempData["SuccessMessage"] = $"Purchase Order {po.PONumber} deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: PurchaseOrders/DownloadPdf/5
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var po = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            if (po == null)
                return NotFound();

            var pdfBytes = await _pdfService.GeneratePurchaseOrderPdfAsync(po);
            return File(pdfBytes, "application/pdf", $"PurchaseOrder_{po.PONumber}.pdf");
        }
    }
}

