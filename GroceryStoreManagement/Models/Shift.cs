using System;

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج الوردية اليومية لنقطة البيع.
    /// </summary>
    public class Shift
    {
        public int ShiftID { get; set; }
        public int OpenedBy { get; set; }
        public string OpenedByName { get; set; }
        public DateTime OpenedAt { get; set; }
        public decimal OpeningCash { get; set; }

        public int? ClosedBy { get; set; }
        public string ClosedByName { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal? ClosingCash { get; set; }

        public decimal CashSalesTotal { get; set; }
        public decimal CardSalesTotal { get; set; }
        public decimal TransferSalesTotal { get; set; }
        public decimal CreditSalesTotal { get; set; }
        public decimal CashRefundsTotal { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal CashDifference { get; set; }

        public string Notes { get; set; }
        public string Status { get; set; } = "Open";

        public bool IsOpen => string.Equals(Status, "Open", StringComparison.OrdinalIgnoreCase);
        public string StatusArabic => IsOpen ? "مفتوحة" : "مغلقة";
        public string DisplayOpenedAt => OpenedAt.ToString("yyyy/MM/dd HH:mm");
        public string DisplayClosedAt => ClosedAt?.ToString("yyyy/MM/dd HH:mm") ?? "-";
    }
}
