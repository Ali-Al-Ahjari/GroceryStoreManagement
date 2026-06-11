// =====================================================
// App.xaml.cs - نقطة بدء التطبيق
// هذا الملف يتحكم في بدء تشغيل التطبيق وتهيئة قاعدة البيانات
// ويتضمن معالجة الأخطاء غير المتوقعة على مستوى التطبيق
// =====================================================

using GroceryStoreManagement.Helpers;  // فئات المساعدة
using System;
using System.Windows;                  // عناصر WPF
using System.Windows.Threading;        // للتعامل مع أخطاء الـ Dispatcher

namespace GroceryStoreManagement
{
    /// <summary>
    /// الفئة الرئيسية للتطبيق
    /// تُنفذ عند بدء تشغيل البرنامج
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// يُستدعى عند بدء تشغيل التطبيق
        /// يقوم بتهيئة قاعدة البيانات وإعداد معالجات الأخطاء
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            // استدعاء الدالة الأساسية
            base.OnStartup(e);

            // ═══════════════════════════════════════════════════════════
            // تهيئة الثقافة واللغة (Localization & Globalization)
            // ═══════════════════════════════════════════════════════════
            var culture = AppSettings.ApplyCulture();

            // تهيئة لغة الواجهة الأساسية لكل الـ FrameworkElements لضمان تنسيق العملات والتواريخ في الـ XAML
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            // ═══════════════════════════════════════════════════════════
            // تهيئة وتطبيق المظهر (Theme Loading)
            // ═══════════════════════════════════════════════════════════
            ThemeManager.LoadTheme();

            // ═══════════════════════════════════════════════════════════
            // إعداد معالجات الأخطاء العامة (Global Exception Handlers)
            // ═══════════════════════════════════════════════════════════

            // معالج أخطاء واجهة المستخدم (UI Thread Exceptions)
            // يلتقط الأخطاء التي تحدث في خيط الواجهة الرئيسي
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // معالج أخطاء المهام غير المتزامنة (Task Exceptions)
            // يلتقط الأخطاء في المهام الخلفية التي لم تتم معالجتها
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // معالج أخطاء النطاق العام (AppDomain Exceptions)
            // آخر خط دفاع - يلتقط أي استثناء لم يتم معالجته
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // ═══════════════════════════════════════════════════════════
            // تسجيل بدء التشغيل
            // ═══════════════════════════════════════════════════════════
            Logger.LogInfo("═══ بدء تشغيل التطبيق ═══");
            Logger.LogInfo($"إصدار النظام: {Environment.OSVersion}");
            Logger.LogInfo($"اسم الجهاز: {Environment.MachineName}");
            Logger.LogInfo($"المستخدم الحالي: {Environment.UserName}");

            try
            {
                // أولاً: تهيئة قاعدة البيانات والجداول باستخدام DatabaseHelper
                // هذا يضمن وجود الجداول الصحيحة قبل محاولة إضافة أي بيانات
                Logger.LogInfo("جاري تهيئة قاعدة البيانات...");
                DatabaseHelper.InitializeDatabase();
                Logger.LogInfo("تم تهيئة قاعدة البيانات بنجاح");

                // تهيئة الصلاحيات بناءً على المفاتيح المعرفة في الكود
                Logger.LogInfo("جاري تهيئة نظام الصلاحيات...");
                DAL.PermissionDAL.InitializePermissions();
                Logger.LogInfo("تم تهيئة الصلاحيات بنجاح");

                // ثانياً: إضافة البيانات التجريبية إذا كانت قاعدة البيانات فارغة
                Logger.LogInfo("جاري التحقق من البيانات التجريبية...");
                SeedData.InitializeDatabase();

                // ثالثاً: التحقق من النسخ الاحتياطي التلقائي
                try
                {
                    BackupHelper.PerformAutoBackupIfNeeded();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"فشل النسخ الاحتياطي التلقائي عند بدء التشغيل: {ex.Message}");
                }

                // إعداد مؤقت للنسخ الاحتياطي الدوري (كل ساعة)
                SetupBackupTimer();
                // إعداد فحص دوري للترخيص أثناء التشغيل
                SetupLicenseTimer();

                // التحقق من وجود بيانات
                if (!DatabaseHelper.HasData())
                {
                    Logger.LogWarning("قاعدة البيانات فارغة - لا توجد بيانات");
                }
                else
                {
                    Logger.LogInfo("تم التحقق من وجود البيانات بنجاح");
                }

                Logger.LogInfo("═══ اكتمل بدء التشغيل بنجاح ═══");
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ قبل عرض الرسالة
                Logger.LogCritical(ex, "فشل في بدء تشغيل التطبيق");

