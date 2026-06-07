// =====================================================
// ReportData.cs - نماذج بيانات التقارير
// تحتوي على جميع الفئات (Classes) المستخدمة لتمرير بيانات التقارير المختلفة للواجهة
// =====================================================

using System;
using System.Collections.Generic;

namespace GroceryStoreManagement.Models
{
    #region فئات تقارير المبيعات

    /// <summary>
    /// نموذج لصف واحد في تقرير المبيعات اليومية
    /// </summary>
    public class DailySalesReport
    {
        public int SaleID { get; set; } // رقم الفاتورة
        public string CustomerName { get; set; } // اسم العميل
        public DateTime SaleTime { get; set; } // وقت البيع
        public decimal TotalAmount { get; set; } // المبلغ الإجمالي
        public int ItemCount { get; set; } // عدد الأصناف

        // خصائص للعرض
        public string DisplayTime => SaleTime.ToString("hh:mm tt"); // الوقت بتنسيق ساعة ودقيقة ومسائي/صباحي
        public string DisplayAmount => TotalAmount.ToDisplayCurrency(); // المبلغ بتنسيق العملة
    }

    /// <summary>
    /// صف تجميعي يومي للمبيعات (مخصص للمخططات).
    /// </summary>
    public class DailySalesAggregate
    {
        public DateTime SaleDate { get; set; }
        public decimal TotalSales { get; set; }
    }

    /// <summary>
    /// نموذج لتقرير المبيعات الشهرية الشامل
    /// </summary>
    public class MonthlySalesReport
    {
        public int Year { get; set; } // السنة
        public int Month { get; set; } // الشهر
        public decimal TotalSales { get; set; } // إجمالي المبيعات
        public int TotalTransactions { get; set; } // عدد عمليات البيع
        public int TotalItemsSold { get; set; } // عدد القطع المباعة
        public decimal AverageDailySales { get; set; } // متوسط المبيعات اليومي
        public decimal AverageTransactionValue { get; set; } // متوسط قيمة الفاتورة
        public Dictionary<DateTime, decimal> DailyBreakdown { get; set; } // تفصيل يومي للمخططات البيانية

        // اسم الشهر بالعربية (مثلاً: يناير)
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM", new System.Globalization.CultureInfo("ar-SA"));
        public string DisplayTotalSales => TotalSales.ToDisplayCurrency();
        public string DisplayAverageDaily => AverageDailySales.ToDisplayCurrency();
    }

    /// <summary>
    /// نموذج لتقرير المبيعات السنوية
    /// </summary>
    public class YearlySalesReport
    {
        public int Year { get; set; } // السنة
        public decimal TotalSales { get; set; } // إجمالي المبيعات
        public int TotalTransactions { get; set; } // عدد الفواتير
        public int TotalItemsSold { get; set; } // عدد المنتجات
        public decimal AverageMonthlySales { get; set; } // المتوسط الشهري
        public decimal AverageTransactionValue { get; set; } // متوسط الفاتورة
        public Dictionary<int, decimal> MonthlyBreakdown { get; set; } // تفصيل شهري (شهر:مبلغ)

        public string DisplayTotalSales => TotalSales.ToDisplayCurrency();
        public string DisplayAverageMonthly => AverageMonthlySales.ToDisplayCurrency();
    }

