using InvoiceManagement.Models;

namespace InvoiceManagement.Services
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetAllPaymentsAsync();
        Task<Payment?> GetPaymentByIdAsync(int id);
        Task<Payment?> GetPaymentWithAllocationsAsync(int id);
        Task<IEnumerable<Payment>> GetPaymentsByInvoiceIdAsync(int invoiceId);
        Task<IEnumerable<Payment>> GetUnallocatedPaymentsAsync();
        Task<IEnumerable<Payment>> GetPartiallyAllocatedPaymentsAsync();
        Task<Payment> CreatePaymentAsync(Payment payment);
        Task UpdatePaymentAsync(Payment payment);
        Task DeletePaymentAsync(int id);
        
        // Payment Allocation methods
        Task<PaymentAllocation> AllocatePaymentToInvoiceAsync(int paymentId, int invoiceId, decimal amount, string? notes = null);
        Task<IEnumerable<PaymentAllocation>> GetPaymentAllocationsAsync(int paymentId);
        Task<PaymentAllocation?> GetAllocationByIdAsync(int allocationId);
        Task UpdateAllocationAsync(PaymentAllocation allocation);
        Task DeleteAllocationAsync(int allocationId);
        Task<decimal> GetUnallocatedAmountAsync(int paymentId);
        Task UpdatePaymentStatusAsync(int paymentId);
    }
}

