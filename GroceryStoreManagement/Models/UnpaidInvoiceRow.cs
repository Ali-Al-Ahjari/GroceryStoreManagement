using System;

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// صف موحّد لعرض الفواتير غير المسددة في التقارير.
    /// </summary>
    public class UnpaidInvoiceRow
    {
        public string Type { get; set; } // مبيعات / مشتريات
        public int ID { get; set; }
        public string Name { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public DateTime Date { get; set; }
        public int? CustomerID { get; set; }
        public DateTime? DueDate { get; set; }

        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today;
        public string DisplayTotal => Total.ToDisplayCurrency();
        public string DisplayPaid => Paid.ToDisplayCurrency();
        public string DisplayRemaining => Remaining.ToDisplayCurrency();
        public string DisplayDate => Date.ToString("yyyy/MM/dd");
    }
}
