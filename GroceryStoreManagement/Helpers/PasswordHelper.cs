// =====================================================
// PasswordHelper.cs - مساعد تشفير كلمات المرور
// يوفر دوال آمنة لتشفير والتحقق من كلمات المرور
// باستخدام خوارزمية PBKDF2 مع Salt عشوائي
// =====================================================

using System;
using System.Security.Cryptography;
using System.Linq;
using System.Text;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// كلاس ثابت لإدارة تشفير كلمات المرور
    /// يستخدم خوارزمية PBKDF2 التي تعتبر من أكثر الخوارزميات أماناً
    /// </summary>
    public static class PasswordHelper
    {
        // ═══════════════════════════════════════════════════════════
        // ثوابت التشفير
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// حجم الـ Salt بالبايت (16 بايت = 128 بت)
        /// الـ Salt هو قيمة عشوائية تُضاف لكلمة المرور قبل التشفير
        /// لمنع هجمات Rainbow Table
        /// </summary>
        private const int SaltSize = 16;

        /// <summary>
        /// حجم الـ Hash الناتج بالبايت (32 بايت = 256 بت)
        /// </summary>
        private const int HashSize = 32;

        /// <summary>
        /// عدد التكرارات في خوارزمية PBKDF2
        /// كلما زاد العدد، زاد الأمان لكن بطء الأداء
        /// 10000 هو الحد الأدنى الموصى به
        /// </summary>
        private const int Iterations = 10000;

        // ═══════════════════════════════════════════════════════════
        // الدوال الرئيسية
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// تشفير كلمة المرور وإرجاع النص المشفر
        /// </summary>
        /// <param name="password">كلمة المرور الأصلية</param>
        /// <returns>النص المشفر بصيغة Base64 (يحتوي Salt + Hash)</returns>
        /// <example>
        /// string hash = PasswordHelper.HashPassword("MySecretPassword123");
        /// // النتيجة مثل: "iterations:salt:hash" بصيغة Base64
        /// </example>
        public static string HashPassword(string password)
        {
            // التحقق من صحة المدخلات
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), "كلمة المرور لا يمكن أن تكون فارغة");
            }

            try
            {
                // توليد Salt عشوائي
                byte[] salt = GenerateSalt();

                // تشفير كلمة المرور باستخدام PBKDF2
                byte[] hash = ComputeHash(password, salt, Iterations);

                // دمج البيانات: iterations + salt + hash
                // نحفظ عدد التكرارات للسماح بتغييره مستقبلاً
                string result = $"{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";

                Logger.LogDebug($"تم تشفير كلمة مرور جديدة");

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في تشفير كلمة المرور");
                throw new Exception("فشل في تشفير كلمة المرور", ex);
            }
        }

        /// <summary>
        /// التحقق من صحة كلمة المرور مقابل الهاش المخزن
        /// </summary>
        /// <param name="password">كلمة المرور المدخلة للتحقق</param>
        /// <param name="hashedPassword">الهاش المخزن في قاعدة البيانات</param>
        /// <returns>true إذا تطابقت كلمة المرور، false إذا لم تتطابق</returns>
        /// <example>
        /// bool isValid = PasswordHelper.VerifyPassword("MySecretPassword123", storedHash);
        /// </example>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return VerifyPassword(password, hashedPassword, out _);
        }

        /// <summary>
        /// التحقق من كلمة المرور مع تحديد ما إذا كانت بحاجة إلى ترقية صامتة
        /// </summary>
        /// <param name="password">كلمة المرور المدخلة</param>
        /// <param name="hashedPassword">القيمة المخزنة في قاعدة البيانات</param>
        /// <param name="requiresMigration">true إذا كانت كلمة المرور بصيغة قديمة (نص عادي أو SHA1)</param>
        public static bool VerifyPassword(string password, string hashedPassword, out bool requiresMigration)
        {
            requiresMigration = false;

            // التحقق من صحة المدخلات
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            try
            {
                // تحليل الهاش المخزن
                string[] parts = hashedPassword.Split(':');

                // التحقق من الصيغة الصحيحة
                if (parts.Length != 3)
                {
                    // قد تكون كلمة مرور قديمة (نص عادي) - للتوافق مع النظام القديم
                    bool plainTextMatch = SlowEqualsString(password, hashedPassword);
                    requiresMigration = plainTextMatch;
                    return plainTextMatch;
                }

                // استخراج المكونات
                if (!int.TryParse(parts[0], out int iterations) || iterations <= 0)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] storedHash = Convert.FromBase64String(parts[2]);

                // محاولة التحقق بـ SHA256 (الخوارزمية الجديدة) أولاً
                byte[] computedHash = ComputeHash(password, salt, iterations);
                bool isValid = SlowEquals(storedHash, computedHash);

                // إذا فشلت، محاولة التحقق بـ SHA1 (للتوافق مع كلمات المرور القديمة)
                if (!isValid)
                {
                    byte[] legacyHash = ComputeHashLegacySHA1(password, salt, iterations);
                    isValid = SlowEquals(storedHash, legacyHash);

                    if (isValid)
                    {
                        requiresMigration = true;
                        Logger.LogInfo("تم اكتشاف كلمة مرور بتشفير SHA1 قديم - ستتم الترقية عند تسجيل الدخول");
                    }
                }

                if (!isValid)
                {
                    Logger.LogWarning("محاولة تسجيل دخول بكلمة مرور خاطئة");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في التحقق من كلمة المرور");
                return false;
            }
        }

        /// <summary>
        /// التحقق مما إذا كانت كلمة المرور مشفرة بالصيغة الجديدة
        /// </summary>
        /// <param name="password">النص المراد فحصه</param>
        /// <returns>true إذا كانت مشفرة، false إذا كانت نص عادي</returns>
        public static bool IsHashed(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            string[] parts = password.Split(':');

            // الصيغة المشفرة: "iterations:salt:hash"
            if (parts.Length != 3)
                return false;

            // التحقق من أن الجزء الأول رقم
            if (int.TryParse(parts[0], result: out _))
                // التحقق من أن الأجزاء الأخرى Base64 صالحة
                try
                {
                    _ = Convert.FromBase64String(parts[1]);
                    _ = Convert.FromBase64String(parts[2]);
                    return true;
                }
                catch
                {
                    return false;
                }

            return false;
        }

        /// <summary>
        /// ترحيل كلمة مرور قديمة (نص عادي) إلى الصيغة المشفرة
        /// </summary>
        /// <param name="plainPassword">كلمة المرور بالنص العادي</param>
        /// <returns>كلمة المرور المشفرة</returns>
        public static string MigrateLegacyPassword(string plainPassword)
        {
            Logger.LogInfo("ترحيل كلمة مرور من النظام القديم");
            return HashPassword(plainPassword);
        }

        // ═══════════════════════════════════════════════════════════
        // الدوال المساعدة الخاصة
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// توليد Salt عشوائي آمن
        /// </summary>
        private static byte[] GenerateSalt()
        {
            return RandomNumberGenerator.GetBytes(SaltSize);
        }

        /// <summary>
        /// حساب الهاش باستخدام PBKDF2 مع SHA256 (الخوارزمية الجديدة الآمنة)
        /// </summary>
        private static byte[] ComputeHash(string password, byte[] salt, int iterations)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);
        }

        /// <summary>
        /// حساب الهاش باستخدام PBKDF2 مع SHA1 (للتوافق مع كلمات المرور القديمة فقط)
        /// </summary>
        private static byte[] ComputeHashLegacySHA1(string password, byte[] salt, int iterations)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA1, HashSize);
        }

        /// <summary>
        /// مقارنة آمنة ثابتة الوقت باستخدام CryptographicOperations
        /// تمنع هجمات التوقيت (Timing Attacks)
        /// </summary>
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        private static bool SlowEqualsString(string a, string b)
        {
            byte[] aBytes = Encoding.UTF8.GetBytes(a);
            byte[] bBytes = Encoding.UTF8.GetBytes(b);
            return SlowEquals(aBytes, bBytes);
        }

        // ═══════════════════════════════════════════════════════════
        // دوال التحقق من قوة كلمة المرور
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// التحقق من قوة كلمة المرور
        /// </summary>
        /// <param name="password">كلمة المرور</param>
        /// <param name="errorMessage">رسالة الخطأ إذا كانت ضعيفة</param>
        /// <returns>true إذا كانت قوية بما فيه الكفاية</returns>
        public static bool IsStrongPassword(string password, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "كلمة المرور لا يمكن أن تكون فارغة";
                return false;
            }

            if (password.Length < 8)
            {
                errorMessage = "كلمة المرور يجب أن تكون 8 أحرف على الأقل";
                return false;
            }

            bool hasLetter = password.Any(char.IsLetter);
            bool hasDigit = password.Any(char.IsDigit);

            if (!hasLetter || !hasDigit)
            {
                errorMessage = "كلمة المرور يجب أن تحتوي على أحرف وأرقام";
                return false;
            }

            return true;
        }

        /// <summary>
        /// توليد كلمة مرور عشوائية
        /// </summary>
        /// <param name="length">طول كلمة المرور المطلوبة</param>
        /// <returns>كلمة مرور عشوائية</returns>
        public static string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";

            byte[] randomBytes = RandomNumberGenerator.GetBytes(length);

            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[randomBytes[i] % validChars.Length];
            }

            return new string(chars);
        }
    }
}
