// =====================================================
// ProductDialog.xaml.cs - نافذة إضافة/تعديل منتج
// هذا الملف يتحكم في منطق إضافة وتعديل المنتجات
// =====================================================

using GroceryStoreManagement.DAL;       // طبقة الوصول للبيانات
using GroceryStoreManagement.Helpers;   // فئات المساعدة والتحقق
using GroceryStoreManagement.Models;    // نموذج المنتج
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة إضافة/تعديل منتج
    /// تُستخدم لإدخال بيانات منتج جديد أو تعديل منتج موجود
    /// </summary>
    public partial class ProductDialog : Window
    {
        // المنتج الحالي (في حالة التعديل)
        private Product _product;

        // هل نحن في وضع التعديل أم الإضافة
        private readonly bool _isEditMode;

        // للحفظ وإضافة جديد
        private bool _saveAndAddNew = false;

        /// <summary>
        /// المُنشئ - وضع إضافة منتج جديد
        /// </summary>
        public ProductDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializeDialog();
            GenerateProductCode();
        }

        /// <summary>
        /// المُنشئ - وضع تعديل منتج موجود
        /// </summary>
        public ProductDialog(Product product)
        {
            InitializeComponent();
            _product = product;
            _isEditMode = true;
            InitializeDialog();
            LoadProductData();
        }

        /// <summary>
        /// تهيئة النافذة - تحميل الفئات والموردين
        /// </summary>
        private void InitializeDialog()
        {
            // تفعيل التنقل بمفتاح Enter
            EnterKeyHelper.EnableEnterKeyNavigation(this);
            
            // تحميل قائمة الفئات
            var categories = ProductDAL.GetAllCategories();
            _ = CategoryComboBox.Items.Add("");
            foreach (var category in categories)
            {
                _ = CategoryComboBox.Items.Add(category);
            }

            // تحميل قائمة الموردين
            var suppliers = SupplierDAL.GetAllSuppliers();
            _ = SupplierComboBox.Items.Add("");
            foreach (var supplier in suppliers)
            {
                _ = SupplierComboBox.Items.Add(supplier.Name);
            }

            // ضبط عنوان النافذة
            Title = _isEditMode ? "تعديل منتج" : "إضافة منتج جديد";
            HeaderText.Text = _isEditMode ? "تعديل منتج" : "إضافة منتج جديد";

            // ربط أحداث تحديث الربح
            PurchasePriceTextBox.TextChanged += UpdateProfit;
            SellingPriceTextBox.TextChanged += UpdateProfit;
        }

        /// <summary>
        /// توليد كود المنتج تلقائياً
        /// </summary>
        private void GenerateProductCode()
        {
            CodeTextBox.Text = ProductDAL.GenerateProductCode();
        }

        private void GenerateCode_Click(object sender, RoutedEventArgs e)
        {
            GenerateProductCode();
        }

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string code = CodeTextBox.Text;
            if (!string.IsNullOrEmpty(code))
            {
                // توليد صورة الباركود
                var barcode = BarcodeGenerator.GenerateBarcode(code, 300, 80);
                if (barcode != null)
                {
                    BarcodeImage.Source = barcode;
                    BarcodeImage.Visibility = Visibility.Visible;
                    PrintBarcodeBtn.Visibility = Visibility.Visible;
                }
            }
            else
            {
                BarcodeImage.Visibility = Visibility.Collapsed;
                PrintBarcodeBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void PrintBarcode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CodeTextBox.Text)) return;

            try
            {
                PrintDialog printDialog = new();
                if (printDialog.ShowDialog() == true)
                {
                    // تصميم بسيط للطباعة
                    StackPanel printPanel = new()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(20)
                    };

                    _ = printPanel.Children.Add(new TextBlock
                    {
                        Text = NameTextBox.Text,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 10)
                    });

                    Image printImage = new()
                    {
                        Source = BarcodeImage.Source,
                        Height = 80,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };
                    _ = printPanel.Children.Add(printImage);

                    _ = printPanel.Children.Add(new TextBlock
                    {
                        Text = CodeTextBox.Text,
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 5, 0, 0)
                    });

                    _ = printPanel.Children.Add(new TextBlock
                    {
                        Text = $"S.P: {SellingPriceTextBox.Text}",
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 5, 0, 0)
                    });

                    // قياس وترتيب العناصر للطباعة
                    printPanel.Measure(new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight));
                    printPanel.Arrange(new Rect(new Point(0, 0), printPanel.DesiredSize));

                    printDialog.PrintVisual(printPanel, $"Barcode_{CodeTextBox.Text}");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحميل بيانات المنتج الحالي في حقول الإدخال
        /// </summary>
        private void LoadProductData()
        {
            if (_product != null)
            {
                CodeTextBox.Text = _product.Code;
                NameTextBox.Text = _product.Name;
                PurchasePriceTextBox.Text = _product.PurchasePrice.ToString();
                SellingPriceTextBox.Text = _product.SellingPrice.ToString();
                QuantityTextBox.Text = _product.Quantity.ToString();
                MinQuantityTextBox.Text = _product.MinQuantity.ToString();
                ExpiryDatePicker.SelectedDate = _product.ExpiryDate; // تحميل تاريخ الانتهاء

                // تحديد الوحدة
                if (!string.IsNullOrEmpty(_product.Unit))
                {
                    foreach (ComboBoxItem item in UnitComboBox.Items)
                    {
                        if (item.Content?.ToString() == _product.Unit)
                        {
                            UnitComboBox.SelectedItem = item;
                            break;
                        }
                    }
                    if (UnitComboBox.SelectedIndex < 0)
                    {
                        UnitComboBox.Text = _product.Unit;
                    }
                }

                // تحديد الفئة
                if (!string.IsNullOrEmpty(_product.Category))
                {
                    CategoryComboBox.SelectedItem = _product.Category;
                    if (CategoryComboBox.SelectedIndex < 0)
                    {
                        CategoryComboBox.Text = _product.Category;
                    }
                }

                // تحديد المورد
                if (!string.IsNullOrEmpty(_product.SupplierName))
                {
                    SupplierComboBox.SelectedItem = _product.SupplierName;
                }

                // عرض معلومات التدقيق
                ShowAuditInfo();

            }
        }

        private void ShowAuditInfo()
        {
            if (_isEditMode && _product != null)
            {
                string info = AuditHelper.GetAuditInfo(
                    _product.CreatedDate,
                    AuditHelper.GetUserName(_product.CreatedBy),
                    _product.ModifiedDate,
                    AuditHelper.GetUserName(_product.ModifiedBy));

                if (!string.IsNullOrEmpty(info))
                {
                    AuditInfoText.Text = info;
                    AuditInfoBorder.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// تحديث عرض الربح
        /// </summary>
        private void UpdateProfit(object sender, EventArgs e)
        {
            try
            {
                _ = decimal.TryParse(PurchasePriceTextBox.Text, out decimal purchasePrice);
                _ = decimal.TryParse(SellingPriceTextBox.Text, out decimal sellingPrice);

                decimal profit = sellingPrice - purchasePrice;
                ProfitText.Text = profit.ToDisplayCurrency();
            }
            catch
            {
                ProfitText.Text = "$0.00";
            }
        }

        private void ClearForm()
        {
            GenerateProductCode();
            NameTextBox.Text = "";
            PurchasePriceTextBox.Text = "0";
            SellingPriceTextBox.Text = "0";
            QuantityTextBox.Text = "0";
            MinQuantityTextBox.Text = "5";
            UnitComboBox.SelectedIndex = 0;
            CategoryComboBox.SelectedIndex = 0;
            SupplierComboBox.SelectedIndex = 0;
            ExpiryDatePicker.SelectedDate = null;
            _ = NameTextBox.Focus();
        }

        /// <summary>
        /// معالج حدث النقر على زر الحفظ
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _saveAndAddNew = false;
            SaveProduct();
        }

        private void SaveAndAddNewButton_Click(object sender, RoutedEventArgs e)
        {
            _saveAndAddNew = true;
            SaveProduct();
        }

        private void SaveProduct()
        {
            try
            {
                if (!ValidateData())
                    return;

                Product product = _isEditMode ? _product : new Product();

                product.Code = CodeTextBox.Text.Trim();
                product.Name = NameTextBox.Text.Trim();

                // استخدام TryParse لتجنب أخطاء التحويل مع القيم الفارغة

                _ = decimal.TryParse(PurchasePriceTextBox.Text, out decimal purchasePrice);
                _ = decimal.TryParse(SellingPriceTextBox.Text, out decimal sellingPrice);
                _ = int.TryParse(QuantityTextBox.Text, out int quantity);
                if (!int.TryParse(MinQuantityTextBox.Text, out int minQuantity) || minQuantity < 0)
                {
                    minQuantity = 5;
                }

                product.PurchasePrice = purchasePrice;
                product.SellingPrice = sellingPrice;
                product.Quantity = quantity;
                product.MinQuantity = minQuantity;
                product.ExpiryDate = ExpiryDatePicker.SelectedDate; // تاريخ الانتهاء

                // الوحدة
                if (UnitComboBox.SelectedItem is ComboBoxItem unitItem)
                {
                    product.Unit = unitItem.Content?.ToString();
                }
                else
                {
                    product.Unit = UnitComboBox.Text;
                }

                // الفئة
                product.Category = CategoryComboBox.SelectedItem?.ToString() ?? CategoryComboBox.Text;

                // المورد
                if (!string.IsNullOrEmpty(SupplierComboBox.SelectedItem?.ToString()))
                {
                    var supplierName = SupplierComboBox.SelectedItem.ToString();
                    var suppliers = SupplierDAL.GetAllSuppliers();
                    var supplier = suppliers.FirstOrDefault(s => s.Name == supplierName);
                    if (supplier != null)
                    {
                        product.SupplierID = supplier.SupplierID;
                        product.SupplierName = supplierName;
                    }
                }
                else
                {
                    product.SupplierID = null;
                }

                if (!_isEditMode)
                {
                    product.CreatedDate = DateTime.Now;
                }

                // حفظ المنتج
                if (_isEditMode)
                {
                    _ = ProductDAL.UpdateProduct(product);
                    _ = MessageBox.Show("تم تحديث المنتج بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _ = ProductDAL.AddProduct(product);
                    _ = MessageBox.Show("تم إضافة المنتج بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (_saveAndAddNew)
                {
                    ClearForm();
                    _product = null;
                }
                else
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حفظ المنتج: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// التحقق من صحة البيانات المدخلة
        /// </summary>
        private bool ValidateData()
        {

            if (!Validator.ValidateName(NameTextBox.Text, out string errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = NameTextBox.Focus();
                return false;
            }

            if (!Validator.ValidatePrice(SellingPriceTextBox.Text, out errorMessage))
            {
                _ = MessageBox.Show("سعر البيع: " + errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = SellingPriceTextBox.Focus();
                return false;
            }

            if (!Validator.ValidateQuantity(QuantityTextBox.Text, out errorMessage))
            {
                _ = MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = QuantityTextBox.Focus();
                return false;
            }

            if (!Validator.ValidateQuantity(MinQuantityTextBox.Text, out errorMessage))
            {
                _ = MessageBox.Show("حد الطلب: " + errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = MinQuantityTextBox.Focus();
                return false;
            }

            // التحقق من أن سعر البيع أكبر من سعر الشراء
            _ = decimal.TryParse(PurchasePriceTextBox.Text, out decimal purchasePrice);
            _ = decimal.TryParse(SellingPriceTextBox.Text, out decimal sellingPrice);

            if (sellingPrice < purchasePrice)
            {
                var result = MessageBox.Show("سعر البيع أقل من سعر الشراء. هل تريد المتابعة؟",
                    "تحذير", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// معالج حدث النقر على زر الإلغاء
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