    /// <summary>
    /// نموذج لتقرير مبيعات فترة محددة (مخصص)
    /// </summary>
    public class PeriodSalesReport
    {
        public DateTime StartDate { get; set; } // من تاريخ
        public DateTime EndDate { get; set; } // إلى تاريخ
        public decimal TotalSales { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal AverageDailySales { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public Dictionary<DateTime, decimal> DailyBreakdown { get; set; } // تفصيل للأيام
        public List<ProductSalesReport> TopProducts { get; set; } // أفضل المنتجات مبيعاً في الفترة
        public List<CustomerPurchaseReport> TopCustomers { get; set; } // أفضل العملاء في الفترة

        public string DisplayPeriod => $"{StartDate:yyyy/MM/dd} - {EndDate:yyyy/MM/dd}"; // عرض الفترة
        public string DisplayTotalSales => TotalSales.ToDisplayCurrency();
    }

    #endregion

    #region فئات تقارير المنتجات

    /// <summary>
    /// نموذج لصف واحد في تقرير أداء المنتجات
    /// </summary>
    public class ProductSalesReport
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public int QuantitySold { get; set; } // الكمية المباعة
        public decimal TotalRevenue { get; set; } // الإيرادات المحققة
        public decimal AveragePrice { get; set; } // متوسط سعر البيع الفعلي
        public int CurrentStock { get; set; } // المخزون الحالي
        public decimal StockValue { get; set; } // قيمة المخزون الحالي

        public string DisplayRevenue => TotalRevenue.ToDisplayCurrency();
        public string DisplayAveragePrice => AveragePrice.ToDisplayCurrency();
        public string DisplayStockValue => StockValue.ToDisplayCurrency();
        public string StockStatus => CurrentStock <= 10 ? "منخفض" : "جيد"; // تقييم بسيط للمخزون
    }

    /// <summary>
    /// تقرير ملخص مبيعات الفئات
    /// </summary>
    public class CategorySalesReport
    {
        public string Category { get; set; } // الفئة
        public int ProductCount { get; set; } // عدد منتجات هذه الفئة
        public int TotalQuantitySold { get; set; } // إجمالي الوحدات المباعة
        public decimal TotalRevenue { get; set; } // إجمالي الإيرادات

        public string DisplayRevenue => TotalRevenue.ToDisplayCurrency();
    }

    /// <summary>
    /// تقرير المنتجات التي وصلت للحد الأدنى (النواقص)
    /// </summary>
    public class LowStockReport
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public int CurrentStock { get; set; } // الرصيد الحالي
        public int MinimumStock { get; set; } // حد الطلب
        public double DailyAverageSales { get; set; } // معدل البيع اليومي
        public double DaysRemaining { get; set; } // الأيام المتوقعة حتى النفاد
        public string Status { get; set; } // حالة الخطر (حرج، منخفض...)
        public string Urgency { get; set; } // درجة الاستعجال

        public string DisplayDaysRemaining => DaysRemaining.ToString("0.0");
        public string Recommendation => GetRecommendation(); // التوصية

        // تحديد التوصية بناءً على الأيام المتبقية
        private string GetRecommendation()
        {
            if (DaysRemaining <= 3) return "توصيل عاجل";
            if (DaysRemaining <= 7) return "طلب سريع";
            if (DaysRemaining <= 14) return "طلب خلال الأسبوع";
            return "متابعة دورية";
        }
    }

    /// <summary>
    /// تقرير توزيع قيمة المخزون على الفئات
    /// </summary>
    public class InventoryValueReport
    {
        public string Category { get; set; }
        public int ProductCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AveragePrice { get; set; }
        public double PercentageOfTotal { get; set; } // النسبة من إجمالي المخزون

        public string DisplayTotalValue => TotalValue.ToDisplayCurrency();
        public string DisplayAveragePrice => AveragePrice.ToDisplayCurrency();
    }

    /// <summary>
    /// تقرير كفاءة دوران المخزون (Inventory Turnover)
    /// </summary>
    public class InventoryTurnoverReport
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public int QuantitySold { get; set; }
        public int AverageInventory { get; set; } // متوسط المخزون خلال الفترة
        public double TurnoverRatio { get; set; } // معدل الدوران
        public double DaysToSell { get; set; } // متوسط الأيام لبيع المخزون
        public string TurnoverRating { get; set; } // تقييم (ممتاز، جيد، ضعيف)

