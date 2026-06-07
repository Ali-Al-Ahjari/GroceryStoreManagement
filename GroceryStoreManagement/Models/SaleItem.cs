using System.ComponentModel; // استيراد واجهة INotifyPropertyChanged لدعم تحديث الواجهة عند تغيير البيانات

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج عنصر الفاتورة (سطر واحد في الفاتورة)
    /// يربط بين الفاتورة والمنتج والكمية والسعر
    /// </summary>
    public class SaleItem : INotifyPropertyChanged
    {
        // حقول خاصة (Backing Fields)
        private int _quantity; // الكمية المباعة
        private decimal _unitPrice; // سعر الوحدة
        private decimal _totalPrice; // الإجمالي لهذا السطر
        private decimal _discountPercent; // نسبة الخصم على هذا السطر (إن وجدت)

        /// <summary>
        /// الرقم المعرف لعنصر البيع (تلقائي)
        /// </summary>
        public int SaleItemID { get; set; }

        /// <summary>
        /// رقم الفاتورة التي ينتمي إليها هذا العنصر
        /// </summary>
        public int SaleID { get; set; }

        /// <summary>
        /// رقم المنتج المباع
        /// </summary>
        public int ProductID { get; set; }

        /// <summary>
        /// اسم المنتج (للعرض فقط)
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// الكمية المباعة من المنتج
        /// عند تغيير الكمية، يتم إعادة حساب السعر الإجمالي تلقائياً
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
                    CalculateTotalPrice(); // تحديث الإجمالي
                }
            }
        }

        /// <summary>
        /// الكمية المرتجعة من هذا الصنف
        /// </summary>
        public int ReturnedQuantity { get; set; }

        /// <summary>
        /// سعر الوحدة الواحدة وقت البيع
        /// </summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged(nameof(UnitPrice));
                    CalculateTotalPrice(); // تحديث الإجمالي
                }
            }
        }

        /// <summary>
        /// نسبة الخصم المطبقة على هذا العنصر (مثلاً 10%)
        /// </summary>
        public decimal DiscountPercent
        {
            get => _discountPercent;
            set
            {
                if (_discountPercent != value)
                {
                    _discountPercent = value;
                    OnPropertyChanged(nameof(DiscountPercent));
                    OnPropertyChanged(nameof(DisplayDiscount));
                    CalculateTotalPrice(); // تحديث الإجمالي
                }
            }
        }

        /// <summary>
        /// السعر الإجمالي لهذا السطر (الكمية * السعر - الخصم)
        /// يتم حسابه تلقائياً، ولكن يمكن تعيينه يدوياً إذا لزم الأمر
        /// </summary>
        public decimal TotalPrice
        {
            get => _totalPrice;
            set
            {
                if (_totalPrice != value)
                {
                    _totalPrice = value;
                    OnPropertyChanged(nameof(TotalPrice));
                    OnPropertyChanged(nameof(DisplayTotalPrice));
                }
            }
        }

        /// <summary>
        /// دالة داخلية لحساب السعر الإجمالي بناءً على الكمية والسعر ونسبة الخصم
        /// </summary>
        private void CalculateTotalPrice()
        {
            // السعر الأساسي = الكمية * سعر الوحدة
            decimal basePrice = Quantity * UnitPrice;

            // قيمة الخصم = السعر الأساسي * (النسبة / 100)
            decimal discountAmount = basePrice * (DiscountPercent / 100);

            // الصافي = السعر الأساسي - قيمة الخصم
            TotalPrice = basePrice - discountAmount;
        }

        // --- خصائص للعرض في الواجهة (Read-Only) ---

        public string DisplayUnitPrice => UnitPrice.ToDisplayCurrency(); // عرض السعر بتنسيق العملة
        public string DisplayTotalPrice => TotalPrice.ToDisplayCurrency(); // عرض الإجمالي بتنسيق العملة
        public string DisplayDiscount => DiscountPercent > 0 ? $"{DiscountPercent}%" : "-"; // عرض الخصم أو شرطة إذا لم يوجد

        // تنفيذ واجهة INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
