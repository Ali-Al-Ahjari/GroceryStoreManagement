using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GroceryStoreManagement.Helpers;

namespace GroceryStoreManagement.Windows
{
    public partial class SuppliersWindow : UserControl
    {
        private List<Supplier> _allSuppliers = [];

        public SuppliersWindow()
        {
            InitializeComponent();
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            try
            {
                _allSuppliers = SupplierDAL.GetAllSuppliers();
                SuppliersGrid.ItemsSource = _allSuppliers;

                // تحديث معلومات التلخيص
                TotalSuppliersCountText.Text = _allSuppliers.Count.ToString();

                decimal totalValue = 0;
                foreach (var supplier in _allSuppliers)
                {
                    totalValue += supplier.TotalSuppliedValue;
                }
                TotalSuppliedValueText.Text = totalValue.ToDisplayCurrency();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل الموردين: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterSuppliers()
        {
            try
            {
                // التحقق من جاهزية العناصر
                if (_allSuppliers == null || SuppliersGrid == null)
                    return;

                var filteredSuppliers = _allSuppliers;

                // تطبيق فلتر البحث
                if (SearchTextBox != null && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    string searchTerm = SearchTextBox.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                    filteredSuppliers = [.. filteredSuppliers.Where(s =>
                        (s.Name != null && s.Name.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) ||
                        (s.Phone != null && s.Phone.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) ||
                        (s.Email != null && s.Email.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) ||
                        (s.Address != null && s.Address.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase))
                    )];
                }

                SuppliersGrid.ItemsSource = filteredSuppliers;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تصفية الموردين: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterSuppliers();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterSuppliers();
        }

        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.ManageSuppliers)) return;

                var dialog = new SupplierDialog();
                if (dialog.ShowDialog() == true)
                {
                    // إعادة تحميل الموردين
                    LoadSuppliers();

                    _ = MessageBox.Show("تم إضافة المورد بنجاح",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إضافة المورد: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.ManageSuppliers)) return;

                if (sender is Button button && button.Tag != null)
                {
                    int supplierId = Convert.ToInt32(button.Tag);
                    var supplier = SupplierDAL.GetSupplierById(supplierId);

                    if (supplier != null)
                    {
                        var dialog = new SupplierDialog(supplier);
                        if (dialog.ShowDialog() == true)
                        {
                            // إعادة تحميل الموردين
                            LoadSuppliers();

                            _ = MessageBox.Show("تم تحديث المورد بنجاح",
                                "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تعديل المورد: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.ManageSuppliers)) return;

                if (sender is Button button && button.Tag != null)
                {
                    int supplierId = Convert.ToInt32(button.Tag);
                    var supplier = SupplierDAL.GetSupplierById(supplierId);

                    if (supplier != null)
                    {
                        var result = MessageBox.Show($"هل تريد حذف المورد '{supplier.Name}'؟",
                            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            bool success = SupplierDAL.DeleteSupplier(supplierId);
                            if (success)
                            {
                                LoadSuppliers();
                                _ = MessageBox.Show("تم حذف المورد بنجاح",
                                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حذف المورد: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SuppliersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة وظائف عند تحديد مورد
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSuppliers();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.ExportReports)) return;

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.csv)|*.csv",
                    FileName = $"Suppliers_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var lines = new List<string>
                    {
                        "رقم المورد,الاسم,الهاتف,البريد الإلكتروني,العنوان,عدد المنتجات,إجمالي قيمة التوريد"
                    };

                    foreach (var supplier in _allSuppliers)
                    {
                        var line = $"{supplier.SupplierID},{supplier.Name},{supplier.Phone},{supplier.Email},{supplier.Address},{supplier.ProductCount},{supplier.TotalSuppliedValue}";
                        lines.Add(line);
                    }

                    System.IO.File.WriteAllLines(saveDialog.FileName, lines, System.Text.Encoding.UTF8);
                    _ = MessageBox.Show($"تم تصدير {_allSuppliers.Count} مورد بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تصدير البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
