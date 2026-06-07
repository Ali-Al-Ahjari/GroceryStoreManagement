using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Windows;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة إدارة الحساب الشخصي للمستخدم الحالي
    /// </summary>
    public partial class MyAccountWindow : Window
    {
        private User _currentUser;

        public MyAccountWindow()
        {
            InitializeComponent();
            LoadCurrentUserData();
        }

        private void LoadCurrentUserData()
        {
            try
            {
                if (!SessionContext.IsLoggedIn)
                {
                    _ = MessageBox.Show("لا توجد جلسة مستخدم نشطة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DialogResult = false;
                    Close();
                    return;
                }

                _currentUser = UserDAL.GetUserById(SessionContext.CurrentUserID);
                if (_currentUser == null)
                {
                    _ = MessageBox.Show("تعذر تحميل بيانات الحساب الحالي.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                    return;
                }

                UsernameTextBox.Text = _currentUser.Username;
                FullNameTextBox.Text = _currentUser.FullName;
                PhoneTextBox.Text = _currentUser.Phone;
                EmailTextBox.Text = _currentUser.Email;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في تحميل بيانات الحساب الشخصي");
                _ = MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    _ = MessageBox.Show("بيانات الحساب غير متاحة.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                {
                    _ = MessageBox.Show("الاسم الكامل مطلوب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _ = FullNameTextBox.Focus();
                    return;
                }

                // نجلب أحدث نسخة من المستخدم قبل الحفظ
                User userToUpdate = UserDAL.GetUserById(_currentUser.UserID);
                if (userToUpdate == null)
                {
                    _ = MessageBox.Show("تعذر تحديث الحساب، المستخدم غير موجود.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                userToUpdate.FullName = FullNameTextBox.Text.Trim();
                userToUpdate.Phone = PhoneTextBox.Text?.Trim();
                userToUpdate.Email = EmailTextBox.Text?.Trim();

                bool wantsPasswordChange =
                    !string.IsNullOrWhiteSpace(CurrentPasswordBox.Password) ||
                    !string.IsNullOrWhiteSpace(NewPasswordBox.Password) ||
                    !string.IsNullOrWhiteSpace(ConfirmNewPasswordBox.Password);

                if (wantsPasswordChange)
                {
                    if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password) ||
                        string.IsNullOrWhiteSpace(NewPasswordBox.Password) ||
                        string.IsNullOrWhiteSpace(ConfirmNewPasswordBox.Password))
                    {
                        _ = MessageBox.Show("لتغيير كلمة المرور يجب تعبئة الحقول الثلاثة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!UserDAL.VerifyCurrentPassword(userToUpdate.UserID, CurrentPasswordBox.Password))
                    {
                        _ = MessageBox.Show("كلمة المرور الحالية غير صحيحة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (NewPasswordBox.Password != ConfirmNewPasswordBox.Password)
                    {
                        _ = MessageBox.Show("كلمة المرور الجديدة غير متطابقة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!PasswordHelper.IsStrongPassword(NewPasswordBox.Password, out string passwordError))
                    {
                        _ = MessageBox.Show(passwordError, "كلمة مرور ضعيفة", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    userToUpdate.Password = NewPasswordBox.Password;
                }

                UserDAL.UpdateUser(userToUpdate);
                SessionContext.CurrentUser = UserDAL.GetUserById(userToUpdate.UserID) ?? userToUpdate;

                _ = MessageBox.Show("تم حفظ بيانات الحساب بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في حفظ بيانات الحساب الشخصي");
                _ = MessageBox.Show($"تعذر حفظ البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
