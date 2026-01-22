using Microsoft.AspNetCore.Mvc;
using InvoiceManagement.Models;
using InvoiceManagement.Services;
using InvoiceManagement.Authorization;

namespace InvoiceManagement.Controllers
{
    [Authorize]
    public class RequisitionsController : Controller
    {
        private readonly IRequisitionService _requisitionService;
        private readonly IPdfService _pdfService;

        public RequisitionsController(IRequisitionService requisitionService, IPdfService pdfService)
        {
            _requisitionService = requisitionService;
            _pdfService = pdfService;
        }

        // GET: Requisitions
        public async Task<IActionResult> Index(string? status)
        {
            IEnumerable<Requisition> requisitions;

            if (!string.IsNullOrEmpty(status))
                requisitions = await _requisitionService.GetRequisitionsByStatusAsync(status);
            else
                requisitions = await _requisitionService.GetAllRequisitionsAsync();

            ViewBag.Status = status;
            return View(requisitions);
        }

        // GET: Requisitions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var requisition = await _requisitionService.GetRequisitionByIdAsync(id);
            if (requisition == null)
                return NotFound();

            return View(requisition);
        }

        // GET: Requisitions/Create
        public IActionResult Create()
        {
            var requisition = new Requisition
            {
                RequisitionDate = DateTime.Now,
                RequisitionNumber = $"REQ-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}"
            };
            return View(requisition);
        }

        // POST: Requisitions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Requisition requisition, List<RequisitionItem> items)
        {
            ModelState.Remove("RequisitionItems");

            if (items != null)
            {
                items = items.Where(i => !string.IsNullOrWhiteSpace(i.ItemDescription)).ToList();
            }
            else
            {
                items = new List<RequisitionItem>();
            }

            if (!items.Any())
            {
                ModelState.AddModelError("", "Please add at least one item to the requisition.");
                return View(requisition);
            }

            if (ModelState.IsValid)
            {
                requisition.RequisitionItems = items;
                requisition.EstimatedAmount = items.Sum(i => i.EstimatedTotal);
                requisition.Status = "Pending_Supervisor";

                await _requisitionService.CreateRequisitionAsync(requisition);
                TempData["SuccessMessage"] = $"Requisition {requisition.RequisitionNumber} created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(requisition);
        }

        // GET: Requisitions/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var requisition = await _requisitionService.GetRequisitionByIdAsync(id);
            if (requisition == null)
                return NotFound();

            // Admin users can edit any requisition, others can only edit pending/rejected/draft
            var userRole = HttpContext.Session.GetString("UserRole");
            var isAdmin = userRole == "Admin" || userRole == "SystemAdmin";

            if (!isAdmin && requisition.Status != "Pending_Supervisor" && requisition.Status != "Rejected" && requisition.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Only draft, pending or rejected requisitions can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(requisition);
        }

        // POST: Requisitions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Requisition requisition, List<RequisitionItem> items)
        {
            if (id != requisition.Id)
                return NotFound();

            ModelState.Remove("RequisitionItems");

            if (items != null)
            {
                items = items.Where(i => !string.IsNullOrWhiteSpace(i.ItemDescription)).ToList();
            }
            else
            {
                items = new List<RequisitionItem>();
            }

            if (!items.Any())
            {
                ModelState.AddModelError("", "Please add at least one item to the requisition.");
                return View(requisition);
            }

            if (ModelState.IsValid)
            {
                requisition.RequisitionItems = items;
                requisition.EstimatedAmount = items.Sum(i => i.EstimatedTotal);
                requisition.ModifiedDate = DateTime.Now;

                await _requisitionService.UpdateRequisitionAsync(requisition);
                TempData["SuccessMessage"] = $"Requisition {requisition.RequisitionNumber} updated successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(requisition);
        }

        // GET: Requisitions/Approve/5
        public async Task<IActionResult> Approve(int id)
        {
            var requisition = await _requisitionService.GetRequisitionByIdAsync(id);
            if (requisition == null)
                return NotFound();

            return View(requisition);
        }

        // POST: Requisitions/ApproveBySupervisor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBySupervisor(int id, string supervisorName, string? comments)
        {
            await _requisitionService.ApproveBySupervisorAsync(id, supervisorName, comments);
            TempData["SuccessMessage"] = "Requisition approved by supervisor!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Requisitions/ApproveByFinance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveByFinance(int id, string financeName, bool budgetOk, bool needOk, bool costCodeOk, string? comments)
        {
            await _requisitionService.ApproveByFinanceAsync(id, financeName, budgetOk, needOk, costCodeOk, comments);
            TempData["SuccessMessage"] = "Requisition screened by finance!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Requisitions/ApproveByManager
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveByManager(int id, string approverName, string? comments)
        {
            await _requisitionService.ApproveByFinalApproverAsync(id, approverName, comments);
            TempData["SuccessMessage"] = "Requisition approved by manager!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Requisitions/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            await _requisitionService.RejectRequisitionAsync(id, rejectionReason);
            TempData["SuccessMessage"] = "Requisition rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Requisitions/PendingApprovals
        public async Task<IActionResult> PendingApprovals(string role = "supervisor")
        {
            var requisitions = await _requisitionService.GetPendingApprovalsAsync(role);
            ViewBag.Role = role;
            return View(requisitions);
        }

        // GET: Requisitions/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var requisition = await _requisitionService.GetRequisitionByIdAsync(id);
            if (requisition == null)
                return NotFound();

            // Admin users can delete any requisition, others can only delete draft/rejected/pending
            var userRole = HttpContext.Session.GetString("UserRole");
            var isAdmin = userRole == "Admin" || userRole == "SystemAdmin";

            if (!isAdmin && requisition.Status != "Draft" && requisition.Status != "Rejected" && requisition.Status != "Pending_Supervisor")
            {
                TempData["ErrorMessage"] = "Only draft, rejected, or pending requisitions can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(requisition);
        }

        // POST: Requisitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var requisition = await _requisitionService.GetRequisitionByIdAsync(id);
            if (requisition == null)
                return NotFound();

            await _requisitionService.DeleteRequisitionAsync(id);
            TempData["SuccessMessage"] = $"Requisition {requisition.RequisitionNumber} deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Requisitions/DownloadPdf/5
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var requisition = await _requisitionService.GetRequisitionByIdAsync(id);
            if (requisition == null)
                return NotFound();

            var pdfBytes = await _pdfService.GenerateRequisitionPdfAsync(requisition);
            return File(pdfBytes, "application/pdf", $"Requisition_{requisition.RequisitionNumber}.pdf");
        }
    }
}

