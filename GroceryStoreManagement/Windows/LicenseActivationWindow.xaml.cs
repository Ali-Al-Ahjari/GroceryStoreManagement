using GroceryStoreManagement.Helpers;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GroceryStoreManagement.Windows
{
    public partial class LicenseActivationWindow : Window
    {
        private bool _isActivating;

        private enum InlineStatusTone
        {
            Info = 0,
            Success = 1,
            Warning = 2,
            Error = 3
        }

        public LicenseActivationWindow()
        {
            InitializeComponent();
            Loaded += LicenseActivationWindow_Loaded;
        }

        private void LicenseActivationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtMachineFingerprint.Text = LicenseService.GetMachineFingerprint();
            RefreshCurrentLicenseStatus();
            UpdateActivationCodeState();
            SetInlineStatus(
                "جاهز",
                "أدخل كود التفعيل ثم اضغط تفعيل الآن.",
                InlineStatusTone.Info);

            TxtActivationCode.Focus();
        }

        private void BtnCopyFingerprint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(TxtMachineFingerprint.Text ?? string.Empty);
                SetInlineStatus(
                    "تم النسخ",
                    "تم نسخ بصمة الجهاز إلى الحافظة. يمكنك إرسالها مباشرة للمطور.",
                    InlineStatusTone.Success);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل نسخ بصمة الجهاز");
                SetInlineStatus(
                    "تعذر النسخ",
                    "تعذر نسخ البصمة. يمكنك تحديد النص ونسخه يدويًا.",
                    InlineStatusTone.Warning);
            }
        }

        private void BtnCopyActivationRequest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string requestText = BuildActivationRequestMessage();
                Clipboard.SetText(requestText);
                SetInlineStatus(
                    "تم نسخ الرسالة",
                    "تم نسخ رسالة طلب التفعيل. أرسلها للمطور كما هي.",
                    InlineStatusTone.Success);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل نسخ رسالة طلب التفعيل");
                SetInlineStatus(
                    "تعذر النسخ",
                    "تعذر نسخ رسالة الطلب. انسخ البصمة يدويًا وأرسلها للمطور.",
                    InlineStatusTone.Warning);
            }
        }

        private void BtnPasteActivationCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    SetInlineStatus(
                        "الحافظة فارغة",
                        "لا يوجد نص في الحافظة للصقه ككود تفعيل.",
                        InlineStatusTone.Warning);
                    return;
                }

                TxtActivationCode.Text = Clipboard.GetText();
                TxtActivationCode.Focus();
                TxtActivationCode.CaretIndex = TxtActivationCode.Text?.Length ?? 0;

                SetInlineStatus(
                    "تم اللصق",
                    "تم لصق الكود. تأكد منه ثم اضغط تفعيل الآن.",
                    InlineStatusTone.Info);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل لصق كود التفعيل من الحافظة");
                SetInlineStatus(
                    "فشل اللصق",
                    "تعذر قراءة النص من الحافظة. الصق الكود يدويًا.",
                    InlineStatusTone.Warning);
            }
        }

        private void BtnClearActivationCode_Click(object sender, RoutedEventArgs e)
        {
            TxtActivationCode.Clear();
            SetInlineStatus(
                "تم المسح",
                "تم مسح محتوى كود التفعيل.",
                InlineStatusTone.Info);
            TxtActivationCode.Focus();
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            ActivateLicense();
        }

        private void ActivateLicense()
        {
            if (_isActivating)
            {
                return;
            }

            string token = NormalizeToken(TxtActivationCode.Text);
            if (string.IsNullOrWhiteSpace(token))
            {
                SetInlineStatus(
                    "كود غير موجود",
                    "أدخل كود التفعيل أولاً ثم أعد المحاولة.",
                    InlineStatusTone.Warning);
                TxtActivationCode.Focus();
                return;
            }

            SetBusyState(true);
            SetInlineStatus(
                "جاري التحقق",
                "يتم التحقق من الكود وتسجيل حالة الترخيص، يرجى الانتظار.",
                InlineStatusTone.Info);

            try
            {
                LicenseActivationResult result = LicenseService.ActivateLicense(token);
                if (result.IsSuccess)
                {
                    string expiryText = result.ExpiresAtUtc.HasValue
                        ? result.ExpiresAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                        : "غير معروف";

                    SetInlineStatus(
                        "تم التفعيل",
                        $"تم تفعيل الترخيص بنجاح. الصلاحية حتى: {expiryText}",
                        InlineStatusTone.Success);
                    RefreshCurrentLicenseStatus();

                    _ = MessageBox.Show(
                        $"تم تفعيل الترخيص بنجاح.\nصالح حتى: {expiryText}",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                    return;
                }

                SetInlineStatus(
                    "فشل التفعيل",
                    result.Message,
                    InlineStatusTone.Error);

                _ = MessageBox.Show(
                    result.Message,
                    "فشل التفعيل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void TxtActivationCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateActivationCodeState();
        }

        private void TxtActivationCode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control && BtnActivate.IsEnabled)
            {
                e.Handled = true;
                ActivateLicense();
            }
        }

        private void UpdateActivationCodeState()
        {
            string normalizedToken = NormalizeToken(TxtActivationCode.Text);
            TxtCodeStats.Text = $"عدد أحرف الكود: {normalizedToken.Length}";
            BtnActivate.IsEnabled = !_isActivating && normalizedToken.Length > 0;
        }

        private void SetBusyState(bool isBusy)
        {
            _isActivating = isBusy;
            TxtActivationCode.IsReadOnly = isBusy;
            BtnPasteActivationCode.IsEnabled = !isBusy;
            BtnClearActivationCode.IsEnabled = !isBusy;
            BtnCopyFingerprint.IsEnabled = !isBusy;
            BtnCopyActivationRequest.IsEnabled = !isBusy;
            BtnCancel.IsEnabled = !isBusy;
            TxtActivateButtonLabel.Text = isBusy ? "جاري التحقق..." : "تفعيل الآن";
            UpdateActivationCodeState();
        }

        private void RefreshCurrentLicenseStatus()
        {
            LicenseCheckResult currentStatus = LicenseService.GetCurrentStatus();

            TxtCurrentLicenseState.Text = currentStatus.State switch
            {
                LicenseState.Active => "مفعل",
                LicenseState.NotActivated => "غير مفعل",
                LicenseState.Expired => "منتهي",
                LicenseState.Locked => "مقفل",
                _ => "غير معروف"
            };

            TxtCurrentLicenseExpiry.Text = currentStatus.ExpiresAtUtc.HasValue
                ? $"الصلاحية حتى: {currentStatus.ExpiresAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}"
                : currentStatus.Message;

            switch (currentStatus.State)
            {
                case LicenseState.Active:
                    TxtCurrentLicenseState.Foreground = CreateBrush("#166534");
                    TxtCurrentLicenseExpiry.Foreground = CreateBrush("#166534");
                    break;

                case LicenseState.Expired:
                    TxtCurrentLicenseState.Foreground = CreateBrush("#B45309");
                    TxtCurrentLicenseExpiry.Foreground = CreateBrush("#92400E");
                    break;

                case LicenseState.Locked:
                    TxtCurrentLicenseState.Foreground = CreateBrush("#991B1B");
                    TxtCurrentLicenseExpiry.Foreground = CreateBrush("#991B1B");
                    break;

                case LicenseState.NotActivated:
                default:
                    TxtCurrentLicenseState.Foreground = CreateBrush("#1E40AF");
                    TxtCurrentLicenseExpiry.Foreground = CreateBrush("#334155");
                    break;
            }
        }

        private void SetInlineStatus(string title, string message, InlineStatusTone tone)
        {
            TxtStatusTitle.Text = title;
            TxtStatusMessage.Text = message;

            switch (tone)
            {
                case InlineStatusTone.Success:
                    StatusBanner.Background = CreateBrush("#ECFDF5");
                    StatusBanner.BorderBrush = CreateBrush("#86EFAC");
                    TxtStatusTitle.Foreground = CreateBrush("#166534");
                    TxtStatusMessage.Foreground = CreateBrush("#166534");
                    break;

                case InlineStatusTone.Warning:
                    StatusBanner.Background = CreateBrush("#FFF7ED");
                    StatusBanner.BorderBrush = CreateBrush("#FDBA74");
                    TxtStatusTitle.Foreground = CreateBrush("#9A3412");
                    TxtStatusMessage.Foreground = CreateBrush("#7C2D12");
                    break;

                case InlineStatusTone.Error:
                    StatusBanner.Background = CreateBrush("#FEF2F2");
                    StatusBanner.BorderBrush = CreateBrush("#FCA5A5");
                    TxtStatusTitle.Foreground = CreateBrush("#991B1B");
                    TxtStatusMessage.Foreground = CreateBrush("#991B1B");
                    break;

                case InlineStatusTone.Info:
                default:
                    StatusBanner.Background = CreateBrush("#EFF6FF");
                    StatusBanner.BorderBrush = CreateBrush("#BFDBFE");
                    TxtStatusTitle.Foreground = CreateBrush("#1E40AF");
                    TxtStatusMessage.Foreground = CreateBrush("#334155");
                    break;
            }
        }

        private static string NormalizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(token.Length);
            foreach (char character in token)
            {
                if (!char.IsWhiteSpace(character))
                {
                    _ = builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private string BuildActivationRequestMessage()
        {
            string fingerprint = (TxtMachineFingerprint.Text ?? string.Empty).Trim();
            string machineName = Environment.MachineName;
            string nowLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            var builder = new StringBuilder();
            _ = builder.AppendLine("طلب تفعيل - نظام ادارة المتجر");
            _ = builder.AppendLine($"التاريخ: {nowLocal}");
            _ = builder.AppendLine($"اسم الجهاز: {machineName}");
            _ = builder.AppendLine($"بصمة الجهاز: {fingerprint}");
            _ = builder.AppendLine("المدة المطلوبة: 30 يوم");
            _ = builder.Append("الرجاء ارسال كود التفعيل.");
            return builder.ToString();
        }

        private static Brush CreateBrush(string hexColor)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
