using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GroceryStoreManagement.Windows
{
    public partial class InventoryWindow : UserControl
    {
        private List<Product> _allProducts = [];

        public InventoryWindow()
        {
            InitializeComponent();
            LoadCategories();
            LoadInventory();
        }

        private void LoadCategories()
        {
            try
            {
                var categories = ProductDAL.GetAllCategories();
                CmbCategory.Items.Clear();
                _ = CmbCategory.Items.Add("الكل");
                foreach (var category in categories)
                {
                    _ = CmbCategory.Items.Add(category);
                }
                CmbCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private void LoadInventory()
        {
            try
            {
                _allProducts = ProductDAL.GetAllProducts();
                ApplyFilters();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل المخزون: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary()
        {
            try
            {
                TxtTotalProducts.Text = _allProducts.Count.ToString();

                decimal totalValue = 0;
                int lowStockCount = 0;
                int outOfStockCount = 0;

                foreach (var product in _allProducts)
                {
                    totalValue += product.TotalValue;

                    if (product.Quantity <= 0)
                        outOfStockCount++;
                    else if (product.IsLowStock)
                        lowStockCount++;
                }

                TxtInventoryValue.Text = totalValue.ToDisplayCurrency();
                TxtLowStock.Text = lowStockCount.ToString();
                TxtOutOfStock.Text = outOfStockCount.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating summary: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allProducts == null || InventoryDataGrid == null)
                    return;

                var filteredProducts = _allProducts.AsEnumerable();

                // البحث بالاسم أو الكود
                if (TxtSearch != null && !string.IsNullOrWhiteSpace(TxtSearch.Text))
                {
                    string searchTerm = TxtSearch.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                    filteredProducts = filteredProducts.Where(p =>
                        (p.Name != null && p.Name.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) ||
                        (p.Code != null && p.Code.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) ||
                        (p.Category != null && p.Category.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase))
                    );
                }

                // فلتر الفئة
                if (CmbCategory != null && CmbCategory.SelectedIndex > 0)
                {
                    string selectedCategory = CmbCategory.SelectedItem.ToString();
                    filteredProducts = filteredProducts.Where(p => p.Category == selectedCategory);
                }

                // فلتر حالة المخزون
                if (CmbStockStatus != null && CmbStockStatus.SelectedIndex > 0)
                {
                    if (CmbStockStatus.SelectedItem is ComboBoxItem item)
                    {
                        string status = item.Content.ToString();
                        switch (status)
                        {
                            case "نفد":
                                filteredProducts = filteredProducts.Where(p => p.Quantity <= 0);
                                break;
                            case "منخفض":
                                filteredProducts = filteredProducts.Where(p => p.Quantity > 0 && p.IsLowStock);
                                break;
                            case "جيد":
                                filteredProducts = filteredProducts.Where(p => !p.IsLowStock && p.Quantity > 0);
                                break;
                        }
                    }
                }

                InventoryDataGrid.ItemsSource = filteredProducts.ToList();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تصفية المنتجات: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbStockStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            _ = MessageBox.Show("ميزة التصدير ستكون متاحة قريباً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InventoryDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (InventoryDataGrid.SelectedItem is Product product)
            {
                OpenUpdateDialog(product);
            }
        }

        private void BtnUpdateQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                var product = _allProducts.FirstOrDefault(p => p.ProductID == productId);
                if (product != null)
                {
                    OpenUpdateDialog(product);
                }
            }
        }

        private void OpenUpdateDialog(Product product)
        {
            var dialog = new UpdateQuantityDialog(product)
            {
                Owner = Window.GetWindow(this) // Fix Owner since this is UserControl
            };
            if (dialog.ShowDialog() == true)
            {
                LoadInventory();
            }
        }
    }
}

