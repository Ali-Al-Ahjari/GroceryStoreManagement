using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Reflection;
using Dapper;
using System.Linq;

namespace GroceryStoreManagement.DAL
{
    public static class PermissionDAL
    {
        // تحميل جميع الصلاحيات المعرفة في النظام
        public static void InitializePermissions()
        {
            try
            {
                var permissions = new List<Permission>();
                var permissionMetadata = PermissionHelper.GetAllSystemPermissions()
                    .GroupBy(p => p.PermissionKey)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                // استخدام الانعكاس للحصول على كل الثوابت في PermissionKeys
                FieldInfo[] fields = typeof(PermissionKeys).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                var allKeys = new List<string>();

                using var conn = DatabaseHelper.GetConnection();
                using var transaction = conn.BeginTransaction();
                foreach (FieldInfo field in fields)
                {
                    if (field.IsLiteral && !field.IsInitOnly)
                    {
                        string key = (string)field.GetValue(null);
                        bool hasMetadata = permissionMetadata.TryGetValue(key, out Permission permissionInfo);
                        string name = hasMetadata ? permissionInfo.DisplayName : GetDisplayName(key);
                        string category = hasMetadata ? permissionInfo.Category : GetCategory(key);
                        string description = hasMetadata
                            ? (string.IsNullOrWhiteSpace(permissionInfo.Description) ? name : permissionInfo.Description)
                            : name;
                        allKeys.Add(key);

                        string insertQuery = @"
                                    INSERT OR IGNORE INTO Permissions (PermissionKey, DisplayName, Description, Category)
                                    VALUES (@Key, @Name, @Desc, @Cat)";

                        using var cmd = new SQLiteCommand(insertQuery, conn);
                        _ = cmd.Parameters.AddWithValue("@Key", key);
                        _ = cmd.Parameters.AddWithValue("@Name", name);
                        _ = cmd.Parameters.AddWithValue("@Desc", description);
                        _ = cmd.Parameters.AddWithValue("@Cat", category);
                        _ = cmd.ExecuteNonQuery();

                        // مزامنة أسماء/تصنيفات الصلاحيات حتى يتم تعريب السجلات القديمة الموجودة مسبقًا.
                        string updateQuery = @"
                                    UPDATE Permissions
                                    SET DisplayName = @Name,
                                        Description = @Desc,
                                        Category = @Cat
                                    WHERE PermissionKey = @Key";

                        using var updateCmd = new SQLiteCommand(updateQuery, conn);
                        _ = updateCmd.Parameters.AddWithValue("@Key", key);
                        _ = updateCmd.Parameters.AddWithValue("@Name", name);
                        _ = updateCmd.Parameters.AddWithValue("@Desc", description);
                        _ = updateCmd.Parameters.AddWithValue("@Cat", category);
                        _ = updateCmd.ExecuteNonQuery();
                    }
                }

                // منح كافة الصلاحيات لدور المدير (Admin - RoleID=1)
                if (allKeys.Count > 0)
                {
                    // التحقق مما إذا كان للمدير صلاحيات بالفعل
                    string checkAdminParams = "SELECT COUNT(*) FROM RolePermissions WHERE RoleID = 1";
                    using var cmd = new SQLiteCommand(checkAdminParams, conn);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count == 0)
                    {
                        string insertRolePerm = "INSERT INTO RolePermissions (RoleID, PermissionKey) VALUES (1, @Key)";
                        using var insertCmd = new SQLiteCommand(insertRolePerm, conn);
                        var keyParam = insertCmd.Parameters.Add("@Key", DbType.String);
                        foreach (var key in allKeys)
                        {
                            keyParam.Value = key;
                            _ = insertCmd.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في تهيئة الصلاحيات");
            }
        }

        /// <summary>
        /// الحصول على جميع الصلاحيات المعرفة في جدول Permissions
        /// </summary>
        public static List<Permission> GetAllSystemPermissions()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                return [.. conn.Query<Permission>("SELECT * FROM Permissions ORDER BY Category, DisplayName")];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في جلب قائمة الصلاحيات");
                return [];
            }
        }

