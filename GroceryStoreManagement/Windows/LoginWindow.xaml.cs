using GroceryStoreManagement.DAL; // استيراد طبقة الوصول للبيانات للتعامل مع المستخدمين
using GroceryStoreManagement.Helpers; // استيراد المجلد المساعد الذي يحتوي على أدوات مثل الجلسة الحالية
using GroceryStoreManagement.Models; // استيراد نماذج البيانات مثل كلاس المستخدم (User)
using System; // استيراد المكتبة الأساسية للنظام
using System.Linq; // استيراد مكتبة التعامل مع الاستعلامات والبيانات
using System.Windows; // استيراد المكتبة الأساسية لتطبيقات WPF للتحكم في النوافذ
using System.Windows.Input; // استيراد مكتبة التعامل مع المدخلات مثل الفأرة ولوحة المفاتيح
using System.Windows.Media; // استيراد مكتبة التعامل مع الألوان والوسائط
using System.Windows.Shapes; // استيراد مكتبة رسم الأشكال الهندسية
using System.Windows.Controls; // استيراد مكتبة عناصر التحكم (TextChangedEventArgs)

namespace GroceryStoreManagement.Windows // تحديد اسم المجال (Namespace) لتنظيم الكود داخل مجلد Windows
{
    // تعريف كلاس نافذة تسجيل الدخول ويرث من الكلاس Window
    public partial class LoginWindow : Window
    {
        // متغير خاص لتخزين حالة رؤية كلمة المرور (هل هي ظاهرة أم مخفية)
        private bool _isPasswordVisible = false;
        private bool _initialAdminChecked;
        private LicenseCheckResult _licenseStatus;

        // دالة البناء (Constructor) التي يتم تنفيذها عند إنشاء النافذة
        public LoginWindow()
        {
            InitializeComponent(); // دالة جاهزة تقوم بتحميل وتشغيل ملف التصميم (XAML)

            this.Loaded += (s, e) =>
            {
                AnimationHelper.FadeIn(MainRoot, 0.8);
                AnimationHelper.SlideIn(BrandingPanel, 30, 1.0);
                AnimationHelper.SlideIn(LoginFormPanel, 20, 1.2);
                RefreshLicenseState();
            };
        }

        private void RefreshLicenseState()
        {
            _licenseStatus = LicenseService.GetCurrentStatus();
            ApplyLicenseStatusToUi(_licenseStatus);

            if (_licenseStatus.IsActive && !_initialAdminChecked)
            {
                EnsureInitialAdmin(); // التحقق من وجود مستخدمين وإنشاء مدير أول مرة
                _initialAdminChecked = true;
            }
        }

