using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GroceryStoreManagement.Windows
{
    public partial class DebtPaymentDialog : Window
    {
        private readonly Customer _customer;

        public DebtPaymentDialog(Customer customer)
        {
            InitializeComponent();
            _customer = customer;
            LoadCustomerData();
            
            // تفعيل التنقل بمفتاح Enter
            Helpers.EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void LoadCustomerData()
        {
            if (_customer != null)
            {
                TxtCustomerName.Text = _customer.Name;
                TxtTotalDebt.Text = _customer.CurrentDebt.ToDisplayCurrency();
                TxtPaymentAmount.Text = _customer.CurrentDebt.ToString("F2");
                UpdateEstimatedRemaining();
            }
        }

        private void BtnPayFull_Click(object sender, RoutedEventArgs e)
        {
            if (_customer != null)
            {
                TxtPaymentAmount.Text = _customer.CurrentDebt.ToString("F2");
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(TxtPaymentAmount.Text, out decimal amount) || amount <= 0)
                {
                    _ = MessageBox.Show("يرجى إدخال مبلغ صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (amount > _customer.CurrentDebt)
                {
                    _ = MessageBox.Show("المبلغ المدخل أكبر من الدين الحالي", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string paymentMethod = (CmbPaymentMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
                int actorUserId = Helpers.SessionContext.CurrentUserID;
                var result = CustomerDAL.ApplyDebtPayment(_customer.CustomerID, amount, paymentMethod, actorUserId);

                if (result.AppliedAmount > 0)
                {
                    string warning = result.UnappliedAmount > 0
                        ? $"\nمبلغ غير مطبّق: {result.UnappliedAmount.ToDisplayCurrency()}"
                        : string.Empty;

                    _ = MessageBox.Show(
                        $"تم تسجيل السداد بنجاح.\n" +
                        $"المبلغ المسدّد: {result.AppliedAmount.ToDisplayCurrency()}\n" +
                        $"الفواتير المتأثرة: {result.AffectedInvoicesCount}\n" +
                        $"إجمالي المتبقي بعد السداد: {result.TotalRemainingAfterPayment.ToDisplayCurrency()}{warning}",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    _ = MessageBox.Show("فشل تسجيل السداد", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtPaymentAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateEstimatedRemaining();
        }

        private void UpdateEstimatedRemaining()
        {
            if (_customer == null)
            {
                TxtEstimatedRemaining.Text = "المتبقي بعد السداد: ر.ي. 0";
                return;
            }

            if (!decimal.TryParse(TxtPaymentAmount.Text, out decimal amount))
            {
                amount = 0;
            }

            decimal estimatedRemaining = Math.Max(0, _customer.CurrentDebt - amount);
            TxtEstimatedRemaining.Text = $"المتبقي بعد السداد: {estimatedRemaining.ToDisplayCurrency()}";
        }
    }
}

