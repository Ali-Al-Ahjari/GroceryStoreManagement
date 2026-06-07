namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج ملخص لوحة التحكم
    /// يستخدم لعرض الإحصائيات السريعة في الشاشة الرئيسية
    /// </summary>
    public class DashboardSummary
    {
        /// <summary>
        /// مجموع مبيعات اليوم الحالي
        /// </summary>
        public decimal TodaySales { get; set; }

        /// <summary>
        /// عدد فواتير اليوم
        /// </summary>
        public int TodayTransactions { get; set; }

        /// <summary>
        /// عدد المنتجات التي وصلت للحد الأدنى للمخزون
        /// </summary>
        public int LowStockProducts { get; set; }

        /// <summary>
        /// إجمالي عدد المنتجات المسجلة في النظام
        /// </summary>
        public int TotalProducts { get; set; }

        /// <summary>
        /// إجمالي عدد العملاء المسجلين
        /// </summary>
        public int TotalCustomers { get; set; }

        /// <summary>
        /// مجموع مبيعات الشهر الحالي
        /// </summary>
        public decimal MonthlySales { get; set; }

        /// <summary>
        /// القيمة المالية الإجمالية للمخزون الحالي
        /// </summary>
        public decimal StockValue { get; set; }

        // --- خصائص منسقة للعرض (Formatted Strings) ---

        /// <summary>
        /// مبيعات اليوم بتنسيق العملة
        /// </summary>
        public string DisplayTodaySales => TodaySales.ToDisplayCurrency();

        /// <summary>
        /// مبيعات الشهر بتنسيق العملة
        /// </summary>
        public string DisplayMonthlySales => MonthlySales.ToDisplayCurrency();

        /// <summary>
        /// قيمة المخزون بتنسيق العملة
        /// </summary>
        public string DisplayStockValue => StockValue.ToDisplayCurrency();
    }
}
