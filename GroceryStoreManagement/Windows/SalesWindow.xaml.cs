using GroceryStoreManagement.DAL; // طبقة الوصول للبيانات للتعامل مع المبيعات
using GroceryStoreManagement.Helpers; // المساعدات مثل خدمة الطباعة
using GroceryStoreManagement.Models; // نماذج البيانات مثل كلاس Sale
using System; // الأنواع الأساسية
using System.Collections.Generic; // القوائم والمجموعات
using System.Linq; // استعلامات LINQ لتصفية البيانات
using System.Threading.Tasks;
using System.Windows; // عناصر WPF الأساسية مثل MessageBox
using System.Windows.Controls; // عناصر التحكم مثل زر و قائمة منسدلة
using System.Windows.Input;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة إدارة المبيعات - تعرض سجل المبيعات وتسمح بإدارتها
    /// هذا الكلاس يتحكم في واجهة عرض سجل الفواتير والمبيعات اليومية
    /// </summary>
    public partial class SalesWindow : UserControl
    {
        // قائمة لتخزين جميع المبيعات محلياً لتمكين البحث والفلترة السريعة بدون إعادة الاتصال بقاعدة البيانات
        private List<Sale> _allSales = [];
        private List<Sale> _filteredSales = [];
        private int _currentPage = 1;
        private int _pageSize = 10;
        private bool _isLoading;
        private Shift _currentShift;

        /// <summary>
        /// المُنشئ - يتم استدعاؤه عند فتح النافذة
        /// </summary>
        public SalesWindow()
        {
            InitializeComponent(); // تهيئة واجهة المستخدم
            this.Loaded += SalesWindow_Loaded; // ربط حدث التحميل
        }

        /// <summary>
        /// حدث Loaded - يتم استدعاؤه بعد تهيئة جميع عناصر UI
        /// </summary>
        private async void SalesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadShiftStatusAsync();
            await LoadSalesAsync(); // بدء تحميل البيانات
        }

        /// <summary>
        /// تحميل المبيعات من قاعدة البيانات وعرضها
        /// </summary>
        private void LoadSales()
        {
            _ = LoadSalesAsync();
        }

        private async Task LoadSalesAsync()
        {
            try
            {
                SetLoadingState(true);
                // جلب كل المبيعات
                _allSales = await SaleDAL.GetAllSalesAsync();

                // تطبيق الفلاتر الحالية (التاريخ، البحث...)
                ApplyFilters();

                // تحديث شريط الملخص في الأعلى (إجمالي المبيعات، المدفوع، المتبقي)
                UpdateSummary();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل المبيعات: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task LoadShiftStatusAsync()
        {
            try
            {
                _currentShift = await ShiftDAL.GetOpenShiftAsync();
                if (_currentShift != null)
                {
                    SessionContext.CurrentShiftID = _currentShift.ShiftID;
                    ShiftStatusText.Text = $"الوردية: مفتوحة #{_currentShift.ShiftID} ({_currentShift.OpenedAt:HH:mm})";
                    ShiftStatusText.Foreground = (System.Windows.Media.Brush)FindResource("SuccessBrush");
                    StartShiftButton.IsEnabled = false;
                    CloseShiftButton.IsEnabled = true;
                }
                else
                {
                    SessionContext.CurrentShiftID = null;
                    ShiftStatusText.Text = "الوردية: غير مفتوحة";
                    ShiftStatusText.Foreground = (System.Windows.Media.Brush)FindResource("WarningBrush");
                    StartShiftButton.IsEnabled = true;
                    CloseShiftButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل تحميل حالة الوردية");
            }
        }

        private bool EnsureShiftOpen()
        {
            if (_currentShift != null) return true;

            _ = MessageBox.Show(
                "لا يمكن تنفيذ العملية قبل فتح وردية.\nاستخدم زر (بدء وردية) أولاً.",
                "الوردية غير مفتوحة",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;
            NewInvoiceButton?.SetCurrentValue(UIElement.IsEnabledProperty, !isLoading);
            Mouse.OverrideCursor = isLoading ? Cursors.Wait : null;
        }

        /// <summary>
        /// تحديث شريط الملخص بالأرقام الحالية
        /// </summary>
        private void UpdateSummary()
        {
            try
            {
                // عرض العدد الكلي للفواتير
                TotalInvoicesStatText.Text = _allSales.Count.ToString();

                decimal totalSales = 0;
                decimal totalPaid = 0;
                decimal totalRemaining = 0;

                // تجميع المبالغ من جميع الفواتير
                foreach (var sale in _allSales)
                {
                    totalSales += sale.NetTotal;
                    totalPaid += sale.PaidAmount;
                    totalRemaining += sale.RemainingAmount;
                }

                // عرض النتائج بتنسيق العملة
                TotalSalesStatText.Text = totalSales.ToDisplayCurrency(); // التحديث في الكرت
                TotalSalesText.Text = totalSales.ToDisplayCurrency();     // التحديث في الفوتر
                TotalPaidStatText.Text = totalPaid.ToDisplayCurrency();
                TotalRemainingStatText.Text = totalRemaining.ToDisplayCurrency();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating summary: {ex.Message}");
            }
        }

        /// <summary>
        /// تطبيق جميع الفلاتر المتاحة على قائمة المبيعات
        /// </summary>
        private void ApplyFilters()
        {
            try
            {
                // التحقق من أن العناصر جاهزة
                if (_allSales == null || SalesGrid == null)
                    return;

                var filteredSales = _allSales.AsEnumerable();

                // 1. فلتر التاريخ (من تاريخ معين)
                if (DateFromPicker != null && DateFromPicker.SelectedDate.HasValue)
                {
                    var fromDate = DateFromPicker.SelectedDate.Value.Date;
                    filteredSales = filteredSales.Where(s => s.SaleDate.Date >= fromDate);
                }

                // 2. فلتر التاريخ (إلى تاريخ معين)
                if (DateToPicker != null && DateToPicker.SelectedDate.HasValue)
                {
                    var toDate = DateToPicker.SelectedDate.Value.Date;
                    filteredSales = filteredSales.Where(s => s.SaleDate.Date <= toDate);
                }

                // 3. فلتر حالة الدفع (مدفوع كلياً، جزئياً، غير مدفوع)
                if (PaymentStatusComboBox != null)
                {
                    switch (PaymentStatusComboBox.SelectedIndex)
                    {
                        case 1: // مدفوعة بالكامل
                            filteredSales = filteredSales.Where(s => s.PaymentStatus == "Paid");
                            break;
                        case 2: // مدفوعة جزئياً
                            filteredSales = filteredSales.Where(s => s.PaymentStatus == "Partial");
                            break;
                        case 3: // غير مدفوعة
                            filteredSales = filteredSales.Where(s => s.PaymentStatus == "Unpaid");
                            break;
                    }
                }

                // 4. فلتر البحث النصي (رقم الفاتورة أو اسم العميل)
                if (SearchTextBox != null && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    string searchTerm = SearchTextBox.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                    filteredSales = filteredSales.Where(s =>
                        s.SaleID.ToString().Contains(searchTerm) || // البحث برقم الفاتورة
                        (s.CustomerName != null && s.CustomerName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) // البحث باسم العميل
                    );
                }

                // تحديث حالة زر البحث
                _ = (ClearSearchButton?.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
                        ? Visibility.Collapsed : Visibility.Visible);

                // تحديث النتائج والصفحات
                _filteredSales = [.. filteredSales];
                _currentPage = 1;
                UpdatePagedData();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تصفية المبيعات: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // عند تغيير أي قيمة في الفلاتر، نعيد تطبيق الفلترة
        private void FilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        // عند الكتابة في مربع البحث
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        // زر لإعادة تعيين جميع الفلاتر للوضع الافتراضي
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            PaymentStatusComboBox.SelectedIndex = 0;
            SearchTextBox.Text = "";
            ApplyFilters();
        }

        /// <summary>
        /// زر إنشاء فاتورة جديدة
        /// </summary>
        private void ManagePromotions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.ManagePromotions)) return;

                var window = new PromotionsWindow
                {
                    Owner = Window.GetWindow(this)
                };
                _ = window.ShowDialog();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في فتح نافذة العروض: {ex.Message}");
            }
        }

        private async void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.CreateSales)) return;
                if (!EnsureShiftOpen()) return;

                var dialog = new SaleDialog(); // فتح نافذة نقطة البيع
                if (dialog.ShowDialog() == true)
                {
                    LoadSales(); // إعادة تحميل القائمة إذا تم حفظ فاتورة
                    await LoadShiftStatusAsync();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إنشاء فاتورة جديدة: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// عرض تفاصيل فاتورة محددة في نافذة منفصلة
        /// </summary>
        private void ViewSale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    int saleId = Convert.ToInt32(button.Tag); // الحصول على رقم الفاتورة من خاصية Tag للزر
                    var dialog = new SaleDetailsDialog(saleId);
                    _ = dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في عرض الفاتورة: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تعديل فاتورة موجودة
        /// </summary>
        private void EditSale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.EditSales)) return;

                if (sender is Button button && button.Tag != null)
                {
                    int saleId = Convert.ToInt32(button.Tag);
                    var sale = SaleDAL.GetSaleById(saleId);

                    if (sale != null)
                    {
                        var dialog = new SaleDialog(sale);
                        if (dialog.ShowDialog() == true)
                        {
                            LoadSales();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تعديل الفاتورة: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// إجراء سداد لفاتورة (فتح نافذة السداد)
        /// </summary>
        private async void PaySale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.EditSales)) return;
                if (!EnsureShiftOpen()) return;

                if (sender is Button button && button.Tag != null)
                {
                    int saleId = Convert.ToInt32(button.Tag);
                    var sale = SaleDAL.GetSaleById(saleId);

                    if (sale != null)
                    {
                        if (sale.PaymentStatus == "Paid")
                        {
                            _ = MessageBox.Show("هذه الفاتورة مدفوعة بالكامل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        // فتح نافذة السداد الجديدة
                        var dialog = new PaymentDialog(sale)
                        {
                            Owner = Window.GetWindow(this)
                        };
                        if (dialog.ShowDialog() == true)
                        {
                            LoadSales(); // إعادة تحميل البيانات بعد السداد
                            await LoadShiftStatusAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في السداد: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// إجراء عملية إرجاع (فتح نافذة الإرجاع)
        /// </summary>
        private async void CreateReturn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.ProcessReturns))
                    return;
                if (!EnsureShiftOpen()) return;

                if (sender is Button button && button.Tag != null)
                {
                    int saleId = Convert.ToInt32(button.Tag);
                    var sale = SaleDAL.GetSaleById(saleId);

                    if (sale != null)
                    {
                        var dialog = new ReturnDialog(sale)
                        {
                            Owner = Window.GetWindow(this)
                        };
                        if (dialog.ShowDialog() == true)
                        {
                            LoadSales(); // إعادة تحميل البيانات لتحديث المبالغ (المرتجع)
                            await LoadShiftStatusAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في عملية الإرجاع: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حذف فاتورة
        /// </summary>
        private void DeleteSale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.DeleteSales)) return;

                if (sender is Button button && button.Tag != null)
                {
                    int saleId = Convert.ToInt32(button.Tag);
                    var sale = SaleDAL.GetSaleById(saleId);

                    if (sale != null)
                    {
                        var result = MessageBox.Show($"هل تريد حذف الفاتورة رقم {sale.SaleID}؟",
                            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            bool success = SaleDAL.DeleteSale(saleId);
                            if (success)
                            {
                                LoadSales();
                                _ = MessageBox.Show("تم حذف الفاتورة بنجاح",
                                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حذف الفاتورة: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// طباعة إيصال الفاتورة
        /// </summary>
        private void PrintSale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.PrintInvoices)) return;

                if (sender is Button button && button.Tag != null)
                {
                    int saleId = Convert.ToInt32(button.Tag);
                    PrintHelper.PrintReceipt(saleId); // استدعاء مساعد الطباعة
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في طباعة الفاتورة: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // حدث عند تحديد سطر في الجدول (غير مستخدم حاليا)
        private void SalesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة وظائف عند تحديد فاتورة
        }

        #region الترحيل والبحث

        /// <summary>
        /// تحديث البيانات المعروضة بناءً على الصفحة الحالية
        /// </summary>
        private void UpdatePagedData()
        {
            if (_filteredSales == null || SalesGrid == null) return;

            int totalItems = _filteredSales.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / _pageSize);

            if (totalPages == 0) totalPages = 1;
            if (_currentPage > totalPages) _currentPage = totalPages;

            var pagedData = _filteredSales
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            SalesGrid.ItemsSource = pagedData;

            // تحديث نص الترحيل
            int startItem = totalItems == 0 ? 0 : (_currentPage - 1) * _pageSize + 1;
            int endItem = Math.Min(_currentPage * _pageSize, totalItems);

            _ = (PageInfoText?.Text = $"عرض {startItem}-{endItem} من {totalItems}");

            // تحديث حالة الأزرار
            _ = (PrevPageButton?.IsEnabled = _currentPage > 1);
            _ = (NextPageButton?.IsEnabled = _currentPage < totalPages);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            _ = SearchTextBox.Focus();
        }

        private void PageSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeComboBox != null && PageSizeComboBox.SelectedItem is ComboBoxItem item)
            {
                if (int.TryParse(item.Content.ToString(), out int size))
                {
                    _pageSize = size;
                    _currentPage = 1;
                    UpdatePagedData();
                }
            }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagedData();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalItems = _filteredSales.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / _pageSize);

            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdatePagedData();
            }
        }

        private async void StartShift_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentShift != null)
                {
                    _ = MessageBox.Show("توجد وردية مفتوحة بالفعل.");
                    return;
                }

                var dialog = new ShiftManagementWindow
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    await LoadShiftStatusAsync();
                    LoadSales();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ أثناء فتح الوردية: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CloseShift_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentShift == null)
                {
                    _ = MessageBox.Show("لا توجد وردية مفتوحة حالياً.");
                    return;
                }

                var dialog = new ShiftManagementWindow
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    await LoadShiftStatusAsync();
                    LoadSales();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ أثناء إغلاق الوردية: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}

