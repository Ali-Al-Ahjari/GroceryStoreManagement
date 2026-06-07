using Dapper;
using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace GroceryStoreManagement.Windows
{
    public partial class ShiftManagementWindow : Window
    {
        private Shift _openShift;

        public ShiftManagementWindow()
        {
            InitializeComponent();
            EnterKeyHelper.EnableEnterKeyNavigation(this);
            LoadState();
        }

        private void LoadState()
        {
            _openShift = ShiftDAL.GetOpenShift();

            if (_openShift == null)
            {
                ShiftStateText.Text = "لا توجد وردية مفتوحة";
                ShiftStateText.Foreground = (System.Windows.Media.Brush)FindResource("WarningBrush");
                ShiftInfoText.Text = "يمكنك بدء وردية جديدة عبر إدخال العهدة الافتتاحية.";
                StartShiftPanel.Visibility = Visibility.Visible;
                CloseShiftPanel.Visibility = Visibility.Collapsed;
                return;
            }

            ShiftStateText.Text = $"وردية مفتوحة #{_openShift.ShiftID}";
            ShiftStateText.Foreground = (System.Windows.Media.Brush)FindResource("SuccessBrush");
            ShiftInfoText.Text = $"مفتوحة منذ: {_openShift.DisplayOpenedAt} | العهدة: {_openShift.OpeningCash.ToDisplayCurrency()}";

            StartShiftPanel.Visibility = Visibility.Collapsed;
            CloseShiftPanel.Visibility = Visibility.Visible;

            // نقدي متوقع بشكل لحظي (قبل الإغلاق): عهدة + مدفوعات كاش - مرتجعات كاش.
            decimal liveExpectedCash = _openShift.OpeningCash + EstimateLiveCashSales(_openShift.ShiftID) - EstimateLiveCashRefunds(_openShift.ShiftID);
            ExpectedCashText.Text = liveExpectedCash.ToDisplayCurrency();
            ClosingCashTextBox.Text = liveExpectedCash.ToDisplayNumber();
        }

        private static decimal EstimateLiveCashSales(int shiftId)
        {
            using var connection = DatabaseHelper.GetConnection();
            return connection.ExecuteScalar<decimal>(@"
                    SELECT COALESCE(SUM(
                        CASE
                            WHEN PaymentMethod IN ('Cash', 'Partial') THEN PaidAmount
                            ELSE 0
                        END
                    ), 0)
                    FROM Sales
                    WHERE ShiftID = @ShiftID;",
                new { ShiftID = shiftId });
        }

        private static decimal EstimateLiveCashRefunds(int shiftId)
        {
            using var connection = DatabaseHelper.GetConnection();
            return connection.ExecuteScalar<decimal>(@"
                    SELECT COALESCE(SUM(TotalRefund), 0)
                    FROM Returns
                    WHERE ShiftID = @ShiftID;",
                new { ShiftID = shiftId });
        }

        private async void StartShift_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryParseAmount(OpeningCashTextBox.Text, out decimal openingCash))
                {
                    _ = MessageBox.Show("أدخل قيمة صحيحة للعهدة الافتتاحية.");
                    return;
                }

                int shiftId = await ShiftDAL.StartShiftAsync(openingCash, StartNotesTextBox.Text, SessionContext.CurrentUserID);
                _ = MessageBox.Show($"تم فتح الوردية بنجاح. رقم الوردية: #{shiftId}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"تعذر فتح الوردية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CloseShift_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryParseAmount(ClosingCashTextBox.Text, out decimal closingCash))
                {
                    _ = MessageBox.Show("أدخل قيمة صحيحة للنقدية الفعلية.");
                    return;
                }

                Shift closedShift = await ShiftDAL.CloseCurrentShiftAsync(closingCash, CloseNotesTextBox.Text, SessionContext.CurrentUserID);
                PrintHelper.PrintShiftZReport(closedShift);

                _ = MessageBox.Show(
                    $"تم إغلاق الوردية #{closedShift.ShiftID}.\n" +
                    $"المتوقع: {closedShift.ExpectedCash.ToDisplayCurrency()}\n" +
                    $"الفعلي: {closedShift.ClosingCash.GetValueOrDefault().ToDisplayCurrency()}\n" +
                    $"الفرق: {closedShift.CashDifference.ToDisplayCurrency()}",
                    "تم الإغلاق",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"تعذر إغلاق الوردية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool TryParseAmount(string text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                   || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
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
