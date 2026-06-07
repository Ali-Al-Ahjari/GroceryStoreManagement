using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GroceryStoreManagement.Helpers;
using System.Windows.Input;

namespace GroceryStoreManagement.Windows
{
    public partial class CustomersWindow : UserControl
    {
        private List<Customer> _allCustomers = [];

        public CustomersWindow()
        {
            InitializeComponent();
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            try
            {
                _allCustomers = CustomerDAL.GetAllCustomers();
                ApplyFilters();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary()
        {
            try
            {
                TxtTotalCustomers.Text = _allCustomers.Count.ToString();
                TxtWithDebt.Text = _allCustomers.Count(c => c.CurrentDebt > 0).ToString();
                TxtTotalDebtAmount.Text = _allCustomers.Sum(c => c.CurrentDebt).ToDisplayNumber();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في تحديث ملخص العملاء");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filteredCustomers = _allCustomers.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
                {
                    string searchTerm = TxtSearch.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                    filteredCustomers = filteredCustomers.Where(c =>
                        (c.Name?.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) == true) ||
                        (c.Phone?.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) == true)
                    );
                }

                if (ChkShowWithDebt.IsChecked == true)
                {
                    filteredCustomers = filteredCustomers.Where(c => c.CurrentDebt > 0);
                }

                CustomersDataGrid.ItemsSource = filteredCustomers.ToList();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تصفية العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ChkShowWithDebt_Checked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ChkShowWithDebt_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.AddCustomers)) return;

                var dialog = new CustomerDialog
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true)
                {
                    LoadCustomers();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إضافة العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEditCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.EditCustomers)) return;

                if (sender is Button button && button.Tag != null)
                {
                    int customerId = Convert.ToInt32(button.Tag);
                    var customer = CustomerDAL.GetCustomerById(customerId);

                    if (customer != null)
                    {
                        var dialog = new CustomerDialog(customer)
                        {
                            Owner = Window.GetWindow(this)
                        };
                        if (dialog.ShowDialog() == true)
                        {
                            LoadCustomers();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تعديل العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.DeleteCustomers)) return;

                if (sender is Button button && button.Tag != null)
                {
                    int customerId = Convert.ToInt32(button.Tag);
                    var customer = CustomerDAL.GetCustomerById(customerId);

                    if (customer != null)
                    {
                        if (MessageBox.Show($"هل تريد حذف العميل '{customer.Name}'؟",
                            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            if (CustomerDAL.DeleteCustomer(customerId))
                            {
                                LoadCustomers();
                                _ = MessageBox.Show("تم حذف العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حذف العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPayDebt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PermissionHelper.CheckPermission(PermissionKeys.ManageCustomerDebt)) return;

                if (sender is Button button && button.Tag != null)
                {
                    int customerId = Convert.ToInt32(button.Tag);
                    var customer = CustomerDAL.GetCustomerById(customerId);
                    if (customer != null)
                    {
                        var dialog = new DebtPaymentDialog(customer)
                        {
                            Owner = Window.GetWindow(this)
                        };
                        if (dialog.ShowDialog() == true)
                        {
                            LoadCustomers();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ");
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = CustomersDataGrid.ItemsSource as IEnumerable<Customer> ?? _allCustomers;

                string filePath = ExportHelper.ShowSaveFileDialog(
                    $"customers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|PDF (*.pdf)|*.pdf|Text (*.txt)|*.txt");

                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                ExportFormat format = filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                    ? ExportFormat.CSV
                    : filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                        ? ExportFormat.PDF
                    : filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                        ? ExportFormat.Text
                        : ExportFormat.Excel;

                if (ExportHelper.ExportReport(data, filePath, format))
                {
                    _ = MessageBox.Show("تم تصدير العملاء بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في تصدير العملاء");
                _ = MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CustomersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomersDataGrid.SelectedItem is Customer customer)
            {
                try
                {
                    if (!PermissionHelper.CheckPermission(PermissionKeys.EditCustomers)) return;

                    var dialog = new CustomerDialog(customer)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    if (dialog.ShowDialog() == true)
                    {
                        LoadCustomers();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "خطأ في فتح تعديل العميل");
                    _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

