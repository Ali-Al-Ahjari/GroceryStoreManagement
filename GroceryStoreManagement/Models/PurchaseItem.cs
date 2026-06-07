using System.ComponentModel; // واجهة التحديث التلقائي للواجهة

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج عنصر في فاتورة المشتريات (سطر واحد يمثل منتجاً تم شراؤه)
    /// </summary>
    public class PurchaseItem : INotifyPropertyChanged
    {
        // حقول البيانات الأساسية
        private int _quantity;
        private decimal _unitPrice;
        private decimal _totalPrice;

        /// <summary>
        /// المعرف الفريد لعنصر الشراء
        /// </summary>
        public int PurchaseItemID { get; set; }

        /// <summary>
        /// معرف فاتورة الشراء المرتبطة
        /// </summary>
        public int PurchaseID { get; set; }

        /// <summary>
        /// معرف المنتج الذي تم شراؤه
        /// </summary>
        public int ProductID { get; set; }

        /// <summary>
        ///  اسم المنتج (للعرض)
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// كود المنتج (اختياري)
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// الكمية المشتراة
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
                    CalculateTotalPrice(); // إعادة حساب الإجمالي عند تغيير الكمية
                }
            }
        }

        /// <summary>
        /// سعر تكلفة الوحدة
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
                    CalculateTotalPrice(); // إعادة حساب الإجمالي عند تغيير السعر
                }
            }
        }

        /// <summary>
        /// السعر الإجمالي لهذا البند (الكمية × السعر)
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
        /// دالة لحساب المجموع: الكمية × سعر الوحدة
        /// </summary>
        private void CalculateTotalPrice()
        {
            TotalPrice = Quantity * UnitPrice;
        }

        // --- خصائص للعرض (Formatted output) ---

        public string DisplayUnitPrice => UnitPrice.ToDisplayCurrency();
        public string DisplayTotalPrice => TotalPrice.ToDisplayCurrency();

        // تنفيذ واجهة INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

