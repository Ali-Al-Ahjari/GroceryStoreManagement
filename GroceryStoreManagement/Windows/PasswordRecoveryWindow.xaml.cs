using System;
using System.Windows;
using GroceryStoreManagement.DAL;

namespace GroceryStoreManagement.Windows
{
    public partial class PasswordRecoveryWindow : Window
    {
        public PasswordRecoveryWindow()
        {
            InitializeComponent();
            
            // تفعيل التنقل بمفتاح Enter
            Helpers.EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string email = TxtEmail.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
            {
                ShowMessage("الرجاء إدخال اسم المستخدم والبريد الإلكتروني", true);
                return;
            }

            try
            {
                var user = UserDAL.GetUserByUsername(username);
                if (user != null && user.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    ShowMessage("تم إرسال رابط استعادة كلمة المرور إلى بريدك الإلكتروني بنجاح.", false);
                    BtnSubmit.IsEnabled = false;
                }
                else
                {
                    ShowMessage("بيانات المستخدم غير متطابقة. يرجى التحقق من اسم المستخدم والبريد الإلكتروني.", true);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("حدث خطأ أثناء معالجة الطلب: " + ex.Message, true);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowMessage(string message, bool isError)
        {
            TxtMessage.Text = message;
            TxtMessage.Foreground = isError ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
            MessagePanel.Visibility = Visibility.Visible;
        }
    }
}