                // عرض رسالة خطأ وإغلاق التطبيق في حالة فشل التهيئة
                _ = MessageBox.Show(
                    $"خطأ في بدء التشغيل:\n{ex.Message}\n\nتم تسجيل التفاصيل في ملف السجل.",
                    "خطأ فادح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// معالج أخطاء واجهة المستخدم
        /// يلتقط الاستثناءات التي تحدث في الخيط الرئيسي للواجهة
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // تسجيل الخطأ
            Logger.LogError(e.Exception, "خطأ غير معالج في واجهة المستخدم");

            // بعض أنماط WPF قد ترمي هذا الخطأ عند إعادة تقييم Effect أثناء انتقال التركيز.
            // نتعامل معه كخطأ قابل للتعافي لمنع إغلاق التطبيق.
            if (IsKnownEffectUnsetValueException(e.Exception))
            {
                Logger.LogWarning("تم تجاهل خطأ Effect/UnsetValue القابل للتعافي للحفاظ على استمرارية التطبيق.");
                e.Handled = true;
                return;
            }

            // عرض رسالة للمستخدم
            string message = $"حدث خطأ غير متوقع:\n{e.Exception.Message}";

            // إضافة رسالة الخطأ الداخلي إذا وجدت
            if (e.Exception.InnerException != null)
            {
                message += $"\n\nتفاصيل إضافية:\n{e.Exception.InnerException.Message}";
            }

            message += "\n\nتم تسجيل الخطأ. هل تريد متابعة استخدام البرنامج؟";

            MessageBoxResult result = MessageBox.Show(
                message,
                "خطأ في التطبيق",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            // إذا اختار المستخدم نعم، نمنع إغلاق التطبيق
            e.Handled = (result == MessageBoxResult.Yes);

            // إذا اختار لا، نسجل إغلاق التطبيق
            if (!e.Handled)
            {
                Logger.LogWarning("المستخدم اختار إغلاق التطبيق بعد حدوث خطأ");
            }
        }

        private static bool IsKnownEffectUnsetValueException(Exception ex)
        {
            Exception current = ex;

            while (current != null)
            {
                if (current is InvalidOperationException)
                {
                    string message = current.Message ?? string.Empty;
                    bool isEffectRelated = message.Contains("property 'Effect'", StringComparison.OrdinalIgnoreCase) ||
                                           message.Contains("Property 'Effect'", StringComparison.OrdinalIgnoreCase);

                    bool isInvalidValuePattern = message.Contains("DependencyProperty.UnsetValue", StringComparison.OrdinalIgnoreCase) ||
                                                 message.Contains("UnsetValue", StringComparison.OrdinalIgnoreCase) ||
                                                 message.Contains("not a valid value", StringComparison.OrdinalIgnoreCase);

                    if (isEffectRelated && isInvalidValuePattern)
                    {
                        return true;
                    }
                }

                current = current.InnerException;
            }

            return false;
        }

        /// <summary>
        /// معالج أخطاء المهام غير المتزامنة
        /// يلتقط الاستثناءات من المهام (Tasks) التي لم يتم انتظارها
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            // تسجيل الخطأ
            Logger.LogError(e.Exception, "خطأ غير معالج في مهمة خلفية");

            // تحديد الخطأ كمعالج لمنع إغلاق التطبيق
            e.SetObserved();
        }

        /// <summary>
        /// معالج أخطاء النطاق العام
        /// آخر خط دفاع - يلتقط أي استثناء لم تتم معالجته
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                // تسجيل الخطأ الحرج
                Logger.LogCritical(ex, "خطأ حرج غير معالج - التطبيق سيغلق");
            }
            else
            {
                // تسجيل كائن غير معروف
                Logger.LogCritical(new Exception("كائن استثناء غير معروف"),
                    $"نوع الكائن: {e.ExceptionObject?.GetType()?.Name ?? "null"}");
            }

            // عرض رسالة للمستخدم
            _ = MessageBox.Show(
                "حدث خطأ حرج وسيتم إغلاق التطبيق.\nتم حفظ تفاصيل الخطأ في ملف السجل.",
                "خطأ حرج",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private DispatcherTimer _backupTimer;
        private DispatcherTimer _licenseTimer;
        private bool _isShuttingDownForLicense;

        private void SetupBackupTimer()
        {
            _backupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(1) // فحص كل ساعة
            };
            _backupTimer.Tick += (s, args) =>
            {
                // تنفيذ النسخ الاحتياطي في خلفية لعدم تجميد الواجهة
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    BackupHelper.PerformAutoBackupIfNeeded();
                });
            };
            _backupTimer.Start();
            Logger.LogInfo("تم بدء مؤقت النسخ الاحتياطي التلقائي");
        }

        private void SetupLicenseTimer()
        {
            _licenseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };

            _licenseTimer.Tick += (s, args) =>
            {
                if (_isShuttingDownForLicense || !SessionContext.IsLoggedIn)
                {
                    return;
                }

                try
                {
                    LicenseCheckResult status = LicenseService.GetCurrentStatus();
                    if (status.IsActive)
                    {
                        return;
                    }

                    _isShuttingDownForLicense = true;
                    Logger.LogWarning($"إغلاق التطبيق بسبب حالة الترخيص: {status.State} - {status.Message}");

                    _ = MessageBox.Show(
                        $"تم إيقاف النظام لأن الترخيص لم يعد صالحاً.\n{status.Message}",
                        "انتهاء/قفل الترخيص",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    Shutdown();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "فشل فحص الترخيص الدوري");
                }
            };

            _licenseTimer.Start();
            Logger.LogInfo("تم بدء مؤقت فحص الترخيص");
        }

        /// <summary>
        /// يُستدعى عند إغلاق التطبيق
        /// نستخدمه لتسجيل إغلاق التطبيق
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            Logger.LogInfo("═══ إغلاق التطبيق ═══");
            base.OnExit(e);
        }
    }
}
