using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Windows;

namespace GroceryStoreManagement.Windows
{
    public partial class UpdateQuantityDialog : Window
    {
        private readonly Product _product;

        public UpdateQuantityDialog(Product product)
        {
            InitializeComponent();
            _product = product;
            LoadProductData();
            
            // تفعيل التنقل بمفتاح Enter
            EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void LoadProductData()
        {
            ProductNameText.Text = _product.Name;
            CurrentQuantityText.Text = $"الكمية الحالية: {_product.Quantity}";
            NewQuantityTextBox.Text = _product.Quantity.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من صحة البيانات
                if (!int.TryParse(NewQuantityTextBox.Text, out int newQuantity))
                {
                    _ = MessageBox.Show("الرجاء إدخال رقم صحيح للكمية", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // التحقق من صحة الكمية باستخدام Validator
                if (!Validator.ValidateQuantity(NewQuantityTextBox.Text, out string errorMessage))
                {
                    _ = MessageBox.Show(errorMessage, "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // حساب الفرق في الكمية
                int quantityDifference = newQuantity - _product.Quantity;

                // تحديث الكمية في قاعدة البيانات
                bool success = ProductDAL.UpdateProductQuantity(_product.ProductID, quantityDifference);

                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    _ = MessageBox.Show("حدث خطأ في تحديث الكمية", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحديث الكمية: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
