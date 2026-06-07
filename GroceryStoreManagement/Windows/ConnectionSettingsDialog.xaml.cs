using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace GroceryStoreManagement.Windows
{
    public partial class ConnectionSettingsDialog : Window
    {
        public ConnectionSettingsDialog()
        {
            InitializeComponent();
            LoadCurrentPath();
            
            // تفعيل التنقل بمفتاح Enter
            Helpers.EnterKeyHelper.EnableEnterKeyNavigation(this);
        }

        private void LoadCurrentPath()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "db_config.txt");
            if (File.Exists(configPath))
            {
                TxtDatabasePath.Text = File.ReadAllText(configPath).Trim();
            }
            else
            {
                TxtDatabasePath.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "GroceryStore.db");
            }

            UpdateDatabaseInfo();
        }

        private void UpdateDatabaseInfo()
        {
            try
            {
                if (File.Exists(TxtDatabasePath.Text))
                {
                    var fileInfo = new FileInfo(TxtDatabasePath.Text);
                    double sizeMB = fileInfo.Length / (1024.0 * 1024.0);
                    TxtDatabaseSize.Text = $"{sizeMB:F2} MB";
                    TxtConnectionStatus.Text = "متصل ✓";
                }
                else
                {
                    TxtDatabaseSize.Text = "-- MB";
                    TxtConnectionStatus.Text = "غير متصل ✗";
                }
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogError(ex, "فشل تحديث معلومات قاعدة البيانات");
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Database Files (*.db)|*.db|All Files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                TxtDatabasePath.Text = openFileDialog.FileName;
                UpdateDatabaseInfo();
            }
        }

        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(TxtDatabasePath.Text))
                {
                    _ = MessageBox.Show("الاتصال ناجح.", "اختبار الاتصال", MessageBoxButton.OK, MessageBoxImage.Information);
                    TxtConnectionStatus.Text = "متصل ✓";
                }
                else
                {
                    _ = MessageBox.Show("ملف قاعدة البيانات غير موجود.", "اختبار الاتصال", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtConnectionStatus.Text = "غير متصل ✗";
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"فشل الاتصال: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newPath = TxtDatabasePath.Text.Trim();
                if (string.IsNullOrEmpty(newPath))
                {
                    _ = MessageBox.Show("الرجاء اختيار مسار قاعدة البيانات", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string configFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(configFolder))
                {
                    _ = Directory.CreateDirectory(configFolder);
                }

                string configPath = Path.Combine(configFolder, "db_config.txt");
                File.WriteAllText(configPath, newPath);

                _ = MessageBox.Show("تم حفظ البيانات بنجاح. يرجى إعادة تشغيل البرنامج لتطبيق التغييرات.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
