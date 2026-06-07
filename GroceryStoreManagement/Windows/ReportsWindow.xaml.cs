using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة التقارير الشاملة - تعرض جميع أنواع التقارير مع أدوات التصدير والطباعة
    /// </summary>
    public partial class ReportsWindow : UserControl
    {
        // نوع التقرير الحالي
        private string _currentReportType = "Sales";

        // بيانات التقرير الحالي
        private DateTime _fromDate;
        private DateTime _toDate;

        // سلاسل الرسوم البيانية
        public SeriesCollection MainSeries { get; set; } = [];
        public SeriesCollection PieSeries { get; set; } = [];
        public string[] Dates { get; set; } = [];

        // دالة تنسيق المحور العمودي
        public Func<double, string> YFormatter { get; set; }

        // عناصر التحكم للفلاتر الديناميكية
        private ComboBox _customerFilter;
        private CheckBox _overdueOnlyFilter;
        private CancellationTokenSource _reportLoadCts;

        /// <summary>
        /// المُنشئ
        /// </summary>
        public ReportsWindow()
        {
            InitializeComponent();
            InitializeDates();
            InitializeCharts();
            DataContext = this;
            this.Loaded += ReportsWindow_Loaded;
        }

        private async void ReportsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateReportUI(); // Ensure UI text is correct
            await LoadReportAsync();
        }

        /// <summary>
        /// تهيئة التواريخ الافتراضية
        /// </summary>
        private void InitializeDates()
        {
            _toDate = DateTime.Today;
            _fromDate = _toDate.AddDays(-30);

            _ = (FromDatePicker?.SelectedDate = _fromDate);
            _ = (ToDatePicker?.SelectedDate = _toDate);
        }

        /// <summary>
        /// تهيئة الرسوم البيانية
        /// </summary>
        private void InitializeCharts()
        {
            MainSeries = [];
            PieSeries = [];
            YFormatter = value => value.ToString("N0");
        }

        #region أحداث تغيير نوع التقرير

        /// <summary>
        /// معالج تغيير نوع التقرير
        /// </summary>
        /// <summary>
        /// معالج تغيير نوع التقرير
        /// </summary>
        private async void CmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedIndex == -1) return;

            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            var content = selectedItem?.Content?.ToString();

            // تحديد نوع التقرير بناءً على العنصر المحدد (نفترض أن XAML يحتوي على محتوى نصي يمكن استخدامه أو استخدام Tag)
            // بما أن المحتوى بالعربية، يمكننا الاعتماد على الترتيب أو النص

            _currentReportType = comboBox.SelectedIndex switch
            {
                0 => "Sales",
                1 => "Purchases",
                2 => "Inventory",
                3 => "Customers",
                4 => "Profits",
                5 => "ActivityLogs",
                6 => "TopSelling",
                7 => "Unpaid",
                _ => "Sales",
            };

            // أو بدلاً من الترتيب، نستخدم المحتوى إذا كان ثابتاً
            if (content == "تقرير المبيعات") _currentReportType = "Sales";
            else if (content == "تقرير المشتريات") _currentReportType = "Purchases";
            else if (content == "تقرير المخزون") _currentReportType = "Inventory";
            else if (content == "تقرير العملاء") _currentReportType = "Customers";
            else if (content == "تقرير الأرباح") _currentReportType = "Profits";
            else if (content == "تقرير المنتجات الأكثر مبيعاً") _currentReportType = "TopSelling";
            else if (content == "سجل النشاطات") _currentReportType = "ActivityLogs";
            else if (content == "الفواتير غير المدفوعة") _currentReportType = "Unpaid";

            UpdateReportUI();
            SetupDataGridColumns();
            await LoadReportAsync();
        }

        /// <summary>
        /// تحديث واجهة المستخدم حسب نوع التقرير
        /// </summary>
        private void UpdateReportUI()
        {
            if (CurrentReportIcon == null || CurrentReportTitle == null || CurrentReportSubtitle == null ||
                ChartTitle == null || PieChartTitle == null || DataGridTitle == null || AdditionalFilterPanel == null)
                return;

            switch (_currentReportType)
            {
                case "Sales":
                    CurrentReportIcon.Text = "💰";
                    CurrentReportTitle.Text = "تقرير المبيعات";
                    CurrentReportSubtitle.Text = "عرض تفاصيل المبيعات وتحليل الأداء";
                    ChartTitle.Text = "تحليل المبيعات اليومية";
                    PieChartTitle.Text = "توزيع المبيعات حسب طريقة الدفع";
                    DataGridTitle.Text = "تفاصيل فواتير المبيعات";
                    UpdateSalesCards();
                    AdditionalFilterPanel.Children.Clear();
                    break;

                case "Purchases":
                    CurrentReportIcon.Text = "🛒";
                    CurrentReportTitle.Text = "تقرير المشتريات";
                    CurrentReportSubtitle.Text = "عرض تفاصيل المشتريات من الموردين";
                    ChartTitle.Text = "تحليل المشتريات اليومية";
                    PieChartTitle.Text = "توزيع المشتريات حسب المورد";
                    DataGridTitle.Text = "تفاصيل فواتير المشتريات";
                    UpdatePurchasesCards();
                    AdditionalFilterPanel.Children.Clear();
                    break;

                case "Customers":
                    CurrentReportIcon.Text = "👥";
                    CurrentReportTitle.Text = "تقرير العملاء";
                    CurrentReportSubtitle.Text = "تحليل أداء العملاء والمشتريات";
                    ChartTitle.Text = "نشاط العملاء";
                    PieChartTitle.Text = "توزيع العملاء حسب المشتريات";
                    DataGridTitle.Text = "قائمة العملاء";
                    UpdateCustomersCards();
                    AdditionalFilterPanel.Children.Clear();
                    break;

                case "Suppliers":
                    CurrentReportIcon.Text = "🏢";
                    CurrentReportTitle.Text = "تقرير الموردين";
                    CurrentReportSubtitle.Text = "تحليل التوريدات والمدفوعات للموردين";
                    ChartTitle.Text = "قيمة التوريدات حسب المورد";
                    PieChartTitle.Text = "توزيع التوريدات";
                    DataGridTitle.Text = "قائمة الموردين";
                    UpdateSuppliersCards();
                    AdditionalFilterPanel.Children.Clear();
                    break;

                case "Inventory":
                    CurrentReportIcon.Text = "📦";
                    CurrentReportTitle.Text = "تقرير المخزون";
                    CurrentReportSubtitle.Text = "حالة المخزون والمنتجات";
                    ChartTitle.Text = "توزيع المخزون حسب الفئة";
                    PieChartTitle.Text = "حالة المخزون";
                    DataGridTitle.Text = "قائمة المنتجات";
                    UpdateInventoryCards();
                    AdditionalFilterPanel.Children.Clear();
                    break;

                case "Profits":
                    CurrentReportIcon.Text = "📈";
                    CurrentReportTitle.Text = "تقرير الأرباح";
                    CurrentReportSubtitle.Text = "تحليل الإيرادات والمصروفات والأرباح";
                    ChartTitle.Text = "الأرباح اليومية";
                    PieChartTitle.Text = "مقارنة الإيرادات والمصروفات";
                    DataGridTitle.Text = "تفاصيل الأرباح";
                    UpdateProfitsCards();
                    AdditionalFilterPanel.Children.Clear();
                    break;

                case "Unpaid":
                    CurrentReportIcon.Text = "⚠️";
                    CurrentReportTitle.Text = "الفواتير غير المدفوعة";
                    CurrentReportSubtitle.Text = "فواتير المبيعات والمشتريات المستحقة";
                    ChartTitle.Text = "المبالغ المستحقة";
                    PieChartTitle.Text = "توزيع المستحقات";
                    DataGridTitle.Text = "الفواتير غير المدفوعة";
                    UpdateUnpaidCards();
                    SetupUnpaidFilters();
                    break;

                case "ActivityLogs":
                    AdditionalFilterPanel.Children.Clear();
                    CurrentReportIcon.Text = "📋";
                    CurrentReportTitle.Text = "سجل النشاطات";
                    CurrentReportSubtitle.Text = "سجل جميع العمليات في النظام";
                    ChartTitle.Text = "النشاطات اليومية";
                    PieChartTitle.Text = "توزيع النشاطات";
                    DataGridTitle.Text = "سجل النشاطات";
                    UpdateActivityLogsCards();
                    break;

                case "TopSelling":
                    CurrentReportIcon.Text = "🔥";
                    CurrentReportTitle.Text = "المنتجات الأكثر مبيعاً";
                    CurrentReportSubtitle.Text = "تحليل أداء المنتجات ومعدلات البيع";
                    ChartTitle.Text = "الكميات المباعة";
                    PieChartTitle.Text = "الإيرادات حسب المنتج";
                    DataGridTitle.Text = "قائمة المنتجات الأكثر مبيعاً";
                    UpdateTopSellingCards();
                    AdditionalFilterPanel.Children.Clear();
                    break;
            }
            SetupDataGridColumns(); // تحديث أعمدة الجدول عند تغيير النوع
        }

        #endregion

        #region تحميل التقارير

        /// <summary>
        /// زر توليد التقرير
        /// </summary>
        private async void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        /// <summary>
        /// تحميل التقرير الحالي
        /// </summary>
        private async Task LoadReportAsync()
        {
            try
            {
                // التحقق من التواريخ
                if (FromDatePicker != null && FromDatePicker.SelectedDate.HasValue)
                    _fromDate = FromDatePicker.SelectedDate.Value;
                if (ToDatePicker != null && ToDatePicker.SelectedDate.HasValue)
                    _toDate = ToDatePicker.SelectedDate.Value;

                if (_fromDate > _toDate)
                {
                    _ = MessageBox.Show("تاريخ البداية يجب أن يكون قبل تاريخ النهاية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _reportLoadCts?.Cancel();
                _reportLoadCts?.Dispose();
                _reportLoadCts = new CancellationTokenSource();
                var token = _reportLoadCts.Token;

                SetReportLoadingState(true);

                // تحميل التقرير حسب النوع
                switch (_currentReportType)
                {
                    case "Sales": await LoadSalesReportAsync(token); break;
                    case "Purchases": await LoadPurchasesReportAsync(token); break;
                    case "Customers": await LoadCustomersReportAsync(token); break;
                    case "Suppliers": await LoadSuppliersReportAsync(token); break;
                    case "Inventory": await LoadInventoryReportAsync(token); break;
                    case "Profits": await LoadProfitsReportAsync(token); break;
                    case "Unpaid": await LoadUnpaidReportAsync(token); break;
                    case "ActivityLogs": await LoadActivityLogsReportAsync(token); break;
                    case "TopSelling": await LoadTopSellingReportAsync(token); break;
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore stale request.
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetReportLoadingState(false);
            }
        }

        private void SetReportLoadingState(bool isLoading)
        {
            BtnGenerateReport?.SetCurrentValue(UIElement.IsEnabledProperty, !isLoading);
            BtnExportExcel?.SetCurrentValue(UIElement.IsEnabledProperty, !isLoading);
            CmbReportType?.SetCurrentValue(UIElement.IsEnabledProperty, !isLoading);
            Mouse.OverrideCursor = isLoading ? Cursors.Wait : null;
        }

        /// <summary>
        /// تحميل تقرير المبيعات
        /// </summary>
        private async Task LoadSalesReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var salesTask = SaleDAL.GetSalesByDateRangeAsync(_fromDate, _toDate);
            var dailyTask = SaleDAL.GetDailySalesTotalsInRangeAsync(_fromDate, _toDate);

            await Task.WhenAll(salesTask, dailyTask);
            token.ThrowIfCancellationRequested();

            var sales = salesTask.Result;
            var daily = dailyTask.Result;

            decimal totalSales = sales.Sum(s => s.NetTotal);
            int invoiceCount = sales.Count;
            int productsSold = sales.Sum(s => s.ItemCount);

            Card1Value.Text = totalSales.ToDisplayCurrency();
            Card2Value.Text = invoiceCount.ToString();
            Card3Value.Text = productsSold.ToString();
            Card4Value.Text = (invoiceCount > 0 ? totalSales / invoiceCount : 0).ToDisplayCurrency();

            ReportDataGrid.ItemsSource = sales;
            DataGridCount.Text = $"{sales.Count} سجل";

            LoadSalesChart(daily);
            LoadSalesPieChart(sales);
        }

        /// <summary>
        /// تحميل تقرير المشتريات
        /// </summary>
        private async Task LoadPurchasesReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var allPurchases = await PurchaseDAL.GetAllPurchasesAsync();
            token.ThrowIfCancellationRequested();

            var purchases = allPurchases
                .Where(p => p.PurchaseDate.Date >= _fromDate && p.PurchaseDate.Date <= _toDate)
                .ToList();

            decimal totalPurchases = purchases.Sum(p => p.TotalAmount);
            decimal totalPaid = purchases.Sum(p => p.PaidAmount);
            int invoiceCount = purchases.Count;
            int itemCount = purchases.Sum(p => p.ItemCount);

            Card1Value.Text = totalPurchases.ToDisplayCurrency();
            Card2Value.Text = invoiceCount.ToString();
            Card3Value.Text = itemCount.ToString();
            Card4Value.Text = (totalPurchases - totalPaid).ToDisplayCurrency();

            ReportDataGrid.ItemsSource = purchases;
            DataGridCount.Text = $"{purchases.Count} سجل";

            LoadPurchasesChart(purchases);
        }

        /// <summary>
        /// تحميل تقرير العملاء
        /// </summary>
        private async Task LoadCustomersReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var customers = await CustomerDAL.GetAllCustomersAsync();
            token.ThrowIfCancellationRequested();

            decimal totalPurchases = customers.Sum(c => c.TotalPurchases);
            int activeCustomers = customers.Count(c => c.PurchaseCount > 0);
            decimal avgPurchase = activeCustomers > 0 ? totalPurchases / activeCustomers : 0;

            Card1Value.Text = customers.Count.ToString();
            Card2Value.Text = activeCustomers.ToString();
            Card3Value.Text = totalPurchases.ToDisplayCurrency();
            Card4Value.Text = avgPurchase.ToDisplayCurrency();

            ReportDataGrid.ItemsSource = customers;
            DataGridCount.Text = $"{customers.Count} عميل";

            LoadCustomersChart(customers);
        }

        /// <summary>
        /// تحميل تقرير الموردين
        /// </summary>
        private async Task LoadSuppliersReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var suppliers = await Task.Run(SupplierDAL.GetAllSuppliers, token);
            token.ThrowIfCancellationRequested();

            decimal totalSupplied = suppliers.Sum(s => s.TotalSuppliedValue);
            int activeSuppliers = suppliers.Count(s => s.TotalSuppliedValue > 0);

            Card1Value.Text = suppliers.Count.ToString();
            Card2Value.Text = activeSuppliers.ToString();
            Card3Value.Text = totalSupplied.ToDisplayCurrency();
            Card4Value.Text = (activeSuppliers > 0 ? totalSupplied / activeSuppliers : 0).ToDisplayCurrency();

            ReportDataGrid.ItemsSource = suppliers;
            DataGridCount.Text = $"{suppliers.Count} مورد";
        }

        /// <summary>
        /// تحميل تقرير المخزون
        /// </summary>
        private async Task LoadInventoryReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var products = await ProductDAL.GetAllProductsAsync();
            token.ThrowIfCancellationRequested();

            int totalProducts = products.Count;
            int lowStock = products.Count(p => p.Quantity <= p.MinQuantity);
            decimal totalValue = products.Sum(p => p.SellingPrice * p.Quantity);
            int totalQuantity = products.Sum(p => p.Quantity);

            Card1Value.Text = totalProducts.ToString();
            Card2Value.Text = lowStock.ToString();
            Card3Value.Text = totalValue.ToDisplayCurrency();
            Card4Value.Text = totalQuantity.ToString();

            ReportDataGrid.ItemsSource = products;
            DataGridCount.Text = $"{products.Count} منتج";

            LoadInventoryChart(products);
        }

        /// <summary>
        /// تحميل تقرير الأرباح
        /// </summary>
        private async Task LoadProfitsReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var salesTask = SaleDAL.GetSalesByDateRangeAsync(_fromDate, _toDate);
            var purchasesTask = PurchaseDAL.GetAllPurchasesAsync();
            await Task.WhenAll(salesTask, purchasesTask);
            token.ThrowIfCancellationRequested();

            var sales = salesTask.Result;
            var purchases = purchasesTask.Result
                .Where(p => p.PurchaseDate.Date >= _fromDate && p.PurchaseDate.Date <= _toDate)
                .ToList();

            decimal totalRevenue = sales.Sum(s => s.NetTotal);
            decimal totalCost = purchases.Sum(p => p.TotalAmount);
            decimal profit = totalRevenue - totalCost;
            decimal profitMargin = totalRevenue > 0 ? (profit / totalRevenue) * 100 : 0;

            Card1Value.Text = totalRevenue.ToDisplayCurrency();
            Card2Value.Text = totalCost.ToDisplayCurrency();
            Card3Value.Text = profit.ToDisplayCurrency();
            Card4Value.Text = profitMargin.ToString("N1") + "%";

            LoadProfitsChart(totalRevenue, totalCost, profit);
        }

        /// <summary>
        /// تحميل تقرير الفواتير غير المدفوعة
        /// </summary>
        private async Task LoadUnpaidReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var salesTask = SaleDAL.GetAllSalesAsync();
            var purchasesTask = PurchaseDAL.GetAllPurchasesAsync();
            await Task.WhenAll(salesTask, purchasesTask);
            token.ThrowIfCancellationRequested();

            var unpaidSales = salesTask.Result
                .Where(s => s.PaymentStatus != "Paid")
                .ToList();

            var unpaidPurchases = purchasesTask.Result
                .Where(p => p.PaymentStatus != "Paid")
                .ToList();

            decimal totalUnpaidSales = unpaidSales.Sum(s => s.RemainingAmount);
            decimal totalUnpaidPurchases = unpaidPurchases.Sum(p => p.RemainingAmount);

            Card1Value.Text = unpaidSales.Count.ToString();
            Card2Value.Text = totalUnpaidSales.ToDisplayCurrency();
            Card3Value.Text = unpaidPurchases.Count.ToString();
            Card4Value.Text = totalUnpaidPurchases.ToDisplayCurrency();

            var rows = new List<UnpaidInvoiceRow>(unpaidSales.Count + unpaidPurchases.Count);
            rows.AddRange(unpaidSales.Select(s => new UnpaidInvoiceRow
            {
                Type = "مبيعات",
                ID = s.SaleID,
                Name = s.CustomerName ?? "عميل نقدي",
                Total = s.NetTotal,
                Paid = s.PaidAmount,
                Remaining = s.RemainingAmount,
                Date = s.SaleDate,
                CustomerID = s.CustomerID,
                DueDate = s.DueDate
            }));
            rows.AddRange(unpaidPurchases.Select(p => new UnpaidInvoiceRow
            {
                Type = "مشتريات",
                ID = p.PurchaseID,
                Name = p.SupplierName ?? "مورد",
                Total = p.TotalAmount,
                Paid = p.PaidAmount,
                Remaining = p.RemainingAmount,
                Date = p.PurchaseDate
            }));

            IEnumerable<UnpaidInvoiceRow> filtered = rows;
            if (_customerFilter?.SelectedValue is int customerId && customerId != -1)
            {
                filtered = filtered.Where(x => x.Type == "مبيعات" && x.CustomerID == customerId);
            }

            if (_overdueOnlyFilter?.IsChecked == true)
            {
                filtered = filtered.Where(x => x.Type == "مبيعات" && x.IsOverdue);
            }

            var finalList = filtered.ToList();
            ReportDataGrid.ItemsSource = finalList;
            DataGridCount.Text = $"{finalList.Count} فاتورة";
        }

        /// <summary>
        /// تحميل سجل النشاطات
        /// </summary>
        private async Task LoadActivityLogsReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var logs = await Task.Run(() => DAL.ActivityLogDAL.GetLogsByDateRange(_fromDate, _toDate), token);
            token.ThrowIfCancellationRequested();

            int totalLogs = logs.Count;
            int activeUsers = logs.Select(l => l.UserID).Distinct().Count();
            int addOps = logs.Count(l => l.Action == "Add");
            int updateOps = logs.Count(l => l.Action == "Update");

            Card1Value.Text = totalLogs.ToString();
            Card2Value.Text = activeUsers.ToString();
            Card3Value.Text = addOps.ToString();
            Card4Value.Text = updateOps.ToString();

            ReportDataGrid.ItemsSource = logs;
            DataGridCount.Text = $"{logs.Count} سجل";

            LoadActivityLogsChart(logs);
        }

        /// <summary>
        /// تحميل تقرير المنتجات الأكثر مبيعاً
        /// </summary>
        private async Task LoadTopSellingReportAsync(CancellationToken token)
        {
            if (Card1Value == null) return;

            var topProducts = await SaleDAL.GetTopSellingProductsAsync(_fromDate, _toDate);
            token.ThrowIfCancellationRequested();

            decimal totalRevenue = topProducts.Sum(p => p.TotalRevenue);
            int totalSold = topProducts.Sum(p => p.QuantitySold);
            int productCount = topProducts.Count;

            Card1Value.Text = totalSold.ToString();
            Card2Value.Text = productCount.ToString();
            Card3Value.Text = totalRevenue.ToDisplayCurrency();
            Card4Value.Text = (totalSold > 0 ? totalRevenue / totalSold : 0).ToDisplayCurrency();

            ReportDataGrid.ItemsSource = topProducts;
            DataGridCount.Text = $"{topProducts.Count} منتج";

            LoadTopSellingChart(topProducts);
        }

        private void LoadTopSellingChart(List<ProductSalesReport> topProducts)
        {
            try
            {
                var top = topProducts
                    .OrderByDescending(p => p.QuantitySold)
                    .Take(8)
                    .ToList();

                MainSeries.Clear();
                MainSeries.Add(new ColumnSeries
                {
                    Title = "الكمية المباعة",
                    Values = new ChartValues<int>(top.Select(x => x.QuantitySold))
                });
                _ = (MainChart?.Series = MainSeries);

                PieSeries.Clear();
                foreach (var item in top)
                {
                    PieSeries.Add(new PieSeries
                    {
                        Title = item.ProductName,
                        Values = new ChartValues<decimal> { item.TotalRevenue }
                    });
                }
                _ = (PieChart?.Series = PieSeries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل مخطط المنتجات الأكثر مبيعاً");
            }
        }

        #endregion

        #region إعداد أعمدة الجدول ديناميكياً

        /// <summary>
        /// إعداد أعمدة الجدول بناءً على نوع التقرير
        /// </summary>
        private void SetupDataGridColumns()
        {
            if (ReportDataGrid == null) return;

            ReportDataGrid.Columns.Clear();

            switch (_currentReportType)
            {
                case "Sales":
                    AddColumn("SaleID", "رقم الفاتورة", 80);
                    AddColumn("CustomerName", "العميل", 150);
                    AddColumn("SaleDate", "التاريخ", 120, "{0:yyyy/MM/dd}");
                    AddColumn("NetTotal", "الإجمالي", 100, "{0:C2}");
                    AddColumn("PaidAmount", "المدفوع", 100, "{0:C2}");
                    AddColumn("PaymentStatusText", "الحالة", 100);
                    break;

                case "Purchases":
                    AddColumn("PurchaseID", "رقم الفاتورة", 80);
                    AddColumn("SupplierName", "المورد", 150);
                    AddColumn("PurchaseDate", "التاريخ", 120, "{0:yyyy/MM/dd}");
                    AddColumn("TotalAmount", "الإجمالي", 100, "{0:C2}");
                    AddColumn("PaymentStatusText", "الحالة", 100);
                    break;

                case "Customers":
                    AddColumn("Name", "اسم العميل", 150);
                    AddColumn("Phone", "الهاتف", 120);
                    AddColumn("TotalPurchases", "إجمالي المشتريات", 130, "{0:C2}");
                    AddColumn("PurchaseCount", "عدد الزيارات", 100);
                    break;

                case "Suppliers":
                    AddColumn("Name", "اسم المورد", 150);
                    AddColumn("Phone", "الهاتف", 120);
                    AddColumn("TotalSuppliedValue", "إجمالي التوريدات", 130, "{0:C2}");
                    break;

                case "Inventory":
                    AddColumn("Name", "المنتج", 150);
                    AddColumn("Category", "الفئة", 100);
                    AddColumn("Quantity", "الكمية", 80);
                    AddColumn("SellingPrice", "السعر", 100, "{0:C2}");
                    AddColumn("MinQuantity", "حد الطلب", 80);
                    break;

                case "Unpaid":
                    AddColumn("Type", "النوع", 80);
                    AddColumn("ID", "رقم الفاتورة", 80);
                    AddColumn("Name", "العميل/المورد", 150);
                    AddColumn("Date", "التاريخ", 120, "{0:yyyy/MM/dd}");
                    AddColumn("Total", "الإجمالي", 100, "{0:C2}");
                    AddColumn("Paid", "المدفوع", 100, "{0:C2}");
                    AddColumn("Remaining", "المتبقي", 100, "{0:C2}");
                    break;

                case "ActivityLogs":
                    AddColumn("LogDate", "الوقت/التاريخ", 150, "{0:yyyy/MM/dd HH:mm}");
                    AddColumn("Username", "المستخدم", 100);
                    AddColumn("ActionTypeAR", "العملية", 100);
                    AddColumn("Details", "التفاصيل", 250);
                    break;

                case "TopSelling":
                    AddColumn("ProductName", "المنتج", 150);
                    AddColumn("Category", "الفئة", 100);
                    AddColumn("QuantitySold", "الكمية المباعة", 100);
                    AddColumn("TotalRevenue", "الإيرادات", 100, "{0:C2}");
                    AddColumn("AveragePrice", "متوسط السعر", 100, "{0:C2}");
                    AddColumn("StockStatus", "حالة المخزون", 100);
                    break;
            }
        }

        private void AddColumn(string bindingPath, string header, double width, string format = null)
        {
            var binding = new Binding(bindingPath);
            if (format == "{0:C}" || format == "{0:C0}" || format == "{0:C2}")
            {
                binding.Converter = _currencyConverter;
            }
            else
            {
                binding.StringFormat = format;
            }

            var col = new DataGridTextColumn
            {
                Header = header,
                Binding = binding,
                Width = width,
                ElementStyle = new Style(typeof(TextBlock)) { Setters = { new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center) } }
            };
            ReportDataGrid.Columns.Add(col);
        }

        private static readonly IValueConverter _currencyConverter = new CurrencyBindingConverter();

        private sealed class CurrencyBindingConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value switch
                {
                    decimal d => d.ToDisplayCurrency(),
                    double d => d.ToDisplayCurrency(),
                    float f => f.ToDisplayCurrency(),
                    int i => ((decimal)i).ToDisplayCurrency(),
                    long l => ((decimal)l).ToDisplayCurrency(),
                    _ => value?.ToString() ?? string.Empty
                };
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return Binding.DoNothing;
            }
        }

        #endregion

        #region تحديث البطاقات

        private void UpdateSalesCards()
        {
            Card1Icon.Text = "💰"; Card1Title.Text = "إجمالي المبيعات"; Card1Trend.Text = "للفترة المحددة";
            Card2Icon.Text = "📄"; Card2Title.Text = "عدد الفواتير"; Card2Trend.Text = "فاتورة";
            Card3Icon.Text = "📦"; Card3Title.Text = "المنتجات المباعة"; Card3Trend.Text = "وحدة";
            Card4Icon.Text = "📊"; Card4Title.Text = "متوسط الفاتورة"; Card4Trend.Text = "لكل عملية";
        }

        private void UpdatePurchasesCards()
        {
            Card1Icon.Text = "🛒"; Card1Title.Text = "إجمالي المشتريات"; Card1Trend.Text = "للفترة المحددة";
            Card2Icon.Text = "📄"; Card2Title.Text = "عدد الفواتير"; Card2Trend.Text = "فاتورة";
            Card3Icon.Text = "📦"; Card3Title.Text = "المنتجات المشتراة"; Card3Trend.Text = "وحدة";
            Card4Icon.Text = "💳"; Card4Title.Text = "المتبقي للموردين"; Card4Trend.Text = "غير مدفوع";
        }

        private void UpdateCustomersCards()
        {
            Card1Icon.Text = "👥"; Card1Title.Text = "إجمالي العملاء"; Card1Trend.Text = "عميل";
            Card2Icon.Text = "✅"; Card2Title.Text = "العملاء النشطين"; Card2Trend.Text = "لديهم مشتريات";
            Card3Icon.Text = "💰"; Card3Title.Text = "إجمالي المشتريات"; Card3Trend.Text = "من العملاء";
            Card4Icon.Text = "📊"; Card4Title.Text = "متوسط المشتريات"; Card4Trend.Text = "لكل عميل";
        }

        private void UpdateSuppliersCards()
        {
            Card1Icon.Text = "🏢"; Card1Title.Text = "إجمالي الموردين"; Card1Trend.Text = "مورد";
            Card2Icon.Text = "✅"; Card2Title.Text = "الموردين النشطين"; Card2Trend.Text = "لديهم توريدات";
            Card3Icon.Text = "💰"; Card3Title.Text = "إجمالي التوريدات"; Card3Trend.Text = "قيمة";
            Card4Icon.Text = "📊"; Card4Title.Text = "متوسط التوريد"; Card4Trend.Text = "لكل مورد";
        }

        private void UpdateInventoryCards()
        {
            Card1Icon.Text = "📦"; Card1Title.Text = "إجمالي المنتجات"; Card1Trend.Text = "منتج";
            Card2Icon.Text = "⚠️"; Card2Title.Text = "منخفض المخزون"; Card2Trend.Text = "يحتاج إعادة طلب";
            Card3Icon.Text = "💰"; Card3Title.Text = "قيمة المخزون"; Card3Trend.Text = "إجمالي";
            Card4Icon.Text = "📊"; Card4Title.Text = "إجمالي الكميات"; Card4Trend.Text = "وحدة";
        }

        private void UpdateProfitsCards()
        {
            Card1Icon.Text = "💵"; Card1Title.Text = "إجمالي الإيرادات"; Card1Trend.Text = "المبيعات";
            Card2Icon.Text = "💸"; Card2Title.Text = "إجمالي المصروفات"; Card2Trend.Text = "المشتريات";
            Card3Icon.Text = "📈"; Card3Title.Text = "صافي الربح"; Card3Trend.Text = "الفرق";
            Card4Icon.Text = "📊"; Card4Title.Text = "هامش الربح"; Card4Trend.Text = "نسبة مئوية";
        }

        private void UpdateUnpaidCards()
        {
            Card1Icon.Text = "📄"; Card1Title.Text = "فواتير مبيعات"; Card1Trend.Text = "غير مدفوعة";
            Card2Icon.Text = "💰"; Card2Title.Text = "مستحق من العملاء"; Card2Trend.Text = "مبالغ";
            Card3Icon.Text = "📄"; Card3Title.Text = "فواتير مشتريات"; Card3Trend.Text = "غير مدفوعة";
            Card4Icon.Text = "💸"; Card4Title.Text = "مستحق للموردين"; Card4Trend.Text = "مبالغ";
        }

        private void UpdateActivityLogsCards()
        {
            Card1Icon.Text = "📊"; Card1Title.Text = "إجمالي النشاطات"; Card1Trend.Text = "عملية";
            Card2Icon.Text = "👤"; Card2Title.Text = "المستخدمين"; Card2Trend.Text = "نشط";
            Card3Icon.Text = "➕"; Card3Title.Text = "عمليات إضافة"; Card3Trend.Text = "سجل";
            Card4Icon.Text = "✏️"; Card4Title.Text = "عمليات تعديل"; Card4Trend.Text = "سجل";
        }

        private void UpdateTopSellingCards()
        {
            Card1Icon.Text = "📦"; Card1Title.Text = "إجمالي الوحدات المباعة"; Card1Trend.Text = "قطعة";
            Card2Icon.Text = "📊"; Card2Title.Text = "عدد المنتجات"; Card2Trend.Text = "صنف";
            Card3Icon.Text = "💰"; Card3Title.Text = "إجمالي الإيرادات"; Card3Trend.Text = "من الأكثر مبيعاً";
            Card4Icon.Text = "🏷️"; Card4Title.Text = "متوسط سعر البيع"; Card4Trend.Text = "للوحدة";
        }

        #endregion

        #region الرسوم البيانية

        private void LoadSalesChart(List<DailySalesAggregate> dailyData)
        {
            try
            {
                var lookup = (dailyData ?? [])
                    .GroupBy(x => x.SaleDate.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(v => v.TotalSales));

                var dailySales = new List<double>();
                var dateLabels = new List<string>();
                for (var day = _fromDate.Date; day <= _toDate.Date; day = day.AddDays(1))
                {
                    lookup.TryGetValue(day, out decimal total);
                    dailySales.Add((double)total);
                    dateLabels.Add(day.ToString("dd/MM"));
                }

                Dates = [.. dateLabels];

                MainSeries.Clear();
                MainSeries.Add(new LineSeries
                {
                    Title = "المبيعات",
                    Values = new ChartValues<double>(dailySales),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    Stroke = new SolidColorBrush(Color.FromRgb(139, 92, 246)), // Purple
                    Fill = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops =
                        [
                            new GradientStop(Color.FromArgb(100, 139, 92, 246), 0),
                            new GradientStop(Color.FromArgb(0, 217, 70, 239), 1)
                        ]
                    }
                });

                _ = (MainChart?.Series = MainSeries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل مخطط المبيعات اليومية");
            }
        }

        private void LoadSalesPieChart(List<Sale> sales)
        {
            try
            {
                var paidCount = sales.Count(s => s.PaymentStatus == "Paid");
                var partialCount = sales.Count(s => s.PaymentStatus == "Partial");
                var unpaidCount = sales.Count(s => s.PaymentStatus == "Unpaid");

                PieSeries.Clear();
                if (paidCount > 0) PieSeries.Add(new PieSeries { Title = "مدفوعة", Values = new ChartValues<int> { paidCount }, Fill = new SolidColorBrush(Color.FromRgb(39, 174, 96)) });
                if (partialCount > 0) PieSeries.Add(new PieSeries { Title = "جزئية", Values = new ChartValues<int> { partialCount }, Fill = new SolidColorBrush(Color.FromRgb(243, 156, 18)) });
                if (unpaidCount > 0) PieSeries.Add(new PieSeries { Title = "غير مدفوعة", Values = new ChartValues<int> { unpaidCount }, Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60)) });

                _ = (PieChart?.Series = PieSeries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل مخطط حالة الدفع");
            }
        }

        private void LoadPurchasesChart(List<Purchase> purchases)
        {
            try
            {
                var supplierGroups = purchases.GroupBy(p => p.SupplierName ?? "غير محدد")
                    .Select(g => new { Name = g.Key, Total = g.Sum(p => p.TotalAmount) })
                    .OrderByDescending(g => g.Total)
                    .Take(5)
                    .ToList();

                PieSeries.Clear();
                foreach (var group in supplierGroups)
                {
                    PieSeries.Add(new PieSeries { Title = group.Name, Values = new ChartValues<decimal> { group.Total } });
                }

                _ = (PieChart?.Series = PieSeries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل مخطط الموردين");
            }
        }

        private void LoadCustomersChart(List<Customer> customers)
        {
            try
            {
                var topCustomers = customers.OrderByDescending(c => c.TotalPurchases).Take(5).ToList();

                MainSeries.Clear();
                MainSeries.Add(new ColumnSeries
                {
                    Title = "المشتريات",
                    Values = new ChartValues<decimal>(topCustomers.Select(c => c.TotalPurchases))
                });

                Dates = [.. topCustomers.Select(c => c.Name)];
                _ = (MainChart?.Series = MainSeries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل مخطط العملاء");
            }
        }

        private void LoadInventoryChart(List<Product> products)
        {
            try
            {
                var categoryGroups = products.GroupBy(p => p.Category ?? "غير مصنف")
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                PieSeries.Clear();
                foreach (var group in categoryGroups)
                {
                    PieSeries.Add(new PieSeries { Title = group.Category, Values = new ChartValues<int> { group.Count } });
                }

                _ = (PieChart?.Series = PieSeries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل مخطط المخزون");
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private void LoadProfitsChart(decimal revenue, decimal cost, decimal profit)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            try
            {
                PieSeries.Clear();
                // عرض الأرباح والمصروفات كنسبة من الإجمالي
                PieSeries.Add(new PieSeries
                {
                    Title = "صافي الربح",
                    Values = new ChartValues<decimal> { profit },
                    Fill = new SolidColorBrush(Color.FromRgb(39, 174, 96))
                });

                PieSeries.Add(new PieSeries
                {
                    Title = "المصروفات",
                    Values = new ChartValues<decimal> { cost },
                    Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60))
                });

                _ = (PieChart?.Series = PieSeries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل مخطط الأرباح");
            }
        }

        private void LoadActivityLogsChart(List<Models.ActivityLog> logs)
        {
            try
            {
                var actionGroups = logs.GroupBy(l => l.ActionTypeAR ?? l.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                PieSeries.Clear();
                foreach (var group in actionGroups)
                {
                    PieSeries.Add(new PieSeries { Title = group.Action, Values = new ChartValues<int> { group.Count } });
                }

                _ = (PieChart?.Series = PieSeries);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل مخطط سجل النشاطات");
            }
        }

        #endregion

        #region الفلاتر السريعة

        private void QuickFilter_Today(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            _ = LoadReportAsync();
        }

        private void QuickFilter_Week(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            FromDatePicker.SelectedDate = startOfWeek;
            ToDatePicker.SelectedDate = today;
            _ = LoadReportAsync();
        }

        private void QuickFilter_Month(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            FromDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            ToDatePicker.SelectedDate = today;
            _ = LoadReportAsync();
        }

        private void SetupUnpaidFilters()
        {
            AdditionalFilterPanel.Children.Clear();

            // فلتر العميل
            var customerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 10, 0) };
            _ = customerPanel.Children.Add(new TextBlock { Text = "العميل:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0), Foreground = (Brush)FindResource("TextSecondaryBrush") });

            _customerFilter = new ComboBox { Width = 150, Height = 35, DisplayMemberPath = "Name", SelectedValuePath = "CustomerID", Background = Brushes.White };

            try
            {
                var customers = CustomerDAL.GetAllCustomers();
                customers.Insert(0, new Customer { CustomerID = -1, Name = "الكل" });
                _customerFilter.ItemsSource = customers;
                _customerFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل فلتر العملاء للديون");
            }

            _ = customerPanel.Children.Add(_customerFilter);
            _ = AdditionalFilterPanel.Children.Add(customerPanel);
            _customerFilter.SelectionChanged += (_, _) => _ = LoadReportAsync();

            // فلتر المتأخرة
            _overdueOnlyFilter = new CheckBox { Content = "المتأخرة فقط", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), Foreground = (Brush)FindResource("TextPrimaryBrush") };
            _ = AdditionalFilterPanel.Children.Add(_overdueOnlyFilter);
            _overdueOnlyFilter.Checked += (_, _) => _ = LoadReportAsync();
            _overdueOnlyFilter.Unchecked += (_, _) => _ = LoadReportAsync();
        }

        private void QuickFilter_Year(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            FromDatePicker.SelectedDate = new DateTime(today.Year, 1, 1);
            ToDatePicker.SelectedDate = today;
            _ = LoadReportAsync();
        }

        #endregion

        #region التصدير والطباعة

        /// <summary>
        /// تصدير إلى PDF
        /// </summary>
        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            BtnExportExcel_Click(sender, e);
        }

        /// <summary>
        /// تصدير إلى Excel
        /// </summary>
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|PDF Files (*.pdf)|*.pdf",
                    FileName = $"تقرير_{_currentReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var filePath = saveDialog.FileName;
                    var data = ReportDataGrid.ItemsSource;

                    if (data == null)
                    {
                        _ = MessageBox.Show("لا توجد بيانات للتصدير", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var format = Path.GetExtension(filePath).ToLowerInvariant() switch
                    {
                        ".csv" => ExportFormat.CSV,
                        ".pdf" => ExportFormat.PDF,
                        _ => ExportFormat.Excel
                    };

                    bool success = ExportTypedData(data, filePath, format);
                    if (success)
                        _ = MessageBox.Show("تم تصدير التقرير بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ExportTypedData(System.Collections.IEnumerable data, string filePath, ExportFormat format)
        {
            var headerMap = GetColumnMapForCurrentReport();
            string title = CurrentReportTitle?.Text ?? "تقرير";

            return _currentReportType switch
            {
                "Sales" => ExportTyped((IEnumerable<Sale>)data, filePath, format, title, headerMap),
                "Purchases" => ExportTyped((IEnumerable<Purchase>)data, filePath, format, title, headerMap),
                "Customers" => ExportTyped((IEnumerable<Customer>)data, filePath, format, title, headerMap),
                "Suppliers" => ExportTyped((IEnumerable<Supplier>)data, filePath, format, title, headerMap),
                "Inventory" => ExportTyped((IEnumerable<Product>)data, filePath, format, title, headerMap),
                "ActivityLogs" => ExportTyped((IEnumerable<Models.ActivityLog>)data, filePath, format, title, headerMap),
                "TopSelling" => ExportTyped((IEnumerable<ProductSalesReport>)data, filePath, format, title, headerMap),
                "Unpaid" => ExportTyped((IEnumerable<UnpaidInvoiceRow>)data, filePath, format, title, headerMap),
                _ => false,
            };
        }

        private static bool ExportTyped<T>(IEnumerable<T> data, string filePath, ExportFormat format, string title, IReadOnlyDictionary<string, string> headerMap)
        {
            return format switch
            {
                ExportFormat.CSV => ExportHelper.ExportToCSV(data, filePath, headerMap),
                ExportFormat.Excel => ExportHelper.ExportToExcel(data, filePath, headerMap),
                ExportFormat.PDF => ExportHelper.ExportToPDF(data, filePath, title, headerMap),
                _ => ExportHelper.ExportReport(data, filePath, format)
            };
        }

        private Dictionary<string, string> GetColumnMapForCurrentReport()
        {
            return _currentReportType switch
            {
                "Sales" => new Dictionary<string, string>
                {
                    ["SaleID"] = "رقم الفاتورة",
                    ["CustomerName"] = "العميل",
                    ["SaleDate"] = "التاريخ",
                    ["NetTotal"] = "الإجمالي",
                    ["PaidAmount"] = "المدفوع",
                    ["RemainingAmount"] = "المتبقي",
                    ["PaymentStatusText"] = "الحالة"
                },
                "Purchases" => new Dictionary<string, string>
                {
                    ["PurchaseID"] = "رقم الفاتورة",
                    ["SupplierName"] = "المورد",
                    ["PurchaseDate"] = "التاريخ",
                    ["TotalAmount"] = "الإجمالي",
                    ["PaidAmount"] = "المدفوع",
                    ["RemainingAmount"] = "المتبقي"
                },
                "Customers" => new Dictionary<string, string>
                {
                    ["Name"] = "اسم العميل",
                    ["Phone"] = "الهاتف",
                    ["TotalPurchases"] = "إجمالي المشتريات",
                    ["PurchaseCount"] = "عدد الزيارات"
                },
                "Suppliers" => new Dictionary<string, string>
                {
                    ["Name"] = "اسم المورد",
                    ["Phone"] = "الهاتف",
                    ["TotalSuppliedValue"] = "إجمالي التوريدات"
                },
                "Inventory" => new Dictionary<string, string>
                {
                    ["Name"] = "المنتج",
                    ["Category"] = "الفئة",
                    ["Quantity"] = "الكمية",
                    ["SellingPrice"] = "السعر",
                    ["MinQuantity"] = "حد الطلب"
                },
                "ActivityLogs" => new Dictionary<string, string>
                {
                    ["LogDate"] = "الوقت/التاريخ",
                    ["Username"] = "المستخدم",
                    ["ActionTypeAR"] = "العملية",
                    ["Details"] = "التفاصيل"
                },
                "TopSelling" => new Dictionary<string, string>
                {
                    ["ProductName"] = "المنتج",
                    ["Category"] = "الفئة",
                    ["QuantitySold"] = "الكمية المباعة",
                    ["TotalRevenue"] = "الإيرادات",
                    ["AveragePrice"] = "متوسط السعر",
                    ["CurrentStock"] = "المخزون الحالي"
                },
                "Unpaid" => new Dictionary<string, string>
                {
                    ["Type"] = "النوع",
                    ["ID"] = "رقم الفاتورة",
                    ["Name"] = "العميل/المورد",
                    ["Date"] = "التاريخ",
                    ["Total"] = "الإجمالي",
                    ["Paid"] = "المدفوع",
                    ["Remaining"] = "المتبقي",
                    ["DueDate"] = "تاريخ الاستحقاق"
                },
                _ => []
            };
        }

        /// <summary>
        /// طباعة التقرير
        /// </summary>
        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // إنشاء مستند للطباعة
                    var document = new FlowDocument
                    {
                        PageWidth = printDialog.PrintableAreaWidth,
                        PagePadding = new Thickness(50),
                        ColumnWidth = printDialog.PrintableAreaWidth
                    };

                    // العنوان
                    var title = new Paragraph(new Run(CurrentReportTitle.Text))
                    {
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 20)
                    };
                    document.Blocks.Add(title);

                    // الفترة
                    var period = new Paragraph(new Run($"من: {_fromDate:yyyy/MM/dd} إلى: {_toDate:yyyy/MM/dd}"))
                    {
                        FontSize = 14,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 30)
                    };
                    document.Blocks.Add(period);

                    // الملخص
                    var summary = new Paragraph();
                    summary.Inlines.Add(new Run($"{Card1Title.Text}: {Card1Value.Text}\n"));
                    summary.Inlines.Add(new Run($"{Card2Title.Text}: {Card2Value.Text}\n"));
                    summary.Inlines.Add(new Run($"{Card3Title.Text}: {Card3Value.Text}\n"));
                    summary.Inlines.Add(new Run($"{Card4Title.Text}: {Card4Value.Text}\n"));
                    summary.FontSize = 14;
                    summary.Margin = new Thickness(0, 0, 0, 20);
                    document.Blocks.Add(summary);

                    // الطباعة
                    IDocumentPaginatorSource idpSource = document;
                    printDialog.PrintDocument(idpSource.DocumentPaginator, CurrentReportTitle.Text);

                    _ = MessageBox.Show("تم إرسال التقرير للطباعة!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}

