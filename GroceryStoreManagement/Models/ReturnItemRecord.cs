namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// عنصر داخل عملية الإرجاع.
    /// </summary>
    public class ReturnItemRecord
    {
        public int ReturnItemID { get; set; }
        public int ReturnID { get; set; }
        public int SaleItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal RefundAmount { get; set; }

        public string DisplayUnitPrice => UnitPrice.ToDisplayCurrency();
        public string DisplayRefundAmount => RefundAmount.ToDisplayCurrency();
    }
}
