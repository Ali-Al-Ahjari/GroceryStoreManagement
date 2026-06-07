using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Windows;

namespace GroceryStoreManagement.Windows
{
    public partial class InitialSetupWindow : Window
    {
        public string CreatedUsername { get; private set; }

        public InitialSetupWindow()
        {
            InitializeComponent();
            EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameTextBox.Text.Trim();
                string fullName = FullNameTextBox.Text.Trim();
                string email = EmailTextBox.Text.Trim();
                string phone = PhoneTextBox.Text.Trim();
                string password = PasswordBox.Password;
                string confirm = ConfirmPasswordBox.Password;

                if (string.IsNullOrWhiteSpace(username))
                {
                    _ = MessageBox.Show("اسم المستخدم مطلوب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    UsernameTextBox.Focus();
                    return;
                }

                if (UserDAL.IsUsernameExists(username))
                {
                    _ = MessageBox.Show("اسم المستخدم مستخدم مسبقاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    UsernameTextBox.Focus();
                    return;
                }

                if (!PasswordHelper.IsStrongPassword(password, out string error))
                {
                    _ = MessageBox.Show(error, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (password != confirm)
                {
                    _ = MessageBox.Show("كلمة المرور غير متطابقة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return;
                }

                var admin = new User
                {
                    Username = username,
                    Password = password,
                    FullName = string.IsNullOrWhiteSpace(fullName) ? "مدير النظام" : fullName,
                    Email = email,
                    Phone = phone,
                    IsActive = true,
                    RoleID = 1,
                    CanAccessDashboard = true,
                    CanViewCustomers = true,
                    CanAddCustomers = true,
                    CanEditCustomers = true,
                    CanDeleteCustomers = true,
                    CanManageProducts = true,
                    CanManageInvoices = true,
                    CanViewReports = true,
                    CanManageSettings = true,
                    CanBackup = true
                };

                UserDAL.AddUser(admin);
                CreatedUsername = admin.Username;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل إنشاء مدير النظام لأول مرة");
                _ = MessageBox.Show($"حدث خطأ أثناء إنشاء المدير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
