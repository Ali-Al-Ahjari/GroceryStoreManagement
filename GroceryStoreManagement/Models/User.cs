using System; // استيراد المكتبة الأساسية
using System.ComponentModel; // استيراد مكتبة التعامل مع أحداث تغيير الخصائص (مهمة لـ MVVM و WPF)
using System.Collections.Generic; // استيراد مجموعات البيانات

namespace GroceryStoreManagement.Models // تحديد اسم المجال للكلاسات الخاصة بالبيانات (Models)
{
    // تعريف كلاس المستخدم (User) والذي يمثل بيانات الموظف أو المدير في النظام
    // يرث من واجهة INotifyPropertyChanged ليتمكن من تنبيه الواجهة عند تغير أي قيمة داخله
    public class User : INotifyPropertyChanged, IAuditable
    {
        // حقول التدقيق
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        // متغيرات خاصة (Private Fields) لتخزين القيم الفعلية للخصائص
        private string _username; // لتخزين اسم المستخدم
        private string _password; // لتخزين كلمة المرور
        private string _fullName; // لتخزين الاسم الكامل
        private string _phone; // لتخزين رقم الهاتف
        private string _email; // لتخزين البريد الإلكتروني
        private bool _isActive; // لتحديد هل الحساب نشط أم موقوف

        // متغيرات خاصة للصلاحيات (Permissions) - تحدد ماذا يمكن للمستخدم أن يفعل
        private bool _canAccessDashboard; // صلاحية الوصول للوحة التحكم الرئيسية
        private bool _canViewCustomers; // صلاحية رؤية قائمة العملاء
        private bool _canAddCustomers; // صلاحية إضافة عميل جديد
        private bool _canEditCustomers; // صلاحية تعديل بيانات عميل
        private bool _canDeleteCustomers; // صلاحية حذف عميل
        private bool _canManageProducts; // صلاحية إدارة المنتجات (إضافة/تعديل/حذف)
        private bool _canManageInvoices; // صلاحية إدارة الفواتير والمبيعات
        private bool _canViewReports; // صلاحية الاطلاع على التقارير المالية
        private bool _canManageSettings; // صلاحية تغيير إعدادات النظام
        private bool _canBackup; // صلاحية عمل نسخة احتياطية لقاعدة البيانات

        // خاصية معرف المستخدم (ID) - يتم توليدها تلقائياً من قاعدة البيانات
        public int UserID { get; set; }

        // معرف الدور (Role) المرتبط به المستخدم
        private int _roleId;
        public int RoleID
        {
            get => _roleId;
            set { _roleId = value; OnPropertyChanged(nameof(RoleID)); }
        }

        // اسم الدور للعرض (ليس عموداً في جدول Users)
        private string _roleName;
        public string RoleName
        {
            get => _roleName;
            set { _roleName = value; OnPropertyChanged(nameof(RoleName)); }
        }

        // خاصية اسم المستخدم (التي يكتبها عند تسجيل الدخول)
        public string Username
        {
            get => _username; // قراءة القيمة
            set
            {
                _username = value; // تحديث القيمة
                OnPropertyChanged(nameof(Username)); // تنبيه الواجهة بأن القيمة تغيرت
            }
        }

        // خاصية كلمة المرور
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        // خاصية الاسم الكامل (الذي يظهر في التقارير والواجهة)
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }

        // خاصية رقم الهاتف للتواصل
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(nameof(Phone)); }
        }

        // خاصية البريد الإلكتروني
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        // خاصية حالة الحساب (مفعل/موقوف)
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        // خصائص الصلاحيات العامة (Permissions Properties)

        // هل يمكنه رؤية لوحة التحكم والإحصائيات؟
        public bool CanAccessDashboard
        {
            get => _canAccessDashboard;
            set { _canAccessDashboard = value; OnPropertyChanged(nameof(CanAccessDashboard)); }
        }

        // هل يمكنه فقط تصفح العملاء؟
        public bool CanViewCustomers
        {
            get => _canViewCustomers;
            set { _canViewCustomers = value; OnPropertyChanged(nameof(CanViewCustomers)); }
        }

        // هل يملك صلاحية الإضافة للعملاء؟
        public bool CanAddCustomers
        {
            get => _canAddCustomers;
            set { _canAddCustomers = value; OnPropertyChanged(nameof(CanAddCustomers)); }
        }

        // هل يملك صلاحية التعديل على العملاء؟
        public bool CanEditCustomers
        {
            get => _canEditCustomers;
            set { _canEditCustomers = value; OnPropertyChanged(nameof(CanEditCustomers)); }
        }

        // هل يملك صلاحية الحذف الخطرة للعملاء؟
        public bool CanDeleteCustomers
        {
            get => _canDeleteCustomers;
            set { _canDeleteCustomers = value; OnPropertyChanged(nameof(CanDeleteCustomers)); }
        }

        // هل يملك صلاحية التحكم الكامل بالمنتجات؟
        public bool CanManageProducts
        {
            get => _canManageProducts;
            set { _canManageProducts = value; OnPropertyChanged(nameof(CanManageProducts)); }
        }

        // هل يمكنه البيع وإصدار الفواتير؟
        public bool CanManageInvoices
        {
            get => _canManageInvoices;
            set { _canManageInvoices = value; OnPropertyChanged(nameof(CanManageInvoices)); }
        }

        // هل يمكنه رؤية الأرباح والتقارير؟
        public bool CanViewReports
        {
            get => _canViewReports;
            set { _canViewReports = value; OnPropertyChanged(nameof(CanViewReports)); }
        }

        // هل يمكنه تغيير إعدادات البرنامج والمستخدمين الآخرين؟
        public bool CanManageSettings
        {
            get => _canManageSettings;
            set { _canManageSettings = value; OnPropertyChanged(nameof(CanManageSettings)); }
        }

        // هل يمكنه حفظ نسخة من البيانات؟
        public bool CanBackup
        {
            get => _canBackup;
            set { _canBackup = value; OnPropertyChanged(nameof(CanBackup)); }
        }

        // ═══════════════════════════════════════════════════════════
        // نظام الصلاحيات المتقدم (Phase 7.1)
        // ═══════════════════════════════════════════════════════════

        private List<string> _permissions = [];

        /// <summary>
        /// قائمة مفاتيح الصلاحيات الممنوحة لهذا المستخدم
        /// </summary>
        public List<string> Permissions
        {
            get => _permissions;
            set { _permissions = value; OnPropertyChanged(nameof(Permissions)); }
        }

        /// <summary>
        /// للتحقق مما إذا كان المستخدم يملك صلاحية معينة
        /// </summary>
        public bool HasPermission(string permissionKey)
        {
            // المدير العام يملك كافة الصلاحيات دائماً
            if (Username?.ToLower(System.Globalization.CultureInfo.CurrentCulture) == "admin") return true;

            return Permissions != null && Permissions.Contains(permissionKey);
        }

        // تعريف الحدث الذي سيتم إطلاقه عند تغيير أي خاصية
        public event PropertyChangedEventHandler PropertyChanged;

        // دالة مساعدة محمية (Protected) لإطلاق الحدث بأمان
        protected virtual void OnPropertyChanged(string propertyName)
        {
            // استدعاء الحدث إذا كان هناك من يستمع إليه (مثل الواجهة)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
