using System; // استيراد الوظائف الأساسية مثل DateTime
using System.ComponentModel; // استيراد واجهة INotifyPropertyChanged لدعم تحديث الواجهة

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج يمثل بيانات العميل
    /// ينفذ واجهة INotifyPropertyChanged ليتم تحديث الواجهة تلقائياً عند تغيير البيانات
    /// </summary>
    public class Customer : INotifyPropertyChanged, IAuditable
    {
        // حقول التدقيق
        public int? CreatedBy { get; set; }

        private DateTime? _createdDate;
        public DateTime? CreatedDate
        {
            get => _createdDate;
            set
            {
                if (_createdDate != value)
                {
                    _createdDate = value;
                    OnPropertyChanged(nameof(CreatedDate));
                }
            }
        }

        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // حقول خاصة لتخزين البيانات (Backing Fields)
        private string _name; // اسم العميل
        private string _phone; // رقم الهاتف
        private string _email; // البريد الإلكتروني
        private string _address; // العنوان
        private string _notes; // ملاحظات إضافية
        private bool _isActive; // هل العميل نشط؟

        // الخصائص العامة (Properties) التي يتم ربطها بالواجهة

        /// <summary>
        /// الرقم المعرف الفريد للعميل (المفتاح الأساسي)
        /// </summary>
        public int CustomerID { get; set; }

        /// <summary>
        /// اسم العميل
        /// عند التغيير، يتم إشعار الواجهة لتحديث نفسها
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value) // التأكد من أن القيمة قد تغيرت فعلاً
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name)); // إشعار بتغيير الاسم
                    OnPropertyChanged(nameof(DisplayInfo)); // إشعار بتغيير نص العرض المدمج
                }
            }
        }

        /// <summary>
        /// رقم هاتف العميل
        /// </summary>
        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    _phone = value;
                    OnPropertyChanged(nameof(Phone));
                    OnPropertyChanged(nameof(DisplayInfo));
                }
            }
        }

        /// <summary>
        /// البريد الإلكتروني للعميل
        /// </summary>
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged(nameof(Email));
                }
            }
        }

        /// <summary>
        /// عنوان العميل
        /// </summary>
        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged(nameof(Address));
                }
            }
        }

        /// <summary>
        /// ملاحظات إضافية عن العميل
        /// </summary>
        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged(nameof(Notes));
                }
            }
        }

        /// <summary>
        /// حالة نشاط العميل (true = نشط، false = معطل)
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                    OnPropertyChanged(nameof(StatusText)); // إشعار بتغيير نص الحالة
                }
            }
        }

        /// <summary>
        /// نص عربي يعبر عن حالة العميل
        /// </summary>
        public string StatusText => IsActive ? "نشط" : "غير نشط";



        /// <summary>
        /// خاصية للعرض فقط، تدمج الاسم والرقم (تستخدم في القوائم المنسدلة مثلاً)
        /// </summary>
        public string DisplayInfo => $"{Name} - {Phone}";

        /// <summary>
        /// الحد الائتماني المسموح به للعميل
        /// </summary>
        public decimal CreditLimit { get; set; }

        // خصائص إحصائية غير مخزنة في جدول العملاء مباشرة (يتم حسابها أو جلبها)
        public decimal TotalPurchases { get; set; } // إجمالي المشتريات
        public decimal CurrentDebt { get; set; } // الديون الحالية (المبالغ المتبقية)
        public decimal AvailableCredit => CreditLimit - CurrentDebt; // الرصيد المتاح
        public decimal CreditUsagePercent => CreditLimit > 0 ? Math.Min(999, (CurrentDebt / CreditLimit) * 100) : 0; // نسبة استهلاك الحد الائتماني
        public string DisplayCreditUsage => CreditLimit > 0 ? $"{CreditUsagePercent:0.#}%" : "-";
        public int PurchaseCount { get; set; } // عدد مرات الشراء

        /// <summary>
        /// مجموع نقاط الولاء الحالية للعميل
        /// </summary>
        public int TotalPoints { get; set; }

        /// <summary>
        /// القيمة المالية للنقاط الحالية
        /// </summary>
        public decimal PointsValue { get; set; }

        // حدث يتم استدعاؤه عند تغيير أي خاصية
        public event PropertyChangedEventHandler PropertyChanged;

        // دالة مساعدة لرفع حدث التغيير بأمان
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
