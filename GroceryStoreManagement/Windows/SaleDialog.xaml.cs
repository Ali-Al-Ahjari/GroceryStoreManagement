using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة إنشاء/تعديل فاتورة مبيعات
    /// </summary>
    public partial class SaleDialog : Window
    {
        private readonly ObservableCollection<SaleItem> _saleItems = [];
        private readonly Sale _sale;
        private readonly bool _isEditMode = false;
        private bool _printAfterSave = false;
        private bool _isSaving;
        private Shift _activeShift;
        private string _baseHeaderText;
        private readonly DispatcherTimer _quickSearchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
        private CancellationTokenSource _searchCts;
        private const int InitialProductLoadLimit = 120;

        // متغيرات للماسح الضوئي (Barcode Scanner)
        private string _barcodeBuffer = string.Empty;
        private DateTime _lastKeystrokeTime = DateTime.MinValue;

        /// <summary>
        /// المُنشئ - فاتورة جديدة
        /// </summary>
        public SaleDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializeControls();
        }

        /// <summary>
        /// المُنشئ - تعديل فاتورة موجودة
        /// </summary>
        public SaleDialog(Sale sale)
        {
            InitializeComponent();
            _sale = sale;
            _isEditMode = true;
            InitializeControls();
            LoadSaleData();
        }

        /// <summary>
        /// تهيئة عناصر التحكم
        /// </summary>
        private void InitializeControls()
        {
            // تفعيل التنقل بمفتاح Enter
            EnterKeyHelper.EnableEnterKeyNavigation(this);

            LoadCustomers();
            _ = LoadInitialProductsAsync();
            SaleItemsGrid.ItemsSource = _saleItems;
            _quickSearchDebounceTimer.Tick += QuickSearchDebounceTimer_Tick;

            HeaderText.Text = _isEditMode ? "تعديل فاتورة" : "فاتورة مبيعات جديدة";
            Title = _isEditMode ? "تعديل فاتورة" : "فاتورة جديدة";
            _baseHeaderText = HeaderText.Text;

            // ربط حدث تحديث السعر عند اختيار المنتج
            ProductComboBox.SelectionChanged += ProductComboBox_SelectionChanged;

            // تسجيل التقاط المفاتيح للماسح الضوئي (تعمل على مستوى النافذة)
            this.PreviewTextInput += SaleDialog_PreviewTextInput;

            RefreshShiftContext();
            ApplyDefaultTaxRate();
            CalculateTotals(null, null);
        }

        private void RefreshShiftContext()
        {
            _activeShift = ShiftDAL.GetOpenShift();

            bool canCommit = _activeShift != null;
            if (BtnSave != null) BtnSave.IsEnabled = canCommit;
            if (BtnSaveAndPrint != null) BtnSaveAndPrint.IsEnabled = canCommit;
            if (BtnSuspend != null) BtnSuspend.IsEnabled = canCommit;

            if (!canCommit)
            {
                HeaderText.Text = $"{_baseHeaderText} (الوردية غير مفتوحة)";
            }
            else
            {
                HeaderText.Text = _baseHeaderText;
            }
        }

        private bool EnsureShiftIsOpen()
        {
            RefreshShiftContext();
            if (_activeShift != null) return true;

            _ = MessageBox.Show(
                "لا يمكن إكمال الفاتورة قبل فتح وردية.\nافتح الوردية من شاشة المبيعات أولاً.",
                "الوردية غير مفتوحة",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        private void ApplyDefaultTaxRate()
        {
            if (_isEditMode || TaxTextBox == null)
                return;

            decimal defaultVatPercent = AppSettings.GetVatPercent();
            TaxTextBox.Text = defaultVatPercent.ToString("0.##");
        }

        /// <summary>
        /// تحميل بيانات الفاتورة للتعديل
        /// </summary>
        private void LoadSaleData()
        {
            if (_sale != null)
            {
                InvoiceNumberText.Text = $"فاتورة رقم: {_sale.SaleID}";
                DiscountTextBox.Text = _sale.Discount.ToString();
                TaxTextBox.Text = _sale.Tax.ToString();
                PaidAmountTextBox.Text = _sale.PaidAmount.ToString();
                NotesTextBox.Text = _sale.Notes;

                // تحديد طريقة الدفع
                switch (_sale.PaymentMethod)
                {
                    case "Cash": PaymentMethodComboBox.SelectedIndex = 0; break;
                    case "Card": PaymentMethodComboBox.SelectedIndex = 1; break;
                    case "Transfer": PaymentMethodComboBox.SelectedIndex = 2; break;
                    case "Partial": PaymentMethodComboBox.SelectedIndex = 3; break;
                    case "Credit": PaymentMethodComboBox.SelectedIndex = 3; break;
                }

                // تحميل عناصر الفاتورة
                var items = SaleItemDAL.GetSaleItemsBySaleId(_sale.SaleID);
                foreach (var item in items)
                {
                    _saleItems.Add(item);
                }

                // تحديد العميل
                if (CustomerComboBox.ItemsSource is List<Customer> customers && _sale.CustomerID.HasValue)
                {
                    var customer = customers.FirstOrDefault(c => c.CustomerID == _sale.CustomerID.Value);
                    if (customer != null)
                    {
                        CustomerComboBox.SelectedItem = customer;
                    }
                }

                // تاريخ الاستحقاق
                if (_sale.DueDate.HasValue)
                {
                    DueDatePicker.SelectedDate = _sale.DueDate;
                }

                CalculateTotals(null, null);
            }
        }

        /// <summary>
        /// تحميل العملاء
        /// </summary>
        private void LoadCustomers()
        {
            try
            {
                var customers = CustomerDAL.GetAllCustomers();
                var customerList = new List<Customer>
                {
                    new() { CustomerID = 0, Name = "عميل نقدي" }
                };
                customerList.AddRange(customers);
                CustomerComboBox.ItemsSource = customerList;
                CustomerComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحميل المنتجات
        /// </summary>
        private void LoadProducts()
        {
            _ = LoadInitialProductsAsync();
        }

        private async Task LoadInitialProductsAsync()
        {
            await LoadProductsAsync(null);
        }

        private async Task LoadProductsAsync(string searchTerm)
        {
            try
            {
                var products = await ProductDAL.GetProductsPageAsync(
                    limit: InitialProductLoadLimit,
                    offset: 0,
                    searchTerm: searchTerm);
                ProductComboBox.ItemsSource = products;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل المنتجات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// عند اختيار منتج - تحديث السعر
        /// </summary>
        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is Product selectedProduct)
            {
                PriceTextBox.Text = selectedProduct.SellingPrice.ToString();
            }
        }

        /// <summary>
        /// عند تغيير العميل - تحديث الرصيد
        /// </summary>
        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerComboBox.SelectedItem is Customer customer && customer.CustomerID > 0)
            {
                CustomerBalanceText.Text = customer.TotalPurchases.ToDisplayCurrency();

                // عرض تنبيه إذا تجاوز الحد الائتماني
                if (customer.CreditLimit > 0 && customer.CurrentDebt >= customer.CreditLimit)
                {
                    CustomerBalanceText.Foreground = System.Windows.Media.Brushes.Red;
                    CustomerBalanceText.Text += " (تجاوز الحد)";
                }
                else
                {
                    CustomerBalanceText.Foreground = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
                }
            }
            else
            {
                CustomerBalanceText.Text = decimal.Zero.ToDisplayCurrency();
            }
        }

        /// <summary>
        /// إضافة عميل جديد
        /// </summary>
        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CustomerDialog();
                if (dialog.ShowDialog() == true)
                {
                    LoadCustomers();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إضافة العميل: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// إضافة منتج للسلة
        /// </summary>
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProductComboBox.SelectedItem is not Product selectedProduct)
                {
                    _ = MessageBox.Show("الرجاء اختيار منتج", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
                {
                    _ = MessageBox.Show("الرجاء إدخال كمية صحيحة", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal unitPrice = selectedProduct.SellingPrice;
                _ = decimal.TryParse(PriceTextBox.Text, out unitPrice);

                _ = decimal.TryParse(ItemDiscountTextBox.Text, out decimal itemDiscount);

                // التحقق من العروض (Promotions) - Phase 2.3
                try
                {
                    string categoryName = selectedProduct.Category; // فرضاً أن النموذج يحتوي على هذا
                                                                    // إذا لم يكن المنتج يحتوي على Category يمكن جلبها من الـ DB، لكن سنستخدم المتوفر

                    var bestPromo = PromotionDAL.GetBestPromotionForProduct(selectedProduct.ProductID, categoryName, unitPrice);
                    if (bestPromo != null)
                    {
                        decimal promoDiscountPercent = 0;
                        if (bestPromo.DiscountType == "Percentage")
                        {
                            promoDiscountPercent = bestPromo.DiscountValue;
                        }
                        else // Fixed
                        {
                            // تحويل الثابت إلى نسبة مئوية
                            if (unitPrice > 0)
                                promoDiscountPercent = (bestPromo.DiscountValue / unitPrice) * 100;
                        }

                        // نستخدم الخصم الأعلى (سواء المدخل يدوياً أو العرض)
                        if (promoDiscountPercent > itemDiscount)
                        {
                            itemDiscount = promoDiscountPercent;
                            // يمكن إضافة كود هنا لإعلام المستخدم
                            // MessageBox.Show($"تم تطبيق عرض: {bestPromo.Name}", "عرض خاص");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking promotions: {ex.Message}");
                }

                var existingItem = _saleItems.FirstOrDefault(item => item.ProductID == selectedProduct.ProductID);
                int currentQuantityInCart = existingItem?.Quantity ?? 0;
                int requestedTotalQuantity = currentQuantityInCart + quantity;

                if (requestedTotalQuantity > selectedProduct.Quantity)
                {
                    int availableToAdd = Math.Max(0, selectedProduct.Quantity - currentQuantityInCart);
                    _ = MessageBox.Show(
                        $"الكمية المطلوبة غير متوفرة.\nالمتوفر حالياً: {selectedProduct.Quantity}\n" +
                        $"الكمية في السلة: {currentQuantityInCart}\n" +
                        $"يمكنك إضافة: {availableToAdd}",
                        "تحذير",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (existingItem != null)
                {
                    existingItem.Quantity = requestedTotalQuantity;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice * (1 - existingItem.DiscountPercent / 100);
                }
                else
                {
                    var saleItem = new SaleItem
                    {
                        ProductID = selectedProduct.ProductID,
                        ProductName = selectedProduct.Name,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        DiscountPercent = itemDiscount,
                        TotalPrice = quantity * unitPrice * (1 - itemDiscount / 100)
                    };
                    _saleItems.Add(saleItem);
                }

                CalculateTotals(null, null);
                QuantityTextBox.Text = "1";
                ItemDiscountTextBox.Text = "0";
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إضافة المنتج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حذف منتج من السلة
        /// </summary>
        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    int productId = Convert.ToInt32(button.Tag);
                    var itemToRemove = _saleItems.FirstOrDefault(item => item.ProductID == productId);

                    if (itemToRemove != null)
                    {
                        _ = _saleItems.Remove(itemToRemove);
                        CalculateTotals(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حذف المنتج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حساب الإجماليات
        /// </summary>
        private void CalculateTotals(object sender, EventArgs e)
        {
            try
            {
                // Ensure controls are initialized before accessing them
                if (DiscountTextBox == null || TaxTextBox == null || PaidAmountTextBox == null ||
                    ItemCountText == null || SubtotalText == null || DiscountAmountText == null ||
                    TaxAmountText == null || TotalAmountText == null || RemainingAmountText == null)
                {
                    return;
                }

                decimal subtotal = _saleItems.Sum(item => item.TotalPrice);

                _ = decimal.TryParse(DiscountTextBox.Text, out decimal discount);
                _ = decimal.TryParse(TaxTextBox.Text, out decimal taxPercent);
                _ = decimal.TryParse(PaidAmountTextBox.Text, out decimal paidAmount);

                decimal taxAmount = subtotal * (taxPercent / 100);
                decimal total = subtotal - discount + taxAmount;
                decimal remaining = total - paidAmount;

                ItemCountText.Text = _saleItems.Count.ToString();
                SubtotalText.Text = subtotal.ToDisplayCurrency();
                DiscountAmountText.Text = $"-{discount.ToDisplayCurrency()}";
                TaxAmountText.Text = $"+{taxAmount.ToDisplayCurrency()}";
                TotalAmountText.Text = total.ToDisplayCurrency();
                RemainingAmountText.Text = remaining.ToDisplayCurrency();

                // إظهار/إخفاء لوحة المبلغ المتبقي
                if (remaining > 0)
                {
                    RemainingPaymentPanel.Visibility = Visibility.Visible;
                    if (!DueDatePicker.SelectedDate.HasValue)
                        DueDatePicker.SelectedDate = DateTime.Today.AddDays(30); // افتراضي بعد شهر
                }
                else
                {
                    RemainingPaymentPanel.Visibility = Visibility.Collapsed;
                    DueDatePicker.SelectedDate = null;
                }
            }
            catch
            {
                // تجاهل الأخطاء أثناء الكتابة
            }
        }

        /// <summary>
        /// حفظ الفاتورة
        /// </summary>
        private async void SaveSale_Click(object sender, RoutedEventArgs e)
        {
            _printAfterSave = false;
            await SaveSaleAsync();
        }

        /// <summary>
        /// حفظ وطباعة
        /// </summary>
        private async void SaveAndPrint_Click(object sender, RoutedEventArgs e)
        {
            _printAfterSave = true;
            await SaveSaleAsync();
        }

        private async Task SaveSaleAsync()
        {
            if (_isSaving)
            {
                return;
            }

            try
            {
                if (!EnsureShiftIsOpen())
                {
                    return;
                }

                if (_saleItems.Count == 0)
                {
                    _ = MessageBox.Show("الرجاء إضافة منتجات على الأقل", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? customerId = null;
                if (CustomerComboBox.SelectedItem is Customer selectedCustomer && selectedCustomer.CustomerID > 0)
                {
                    customerId = selectedCustomer.CustomerID;
                }

                decimal subtotal = _saleItems.Sum(item => item.TotalPrice);

                _ = decimal.TryParse(DiscountTextBox.Text, out decimal discount);
                _ = decimal.TryParse(TaxTextBox.Text, out decimal taxPercent);
                _ = decimal.TryParse(PaidAmountTextBox.Text, out decimal paidAmount);

                decimal taxAmount = subtotal * (taxPercent / 100);
                decimal total = subtotal - discount + taxAmount;
                decimal remaining = total - paidAmount;

                // التحقق من الحد الائتماني إذا كان هناك مبلغ متبقي
                if (remaining > 0 && customerId.HasValue)
                {
                    if (CustomerComboBox.SelectedItem is Customer customer && customer.CreditLimit > 0)
                    {
                        decimal newTotalDebt = customer.CurrentDebt + remaining;
                        if (newTotalDebt > customer.CreditLimit)
                        {
                            var result = MessageBox.Show(
                                $"حذير: سيؤدي هذا البيع إلى تجاوز الحد الائتماني للعميل!\n" +
                                $"الحد المسموح: {customer.CreditLimit.ToDisplayCurrency()}\n" +
                                $"المديونية الحالية: {customer.CurrentDebt.ToDisplayCurrency()}\n" +
                                $"المديونية الجديدة: {newTotalDebt.ToDisplayCurrency()}\n\n" +
                                "هل تريد المتابعة والحفظ؟",
                                "تجاوز الحد الائتماني",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning);

                            if (result == MessageBoxResult.No)
                            {
                                return;
                            }
                        }
                    }

                    if (!DueDatePicker.SelectedDate.HasValue)
                    {
                        _ = MessageBox.Show("الرجاء تحديد تاريخ الاستحقاق للمبلغ المتبقي", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _ = DueDatePicker.Focus();
                        return;
                    }
                }

                // تحديد حالة الدفع
                string paymentStatus = "Unpaid";
                if (paidAmount >= total)
                    paymentStatus = "Paid";
                else if (paidAmount > 0)
                    paymentStatus = "Partial";

                // تحديد طريقة الدفع
                string paymentMethod = "Cash";
                switch (PaymentMethodComboBox.SelectedIndex)
                {
                    case 0: paymentMethod = "Cash"; break;
                    case 1: paymentMethod = "Card"; break;
                    case 2: paymentMethod = "Transfer"; break;
                    case 3: paymentMethod = paidAmount > 0 ? "Partial" : "Credit"; break;
                }

                var sale = _isEditMode ? _sale : new Sale();
                sale.CustomerID = customerId;
                sale.ShiftID = _activeShift?.ShiftID;
                sale.TotalAmount = subtotal;
                sale.Discount = discount;
                sale.Tax = taxPercent;
                sale.PaidAmount = paidAmount;
                sale.PaymentStatus = paymentStatus;
                sale.PaymentMethod = paymentMethod;
                sale.Notes = NotesTextBox.Text;
                sale.ItemCount = _saleItems.Count;
                sale.DueDate = (remaining > 0) ? DueDatePicker.SelectedDate : null;

                if (!_isEditMode)
                {
                    sale.SaleDate = DateTime.Now;
                }

                _isSaving = true;
                BtnSave.IsEnabled = false;
                BtnSaveAndPrint.IsEnabled = false;

                int saleId = await Task.Run(() =>
                    SaleDAL.SaveSaleWithItems(sale, _saleItems.ToList(), _isEditMode));


                if (_printAfterSave)
                {
                    PrintHelper.PrintReceipt(saleId);
                }

                if ((paymentMethod == "Cash" || paymentMethod == "Partial") && paidAmount > 0)
                {
                    _ = PrintHelper.TryOpenCashDrawer();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حفظ الفاتورة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isSaving = false;
                RefreshShiftContext();
            }
        }

        private async void SuspendSale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureShiftIsOpen())
                {
                    return;
                }

                if (_saleItems.Count == 0)
                {
                    _ = MessageBox.Show("لا توجد أصناف في السلة لتعليقها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int? customerId = null;
                if (CustomerComboBox.SelectedItem is Customer selectedCustomer && selectedCustomer.CustomerID > 0)
                {
                    customerId = selectedCustomer.CustomerID;
                }

                _ = decimal.TryParse(DiscountTextBox.Text, out decimal discount);
                _ = decimal.TryParse(TaxTextBox.Text, out decimal tax);
                string paymentMethod = PaymentMethodComboBox.SelectedIndex switch
                {
                    1 => "Card",
                    2 => "Transfer",
                    3 => "Credit",
                    _ => "Cash"
                };

                int suspendedId = await SuspendedSaleDAL.SaveSuspendedSaleAsync(
                    customerId: customerId,
                    notes: NotesTextBox.Text,
                    discount: discount,
                    tax: tax,
                    paymentMethod: paymentMethod,
                    shiftId: _activeShift?.ShiftID,
                    userId: SessionContext.CurrentUserID,
                    items: _saleItems.ToList());

                _ = MessageBox.Show($"تم تعليق الفاتورة بنجاح. رقم الحفظ المؤقت: #{suspendedId}", "تم التعليق",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"تعذر تعليق الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResumeSuspended_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureShiftIsOpen())
                {
                    return;
                }

                var picker = new SuspendedSalesWindow(_activeShift?.ShiftID)
                {
                    Owner = this
                };

                if (picker.ShowDialog() != true)
                {
                    return;
                }

                if (_saleItems.Count > 0)
                {
                    var overwrite = MessageBox.Show(
                        "سيتم استبدال الفاتورة الحالية بالفاتورة المعلقة المحددة. هل تريد المتابعة؟",
                        "تأكيد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (overwrite != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                var suspendedSale = SuspendedSaleDAL.ResumeAndDelete(picker.SelectedSuspendedSaleId, SessionContext.CurrentUserID);
                ApplySuspendedSale(suspendedSale);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"تعذر استدعاء الفاتورة المعلقة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySuspendedSale(SuspendedSale suspendedSale)
        {
            if (suspendedSale == null)
            {
                return;
            }

            _saleItems.Clear();
            foreach (var item in suspendedSale.Items)
            {
                _saleItems.Add(new SaleItem
                {
                    ProductID = item.ProductID,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    TotalPrice = item.TotalPrice
                });
            }

            DiscountTextBox.Text = suspendedSale.Discount.ToDisplayNumber();
            TaxTextBox.Text = suspendedSale.Tax.ToDisplayNumber();
            NotesTextBox.Text = suspendedSale.Notes;

            PaymentMethodComboBox.SelectedIndex = suspendedSale.PaymentMethod switch
            {
                "Card" => 1,
                "Transfer" => 2,
                "Partial" => 3,
                "Credit" => 3,
                _ => 0
            };

            if (CustomerComboBox.ItemsSource is List<Customer> customers)
            {
                if (suspendedSale.CustomerID.HasValue && suspendedSale.CustomerID.Value > 0)
                {
                    CustomerComboBox.SelectedItem = customers.FirstOrDefault(c => c.CustomerID == suspendedSale.CustomerID.Value);
                }
                else
                {
                    CustomerComboBox.SelectedIndex = 0;
                }
            }

            _ = MessageBox.Show($"تم استدعاء الفاتورة المعلقة #{suspendedSale.SuspendedSaleID}.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            CalculateTotals(null, null);
        }

        /// <summary>
        /// تحويل لعرض سعر
        /// </summary>
        private void ConvertToQuote_Click(object sender, RoutedEventArgs e)
        {
            _ = MessageBox.Show("سيتم تحويل الفاتورة إلى عرض سعر", "عرض سعر", MessageBoxButton.OK, MessageBoxImage.Information);
            // يمكن إضافة منطق تحويل لعرض سعر هنا
        }

        /// <summary>
        /// إلغاء
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ScanBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var scannerWindow = new BarcodeScannerWindow
                {
                    Owner = this
                };
                if (scannerWindow.ShowDialog() == true)
                {
                    string code = scannerWindow.ScannedCode;
                    if (string.IsNullOrEmpty(code)) return;

                    var product = ProductDAL.GetProductByCode(code);
                    if (product == null)
                    {
                        // Fallback: try by ID if numeric
                        if (int.TryParse(code, out int id))
                        {
                            product = ProductDAL.GetProductById(id);
                        }
                    }

                    if (product != null)
                    {
                        // Select product in dropdown (optional)
                        // ProductComboBox.SelectedItem = product; // This might fail if references don't match, better to just use logic

                        AddProductToCart(product);
                    }
                    else
                    {
                        _ = MessageBox.Show($"لم يتم العثور على منتج بهذا الكود: {code}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في المسح: {ex.Message}");
            }
        }

        /// <summary>
        /// معالجة الباركود المقروء (للبحث عن المنتج وإضافته مباشرة)
        /// </summary>
        private void ProcessBarcode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            var product = ProductDAL.GetProductByCode(code);
            if (product == null)
            {
                // Fallback: try by ID if numeric
                if (int.TryParse(code, out int id))
                {
                    product = ProductDAL.GetProductById(id);
                }
            }

            if (product != null)
            {
                AddProductToCart(product);
            }
            else
            {
                _ = MessageBox.Show($"لم يتم العثور على منتج بهذا الكود: {code}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddProductToCart(Product product, int quantity = 1)
        {
            if (product == null || quantity <= 0)
            {
                return;
            }

            decimal unitPrice = product.SellingPrice;
            // Check promotions
            decimal discountPercent = 0;
            var bestPromo = PromotionDAL.GetBestPromotionForProduct(product.ProductID, product.Category, unitPrice);
            if (bestPromo != null)
            {
                if (bestPromo.DiscountType == "Percentage")
                {
                    discountPercent = bestPromo.DiscountValue;
                }
                else
                {
                    if (unitPrice > 0)
                        discountPercent = (bestPromo.DiscountValue / unitPrice) * 100;
                }
            }

            var existingItem = _saleItems.FirstOrDefault(item => item.ProductID == product.ProductID);
            int currentQuantityInCart = existingItem?.Quantity ?? 0;
            int requestedTotalQuantity = currentQuantityInCart + quantity;

            if (requestedTotalQuantity > product.Quantity)
            {
                _ = MessageBox.Show(
                    $"لا تتوفر كمية كافية من المنتج '{product.Name}'.\n" +
                    $"المتوفر: {product.Quantity}\n" +
                    $"الموجود في السلة: {currentQuantityInCart}",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (existingItem != null)
            {
                existingItem.Quantity = requestedTotalQuantity;
                // Recalculate with potentially new discount? Usually discount stays same unless tier based.
                existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice * (1 - existingItem.DiscountPercent / 100);
            }
            else
            {
                _saleItems.Add(new SaleItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.Name,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    DiscountPercent = discountPercent,
                    TotalPrice = quantity * unitPrice * (1 - discountPercent / 100),
                    SaleID = _sale?.SaleID ?? 0
                });
            }
            CalculateTotals(null, null);
        }

        #region UI Event Handlers

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _quickSearchDebounceTimer.Stop();
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            Close();
        }

        private void QuickSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text.Contains("بحث"))
            {
                textBox.Text = "";
                textBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void QuickSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "بحث سريع عن منتج (باركود / اسم)...";
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void QuickSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_quickSearchDebounceTimer == null || sender is not TextBox textBox)
            {
                return;
            }

            // تجاهل نص الـ placeholder
            if (textBox.Foreground == System.Windows.Media.Brushes.Gray && textBox.Text.Contains("بحث"))
            {
                return;
            }

            _quickSearchDebounceTimer.Stop();
            _quickSearchDebounceTimer.Start();
        }

        private async void QuickSearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            _quickSearchDebounceTimer.Stop();

            string query = QuickSearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query) || query.Contains("بحث"))
            {
                query = null;
            }

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                var products = string.IsNullOrWhiteSpace(query)
                    ? await ProductDAL.GetProductsPageAsync(
                        limit: InitialProductLoadLimit,
                        offset: 0,
                        searchTerm: null)
                    : await ProductDAL.SearchProductsAsync(query, InitialProductLoadLimit);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                ProductComboBox.ItemsSource = products;
                ProductComboBox.IsDropDownOpen = !string.IsNullOrWhiteSpace(query) && products.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل البحث السريع في شاشة البيع");
            }
        }

        private void IncreaseQtyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SaleItem item)
            {
                var product = ProductDAL.GetProductById(item.ProductID);
                if (product == null)
                {
                    _ = MessageBox.Show("لا يمكن تحديث الكمية لأن المنتج غير موجود.", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (item.Quantity + 1 > product.Quantity)
                {
                    _ = MessageBox.Show($"الكمية غير متوفرة. المتاح من المنتج '{product.Name}': {product.Quantity}",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                item.Quantity++;
                CalculateTotals(null, null);
                SaleItemsGrid.Items.Refresh();
            }
        }

        private void DecreaseQtyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SaleItem item)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    CalculateTotals(null, null);
                    SaleItemsGrid.Items.Refresh();
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // F1: التركيز على اختيار المنتج
            if (e.Key == System.Windows.Input.Key.F1)
            {
                _ = ProductComboBox.Focus();
                ProductComboBox.IsDropDownOpen = true;
                e.Handled = true;
            }
            // F2: البحث السريع
            else if (e.Key == System.Windows.Input.Key.F2)
            {
                _ = QuickSearchBox.Focus();
                e.Handled = true;
            }
            // Ctrl + S: حفظ
            else if (e.Key == System.Windows.Input.Key.S && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                _printAfterSave = false;
                _ = SaveSaleAsync();
                e.Handled = true;
            }
            // Ctrl + P: حفظ وطباعة
            else if (e.Key == System.Windows.Input.Key.P && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                _printAfterSave = true;
                _ = SaveSaleAsync();
                e.Handled = true;
            }
            // Esc: إغلاق
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                var result = MessageBox.Show("هل تريد إلغاء الفاتورة الحالية؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Close();
                }
                e.Handled = true;
            }

            // التقاط ضغطة Enter للماسح الضوئي
            // إذا كان المستخدم لا يكتب داخل مربع بحث/نص وكان الباركود في الـ Buffer
            if (e.Key == System.Windows.Input.Key.Enter && !string.IsNullOrEmpty(_barcodeBuffer))
            {
                TimeSpan timeSinceLastKeystroke = DateTime.Now - _lastKeystrokeTime;
                
                // إذا تم إدخال آخر حرف بسرعة (دليل على أنه ماسح ضوئي وليس مستخدم)
                if (timeSinceLastKeystroke.TotalMilliseconds < 100)
                {
                    ProcessBarcode(_barcodeBuffer);
                    _barcodeBuffer = string.Empty;
                    e.Handled = true; // منع الـ Enter من عمل شيء آخر
                }
                else
                {
                    _barcodeBuffer = string.Empty; // تصفير إذا كان الإدخال بطيئاً جداً
                }
            }
        }

        /// <summary>
        /// التقاط الحروف المكتوبة بسرعة (من الماسح الضوئي)
        /// </summary>
        private void SaleDialog_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            TimeSpan timeSinceLastKeystroke = DateTime.Now - _lastKeystrokeTime;
            
            // إذا كان الوقت منذ آخر نقرة أكثر من 100 ملي ثانية (كتابة بشرية وليس ماسح)، صفر الـ Buffer
            if (timeSinceLastKeystroke.TotalMilliseconds > 100)
            {
                _barcodeBuffer = string.Empty;
            }

            // نضيف الحرف الجديد
            _barcodeBuffer += e.Text;
            _lastKeystrokeTime = DateTime.Now;
        }

        #endregion
    }
}