        public string DisplayTurnoverRatio => TurnoverRatio.ToString("0.00");
        public string DisplayDaysToSell => DaysToSell.ToString("0.0");
    }

    #endregion

    #region فئات تقارير العملاء

    /// <summary>
    /// تقرير تحليل مشتريات العملاء
    /// </summary>
    public class CustomerPurchaseReport
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public decimal TotalPurchases { get; set; } // المبلغ الإجمالي
        public int PurchaseCount { get; set; } // عدد الفواتير
        public decimal AveragePurchase { get; set; } // متوسط الفاتورة
        public DateTime LastPurchaseDate { get; set; }
        public List<string> Recommendations { get; set; }

        public string DisplayTotalPurchases => TotalPurchases.ToDisplayCurrency();
        public string DisplayAveragePurchase => AveragePurchase.ToDisplayCurrency();
        public string DisplayLastPurchase => LastPurchaseDate.ToString("yyyy/MM/dd");
        public int DaysSinceLastPurchase => (DateTime.Today - LastPurchaseDate).Days;
    }

    public class MonthlySpendingData
    {
        public string Month { get; set; }
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }

        public string DisplayAmount => Amount.ToDisplayCurrency();
    }

    /// <summary>
    /// تقرير ولاء العملاء وتصنيفهم
    /// </summary>
    public class CustomerLoyaltyReport
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public decimal TotalPurchases { get; set; }
        public int PurchaseCount { get; set; }
        public decimal AveragePurchase { get; set; }
        public int DaysSinceLastPurchase { get; set; }
        public double LoyaltyScore { get; set; } // نقاط الولاء (0-100)
        public string CustomerType { get; set; } // تصنيف (ذهبي، فضي...)
        public List<string> Recommendations { get; set; } // مقترحات للتسويق

        public string DisplayTotalPurchases => TotalPurchases.ToDisplayCurrency();
        public string DisplayAveragePurchase => AveragePurchase.ToDisplayCurrency();
        public string DisplayLoyaltyScore => $"{LoyaltyScore:0.0}%";
    }

    /// <summary>
    /// تقرير العملاء الجدد ومعدل نموهم
    /// </summary>
    public class NewCustomersReport
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public DateTime RegistrationDate { get; set; } // تاريخ أول شراء
        public int DaysSinceRegistration { get; set; }
        public decimal TotalPurchases { get; set; }
        public int PurchaseCount { get; set; }

        public string DisplayRegistrationDate => RegistrationDate.ToString("yyyy/MM/dd");
        public string DisplayTotalPurchases => TotalPurchases.ToDisplayCurrency();
    }

    #endregion

    #region فئات تقارير الموردين

    /// <summary>
    /// تقرير تقييم أداء الموردين
    /// </summary>
    public class SupplierPerformanceReport
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string ContactPhone { get; set; }
        public int TotalProducts { get; set; } // عدد الأصناف الموردة
        public decimal TotalStockValue { get; set; } // قيمة البضاعة
        public decimal AverageProductPrice { get; set; }
        public int LowStockProducts { get; set; } // عدد الأصناف الناقصة
        public double PerformanceRating { get; set; } // تقييم الأداء (0-100)
        public DateTime LastDeliveryDate { get; set; } // آخر توريد

        public string DisplayStockValue => TotalStockValue.ToDisplayCurrency();
        public string DisplayAveragePrice => AverageProductPrice.ToDisplayCurrency();
        public string DisplayPerformanceRating => $"{PerformanceRating:0.0}%";
        public string DisplayLastDelivery => LastDeliveryDate.ToString("yyyy/MM/dd");
    }

    /// <summary>
    /// تقرير تحليل الموردين حسب الفئات (من يورد ماذا؟)
    /// </summary>
    public class SupplierCategoryAnalysis
    {
        public string Category { get; set; } // الفئة
        public int SupplierCount { get; set; } // كم مورد يورد هذه الفئة
        public int ProductCount { get; set; }
        public decimal TotalValue { get; set; }
        public string MainSupplier { get; set; } // المورد الرئيسي للفئة
        public double MainSupplierPercentage { get; set; } // نسبة سيطرة المورد الرئيسي

        public string DisplayTotalValue => TotalValue.ToDisplayCurrency();
        public string DisplayMainSupplierPercentage => $"{MainSupplierPercentage:0.0}%";
    }

    #endregion
}
