// =====================================================
// ReportHelper.cs - مساعد التقارير
// يحتوي على جميع وظائف توليد التقارير المختلفة
// =====================================================

using GroceryStoreManagement.DAL; // الوصول لطبقة البيانات لجلب المعلومات
using GroceryStoreManagement.Models; // نماذج البيانات التي ستستخدم في التقارير
using System; // الدوال الأساسية
using System.Collections.Generic; // القوائم والمجموعات
using System.Linq; // استعلامات المعالجة والفلترة

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// فئة مساعد التقارير - كلاس ثابت (Static) يوفر مجموعة من الدوال الجاهزة لتوليد أنواع مختلفة من التقارير
    /// </summary>
    public static class ReportHelper
    {
        private const double V = 0.0;
        #region ملخص لوحة التحكم (Dashboard)

        /// <summary>
        /// الحصول على ملخص سريع للأرقام الرئيسية لعرضها في الشاشة الرئيسية
        /// </summary>
        /// <returns>كائن يحتوي على إحصائيات اليوم والشهر والمخزون</returns>
        public static DashboardSummary GetDashboardSummary()
        {
            // إنشاء كائن الحاوية للبيانات
            var summary = new DashboardSummary();
            var today = DateTime.Today; // تحديد تاريخ اليوم

            try
            {
                // 1. مبيعات اليوم (المبلغ المالي)
                summary.TodaySales = SaleDAL.GetDailySalesTotal(today);

                // 2. عدد عمليات البيع (الفواتير) اليوم
                summary.TodayTransactions = SaleDAL.GetDailySalesCount(today);

                // 3. عدد المنتجات التي مخزونها أقل من الحد الأدنى
                summary.LowStockProducts = ProductDAL.GetLowStockProducts(10).Count;

                // 4. العدد الإجمالي للأصناف في النظام
                summary.TotalProducts = ProductDAL.GetTotalProductsCount();

                // 5. العدد الإجمالي للعملاء المسجلين
                summary.TotalCustomers = CustomerDAL.GetTotalCustomersCount();

                // 6. مجموع مبيعات الشهر الحالي
                summary.MonthlySales = SaleDAL.GetMonthlySalesTotal(today);

                // 7. القيمة المالية الإجمالية للمخزون الحالي
                summary.StockValue = ProductDAL.GetTotalStockValue();
            }
            catch (Exception ex)
            {
                // في حالة فشل أي استعلام، نوقف العملية ونظهر خطأ واضح
                throw new Exception($"خطأ في توليد ملخص لوحة التحكم: {ex.Message}");
            }

            return summary;
        }

        #endregion

        #region تقارير المبيعات اليومية

        /// <summary>
        /// توليد تقرير مفصل بجميع مبيعات يوم محدد
        /// </summary>
        /// <param name="date">التاريخ المطلوب عرض تقريره</param>
        /// <returns>قائمة بصفوف التقرير</returns>
        public static List<DailySalesReport> GetDailySalesReport(DateTime date)
        {
            try
            {
                // جلب البيانات الخام من قاعدة البيانات لليوم المحدد
                var sales = SaleDAL.GetSalesByDate(date);
                var report = new List<DailySalesReport>();

                // تحويل كل سجل بيع إلى نموذج تقرير مبسط
                foreach (var sale in sales)
                {
                    report.Add(new DailySalesReport
                    {
                        SaleID = sale.SaleID, // رقم الفاتورة
                        CustomerName = sale.CustomerName ?? "عميل نقدي", // اسم العميل (أو افتراضي)
                        SaleTime = sale.SaleDate, // وقت البيع
                        TotalAmount = sale.NetTotal, // المبلغ الصافي بعد الخصم والضريبة
                        ItemCount = sale.ItemCount // عدد العناصر
                    });
                }

                // ترتيب النتائج بحيث تظهر الأحدث أولاً
                return [.. report.OrderByDescending(r => r.SaleTime)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير المبيعات اليومية: {ex.Message}");
            }
        }

        #endregion

        #region تقارير المبيعات الشهرية

        /// <summary>
        /// توليد تقرير شامل لشهر كامل
        /// </summary>
        /// <param name="year">السنة</param>
        /// <param name="month">الشهر</param>
        /// <returns>كائن يحتوي على إجماليات الشهر وتفصيل يومي</returns>
        public static MonthlySalesReport GetMonthlySalesReport(int year, int month)
        {
            try
            {
                // تحديد بداية ونهاية الشهر بدقة
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // جلب جميع المبيعات التي وقعت في هذه الفترة
                var sales = SaleDAL.GetSalesByDateRange(startDate, endDate);

                // حساب المؤشرات العامة
                decimal totalSales = sales.Sum(s => s.NetTotal); // مجموع المبيعات الصافية
                int totalTransactions = sales.Count; // عدد الفواتير
                int totalItems = sales.Sum(s => s.ItemCount); // عدد القطع المباعة

                // إنشاء تفصيل يومي (كم بعنا في كل يوم من أيام الشهر؟)
                var dailyBreakdown = new Dictionary<DateTime, decimal>();
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    // حساب مبيعات هذا اليوم
                    var dailyTotal = SaleDAL.GetDailySalesTotal(date);
                    dailyBreakdown.Add(date, dailyTotal);
                }

                // تعبئة وإرجاع نموذج التقرير
                return new MonthlySalesReport
                {
                    Year = year,
                    Month = month,
                    TotalSales = totalSales,
                    TotalTransactions = totalTransactions,
                    TotalItemsSold = totalItems,
                    // متوسط المبيعات اليومي (إجمالي / عدد أيام الشهر)
                    AverageDailySales = totalSales / DateTime.DaysInMonth(year, month),
                    // متوسط قيمة الفاتورة الواحدة
                    AverageTransactionValue = totalTransactions > 0 ? totalSales / totalTransactions : 0,
                    DailyBreakdown = dailyBreakdown
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير المبيعات الشهرية: {ex.Message}");
            }
        }

        #endregion

        #region تقارير المبيعات السنوية

        /// <summary>
        /// توليد تقرير شامل لسنة كاملة
        /// </summary>
        /// <param name="year">السنة المطلوبة</param>
        /// <returns>تقرير سنوي</returns>
        public static YearlySalesReport GetYearlySalesReport(int year)
        {
            try
            {
                // تحديد بداية ونهاية السنة
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31);

                // جلب البيانات
                var sales = SaleDAL.GetSalesByDateRange(startDate, endDate);

                // حساب الإجماليات
                decimal totalSales = sales.Sum(s => s.NetTotal);
                int totalTransactions = sales.Count;
                int totalItems = sales.Sum(s => s.ItemCount);

                // تفصيل شهري (كم بعنا في كل شهر؟)
                var monthlyBreakdown = new Dictionary<int, decimal>();
                for (int month = 1; month <= 12; month++)
                {
                    var monthStart = new DateTime(year, month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    // فلترة المبيعات لهذا الشهر فقط وجمعها
                    var monthSales = sales.Where(s => s.SaleDate >= monthStart && s.SaleDate <= monthEnd)
                                        .Sum(s => s.NetTotal);
                    monthlyBreakdown.Add(month, monthSales);
                }

                // إرجاع التقرير
                return new YearlySalesReport
                {
                    Year = year,
                    TotalSales = totalSales,
                    TotalTransactions = totalTransactions,
                    TotalItemsSold = totalItems,
                    AverageMonthlySales = totalSales / 12, // متوسط شهري
                    AverageTransactionValue = totalTransactions > 0 ? totalSales / totalTransactions : 0,
                    MonthlyBreakdown = monthlyBreakdown
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير المبيعات السنوية: {ex.Message}");
            }
        }

        #endregion

        #region تقارير المبيعات حسب الفترة (مخصص)

        /// <summary>
        /// تقرير مبيعات لفترة محددة يختارها المستخدم (من تاريخ - إلى تاريخ)
        /// </summary>
        public static PeriodSalesReport GetPeriodSalesReport(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sales = SaleDAL.GetSalesByDateRange(startDate, endDate);

                decimal totalSales = sales.Sum(s => s.NetTotal);
                int totalTransactions = sales.Count;
                int totalItems = sales.Sum(s => s.ItemCount);

                // تفصيل يومي للفترة المختارة
                var dailyBreakdown = new Dictionary<DateTime, decimal>();
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dailyTotal = sales.Where(s => s.SaleDate.Date == date.Date)
                                        .Sum(s => s.NetTotal);
                    dailyBreakdown.Add(date, dailyTotal);
                }

                // جلب أفضل 5 منتجات مبيعاً في هذه الفترة
                var topProducts = GetTopSellingProducts(startDate, endDate, 5);

                // جلب أفضل 5 عملاء شراءً في هذه الفترة
                var topCustomers = GetTopCustomers(startDate, endDate, 5);

                return new PeriodSalesReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalSales = totalSales,
                    TotalTransactions = totalTransactions,
                    TotalItemsSold = totalItems,
                    // متوسط يومي فعلي (المجموع / عدد الأيام)
                    AverageDailySales = totalSales / ((endDate - startDate).Days + 1),
                    AverageTransactionValue = totalTransactions > 0 ? totalSales / totalTransactions : 0,
                    DailyBreakdown = dailyBreakdown,
                    TopProducts = topProducts,
                    TopCustomers = topCustomers
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير المبيعات حسب الفترة: {ex.Message}");
            }
        }

        #endregion

        #region تقارير الفئات

        /// <summary>
        /// الحصول على قائمة بالفئات الأكثر مبيعاً (ترتيب تنازلي حسب الإيرادات)
        /// </summary>
        public static List<CategorySalesReport> GetTopSellingCategories(int limit = 5)
        {
            try
            {
                var products = ProductDAL.GetAllProducts();
                var categoryStats = new Dictionary<string, CategorySalesReport>();

                foreach (var product in products)
                {
                    // التعامل مع المنتجات غير المصنفة
                    var category = string.IsNullOrEmpty(product.Category) ? "غير مصنف" : product.Category;

                    // جلب مبيعات هذا المنتج
                    var salesItems = SaleItemDAL.GetSaleItemsByProduct(product.ProductID);
                    var totalSold = salesItems.Sum(item => item.Quantity);
                    var totalRevenue = salesItems.Sum(item => item.TotalPrice);

                    // إذا لم تكن الفئة موجودة في القاموس، نضيفها
                    if (!categoryStats.TryGetValue(category, out CategorySalesReport value))
                    {
                        value = new CategorySalesReport
                        {
                            Category = category,
                            ProductCount = 0,
                            TotalQuantitySold = 0,
                            TotalRevenue = 0
                        };
                        categoryStats[category] = value;
                    }

                    value.ProductCount++;
                    value.TotalQuantitySold += totalSold;
                    value.TotalRevenue += totalRevenue;
                }

                // ترتيب النتائج وإرجاع العدد المطلوب
                return [.. categoryStats.Values
                    .OrderByDescending(c => c.TotalRevenue)
                    .Take(limit)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير الفئات الأكثر مبيعاً: {ex.Message}");
            }
        }

        #endregion

        #region تقارير المنتجات

        /// <summary>
        /// تقرير المنتجات الأكثر مبيعاً
        /// </summary>
        public static List<ProductSalesReport> GetTopSellingProducts(DateTime? startDate = null, DateTime? endDate = null, int limit = 10)
        {
            if (startDate is null)
            {
                throw new ArgumentNullException(nameof(startDate));
            }

            if (endDate is null)
            {
                throw new ArgumentNullException(nameof(endDate));
            }

            try
            {
                var allProducts = ProductDAL.GetAllProducts();
                var report = new List<ProductSalesReport>();

                foreach (var product in allProducts)
                {
                    // جلب المبيعات لهذا المنتج (يمكن تحسين هذا الاستعلام ليكون bulk query للأداء الأفضل)
                    var salesItems = SaleItemDAL.GetSaleItemsByProduct(product.ProductID);

                    var totalSold = salesItems.Sum(item => item.Quantity);
                    var totalRevenue = salesItems.Sum(item => item.TotalPrice);

                    // نضيفه للتقرير فقط إذا تم بيعه
                    if (totalSold > 0)
                    {
                        report.Add(new ProductSalesReport
                        {
                            ProductID = product.ProductID,
                            ProductName = product.Name,
                            Category = product.Category,
                            QuantitySold = totalSold,
                            TotalRevenue = totalRevenue,
                            // متوسط سعر البيع الفعلي
                            AveragePrice = totalSold > 0 ? totalRevenue / totalSold : 0,
                            CurrentStock = product.Quantity,
                            StockValue = product.Quantity * product.Price
                        });
                    }
                }

                // الفرز حسب الكمية المباعة
                return [.. report.OrderByDescending(r => r.QuantitySold).Take(limit)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير المنتجات الأكثر مبيعاً: {ex.Message}");
            }
        }

        /// <summary>
        /// عكس التقرير السابق: المنتجات الأقل مبيعاً (الراكدة)
        /// </summary>
        public static List<ProductSalesReport> GetLeastSellingProducts(DateTime? startDate = null, DateTime? endDate = null, int limit = 10)
        {
            try
            {
                // نستخدم نفس المنطق لكن نأخذ الكل
                var report = GetTopSellingProducts(startDate, endDate, int.MaxValue);

                // نعكس الترتيب (الأقل أولاً)
                return [.. report.Where(r => r.QuantitySold > 0)
                           .OrderBy(r => r.QuantitySold)
                           .Take(limit)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير المنتجات الأقل مبيعاً: {ex.Message}");
            }
        }

        /// <summary>
        /// تقرير المخزون المنخفض مع توقعات النفاد
        /// </summary>
        public static List<LowStockReport> GetLowStockReport(int threshold = 10)
        {
            try
            {
                // جلب المنتجات التي قلت عن الحد
                var lowStockProducts = ProductDAL.GetLowStockProducts(threshold);
                var report = new List<LowStockReport>();

                foreach (var product in lowStockProducts)
                {
                    // حساب معدل البيع اليومي لتقدير متى سينفد المخزون
                    var salesItems = SaleItemDAL.GetSaleItemsByProduct(product.ProductID);
                    var totalSold = salesItems.Sum(item => item.Quantity);
                    // نفترض أن المبيعات تمت خلال 30 يوم (تقديري)
                    var daysOfSales = salesItems.Count > 0 ? 30 : 1;
                    var dailyAverage = (double)totalSold / daysOfSales;

                    // المعادلة: الكمية الحالية / معدل البيع اليومي = الأيام المتبقية
                    double daysRemaining = dailyAverage > 0 ? product.Quantity / dailyAverage : 999;

                    report.Add(new LowStockReport
                    {
                        ProductID = product.ProductID,
                        ProductName = product.Name,
                        Category = product.Category,
                        CurrentStock = product.Quantity,
                        MinimumStock = threshold,
                        DailyAverageSales = dailyAverage,
                        DaysRemaining = daysRemaining,
                        // تحديد درجة الخطر نصياً
                        Status = GetStockStatus(product.Quantity, threshold),
                        Urgency = GetStockUrgency(daysRemaining)
                    });
                }

                // الترتيب حسب الأيام المتبقية (الأكثر خطراً أولاً)
                return [.. report.OrderBy(r => r.DaysRemaining)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير المنتجات منخفضة المخزون: {ex.Message}");
            }
        }

        /// <summary>
        /// تقرير يوضح توزيع قيمة رأس المال (المخزون) على الفئات المختلفة
        /// </summary>
        public static List<InventoryValueReport> GetInventoryValueByCategory()
        {
            try
            {
                var products = ProductDAL.GetAllProducts();
                var report = new Dictionary<string, InventoryValueReport>();

                foreach (var product in products)
                {
                    var category = string.IsNullOrEmpty(product.Category) ? "غير مصنف" : product.Category;

                    if (!report.TryGetValue(category, out InventoryValueReport value))
                    {
                        value = new InventoryValueReport
                        {
                            Category = category,
                            ProductCount = 0,
                            TotalQuantity = 0,
                            TotalValue = 0,
                            AveragePrice = 0
                        };
                        report[category] = value;
                    }

                    value.ProductCount++;
                    value.TotalQuantity += product.Quantity;
                    value.TotalValue += product.TotalValue;
                }

                foreach (var item in report.Values)
                {
                    item.AveragePrice = item.TotalQuantity > 0 ? item.TotalValue / item.TotalQuantity : 0;
                }

                return [.. report.Values.OrderByDescending(r => r.TotalValue)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير قيمة المخزون حسب الفئة: {ex.Message}");
            }
        }

        /// <summary>
        /// تقرير معدل دوران المخزون (Inventory Turnover)
        /// يوضح كفاءة حركة المنتجات
        /// </summary>
        public static List<InventoryTurnoverReport> GetInventoryTurnoverReport(int daysPeriod = 30)
        {
            try
            {
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-daysPeriod);

                var products = ProductDAL.GetAllProducts();
                var report = new List<InventoryTurnoverReport>();

                foreach (var product in products)
                {
                    var salesItems = SaleItemDAL.GetSaleItemsByProduct(product.ProductID);
                    var quantitySold = salesItems.Sum(item => item.Quantity);
                    var averageInventory = product.Quantity; // تبسيط: نستخدم المخزون الحالي كمتوسط

                    // نسبة الدوران: الكمية المباعة / متوسط المخزون
                    var turnoverRatio = averageInventory > 0 ? (double)quantitySold / averageInventory : 0;
                    var daysToSell = turnoverRatio > 0 ? daysPeriod / turnoverRatio : 999;

                    report.Add(new InventoryTurnoverReport
                    {
                        ProductID = product.ProductID,
                        ProductName = product.Name,
                        Category = product.Category,
                        QuantitySold = quantitySold,
                        AverageInventory = averageInventory,
                        TurnoverRatio = turnoverRatio,
                        DaysToSell = daysToSell,
                        TurnoverRating = GetTurnoverRating(turnoverRatio)
                    });
                }

                return [.. report.OrderByDescending(r => r.TurnoverRatio)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير دوران المخزون: {ex.Message}");
            }
        }

        #endregion

        #region تقارير العملاء

        /// <summary>
        /// قائمة بأفضل العملاء بناءً على حجم مشترياتهم
        /// </summary>
        public static List<CustomerPurchaseReport> GetTopCustomers(DateTime? startDate = null, DateTime? endDate = null, int limit = 10)
        {
            if (startDate is null)
            {
                throw new ArgumentNullException(nameof(startDate));
            }

            if (endDate is null)
            {
                throw new ArgumentNullException(nameof(endDate));
            }

            try
            {
                var allCustomers = CustomerDAL.GetAllCustomers();
                var report = new List<CustomerPurchaseReport>();

                foreach (var customer in allCustomers)
                {
                    if (customer.PurchaseCount > 0)
                    {
                        report.Add(new CustomerPurchaseReport
                        {
                            CustomerID = customer.CustomerID,
                            CustomerName = customer.Name,
                            Phone = customer.Phone,
                            Email = customer.Email,
                            TotalPurchases = customer.TotalPurchases, // المبلغ الإجمالي
                            PurchaseCount = customer.PurchaseCount, // عدد الفواتير
                            // متوسط قيمة الفاتورة لهذا العميل
                            AveragePurchase = customer.PurchaseCount > 0 ? customer.TotalPurchases / customer.PurchaseCount : 0,
                            LastPurchaseDate = GetCustomerLastPurchaseDate(customer.CustomerID)
                        });
                    }
                }

                return [.. report.OrderByDescending(r => r.TotalPurchases).Take(limit)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير العملاء الأكثر شراءً: {ex.Message}");
            }
        }

        /// <summary>
        /// تقرير يقيم ولاء العملاء ويعطي توصيات للتعامل معهم
        /// </summary>
        public static List<CustomerLoyaltyReport> GetCustomerLoyaltyReport()
        {
            try
            {
                var customers = CustomerDAL.GetAllCustomers();
                var report = new List<CustomerLoyaltyReport>();

                foreach (var customer in customers)
                {
                    if (customer.PurchaseCount > 0)
                    {
                        // خوارزمية حساب نقاط الولاء
                        var loyaltyScore = CalculateLoyaltyScore(customer, 0.0);
                        var customerType = GetCustomerType(loyaltyScore);

                        report.Add(new CustomerLoyaltyReport
                        {
                            CustomerID = customer.CustomerID,
                            CustomerName = customer.Name,
                            Phone = customer.Phone,
                            TotalPurchases = customer.TotalPurchases,
                            PurchaseCount = customer.PurchaseCount,
                            AveragePurchase = customer.PurchaseCount > 0 ? customer.TotalPurchases / customer.PurchaseCount : 0,
                            DaysSinceLastPurchase = GetDaysSinceLastPurchase(customer.CustomerID),
                            LoyaltyScore = loyaltyScore,
                            CustomerType = customerType, // تصنيف نصي (ذهبي، قضي، الخ)
                            Recommendations = GetCustomerRecommendations(loyaltyScore) // اقتراحات تسويقية
                        });
                    }
                }

                return [.. report.OrderByDescending(r => r.LoyaltyScore)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير ولاء العملاء: {ex.Message}");
            }
        }

        /// <summary>
        /// تقرير بالعملاء الذين انضموا حديثاً (أول عملية شراء لهم كانت قريبة)
        /// </summary>
        public static List<NewCustomersReport> GetNewCustomersReport(int daysThreshold = 30)
        {
            try
            {
                var allCustomers = CustomerDAL.GetAllCustomers();
                var report = new List<NewCustomersReport>();
                var thresholdDate = DateTime.Today.AddDays(-daysThreshold);

                foreach (var customer in allCustomers)
                {
                    var firstPurchaseDate = GetCustomerFirstPurchaseDate(customer.CustomerID);

                    // إذا كان تاريخ أول عملية شراء أحدث من التاريخ المحدد
                    if (firstPurchaseDate >= thresholdDate)
                    {
                        report.Add(new NewCustomersReport
                        {
                            CustomerID = customer.CustomerID,
                            CustomerName = customer.Name,
                            Phone = customer.Phone,
                            RegistrationDate = firstPurchaseDate,
                            DaysSinceRegistration = (DateTime.Today - firstPurchaseDate).Days,
                            TotalPurchases = customer.TotalPurchases,
                            PurchaseCount = customer.PurchaseCount
                        });
                    }
                }

                return [.. report.OrderByDescending(r => r.RegistrationDate)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير العملاء الجدد: {ex.Message}");
            }
        }

        #endregion

        #region تقارير الموردين

        /// <summary>
        /// تقييم أداء الموردين بناءً على توفر المنتجات وقيمة المخزون
        /// </summary>
        public static List<SupplierPerformanceReport> GetSupplierPerformanceReport()
        {
            try
            {
                var suppliers = SupplierDAL.GetAllSuppliers();
                var report = new List<SupplierPerformanceReport>();

                foreach (var supplier in suppliers)
                {
                    var products = ProductDAL.GetAllProducts()
                        .Where(p => p.SupplierID == supplier.SupplierID)
                        .ToList();

                    var totalProducts = products.Count;
                    var totalStockValue = products.Sum(p => p.TotalValue);
                    var averagePrice = products.Count > 0 ? products.Average(p => p.Price) : 0;
                    var lowStockProducts = products.Count(p => p.Quantity <= 10);

                    report.Add(new SupplierPerformanceReport
                    {
                        SupplierID = supplier.SupplierID,
                        SupplierName = supplier.Name,
                        ContactPhone = supplier.Phone,
                        TotalProducts = totalProducts,
                        TotalStockValue = totalStockValue,
                        AverageProductPrice = averagePrice,
                        LowStockProducts = lowStockProducts,
                        // خوارزمية تقييم الأداء
                        PerformanceRating = CalculateSupplierPerformance(totalProducts, totalStockValue, lowStockProducts),
                        LastDeliveryDate = DateTime.Today
                    });
                }

                return [.. report.OrderByDescending(r => r.PerformanceRating)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير أداء الموردين: {ex.Message}");
            }
        }

        /// <summary>
        /// تحليل الموردين حسب الفئات التي يوردونها
        /// </summary>
        public static List<SupplierCategoryAnalysis> GetSupplierCategoryAnalysis()
        {
            try
            {
                var suppliers = SupplierDAL.GetAllSuppliers();
                var products = ProductDAL.GetAllProducts();
                var report = new Dictionary<string, SupplierCategoryAnalysis>();

                foreach (var product in products)
                {
                    if (product.SupplierID.HasValue && !string.IsNullOrEmpty(product.Category))
                    {
                        var category = product.Category;

                        if (!report.TryGetValue(category, out SupplierCategoryAnalysis value))
                        {
                            value = new SupplierCategoryAnalysis
                            {
                                Category = category,
                                SupplierCount = 0,
                                ProductCount = 0,
                                TotalValue = 0,
                                MainSupplier = string.Empty,
                                MainSupplierPercentage = 0
                            };
                            report[category] = value;
                        }

                        value.ProductCount++;
                        value.TotalValue += product.TotalValue;
                    }
                }

                // حساب عدد الموردين مختلفين لكل فئة
                foreach (var category in report.Keys.ToList())
                {
                    var categoryProducts = products.Where(p => p.Category == category).ToList();
                    var uniqueSuppliers = categoryProducts.Select(p => p.SupplierID).Distinct().Count();
                    report[category].SupplierCount = uniqueSuppliers;
                }

                return [.. report.Values.OrderByDescending(r => r.TotalValue)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد تقرير تحليل الموردين حسب الفئة: {ex.Message}");
            }
        }

        #endregion

        #region الدوال المساعدة (Helper Methods)

        // تحديد حالة المخزون كنص
        private static string GetStockStatus(int quantity, int threshold)
        {
            if (quantity <= threshold * 0.3) return "حرج";
            if (quantity <= threshold * 0.6) return "منخفض";
            if (quantity <= threshold) return "تحت الحد";
            return "طبيعي";
        }

        // تحديد مدى الاستعجال بناءً على الأيام المتبقية
        private static string GetStockUrgency(double daysRemaining)
        {
            if (daysRemaining <= 3) return "عاجل";
            if (daysRemaining <= 7) return "فوري";
            if (daysRemaining <= 14) return "قريب";
            return "عادي";
        }

        // تقييم معدل الدوران
        private static string GetTurnoverRating(double ratio)
        {
            if (ratio >= 2.0) return "ممتاز";
            if (ratio >= 1.0) return "جيد";
            if (ratio >= 0.5) return "متوسط";
            if (ratio > 0) return "ضعيف";
            return "لا يوجد مبيعات";
        }

        // جلب آخر تاريخ شراء للعميل
        private static DateTime GetCustomerLastPurchaseDate(int customerId)
        {
            try
            {
                var sales = SaleDAL.GetSalesByCustomer(customerId);
                return sales.Count != 0 ? sales.Max(s => s.SaleDate) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        // جلب أول تاريخ شراء للعميل
        private static DateTime GetCustomerFirstPurchaseDate(int customerId)
        {
            try
            {
                var sales = SaleDAL.GetSalesByCustomer(customerId);
                return sales.Count != 0 ? sales.Min(s => s.SaleDate) : DateTime.Today;
            }
            catch
            {
                return DateTime.Today;
            }
        }

        // حساب عدد الأيام منذ آخر شراء
        private static int GetDaysSinceLastPurchase(int customerId)
        {
            var lastPurchase = GetCustomerLastPurchaseDate(customerId);
            if (lastPurchase == DateTime.MinValue) return 999;
            return (DateTime.Today - lastPurchase).Days;
        }

        // خوارزمية حساب نقاط الولاء (0-100)
#pragma warning disable IDE0060 // Remove unused parameter
        private static double CalculateLoyaltyScore(Customer customer, double totalScore)
#pragma warning restore IDE0060 // Remove unused parameter
        {

            // 1. عامل المبلغ الإجمالي (40%) - كلما اشترى بمبلغ أكبر زادت النقاط
            var purchaseScore = Math.Min((double)customer.TotalPurchases / 10000.0, 1.0) * 40;

            // 2. عامل التكرار (30%) - كلما زاد عدد مرات الشراء
            var countScore = Math.Min(customer.PurchaseCount / 50.0, 1.0) * 30;

            // 3. متوسط الفاتورة (20%) - هل يشتري بكميات كبيرة؟
            var avgPurchase = customer.PurchaseCount > 0 ? customer.TotalPurchases / customer.PurchaseCount : 0;
            var avgScore = Math.Min((double)avgPurchase / 500.0, 1.0) * 20;

            // 4. الحداثة (Recency) (10%) - هل اشترى قريباً؟
            var daysSince = GetDaysSinceLastPurchase(customer.CustomerID);
            var daysScore = daysSince <= 7 ? 10 :
                          daysSince <= 30 ? 7 :
                          daysSince <= 90 ? 4 :
                          daysSince <= 180 ? 2 : 0;

            totalScore = purchaseScore + countScore + avgScore + daysScore;

            return Math.Min(totalScore, 100);
        }

        // تصنيف العميل بناءً على النقاط
        private static string GetCustomerType(double loyaltyScore)
        {
            if (loyaltyScore >= 80) return "عميل متميز";
            if (loyaltyScore >= 60) return "عميل دائم";
            if (loyaltyScore >= 40) return "عميل متكرر";
            if (loyaltyScore >= 20) return "عميل عادي";
            return "عميل جديد/نادر";
        }

        // توليد توصيات تسويقية
        private static List<string> GetCustomerRecommendations(double loyaltyScore)
        {
            var recommendations = new List<string>();

            if (loyaltyScore < 40)
            {
                recommendations.Add("تقديم عروض ترحيبية");
                recommendations.Add("إرسال تذكير بالمنتجات");
            }
            else if (loyaltyScore < 70)
            {
                recommendations.Add("برنامج الولاء والمكافآت");
                recommendations.Add("عروض خاصة للمنتجات المفضلة");
            }
            else
            {
                recommendations.Add("خصومات حصرية");
                recommendations.Add("هدايا تقديرية");
                recommendations.Add("خدمة عملاء متميزة");
            }

            return recommendations;
        }

        // خوارزمية تقييم المورد
        private static double CalculateSupplierPerformance(int productCount, decimal stockValue, int lowStockCount)
        {

            // حجم التعامل (عدد المنتجات) - 30%
            var productCountScore = Math.Min(productCount / 50.0, 1.0) * 30;

            // حجم التعامل المالي - 30%
            var stockValueScore = Math.Min((double)stockValue / 10000.0, 1.0) * 30;

            // الاعتمادية (قلة نفاد المخزون) - 40%
            var qualityScore = productCount > 0 ?
                (1.0 - (double)lowStockCount / productCount) * 40 : 40;

            var totalScore = productCountScore + stockValueScore + qualityScore;
            return Math.Min(Math.Max(totalScore, 0), 100);
        }

        #endregion
    }
}
