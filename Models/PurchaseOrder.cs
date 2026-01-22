using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PONumber { get; set; } = string.Empty;

        [Required]
        public DateTime PODate { get; set; }

        public int? RequisitionId { get; set; }

        public int SupplierId { get; set; }

        [Required]
        public DateTime ExpectedDeliveryDate { get; set; }

        [StringLength(200)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; 
        // Pending, Sent_to_Supplier, Partially_Received, Fully_Received, Cancelled

        [StringLength(200)]
        public string PreparedBy { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }

        [StringLength(500)]
        public string? TermsAndConditions { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual Requisition? Requisition { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

    public class PurchaseOrderItem
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        [Required]
        [StringLength(200)]
        public string ItemDescription { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ItemCode { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityOrdered { get; set; }

        [StringLength(50)]
        public string Unit { get; set; } = "pcs";

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => QuantityOrdered * UnitPrice;

        [Range(0, int.MaxValue)]
        public int QuantityReceived { get; set; } = 0;

        public int QuantityPending => QuantityOrdered - QuantityReceived;

        public DateTime? ReceivedDate { get; set; }

        // Navigation property
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
    }
}

