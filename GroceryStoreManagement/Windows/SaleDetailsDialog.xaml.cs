using GroceryStoreManagement.DAL; // طبقة الوصول للبيانات
using GroceryStoreManagement.Helpers; // المساعدات والأدوات
using GroceryStoreManagement.Models; // نماذج البيانات
using System; // الأنواع الأساسية
using System.Windows; // عناصر WPF الأساسية

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة عرض تفاصيل الفاتورة
    /// </summary>
    public partial class SaleDetailsDialog : Window
    {
        // متغير لتخزين معرف الفاتورة المعروضة
        private readonly int _saleId;

        /// <summary>
        /// المُنشئ - يستقبل معرف الفاتورة
        /// </summary>
        /// <param name="saleId">معرف الفاتورة المراد عرضها</param>
        public SaleDetailsDialog(int saleId)
        {
            InitializeComponent(); // تهيئة واجهة المستخدم
            _saleId = saleId; // حفظ معرف الفاتورة
            LoadSaleDetails(); // تحميل التفاصيل
        }

        // متغير لتخزين الفاتورة الحالية
        private Sale _sale;

        /// <summary>
        /// تحميل تفاصيل الفاتورة وعرضها
        /// </summary>
        private void LoadSaleDetails()
        {
            try
            {
                // تحميل بيانات الفاتورة من قاعدة البيانات
                _sale = SaleDAL.GetSaleById(_saleId);
                // التحقق من وجود الفاتورة
                if (_sale == null)
                {
                    _ = MessageBox.Show("الفاتورة غير موجودة", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close(); // إغلاق النافذة إذا لم توجد الفاتورة
                    return;
                }

                // عرض بيانات الفاتورة الأساسية في الواجهة
                InvoiceNumberText.Text = _sale.SaleID.ToString(); // رقم الفاتورة
                InvoiceDateText.Text = _sale.DisplayDateTime; // تاريخ الفاتورة
                // اسم العميل (أو "عميل نقدي" إذا لم يكن هناك عميل مسجل)
                CustomerNameText.Text = !string.IsNullOrEmpty(_sale.CustomerName) ? _sale.CustomerName : "عميل نقدي";

                // عرض اسم الموظف الذي أنشأ الفاتورة
                CreatedByText.Text = AuditHelper.GetUserName(_sale.CreatedBy);

                TotalAmountText.Text = _sale.DisplayTotal; // المبلغ الإجمالي

                // تحميل عناصر الفاتورة (المنتجات)
                var saleItems = SaleItemDAL.GetSaleItemsBySaleId(_saleId);
                SaleItemsGrid.ItemsSource = saleItems; // عرضها في الجدول
                ItemsCountText.Text = saleItems.Count.ToString(); // عدد المنتجات

                // تحميل سجل المرتجعات
                var returns = ReturnDAL.GetReturnsBySaleId(_saleId);
                ReturnsGrid.ItemsSource = returns;
                ReturnsCountText.Text = $"{returns.Count} عملية";

                // تحميل بيانات العميل الإضافية إذا كان موجوداً
                if (_sale.CustomerID.HasValue)
                {
                    var customer = CustomerDAL.GetCustomerById(_sale.CustomerID.Value);
                    if (customer != null)
                    {
                        CustomerPhoneText.Text = customer.Phone; // عرض رقم الهاتف
                    }
                }
            }
            catch (Exception ex)
            {
                // عرض رسالة خطأ
                _ = MessageBox.Show($"خطأ في تحميل تفاصيل الفاتورة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close(); // إغلاق النافذة
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر الطباعة
        /// </summary>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // استدعاء دالة طباعة الإيصال
                PrintHelper.PrintReceipt(_saleId);
            }
            catch (Exception ex)
            {
                // عرض رسالة خطأ في الطباعة
                _ = MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// معالج حدث النقر على زر الإغلاق
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // إغلاق النافذة
        }
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_sale == null)
                    return;

                var saleItems = SaleItemDAL.GetSaleItemsBySaleId(_saleId);

                var sb = new System.Text.StringBuilder();
                _ = sb.AppendLine("تفاصيل الفاتورة");
                _ = sb.AppendLine($"رقم الفاتورة: {_sale.SaleID}");
                _ = sb.AppendLine($"التاريخ: {_sale.DisplayDateTime}");
                _ = sb.AppendLine($"العميل: {(!string.IsNullOrEmpty(_sale.CustomerName) ? _sale.CustomerName : "عميل نقدي")}");
                _ = sb.AppendLine($"الإجمالي: {_sale.DisplayTotal}");
                _ = sb.AppendLine();
                _ = sb.AppendLine("العناصر:");
                foreach (var item in saleItems)
                {
                    _ = sb.AppendLine($"- {item.ProductName} | الكمية: {item.Quantity} | السعر: {item.UnitPrice.ToDisplayNumber()} | الإجمالي: {item.TotalPrice.ToDisplayNumber()}");
                }

                string filePath = ExportHelper.ShowSaveFileDialog(
                    $"invoice_{_sale.SaleID}_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    "Text (*.txt)|*.txt");

                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                if (ExportHelper.ExportToText(sb.ToString(), filePath))
                {
                    _ = MessageBox.Show("تم تصدير الفاتورة بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sale == null) return;
            var returnDialog = new ReturnDialog(_sale);
            if (returnDialog.ShowDialog() == true)
            {
                // Reload data to reflect changes (if any fields like ReturnedAmount are displayed)
                LoadSaleDetails();
            }
        }
    }
}

