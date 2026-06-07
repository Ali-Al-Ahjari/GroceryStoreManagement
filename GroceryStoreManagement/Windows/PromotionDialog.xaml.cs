using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GroceryStoreManagement.Windows
{
    public partial class PromotionDialog : Window
    {
        private readonly Promotion _promotion;
        private readonly bool _isEditMode;

        public PromotionDialog(Promotion promotion = null)
        {
            InitializeComponent();
            _promotion = promotion;
            _isEditMode = _promotion != null;

            LoadReferenceData();
            LoadPromotionData();
        }

        private void LoadReferenceData()
        {
            try
            {
                CategoryComboBox.ItemsSource = ProductDAL.GetAllCategories();
                ProductComboBox.ItemsSource = ProductDAL.GetAllProducts();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في تحميل بيانات العروض");
            }
        }

        private void LoadPromotionData()
        {
            if (!_isEditMode)
            {
                DialogTitle.Text = "إضافة عرض جديد";
                StartDatePicker.SelectedDate = DateTime.Today;
                EndDatePicker.SelectedDate = DateTime.Today.AddDays(7);
                return;
            }

            DialogTitle.Text = "تعديل عرض";
            NameTextBox.Text = _promotion.Name;
            DiscountValueTextBox.Text = _promotion.DiscountValue.ToString(CultureInfo.InvariantCulture);
            StartDatePicker.SelectedDate = _promotion.StartDate == DateTime.MinValue ? DateTime.Today : _promotion.StartDate;
            EndDatePicker.SelectedDate = _promotion.EndDate == DateTime.MinValue ? DateTime.Today.AddDays(7) : _promotion.EndDate;
            MinPurchaseTextBox.Text = _promotion.MinPurchase.ToString(CultureInfo.InvariantCulture);
            IsActiveCheckBox.IsChecked = _promotion.IsActive;

            // نوع الخصم
            foreach (ComboBoxItem item in DiscountTypeComboBox.Items)
            {
                if ((item.Tag as string) == _promotion.DiscountType)
                {
                    DiscountTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // نطاق التطبيق
            foreach (ComboBoxItem item in AppliesToComboBox.Items)
            {
                if ((item.Tag as string) == _promotion.AppliesTo)
                {
                    AppliesToComboBox.SelectedItem = item;
                    break;
                }
            }

            if (_promotion.AppliesTo == "Category")
            {
                CategoryComboBox.SelectedItem = _promotion.TargetName;
            }
            else if (_promotion.AppliesTo == "Product")
            {
                ProductComboBox.SelectedValue = _promotion.TargetID;
            }

            UpdateTargetVisibility();
        }

        private void AppliesToComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTargetVisibility();
        }

        private void UpdateTargetVisibility()
        {
            string appliesTo = GetSelectedTag(AppliesToComboBox);
            CategoryPanel.Visibility = appliesTo == "Category" ? Visibility.Visible : Visibility.Collapsed;
            ProductPanel.Visibility = appliesTo == "Product" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = NameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    _ = MessageBox.Show("اسم العرض مطلوب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(DiscountValueTextBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal discountValue) || discountValue <= 0)
                {
                    _ = MessageBox.Show("يرجى إدخال قيمة خصم صحيحة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(MinPurchaseTextBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal minPurchase))
                {
                    minPurchase = 0;
                }

                DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Today;
                DateTime endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
                if (endDate < startDate)
                {
                    _ = MessageBox.Show("تاريخ النهاية يجب أن يكون بعد تاريخ البداية.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string discountType = GetSelectedTag(DiscountTypeComboBox);
                string appliesTo = GetSelectedTag(AppliesToComboBox);

                int targetId = 0;
                string targetName = null;

                if (appliesTo == "Category")
                {
                    targetName = CategoryComboBox.SelectedItem as string;
                    if (string.IsNullOrWhiteSpace(targetName))
                    {
                        _ = MessageBox.Show("يرجى اختيار فئة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else if (appliesTo == "Product")
                {
                    if (ProductComboBox.SelectedValue is not int productId || productId <= 0)
                    {
                        _ = MessageBox.Show("يرجى اختيار منتج.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    targetId = productId;
                }

                var promo = _promotion ?? new Promotion();
                promo.Name = name;
                promo.DiscountType = discountType;
                promo.DiscountValue = discountValue;
                promo.StartDate = startDate;
                promo.EndDate = endDate;
                promo.MinPurchase = minPurchase;
                promo.AppliesTo = appliesTo;
                promo.TargetID = targetId;
                promo.TargetName = targetName;
                promo.IsActive = IsActiveCheckBox.IsChecked ?? true;

                if (_isEditMode)
                {
                    PromotionDAL.UpdatePromotion(promo);
                }
                else
                {
                    PromotionDAL.AddPromotion(promo);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في حفظ العرض");
                _ = MessageBox.Show($"خطأ في حفظ العرض: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string GetSelectedTag(ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
        }
    }
}
