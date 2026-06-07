using System; // دوال الوقت والتاريخ
using System.ComponentModel; // واجهة تحديث الواجهة الرسومية

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج فاتورة المشتريات (شراء بضاعة من الموردين)
    /// </summary>
    public class Purchase : INotifyPropertyChanged
    {
        // حقول خاصة للبيانات
        private decimal _totalAmount;
        private decimal _paidAmount;
        private decimal _discount;
        private DateTime _purchaseDate;
        private string _paymentStatus;

        /// <summary>
        /// المعرف الفريد لفاتورة الشراء
        /// </summary>
        public int PurchaseID { get; set; }

        /// <summary>
        /// معرف المورد المرتبط بالفاتورة
        /// </summary>
        public int? SupplierID { get; set; }

        /// <summary>
        /// اسم المورد (للعرض)
        /// </summary>
        public string SupplierName { get; set; }

        /// <summary>
        /// ملاحظات إضافية
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// رقم الفاتورة الورقية من المورد (مرجع خارجي)
        /// </summary>
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// تاريخ عملية الشراء
        /// </summary>
        public DateTime PurchaseDate
        {
            get => _purchaseDate;
            set
            {
                if (_purchaseDate != value)
                {
                    _purchaseDate = value;
                    OnPropertyChanged(nameof(PurchaseDate));
                    OnPropertyChanged(nameof(DisplayDate));
                    OnPropertyChanged(nameof(DisplayDateTime));
                }
            }
        }

        /// <summary>
        /// المبلغ الإجمالي للفاتورة
        /// </summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (_totalAmount != value)
                {
                    _totalAmount = value;
                    OnPropertyChanged(nameof(TotalAmount));
                    OnPropertyChanged(nameof(DisplayTotal));
                    OnPropertyChanged(nameof(RemainingAmount));
                }
            }
        }

        /// <summary>
        /// المبلغ المدفوع للمورد
        /// </summary>
        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (_paidAmount != value)
                {
                    _paidAmount = value;
                    OnPropertyChanged(nameof(PaidAmount));
                    OnPropertyChanged(nameof(RemainingAmount));
                    OnPropertyChanged(nameof(DisplayPaidAmount));
                }
            }
        }

        /// <summary>
        /// قيمة الخصم المكتسب من المورد
        /// </summary>
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (_discount != value)
                {
                    _discount = value;
                    OnPropertyChanged(nameof(Discount));
                    OnPropertyChanged(nameof(NetTotal));
                }
            }
        }

        /// <summary>
        /// حالة السداد: Paid (خالصة)، Partial (جزئي)، Unpaid (آجل)
        /// </summary>
        public string PaymentStatus
        {
            get => _paymentStatus;
            set
            {
                if (_paymentStatus != value)
                {
                    _paymentStatus = value;
                    OnPropertyChanged(nameof(PaymentStatus));
                    OnPropertyChanged(nameof(PaymentStatusText));
                    OnPropertyChanged(nameof(PaymentStatusColor));
                }
            }
        }

        /// <summary>
        /// عدد الأصناف في الفاتورة
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// هل تم استلام البضاعة وإدخالها للمخزون؟
        /// </summary>
        public bool IsImported { get; set; }

        // --- حسابات --- (Calculations)

        /// <summary>
        /// الصافي بعد الخصم
        /// </summary>
        public decimal NetTotal => TotalAmount - Discount;

        /// <summary>
        /// المبلغ المتبقي للمورد
        /// </summary>
        public decimal RemainingAmount => NetTotal - PaidAmount;

        // --- خصائص للعرض (Formatted Strings) ---

        public string DisplayDate => PurchaseDate.ToString("dd/MM/yyyy");
        public string DisplayDateTime => PurchaseDate.ToString("dd/MM/yyyy HH:mm");
        public string DisplayTotal => TotalAmount.ToDisplayCurrency(); // عملة
        public string DisplayNetTotal => NetTotal.ToDisplayCurrency();
        public string DisplayPaidAmount => PaidAmount.ToDisplayCurrency();
        public string DisplayRemainingAmount => RemainingAmount.ToDisplayCurrency();

        /// <summary>
        /// نص حالة السداد بالعربية
        /// </summary>
        public string PaymentStatusText
        {
            get
            {
                return PaymentStatus switch
                {
                    "Paid" => "مدفوعة",
                    "Partial" => "جزئي",
                    "Unpaid" => "غير مدفوعة",
                    _ => "غير محدد",
                };
            }
        }

        /// <summary>
        /// نص حالة السداد بالعربية (تستخدم في البايندينج)
        /// </summary>
        public string PaymentStatusArabic => PaymentStatusText;

        /// <summary>
        /// لون حالة السداد
        /// </summary>
        public string PaymentStatusColor
        {
            get
            {
                return PaymentStatus switch
                {
                    "Paid" => "#4CAF50",// أخضر
                    "Partial" => "#FF9800",// برتقالي
                    "Unpaid" => "#F44336",// أحمر
                    _ => "#9E9E9E",
                };
            }
        }

        /// <summary>
        /// نص حالة التوريد/الاستلام
        /// </summary>
        public string ImportStatusText => IsImported ? "تم الاستيراد" : "لم يتم";
        public string ImportStatusColor => IsImported ? "#4CAF50" : "#9E9E9E";

        // تنفيذ واجهة INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

