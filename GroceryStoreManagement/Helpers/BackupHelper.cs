// =====================================================
// BackupHelper.cs - نظام النسخ الاحتياطي
// يوفر دوال لإنشاء واستعادة النسخ الاحتياطية لقاعدة البيانات
// مع دعم النسخ التلقائي والجدولة
// =====================================================

using System;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// كلاس ثابت لإدارة النسخ الاحتياطي للنظام
    /// يدعم النسخ اليدوي والتلقائي مع إمكانية الاستعادة
    /// </summary>
    public static class BackupHelper
    {
        // ═══════════════════════════════════════════════════════════
        // الإعدادات الافتراضية
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// اسم مجلد النسخ الاحتياطية الافتراضي
        /// </summary>
        private const string DefaultBackupFolder = "Backups";

        /// <summary>
        /// الحد الأقصى لعدد النسخ المحتفظ بها
        /// </summary>
        private const int MaxBackupCount = 30;

        /// <summary>
        /// امتداد ملف النسخة الاحتياطية
        /// </summary>
        private const string BackupExtension = ".backup";

        // ═══════════════════════════════════════════════════════════
        // الخصائص
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// مسار مجلد النسخ الاحتياطية
        /// </summary>
        public static string BackupDirectory
        {
            get
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string backupPath = Path.Combine(appDirectory, DefaultBackupFolder);

                // إنشاء المجلد إذا لم يكن موجوداً
                if (!Directory.Exists(backupPath))
                {
                    _ = Directory.CreateDirectory(backupPath);
                }

                return backupPath;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // دوال النسخ الاحتياطي
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// إنشاء نسخة احتياطية جديدة
        /// </summary>
        /// <param name="description">وصف اختياري للنسخة</param>
        /// <returns>مسار ملف النسخة الاحتياطية</returns>
        public static string CreateBackup(string description = "")
        {
            try
            {
                Logger.LogInfo("بدء إنشاء نسخة احتياطية...");

                // الحصول على مسار قاعدة البيانات
                string dbPath = DatabaseHelper.GetDatabasePath();

                if (!File.Exists(dbPath))
                {
                    throw new FileNotFoundException("ملف قاعدة البيانات غير موجود", dbPath);
                }

                // إنشاء اسم الملف بناءً على التاريخ والوقت
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFileName = $"backup_{timestamp}{BackupExtension}";
                string backupFilePath = Path.Combine(BackupDirectory, backupFileName);

                // نسخ ملف قاعدة البيانات
                File.Copy(dbPath, backupFilePath, true);

                // إنشاء ملف معلومات النسخة
                CreateBackupInfo(backupFilePath, description);

                // تنظيف النسخ القديمة
                CleanupOldBackups();

                Logger.LogInfo($"تم إنشاء نسخة احتياطية: {backupFileName}");

                return backupFilePath;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في إنشاء النسخة الاحتياطية");
                throw new Exception($"فشل في إنشاء النسخة الاحتياطية: {ex.Message}");
            }
        }

        /// <summary>
        /// إنشاء نسخة احتياطية مضغوطة (ZIP)
        /// </summary>
        /// <param name="description">وصف اختياري</param>
        /// <returns>مسار ملف الـ ZIP</returns>
        public static string CreateCompressedBackup(string description = "")
        {
            try
            {
                Logger.LogInfo("بدء إنشاء نسخة احتياطية مضغوطة...");

                // إنشاء النسخة العادية أولاً
                string backupPath = CreateBackup(description);

                // ضغط الملف
                string zipPath = backupPath.Replace(BackupExtension, ".zip");

                // حذف الملف المضغوط إذا كان موجوداً
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                // إنشاء ملف ZIP يحتوي على النسخة الاحتياطية
                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    _ = zipArchive.CreateEntryFromFile(backupPath, Path.GetFileName(backupPath));

                    // إضافة ملف المعلومات إذا وجد
                    string infoFile = backupPath + ".info";
                    if (File.Exists(infoFile))
                    {
                        _ = zipArchive.CreateEntryFromFile(infoFile, Path.GetFileName(infoFile));
                        File.Delete(infoFile);
                    }
                }

                // حذف الملف الأصلي غير المضغوط
                File.Delete(backupPath);

                Logger.LogInfo($"تم إنشاء نسخة احتياطية مضغوطة: {Path.GetFileName(zipPath)}");

                return zipPath;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في إنشاء النسخة الاحتياطية المضغوطة");
                throw new Exception($"فشل في إنشاء النسخة الاحتياطية المضغوطة: {ex.Message}");
            }
        }

        /// <summary>
        /// استعادة قاعدة البيانات من نسخة احتياطية
        /// </summary>
        /// <param name="backupFilePath">مسار ملف النسخة الاحتياطية</param>
        /// <returns>true إذا نجحت الاستعادة</returns>
        public static bool RestoreBackup(string backupFilePath)
        {
            try
            {
                Logger.LogInfo($"بدء استعادة النسخة الاحتياطية: {Path.GetFileName(backupFilePath)}");

                if (!File.Exists(backupFilePath))
                {
                    throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود", backupFilePath);
                }

                string dbPath = DatabaseHelper.GetDatabasePath();

                // إغلاق/تفريغ جميع اتصالات SQLite المفتوحة لتجنب قفل ملف القاعدة أثناء الاستعادة
                ForceCloseSQLiteConnections();

                // إنشاء نسخة احتياطية من الوضع الحالي قبل الاستعادة
                string safetyBackup = Path.Combine(BackupDirectory, $"before_restore_{DateTime.Now:yyyyMMdd_HHmmss}{BackupExtension}");
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, safetyBackup, true);
                    Logger.LogInfo("تم إنشاء نسخة أمان قبل الاستعادة");
                }

                // التحقق من نوع الملف
                if (backupFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // استخراج من ZIP
                    RestoreFromZip(backupFilePath, dbPath);
                }
                else
                {
                    // نسخ مباشر
                    File.Copy(backupFilePath, dbPath, true);
                }

                Logger.LogInfo("تمت استعادة النسخة الاحتياطية بنجاح");

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في استعادة النسخة الاحتياطية");
                throw new Exception($"فشل في استعادة النسخة الاحتياطية: {ex.Message}");
            }
        }

        private static void ForceCloseSQLiteConnections()
        {
            try
            {
                // إجبار تحرير المراجع غير المُدارة قبل تفريغ الـ pools
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                SQLiteConnection.ClearAllPools();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"تعذر إغلاق جميع اتصالات SQLite المفتوحة قبل الاستعادة: {ex.Message}");
            }
        }

        /// <summary>
        /// استعادة من ملف ZIP
        /// </summary>
        private static void RestoreFromZip(string zipPath, string destinationDbPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"backup_extract_{Guid.NewGuid()}");

            try
            {
                // استخراج الملفات
                ZipFile.ExtractToDirectory(zipPath, tempDir);

                // البحث عن ملف قاعدة البيانات
                string[] backupFiles = Directory.GetFiles(tempDir, $"*{BackupExtension}");

                if (backupFiles.Length == 0)
                {
                    throw new Exception("لم يتم العثور على ملف النسخة الاحتياطية داخل الـ ZIP");
                }

                // نسخ الملف
                File.Copy(backupFiles[0], destinationDbPath, true);
            }
            finally
            {
                // تنظيف المجلد المؤقت
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // دوال إدارة النسخ
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// الحصول على قائمة النسخ الاحتياطية المتاحة
        /// </summary>
        /// <returns>مصفوفة من معلومات النسخ</returns>
        public static BackupInfo[] GetAvailableBackups()
        {
            try
            {
                var backupFiles = Directory.GetFiles(BackupDirectory)
                    .Where(f => f.EndsWith(BackupExtension) || f.EndsWith(".zip"))
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToArray();

                var backups = new BackupInfo[backupFiles.Length];

                for (int i = 0; i < backupFiles.Length; i++)
                {
                    var fileInfo = new FileInfo(backupFiles[i]);
                    backups[i] = new BackupInfo
                    {
                        FilePath = backupFiles[i],
                        FileName = fileInfo.Name,
                        CreatedDate = fileInfo.CreationTime,
                        FileSizeBytes = fileInfo.Length,
                        IsCompressed = fileInfo.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
                    };

                    // قراءة الوصف إذا وجد
                    string infoFile = backupFiles[i] + ".info";
                    if (File.Exists(infoFile))
                    {
                        backups[i].Description = File.ReadAllText(infoFile);
                    }
                }

                return backups;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في جلب قائمة النسخ الاحتياطية");
                return [];
            }
        }

        /// <summary>
        /// حذف نسخة احتياطية
        /// </summary>
        /// <param name="backupFilePath">مسار ملف النسخة</param>
        public static void DeleteBackup(string backupFilePath)
        {
            try
            {
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);

                    // حذف ملف المعلومات إذا وجد
                    string infoFile = backupFilePath + ".info";
                    if (File.Exists(infoFile))
                    {
                        File.Delete(infoFile);
                    }

                    Logger.LogInfo($"تم حذف النسخة الاحتياطية: {Path.GetFileName(backupFilePath)}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في حذف النسخة الاحتياطية");
                throw new Exception($"فشل في حذف النسخة الاحتياطية: {ex.Message}");
            }
        }

        /// <summary>
        /// تنظيف النسخ القديمة (الاحتفاظ بآخر N نسخة فقط)
        /// </summary>
        private static void CleanupOldBackups()
        {
            try
            {
                var backups = GetAvailableBackups();

                if (backups.Length > MaxBackupCount)
                {
                    // حذف النسخ الأقدم
                    var oldBackups = backups.Skip(MaxBackupCount).ToArray();

                    foreach (var backup in oldBackups)
                    {
                        DeleteBackup(backup.FilePath);
                    }

                    Logger.LogInfo($"تم حذف {oldBackups.Length} نسخة احتياطية قديمة");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في تنظيف النسخ القديمة");
            }
        }

        /// <summary>
        /// إنشاء ملف معلومات النسخة
        /// </summary>
        private static void CreateBackupInfo(string backupPath, string description)
        {
            try
            {
                string infoPath = backupPath + ".info";
                string info = $"تاريخ الإنشاء: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                info += $"المستخدم: {SessionContext.CurrentUsername}\n";
                info += $"الجهاز: {Environment.MachineName}\n";

                if (!string.IsNullOrEmpty(description))
                {
                    info += $"الوصف: {description}\n";
                }

                File.WriteAllText(infoPath, info);
            }
            catch
            {
                // تجاهل أخطاء إنشاء ملف المعلومات
            }
        }

        // ═══════════════════════════════════════════════════════════
        // النسخ الاحتياطي التلقائي
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// التحقق مما إذا كان يجب إنشاء نسخة احتياطية تلقائية
        /// بناءً على الإعدادات (تفعيل، تكرار)
        /// </summary>
        public static bool ShouldAutoBackup()
        {
            try
            {
                // 1. قراءة الإعدادات
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "settings.ini");
                bool autoBackupEnabled = true; // افتراضي
                int frequency = 0; // 0=Daily, 1=Weekly, 2=Monthly

                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("AutoBackup="))
                        {
                            _ = bool.TryParse(line.Split('=')[1], out autoBackupEnabled);
                        }
                        else if (line.StartsWith("BackupFrequency="))
                        {
                            _ = int.TryParse(line.Split('=')[1], out frequency);
                        }
                    }
                }

                if (!autoBackupEnabled)
                    return false;

                // 2. التحقق من آخر نسخة
                var backups = GetAvailableBackups();

                if (backups.Length == 0)
                    return true;

                var lastBackup = backups.OrderByDescending(b => b.CreatedDate).First();
                double hoursSinceLastBackup = (DateTime.Now - lastBackup.CreatedDate).TotalHours;

                // 3. التحقق حسب التكرار المحدد
                return frequency switch
                {
                    // يومياً
                    0 => hoursSinceLastBackup >= 24,
                    // أسبوعياً
                    1 => hoursSinceLastBackup >= 24 * 7,
                    // شهرياً
                    2 => hoursSinceLastBackup >= 24 * 30,
                    _ => hoursSinceLastBackup >= 24,
                };
            }
            catch
            {
                // في حال حدوث خطأ، نعود للسلوك الافتراضي الآمن (نسخ يومي)
                return true;
            }
        }

        /// <summary>
        /// تنفيذ النسخ الاحتياطي التلقائي إذا لزم الأمر
        /// يُستدعى عند بدء التطبيق
        /// </summary>
        public static void PerformAutoBackupIfNeeded()
        {
            try
            {
                if (ShouldAutoBackup())
                {
                    Logger.LogInfo("بدء النسخ الاحتياطي التلقائي اليومي...");
                    _ = CreateBackup("نسخة احتياطية تلقائية");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل النسخ الاحتياطي التلقائي");
                // لا نرفع الخطأ لأن هذه عملية خلفية
            }
        }

        /// <summary>
        /// تصدير نسخة احتياطية لمسار خارجي
        /// </summary>
        /// <param name="destinationPath">المسار المطلوب</param>
        /// <param name="compress">هل يتم ضغط النسخة</param>
        public static string ExportBackup(string destinationPath, bool compress = true)
        {
            try
            {
                Logger.LogInfo($"تصدير نسخة احتياطية إلى: {destinationPath}");

                string backupPath;

                if (compress)
                {
                    backupPath = CreateCompressedBackup("نسخة مصدرة");
                }
                else
                {
                    backupPath = CreateBackup("نسخة مصدرة");
                }

                // نسخ للمسار الهدف
                string destFile = Path.Combine(destinationPath, Path.GetFileName(backupPath));
                File.Copy(backupPath, destFile, true);

                Logger.LogInfo($"تم تصدير النسخة الاحتياطية بنجاح: {destFile}");

                return destFile;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في تصدير النسخة الاحتياطية");
                throw new Exception($"فشل في تصدير النسخة الاحتياطية: {ex.Message}");
            }
        }

        /// <summary>
        /// الحصول على حجم مجلد النسخ الاحتياطية
        /// </summary>
        public static long GetBackupFolderSize()
        {
            try
            {
                var files = Directory.GetFiles(BackupDirectory);
                return files.Sum(f => new FileInfo(f).Length);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// تحويل حجم الملف لصيغة قابلة للقراءة
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB"];
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// كلاس يحتوي معلومات النسخة الاحتياطية
    /// </summary>
    public class BackupInfo
    {
        /// <summary>المسار الكامل للملف</summary>
        public string FilePath { get; set; }

        /// <summary>اسم الملف فقط</summary>
        public string FileName { get; set; }

        /// <summary>تاريخ الإنشاء</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>حجم الملف بالبايت</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>هل الملف مضغوط</summary>
        public bool IsCompressed { get; set; }

        /// <summary>وصف النسخة</summary>
        public string Description { get; set; }

        /// <summary>الحجم بصيغة قابلة للقراءة</summary>
        public string FileSizeFormatted => BackupHelper.FormatFileSize(FileSizeBytes);

        /// <summary>التاريخ بصيغة قابلة للقراءة</summary>
        public string CreatedDateFormatted => CreatedDate.ToString("yyyy-MM-dd HH:mm");
    }
}
