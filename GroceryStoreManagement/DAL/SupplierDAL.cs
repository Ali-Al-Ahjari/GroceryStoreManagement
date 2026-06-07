using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using GroceryStoreManagement.Helpers;

namespace GroceryStoreManagement.DAL
{
    public static class SupplierDAL
    {
        public static List<Supplier> GetAllSuppliers()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*,
                               COUNT(p.ProductID) as ProductCount,
                               CAST(COALESCE(SUM(p.SellingPrice * p.Quantity), 0) AS REAL) as TotalSuppliedValue
                        FROM Suppliers s
                        LEFT JOIN Products p ON s.SupplierID = p.SupplierID
                        GROUP BY s.SupplierID
                        ORDER BY s.Name";

                return [.. connection.Query<Supplier>(query)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب الموردين: {ex.Message}", ex);
            }
        }

        public static Supplier GetSupplierById(int supplierId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*,
                               COUNT(p.ProductID) as ProductCount,
                               CAST(COALESCE(SUM(p.SellingPrice * p.Quantity), 0) AS REAL) as TotalSuppliedValue
                        FROM Suppliers s
                        LEFT JOIN Products p ON s.SupplierID = p.SupplierID
                        WHERE s.SupplierID = @SupplierID
                        GROUP BY s.SupplierID";

                return connection.QueryFirstOrDefault<Supplier>(query, new { SupplierID = supplierId });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المورد: {ex.Message}", ex);
            }
        }

        public static List<Supplier> SearchSuppliers(string searchTerm)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*,
                               COUNT(p.ProductID) as ProductCount,
                               CAST(COALESCE(SUM(p.SellingPrice * p.Quantity), 0) AS REAL) as TotalSuppliedValue
                        FROM Suppliers s
                        LEFT JOIN Products p ON s.SupplierID = p.SupplierID
                        WHERE s.Name LIKE @SearchTerm 
                           OR s.Phone LIKE @SearchTerm 
                           OR s.Email LIKE @SearchTerm
                        GROUP BY s.SupplierID
                        ORDER BY s.Name";

                return [.. connection.Query<Supplier>(query, new { SearchTerm = $"%{searchTerm}%" })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في بحث الموردين: {ex.Message}", ex);
            }
        }

        public static int AddSupplier(Supplier supplier)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تعبئة حقول التدقيق
                Helpers.AuditHelper.SetFullAudit(supplier);

                string query = @"
                        INSERT INTO Suppliers (Name, Phone, Email, Address, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                        VALUES (@Name, @Phone, @Email, @Address, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy);
                        SELECT last_insert_rowid();";

                int newId = connection.ExecuteScalar<int>(query, new
                {
                    supplier.Name,
                    supplier.Phone,
                    supplier.Email,
                    supplier.Address,
                    supplier.CreatedDate,
                    supplier.CreatedBy,
                    supplier.ModifiedDate,
                    supplier.ModifiedBy
                });

                // تسجيل النشاط
                ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "إضافة مورد", $"تم إضافة المورد: {supplier.Name}");

                return newId;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة المورد: {ex.Message}", ex);
            }
        }

        public static bool UpdateSupplier(Supplier supplier)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تعبئة حقول التعديل
                Helpers.AuditHelper.SetModificationAudit(supplier);

                string query = @"
                        UPDATE Suppliers 
                        SET Name = @Name, 
                            Phone = @Phone, 
                            Email = @Email, 
                            Address = @Address,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE SupplierID = @SupplierID";

                int rowsAffected = connection.Execute(query, new
                {
                    supplier.SupplierID,
                    supplier.Name,
                    supplier.Phone,
                    supplier.Email,
                    supplier.Address,
                    supplier.ModifiedDate,
                    supplier.ModifiedBy
                });

                if (rowsAffected > 0)
                {
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "تعديل مورد", $"تم تعديل المورد: {supplier.Name}");
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث المورد: {ex.Message}", ex);
            }
        }

        public static bool DeleteSupplier(int supplierId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // التحقق من عدم وجود منتجات مرتبطة بالمورد
                string checkQuery = "SELECT COUNT(*) FROM Products WHERE SupplierID = @SupplierID";
                int productCount = connection.ExecuteScalar<int>(checkQuery, new { SupplierID = supplierId });

                if (productCount > 0)
                {
                    throw new Exception("لا يمكن حذف المورد لأنه مرتبط بمنتجات");
                }

                string query = "DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
                int rowsAffected = connection.Execute(query, new { SupplierID = supplierId });

                if (rowsAffected > 0)
                {
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "حذف مورد", $"تم حذف المورد رقم: {supplierId}");
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف المورد: {ex.Message}", ex);
            }
        }

        public static int GetTotalSuppliersCount()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "SELECT COUNT(*) FROM Suppliers";
                return connection.ExecuteScalar<int>(query);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب عدد الموردين: {ex.Message}", ex);
            }
        }

        public static List<Supplier> GetSuppliersWithProducts()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT DISTINCT s.*
                        FROM Suppliers s
                        INNER JOIN Products p ON s.SupplierID = p.SupplierID
                        ORDER BY s.Name";

                return [.. connection.Query<Supplier>(query)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب الموردين الذين لديهم منتجات: {ex.Message}", ex);
            }
        }

        public static bool SupplierExists(string phone)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "SELECT COUNT(*) FROM Suppliers WHERE Phone = @Phone";
                int count = connection.ExecuteScalar<int>(query, new { Phone = phone });
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التحقق من وجود المورد: {ex.Message}", ex);
            }
        }
    }
}
