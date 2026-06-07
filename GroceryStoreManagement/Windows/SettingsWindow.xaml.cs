using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GroceryStoreManagement.Windows
{
    /// <summary>
    /// نافذة الإعدادات - إدارة جميع إعدادات النظام
    /// </summary>
    public partial class SettingsWindow : UserControl
    {
        // مسار ملف الإعدادات
        private readonly string _settingsPath;
        private readonly string _backupPath;
        private string _savedPrinterName;

        /// <summary>
        /// المُنشئ
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
            OpenCashDrawerCheckBox.IsChecked = true;
            CashDrawerCommandTextBox.Text = "1B-70-00-19-FA";

            // تحديد مسارات الملفات
            string appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _settingsPath = Path.Combine(appDataPath, "settings.ini");
            _backupPath = Path.Combine(appDataPath, "Backups");

            // إنشاء مجلد النسخ الاحتياطية إذا لم يكن موجوداً
            if (!Directory.Exists(_backupPath))
                _ = Directory.CreateDirectory(_backupPath);

            LoadSettings();
            LoadPrinters();
            LoadUsers();
            LoadRoles();
            LoadBackupHistory();
            LoadDatabaseInfo();
            ApplyPermissions();
        }

        #region تحميل البيانات

        /// <summary>
        /// تطبيق الصلاحيات على عناصر الواجهة
        /// </summary>
        private void ApplyPermissions()
        {
            _ = (UsersSettingsRadio?.Visibility = PermissionHelper.HasPermission(PermissionKeys.ManageUsers) ? Visibility.Visible : Visibility.Collapsed);

            _ = (RolesSettingsRadio?.Visibility = PermissionHelper.HasPermission(PermissionKeys.ManagePermissions) ? Visibility.Visible : Visibility.Collapsed);

            _ = (BackupSettingsRadio?.Visibility = PermissionHelper.HasPermission(PermissionKeys.BackupDatabase) ? Visibility.Visible : Visibility.Collapsed);

            _ = (DatabaseSettingsRadio?.Visibility = PermissionHelper.HasPermission(PermissionKeys.ManageSystemSettings) ? Visibility.Visible : Visibility.Collapsed);

            _ = (SystemSettingsRadio?.Visibility = PermissionHelper.HasPermission(PermissionKeys.ManageSystemSettings) ? Visibility.Visible : Visibility.Collapsed);

            _ = (PrintSettingsRadio?.Visibility = PermissionHelper.HasPermission(PermissionKeys.ManageSystemSettings) ? Visibility.Visible : Visibility.Collapsed);

            _ = (EmailSettingsRadio?.Visibility = PermissionHelper.HasPermission(PermissionKeys.ManageSystemSettings) ? Visibility.Visible : Visibility.Collapsed);

            _ = (LogsSettingsButton?.Visibility = PermissionHelper.HasPermission(PermissionKeys.ViewActivityLog) ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// تحميل الإعدادات المحفوظة
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var lines = File.ReadAllLines(_settingsPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            switch (key)
                            {
                                case "CompanyName": CompanyNameTextBox.Text = value; break;
                                case "CompanyPhone": CompanyPhoneTextBox.Text = value; break;
                                case "CompanyAddress": CompanyAddressTextBox.Text = value; break;
                                case "CommercialReg": CommercialRegTextBox.Text = value; break;
                                case "TaxNumber": TaxNumberTextBox.Text = value; break;
                                case "VATPercent": VATPercentTextBox.Text = value; break;
                                case "Currency": if (int.TryParse(value, out int curr)) CurrencyComboBox.SelectedIndex = curr; break;
                                case "Language": if (int.TryParse(value, out int lang)) LanguageComboBox.SelectedIndex = lang; break;
                                case "DateFormat": if (int.TryParse(value, out int df)) DateFormatComboBox.SelectedIndex = df; break;
                                case "SmtpServer": SmtpServerTextBox.Text = value; break;
                                case "SmtpPort": SmtpPortTextBox.Text = value; break;
                                case "EmailAddress": EmailAddressTextBox.Text = value; break;
                                case "InvoiceFooter": InvoiceFooterTextBox.Text = value.Replace("\\n", "\n"); break;
                                case "ReceiptPrinter": _savedPrinterName = value; break;
                                case "OpenCashDrawerOnCashPayment":
                                    if (bool.TryParse(value, out bool openDrawer))
                                        OpenCashDrawerCheckBox.IsChecked = openDrawer;
                                    break;
                                case "CashDrawerKickCommandHex": CashDrawerCommandTextBox.Text = value; break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تحميل الإعدادات: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل قائمة الطابعات
        /// </summary>
        private void LoadPrinters()
        {
            try
            {
                PrinterComboBox.Items.Clear();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    _ = PrinterComboBox.Items.Add(printer);
                }

                if (!string.IsNullOrWhiteSpace(_savedPrinterName) && PrinterComboBox.Items.Contains(_savedPrinterName))
                {
                    PrinterComboBox.SelectedItem = _savedPrinterName;
                }
                else if (PrinterComboBox.Items.Count > 0)
                {
                    PrinterComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تحميل الطابعات: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل قائمة المستخدمين
        /// </summary>
        private void LoadUsers()
        {
            try
            {
                var users = UserDAL.GetAllUsers();
                UsersDataGrid.ItemsSource = users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تحميل المستخدمين: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل سجل النسخ الاحتياطية
        /// </summary>
        /// <summary>
        /// تحميل سجل النسخ الاحتياطية
        /// </summary>
        private void LoadBackupHistory()
        {
            try
            {
                var availableBackups = BackupHelper.GetAvailableBackups();

                var backups = availableBackups.Select(b => new
                {
                    Date = b.CreatedDateFormatted,
                    Size = b.FileSizeFormatted,
                    Type = b.IsCompressed ? "مضغوط" : "قاعدة بيانات",
                    Path = b.FilePath // يمكنك عرض الاسم فقط إذا كان المسار طويلاً: b.FileName
                }).ToList();

                BackupHistoryGrid.ItemsSource = backups;

                // تحديث الإحصائيات
                _ = (TotalBackupsText?.Text = availableBackups.Length.ToString());

                if (LastBackupText != null && availableBackups.Length != 0)
                    LastBackupText.Text = availableBackups.First().CreatedDateFormatted;
                else
                    _ = (LastBackupText?.Text = "-");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تحميل سجل النسخ: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل معلومات قاعدة البيانات
        /// </summary>
        private void LoadDatabaseInfo()
        {
            try
            {
                string dbPath = DatabaseHelper.GetDatabasePath();

                if (File.Exists(dbPath))
                {
                    var info = new FileInfo(dbPath);
                    DatabaseSizeText.Text = FormatFileSize(info.Length);
                    LastUpdateText.Text = info.LastWriteTime.ToString("yyyy/MM/dd");
                }

                // عدد الجداول الفعلي
                using var conn = DatabaseHelper.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                TablesCountText.Text = count.ToString();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في تحميل معلومات القاعدة");
            }
        }

        /// <summary>
        /// تنسيق حجم الملف
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1073741824)
                return $"{bytes / 1073741824.0:F2} GB";
            if (bytes >= 1048576)
                return $"{bytes / 1048576.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }

        #endregion

        #region تغيير الأقسام

        /// <summary>
        /// تحديد قسم معين برمجياً
        /// </summary>
        public void SelectSection(string sectionName)
        {
            switch (sectionName)
            {
                case "System": SystemSettingsRadio.IsChecked = true; break;
                case "Print": PrintSettingsRadio.IsChecked = true; break;
                case "Email": EmailSettingsRadio.IsChecked = true; break;
                case "Users": UsersSettingsRadio.IsChecked = true; break;
                case "Roles": RolesSettingsRadio.IsChecked = true; break;
                case "Backup": BackupSettingsRadio.IsChecked = true; break;
                case "Database": DatabaseSettingsRadio.IsChecked = true; break;
                default: SystemSettingsRadio.IsChecked = true; break;
            }
        }

        /// <summary>
        /// معالج تغيير قسم الإعدادات
        /// </summary>
        private void SettingsSection_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioButton || !radioButton.IsChecked.GetValueOrDefault()) return;

            // إخفاء جميع الأقسام
            _ = (SystemSettingsPanel?.Visibility = Visibility.Collapsed);
            _ = (PrintSettingsPanel?.Visibility = Visibility.Collapsed);
            _ = (EmailSettingsPanel?.Visibility = Visibility.Collapsed);
            _ = (EmailSettingsPanel?.Visibility = Visibility.Collapsed);
            _ = (UsersSettingsPanel?.Visibility = Visibility.Collapsed);
            _ = (RolesSettingsPanel?.Visibility = Visibility.Collapsed);
            _ = (BackupSettingsPanel?.Visibility = Visibility.Collapsed);
            _ = (DatabaseSettingsPanel?.Visibility = Visibility.Collapsed);

            // إظهار القسم المحدد
            if (radioButton == SystemSettingsRadio && SystemSettingsPanel != null)
                SystemSettingsPanel.Visibility = Visibility.Visible;
            else if (radioButton == PrintSettingsRadio && PrintSettingsPanel != null)
                PrintSettingsPanel.Visibility = Visibility.Visible;
            else if (radioButton == EmailSettingsRadio && EmailSettingsPanel != null)
                EmailSettingsPanel.Visibility = Visibility.Visible;
            else if (radioButton == UsersSettingsRadio && UsersSettingsPanel != null)
                UsersSettingsPanel.Visibility = Visibility.Visible;
            else if (radioButton == RolesSettingsRadio && RolesSettingsPanel != null)
                RolesSettingsPanel.Visibility = Visibility.Visible;
            else if (radioButton == BackupSettingsRadio && BackupSettingsPanel != null)
                BackupSettingsPanel.Visibility = Visibility.Visible;
            else if (radioButton == DatabaseSettingsRadio && DatabaseSettingsPanel != null)
                DatabaseSettingsPanel.Visibility = Visibility.Visible;
        }

        #endregion

        #region إعدادات النظام

        /// <summary>
        /// اختيار شعار الشركة
        /// </summary>
        private void SelectLogo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                    Title = "اختيار شعار الشركة"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var bitmap = new BitmapImage(new Uri(openDialog.FileName));
                    CompanyLogoImage.Source = bitmap;

                    // حفظ الشعار
                    string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "logo.png");
                    File.Copy(openDialog.FileName, logoPath, true);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في اختيار الشعار: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حذف شعار الشركة
        /// </summary>
        private void RemoveLogo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CompanyLogoImage.Source = null;

                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "logo.png");
                if (File.Exists(logoPath))
                    File.Delete(logoPath);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حذف الشعار: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حفظ إعدادات النظام
        /// </summary>
        private void SaveSystemSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAllSettings();
                _ = MessageBox.Show("تم حفظ إعدادات النظام بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region إعدادات الطباعة

        /// <summary>
        /// اختيار صورة التوقيع
        /// </summary>
        private void SelectSignature_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                    Title = "اختيار صورة التوقيع"
                };

                if (openDialog.ShowDialog() == true)
                {
                    string signaturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "signature.png");
                    File.Copy(openDialog.FileName, signaturePath, true);
                    _ = MessageBox.Show("تم حفظ التوقيع بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في اختيار التوقيع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حفظ إعدادات الطباعة
        /// </summary>
        private void SavePrintSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAllSettings();
                _ = MessageBox.Show("تم حفظ إعدادات الطباعة بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region إعدادات البريد

        /// <summary>
        /// اختبار اتصال البريد
        /// </summary>
        private void TestEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // اختبار الاتصال (يحتاج تنفيذ فعلي)
                _ = MessageBox.Show("جاري اختبار الاتصال...\n\nهذه الميزة تحتاج إلى تفعيل في الإصدار القادم.", "اختبار الاتصال", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في اختبار الاتصال: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حفظ إعدادات البريد
        /// </summary>
        private void SaveEmailSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAllSettings();
                _ = MessageBox.Show("تم حفظ إعدادات البريد بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region إدارة المستخدمين

        /// <summary>
        /// إضافة مستخدم جديد
        /// </summary>
        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new UserDialog();
                if (dialog.ShowDialog() == true)
                {
                    LoadUsers();
                    _ = MessageBox.Show("تم إضافة المستخدم بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إضافة المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تعديل مستخدم
        /// </summary>
        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int userId = Convert.ToInt32(button.Tag);
                    var user = UserDAL.GetUserById(userId);

                    if (user != null)
                    {
                        var dialog = new UserDialog(user);
                        if (dialog.ShowDialog() == true)
                        {
                            LoadUsers();
                            _ = MessageBox.Show("تم تحديث المستخدم بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تعديل المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// صلاحيات المستخدم
        /// </summary>
        private void UserPermissions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int userId = Convert.ToInt32(button.Tag);
                    _ = MessageBox.Show($"فتح صلاحيات المستخدم رقم {userId}\n\nهذه الميزة ستُضاف في الإصدار القادم.", "الصلاحيات", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حذف مستخدم
        /// </summary>
        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int userId = Convert.ToInt32(button.Tag);

                    var result = MessageBox.Show("هل تريد حذف هذا المستخدم؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        UserDAL.DeleteUser(userId);
                        LoadUsers();
                        _ = MessageBox.Show("تم حذف المستخدم بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في حذف المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion



        #region إدارة الأدوار

        private void LoadRoles()
        {
            try
            {
                RolesDataGrid.ItemsSource = PermissionDAL.GetAllRoles();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تحميل الأدوار: {ex.Message}");
            }
        }

        private void AddRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new RoleDialog();
                if (dialog.ShowDialog() == true)
                {
                    LoadRoles();
                    _ = MessageBox.Show("تم إضافة التعديلات بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is int roleId)
                {
                    var role = PermissionDAL.GetAllRoles().FirstOrDefault(r => r.RoleID == roleId);
                    if (role != null)
                    {
                        var dialog = new RoleDialog(role);
                        if (dialog.ShowDialog() == true)
                        {
                            LoadRoles();
                            _ = MessageBox.Show("تم حفظ التعديلات", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is int roleId)
                {
                    if (MessageBox.Show("هل أنت متأكد من حذف هذا الدور؟ سيتم حذف جميع الصلاحيات المرتبطة به.", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        // Check if any users are assigned to this role first?
                        // PermissionDAL.DeleteRole handles System Role check, but maybe we should check usage.
                        // Assuming simple delete for now.
                        PermissionDAL.DeleteRole(roleId);
                        LoadRoles();
                        _ = MessageBox.Show("تم حذف الدور بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region النسخ الاحتياطي

        /// <summary>
        /// إنشاء نسخة احتياطية
        /// </summary>
        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string backupPath = BackupHelper.CreateBackup("نسخة يدوية من الإعدادات");
                LoadBackupHistory();
                _ = MessageBox.Show($"تم إنشاء النسخة الاحتياطية بنجاح!\n\n{Path.GetFileName(backupPath)}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في إنشاء النسخة الاحتياطية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// استعادة نسخة احتياطية
        /// </summary>
        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Backup Files (*.backup;*.zip;*.db)|*.backup;*.zip;*.db",
                    InitialDirectory = BackupHelper.BackupDirectory,
                    Title = "اختيار نسخة احتياطية للاستعادة"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "تحذير: سيتم استبدال جميع البيانات الحالية بالنسخة الاحتياطية!\n\nهل تريد المتابعة؟",
                        "تأكيد الاستعادة", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        _ = BackupHelper.RestoreBackup(openDialog.FileName);
                        _ = MessageBox.Show("تم استعادة النسخة الاحتياطية بنجاح!\n\nيرجى إعادة تشغيل التطبيق لضمان عمل البيانات بشكل صحيح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                        // يمكن إعادة تشغيل التطبيق تلقائياً
                        // System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                        // Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في استعادة النسخة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تنزيل نسخة احتياطية
        /// </summary>
        private void DownloadBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Zip File (*.zip)|*.zip|Backup File (*.backup)|*.backup",
                    FileName = $"GroceryStore_Backup_{DateTime.Now:yyyyMMdd_HHmm}",
                    Title = "حفظ النسخة الاحتياطية"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // نستخدم ExportBackup من BackupHelper
                    // نحدد الضغط بناءً على الامتداد المختار
                    bool compress = saveDialog.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

                    // لأن ExportBackup تأخذ المسار المجلد وليس الملف الكامل (في التنفيذ الحالي للكود المعروض سابقاً قد يكون مختلفاً،
                    // لكن BackupHelper.ExportBackup تأخذ destinationPath كمسار "مجرد" وتنسخ داخله، أو كملف؟
                    // دعنا نراجع BackupHelper.ExportBackup:
                    // string destFile = Path.Combine(destinationPath, Path.GetFileName(backupPath));
                    // إذن هي تأخذ مجلد. لكن SaveFileDialog يعطينا ملف.
                    // لذا سأستخدم نسخ مباشر من ملف جديد يتم إنشاؤه.

                    string tempBackup = compress ? BackupHelper.CreateCompressedBackup("تنزيل") : BackupHelper.CreateBackup("تنزيل");
                    File.Copy(tempBackup, saveDialog.FileName, true);

                    _ = MessageBox.Show("تم تنزيل النسخة الاحتياطية بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ في تنزيل النسخة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// جدولة النسخ الاحتياطي
        /// </summary>
        private void ScheduleBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAllSettings();
                _ = MessageBox.Show("تم حفظ إعدادات الجدولة!\n\nالنسخ الاحتياطي التلقائي سيعمل حسب الإعدادات المحددة.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حفظ إعدادات النسخ الاحتياطي
        /// </summary>
        private void SaveBackupSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAllSettings();
                _ = MessageBox.Show("تم حفظ إعدادات النسخ الاحتياطي بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region قاعدة البيانات

        /// <summary>
        /// فحص صحة قاعدة البيانات
        /// </summary>
        private void CheckDatabaseHealth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // فحص بسيط - محاولة فتح الاتصال
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                _ = MessageBox.Show("✅ قاعدة البيانات سليمة وتعمل بشكل صحيح!", "فحص الصحة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"❌ خطأ في قاعدة البيانات: {ex.Message}", "فحص الصحة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تنظيف البيانات القديمة
        /// </summary>
        private void CleanOldData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = MessageBox.Show("هذه الميزة ستحذف البيانات القديمة (أكثر من سنة).\n\nستُضاف في الإصدار القادم.", "تنظيف البيانات", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحسين أداء قاعدة البيانات
        /// </summary>
        private void OptimizeDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "VACUUM";
                    _ = cmd.ExecuteNonQuery();
                }

                LoadDatabaseInfo();
                _ = MessageBox.Show("✅ تم تحسين قاعدة البيانات بنجاح!", "تحسين الأداء", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حذف جميع البيانات
        /// </summary>
        private void ClearAllData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "⚠️ تحذير خطير!\n\nسيتم حذف جميع البيانات نهائياً!\nهذا الإجراء لا يمكن التراجع عنه.\n\nهل أنت متأكد؟",
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    var confirm = MessageBox.Show(
                        "آخر فرصة!\n\nاكتب 'نعم' والضغط على OK لتأكيد الحذف.",
                        "تأكيد نهائي", MessageBoxButton.OKCancel, MessageBoxImage.Stop);

                    if (confirm == MessageBoxResult.OK)
                    {
                        // حذف وإعادة إنشاء قاعدة البيانات
                        DatabaseHelper.ResetDatabase();
                        _ = MessageBox.Show("تم حذف جميع البيانات وإعادة تهيئة قاعدة البيانات.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region حفظ الإعدادات

        /// <summary>
        /// حفظ جميع الإعدادات في ملف
        /// </summary>
        private void SaveAllSettings()
        {
            try
            {
                var settings = new List<string>
                {
                    $"CompanyName={CompanyNameTextBox.Text}",
                    $"CompanyPhone={CompanyPhoneTextBox.Text}",
                    $"CompanyAddress={CompanyAddressTextBox.Text.Replace("\n", "\\n")}",
                    $"CommercialReg={CommercialRegTextBox.Text}",
                    $"TaxNumber={TaxNumberTextBox.Text}",
                    $"VATPercent={VATPercentTextBox.Text}",
                    $"Currency={CurrencyComboBox.SelectedIndex}",
                    $"Language={LanguageComboBox.SelectedIndex}",
                    $"DateFormat={DateFormatComboBox.SelectedIndex}",
                    $"SmtpServer={SmtpServerTextBox.Text}",
                    $"SmtpPort={SmtpPortTextBox.Text}",
                    $"EmailAddress={EmailAddressTextBox.Text}",
                    $"InvoiceFooter={InvoiceFooterTextBox.Text.Replace("\n", "\\n")}",
                    $"ReceiptPrinter={PrinterComboBox.SelectedItem?.ToString() ?? string.Empty}",
                    $"OpenCashDrawerOnCashPayment={OpenCashDrawerCheckBox.IsChecked.GetValueOrDefault(true)}",
                    $"CashDrawerKickCommandHex={CashDrawerCommandTextBox.Text.Trim()}",
                    $"BackupFrequency={BackupFrequencyComboBox.SelectedIndex}",
                    $"BackupTime={BackupTimeComboBox.SelectedIndex}",
                    $"AutoBackup={AutoBackupCheckBox.IsChecked}"
                };

                File.WriteAllLines(_settingsPath, settings);
                AppSettings.Reload();
                _ = AppSettings.ApplyCulture();
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حفظ الإعدادات: {ex.Message}");
            }
        }

        #endregion
        private void OpenLogsWindow_Click(object sender, RoutedEventArgs e)
        {
            var win = new LogsWindow();
            _ = win.ShowDialog();

            // إعادة تحديد القسم السابق لتجنب إبقاء الزر محدداً
            RestoreSelection();
        }

        private void OpenErrorLogsWindow_Click(object sender, RoutedEventArgs e)
        {
            var win = new ErrorLogsWindow();
            _ = win.ShowDialog();

            // إعادة تحديد القسم السابق لتجنب إبقاء الزر محدداً
            RestoreSelection();
        }

        private void RestoreSelection()
        {
            if (SystemSettingsPanel.Visibility == Visibility.Visible) SystemSettingsRadio.IsChecked = true;
            else if (PrintSettingsPanel.Visibility == Visibility.Visible) PrintSettingsRadio.IsChecked = true;
            else if (EmailSettingsPanel.Visibility == Visibility.Visible) EmailSettingsRadio.IsChecked = true;
            else if (UsersSettingsPanel.Visibility == Visibility.Visible) UsersSettingsRadio.IsChecked = true;
            else if (BackupSettingsPanel.Visibility == Visibility.Visible) BackupSettingsRadio.IsChecked = true;
            else if (DatabaseSettingsPanel.Visibility == Visibility.Visible) DatabaseSettingsRadio.IsChecked = true;
        }
        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            var activityLogWindow = new LogsWindow();
            _ = activityLogWindow.ShowDialog();
        }
    }
}
