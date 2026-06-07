using System.ComponentModel; // استيراد واجهة INotifyPropertyChanged لدعم تحديث الواجهة

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج يمثل بيانات المورد
    /// ينفذ واجهة INotifyPropertyChanged ليتم تحديث الواجهة عند تعديل البيانات
    /// </summary>
    public class Supplier : INotifyPropertyChanged, IAuditable
    {
        // حقول التدقيق
        public int? CreatedBy { get; set; }
        public System.DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public System.DateTime? ModifiedDate { get; set; }
        // حقول خاصة لتخزين البيانات
        private string _name; // اسم المورد
        private string _phone; // رقم الهاتف
        private string _email; // البريد الإلكتروني
        private string _address; // العنوان

        /// <summary>
        /// الرقم المعرف الفريد للمورد
        /// </summary>
        public int SupplierID { get; set; }

        /// <summary>
        /// اسم المورد أو الشركة الموردة
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value) // إذا تغيرت القيمة
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name)); // تحديث خاصية الاسم
                }
            }
        }

        /// <summary>
        /// رقم الهاتف للتواصل
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
                }
            }
        }

        /// <summary>
        /// عنوان البريد الإلكتروني
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
        /// العنوان الفعلي للمورد
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

        // خصائص إضافية للإحصائيات (لا تخزن بالضرورة في جدول الموردين مباشرة)
        public int ProductCount { get; set; } // عدد المنتجات التي يوردها
        public decimal TotalSuppliedValue { get; set; } // القيمة الإجمالية للمنتجات الموردة

        /// <summary>
        /// نص مختصر يجمع الاسم ورقم الهاتف للعرض في القوائم
        /// </summary>
        public string DisplayInfo => $"{Name} - {Phone}";

        // حدث تغيير الخاصية
        public event PropertyChangedEventHandler PropertyChanged;

        // دالة تنفيذ الحدث
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}