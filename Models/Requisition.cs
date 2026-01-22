using System.ComponentModel.DataAnnotations;

namespace InvoiceManagement.Models
{
    public class Requisition
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RequisitionNumber { get; set; } = string.Empty;

        [Required]
        public DateTime RequisitionDate { get; set; }

        [Required]
        [StringLength(200)]
        public string RequestedBy { get; set; } = string.Empty; // OIC or Unit Head

        [Required]
        [StringLength(200)]
        public string Department { get; set; } = string.Empty; // Health Facility or Hospital Unit

        [Required]
        [StringLength(100)]
        public string FacilityType { get; set; } = string.Empty; // "Outstation" or "Hospital"

        [StringLength(500)]
        public string Purpose { get; set; } = string.Empty;

        public decimal EstimatedAmount { get; set; }

        [StringLength(50)]
        public string CostCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string BudgetCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; 
        // Draft, Pending_Supervisor, Pending_Finance, Pending_Approval, Approved, Rejected

        // Approval chain
        [StringLength(200)]
        public string? SupervisorName { get; set; }
        public DateTime? SupervisorApprovalDate { get; set; }
        [StringLength(500)]
        public string? SupervisorComments { get; set; }

        [StringLength(200)]
        public string? FinanceOfficerName { get; set; }
        public DateTime? FinanceApprovalDate { get; set; }
        [StringLength(500)]
        public string? FinanceComments { get; set; }
        public bool? BudgetApproved { get; set; }
        public bool? NeedApproved { get; set; }
        public bool? CostCodeApproved { get; set; }

        [StringLength(200)]
        public string? FinalApproverName { get; set; } // Health Manager or Hospital Executive Officer
        public DateTime? FinalApprovalDate { get; set; }
        [StringLength(500)]
        public string? FinalApproverComments { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual ICollection<RequisitionItem> RequisitionItems { get; set; } = new List<RequisitionItem>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }

    public class RequisitionItem
    {
        public int Id { get; set; }

        public int RequisitionId { get; set; }

        [Required]
        [StringLength(200)]
        public string ItemDescription { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ItemCode { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityRequested { get; set; }

        [StringLength(50)]
        public string Unit { get; set; } = "pcs"; // pcs, boxes, cartons, etc.

        [Range(0, double.MaxValue)]
        public decimal EstimatedUnitPrice { get; set; }

        public decimal EstimatedTotal => QuantityRequested * EstimatedUnitPrice;

        [StringLength(500)]
        public string? Justification { get; set; }

        // Navigation property
        public virtual Requisition? Requisition { get; set; }
    }
}

