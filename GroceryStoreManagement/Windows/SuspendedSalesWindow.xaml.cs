using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace GroceryStoreManagement.Windows
{
    public partial class SuspendedSalesWindow : Window
    {
        private readonly int? _shiftId;
        private List<SuspendedSale> _rows = [];

        public int SelectedSuspendedSaleId { get; private set; }

        public SuspendedSalesWindow(int? shiftId)
        {
            InitializeComponent();
            EnterKeyHelper.EnableEnterKeyNavigation(this);
            _shiftId = shiftId;
            LoadData();
        }

        private void LoadData()
        {
            _rows = SuspendedSaleDAL.GetSuspendedSales(_shiftId);

            // تحميل بيانات العناصر لحساب الإجماليات بشكل دقيق داخل الجدول.
            for (int i = 0; i < _rows.Count; i++)
            {
                var loaded = SuspendedSaleDAL.GetSuspendedSaleById(_rows[i].SuspendedSaleID);
                if (loaded != null)
                {
                    _rows[i] = loaded;
                }
            }

            SuspendedGrid.ItemsSource = _rows;
        }

        private void ResumeSelected_Click(object sender, RoutedEventArgs e)
        {
            if (SuspendedGrid.SelectedItem is not SuspendedSale selected)
            {
                _ = MessageBox.Show("اختر فاتورة معلقة أولاً.");
                return;
            }

            SelectedSuspendedSaleId = selected.SuspendedSaleID;
            DialogResult = true;
            Close();
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (SuspendedGrid.SelectedItem is not SuspendedSale selected)
            {
                _ = MessageBox.Show("اختر فاتورة معلقة أولاً.");
                return;
            }

            if (MessageBox.Show(
                $"هل تريد حذف الفاتورة المعلقة #{selected.SuspendedSaleID}؟",
                "تأكيد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            _ = SuspendedSaleDAL.DeleteSuspendedSale(selected.SuspendedSaleID, SessionContext.CurrentUserID);
            LoadData();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
