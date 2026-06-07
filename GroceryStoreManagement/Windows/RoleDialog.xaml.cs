using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة إدارة الأدوار والصلاحيات
    /// </summary>
    public partial class RoleDialog : Window
    {
        private readonly Role _role;
        private readonly bool _isEditMode;
        private List<Permission> _allPermissions;

        public RoleDialog(Role role = null)
        {
            InitializeComponent();

            _role = role;
            _isEditMode = _role != null;

            LoadPermissions();
            
            // تفعيل التنقل بمفتاح Enter
            Helpers.EnterKeyHelper.EnableEnterKeyNavigation(this);

            if (_isEditMode)
            {
                DialogTitle.Text = "تعديل الدور";
                RoleNameTextBox.Text = _role.RoleName;
                DescriptionTextBox.Text = _role.Description;

                // منع تعديل اسم دور النظام "Admin" ولكن السماح بتعديل الصلاحيات (اختياري)
                // يفضل عدم السماح بتعديل Admin
                if (_role.IsSystemRole && _role.RoleID == 1)
                {
                    RoleNameTextBox.IsReadOnly = true;
                    // DescriptionTextBox.IsReadOnly = true; 
                    // يمكن السماح بتعديل الوصف
                }
            }
            else
            {
                _role = new Role();
                DialogTitle.Text = "إضافة دور جديد";
            }
        }

        private void LoadPermissions()
        {
            try
            {
                // 1. الحصول على جميع الصلاحيات المعرفة في النظام
                // بما أن جدول Permissions قد لا يحتوي على كل شيء، نعتمد على PermissionKeys ثم نربط
                // لكن الأفضل الاعتماد على ما هو موجود في جدول Permissions
                // سنفترض أن جدول Permissions مملوء بجميع مفاتيح PermissionKeys
                _allPermissions = PermissionDAL.GetAllSystemPermissions();

                // 2. إذا كان تعديل، نحدد الصلاحيات الممنوحة
                if (_isEditMode)
                {
                    var rolePermissions = PermissionDAL.GetRolePermissions(_role.RoleID);
                    foreach (var perm in _allPermissions)
                    {
                        if (rolePermissions.Contains(perm.PermissionKey))
                        {
                            perm.IsGranted = true;
                        }
                    }
                }

                // 3. تجميع الصلاحيات حسب الفئة
                var groupedPermissions = _allPermissions
                    .GroupBy(p => p.Category)
                    .Select(g => new PermissionGroup
                    {
                        Category = g.Key,
                        Permissions = [.. g]
                    })
                    .ToList();

                PermissionsItemsControl.ItemsSource = groupedPermissions;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل الصلاحيات: {ex.Message}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RoleNameTextBox.Text))
            {
                _ = MessageBox.Show("يرجى إدخال اسم الدور", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _role.RoleName = RoleNameTextBox.Text;
                _role.Description = DescriptionTextBox.Text;

                int roleId = _role.RoleID;

                if (_isEditMode)
                {
                    PermissionDAL.UpdateRole(_role);
                }
                else
                {
                    roleId = PermissionDAL.AddRole(_role);
                    _role.RoleID = roleId;
                }

                // حفظ الصلاحيات
                // نجمع المفاتيح التي تم تحديدها
                var grantedPermissions = _allPermissions
                    .Where(p => p.IsGranted)
                    .Select(p => p.PermissionKey)
                    .ToList();

                PermissionDAL.UpdateRolePermissions(roleId, grantedPermissions);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class PermissionGroup
    {
        public string Category { get; set; }
        public List<Permission> Permissions { get; set; }
    }
}
