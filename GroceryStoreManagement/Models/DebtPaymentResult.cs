using System.Collections.Generic;

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// تفاصيل توزيع سداد الدين على فاتورة محددة.
    /// </summary>
    public class DebtPaymentAllocation
    {
        public int SaleID { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal PreviousRemaining { get; set; }
        public decimal RemainingAfterAllocation { get; set; }
    }

    /// <summary>
    /// نتيجة عملية سداد ديون عميل.
    /// </summary>
    public class DebtPaymentResult
    {
        public int CustomerID { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal UnappliedAmount { get; set; }
        public string PaymentMethod { get; set; }
        public int AffectedInvoicesCount { get; set; }
        public decimal TotalRemainingAfterPayment { get; set; }
        public List<DebtPaymentAllocation> Allocations { get; set; } = [];
    }
}

