using System; // استيراد الوظائف الأساسية مثل DateTime
using System.ComponentModel; // استيراد واجهة INotifyPropertyChanged لدعم تحديث الواجهة

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج الفاتورة - يمثل عملية بيع واحدة متكاملة
    /// ينفذ واجهة INotifyPropertyChanged ليتم تحديث الواجهة تلقائياً عند تغيير الحسابات
    /// </summary>
    public class Sale : INotifyPropertyChanged, IAuditable
    {
        // حقول التدقيق
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        // حقول خاصة لتخزين القيم (Backing Fields)
        private decimal _totalAmount; // المبلغ الإجمالي قبل الخصم والضريبة
        private decimal _paidAmount; // المبلغ المدفوع من العميل
        private decimal _discount; // قيمة الخصم على الفاتورة
        private decimal _tax; // نسبة الضريبة المضافة (%)
        private DateTime _saleDate; // تاريخ ووقت الفاتورة
        private string _paymentStatus; // حالة الدفع (مدفوع، آجل، جزئي)
        private string _paymentMethod; // طريقة الدفع (كاش، شبكة، تحويل)
        private DateTime? _dueDate; // تاريخ الاستحقاق

        /// <summary>
        /// الرقم المعرف للفاتورة (تلقائي من قاعدة البيانات)
        /// </summary>
        public int SaleID { get; set; }

        /// <summary>
        /// معرف الفاتورة (اسم مختصر للربط في بعض الواجهات)
        /// </summary>
        public int ID => SaleID;

        /// <summary>
        /// رقم العميل (اختياري - قد يكون عميل نقدي بدون تسجيل)
        /// </summary>
        public int? CustomerID { get; set; }

        /// <summary>
        /// رقم الوردية المرتبطة بالفاتورة.
        /// </summary>
        public int? ShiftID { get; set; }

        /// <summary>
        /// اسم العميل (يتم جلبه عند الاستعلام للعرض)
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// ملاحظات إضافية على الفاتورة
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// تاريخ ووقت عملية البيع
        /// </summary>
        public DateTime SaleDate
        {
            get => _saleDate;
            set
            {
                if (_saleDate != value)
                {
                    _saleDate = value;
                    OnPropertyChanged(nameof(SaleDate));
                    // تحديث خصائص العرض المعتمدة على التاريخ
                    OnPropertyChanged(nameof(DisplayDate));
                    OnPropertyChanged(nameof(DisplayDateTime));
                }
            }
        }

        /// <summary>
        /// إجمالي المبلغ للمنتجات
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
                    // عند تغيير الإجمالي، يجب تحديث العرض والمبلغ المتبقي
                    OnPropertyChanged(nameof(DisplayTotal));
                    OnPropertyChanged(nameof(DisplayNetTotal));
                    OnPropertyChanged(nameof(RemainingAmount));
                    OnPropertyChanged(nameof(DisplayRemainingAmount));
                    OnPropertyChanged(nameof(NetTotal));
                }
            }
        }

        /// <summary>
        /// المبلغ الذي دفعه العميل فعلياً
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
                    // تحديث المبلغ المتبقي عند تغيير المدفوع
                    OnPropertyChanged(nameof(RemainingAmount));
                    OnPropertyChanged(nameof(DisplayPaidAmount));
                    OnPropertyChanged(nameof(DisplayRemainingAmount));
                }
            }
        }

        /// <summary>
        /// قيمة الخصم المطبق على الفاتورة
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
                    // تحديث الصافي النهائي عند تغيير الخصم
                    OnPropertyChanged(nameof(NetTotal));
                    OnPropertyChanged(nameof(DisplayTotal));
                    OnPropertyChanged(nameof(DisplayNetTotal));
                    OnPropertyChanged(nameof(RemainingAmount));
                    OnPropertyChanged(nameof(DisplayRemainingAmount));
                }
            }
        }

        /// <summary>
        /// قيمة الضريبة
        /// </summary>
        public decimal Tax
        {
            get => _tax;
            set
            {
                if (_tax != value)
                {
                    _tax = value;
                    OnPropertyChanged(nameof(Tax));
                    // تحديث الصافي النهائي عند تغيير الضريبة
                    OnPropertyChanged(nameof(NetTotal));
                    OnPropertyChanged(nameof(DisplayTotal));
                    OnPropertyChanged(nameof(DisplayNetTotal));
                    OnPropertyChanged(nameof(RemainingAmount));
                    OnPropertyChanged(nameof(DisplayRemainingAmount));
                }
            }
        }

        /// <summary>
        /// حالة الفاتورة: Paid (مدفوعة)، Partial (جزئي)، Unpaid (غير مدفوعة)
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
                    // تحديث النص واللون المعروضين في الواجهة
                    OnPropertyChanged(nameof(PaymentStatusText));
                    OnPropertyChanged(nameof(PaymentStatusColor));
                }
            }
        }

        /// <summary>
        /// طريقة الدفع: Cash (نقد)، Card (بطاقة)، Transfer (تحويل)، إلخ
        /// </summary>
        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (_paymentMethod != value)
                {
                    _paymentMethod = value;
                    OnPropertyChanged(nameof(PaymentMethod));
                    OnPropertyChanged(nameof(PaymentMethodText));
                }
            }
        }

        /// <summary>
        /// عدد العناصر (المنتجات المختلفة) في الفاتورة
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// تاريخ استحقاق الفاتورة (للفواتير الآجلة)
        /// </summary>
        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                if (_dueDate != value)
                {
                    _dueDate = value;
                    OnPropertyChanged(nameof(DueDate));
                }
            }
        }

        // --- خصائص محسوبة ديناميكياً ---

        /// <summary>
        /// الصافي النهائي = الإجمالي - الخصم + (الإجمالي × نسبة الضريبة)
        /// </summary>
        public decimal NetTotal => TotalAmount - Discount + (TotalAmount * (Tax / 100m));

        /// <summary>
        /// المبلغ الإجمالي للمرتجعات من هذه الفاتورة
        /// </summary>
        public decimal ReturnedAmount { get; set; }

        /// <summary>
        /// المبلغ المتبقي على العميل = الصافي النهائي - المدفوع - المرتجع
        /// </summary>
        public decimal RemainingAmount => NetTotal - PaidAmount - ReturnedAmount;

        // --- خصائص للعرض في واجهة المستخدم (WPF Binding) ---

        public string DisplayDate => SaleDate.ToString("dd/MM/yyyy"); // تاريخ فقط
        public string DisplayDateTime => SaleDate.ToString("dd/MM/yyyy HH:mm"); // تاريخ ووقت
        public string DisplayTotal => NetTotal.ToDisplayCurrency(); // تنسيق عملة للإجمالي النهائي
        public string DisplayNetTotal => NetTotal.ToDisplayCurrency(); // تنسيق عملة للصافي
        public string DisplayPaidAmount => PaidAmount.ToDisplayCurrency(); // تنسيق عملة للمدفوع
        public string DisplayRemainingAmount => RemainingAmount.ToDisplayCurrency(); // تنسيق عملة للمتبقي

        /// <summary>
        /// نص عربي لحالة الدفع
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
        /// نص عربي لحالة الدفع (تستخدم في البايندينج)
        /// </summary>
        public string PaymentStatusArabic => PaymentStatusText;

        /// <summary>
        /// لون يعبر عن حالة الدفع (أخضر، برتقالي، أحمر)
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
                    _ => "#9E9E9E",// رمادي
                };
            }
        }

        /// <summary>
        /// نص عربي لطريقة الدفع
        /// </summary>
        public string PaymentMethodText
        {
            get
            {
                return PaymentMethod switch
                {
                    "Cash" => "كاش",
                    "Card" => "شبكة",
                    "Transfer" => "تحويل",
                    "Partial" => "جزئي",
                    "Credit" => "آجل",
                    _ => "غير محدد",
                };
            }
        }

        // حدث تغيير الخصائص
        public event PropertyChangedEventHandler PropertyChanged;

        // دالة رفع الحدث
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

