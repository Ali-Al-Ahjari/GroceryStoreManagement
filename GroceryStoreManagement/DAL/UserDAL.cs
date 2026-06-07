using Dapper; // استيراد مكتبة Dapper لتسهيل التعامل مع قواعد البيانات وتحويل النتائج لكائنات
using GroceryStoreManagement.Models; // استيراد نماذج البيانات (الكلاسات) مثل كلاس المستخدم User
using System; // استيراد المكتبة الأساسية
using System.Collections.Generic; // استيراد مكتبة القوائم (Lists)
using System.Linq; // استيراد مكتبة معالجة البيانات والاستعلامات
using GroceryStoreManagement.Helpers; // استيراد المساعدات (للوصول لـ DatabaseHelper و ActivityLogDAL)

namespace GroceryStoreManagement.DAL // تحديد اسم المجال كجزء من طبقة الوصول للبيانات (DAL)
{
    // تعريف كلاس ثابت للتعامل مع جدول المستخدمين في قاعدة البيانات
    public static class UserDAL
    {
        // دالة لجلب جميع المستخدمين من قاعدة البيانات
        public static List<User> GetAllUsers()
        {
            try // بداية كتلة التعامل مع الأخطاء
            {
                // فتح اتصال بقاعدة البيانات باستخدام الدالة المساعدة، واستخدام using لإغلاقه تلقائياً عند الانتهاء
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // كتابة استعلام لجلب كل الأعمدة من جدول المستخدمين مع اسم الدور
                string query = @"
                        SELECT u.*, r.RoleName 
                        FROM Users u 
                        LEFT JOIN Roles r ON u.RoleID = r.RoleID 
                        ORDER BY u.FullName";

                // تنفيذ الاستعلام وتحويل النتائج لقائمة من كائنات User باستخدام Dapper
                return [.. connection.Query<User>(query)];
            }
            catch (Exception ex) // التقاط أي خطأ قد يحدث
            {
                Logger.LogError(ex, "خطأ في جلب المستخدمين");
                // رفع الخطأ لمستوى أعلى ليتم عرضه للمستخدم
                throw new Exception($"خطأ في جلب المستخدمين: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// التحقق مما إذا كان هناك أي مستخدمين في النظام
        /// </summary>
        public static bool HasAnyUsers()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                int count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
                return count > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في التحقق من وجود مستخدمين");
                return false;
            }
        }

        // دالة لجلب مستخدم محدد بواسطة معرفه (ID)
        public static User GetUserById(int userId)
        {
            try
            {
                // فتح الاتصال
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // استعلام لجلب المستخدم مع اسم الدور
                string query = @"
                        SELECT u.*, r.RoleName 
                        FROM Users u 
                        LEFT JOIN Roles r ON u.RoleID = r.RoleID 
                        WHERE u.UserID = @UserID";

                // تنفيذ الاستعلام وإرجاع أول نتيجة أو null إذا لم يوجد
                var user = connection.QueryFirstOrDefault<User>(query, new { UserID = userId });
                if (user is null)
                    return null;

                user.Permissions = PermissionDAL.GetUserPermissions(user.UserID);
                return user;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في جلب المستخدم {userId}");
                throw new Exception($"خطأ في جلب المستخدم: {ex.Message}", ex);
            }
        }

        // دالة لجلب مستخدم بواسطة اسم المستخدم (لعملية تسجيل الدخول مثلاً)
        public static User GetUserByUsername(string username)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // البحث عن مستخدم يملك نفس الاسم مع الدور
                string query = @"
                        SELECT u.*, r.RoleName 
                        FROM Users u 
                        LEFT JOIN Roles r ON u.RoleID = r.RoleID 
                        WHERE u.Username = @Username";

                // تمرير الباراميتر @Username بشكل آمن لمنع اختراق SQL Injection
                var user = connection.QueryFirstOrDefault<User>(query, new { Username = username });
                if (user is null)
                    return null;

                user.Permissions = PermissionDAL.GetUserPermissions(user.UserID);
                return user;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في جلب المستخدم {username}");
                throw new Exception($"خطأ في جلب المستخدم: {ex.Message}", ex);
            }
        }

        // دالة تسجيل الدخول الآمنة


