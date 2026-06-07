using System;
using System.Collections.Generic;

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// سجل عملية إرجاع واحدة.
    /// </summary>
    public class ReturnRecord
    {
        public int ReturnID { get; set; }
        public int SaleID { get; set; }
        public DateTime ReturnDate { get; set; }
        public string Reason { get; set; }
        public decimal TotalRefund { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public List<ReturnItemRecord> Items { get; set; } = [];

        public string DisplayReturnDate => ReturnDate.ToString("yyyy/MM/dd HH:mm");
        public string DisplayTotalRefund => TotalRefund.ToDisplayCurrency();
    }

    /// <summary>
    /// عنصر إدخال طلب إرجاع قادم من الواجهة.
    /// </summary>
    public class ReturnRequestItem
    {
        public int SaleItemID { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// نتيجة تنفيذ عملية الإرجاع.
    /// </summary>
    public class ReturnProcessResult
    {
        public int ReturnID { get; set; }
        public int SaleID { get; set; }
        public decimal TotalRefund { get; set; }
        public int ProcessedItemsCount { get; set; }
        public decimal UpdatedRemainingAmount { get; set; }
        public string UpdatedPaymentStatus { get; set; }
    }
}
