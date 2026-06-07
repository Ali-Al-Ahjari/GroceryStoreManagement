using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text;
using Microsoft.Win32;

namespace LicenseIssuerCSharp
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;

        public MainWindow()
        {
            InitializeComponent();
            LoadAppSettings();
            LoadDashboard();
            LoadHistory();
        }

        #region AppSettings & Loading

        private void LoadAppSettings()
        {
            try
            {
                _settings = SettingsStorage.LoadSettings();
                TxtSettingsIssuer.Text = _settings.DefaultIssuer;
                TxtSettingsKey.Text = _settings.CustomPrivateKeyPem;
                TxtExpiryDays.Text = _settings.DefaultExpiryDays.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل الإعدادات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDashboard()
        {
            try
            {
                int total, active, expired;
                LicenseStorage.GetDashboardStats(out total, out active, out expired);
                TxtTotalLicenses.Text = total.ToString();
                TxtActiveLicenses.Text = active.ToString();
                TxtExpiredLicenses.Text = expired.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل الإحصائيات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHistory()
        {
            try
            {
                List<LicenseRecord> list = LicenseStorage.GetAllLicenses();
                ListHistoryItems.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل سجل التراخيص: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Title Bar & Window Management

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Navigation Switching

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var activeBtn = sender as Button;
            if (activeBtn == null) return;

            // Reset all buttons
            BtnDashboard.Tag = null;
            BtnIssue.Tag = null;
            BtnHistory.Tag = null;
            BtnSettings.Tag = null;

            // Mark active
            activeBtn.Tag = "Active";

            // Hide all panels
            GridDashboard.Visibility = Visibility.Collapsed;
            GridIssue.Visibility = Visibility.Collapsed;
            GridHistory.Visibility = Visibility.Collapsed;
            GridSettings.Visibility = Visibility.Collapsed;

            // Show current panel
            if (activeBtn == BtnDashboard)
            {
                LoadDashboard();
                GridDashboard.Visibility = Visibility.Visible;
            }
            else if (activeBtn == BtnIssue)
            {
                GridIssue.Visibility = Visibility.Visible;
            }
            else if (activeBtn == BtnHistory)
            {
                LoadHistory();
                GridHistory.Visibility = Visibility.Visible;
            }
            else if (activeBtn == BtnSettings)
            {
                GridSettings.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Issue License Events

        private void PasteFingerprint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    string fingerprint = LicenseEngine.NormalizeFingerprint(text);
                    if (!string.IsNullOrEmpty(fingerprint))
                    {
                        TxtFingerprint.Text = fingerprint;
                    }
                    else
                    {
                        MessageBox.Show("لم نتمكن من استخراج بصمة جهاز صالحة من النص المنسوخ.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("الحافظة لا تحتوي على نصوص للصقها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الصق: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateLicense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string customer = TxtCustomerName.Text.Trim();
                string fingerprint = TxtFingerprint.Text.Trim();
                string daysText = TxtExpiryDays.Text.Trim();

                if (string.IsNullOrEmpty(fingerprint))
                {
                    MessageBox.Show("يرجى إدخال أو لصق بصمة الجهاز أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int days;
                if (!int.TryParse(daysText, out days) || days <= 0)
                {
                    MessageBox.Show("عدد الأيام يجب أن يكون رقماً صحيحاً أكبر من صفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(customer))
                {
                    customer = "عميل-" + (fingerprint.Length > 8 ? fingerprint.Substring(0, 8) : fingerprint);
                    TxtCustomerName.Text = customer;
                }

                string keyPem = string.IsNullOrEmpty(_settings.CustomPrivateKeyPem) ? null : _settings.CustomPrivateKeyPem;
                string issuer = string.IsNullOrEmpty(_settings.DefaultIssuer) ? "StoreOwner" : _settings.DefaultIssuer;

                string token = LicenseEngine.GenerateToken(fingerprint, days, keyPem, issuer);
                TxtOutputToken.Text = token;

                // Save to XML persistence
                var record = new LicenseRecord
                {
                    CustomerName = customer,
                    MachineFingerprint = LicenseEngine.NormalizeFingerprint(fingerprint),
                    Issuer = issuer,
                    IssueDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ExpiryDate = DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Token = token
                };

                LicenseStorage.SaveLicense(record);
                LoadDashboard();

                MessageBox.Show("تم إصدار الترخيص بنجاح وحفظه في السجل!", "تم بنجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل توليد كود التفعيل: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string token = TxtOutputToken.Text.Trim();
                if (!string.IsNullOrEmpty(token))
                {
                    Clipboard.SetText(token);
                    MessageBox.Show("تم نسخ كود التفعيل إلى الحافظة بنجاح.", "تم النسخ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("لا يوجد كود تفعيل لنسخه.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل نسخ الكود: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region History Tab Events

        private void RefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            LoadHistory();
        }

        private void CopyHistoryToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                if (btn != null && btn.Tag != null)
                {
                    string token = btn.Tag.ToString();
                    Clipboard.SetText(token);
                    MessageBox.Show("تم نسخ كود التفعيل إلى الحافظة بنجاح.", "تم النسخ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل نسخ الكود: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Settings Tab Events

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string issuer = TxtSettingsIssuer.Text.Trim();
                string keyPem = TxtSettingsKey.Text.Trim();
                string daysText = TxtExpiryDays.Text.Trim();

                int days = 30;
                int.TryParse(daysText, out days);

                _settings.DefaultIssuer = string.IsNullOrEmpty(issuer) ? "StoreOwner" : issuer;
                _settings.CustomPrivateKeyPem = keyPem;
                _settings.DefaultExpiryDays = days > 0 ? days : 30;

                SettingsStorage.SaveSettings(_settings);
                MessageBox.Show("تم حفظ الإعدادات والأمان بنجاح.", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل حفظ الإعدادات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateKeys_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Title = "اختر مكان حفظ المفتاح الخاص والمفتاح العام",
                    Filter = "Private Key File (*.pem)|*.pem",
                    FileName = "license_private_key.pem"
                };

                if (sfd.ShowDialog() == true)
                {
                    string outDir = Path.GetDirectoryName(sfd.FileName);
                    
                    string privateKey, publicKey;
                    LicenseEngine.GenerateNewKeypair(outDir, out privateKey, out publicKey);

                    // E2E scripts also look for private_key.pem / public_key.pem
                    File.WriteAllText(Path.Combine(outDir, "private_key.pem"), privateKey, Encoding.ASCII);
                    File.WriteAllText(Path.Combine(outDir, "public_key.pem"), publicKey, Encoding.ASCII);

                    MessageBox.Show("تم توليد زوج المفاتيح وتصديرهما بنجاح إلى المجلد:\n" + outDir, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل توليد وتصدير المفاتيح: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