        /// <summary>
        /// التحقق من بيانات تسجيل الدخول مع دعم كلمات المرور المشفرة والقديمة
        /// </summary>
        /// <param name="username">اسم المستخدم</param>
        /// <param name="password">كلمة المرور</param>
        /// <returns>كائن المستخدم إذا نجح التحقق، null إذا فشل</returns>
        public static User Login(string username, string password)
        {
            try
            {
                Logger.LogInfo($"محاولة تسجيل دخول للمستخدم: {username}");

                // جلب المستخدم من قاعدة البيانات
                User user = GetUserByUsername(username);

                // التحقق من وجود المستخدم
                if (user == null)
                {
                    Logger.LogWarning($"محاولة تسجيل دخول فاشلة - المستخدم غير موجود: {username}");
                    return null;
                }

                // التحقق من أن المستخدم نشط
                if (!user.IsActive)
                {
                    Logger.LogWarning($"محاولة تسجيل دخول لمستخدم غير نشط: {username}");
                    return null;
                }

                // التحقق من كلمة المرور (يدعم الصيغ الجديدة والقديمة مع إشارة للترقية الصامتة)
                bool isPasswordValid = PasswordHelper.VerifyPassword(password, user.Password, out bool requiresMigration);

                // ترقية كلمة المرور تلقائياً إذا كانت بصيغة قديمة (نص عادي أو SHA1)
                if (isPasswordValid && requiresMigration)
                {
                    Logger.LogInfo($"ترحيل كلمة مرور المستخدم {username} للصيغة الآمنة");
                    MigrateUserPassword(user.UserID, password);
                }

                if (isPasswordValid)
                {
                    Logger.LogInfo($"تسجيل دخول ناجح للمستخدم: {username}");

                    // تسجيل عملية الدخول
                    ActivityLogDAL.AddLog(user.UserID, "تسجيل دخول", $"تم تسجيل دخول المستخدم: {username}");

                    // تحديث تاريخ آخر دخول
                    Helpers.AuditHelper.UpdateLastLogin(user.UserID);

                    return user;
                }
                else
                {
                    Logger.LogWarning($"محاولة تسجيل دخول فاشلة - كلمة مرور خاطئة للمستخدم: {username}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في عملية تسجيل الدخول للمستخدم: {username}");
                throw new Exception($"خطأ في تسجيل الدخول: {ex.Message}", ex);
            }
        }

        // دالة لإضافة مستخدم جديد لقاعدة البيانات
        public static void AddUser(User user)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تشفير كلمة المرور قبل الحفظ
                string hashedPassword = PasswordHelper.HashPassword(user.Password);

                // تعبئة حقول التدقيق
                Helpers.AuditHelper.SetFullAudit(user);

                // تجهيز أمر الإدخال SQL مع تحديد جميع الأعمدة والقيم
                string query = @"
                        INSERT INTO Users (Username, Password, FullName, RoleID, Phone, Email, IsActive, 
                            CanAccessDashboard, CanViewCustomers, CanAddCustomers, CanEditCustomers, CanDeleteCustomers,
                            CanManageProducts, CanManageInvoices, CanViewReports, CanManageSettings, CanBackup,
                            CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                        VALUES (@Username, @Password, @FullName, @RoleID, @Phone, @Email, @IsActive, 
                            @CanAccessDashboard, @CanViewCustomers, @CanAddCustomers, @CanEditCustomers, @CanDeleteCustomers,
                            @CanManageProducts, @CanManageInvoices, @CanViewReports, @CanManageSettings, @CanBackup,
                            @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)";

                // تنفيذ الأمر مع كلمة المرور المشفرة
                _ = connection.Execute(query, new
                {
                    user.Username,
                    Password = hashedPassword, // استخدام كلمة المرور المشفرة
                    user.FullName,
                    user.RoleID,
                    user.Phone,
                    user.Email,
                    user.IsActive,
                    user.CanAccessDashboard,
                    user.CanViewCustomers,
                    user.CanAddCustomers,
                    user.CanEditCustomers,
                    user.CanDeleteCustomers,
                    user.CanManageProducts,
                    user.CanManageInvoices,
                    user.CanViewReports,
                    user.CanManageSettings,
                    user.CanBackup,
                    user.CreatedDate,
                    user.CreatedBy,
                    user.ModifiedDate,
                    user.ModifiedBy
                });

                Logger.LogInfo($"تم إضافة مستخدم جديد: {user.Username}");

                // تسجيل ما حدث في سجل النشاطات (Log)
                ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "إضافة مستخدم", $"تم إضافة المستخدم: {user.Username}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في إضافة المستخدم: {user.Username}");
                throw new Exception($"خطأ في إضافة المستخدم: {ex.Message}", ex);
            }
        }

        // دالة لتحديث بيانات مستخدم موجود
        public static void UpdateUser(User user)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // جلب المستخدم الحالي للتحقق من كلمة المرور
                User currentUser = GetUserById(user.UserID);

                string passwordToSave = user.Password;

                // إذا تم تغيير كلمة المرور، نقوم بتشفيرها
                if (currentUser != null && user.Password != currentUser.Password && !PasswordHelper.IsHashed(user.Password))
                {
                    passwordToSave = PasswordHelper.HashPassword(user.Password);
                    Logger.LogInfo($"تم تحديث كلمة مرور المستخدم: {user.Username}");
                }

                // تعبئة حقول التعديل
                Helpers.AuditHelper.SetModificationAudit(user);

                // تجهيز أمر التحديث SQL لتغيير قيم الحقول للمستخدم المحدد بالـ ID
                string query = @"
                        UPDATE Users 
                        SET Username = @Username, 
                            Password = @Password, 
                            FullName = @FullName, 
                            RoleID = @RoleID,
                            Phone = @Phone, 
                            Email = @Email, 
                            IsActive = @IsActive,
                            CanAccessDashboard = @CanAccessDashboard,
                            CanViewCustomers = @CanViewCustomers,
                            CanAddCustomers = @CanAddCustomers,
                            CanEditCustomers = @CanEditCustomers,
                            CanDeleteCustomers = @CanDeleteCustomers,
                            CanManageProducts = @CanManageProducts,
                            CanManageInvoices = @CanManageInvoices,
                            CanViewReports = @CanViewReports,
                            CanManageSettings = @CanManageSettings,
                            CanBackup = @CanBackup,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE UserID = @UserID"; // شرط التحديث

                // تنفيذ الأمر وتخزين عدد الصفوف المتأثرة
                int rows = connection.Execute(query, new
                {
                    user.UserID,
                    user.Username,
                    Password = passwordToSave,
                    user.FullName,
                    user.RoleID,
                    user.Phone,
                    user.Email,
                    user.IsActive,
                    user.CanAccessDashboard,
                    user.CanViewCustomers,
                    user.CanAddCustomers,
                    user.CanEditCustomers,
                    user.CanDeleteCustomers,
                    user.CanManageProducts,
                    user.CanManageInvoices,
                    user.CanViewReports,
                    user.CanManageSettings,
                    user.CanBackup,
                    user.ModifiedDate,
                    user.ModifiedBy
                });

                // إذا تم التحديث بنجاح (عدد الصفوف المتأثرة أكبر من 0)
                if (rows > 0)
                {
                    // تسجيل العملية في السجل
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "تعديل مستخدم", $"تم تعديل المستخدم: {user.Username}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في تحديث المستخدم: {user.Username}");
                throw new Exception($"خطأ في تحديث المستخدم: {ex.Message}", ex);
            }
        }

        // دالة لحذف مستخدم من النظام
        public static void DeleteUser(int userId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // أمر الحذف
                string query = "DELETE FROM Users WHERE UserID = @UserID";

                // تنفيذ الحذف
                int rows = connection.Execute(query, new { UserID = userId });

                if (rows > 0)
                {
                    Logger.LogInfo($"تم حذف المستخدم رقم: {userId}");
                    // تسجيل العملية
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "حذف مستخدم", $"تم حذف المستخدم رقم: {userId}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في حذف المستخدم: {userId}");
                throw new Exception($"خطأ في حذف المستخدم: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تغيير حالة المستخدم (تفعيل/إيقاف)
        /// </summary>
        public static void SetUserActive(int userId, bool isActive)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "UPDATE Users SET IsActive = @IsActive WHERE UserID = @UserID";
                int rows = connection.Execute(query, new { UserID = userId, IsActive = isActive ? 1 : 0 });

                if (rows > 0)
                {
                    string action = isActive ? "تفعيل مستخدم" : "إيقاف مستخدم";
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, action, $"تم تغيير حالة المستخدم رقم: {userId}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في تغيير حالة المستخدم: {userId}");
                throw new Exception($"خطأ في تغيير حالة المستخدم: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// عدد المدراء النشطين (RoleID = 1)
        /// </summary>
        public static int GetActiveAdminsCount()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "SELECT COUNT(*) FROM Users WHERE RoleID = 1 AND IsActive = 1";
                return connection.ExecuteScalar<int>(query);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في حساب عدد المدراء النشطين");
                return 0;
            }
        }

        // دالة للتحقق مما إذا كان اسم المستخدم مستخدماً من قبل (لتجنب التكرار)
        public static bool IsUsernameExists(string username, int? excludeUserId = null)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // استعلام لعد المستخدمين بنفس الاسم
                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";

                // إذا كنا في حالة تعديل، نستثني المستخدم الحالي من الفحص (لأنه قد يحتفظ بنفس اسمه)
                if (excludeUserId.HasValue)
                {
                    query += " AND UserID != @ExcludeUserId";
                }

                // تنفيذ الاستعلام وإرجاع العدد
                int count = connection.ExecuteScalar<int>(query, new { Username = username, ExcludeUserId = excludeUserId });

                // إرجاع true إذا كان العدد أكبر من 0 (يعني الاسم موجود)
                return count > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في التحقق من اسم المستخدم: {username}");
                throw new Exception($"خطأ في التحقق من اسم المستخدم: {ex.Message}", ex);
            }
        }

        // دوال إدارة كلمات المرور

        /// <summary>
        /// تحديث كلمة مرور مستخدم
        /// </summary>
        /// <param name="userId">معرف المستخدم</param>
        /// <param name="newPassword">كلمة المرور الجديدة (نص عادي)</param>
        public static void UpdatePassword(int userId, string newPassword)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تشفير كلمة المرور الجديدة
                string hashedPassword = PasswordHelper.HashPassword(newPassword);

                string query = "UPDATE Users SET Password = @Password WHERE UserID = @UserID";

                int rows = connection.Execute(query, new { UserID = userId, Password = hashedPassword });

                if (rows > 0)
                {
                    Logger.LogInfo($"تم تحديث كلمة مرور المستخدم رقم: {userId}");
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "تغيير كلمة المرور", $"تم تغيير كلمة مرور المستخدم رقم: {userId}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في تحديث كلمة المرور للمستخدم: {userId}");
                throw new Exception($"خطأ في تحديث كلمة المرور: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ترحيل كلمة مرور مستخدم من النص العادي للصيغة المشفرة
        /// تُستخدم تلقائياً عند أول تسجيل دخول ناجح بكلمة مرور قديمة
        /// </summary>
        private static void MigrateUserPassword(int userId, string plainPassword)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تشفير كلمة المرور
                string hashedPassword = PasswordHelper.HashPassword(plainPassword);

                string query = "UPDATE Users SET Password = @Password WHERE UserID = @UserID";

                _ = connection.Execute(query, new { UserID = userId, Password = hashedPassword });

                Logger.LogInfo($"تم ترحيل كلمة مرور المستخدم رقم {userId} للصيغة المشفرة");
            }
            catch (Exception ex)
            {
                // لا نرفع الخطأ لأن هذه عملية خلفية
                Logger.LogError(ex, $"فشل ترحيل كلمة مرور المستخدم: {userId}");
            }
        }

        /// <summary>
        /// ترحيل جميع كلمات المرور القديمة للصيغة المشفرة
        /// تُستخدم مرة واحدة عند الترقية للنظام الجديد
        /// </summary>
        public static int MigrateAllPasswords()
        {
            int migratedCount = 0;
            try
            {
                Logger.LogInfo("بدء ترحيل كلمات المرور القديمة...");

                var users = GetAllUsers();

                foreach (var user in users)
                {
                    // التحقق مما إذا كانت كلمة المرور غير مشفرة
                    if (!PasswordHelper.IsHashed(user.Password))
                    {
                        MigrateUserPassword(user.UserID, user.Password);
                        migratedCount++;
                    }
                }

                Logger.LogInfo($"تم ترحيل {migratedCount} كلمة مرور بنجاح");
                return migratedCount;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في ترحيل كلمات المرور");
                throw new Exception($"خطأ في ترحيل كلمات المرور: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// التحقق من كلمة المرور الحالية للمستخدم
        /// تُستخدم قبل السماح بتغيير كلمة المرور
        /// </summary>
        public static bool VerifyCurrentPassword(int userId, string password)
        {
            try
            {
                User user = GetUserById(userId);
                if (user == null)
                    return false;

                bool isValid = PasswordHelper.VerifyPassword(password, user.Password, out bool requiresMigration);

                if (isValid && requiresMigration)
                {
                    Logger.LogInfo($"ترحيل كلمة مرور قديمة للمستخدم رقم {userId} أثناء التحقق من كلمة المرور الحالية");
                    MigrateUserPassword(user.UserID, password);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في التحقق من كلمة المرور للمستخدم: {userId}");
                return false;
            }
        }
    }
}


