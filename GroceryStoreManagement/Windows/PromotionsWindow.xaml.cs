using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GroceryStoreManagement.Windows
{
    public partial class PromotionsWindow : Window
    {
        public PromotionsWindow()
        {
            InitializeComponent();
            LoadPromotions();
        }

        private void LoadPromotions()
        {
            try
            {
                if (FilterComboBox == null || PromotionsDataGrid == null) return;

                var allPromotions = PromotionDAL.GetAllPromotions();

                if (FilterComboBox.SelectedIndex == 1) // Active
                {
                    allPromotions = [.. allPromotions.Where(p => p.Status == "ساري")];
                }
                else if (FilterComboBox.SelectedIndex == 2) // Expired
                {
                    allPromotions = [.. allPromotions.Where(p => p.Status == "منتهي")];
                }

                PromotionsDataGrid.ItemsSource = allPromotions;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل العروض: {ex.Message}");
            }
        }

        private void AddPromotion_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ManagePromotions)) return;

            var dialog = new PromotionDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadPromotions();
            }
        }

        private void EditPromotion_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ManagePromotions)) return;

            if (sender is Button { DataContext: Promotion promo })
            {
                var dialog = new PromotionDialog(promo);
                if (dialog.ShowDialog() == true)
                {
                    LoadPromotions();
                }
            }
        }

        private void DeletePromotion_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionHelper.CheckPermission(PermissionKeys.ManagePromotions)) return;

            var button = sender as Button;
            if (button?.Tag is int id)
            {
                if (MessageBox.Show("هل أنت متأكد من حذف هذا العرض؟", "تأكيد", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        PromotionDAL.DeletePromotion(id);
                        LoadPromotions();
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show($"خطأ في الحذف: {ex.Message}");
                    }
                }
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadPromotions();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PromotionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
