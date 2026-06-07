using System; // الدوال الأساسية
using System.ComponentModel; // واجهة تحديث البيانات في الواجهة

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج المنتج - يمثل السلع المخزنة في النظام
    /// ينفذ واجهة INotifyPropertyChanged لتحديث الواجهة عند تغيير أي معلومة
    /// </summary>
    public class Product : INotifyPropertyChanged, IAuditable
    {
        // حقول التدقيق
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        // حقول خاصة للبيانات (Backing Fields)
        private int _quantity; // الكمية المتوفرة
        private string _code; // الكود (الباركود)
        private string _name; // اسم المنتج
        private string _unit; // وحدة القياس (قطعة، كيلو، إلخ)
        private decimal _purchasePrice; // سعر الشراء (التكلفة)
        private decimal _sellingPrice; // سعر البيع للجمهور
        private int _minQuantity; // حد الطلب (الحد الأدنى)
        private string _imagePath; // مسار صورة المنتج
        private string _category; // تصنيف المنتج

        /// <summary>
        /// الرقم المعرف الفريد للمنتج
        /// </summary>
        public int ProductID { get; set; }

        /// <summary>
        /// كود المنتج أو الباركود
        /// </summary>
        public string Code
        {
            get => _code;
            set
            {
                if (_code != value)
                {
                    _code = value;
                    OnPropertyChanged(nameof(Code));
                }
            }
        }

        /// <summary>
        /// اسم المنتج (مثل: حليب المراعي 1 لتر)
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// الوحدة (حبة، كرتون، كجم)
        /// </summary>
        public string Unit
        {
            get => _unit;
            set
            {
                if (_unit != value)
                {
                    _unit = value;
                    OnPropertyChanged(nameof(Unit));
                }
            }
        }

        /// <summary>
        /// سعر التكلفة (الشراء من المورد)
        /// </summary>
        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set
            {
                if (_purchasePrice != value)
                {
                    _purchasePrice = value;
                    OnPropertyChanged(nameof(PurchasePrice));
                    // تحديث هامش الربح والربح المتوقع عند تغيير التكلفة
                    OnPropertyChanged(nameof(Profit));
                    OnPropertyChanged(nameof(ProfitMargin));
                }
            }
        }

        /// <summary>
        /// سعر البيع للعميل
        /// </summary>
        public decimal SellingPrice
        {
            get => _sellingPrice;
            set
            {
                if (_sellingPrice != value)
                {
                    _sellingPrice = value;
                    OnPropertyChanged(nameof(SellingPrice));
                    OnPropertyChanged(nameof(Price)); // للتوافق
                    OnPropertyChanged(nameof(Profit));
                    OnPropertyChanged(nameof(ProfitMargin));
                    OnPropertyChanged(nameof(TotalValue)); // القيمة الإجمالية للمخزون تتغير
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// خاصية للتوافق مع الأكواد القديمة التي قد تستخدم كلمة Price بدلاً من SellingPrice
        /// </summary>
        public decimal Price
        {
            get => SellingPrice;
            set => SellingPrice = value;
        }

        /// <summary>
        /// الحد الأدنى للكمية (نقطة إعادة الطلب)
        /// إذا قلت الكمية عن هذا الحد، يعتبر المنتج منخفض المخزون
        /// </summary>
        public int MinQuantity
        {
            get => _minQuantity;
            set
            {
                if (_minQuantity != value)
                {
                    _minQuantity = value;
                    OnPropertyChanged(nameof(MinQuantity));
                    OnPropertyChanged(nameof(IsLowStock)); // إعادة تقييم حالة المخزون
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        /// <summary>
        /// الكمية الحالية المتوفرة في المخزون
        /// </summary>
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    // تحديث جميع الخصائص المرتبطة بالحالة والقيمة
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(IsLowStock));
                    OnPropertyChanged(nameof(TotalValue));
                }
            }
        }

        /// <summary>
        /// كمية المخزون (اسم بديل لـ Quantity)
        /// يستخدم للتوافق مع بعض أجزاء الكود التي تتوقع هذا الاسم
        /// </summary>
        public int StockQuantity { get => Quantity; set => Quantity = value; }

        /// <summary>
        /// مسار الصورة (اختياري)
        /// </summary>
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath != value)
                {
                    _imagePath = value;
                    OnPropertyChanged(nameof(ImagePath));
                }
            }
        }

        /// <summary>
        /// الفئة أو التصنيف (مثل: مشروبات، ألبان)
        /// </summary>
        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

        // بيانات المورد المرتبطة
        public int? SupplierID { get; set; }
        public string SupplierName { get; set; }

        // --- خصائص محسوبة (Calculated Properties) ---

        /// <summary>
        /// الربح المتوقع في القطعة الواحدة
        /// </summary>
        public decimal Profit => SellingPrice - PurchasePrice;

        /// <summary>
        /// نسبة هامش الربح المئوية
        /// </summary>
        public decimal ProfitMargin => PurchasePrice > 0 ? (Profit / PurchasePrice) * 100 : 0;

        /// <summary>
        /// هل المنتج وصل لمرحلة الخطر (كمية قليلة)؟
        /// </summary>
        public bool IsLowStock => Quantity <= MinQuantity || Quantity <= 5;

        /// <summary>
        /// القيمة الإجمالية للمخزون المتوفر من هذا المنتج (بسعر البيع)
        /// </summary>
        public decimal TotalValue => SellingPrice * Quantity;

        // --- خصائص تاريخ الصلاحية ---

        private DateTime? _expiryDate;

        /// <summary>
        /// تاريخ انتهاء الصلاحية (اختياري)
        /// </summary>
        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set
            {
                if (_expiryDate != value)
                {
                    _expiryDate = value;
                    OnPropertyChanged(nameof(ExpiryDate));
                    OnPropertyChanged(nameof(DaysUntilExpiry));
                    OnPropertyChanged(nameof(IsExpiringSoon));
                    OnPropertyChanged(nameof(IsExpired));
                    OnPropertyChanged(nameof(ExpiryStatusText));
                    OnPropertyChanged(nameof(ExpiryStatusColor));
                }
            }
        }

        /// <summary>
        /// عدد الأيام المتبقية حتى انتهاء الصلاحية
        /// </summary>
        public int? DaysUntilExpiry
        {
            get
            {
                if (!ExpiryDate.HasValue) return null;
                return (ExpiryDate.Value.Date - DateTime.Today).Days;
            }
        }

        /// <summary>
        /// هل المنتج قريب من انتهاء الصلاحية (أقل من 30 يوم)؟
        /// </summary>
        public bool IsExpiringSoon
        {
            get
            {
                if (!ExpiryDate.HasValue) return false;
                int days = (ExpiryDate.Value.Date - DateTime.Today).Days;
                return days >= 0 && days <= 30;
            }
        }

        /// <summary>
        /// هل المنتج منتهي الصلاحية؟
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (!ExpiryDate.HasValue) return false;
                return ExpiryDate.Value.Date < DateTime.Today;
            }
        }

        /// <summary>
        /// نص حالة الصلاحية للعرض
        /// </summary>
        public string ExpiryStatusText
        {
            get
            {
                if (!ExpiryDate.HasValue) return "-";
                if (IsExpired) return "منتهي";
                if (IsExpiringSoon) return $"قريب ({DaysUntilExpiry} يوم)";
                return "صالح";
            }
        }

        /// <summary>
        /// لون حالة الصلاحية
        /// </summary>
        public string ExpiryStatusColor
        {
            get
            {
                if (!ExpiryDate.HasValue) return "Transparent";
                if (IsExpired) return "#FF5252"; // أحمر
                if (IsExpiringSoon) return "#FFC107"; // أصفر/برتقالي
                return "#4CAF50"; // أخضر
            }
        }

        // --- خصائص للعرض (UI Helpers) ---

        /// <summary>
        /// كود الحالة الداخلي (Low, Medium, Good)
        /// </summary>
        public string Status
        {
            get
            {
                if (Quantity <= MinQuantity || Quantity <= 5) return "Low";
                if (Quantity <= 15) return "Medium";
                return "Good";
            }
        }

        /// <summary>
        /// نص الحالة بالعربية للعرض
        /// </summary>
        public string StatusText
        {
            get
            {
                if (Quantity <= MinQuantity || Quantity <= 5) return "منخفض";
                if (Quantity <= 15) return "متوسط";
                return "جيد";
            }
        }

        /// <summary>
        /// نص الحالة بالعربية (تستخدم في البايندينج)
        /// </summary>
        public string StatusArabic => StatusText;

        /// <summary>
        /// لون الحالة للعرض (أحمر، برتقالي، أخضر)
        /// </summary>
        public string StatusColor
        {
            get
            {
                if (Quantity <= MinQuantity || Quantity <= 5) return "#FF5252"; // أحمر
                if (Quantity <= 15) return "#FF9800"; // برتقالي
                return "#4CAF50"; // أخضر
            }
        }

        /// <summary>
        /// الاسم المعروض في القوائم (الاسم + السعر)
        /// هذا ما تبحث عنه القائمة المنسدلة في نافذة المبيعات
        /// </summary>
        public string DisplayName => $"{Name} - {SellingPrice.ToDisplayCurrency()}";

        // حدث الإشعار بالتغيير
        public event PropertyChangedEventHandler PropertyChanged;

        // دالة رفع الحدث
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

