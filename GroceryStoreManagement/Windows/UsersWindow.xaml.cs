using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Models;
using GroceryStoreManagement.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace GroceryStoreManagement.Windows
{
    public partial class UsersWindow : UserControl
    {
        private List<User> _allUsers = [];

        public UsersWindow()
        {
            InitializeComponent();
            LoadUsers();
            LoadRoles();
        }

        private void LoadUsers()
        {
            try
            {
                _allUsers = UserDAL.GetAllUsers();
                ApplyFilters();
                _ = (TxtTotalUsers?.Text = $"إجمالي المستخدمين: {_allUsers.Count}");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل المستخدمين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRoles()
        {
            try
            {
                var roles = PermissionDAL.GetAllRoles();
                roles.Insert(0, new Role { RoleID = 0, RoleName = "جميع الأدوار" });

                CmbRole.ItemsSource = roles;
                CmbRole.SelectedValue = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في تحميل الأدوار");
            }
        }

        private void ApplyFilters()
        {
            if (_allUsers == null || UsersDataGrid == null) return;

            var filtered = _allUsers.AsEnumerable();

            if (TxtSearch != null && !string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                string search = TxtSearch.Text.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                filtered = filtered.Where(u =>
                    (u.Username != null && u.Username.Contains(search, StringComparison.CurrentCultureIgnoreCase)) ||
                    (u.FullName != null && u.FullName.Contains(search, StringComparison.CurrentCultureIgnoreCase)) ||
                    (u.Email != null && u.Email.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                );
            }

            if (CmbRole != null && CmbRole.SelectedValue is int roleId && roleId > 0)
            {
                filtered = filtered.Where(u => u.RoleID == roleId);
            }

            UsersDataGrid.ItemsSource = filtered.ToList();
        }

        // Event Handlers matching XAML

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var user = UserDAL.GetUserById(userId);
                if (user != null)
                {
                    var dialog = new UserDialog(user);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadUsers();
                    }
                }
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                if (MessageBox.Show("هل أنت متأكد من حذف هذا المستخدم؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        UserDAL.DeleteUser(userId);
                        LoadUsers();
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show($"خطأ في حذف المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnManageRoles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new RoleDialog();
                if (dialog.ShowDialog() == true)
                {
                    LoadRoles();
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في فتح إدارة الأدوار");
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnPermissions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int userId)
                {
                    var user = UserDAL.GetUserById(userId);
                    if (user == null)
                        return;

                    if (user.RoleID <= 0)
                    {
                        _ = MessageBox.Show("يرجى تعيين دور للمستخدم أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var role = PermissionDAL.GetAllRoles().FirstOrDefault(r => r.RoleID == user.RoleID);
                    if (role == null)
                    {
                        _ = MessageBox.Show("تعذر العثور على الدور المرتبط بهذا المستخدم.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var dialog = new RoleDialog(role);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadUsers();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في إدارة صلاحيات المستخدم");
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                try
                {
                    var user = UserDAL.GetUserById(userId);
                    if (user == null) return;

                    if (user.RoleID == 1 && user.IsActive)
                    {
                        int activeAdmins = UserDAL.GetActiveAdminsCount();
                        if (activeAdmins <= 1)
                        {
                            _ = MessageBox.Show("لا يمكن إيقاف آخر مدير نشط في النظام.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    UserDAL.SetUserActive(userId, !user.IsActive);
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "خطأ في تغيير حالة المستخدم");
                    _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
