using GroceryStoreManagement.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GroceryStoreManagement.Windows
{
    public partial class ErrorLogsWindow : Window
    {
        private string _logFilePath;

        public ErrorLogsWindow()
        {
            InitializeComponent();
            LoadLogContent();
        }

        private void LoadLogContent()
        {
            try
            {
                // Get the most recent log file or single log file
                string[] files = Logger.GetLogFiles();
                if (files != null && files.Length > 0)
                {
                    // Get most recent file
                    _logFilePath = files[0];
                    foreach (var f in files)
                    {
                        if (File.GetLastWriteTime(f) > File.GetLastWriteTime(_logFilePath))
                            _logFilePath = f;
                    }

                    string content = Logger.GetLogContent(_logFilePath);
                    _ = (TxtLogContent?.Text = content);

                    _ = (TxtLogInfo?.Text = $"ملف السجل: {Path.GetFileName(_logFilePath)}");
                }
                else
                {
                    _ = (TxtLogContent?.Text = "لا توجد سجلات أخطاء.");
                    _ = (TxtLogInfo?.Text = "ملف السجل: --");
                }
            }
            catch (Exception ex)
            {
                _ = (TxtLogContent?.Text = $"خطأ في تحميل السجل: {ex.Message}");
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLogContent();
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logFolder = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logFolder) && Directory.Exists(logFolder))
                {
                    _ = Process.Start("explorer.exe", logFolder);
                }
                else
                {
                    _ = MessageBox.Show("مجلد السجلات غير موجود.", "تنبيه");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في فتح المجلد: {ex.Message}", "خطأ");
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل أنت متأكد من حذف جميع سجلات الأخطاء؟\nلا يمكن التراجع عن هذه العملية.", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    Logger.ClearAllLogs();
                    LoadLogContent();
                    _ = MessageBox.Show("تم مسح السجلات بنجاح.", "تم");
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show($"خطأ في مسح السجلات: {ex.Message}", "خطأ");
                }
            }
        }
    }
}
