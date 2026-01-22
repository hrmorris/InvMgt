using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IRequisitionService
    {
        Task<IEnumerable<Requisition>> GetAllRequisitionsAsync();
        Task<Requisition?> GetRequisitionByIdAsync(int id);
        Task<Requisition> CreateRequisitionAsync(Requisition requisition);
        Task UpdateRequisitionAsync(Requisition requisition);
        Task DeleteRequisitionAsync(int id);
        Task<IEnumerable<Requisition>> GetRequisitionsByStatusAsync(string status);
        Task<IEnumerable<Requisition>> GetPendingApprovalsAsync(string role);
        Task ApproveBySupervisorAsync(int id, string supervisorName, string? comments);
        Task ApproveByFinanceAsync(int id, string financeName, bool budgetOk, bool needOk, bool costCodeOk, string? comments);
        Task ApproveByFinalApproverAsync(int id, string approverName, string? comments);
        Task RejectRequisitionAsync(int id, string rejectionReason);
    }
}

