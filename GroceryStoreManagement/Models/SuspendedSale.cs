using System;
using System.Collections.Generic;

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// فاتورة مبيعات معلقة يمكن استدعاؤها لاحقاً.
    /// </summary>
    public class SuspendedSale
    {
        public int SuspendedSaleID { get; set; }
        public int? CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string Notes { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public int? ShiftID { get; set; }
        public decimal Subtotal { get; set; }

        public List<SuspendedSaleItem> Items { get; set; } = [];

        public decimal TaxAmount => Subtotal * (Tax / 100m);
        public decimal NetTotal => Subtotal - Discount + TaxAmount;
        public int ItemLinesCount => Items?.Count ?? 0;
        public int ItemQuantityCount => Items == null ? 0 : System.Linq.Enumerable.Sum(Items, x => x.Quantity);
        public string DisplayCreatedAt => CreatedAt.ToString("yyyy/MM/dd HH:mm");
    }

    public class SuspendedSaleItem
    {
        public int SuspendedSaleItemID { get; set; }
        public int SuspendedSaleID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