        /// <summary>
        /// إضافة دور جديد
        /// </summary>
        public static int AddRole(Role role)
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                string query = @"INSERT INTO Roles (RoleName, Description, IsSystemRole) 
                                     VALUES (@RoleName, @Description, 0);
                                     SELECT last_insert_rowid();";
                return conn.ExecuteScalar<int>(query, role);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في إضافة الدور");
                throw;
            }
        }

        /// <summary>
        /// تحديث بيانات الدور
        /// </summary>
        public static void UpdateRole(Role role)
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                string query = "UPDATE Roles SET RoleName = @RoleName, Description = @Description WHERE RoleID = @RoleID";
                _ = conn.Execute(query, role);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في تحديث الدور");
                throw;
            }
        }

        /// <summary>
        /// حذف دور (فقط إذا لم يكن دور نظام)
        /// </summary>
        public static void DeleteRole(int roleId)
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                // التحقق أولاً من نوع الدور
                bool isSystem = conn.ExecuteScalar<bool>("SELECT IsSystemRole FROM Roles WHERE RoleID = @Id", new { Id = roleId });
                if (isSystem) throw new InvalidOperationException("لا يمكن حذف دور نظام أساسي.");

                // حذف الصلاحيات المرتبطة
                _ = conn.Execute("DELETE FROM RolePermissions WHERE RoleID = @Id", new { Id = roleId });

                // حذف الدور
                _ = conn.Execute("DELETE FROM Roles WHERE RoleID = @Id", new { Id = roleId });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في حذف الدور");
                throw;
            }
        }

        /// <summary>
        /// الحصول على جميع الأدوار
        /// </summary>
        public static List<Role> GetAllRoles()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                return [.. conn.Query<Role>("SELECT * FROM Roles ORDER BY RoleID")];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في جلب الأدوار");
                return [];
            }
        }

        /// <summary>
        /// جلب صلاحيات المستخدم بناءً على دوره
        /// </summary>
        public static List<string> GetUserPermissions(int userId)
        {
            var permissions = new List<string>();
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                // جلب الصلاحيات المرتبطة بدور المستخدم
                string query = @"
                        SELECT rp.PermissionKey 
                        FROM RolePermissions rp
                        JOIN Users u ON u.RoleID = rp.RoleID
                        WHERE u.UserID = @UserID";

                using var cmd = new SQLiteCommand(query, conn);
                _ = cmd.Parameters.AddWithValue("@UserID", userId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    permissions.Add(reader["PermissionKey"].ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"فشل في جلب صلاحيات المستخدم {userId}");
            }
            return permissions;
        }

        /// <summary>
        /// التحقق من صلاحية معينة لمستخدم (عن طريق دوره)
        /// </summary>
        public static bool HasPermission(int userId, string permissionKey)
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                string query = @"
                        SELECT COUNT(*) 
                        FROM RolePermissions rp
                        JOIN Users u ON u.RoleID = rp.RoleID
                        WHERE u.UserID = @UserID AND rp.PermissionKey = @Key";

                using var cmd = new SQLiteCommand(query, conn);
                _ = cmd.Parameters.AddWithValue("@UserID", userId);
                _ = cmd.Parameters.AddWithValue("@Key", permissionKey);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ في التحقق من الصلاحية");
                return false;
            }
        }

        /// <summary>
        /// جلب صلاحيات دور معين
        /// </summary>
        public static List<string> GetRolePermissions(int roleId)
        {
            var permissions = new List<string>();
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                string query = "SELECT PermissionKey FROM RolePermissions WHERE RoleID = @RoleID";
                using var cmd = new SQLiteCommand(query, conn);
                _ = cmd.Parameters.AddWithValue("@RoleID", roleId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    permissions.Add(reader["PermissionKey"].ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"فشل في جلب صلاحيات الدور {roleId}");
            }
            return permissions;
        }

        /// <summary>
        /// تحديث صلاحيات دور معين (حذف القديم وإضافة الجديد)
        /// </summary>
        public static void UpdateRolePermissions(int roleId, List<string> permissionKeys)
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                using var transaction = conn.BeginTransaction();
                try
                {
                    // 1. حذف جميع الصلاحيات الحالية للدور
                    string deleteQuery = "DELETE FROM RolePermissions WHERE RoleID = @RoleID";
                    using (var cmd = new SQLiteCommand(deleteQuery, conn))
                    {
                        _ = cmd.Parameters.AddWithValue("@RoleID", roleId);
                        _ = cmd.ExecuteNonQuery();
                    }

                    // 2. إضافة الصلاحيات الجديدة
                    if (permissionKeys != null && permissionKeys.Count > 0)
                    {
                        string insertQuery = "INSERT INTO RolePermissions (RoleID, PermissionKey) VALUES (@RoleID, @Key)";
                        using var cmd = new SQLiteCommand(insertQuery, conn);
                        _ = cmd.Parameters.AddWithValue("@RoleID", roleId);
                        var keyParam = cmd.Parameters.Add("@Key", DbType.String);

                        foreach (var key in permissionKeys)
                        {
                            keyParam.Value = key;
                            _ = cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"فشل في تحديث صلاحيات الدور {roleId}");
                throw;
            }
        }

        // دوال مساعدة لتحديد الاسم والتصنيف (يمكن تطويرها لتأخذ من Attributes)
        private static string GetDisplayName(string key)
        {
            // ترجمة بسيطة أو إرجاع المفتاح نفسه
            // يمكن تحسينها لاحقاً
            return key;
        }

        private static string GetCategory(string key)
        {
            if (key.Contains("Product") || key.Contains("Stock")) return "المنتجات";
            if (key.Contains("Sale") || key.Contains("Invoice")) return "المبيعات";
            if (key.Contains("User") || key.Contains("Permission")) return "المستخدمين";
            if (key.Contains("Report")) return "التقارير";
            return "عام";
        }
    }
}