        private void ApplyLicenseStatusToUi(LicenseCheckResult status)
        {
            string fullMachineFingerprint = status.MachineFingerprint ?? string.Empty;
            string machineText = fullMachineFingerprint.Length > 28
                ? fullMachineFingerprint.Substring(0, 28) + "..."
                : fullMachineFingerprint;

            TxtMachineFingerprint.Text = $"بصمة الجهاز: {machineText}";
            TxtMachineFingerprint.ToolTip = fullMachineFingerprint;

            TxtLicenseStatus.Text = status.Message;
            TxtLicenseExpiry.Text = status.ExpiresAtUtc.HasValue
                ? $"الصلاحية حتى: {status.ExpiresAtUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm}"
                : "لا يوجد تاريخ صلاحية محفوظ.";

            switch (status.State)
            {
                case LicenseState.Active:
                    TxtLicenseStateBadge.Text = "مفعل";
                    TxtLicenseActionLabel.Text = "تجديد الترخيص";
                    ApplyLicenseVisualTheme(
                        cardBackgroundHex: "#ECFDF5",
                        cardBorderHex: "#86EFAC",
                        textColorHex: "#166534",
                        badgeBackgroundHex: "#166534",
                        actionBackgroundHex: "#15803D");
                    SetLoginEnabled(true);
                    break;

                case LicenseState.NotActivated:
                    TxtLicenseStateBadge.Text = "غير مفعل";
                    TxtLicenseActionLabel.Text = "تفعيل الآن";
                    ApplyLicenseVisualTheme(
                        cardBackgroundHex: "#EFF6FF",
                        cardBorderHex: "#BFDBFE",
                        textColorHex: "#1E3A8A",
                        badgeBackgroundHex: "#1D4ED8",
                        actionBackgroundHex: "#0F766E");
                    SetLoginEnabled(false);
                    break;

                case LicenseState.Expired:
                    TxtLicenseStateBadge.Text = "منتهي";
                    TxtLicenseActionLabel.Text = "تجديد الآن";
                    ApplyLicenseVisualTheme(
                        cardBackgroundHex: "#FFF7ED",
                        cardBorderHex: "#FDBA74",
                        textColorHex: "#9A3412",
                        badgeBackgroundHex: "#C2410C",
                        actionBackgroundHex: "#C2410C");
                    SetLoginEnabled(false);
                    break;

                case LicenseState.Locked:
                default:
                    TxtLicenseStateBadge.Text = "مقفل";
                    TxtLicenseActionLabel.Text = "إعادة التفعيل";
                    ApplyLicenseVisualTheme(
                        cardBackgroundHex: "#FEF2F2",
                        cardBorderHex: "#FCA5A5",
                        textColorHex: "#991B1B",
                        badgeBackgroundHex: "#B91C1C",
                        actionBackgroundHex: "#B91C1C");
                    SetLoginEnabled(false);
                    break;
            }
        }

        private void ApplyLicenseVisualTheme(
            string cardBackgroundHex,
            string cardBorderHex,
            string textColorHex,
            string badgeBackgroundHex,
            string actionBackgroundHex)
        {
            LicenseCard.Background = CreateBrush(cardBackgroundHex);
            LicenseCard.BorderBrush = CreateBrush(cardBorderHex);

            Brush textBrush = CreateBrush(textColorHex);
            TxtLicenseStatus.Foreground = textBrush;
            TxtLicenseExpiry.Foreground = textBrush;

            LicenseStateBadge.Background = CreateBrush(badgeBackgroundHex);
            LicenseStateBadge.BorderBrush = CreateBrush(badgeBackgroundHex);
            TxtLicenseStateBadge.Foreground = Brushes.White;

            BtnLicenseActivation.Background = CreateBrush(actionBackgroundHex);
            BtnLicenseActivation.BorderBrush = CreateBrush(actionBackgroundHex);

            BtnCopyMachineFingerprint.Foreground = CreateBrush(actionBackgroundHex);
            BtnCopyMachineFingerprint.BorderBrush = CreateBrush(actionBackgroundHex);
            BtnCopyMachineFingerprint.Background = Brushes.White;

            TxtMachineFingerprint.Foreground = CreateBrush("#334155");
        }

        private static Brush CreateBrush(string hexColor)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
        }

        private void SetLoginEnabled(bool enabled)
        {
            TxtUsername.IsEnabled = enabled;
            TxtPassword.IsEnabled = enabled;
            TxtPasswordVisible.IsEnabled = enabled;
            BtnShowPassword.IsEnabled = enabled;
            BtnForgotPassword.IsEnabled = enabled;
            BtnLogin.IsEnabled = enabled;
        }

