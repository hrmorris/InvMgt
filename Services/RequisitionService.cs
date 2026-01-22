using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    public class RequisitionService : IRequisitionService
    {
        private readonly ApplicationDbContext _context;

        public RequisitionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Requisition>> GetAllRequisitionsAsync()
        {
            return await _context.Requisitions
                .Include(r => r.RequisitionItems)
                .OrderByDescending(r => r.RequisitionDate)
                .ToListAsync();
        }

        public async Task<Requisition?> GetRequisitionByIdAsync(int id)
        {
            return await _context.Requisitions
                .Include(r => r.RequisitionItems)
                .Include(r => r.PurchaseOrders)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Requisition> CreateRequisitionAsync(Requisition requisition)
        {
            requisition.CreatedDate = DateTime.Now;
            requisition.Status = "Draft";
            
            _context.Requisitions.Add(requisition);
            await _context.SaveChangesAsync();
            return requisition;
        }

        public async Task UpdateRequisitionAsync(Requisition requisition)
        {
            requisition.ModifiedDate = DateTime.Now;
            _context.Entry(requisition).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRequisitionAsync(int id)
        {
            var requisition = await _context.Requisitions.FindAsync(id);
            if (requisition != null)
            {
                _context.Requisitions.Remove(requisition);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Requisition>> GetRequisitionsByStatusAsync(string status)
        {
            return await _context.Requisitions
                .Include(r => r.RequisitionItems)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.RequisitionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Requisition>> GetPendingApprovalsAsync(string role)
        {
            var query = _context.Requisitions.Include(r => r.RequisitionItems).AsQueryable();

            query = role.ToLower() switch
            {
                "supervisor" => query.Where(r => r.Status == "Pending_Supervisor"),
                "finance" => query.Where(r => r.Status == "Pending_Finance"),
                "manager" or "executive" => query.Where(r => r.Status == "Pending_Approval"),
                _ => query.Where(r => r.Status == "Draft")
            };

            return await query.OrderBy(r => r.RequisitionDate).ToListAsync();
        }

        public async Task ApproveBySupervisorAsync(int id, string supervisorName, string? comments)
        {
            var requisition = await GetRequisitionByIdAsync(id);
            if (requisition == null) return;

            requisition.SupervisorName = supervisorName;
            requisition.SupervisorApprovalDate = DateTime.Now;
            requisition.SupervisorComments = comments;
            requisition.Status = "Pending_Finance";
            requisition.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ApproveByFinanceAsync(int id, string financeName, bool budgetOk, bool needOk, bool costCodeOk, string? comments)
        {
            var requisition = await GetRequisitionByIdAsync(id);
            if (requisition == null) return;

            requisition.FinanceOfficerName = financeName;
            requisition.FinanceApprovalDate = DateTime.Now;
            requisition.BudgetApproved = budgetOk;
            requisition.NeedApproved = needOk;
            requisition.CostCodeApproved = costCodeOk;
            requisition.FinanceComments = comments;
            requisition.ModifiedDate = DateTime.Now;

            // Only move forward if all criteria approved
            if (budgetOk && needOk && costCodeOk)
            {
                requisition.Status = "Pending_Approval";
            }
            else
            {
                requisition.Status = "Rejected";
                requisition.RejectionReason = $"Finance screening failed. Budget:{budgetOk}, Need:{needOk}, CostCode:{costCodeOk}. {comments}";
            }

            await _context.SaveChangesAsync();
        }

        public async Task ApproveByFinalApproverAsync(int id, string approverName, string? comments)
        {
            var requisition = await GetRequisitionByIdAsync(id);
            if (requisition == null) return;

            requisition.FinalApproverName = approverName;
            requisition.FinalApprovalDate = DateTime.Now;
            requisition.FinalApproverComments = comments;
            requisition.Status = "Approved";
            requisition.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task RejectRequisitionAsync(int id, string rejectionReason)
        {
            var requisition = await GetRequisitionByIdAsync(id);
            if (requisition == null) return;

            requisition.Status = "Rejected";
            requisition.RejectionReason = rejectionReason;
            requisition.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}

