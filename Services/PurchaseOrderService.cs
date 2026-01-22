using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllPurchaseOrdersAsync()
        {
            return await _context.PurchaseOrders
                .Include(po => po.Requisition)
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                .OrderByDescending(po => po.PODate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByStatusAsync(string status)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Requisition)
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                .Where(po => po.Status == status)
                .OrderByDescending(po => po.PODate)
                .ToListAsync();
        }

        public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Requisition)
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                .Include(po => po.Invoices)
                .FirstOrDefaultAsync(po => po.Id == id);
        }

        public async Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrder purchaseOrder)
        {
            purchaseOrder.CreatedDate = DateTime.Now;
            purchaseOrder.Status = "Pending";

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync();
            return purchaseOrder;
        }

        public async Task UpdatePurchaseOrderAsync(PurchaseOrder purchaseOrder)
        {
            purchaseOrder.ModifiedDate = DateTime.Now;
            _context.Entry(purchaseOrder).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeletePurchaseOrderAsync(int id)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po != null)
            {
                _context.PurchaseOrders.Remove(po);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<PurchaseOrder> CreateFromRequisitionAsync(int requisitionId, int supplierId)
        {
            var requisition = await _context.Requisitions
                .Include(r => r.RequisitionItems)
                .FirstOrDefaultAsync(r => r.Id == requisitionId);

            if (requisition == null)
                throw new Exception("Requisition not found");

            if (requisition.Status != "Approved")
                throw new Exception("Requisition must be approved before creating PO");

            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null)
                throw new Exception("Supplier not found");

            // Generate PO Number
            var lastPO = await _context.PurchaseOrders
                .OrderByDescending(po => po.Id)
                .FirstOrDefaultAsync();

            var poNumber = $"PO-{DateTime.Now:yyyyMMdd}-{(lastPO != null ? lastPO.Id + 1 : 1):D4}";

            var purchaseOrder = new PurchaseOrder
            {
                PONumber = poNumber,
                PODate = DateTime.Now,
                RequisitionId = requisitionId,
                SupplierId = supplierId,
                ExpectedDeliveryDate = DateTime.Now.AddDays(supplier.PaymentTermsDays),
                DeliveryAddress = requisition.Department,
                PreparedBy = requisition.RequestedBy,
                Status = "Pending",
                CreatedDate = DateTime.Now,
                PurchaseOrderItems = new List<PurchaseOrderItem>()
            };

            // Copy items from requisition
            foreach (var reqItem in requisition.RequisitionItems)
            {
                purchaseOrder.PurchaseOrderItems.Add(new PurchaseOrderItem
                {
                    ItemDescription = reqItem.ItemDescription,
                    ItemCode = reqItem.ItemCode,
                    QuantityOrdered = reqItem.QuantityRequested,
                    Unit = reqItem.Unit,
                    UnitPrice = reqItem.EstimatedUnitPrice,
                    QuantityReceived = 0
                });
            }

            purchaseOrder.TotalAmount = purchaseOrder.PurchaseOrderItems.Sum(i => i.TotalPrice);

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync();

            return purchaseOrder;
        }

        public async Task MarkItemsReceivedAsync(int poId, Dictionary<int, int> itemQuantities)
        {
            var po = await GetPurchaseOrderByIdAsync(poId);
            if (po == null) return;

            foreach (var item in po.PurchaseOrderItems)
            {
                if (itemQuantities.ContainsKey(item.Id))
                {
                    item.QuantityReceived += itemQuantities[item.Id];
                    if (item.QuantityReceived >= item.QuantityOrdered)
                    {
                        item.ReceivedDate = DateTime.Now;
                    }
                }
            }

            // Update PO status
            if (po.PurchaseOrderItems.All(i => i.QuantityReceived >= i.QuantityOrdered))
            {
                po.Status = "Fully_Received";
            }
            else if (po.PurchaseOrderItems.Any(i => i.QuantityReceived > 0))
            {
                po.Status = "Partially_Received";
            }

            await _context.SaveChangesAsync();
        }
    }
}

