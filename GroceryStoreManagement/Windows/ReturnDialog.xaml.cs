using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;

namespace GroceryStoreManagement.Windows
{
    public partial class ReturnDialog : Window
    {
        private readonly Sale _sale;
        private List<ReturnItemViewModel> _items;
        private Shift _activeShift;

        public ReturnDialog(Sale sale)
        {
            InitializeComponent();
            _sale = sale;
            LoadData();
            
            // تفعيل التنقل بمفتاح Enter
            Helpers.EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void LoadData()
        {
            InvoiceInfoText.Text = $"فاتورة #{_sale.SaleID} - التاريخ: {_sale.SaleDate:dd/MM/yyyy}";
            _activeShift = ShiftDAL.GetOpenShift();

            try
            {
                var saleItems = SaleItemDAL.GetSaleItemsBySaleId(_sale.SaleID);
                _items = [.. saleItems.Select(i => new ReturnItemViewModel(i))];

                // Subscribe to changes to update total
                foreach (var item in _items)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ReturnItemViewModel.RefundAmount))
                            UpdateTotalRefund();
                    };
                }

                ReturnItemsGrid.ItemsSource = _items;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل العناصر: {ex.Message}");
            }
        }

        private void UpdateTotalRefund()
        {
            decimal total = _items.Sum(i => i.RefundAmount);
            TotalRefundText.Text = total.ToDisplayCurrency();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void QtyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static readonly Regex _regex = MyRegex(); //numbers only
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void ConfirmReturn_Click(object sender, RoutedEventArgs e)
        {
            _activeShift ??= ShiftDAL.GetOpenShift();
            if (_activeShift == null)
            {
                _ = MessageBox.Show("لا يمكن تنفيذ المرتجع بدون وردية مفتوحة.", "الوردية غير مفتوحة",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var itemsToReturn = _items.Where(i => i.QtyToReturn > 0).ToList();

            if (itemsToReturn.Count == 0)
            {
                _ = MessageBox.Show("الرجاء تحديد كمية للإرجاع لأي صنف.");
                return;
            }

            string reason = ReturnReasonTextBox.Text.Trim();
            if (reason.Length < 3)
            {
                _ = MessageBox.Show("سبب الإرجاع مطلوب ويجب ألا يقل عن 3 أحرف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                _ = ReturnReasonTextBox.Focus();
                return;
            }

            if (MessageBox.Show("هل أنت متأكد من إرجاع هذه العناصر؟\nسيتم تحديث المخزون وحالة الفاتورة.", "تأكيد الإرجاع", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    var requestItems = itemsToReturn
                        .Select(x => new ReturnRequestItem
                        {
                            SaleItemID = x.OriginalItem.SaleItemID,
                            Quantity = x.QtyToReturn
                        })
                        .ToList();

                    int userId = Helpers.SessionContext.CurrentUserID;
                    var result = ReturnDAL.ProcessReturn(_sale.SaleID, requestItems, reason, userId);

                    _ = MessageBox.Show(
                        $"تمت عملية الإرجاع بنجاح!\nرقم عملية الإرجاع: #{result.ReturnID}\nالقيمة: {result.TotalRefund.ToDisplayCurrency()}",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show($"خطأ أثناء العملية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [GeneratedRegex("[^0-9]+")]
        private static partial Regex MyRegex();
    }

    public class ReturnItemViewModel(SaleItem item) : INotifyPropertyChanged
    {
        public SaleItem OriginalItem { get; } = item;

        public string ProductName => OriginalItem.ProductName;
        public int Quantity => OriginalItem.Quantity;
        public int ReturnedQuantity => OriginalItem.ReturnedQuantity;
        public string DisplayUnitPrice => OriginalItem.DisplayUnitPrice;

        public decimal EffectivePrice => OriginalItem.UnitPrice * (1 - OriginalItem.DiscountPercent / 100m);

        private int _qtyToReturn;
        public int QtyToReturn
        {
            get => _qtyToReturn;
            set
            {
                int maxReturnable = Quantity - ReturnedQuantity;
                if (value > maxReturnable) value = maxReturnable;
                if (value < 0) value = 0;

                if (_qtyToReturn != value)
                {
                    _qtyToReturn = value;
                    OnPropertyChanged(nameof(QtyToReturn));
                    OnPropertyChanged(nameof(RefundAmount));
                }
            }
        }

        public decimal RefundAmount => QtyToReturn * EffectivePrice;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


