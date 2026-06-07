// =====================================================
// PermissionHelper.cs - مساعد التحقق من الصلاحيات
// يوفر دوال للتحقق من صلاحيات المستخدم الحالي
// =====================================================

using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// كلاس ثابت للتحقق من صلاحيات المستخدم الحالي
    /// يوفر طرق سهلة للتحقق من الصلاحيات وإظهار رسائل المنع
    /// </summary>
    public static class PermissionHelper
    {
        // ═══════════════════════════════════════════════════════════
        // كاش الصلاحيات للمستخدم الحالي
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// قائمة صلاحيات المستخدم الحالي (يتم تحميلها عند تسجيل الدخول)
        /// </summary>
        private static readonly HashSet<string> _currentUserPermissions = [];

        /// <summary>
        /// هل المستخدم الحالي مدير (لديه جميع الصلاحيات)
        /// </summary>
        private static bool _isAdmin = false;

        // ═══════════════════════════════════════════════════════════
        // تحميل الصلاحيات
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// تحميل صلاحيات المستخدم بعد تسجيل الدخول
        /// يُستدعى من LoginWindow بعد نجاح الدخول
        /// </summary>
        /// <param name="user">كائن المستخدم الذي سجل دخوله</param>
        public static void LoadUserPermissions(User user)
        {
            _currentUserPermissions.Clear();
            _isAdmin = false;

            if (user == null)
            {
                Logger.LogWarning("محاولة تحميل صلاحيات لمستخدم فارغ");
                return;
            }

            Logger.LogInfo($"تحميل صلاحيات المستخدم: {user.Username}");

            // التحقق من صلاحيات المستخدم بناءً على دوره (Role)
            try
            {
                // إذا كان المستخدم Admin (RoleID = 1) نعطيه صلاحيات المسؤول
                if (user.RoleID == 1)
                {
                    _isAdmin = true;
                    Logger.LogInfo("تم منح صلاحيات المدير الكاملة (Admin Role)");
                }

                // تحميل الصلاحيات التفصيلية من قاعدة البيانات
                var granularPermissions = GroceryStoreManagement.DAL.PermissionDAL.GetUserPermissions(user.UserID);
                foreach (var perm in granularPermissions)
                {
                    _ = _currentUserPermissions.Add(perm);
                }

                // دعم التوافق مع النظام القديم فقط عند عدم وجود دور
                // ملاحظة أمنية: إذا كان للمستخدم دور لكن بدون صلاحيات، يجب أن يبقى بدون صلاحيات
                if (user.RoleID <= 0)
                {
                    ApplyLegacyPermissions(user);
                }
                else if (!_isAdmin && _currentUserPermissions.Count == 0)
                {
                    Logger.LogInfo($"المستخدم {user.Username} لديه دور ({user.RoleID}) بدون أي صلاحيات ممنوحة");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في تحميل صلاحيات المستخدم");
            }



            Logger.LogInfo($"تم تحميل {_currentUserPermissions.Count} صلاحية للمستخدم");
        }

        /// <summary>
        /// مسح صلاحيات المستخدم عند تسجيل الخروج
        /// </summary>
        public static void ClearPermissions()
        {
            _currentUserPermissions.Clear();
            _isAdmin = false;
            Logger.LogInfo("تم مسح صلاحيات المستخدم");
        }

        // ═══════════════════════════════════════════════════════════
        // التحقق من الصلاحيات
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// التحقق من أن المستخدم لديه صلاحية معينة
        /// </summary>
        /// <param name="permissionKey">مفتاح الصلاحية</param>
        /// <returns>true إذا كان لديه الصلاحية</returns>
        public static bool HasPermission(string permissionKey)
        {
            // المدير لديه جميع الصلاحيات
            if (_isAdmin)
                return true;

            return _currentUserPermissions.Contains(permissionKey);
        }

        /// <summary>
        /// التحقق من أن المستخدم لديه أي من الصلاحيات المحددة
        /// </summary>
        /// <param name="permissionKeys">قائمة مفاتيح الصلاحيات</param>
        /// <returns>true إذا كان لديه أي منها</returns>
        public static bool HasAnyPermission(params string[] permissionKeys)
        {
            if (_isAdmin)
                return true;

            return permissionKeys.Any(p => _currentUserPermissions.Contains(p));
        }

        /// <summary>
        /// التحقق من أن المستخدم لديه جميع الصلاحيات المحددة
        /// </summary>
        /// <param name="permissionKeys">قائمة مفاتيح الصلاحيات</param>
        /// <returns>true إذا كان لديه جميعها</returns>
        public static bool HasAllPermissions(params string[] permissionKeys)
        {
            if (_isAdmin)
                return true;

            return permissionKeys.All(p => _currentUserPermissions.Contains(p));
        }

        /// <summary>
        /// التحقق من صلاحية مع عرض رسالة منع إذا لم تكن متوفرة
        /// </summary>
        /// <param name="permissionKey">مفتاح الصلاحية</param>
        /// <param name="actionName">اسم العملية (للعرض في الرسالة)</param>
        /// <returns>true إذا كان لديه الصلاحية</returns>
        public static bool CheckPermission(string permissionKey, string actionName = "")
        {
            if (HasPermission(permissionKey))
                return true;

            // تسجيل محاولة الوصول المرفوضة
            Logger.LogWarning($"محاولة وصول مرفوضة - الصلاحية: {permissionKey}, المستخدم: {SessionContext.CurrentUsername}");

            // عرض رسالة للمستخدم
            string message = string.IsNullOrEmpty(actionName)
                ? "ليس لديك صلاحية للقيام بهذه العملية"
                : $"ليس لديك صلاحية لـ {actionName}";

            _ = MessageBox.Show(
                message,
                "صلاحية مرفوضة",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }

        /// <summary>
        /// التحقق من صلاحية بدون عرض رسالة (للتحكم في الواجهة)
        /// </summary>
        public static bool CanPerform(string permissionKey)
        {
            return HasPermission(permissionKey);
        }

        // ═══════════════════════════════════════════════════════════
        // خصائص سريعة للصلاحيات الشائعة
        // ═══════════════════════════════════════════════════════════

        /// <summary>هل يمكن الوصول للوحة التحكم؟</summary>
        public static bool CanAccessDashboard => HasPermission(PermissionKeys.AccessDashboard);

        /// <summary>هل يمكن إدارة المنتجات؟</summary>
        public static bool CanManageProducts => HasPermission(PermissionKeys.EditProducts);

        /// <summary>هل يمكن تعديل الأسعار؟</summary>
        public static bool CanEditPrices => HasPermission(PermissionKeys.EditPrices);

        /// <summary>هل يمكن تطبيق الخصومات؟</summary>
        public static bool CanApplyDiscount => HasPermission(PermissionKeys.ApplyDiscount);

        /// <summary>هل يمكن حذف الفواتير؟</summary>
        public static bool CanDeleteInvoices => HasPermission(PermissionKeys.DeleteSales);

        /// <summary>هل يمكن عرض التقارير؟</summary>
        public static bool CanViewReports => HasPermission(PermissionKeys.ViewReports);

        /// <summary>هل يمكن إدارة المستخدمين؟</summary>
        public static bool CanManageUsers => HasPermission(PermissionKeys.ManageUsers);

        /// <summary>هل يمكن عمل نسخ احتياطي؟</summary>
        public static bool CanBackup => HasPermission(PermissionKeys.BackupDatabase);

        /// <summary>هل المستخدم مدير؟</summary>
        public static bool IsAdmin => _isAdmin;

        // ═══════════════════════════════════════════════════════════
        // الحصول على قائمة الصلاحيات
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// الحصول على قائمة كاملة بالصلاحيات المتاحة في النظام
        /// </summary>
        public static List<Permission> GetAllSystemPermissions()
        {
            return
            [
                // لوحة التحكم
                new Permission { PermissionKey = PermissionKeys.AccessDashboard, DisplayName = "الوصول للوحة التحكم", Category = "لوحة التحكم", Description = "عرض لوحة التحكم الرئيسية" },
                new Permission { PermissionKey = PermissionKeys.ViewStatistics, DisplayName = "عرض الإحصائيات", Category = "لوحة التحكم", Description = "عرض إحصائيات المبيعات والمخزون" },
                
                // المنتجات
                new Permission { PermissionKey = PermissionKeys.ViewProducts, DisplayName = "عرض المنتجات", Category = "المنتجات", Description = "عرض قائمة المنتجات" },
                new Permission { PermissionKey = PermissionKeys.AddProducts, DisplayName = "إضافة المنتجات", Category = "المنتجات", Description = "إضافة منتجات جديدة" },
                new Permission { PermissionKey = PermissionKeys.EditProducts, DisplayName = "تعديل المنتجات", Category = "المنتجات", Description = "تعديل بيانات المنتجات" },
                new Permission { PermissionKey = PermissionKeys.DeleteProducts, DisplayName = "حذف المنتجات", Category = "المنتجات", Description = "حذف المنتجات من النظام" },
                new Permission { PermissionKey = PermissionKeys.EditPrices, DisplayName = "تعديل الأسعار", Category = "المنتجات", Description = "تغيير أسعار البيع والشراء" },
                new Permission { PermissionKey = PermissionKeys.ManageStock, DisplayName = "إدارة المخزون", Category = "المنتجات", Description = "تعديل كميات المخزون" },
                
                // المبيعات
                new Permission { PermissionKey = PermissionKeys.ViewSales, DisplayName = "عرض المبيعات", Category = "المبيعات", Description = "عرض قائمة الفواتير" },
                new Permission { PermissionKey = PermissionKeys.CreateSales, DisplayName = "إنشاء فاتورة", Category = "المبيعات", Description = "إنشاء فواتير مبيعات جديدة" },
                new Permission { PermissionKey = PermissionKeys.EditSales, DisplayName = "تعديل الفواتير", Category = "المبيعات", Description = "تعديل الفواتير الموجودة" },
                new Permission { PermissionKey = PermissionKeys.DeleteSales, DisplayName = "حذف الفواتير", Category = "المبيعات", Description = "حذف الفواتير" },
                new Permission { PermissionKey = PermissionKeys.ApplyDiscount, DisplayName = "تطبيق الخصم", Category = "المبيعات", Description = "تطبيق خصومات على الفواتير" },
                new Permission { PermissionKey = PermissionKeys.ProcessReturns, DisplayName = "معالجة المرتجعات", Category = "المبيعات", Description = "قبول وإدارة المرتجعات" },
                new Permission { PermissionKey = PermissionKeys.VoidInvoices, DisplayName = "إلغاء الفواتير", Category = "المبيعات", Description = "إلغاء فاتورة كاملة" },
                new Permission { PermissionKey = PermissionKeys.PrintInvoices, DisplayName = "طباعة الفواتير", Category = "المبيعات", Description = "طباعة الفواتير" },
                
                // العملاء
                new Permission { PermissionKey = PermissionKeys.ViewCustomers, DisplayName = "عرض العملاء", Category = "العملاء", Description = "عرض قائمة العملاء" },
                new Permission { PermissionKey = PermissionKeys.AddCustomers, DisplayName = "إضافة العملاء", Category = "العملاء", Description = "إضافة عملاء جدد" },
                new Permission { PermissionKey = PermissionKeys.EditCustomers, DisplayName = "تعديل العملاء", Category = "العملاء", Description = "تعديل بيانات العملاء" },
                new Permission { PermissionKey = PermissionKeys.DeleteCustomers, DisplayName = "حذف العملاء", Category = "العملاء", Description = "حذف العملاء" },
                new Permission { PermissionKey = PermissionKeys.ManageCustomerDebt, DisplayName = "إدارة الديون", Category = "العملاء", Description = "إدارة ديون العملاء وسدادها" },
                
                // الموردين والمشتريات
                new Permission { PermissionKey = PermissionKeys.ViewSuppliers, DisplayName = "عرض الموردين", Category = "الموردين", Description = "عرض قائمة الموردين" },
                new Permission { PermissionKey = PermissionKeys.ManageSuppliers, DisplayName = "إدارة الموردين", Category = "الموردين", Description = "إضافة وتعديل الموردين" },
                new Permission { PermissionKey = PermissionKeys.ViewPurchases, DisplayName = "عرض المشتريات", Category = "المشتريات", Description = "عرض فواتير المشتريات" },
                new Permission { PermissionKey = PermissionKeys.CreatePurchases, DisplayName = "إنشاء فاتورة شراء", Category = "المشتريات", Description = "إنشاء فواتير مشتريات" },
                new Permission { PermissionKey = PermissionKeys.ManagePurchases, DisplayName = "إدارة المشتريات", Category = "المشتريات", Description = "تعديل وحذف المشتريات" },
                
                // التقارير
                new Permission { PermissionKey = PermissionKeys.ViewReports, DisplayName = "عرض التقارير", Category = "التقارير", Description = "الوصول لقسم التقارير" },
                new Permission { PermissionKey = PermissionKeys.ExportReports, DisplayName = "تصدير التقارير", Category = "التقارير", Description = "تصدير التقارير لـ Excel/PDF" },
                new Permission { PermissionKey = PermissionKeys.ViewFinancialReports, DisplayName = "التقارير المالية", Category = "التقارير", Description = "عرض التقارير المالية التفصيلية" },
                new Permission { PermissionKey = PermissionKeys.ViewInventoryReports, DisplayName = "تقارير المخزون", Category = "التقارير", Description = "عرض تقارير المخزون" },
                
                // النظام والإعدادات
                new Permission { PermissionKey = PermissionKeys.AccessSettings, DisplayName = "الوصول للإعدادات", Category = "النظام", Description = "الوصول لصفحة الإعدادات" },
                new Permission { PermissionKey = PermissionKeys.ManageUsers, DisplayName = "إدارة المستخدمين", Category = "النظام", Description = "إضافة وتعديل وحذف المستخدمين" },
                new Permission { PermissionKey = PermissionKeys.ManagePermissions, DisplayName = "إدارة الصلاحيات", Category = "النظام", Description = "تعديل صلاحيات المستخدمين" },
                new Permission { PermissionKey = PermissionKeys.BackupDatabase, DisplayName = "النسخ الاحتياطي", Category = "النظام", Description = "إنشاء نسخ احتياطية" },
                new Permission { PermissionKey = PermissionKeys.RestoreDatabase, DisplayName = "استعادة النسخة", Category = "النظام", Description = "استعادة قاعدة البيانات" },
                new Permission { PermissionKey = PermissionKeys.ViewActivityLog, DisplayName = "سجل النشاطات", Category = "النظام", Description = "عرض سجل العمليات" },
                new Permission { PermissionKey = PermissionKeys.ManageSystemSettings, DisplayName = "إعدادات النظام", Category = "النظام", Description = "تعديل إعدادات النظام العامة" },
                
                // العروض والخصومات
                new Permission { PermissionKey = PermissionKeys.ManagePromotions, DisplayName = "إدارة العروض", Category = "المبيعات", Description = "إنشاء وإدارة العروض والخصومات" }
            ];
        }

        /// <summary>
        /// الحصول على صلاحيات المستخدم الحالي
        /// </summary>
        public static List<string> GetCurrentUserPermissions()
        {
            return [.. _currentUserPermissions];
        }

        private static void ApplyLegacyPermissions(User user)
        {
            if (user == null) return;

            if (user.CanAccessDashboard)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.AccessDashboard);
                _ = _currentUserPermissions.Add(PermissionKeys.ViewStatistics);
            }

            if (user.CanManageProducts)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.ViewProducts);
                _ = _currentUserPermissions.Add(PermissionKeys.AddProducts);
                _ = _currentUserPermissions.Add(PermissionKeys.EditProducts);
                _ = _currentUserPermissions.Add(PermissionKeys.DeleteProducts);
                _ = _currentUserPermissions.Add(PermissionKeys.ManageStock);
                _ = _currentUserPermissions.Add(PermissionKeys.EditPrices);
            }

            if (user.CanManageInvoices)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.ViewSales);
                _ = _currentUserPermissions.Add(PermissionKeys.CreateSales);
                _ = _currentUserPermissions.Add(PermissionKeys.EditSales);
                _ = _currentUserPermissions.Add(PermissionKeys.DeleteSales);
                _ = _currentUserPermissions.Add(PermissionKeys.PrintInvoices);
            }

            if (user.CanViewCustomers)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.ViewCustomers);
            }

            if (user.CanAddCustomers)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.AddCustomers);
            }

            if (user.CanEditCustomers)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.EditCustomers);
            }

            if (user.CanDeleteCustomers)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.DeleteCustomers);
            }

            if (user.CanViewReports)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.ViewReports);
                _ = _currentUserPermissions.Add(PermissionKeys.ViewFinancialReports);
                _ = _currentUserPermissions.Add(PermissionKeys.ViewInventoryReports);
                _ = _currentUserPermissions.Add(PermissionKeys.ExportReports);
            }

            if (user.CanManageSettings)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.AccessSettings);
                _ = _currentUserPermissions.Add(PermissionKeys.ManageSystemSettings);
                _ = _currentUserPermissions.Add(PermissionKeys.ManageUsers);
                _ = _currentUserPermissions.Add(PermissionKeys.ManagePermissions);
                _ = _currentUserPermissions.Add(PermissionKeys.ViewActivityLog);
            }

            if (user.CanBackup)
            {
                _ = _currentUserPermissions.Add(PermissionKeys.BackupDatabase);
                _ = _currentUserPermissions.Add(PermissionKeys.RestoreDatabase);
            }

            Logger.LogInfo($"تم تحميل صلاحيات التوافق للمستخدم: {user.Username}");
        }
    }
}
