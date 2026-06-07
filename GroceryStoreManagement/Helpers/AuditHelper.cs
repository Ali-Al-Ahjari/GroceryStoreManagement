// =====================================================
// AuditHelper.cs - مساعد التدقيق
// يوفر دوال لتعبئة حقول التدقيق تلقائياً
// (من أنشأ، متى أنشأ، من عدل، متى عدل)
// =====================================================

using System;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// كلاس ثابت لإدارة حقول التدقيق
    /// يُستخدم لتعبئة حقول CreatedBy, CreatedDate, ModifiedBy, ModifiedDate تلقائياً
    /// </summary>
    public static class AuditHelper
    {
        // ═══════════════════════════════════════════════════════════
        // تحديث حقول الإنشاء (للسجلات الجديدة)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// تعبئة حقول الإنشاء للسجل الجديد
        /// </summary>
        /// <typeparam name="T">نوع الكائن (يجب أن يحتوي على IAuditable)</typeparam>
        /// <param name="entity">الكائن المراد تحديثه</param>
        public static void SetCreationAudit<T>(T entity) where T : class
        {
            var type = entity.GetType();

            // تعيين تاريخ الإنشاء
            var createdDateProp = type.GetProperty("CreatedDate");
            if (createdDateProp != null && createdDateProp.CanWrite)
            {
                createdDateProp.SetValue(entity, DateTime.Now);
            }

            // تعيين من أنشأ
            var createdByProp = type.GetProperty("CreatedBy");
            if (createdByProp != null && createdByProp.CanWrite)
            {
                createdByProp.SetValue(entity, SessionContext.CurrentUserID);
            }
        }

        /// <summary>
        /// تعبئة حقول التعديل للسجل المحدث
        /// </summary>
        /// <typeparam name="T">نوع الكائن</typeparam>
        /// <param name="entity">الكائن المراد تحديثه</param>
        public static void SetModificationAudit<T>(T entity) where T : class
        {
            var type = entity.GetType();

            // تعيين تاريخ التعديل
            var modifiedDateProp = type.GetProperty("ModifiedDate");
            if (modifiedDateProp != null && modifiedDateProp.CanWrite)
            {
                modifiedDateProp.SetValue(entity, DateTime.Now);
            }

            // تعيين من عدل
            var modifiedByProp = type.GetProperty("ModifiedBy");
            if (modifiedByProp != null && modifiedByProp.CanWrite)
            {
                modifiedByProp.SetValue(entity, SessionContext.CurrentUserID);
            }
        }

        /// <summary>
        /// تعبئة حقول الإنشاء والتعديل معاً
        /// تُستخدم للسجلات الجديدة
        /// </summary>
        public static void SetFullAudit<T>(T entity) where T : class
        {
            SetCreationAudit(entity);
            SetModificationAudit(entity);
        }

        // ═══════════════════════════════════════════════════════════
        // الحصول على معلومات التدقيق للعرض
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// الحصول على نص معلومات التدقيق للعرض
        /// </summary>
        /// <param name="createdDate">تاريخ الإنشاء</param>
        /// <param name="createdByName">اسم من أنشأ</param>
        /// <param name="modifiedDate">تاريخ التعديل</param>
        /// <param name="modifiedByName">اسم من عدل</param>
        /// <returns>نص معلومات التدقيق</returns>
        public static string GetAuditInfo(DateTime? createdDate, string createdByName,
                                          DateTime? modifiedDate, string modifiedByName)
        {
            string info = "";

            if (createdDate.HasValue && createdDate.Value != DateTime.MinValue)
            {
                info += $"تاريخ الإنشاء: {createdDate.Value:yyyy-MM-dd HH:mm}";
                if (!string.IsNullOrEmpty(createdByName))
                {
                    info += $" بواسطة: {createdByName}";
                }
                info += "\n";
            }

            if (modifiedDate.HasValue && modifiedDate.Value != DateTime.MinValue)
            {
                info += $"آخر تعديل: {modifiedDate.Value:yyyy-MM-dd HH:mm}";
                if (!string.IsNullOrEmpty(modifiedByName))
                {
                    info += $" بواسطة: {modifiedByName}";
                }
            }

            return info.Trim();
        }

        /// <summary>
        /// الحصول على اسم المستخدم من معرفه
        /// </summary>
        /// <param name="userId">معرف المستخدم</param>
        /// <returns>اسم المستخدم أو "غير معروف" أو "النظام"</returns>
        public static string GetUserName(int? userId)
        {
            if (!userId.HasValue || userId.Value == 0)
                return "النظام";

            try
            {
                var user = DAL.UserDAL.GetUserById(userId.Value);
                return user?.FullName ?? user?.Username ?? "غير معروف";
            }
            catch
            {
                return "غير معروف";
            }
        }

        // ═══════════════════════════════════════════════════════════
        // تحديث آخر تسجيل دخول
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// تحديث تاريخ آخر تسجيل دخول للمستخدم
        /// </summary>
        /// <param name="userId">معرف المستخدم</param>
        public static void UpdateLastLogin(int userId)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                string query = "UPDATE Users SET LastLoginDate = @LastLoginDate WHERE UserID = @UserID";
                using var cmd = new System.Data.SQLite.SQLiteCommand(query, connection);
                _ = cmd.Parameters.AddWithValue("@LastLoginDate", DateTime.Now);
                _ = cmd.Parameters.AddWithValue("@UserID", userId);
                _ = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"فشل تحديث تاريخ آخر دخول للمستخدم {userId}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // بناء جمل SQL للتدقيق
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// الحصول على الأعمدة المطلوبة للإدراج مع التدقيق
        /// </summary>
        /// <returns>قائمة أسماء الأعمدة</returns>
        public static string GetInsertAuditColumns()
        {
            return "CreatedDate, CreatedBy";
        }

        /// <summary>
        /// الحصول على القيم للإدراج مع التدقيق
        /// </summary>
        /// <returns>قائمة المعاملات</returns>
        public static string GetInsertAuditValues()
        {
            return "@CreatedDate, @CreatedBy";
        }

        /// <summary>
        /// الحصول على جملة التحديث للتدقيق
        /// </summary>
        /// <returns>جملة SET للتحديث</returns>
        public static string GetUpdateAuditSet()
        {
            return "ModifiedDate = @ModifiedDate, ModifiedBy = @ModifiedBy";
        }

        /// <summary>
        /// إنشاء كائن معاملات التدقيق للإدراج
        /// </summary>
        public static object GetInsertAuditParams()
        {
            return new
            {
                CreatedDate = DateTime.Now,
                CreatedBy = SessionContext.CurrentUserID
            };
        }

        /// <summary>
        /// إنشاء كائن معاملات التدقيق للتحديث
        /// </summary>
        public static object GetUpdateAuditParams()
        {
            return new
            {
                ModifiedDate = DateTime.Now,
                ModifiedBy = SessionContext.CurrentUserID
            };
        }
    }
}
