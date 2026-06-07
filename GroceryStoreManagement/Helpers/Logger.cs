// =====================================================
// Logger.cs - نظام تسجيل الأخطاء والأحداث
// يقوم بتسجيل جميع الأخطاء والتحذيرات والمعلومات في ملف نصي
// =====================================================

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// مستوى السجل - يحدد نوع الرسالة المسجلة
    /// </summary>
    public enum LogLevel
    {
        /// <summary>معلومات عامة - للتتبع العادي</summary>
        Info,
        /// <summary>تحذير - شيء غير متوقع لكن ليس خطأ</summary>
        Warning,
        /// <summary>خطأ - استثناء أو مشكلة تحتاج انتباه</summary>
        Error,
        /// <summary>خطأ حرج - يؤثر على استقرار النظام</summary>
        Critical,
        /// <summary>تصحيح - للمطورين فقط</summary>
        Debug
    }

    /// <summary>
    /// كلاس ثابت لتسجيل الأخطاء والأحداث
    /// يحفظ السجلات في مجلد Logs بجانب التطبيق
    /// </summary>
    public static class Logger
    {
        // مسار مجلد السجلات
        private static readonly string LogDirectory;
        private static readonly object v = new();

        // قفل للكتابة المتزامنة (Thread Safety)
        private static readonly object _lockObject = v;
        private static bool _sessionHeaderWritten;

        // الحد الأقصى لحجم ملف السجل بالبايت (5 ميجابايت)
        private const long MaxLogFileSize = 5 * 1024 * 1024;

        // عدد ملفات السجل القديمة المحتفظ بها
        private const int MaxLogFiles = 10;

        /// <summary>
        /// المُنشئ الثابت - يتم تنفيذه مرة واحدة عند أول استخدام للكلاس
        /// </summary>
        static Logger()
        {
            try
            {
                // تحديد مسار مجلد السجلات بجانب ملف التطبيق
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                LogDirectory = Path.Combine(appDirectory, "Logs");

                // إنشاء المجلد إذا لم يكن موجوداً
                if (!Directory.Exists(LogDirectory))
                {
                    _ = Directory.CreateDirectory(LogDirectory);
                }
            }
            catch
            {
                // في حالة فشل إنشاء المجلد، استخدم مجلد المستخدم المؤقت
                LogDirectory = Path.Combine(Path.GetTempPath(), "GroceryStoreLogs");
                _ = Directory.CreateDirectory(LogDirectory);
            }

            WriteSessionHeader();
        }

        /// <summary>
        /// الحصول على مسار ملف السجل الحالي (حسب التاريخ)
        /// </summary>
        private static string GetCurrentLogFilePath()
        {
            string fileName = $"log_{DateTime.Now:yyyy-MM-dd}.txt";
            return Path.Combine(LogDirectory, fileName);
        }

        /// <summary>
        /// تسجيل رسالة معلومات
        /// </summary>
        /// <param name="message">نص الرسالة</param>
        /// <param name="memberName">اسم الدالة (يتم تعبئته تلقائياً)</param>
        /// <param name="filePath">مسار الملف (يتم تعبئته تلقائياً)</param>
        /// <param name="lineNumber">رقم السطر (يتم تعبئته تلقائياً)</param>
        public static void LogInfo(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(LogLevel.Info, message, null, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// تسجيل رسالة تحذير
        /// </summary>
        public static void LogWarning(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(LogLevel.Warning, message, null, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// تسجيل خطأ مع تفاصيل الاستثناء
        /// </summary>
        /// <param name="ex">الاستثناء المراد تسجيله</param>
        /// <param name="context">سياق إضافي يوضح ماذا كان يحدث</param>
        public static void LogError(
            Exception ex,
            string context = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(LogLevel.Error, context, ex, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// تسجيل خطأ حرج
        /// </summary>
        public static void LogCritical(
            Exception ex,
            string context = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(LogLevel.Critical, context, ex, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// تسجيل رسالة تصحيح (للمطورين)
        /// </summary>
        public static void LogDebug(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
#if DEBUG
            WriteLog(LogLevel.Debug, message, null, memberName, filePath, lineNumber);
#endif
        }

        /// <summary>
        /// الدالة الرئيسية لكتابة السجل
        /// </summary>
        private static void WriteLog(
            LogLevel level,
            string message,
            Exception ex,
            string memberName,
            string filePath,
            int lineNumber)
        {
            try
            {
                // بناء نص السجل
                StringBuilder sb = new();
                _ = sb.AppendLine("═══════════════════════════════════════════════════════════");
                _ = sb.AppendLine($"📅 الوقت: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                _ = sb.AppendLine($"📊 المستوى: {GetLevelIcon(level)} {level}");

                // معلومات المستخدم الحالي
                string currentUser = SessionContext.CurrentUsername;
                if (!string.IsNullOrEmpty(currentUser) && currentUser != "System")
                {
                    _ = sb.AppendLine($"👤 المستخدم: {currentUser}");
                }

                // معلومات الموقع في الكود
                string fileName = Path.GetFileName(filePath);
                _ = sb.AppendLine($"📍 الموقع: {fileName} -> {memberName}() [سطر {lineNumber}]");

                // الرسالة
                if (!string.IsNullOrEmpty(message))
                {
                    _ = sb.AppendLine($"💬 الرسالة: {message}");
                }

                // تفاصيل الاستثناء
                if (ex != null)
                {
                    _ = sb.AppendLine($"❌ نوع الخطأ: {ex.GetType().FullName}");
                    _ = sb.AppendLine($"📝 وصف الخطأ: {ex.Message}");

                    if (ex.InnerException != null)
                    {
                        _ = sb.AppendLine($"🔗 الخطأ الداخلي: {ex.InnerException.Message}");
                    }

                    _ = sb.AppendLine($"📚 تتبع المكدس:");
                    _ = sb.AppendLine(ex.StackTrace);
                }

                _ = sb.AppendLine();

                // كتابة السجل مع قفل للأمان
                lock (_lockObject)
                {
                    string logFilePath = GetCurrentLogFilePath();

                    // التحقق من حجم الملف وتدويره إذا لزم
                    RotateLogFileIfNeeded(logFilePath);

                    // كتابة السجل
                    File.AppendAllText(logFilePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // تجاهل أخطاء التسجيل لمنع حلقة لا نهائية
            }
        }

        private static void WriteSessionHeader()
        {
            if (_sessionHeaderWritten)
            {
                return;
            }

            try
            {
                lock (_lockObject)
                {
                    if (_sessionHeaderWritten)
                    {
                        return;
                    }

                    string logFilePath = GetCurrentLogFilePath();
                    RotateLogFileIfNeeded(logFilePath);

                    string header = $"=== Session Start {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Machine: {Environment.MachineName} | OS: {Environment.OSVersion} ==={Environment.NewLine}";
                    File.AppendAllText(logFilePath, header, Encoding.UTF8);
                    _sessionHeaderWritten = true;
                }
            }
            catch
            {
                // تجاهل أخطاء كتابة رأس الجلسة
            }
        }

        /// <summary>
        /// الحصول على أيقونة المستوى
        /// </summary>
        private static string GetLevelIcon(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => "ℹ️",
                LogLevel.Warning => "⚠️",
                LogLevel.Error => "❌",
                LogLevel.Critical => "🔥",
                LogLevel.Debug => "🔧",
                _ => "📝",
            };
        }

        /// <summary>
        /// تدوير ملف السجل إذا تجاوز الحد الأقصى
        /// </summary>
        private static void RotateLogFileIfNeeded(string currentLogPath)
        {
            try
            {
                if (!File.Exists(currentLogPath))
                    return;

                FileInfo fileInfo = new(currentLogPath);

                if (fileInfo.Length > MaxLogFileSize)
                {
                    // إعادة تسمية الملف الحالي
                    string archivePath = currentLogPath.Replace(".txt", $"_{DateTime.Now:HHmmss}.txt");
                    File.Move(currentLogPath, archivePath);

                    // حذف الملفات القديمة إذا تجاوزت الحد
                    CleanupOldLogFiles();
                }
            }
            catch
            {
                // تجاهل أخطاء التدوير
            }
        }

        /// <summary>
        /// حذف ملفات السجل القديمة
        /// </summary>
        private static void CleanupOldLogFiles()
        {
            try
            {
                string[] logFiles = Directory.GetFiles(LogDirectory, "log_*.txt");

                if (logFiles.Length > MaxLogFiles)
                {
                    // ترتيب حسب تاريخ الإنشاء
                    Array.Sort(logFiles, (a, b) =>
                        File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));

                    // حذف الأقدم
                    int filesToDelete = logFiles.Length - MaxLogFiles;
                    for (int i = 0; i < filesToDelete; i++)
                    {
                        File.Delete(logFiles[i]);
                    }
                }
            }
            catch
            {
                // تجاهل أخطاء الحذف
            }
        }

        /// <summary>
        /// قراءة محتوى ملف السجل الحالي
        /// </summary>
        public static string GetCurrentLogContent()
        {
            try
            {
                string logFilePath = GetCurrentLogFilePath();
                if (File.Exists(logFilePath))
                {
                    return File.ReadAllText(logFilePath, Encoding.UTF8);
                }
                return "لا توجد سجلات لهذا اليوم";
            }
            catch (Exception ex)
            {
                return $"خطأ في قراءة السجل: {ex.Message}";
            }
        }

        /// <summary>
        /// الحصول على قائمة ملفات السجل المتاحة
        /// </summary>
        public static string[] GetLogFiles()
        {
            try
            {
                if (Directory.Exists(LogDirectory))
                {
                    return Directory.GetFiles(LogDirectory, "log_*.txt");
                }
                return [];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// قراءة محتوى ملف سجل محدد
        /// </summary>
        public static string GetLogContent(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath, Encoding.UTF8);
                }
                return "الملف غير موجود";
            }
            catch (Exception ex)
            {
                return $"خطأ في قراءة السجل: {ex.Message}";
            }
        }

        /// <summary>
        /// مسح جميع ملفات السجل
        /// </summary>
        public static void ClearAllLogs()
        {
            try
            {
                string[] logFiles = GetLogFiles();
                foreach (string file in logFiles)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // تجاهل الأخطاء
            }
        }

        /// <summary>
        /// الحصول على مسار مجلد السجلات
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }
    }
}