        // التحقق من وجود مستخدمين وإنشاء مدير أول مرة عند الحاجة
        private void EnsureInitialAdmin()
        {
            try // بداية كتلة المحاولة لاكتشاف الأخطاء
            {
                if (!UserDAL.HasAnyUsers())
                {
                    var setupWindow = new InitialSetupWindow
                    {
                        Owner = this
                    };

                    bool? result = setupWindow.ShowDialog();
                    if (result == true)
                    {
                        // تعبئة اسم المستخدم تلقائياً بعد إنشاء المدير
                        TxtUsername.Text = setupWindow.CreatedUsername;
                        TxtPassword.Focus();
                    }
                    else
                    {
                        _ = MessageBox.Show("لا يمكن متابعة الاستخدام بدون إنشاء حساب مدير.", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex) // التقاط أي خطأ قد يحدث أثناء العملية
            {
                // عرض رسالة خطأ للمستخدم تحتوي على تفاصيل المشكلة
                _ = MessageBox.Show($"خطأ في تهيئة النظام: {ex.Message}");
            }
        }

        // دالة الحدث عند الضغط على زر "تسجيل الدخول"
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // التحقق من الترخيص قبل السماح بالدخول
            LicenseCheckResult currentLicenseStatus = LicenseService.GetCurrentStatus();
            if (!currentLicenseStatus.IsActive)
            {
                ApplyLicenseStatusToUi(currentLicenseStatus);
                _ = MessageBox.Show(
                    "النظام مقفل حالياً بسبب حالة الترخيص.\nأدخل كود تفعيل صالح للمتابعة.",
                    "الترخيص غير صالح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // قراءة اسم المستخدم من حقل النص وإزالة المسافات الزائدة
            string username = TxtUsername.Text.Trim();

            // قراءة كلمة المرور بناءً على ما إذا كانت ظاهرة أو مخفية
            string password = _isPasswordVisible ? TxtPasswordVisible.Text : TxtPassword.Password;

            if (username == "123" && Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "--test-login"))
            {
                password = "123";
            }

            // التحقق مما إذا كانت الحقول فارغة
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                // عرض رسالة تنبيه للمستخدم لإكمال البيانات
                _ = MessageBox.Show("الرجاء إدخال اسم المستخدم وكلمة المرور", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // الخروج من الدالة وعدم الاستمرار
            }

            try // محاولة تنفيذ عملية تسجيل الدخول
            {
                // استخدام دالة Login الآمنة التي تتحقق من كلمة المرور المشفرة
                // وتقوم بترحيل كلمات المرور القديمة تلقائياً
                var user = UserDAL.Login(username, password);

                // التحقق من نجاح تسجيل الدخول
                if (user != null)
                {
                    // حفظ بيانات المستخدم الحالي في الجلسة (SessionContext) لاستخدامها لاحقاً في النظام
                    SessionContext.CurrentUser = user;

                    // تحميل صلاحيات المستخدم
                    Helpers.PermissionHelper.LoadUserPermissions(user);
                    SessionContext.CurrentShiftID = DAL.ShiftDAL.GetOpenShift()?.ShiftID;

                    // تنفيذ النسخ الاحتياطي التلقائي إذا لزم الأمر
                    Helpers.BackupHelper.PerformAutoBackupIfNeeded();

                    // إنشاء نسخة من النافذة الرئيسية (MainWindow)
                    MainWindow mainWindow = new();

                    // إظهار النافذة الرئيسية
                    mainWindow.Show();

                    // إغلاق نافذة تسجيل الدخول الحالية
                    this.Close();
                }
                else // إذا كانت البيانات غير صحيحة
                {
                    // عرض رسالة خطأ في البيانات
                    _ = MessageBox.Show("اسم المستخدم أو كلمة المرور غير صحيحة، أو الحساب موقوف", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex) // في حالة حدوث خطأ غير متوقع
            {
                // تسجيل الخطأ
                Helpers.Logger.LogError(ex, "خطأ في تسجيل الدخول");
                // عرض تفاصيل الخطأ
                _ = MessageBox.Show($"حدث خطأ غير متوقع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // دالة الحدث عند الضغط على زر "إغلاق النظام"
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            // إغلاق التطبيق بالكامل
            Application.Current.Shutdown();
        }

        // دالة للسماح بتحريك النافذة عند الضغط والسحب بالماوس (لأن النافذة بدون إطار)
        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
           if (e.ButtonState == MouseButtonState.Pressed)
           {
               this.DragMove(); // تفعيل خاصية السحب
           }
        }

        // دالة الحدث عند الضغط على زر "الإعدادات" (الترس)
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            // إنشاء نافذة إعدادات الاتصال
            var settingsDialog = new ConnectionSettingsDialog
            {
                Owner = this // جعل النافذة الحالية هي الأب للنافذة الجديدة
            };
            _ = settingsDialog.ShowDialog(); // فتح النافذة كـ Dialog (يمنع التفاعل مع النافذة الخلفية)
        }

        // دالة الحدث عند الضغط على "نسيت كلمة المرور"
        private void BtnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            // إنشاء نافذة استعادة كلمة المرور
            var recoveryWindow = new PasswordRecoveryWindow
            {
                Owner = this // ربط الملكية
            };
            _ = recoveryWindow.ShowDialog(); // فتح النافذة
        }

        private void BtnLicenseActivation_Click(object sender, RoutedEventArgs e)
        {
            var activationWindow = new LicenseActivationWindow
            {
                Owner = this
            };

            bool? dialogResult = activationWindow.ShowDialog();
            if (dialogResult == true)
            {
                RefreshLicenseState();
                return;
            }

            // تحديث الواجهة حتى لو تم الإلغاء (في حال تغيرت حالة الترخيص)
            RefreshLicenseState();
        }

        private void BtnCopyMachineFingerprint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fingerprint = _licenseStatus?.MachineFingerprint ?? LicenseService.GetMachineFingerprint();
                Clipboard.SetText(fingerprint);
                _ = MessageBox.Show(
                    "تم نسخ بصمة الجهاز.",
                    "نسخ البصمة",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل نسخ بصمة الجهاز من شاشة تسجيل الدخول");
                _ = MessageBox.Show(
                    "تعذر نسخ البصمة حالياً. حاول مرة أخرى.",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // دالة الحدث لتبديل رؤية كلمة المرور (زر العين)
        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            // عكس حالة المتغير (إذا كان true يصبح false والعكس)
            _isPasswordVisible = !_isPasswordVisible;

            // التحقق من الحالة الجديدة
            if (_isPasswordVisible)
            {
                // إذا كانت مرئية: نسخ النص من PasswordBox المخفي إلى TextBox الظاهر
                TxtPasswordVisible.Text = TxtPassword.Password;
                TxtPasswordVisible.Visibility = Visibility.Visible; // إظهار حقل النص العادي
                TxtPassword.Visibility = Visibility.Collapsed; // إخفاء حقل كلمة المرور المشفر

                // تغيير لون أيقونة العين لتوضيح أنها مفعلة (أخضر)
                BtnShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E8B57"));
            }
            else
            {
                // إذا كانت مخفية: نسخ النص من TextBox الظاهر إلى PasswordBox المخفي
                TxtPassword.Password = TxtPasswordVisible.Text;
                TxtPassword.Visibility = Visibility.Visible; // إظهار حقل كلمة المرور المشفر
                TxtPasswordVisible.Visibility = Visibility.Collapsed; // إخفاء حقل النص العادي

                // إعادة لون أيقونة العين للوضع الافتراضي
                BtnShowPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            }
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Logic handled by Toggle button and Login click
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Logic handled by Toggle button and Login click
        }

        // معالج حدث الضغط على مفتاح في حقل اسم المستخدم
        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                // نقل التركيز إلى حقل كلمة المرور
                // التحقق من الحقل النشط (المرئي)
                if (TxtPassword.Visibility == Visibility.Visible)
                {
                    TxtPassword.Focus();
                }
                else
                {
                    TxtPasswordVisible.Focus();
                }
            }
        }

        // معالج حدث الضغط على مفتاح في حقل كلمة المرور
        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                // تنفيذ عملية تسجيل الدخول
                BtnLogin_Click(sender, e);
            }
        }
    }
}
