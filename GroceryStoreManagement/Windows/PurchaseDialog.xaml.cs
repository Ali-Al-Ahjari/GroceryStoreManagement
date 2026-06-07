using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة إضافة/تعديل فاتورة شراء
    /// </summary>
    public partial class PurchaseDialog : Window
    {
        private readonly ObservableCollection<PurchaseItem> _purchaseItems = [];
        private readonly Purchase _purchase;
        private readonly bool _isEditMode = false;

        /// <summary>
        /// المُنشئ - فاتورة جديدة
        /// </summary>
        public PurchaseDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializeControls();
        }

        /// <summary>
        /// المُنشئ - تعديل فاتورة موجودة
        /// </summary>
        public PurchaseDialog(Purchase purchase)
        {
            InitializeComponent();
            _purchase = purchase;
            _isEditMode = true;
            InitializeControls();
            LoadPurchaseData();
        }

        /// <summary>
        /// تهيئة عناصر التحكم
        /// </summary>
        private void InitializeControls()
        {
            // تفعيل التنقل بمفتاح Enter
            Helpers.EnterKeyHelper.EnableEnterKeyNavigation(this);
            
            LoadSuppliers();
            LoadProducts();
            PurchaseItemsGrid.ItemsSource = _purchaseItems;

            HeaderText.Text = _isEditMode ? "تعديل فاتورة شراء" : "فاتورة شراء جديدة";
            Title = _isEditMode ? "تعديل فاتورة شراء" : "فاتورة شراء جديدة";

            CalculateTotals(null, null);
        }

        /// <summary>
        /// تحميل بيانات الفاتورة للتعديل
        /// </summary>
        private void LoadPurchaseData()
        {
            if (_purchase != null)
            {
                InvoiceNumberText.Text = $"فاتورة رقم: {_purchase.PurchaseID}";
                SupplierInvoiceTextBox.Text = _purchase.InvoiceNumber;
                DiscountTextBox.Text = _purchase.Discount.ToString();
                PaidAmountTextBox.Text = _purchase.PaidAmount.ToString();
                NotesTextBox.Text = _purchase.Notes;

                // تحميل عناصر الفاتورة
                var items = PurchaseItemDAL.GetPurchaseItemsByPurchaseId(_purchase.PurchaseID);
                foreach (var item in items)
                {
                    _purchaseItems.Add(item);
                }

                // تحديد المورد
                if (SupplierComboBox.ItemsSource is List<Supplier> suppliers && _purchase.SupplierID.HasValue)
                {
                    var supplier = suppliers.FirstOrDefault(s => s.SupplierID == _purchase.SupplierID.Value);
                    if (supplier != null)
                    {
                        SupplierComboBox.SelectedItem = supplier;
                    }
                }

                CalculateTotals(null, null);
            }
        }

        /// <summary>
        /// تحميل الموردين
        /// </summary>
        private void LoadSuppliers()
        {
            try
            {
                var suppliers = SupplierDAL.GetAllSuppliers();
                SupplierComboBox.ItemsSource = suppliers;
                if (suppliers.Count > 0)
                {
                    SupplierComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل الموردين: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحميل المنتجات
        /// </summary>
        private void LoadProducts()
        {
            try
            {
                var products = ProductDAL.GetAllProducts();
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
                // استخدام سعر الشراء
                PriceTextBox.Text = selectedProduct.PurchasePrice.ToString();
            }
        }

        /// <summary>
        /// إضافة مورد جديد
        /// </summary>
        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SupplierDialog();
                if (dialog.ShowDialog() == true)
                {
                    LoadSuppliers();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إضافة المورد: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// إضافة منتج للفاتورة
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

                _ = decimal.TryParse(PriceTextBox.Text, out decimal unitPrice);

                var existingItem = _purchaseItems.FirstOrDefault(item => item.ProductID == selectedProduct.ProductID);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
                }
                else
                {
                    var purchaseItem = new PurchaseItem
                    {
                        ProductID = selectedProduct.ProductID,
                        ProductName = selectedProduct.Name,
                        ProductCode = selectedProduct.Code,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = quantity * unitPrice
                    };
                    _purchaseItems.Add(purchaseItem);
                }

                CalculateTotals(null, null);
                PurchaseItemsGrid.Items.Refresh();
                QuantityTextBox.Text = "1";
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إضافة المنتج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IncreaseInputQty_Click(object sender, RoutedEventArgs e)
        {
            int current = ParseInputQuantity();
            QuantityTextBox.Text = (current + 1).ToString();
        }

        private void DecreaseInputQty_Click(object sender, RoutedEventArgs e)
        {
            int current = ParseInputQuantity();
            QuantityTextBox.Text = Math.Max(1, current - 1).ToString();
        }

        private int ParseInputQuantity()
        {
            if (!int.TryParse(QuantityTextBox.Text, out int current) || current < 1)
            {
                current = 1;
            }

            return current;
        }

        private void IncreaseItemQty_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PurchaseItem item)
            {
                item.Quantity++;
                item.TotalPrice = item.Quantity * item.UnitPrice;
                PurchaseItemsGrid.Items.Refresh();
                CalculateTotals(null, null);
            }
        }

        private void DecreaseItemQty_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PurchaseItem item && item.Quantity > 1)
            {
                item.Quantity--;
                item.TotalPrice = item.Quantity * item.UnitPrice;
                PurchaseItemsGrid.Items.Refresh();
                CalculateTotals(null, null);
            }
        }

        /// <summary>
        /// حذف منتج من الفاتورة
        /// </summary>
        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    int productId = Convert.ToInt32(button.Tag);
                    var itemToRemove = _purchaseItems.FirstOrDefault(item => item.ProductID == productId);

                    if (itemToRemove != null)
                    {
                        _ = _purchaseItems.Remove(itemToRemove);
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
                // التحقق من أن العناصر جاهزة
                if (ItemCountText == null || SubtotalText == null ||
                    TotalAmountText == null || RemainingAmountText == null)
                    return;

                decimal subtotal = _purchaseItems.Sum(item => item.TotalPrice);
                decimal discount = 0;
                decimal paidAmount = 0;

                if (DiscountTextBox != null)
                    _ = decimal.TryParse(DiscountTextBox.Text, out discount);
                if (PaidAmountTextBox != null)
                    _ = decimal.TryParse(PaidAmountTextBox.Text, out paidAmount);

                decimal total = subtotal - discount;
                decimal remaining = total - paidAmount;

                ItemCountText.Text = _purchaseItems.Count.ToString();
                SubtotalText.Text = subtotal.ToDisplayCurrency();
                _ = (DiscountAmountText?.Text = $"-{discount.ToDisplayCurrency()}");
                TotalAmountText.Text = total.ToDisplayCurrency();
                RemainingAmountText.Text = remaining.ToDisplayCurrency();
            }
            catch
            {
                // تجاهل الأخطاء أثناء الكتابة
            }
        }

        /// <summary>
        /// حفظ فاتورة الشراء
        /// </summary>
        private void SavePurchase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_purchaseItems.Count == 0)
                {
                    _ = MessageBox.Show("الرجاء إضافة منتجات على الأقل", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SupplierComboBox.SelectedItem is not Supplier selectedSupplier)
                {
                    _ = MessageBox.Show("الرجاء اختيار مورد", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal subtotal = _purchaseItems.Sum(item => item.TotalPrice);

                _ = decimal.TryParse(DiscountTextBox.Text, out decimal discount);
                _ = decimal.TryParse(PaidAmountTextBox.Text, out decimal paidAmount);

                decimal total = subtotal - discount;

                // تحديد حالة الدفع
                string paymentStatus = "Unpaid";
                if (paidAmount >= total)
                    paymentStatus = "Paid";
                else if (paidAmount > 0)
                    paymentStatus = "Partial";

                var purchase = _isEditMode ? _purchase : new Purchase();
                purchase.SupplierID = selectedSupplier.SupplierID;
                purchase.SupplierName = selectedSupplier.Name;
                purchase.TotalAmount = subtotal;
                purchase.Discount = discount;
                purchase.PaidAmount = paidAmount;
                purchase.PaymentStatus = paymentStatus;
                purchase.InvoiceNumber = SupplierInvoiceTextBox.Text;
                purchase.Notes = NotesTextBox.Text;
                purchase.ItemCount = _purchaseItems.Count;

                if (!_isEditMode)
                {
                    purchase.PurchaseDate = DateTime.Now;
                    purchase.IsImported = false;
                }

                int purchaseId;
                if (_isEditMode)
                {
                    _ = PurchaseDAL.UpdatePurchase(purchase);
                    purchaseId = purchase.PurchaseID;

                    // حذف العناصر القديمة وإضافة الجديدة
                    _ = PurchaseItemDAL.DeleteAllPurchaseItems(purchaseId);
                }
                else
                {
                    purchaseId = PurchaseDAL.AddPurchase(purchase);
                }

                foreach (var item in _purchaseItems)
                {
                    item.PurchaseID = purchaseId;
                    _ = PurchaseItemDAL.AddPurchaseItem(item);
                }

                _ = MessageBox.Show("تم حفظ فاتورة الشراء بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حفظ الفاتورة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// إلغاء
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}


