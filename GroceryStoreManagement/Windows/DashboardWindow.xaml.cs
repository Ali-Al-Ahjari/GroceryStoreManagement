using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة لوحة التحكم - تعرض ملخصاً سريعاً لحالة المتجر
    /// </summary>
    public partial class DashboardWindow : UserControl
    {
        public DashboardWindow()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            try
            {
                // 1. تحميل بيانات الملخص
                var summary = ReportHelper.GetDashboardSummary();

                // مبيعات اليوم
                decimal todaySales = SaleDAL.GetDailySalesTotal(DateTime.Today);
                TodaySalesText.Text = todaySales.ToDisplayCurrency();

                // عدد فواتير اليوم
                var todayInvoices = SaleDAL.GetSalesByDateRange(DateTime.Today, DateTime.Today);
                TodaySalesCountText.Text = $"{todayInvoices.Count} فاتورة";

                // إجمالي الفواتير
                var allSales = SaleDAL.GetAllSales();
                TotalInvoicesText.Text = allSales.Count.ToString();

                // فواتير هذا الشهر
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var monthlyInvoices = SaleDAL.GetSalesByDateRange(startOfMonth, DateTime.Today);
                MonthlyInvoicesText.Text = $"{monthlyInvoices.Count} هذا الشهر";

                // إجمالي العملاء
                var allCustomers = CustomerDAL.GetAllCustomers();
                TotalCustomersText.Text = allCustomers.Count.ToString();

                // العملاء الجدد هذا الشهر
                var newCustomers = ReportHelper.GetNewCustomersReport(30);
                NewCustomersText.Text = $"+{newCustomers.Count} جديد هذا الشهر";

                // إجمالي المنتجات
                var allProducts = ProductDAL.GetAllProducts();
                TotalProductsText.Text = allProducts.Count.ToString();

                // المنتجات منخفضة المخزون وفق نفس معيار النظام الموحد
                var lowStockProducts = allProducts.Where(p => p.IsLowStock).ToList();
                LowStockCountText.Text = $"{lowStockProducts.Count} منخفض المخزون";

                // إجمالي المبيعات الشهرية
                TotalSalesText.Text = summary.MonthlySales.ToDisplayCurrency();

                // متوسط قيمة الفاتورة
                var monthlyReport = ReportHelper.GetMonthlySalesReport(DateTime.Today.Year, DateTime.Today.Month);
                AvgOrderValueText.Text = monthlyReport.AverageTransactionValue.ToDisplayCurrency();

                // 2. تحميل الرسوم البيانية
                LoadSalesChart();
                LoadCategoryChart();

                // 3. تحميل التنبيهات (المخزون وانتهاء الصلاحية)
                var expiringProducts = ProductDAL.GetExpiringProducts(30); // خلال 30 يوم
                LoadAlerts(lowStockProducts, expiringProducts);

                // 4. تحميل النشاطات الأخيرة
                LoadRecentActivities();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل بيانات لوحة التحكم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSalesChart()
        {
            try
            {
                var salesValues = new ChartValues<decimal>();
                var dates = new List<string>();

                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var total = SaleDAL.GetDailySalesTotal(date);
                    salesValues.Add(total);
                    dates.Add(date.ToString("dd/MM"));
                }

                var chart = new CartesianChart
                {
                    Series =
                    [
                        new LineSeries
                        {
                            Title = "المبيعات",
                            Values = salesValues,
                            PointGeometry = DefaultGeometries.Circle,
                            PointGeometrySize = 10,
                            LineSmoothness = 1,
                            Stroke = (Brush)FindResource("PrimaryBrush"),
                            Fill = new SolidColorBrush(Color.FromArgb(50, 67, 121, 238)),
                            StrokeThickness = 3
                        }
                    ],
                    AxisX =
                    [
                        new Axis
                        {
                            Labels = dates,
                            Separator = new LiveCharts.Wpf.Separator { Step = 1, StrokeThickness = 0 }
                        }
                    ],
                    AxisY =
                    [
                        new Axis
                        {
                            LabelFormatter = value => value.ToDisplayCurrency(),
                            Separator = new LiveCharts.Wpf.Separator { StrokeThickness = 0.5, Stroke = Brushes.LightGray }
                        }
                    ],
                    DataTooltip = new DefaultTooltip
                    {
                        SelectionMode = TooltipSelectionMode.SharedYValues
                    }
                };

                SalesChartContainer.Content = chart;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sales chart: {ex.Message}");
            }
        }

        private void LoadCategoryChart()
        {
            try
            {
                var topCategories = ReportHelper.GetTopSellingCategories(5);
                var values = new ChartValues<decimal>();
                var labels = new List<string>();

                foreach (var cat in topCategories)
                {
                    values.Add(cat.TotalRevenue);
                    labels.Add(cat.Category);
                }

                var chart = new CartesianChart
                {
                    Series =
                    [
                        new ColumnSeries
                        {
                            Title = "الإيرادات",
                            Values = values,
                            Fill = (Brush)FindResource("SecondaryBrush"),
                            DataLabels = true,
                            LabelPoint = point => point.Y.ToDisplayCurrency()
                        }
                    ],
                    AxisX =
                    [
                        new Axis
                        {
                            Labels = labels,
                            Separator = new LiveCharts.Wpf.Separator { Step = 1, StrokeThickness = 0 }
                        }
                    ],
                    AxisY =
                    [
                        new Axis
                        {
                            LabelFormatter = value => value.ToDisplayCurrency(),
                            Separator = new LiveCharts.Wpf.Separator { StrokeThickness = 0.5, Stroke = Brushes.LightGray }
                        }
                    ]
                };

                CategoryChartContainer.Content = chart;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading category chart: {ex.Message}");
            }
        }

        private void RefreshDashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Already called in Constructor, but good for refresh
            LoadDashboardData();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Cleanup if needed
        }


        private void LoadAlerts(List<Product> lowStockProducts, List<Product> expiringProducts)
        {
            try
            {
                var alerts = new List<AlertItem>();

                // 1. إضافة تنبيهات انتهاء الصلاحية
                foreach (var p in expiringProducts)
                {
                    if (p.IsExpired)
                    {
                        alerts.Add(new AlertItem
                        {
                            Name = p.Name,
                            Message = "منتهي الصلاحية منذ " + (DateTime.Today - p.ExpiryDate.Value.Date).Days + " يوم",
                            Type = "Expiry",
                            Color = "#FF5252", // أحمر
                            Icon = "📅"
                        });
                    }
                    else
                    {
                        alerts.Add(new AlertItem
                        {
                            Name = p.Name,
                            Message = "ينتهي خلال " + p.DaysUntilExpiry + " يوم",
                            Type = "ExpirySoon",
                            Color = "#FFC107", // برتقالي
                            Icon = "⏳"
                        });
                    }
                }

                // 2. إضافة تنبيهات المخزون المنخفض
                foreach (var p in lowStockProducts)
                {
                    // تجنب التكرار إذا كان المنتج منتهي الصلاحية ومخزونه منخفض في نفس الوقت
                    // (يمكنك الاختيار: عرض التنبيهين أو أحدهما. هنا سنعرض الاثنين)
                    alerts.Add(new AlertItem
                    {
                        Name = p.Name,
                        Message = $"الكمية المتبقية: {p.Quantity}",
                        Type = "LowStock",
                        Color = "#FF9800", // أصفر غامق
                        Icon = "📦"
                    });
                }

                // ترتيب التنبيهات: المنتهي أولاً، ثم قليل المخزون، ثم القريب من الانتهاء
                alerts = [.. alerts.OrderBy(a => a.Type == "Expiry" ? 0 : (a.Type == "LowStock" ? 1 : 2))];

                if (alerts.Count > 0)
                {
                    LowStockGrid.ItemsSource = alerts.Take(10).ToList(); // عرض 10 تنبيهات
                    LowStockGrid.Visibility = Visibility.Visible;
                    NoAlertsPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LowStockGrid.Visibility = Visibility.Collapsed;
                    NoAlertsPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading alerts: {ex.Message}");
            }
        }

        // كلاس داخلي لتمثيل عنصر التنبيه
        public class AlertItem
        {
            public string Name { get; set; }
            public string Message { get; set; }
            public string Type { get; set; }
            public string Color { get; set; }
            public string Icon { get; set; }
        }

        private void LoadRecentActivities()
        {
            try
            {
                var activities = ActivityLogDAL.GetRecentLogs(8);
                ActivityLogGrid.ItemsSource = activities;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading activities: {ex.Message}");
            }
        }

        // =========================================================
        // Event Handlers - Quick Action Buttons
        // =========================================================

        private void QuickAddInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftDAL.GetOpenShift() == null)
            {
                _ = MessageBox.Show("لا يمكن إنشاء فاتورة قبل فتح وردية من شاشة المبيعات.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saleDialog = new SaleDialog();
            if (saleDialog.ShowDialog() == true)
            {
                LoadDashboardData();
                ActivityLogDAL.AddLog(null, "إضافة فاتورة", "تم إضافة فاتورة جديدة");
            }
        }

        private void QuickAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var customerDialog = new CustomerDialog();
            if (customerDialog.ShowDialog() == true)
            {
                LoadDashboardData();
                ActivityLogDAL.AddLog(null, "إضافة عميل", "تم إضافة عميل جديد");
            }
        }

        private void QuickAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var productDialog = new ProductDialog();
            if (productDialog.ShowDialog() == true)
            {
                LoadDashboardData();
                ActivityLogDAL.AddLog(null, "إضافة منتج", "تم إضافة منتج جديد");
            }
        }

        private void QuickGoToReports_Click(object sender, RoutedEventArgs e)
        {
            // البحث عن MainWindow والانتقال للتقارير
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToReports();
            }
            else
            {
                _ = MessageBox.Show("يرجى استخدام القائمة الجانبية للانتقال للتقارير.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void QuickOpenReports_Click(object sender, RoutedEventArgs e)
        {
            // البحث عن MainWindow والانتقال للتقارير
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToReports();
            }
            else
            {
                _ = MessageBox.Show("يرجى استخدام القائمة الجانبية للانتقال للتقارير.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void QuickGoToSettings_Click(object sender, RoutedEventArgs e)
        {
            // البحث عن MainWindow والانتقال للإعدادات
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToSettings();
            }
            else
            {
                _ = MessageBox.Show("يرجى استخدام القائمة الجانبية للانتقال للإعدادات.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void QuickOpenSettings_Click(object sender, RoutedEventArgs e)
        {
            // البحث عن MainWindow والانتقال للإعدادات
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToSettings();
            }
            else
            {
                _ = MessageBox.Show("يرجى استخدام القائمة الجانبية للانتقال للإعدادات.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ActivityLogGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
