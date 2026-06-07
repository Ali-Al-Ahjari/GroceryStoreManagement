using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using GroceryStoreManagement.Helpers;
using System;
using System.Windows;
using System.Collections.Generic;
using Dapper;
using System.Linq;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة حوار إضافة/تعديل المستخدم
    /// </summary>
    public partial class UserDialog : Window
    {
        private readonly User _user;
        private readonly bool _isEditMode;

        /// <summary>
        /// مُنشئ لإضافة مستخدم جديد
        /// </summary>
        public UserDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            DialogTitle.Text = "إضافة مستخدم جديد";
            LoadRoles();
            
            // تفعيل التنقل بمفتاح Enter
            Helpers.EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        /// <summary>
        /// مُنشئ لتعديل مستخدم موجود
        /// </summary>
        public UserDialog(User user)
        {
            InitializeComponent();
            _user = user;
            _isEditMode = true;
            DialogTitle.Text = "تعديل بيانات المستخدم";
            LoadRoles();
            LoadUserData();
        }

        /// <summary>
        /// تحميل بيانات المستخدم للتعديل
        /// </summary>
        private void LoadUserData()
        {
            if (_user == null) return;

            UsernameTextBox.Text = _user.Username;
            UsernameTextBox.IsEnabled = false; // لا يمكن تغيير اسم المستخدم
            FullNameTextBox.Text = _user.FullName;
            PhoneTextBox.Text = _user.Phone;
            EmailTextBox.Text = _user.Email;
            IsActiveCheckBox.IsChecked = _user.IsActive;

            // الصلاحيات
            // تعيين الدور
            if (_user.RoleID > 0)
            {
                RoleComboBox.SelectedValue = _user.RoleID;
            }

            // عرض معلومات التدقيق
            ShowAuditInfo();
        }

        private void ShowAuditInfo()
        {
            if (_isEditMode && _user != null)
            {
                string info = AuditHelper.GetAuditInfo(
                    _user.CreatedDate,
                    AuditHelper.GetUserName(_user.CreatedBy),
                    _user.ModifiedDate,
                    AuditHelper.GetUserName(_user.ModifiedBy));

                if (!string.IsNullOrEmpty(info))
                {
                    AuditInfoText.Text = info;
                    AuditInfoBorder.Visibility = Visibility.Visible;
                }
            }
        }

        private void LoadRoles()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                string query = "SELECT * FROM Roles ORDER BY RoleID";
                var roles = conn.Query<Role>(query).AsList();
                RoleComboBox.ItemsSource = roles;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"فشل تحميل الأدوار: {ex.Message}");
            }
        }

        /// <summary>
        /// حفظ المستخدم
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من الحقول المطلوبة
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                {
                    _ = MessageBox.Show("اسم المستخدم مطلوب!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _ = UsernameTextBox.Focus();
                    return;
                }

                // التحقق من كلمة المرور في وضع الإضافة
                if (!_isEditMode)
                {
                    if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        _ = MessageBox.Show("كلمة المرور مطلوبة!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _ = PasswordBox.Focus();
                        return;
                    }

                    if (PasswordBox.Password != ConfirmPasswordBox.Password)
                    {
                        _ = MessageBox.Show("كلمة المرور غير متطابقة!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _ = ConfirmPasswordBox.Focus();
                        return;
                    }
                }

                if (RoleComboBox.SelectedValue == null)
                {
                    _ = MessageBox.Show("يرجى اختيار دور للمستخدم!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // إنشاء أو تحديث المستخدم
                if (_isEditMode)
                {
                    _user.FullName = FullNameTextBox.Text;
                    _user.Phone = PhoneTextBox.Text;
                    _user.Email = EmailTextBox.Text;
                    _user.IsActive = IsActiveCheckBox.IsChecked ?? true;
                    _user.RoleID = (int)RoleComboBox.SelectedValue;

                    // تحديث كلمة المرور إذا تم إدخالها
                    if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        if (PasswordBox.Password != ConfirmPasswordBox.Password)
                        {
                            _ = MessageBox.Show("كلمة المرور غير متطابقة!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        _user.Password = PasswordBox.Password;
                    }

                    // لا حاجة لتحديث الصلاحيات الفردية (UpdatePermissions) لأننا نعتمد على الدور
                    // لكن يمكننا تعيين قيم افتراضية للتوافق مع الأعمدة القديمة
                    SetLegacyPermissionsFromRole(_user);

                    UserDAL.UpdateUser(_user);
                }
                else
                {
                    var newUser = new User
                    {
                        Username = UsernameTextBox.Text,
                        Password = PasswordBox.Password,
                        FullName = FullNameTextBox.Text,
                        Phone = PhoneTextBox.Text,
                        Email = EmailTextBox.Text,
                        IsActive = IsActiveCheckBox.IsChecked ?? true,
                        RoleID = (int)RoleComboBox.SelectedValue
                    };

                    SetLegacyPermissionsFromRole(newUser);

                    UserDAL.AddUser(newUser);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حفظ المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحديث صلاحيات المستخدم
        /// </summary>
        /// <summary>
        /// تعيين قيم افتراضية للحقول القديمة للتوافق
        /// </summary>
        private static void SetLegacyPermissionsFromRole(User user)
        {
            if (user == null) return;

            // افتراض آمن: لا صلاحيات افتراضياً
            user.CanAccessDashboard = false;
            user.CanManageProducts = false;
            user.CanViewCustomers = false;
            user.CanAddCustomers = false;
            user.CanEditCustomers = false;
            user.CanDeleteCustomers = false;
            user.CanManageInvoices = false;
            user.CanViewReports = false;
            user.CanManageSettings = false;
            user.CanBackup = false;

            if (user.RoleID <= 0) return;

            // توافق مع الأعمدة القديمة: اشتقاق القيم من صلاحيات الدور الفعلية
            var rolePermissions = PermissionDAL.GetRolePermissions(user.RoleID);
            if (rolePermissions == null || rolePermissions.Count == 0) return;

            user.CanAccessDashboard = rolePermissions.Contains(PermissionKeys.AccessDashboard);

            user.CanViewCustomers = rolePermissions.Contains(PermissionKeys.ViewCustomers);
            user.CanAddCustomers = rolePermissions.Contains(PermissionKeys.AddCustomers);
            user.CanEditCustomers = rolePermissions.Contains(PermissionKeys.EditCustomers);
            user.CanDeleteCustomers = rolePermissions.Contains(PermissionKeys.DeleteCustomers);

            user.CanManageProducts =
                rolePermissions.Contains(PermissionKeys.ViewProducts) ||
                rolePermissions.Contains(PermissionKeys.AddProducts) ||
                rolePermissions.Contains(PermissionKeys.EditProducts) ||
                rolePermissions.Contains(PermissionKeys.DeleteProducts) ||
                rolePermissions.Contains(PermissionKeys.ManageStock) ||
                rolePermissions.Contains(PermissionKeys.EditPrices);

            user.CanManageInvoices =
                rolePermissions.Contains(PermissionKeys.ViewSales) ||
                rolePermissions.Contains(PermissionKeys.CreateSales) ||
                rolePermissions.Contains(PermissionKeys.EditSales) ||
                rolePermissions.Contains(PermissionKeys.DeleteSales) ||
                rolePermissions.Contains(PermissionKeys.ApplyDiscount) ||
                rolePermissions.Contains(PermissionKeys.ProcessReturns) ||
                rolePermissions.Contains(PermissionKeys.VoidInvoices) ||
                rolePermissions.Contains(PermissionKeys.PrintInvoices);

            user.CanViewReports =
                rolePermissions.Contains(PermissionKeys.ViewReports) ||
                rolePermissions.Contains(PermissionKeys.ExportReports) ||
                rolePermissions.Contains(PermissionKeys.ViewFinancialReports) ||
                rolePermissions.Contains(PermissionKeys.ViewInventoryReports);

            user.CanManageSettings =
                rolePermissions.Contains(PermissionKeys.AccessSettings) ||
                rolePermissions.Contains(PermissionKeys.ManageUsers) ||
                rolePermissions.Contains(PermissionKeys.ManagePermissions) ||
                rolePermissions.Contains(PermissionKeys.ViewActivityLog) ||
                rolePermissions.Contains(PermissionKeys.ManageSystemSettings);

            user.CanBackup =
                rolePermissions.Contains(PermissionKeys.BackupDatabase) ||
                rolePermissions.Contains(PermissionKeys.RestoreDatabase);
        }

        /// <summary>
        /// إلغاء
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
