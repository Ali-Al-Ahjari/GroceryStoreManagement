using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة إدارة المشتريات
    /// </summary>
    public partial class PurchasesWindow : UserControl
    {
        private List<Purchase> _allPurchases = [];

        public PurchasesWindow()
        {
            InitializeComponent();
            LoadPurchases();
        }

        /// <summary>
        /// تحميل المشتريات
        /// </summary>
        private void LoadPurchases()
        {
            try
            {
                _allPurchases = PurchaseDAL.GetAllPurchases();
                ApplyFilters();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل المشتريات: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحديث شريط الملخص
        /// </summary>
        private void UpdateSummary()
        {
            try
            {
                TotalInvoicesText.Text = _allPurchases.Count.ToString();

                decimal totalPurchases = 0;
                decimal totalPaid = 0;
                decimal totalRemaining = 0;

                foreach (var purchase in _allPurchases)
                {
                    totalPurchases += purchase.TotalAmount;
                    totalPaid += purchase.PaidAmount;
                    totalRemaining += purchase.RemainingAmount;
                }

                TotalPurchasesText.Text = totalPurchases.ToDisplayCurrency();
                TotalPaidText.Text = totalPaid.ToDisplayCurrency();
                TotalRemainingText.Text = totalRemaining.ToDisplayCurrency();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating summary: {ex.Message}");
            }
        }

        /// <summary>
        /// تطبيق الفلاتر
        /// </summary>
        private void ApplyFilters()
        {
            try
            {
                // التحقق من جاهزية العناصر
                if (_allPurchases == null || PurchasesGrid == null)
                    return;

                var filteredPurchases = _allPurchases.AsEnumerable();

                // فلتر التاريخ (من)
                if (DateFromPicker != null && DateFromPicker.SelectedDate.HasValue)
                {
                    var fromDate = DateFromPicker.SelectedDate.Value.Date;
                    filteredPurchases = filteredPurchases.Where(p => p.PurchaseDate.Date >= fromDate);
                }

                // فلتر التاريخ (إلى)
                if (DateToPicker != null && DateToPicker.SelectedDate.HasValue)
                {
                    var toDate = DateToPicker.SelectedDate.Value.Date;
                    filteredPurchases = filteredPurchases.Where(p => p.PurchaseDate.Date <= toDate);
                }

                // فلتر حالة الدفع
                if (PaymentStatusComboBox != null)
                {
                    switch (PaymentStatusComboBox.SelectedIndex)
                    {
                        case 1: // مدفوعة
                            filteredPurchases = filteredPurchases.Where(p => p.PaymentStatus == "Paid");
                            break;
                        case 2: // جزئي
                            filteredPurchases = filteredPurchases.Where(p => p.PaymentStatus == "Partial");
                            break;
                        case 3: // غير مدفوعة
                            filteredPurchases = filteredPurchases.Where(p => p.PaymentStatus == "Unpaid");
                            break;
                    }
                }

                // فلتر البحث النصي
                if (SearchTextBox != null && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    string searchTerm = SearchTextBox.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                    filteredPurchases = filteredPurchases.Where(p =>
                        p.PurchaseID.ToString().Contains(searchTerm) ||
                        (p.SupplierName != null && p.SupplierName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) ||
                        (p.InvoiceNumber != null && p.InvoiceNumber.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase))
                    );
                }

                PurchasesGrid.ItemsSource = filteredPurchases.ToList();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تصفية المشتريات: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            PaymentStatusComboBox.SelectedIndex = 0;
            SearchTextBox.Text = "";
            ApplyFilters();
        }

        /// <summary>
        /// فتح نافذة فاتورة شراء جديدة
        /// </summary>
        private void NewPurchase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new PurchaseDialog();
                if (dialog.ShowDialog() == true)
                {
                    LoadPurchases();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إنشاء فاتورة شراء جديدة: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تعديل فاتورة شراء
        /// </summary>
        private void EditPurchase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    int purchaseId = Convert.ToInt32(button.Tag);
                    var purchase = PurchaseDAL.GetPurchaseById(purchaseId);

                    if (purchase != null)
                    {
                        var dialog = new PurchaseDialog(purchase);
                        if (dialog.ShowDialog() == true)
                        {
                            LoadPurchases();
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
        /// سداد المورد
        /// </summary>
        private void PaySupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    int purchaseId = Convert.ToInt32(button.Tag);
                    var purchase = PurchaseDAL.GetPurchaseById(purchaseId);

                    if (purchase != null)
                    {
                        if (purchase.PaymentStatus == "Paid")
                        {
                            _ = MessageBox.Show("هذه الفاتورة مدفوعة بالكامل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        // فتح نافذة تعديل الفاتورة للسداد
                        var dialog = new PurchaseDialog(purchase);
                        if (dialog.ShowDialog() == true)
                        {
                            LoadPurchases();
                            _ = MessageBox.Show("تم السداد بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// استيراد للمخزون
        /// </summary>
        private void ImportToInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    int purchaseId = Convert.ToInt32(button.Tag);
                    var purchase = PurchaseDAL.GetPurchaseById(purchaseId);

                    if (purchase != null)
                    {
                        if (purchase.IsImported)
                        {
                            _ = MessageBox.Show("تم استيراد هذه الفاتورة مسبقاً للمخزون", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var result = MessageBox.Show(
                            "هل تريد استيراد المنتجات من هذه الفاتورة للمخزون؟\nسيتم إضافة الكميات للمنتجات الموجودة.",
                            "تأكيد الاستيراد", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            bool success = PurchaseItemDAL.ImportToInventory(purchaseId);
                            if (success)
                            {
                                LoadPurchases();
                                _ = MessageBox.Show("تم استيراد المنتجات للمخزون بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                _ = MessageBox.Show("حدث خطأ أثناء الاستيراد", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في الاستيراد: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حذف فاتورة شراء
        /// </summary>
        private void DeletePurchase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag != null)
                {
                    int purchaseId = Convert.ToInt32(button.Tag);
                    var purchase = PurchaseDAL.GetPurchaseById(purchaseId);

                    if (purchase != null)
                    {
                        if (purchase.IsImported)
                        {
                            _ = MessageBox.Show("لا يمكن حذف فاتورة تم استيرادها للمخزون", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var result = MessageBox.Show($"هل تريد حذف الفاتورة رقم {purchase.PurchaseID}؟",
                            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            bool success = PurchaseDAL.DeletePurchase(purchaseId);
                            if (success)
                            {
                                LoadPurchases();
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

        private void PurchasesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة وظائف عند تحديد فاتورة
        }

        /// <summary>
        /// مسح مربع البحث
        /// </summary>
        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
        }
    }
}

