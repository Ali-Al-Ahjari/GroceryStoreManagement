using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using GroceryStoreManagement.Models;
using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة تسديد الفواتير - تسمح بالدفع الكامل أو الجزئي
    /// </summary>
    public partial class PaymentDialog : Window
    {
        // الفاتورة المراد تسديدها
        private readonly Sale _sale;

        // المبلغ المتبقي قبل هذه الدفعة
        private decimal _remainingAmount;
        private Shift _activeShift;

        /// <summary>
        /// المنشئ - يستقبل الفاتورة المراد تسديدها
        /// </summary>
        /// <param name="sale">كائن الفاتورة</param>
        public PaymentDialog(Sale sale)
        {
            InitializeComponent();
            _sale = sale;
            LoadSaleData();
            RefreshShiftContext();
            
            // تفعيل التنقل بمفتاح Enter
            EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void RefreshShiftContext()
        {
            _activeShift = ShiftDAL.GetOpenShift();
            if (_activeShift != null)
            {
                return;
            }

            SaveButton.IsEnabled = false;
            _ = MessageBox.Show(
                "لا يمكن تسجيل دفعة بدون وردية مفتوحة.\nافتح وردية أولاً من شاشة المبيعات.",
                "الوردية غير مفتوحة",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>
        /// تحميل بيانات الفاتورة وعرضها في الواجهة
        /// </summary>
        private void LoadSaleData()
        {
            try
            {
                // عرض رقم الفاتورة
                InvoiceNumberText.Text = $"#{_sale.SaleID}";

                // عرض اسم العميل
                CustomerNameText.Text = string.IsNullOrEmpty(_sale.CustomerName)
                    ? "عميل نقدي"
                    : _sale.CustomerName;

                // عرض إجمالي الفاتورة (الصافي بعد الخصم والضريبة)
                TotalAmountText.Text = _sale.NetTotal.ToDisplayCurrency();

                // عرض المبلغ المدفوع سابقاً
                PaidAmountText.Text = _sale.PaidAmount.ToDisplayCurrency();

                // حساب المبلغ المتبقي
                _remainingAmount = Math.Max(0, _sale.RemainingAmount);
                RemainingAmountText.Text = _remainingAmount.ToDisplayCurrency();

                // تعيين المبلغ المتبقي كقيمة افتراضية للدفع
                PaymentAmountTextBox.Text = _remainingAmount.ToDisplayNumber();

                // إذا كانت الفاتورة مدفوعة بالكامل، نعطل زر الحفظ
                if (_remainingAmount <= 0)
                {
                    SaveButton.IsEnabled = false;
                    PaymentAmountTextBox.IsEnabled = false;
                    _ = MessageBox.Show("هذه الفاتورة مدفوعة بالكامل!", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PaymentDialog.LoadSaleData");
                _ = MessageBox.Show($"خطأ في تحميل بيانات الفاتورة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حفظ الدفعة وتحديث الفاتورة
        /// </summary>
        private void SavePayment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _activeShift ??= ShiftDAL.GetOpenShift();
                if (_activeShift == null)
                {
                    _ = MessageBox.Show("لا يمكن تسجيل دفعة قبل فتح وردية.", "الوردية غير مفتوحة",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من صحة المبلغ المدخل
                if (!decimal.TryParse(PaymentAmountTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal paymentAmount) &&
                    !decimal.TryParse(PaymentAmountTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out paymentAmount))
                {
                    _ = MessageBox.Show("يرجى إدخال مبلغ صحيح!", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    _ = PaymentAmountTextBox.Focus();
                    return;
                }

                // التحقق من أن المبلغ أكبر من صفر
                if (paymentAmount <= 0)
                {
                    _ = MessageBox.Show("يجب أن يكون المبلغ أكبر من صفر!", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من أن المبلغ لا يتجاوز المتبقي
                if (paymentAmount > _remainingAmount)
                {
                    var result = MessageBox.Show(
                        $"المبلغ المدخل ({paymentAmount.ToDisplayCurrency()}) أكبر من المتبقي ({_remainingAmount.ToDisplayCurrency()}).\n" +
                        "هل تريد تعديله للمبلغ المتبقي؟",
                        "تنبيه",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        paymentAmount = _remainingAmount;
                    }
                    else
                    {
                        return;
                    }
                }

                // الحصول على طريقة الدفع
                string paymentMethod = "Cash";
                if (PaymentMethodComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
                {
                    paymentMethod = selectedItem.Tag?.ToString() ?? "Cash";
                }

                // تحديث المبلغ المدفوع
                decimal newPaidAmount = _sale.PaidAmount + paymentAmount;
                decimal newRemainingAmount = Math.Max(0, (_sale.NetTotal - _sale.ReturnedAmount) - newPaidAmount);

                // تحديد حالة الدفع الجديدة
                string newPaymentStatus;
                if (newRemainingAmount <= 0)
                {
                    newPaymentStatus = "Paid";
                }
                else if (newPaidAmount > 0)
                {
                    newPaymentStatus = "Partial";
                }
                else
                {
                    newPaymentStatus = "Unpaid";
                }

                // تحديث الفاتورة في قاعدة البيانات
                bool success = SaleDAL.UpdatePayment(_sale.SaleID, newPaidAmount, newPaymentStatus, paymentMethod);

                if (success)
                {
                    if (paymentMethod == "Cash" && paymentAmount > 0)
                    {
                        _ = PrintHelper.TryOpenCashDrawer();
                    }

                    // تسجيل العملية في سجل النشاطات
                    string notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? "" : $" - {NotesTextBox.Text}";
                    ActivityLogDAL.AddLog(
                        SessionContext.CurrentUserID,
                        "تسديد فاتورة",
                        $"تم تسديد {paymentAmount.ToDisplayCurrency()} للفاتورة #{_sale.SaleID}{notes}");

                    _ = MessageBox.Show(
                        $"تم تسديد المبلغ بنجاح!\n\n" +
                        $"المبلغ المدفوع: {paymentAmount.ToDisplayCurrency()}\n" +
                        $"المتبقي: {newRemainingAmount.ToDisplayCurrency()}\n" +
                        $"الحالة: {(newPaymentStatus == "Paid" ? "مدفوعة بالكامل" : "جزئي")}",
                        "تم بنجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    _ = MessageBox.Show("حدث خطأ أثناء حفظ الدفعة!", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PaymentDialog.SavePayment_Click");
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// دفع المبلغ المتبقي بالكامل
        /// </summary>
        private void PayFullAmount_Click(object sender, RoutedEventArgs e)
        {
            PaymentAmountTextBox.Text = _remainingAmount.ToDisplayNumber();
        }

        /// <summary>
        /// دفع نصف المبلغ المتبقي
        /// </summary>
        private void PayHalfAmount_Click(object sender, RoutedEventArgs e)
        {
            PaymentAmountTextBox.Text = (_remainingAmount / 2).ToDisplayNumber();
        }

        /// <summary>
        /// التحقق من صحة المبلغ عند الكتابة
        /// </summary>
        private void PaymentAmountTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // يمكن إضافة تحقق إضافي هنا
        }

        /// <summary>
        /// سحب النافذة من شريط العنوان
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        /// <summary>
        /// إغلاق النافذة
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}


