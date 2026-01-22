using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IPurchaseOrderService
    {
        Task<IEnumerable<PurchaseOrder>> GetAllPurchaseOrdersAsync();
        Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByStatusAsync(string status);
        Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id);
        Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrder purchaseOrder);
        Task UpdatePurchaseOrderAsync(PurchaseOrder purchaseOrder);
        Task DeletePurchaseOrderAsync(int id);
        Task<PurchaseOrder> CreateFromRequisitionAsync(int requisitionId, int supplierId);
        Task MarkItemsReceivedAsync(int poId, Dictionary<int, int> itemQuantities);
    }
}

