// =====================================================
// Permission.cs - نموذج الصلاحيات
// يعرف هيكل الصلاحيات المختلفة في النظام
// =====================================================

using System.ComponentModel;

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج الصلاحية الفردية
    /// يمثل صلاحية واحدة يمكن منحها أو سحبها من المستخدم
    /// </summary>
    public class Permission : INotifyPropertyChanged
    {
        // ═══════════════════════════════════════════════════════════
        // الخصائص الأساسية
        // ═══════════════════════════════════════════════════════════

        private int _permissionId;
        private string _permissionKey;
        private string _displayName;
        private string _description;
        private string _category;
        private bool _isGranted;

        /// <summary>
        /// معرف الصلاحية الفريد
        /// </summary>
        public int PermissionID
        {
            get => _permissionId;
            set { _permissionId = value; OnPropertyChanged(nameof(PermissionID)); }
        }

        /// <summary>
        /// مفتاح الصلاحية (للاستخدام البرمجي)
        /// مثال: "CanEditPrices", "CanDeleteInvoices"
        /// </summary>
        public string PermissionKey
        {
            get => _permissionKey;
            set { _permissionKey = value; OnPropertyChanged(nameof(PermissionKey)); }
        }

        /// <summary>
        /// الاسم المعروض للمستخدم (بالعربية)
        /// مثال: "تعديل الأسعار", "حذف الفواتير"
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); }
        }

        /// <summary>
        /// وصف تفصيلي للصلاحية
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        /// <summary>
        /// تصنيف الصلاحية (المبيعات، المخزون، التقارير، إلخ)
        /// </summary>
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        /// <summary>
        /// هل الصلاحية ممنوحة للمستخدم؟
        /// (تُستخدم في الواجهة)
        /// </summary>
        public bool IsGranted
        {
            get => _isGranted;
            set { _isGranted = value; OnPropertyChanged(nameof(IsGranted)); }
        }

        // ═══════════════════════════════════════════════════════════
        // INotifyPropertyChanged
        // ═══════════════════════════════════════════════════════════

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ثوابت مفاتيح الصلاحيات
    /// تُستخدم للوصول للصلاحيات بشكل آمن
    /// </summary>
    public static class PermissionKeys
    {
        // ═══════════════════════════════════════════════════════════
        // صلاحيات لوحة التحكم
        // ═══════════════════════════════════════════════════════════
        public const string AccessDashboard = "AccessDashboard";
        public const string ViewStatistics = "ViewStatistics";

        // ═══════════════════════════════════════════════════════════
        // صلاحيات المنتجات
        // ═══════════════════════════════════════════════════════════
        public const string ViewProducts = "ViewProducts";
        public const string AddProducts = "AddProducts";
        public const string EditProducts = "EditProducts";
        public const string DeleteProducts = "DeleteProducts";
        public const string EditPrices = "EditPrices";
        public const string ManageStock = "ManageStock";

        // ═══════════════════════════════════════════════════════════
        // صلاحيات المبيعات والفواتير
        // ═══════════════════════════════════════════════════════════
        public const string ViewSales = "ViewSales";
        public const string CreateSales = "CreateSales";
        public const string EditSales = "EditSales";
        public const string DeleteSales = "DeleteSales";
        public const string ApplyDiscount = "ApplyDiscount";
        public const string ProcessReturns = "ProcessReturns";
        public const string VoidInvoices = "VoidInvoices";
        public const string PrintInvoices = "PrintInvoices";

        // ═══════════════════════════════════════════════════════════
        // صلاحيات العملاء
        // ═══════════════════════════════════════════════════════════
        public const string ViewCustomers = "ViewCustomers";
        public const string AddCustomers = "AddCustomers";
        public const string EditCustomers = "EditCustomers";
        public const string DeleteCustomers = "DeleteCustomers";
        public const string ManageCustomerDebt = "ManageCustomerDebt";

        // ═══════════════════════════════════════════════════════════
        // صلاحيات الموردين والمشتريات
        // ═══════════════════════════════════════════════════════════
        public const string ViewSuppliers = "ViewSuppliers";
        public const string ManageSuppliers = "ManageSuppliers";
        public const string ViewPurchases = "ViewPurchases";
        public const string CreatePurchases = "CreatePurchases";
        public const string ManagePurchases = "ManagePurchases";

        // ═══════════════════════════════════════════════════════════
        // صلاحيات التقارير
        // ═══════════════════════════════════════════════════════════
        public const string ViewReports = "ViewReports";
        public const string ExportReports = "ExportReports";
        public const string ViewFinancialReports = "ViewFinancialReports";
        public const string ViewInventoryReports = "ViewInventoryReports";

        // ═══════════════════════════════════════════════════════════
        // صلاحيات النظام والإعدادات
        // ═══════════════════════════════════════════════════════════
        public const string AccessSettings = "AccessSettings";
        public const string ManageUsers = "ManageUsers";
        public const string ManagePermissions = "ManagePermissions";
        public const string BackupDatabase = "BackupDatabase";
        public const string RestoreDatabase = "RestoreDatabase";
        public const string ViewActivityLog = "ViewActivityLog";
        public const string ManageSystemSettings = "ManageSystemSettings";
        public const string ManagePromotions = "ManagePromotions";
    }
}
