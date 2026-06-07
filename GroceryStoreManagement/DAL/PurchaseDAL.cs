using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroceryStoreManagement.DAL
{
    /// <summary>
    /// طبقة الوصول لبيانات المشتريات
    /// </summary>
    public static class PurchaseDAL
    {
        /// <summary>
        /// جلب جميع المشتريات
        /// </summary>
        public static List<Purchase> GetAllPurchases()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = @"SELECT p.PurchaseID, p.SupplierID, p.TotalAmount, p.PaidAmount, p.Discount, 
                               p.PaymentStatus, p.PurchaseDate, p.Notes, p.InvoiceNumber, p.ItemCount, p.IsImported,
                               s.Name as SupplierName 
                               FROM Purchases p 
                               LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID 
                               ORDER BY p.PurchaseDate DESC";
                return [.. connection.Query<Purchase>(sql)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المشتريات: {ex.Message}", ex);
            }
        }

        public static Task<List<Purchase>> GetAllPurchasesAsync()
        {
            return Task.Run(GetAllPurchases);
        }

        /// <summary>
        /// جلب مشتريات بتاريخ معين
        /// </summary>
        public static List<Purchase> GetPurchasesByDate(DateTime date)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = @"SELECT p.PurchaseID, p.SupplierID, p.TotalAmount, p.PaidAmount, p.Discount, 
                               p.PaymentStatus, p.PurchaseDate, p.Notes, p.InvoiceNumber, p.ItemCount, p.IsImported,
                               s.Name as SupplierName 
                               FROM Purchases p 
                               LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID 
                               WHERE DATE(p.PurchaseDate) = DATE(@Date)
                               ORDER BY p.PurchaseDate DESC";
                return [.. connection.Query<Purchase>(sql, new { Date = date })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المشتريات: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// جلب مشترية بالمعرف
        /// </summary>
        public static Purchase GetPurchaseById(int purchaseId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = @"SELECT p.PurchaseID, p.SupplierID, p.TotalAmount, p.PaidAmount, p.Discount, 
                               p.PaymentStatus, p.PurchaseDate, p.Notes, p.InvoiceNumber, p.ItemCount, p.IsImported,
                               s.Name as SupplierName 
                               FROM Purchases p 
                               LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID 
                               WHERE p.PurchaseID = @PurchaseID";
                return connection.QueryFirstOrDefault<Purchase>(sql, new { PurchaseID = purchaseId });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب الفاتورة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// إضافة فاتورة شراء جديدة
        /// </summary>
        public static int AddPurchase(Purchase purchase)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = @"INSERT INTO Purchases (SupplierID, TotalAmount, PaidAmount, Discount, PaymentStatus, PurchaseDate, Notes, InvoiceNumber, ItemCount, IsImported) 
                               VALUES (@SupplierID, @TotalAmount, @PaidAmount, @Discount, @PaymentStatus, @PurchaseDate, @Notes, @InvoiceNumber, @ItemCount, @IsImported);
                               SELECT last_insert_rowid();";
                return connection.ExecuteScalar<int>(sql, purchase);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة الفاتورة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تحديث فاتورة شراء
        /// </summary>
        public static bool UpdatePurchase(Purchase purchase)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = @"UPDATE Purchases SET 
                               SupplierID = @SupplierID,
                               TotalAmount = @TotalAmount,
                               PaidAmount = @PaidAmount,
                               Discount = @Discount,
                               PaymentStatus = @PaymentStatus,
                               Notes = @Notes,
                               InvoiceNumber = @InvoiceNumber,
                               ItemCount = @ItemCount,
                               IsImported = @IsImported
                               WHERE PurchaseID = @PurchaseID";
                return connection.Execute(sql, purchase) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث الفاتورة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// حذف فاتورة شراء
        /// </summary>
        public static bool DeletePurchase(int purchaseId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // حذف العناصر أولاً
                _ = connection.Execute("DELETE FROM PurchaseItems WHERE PurchaseID = @PurchaseID", new { PurchaseID = purchaseId });
                // ثم حذف الفاتورة
                return connection.Execute("DELETE FROM Purchases WHERE PurchaseID = @PurchaseID", new { PurchaseID = purchaseId }) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف الفاتورة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// الحصول على إجمالي المشتريات
        /// </summary>
        public static decimal GetTotalPurchases()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = "SELECT COALESCE(SUM(TotalAmount), 0) FROM Purchases";
                return connection.ExecuteScalar<decimal>(sql);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب إجمالي المشتريات: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// الحصول على إجمالي المدفوع للموردين
        /// </summary>
        public static decimal GetTotalPaidToSuppliers()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = "SELECT COALESCE(SUM(PaidAmount), 0) FROM Purchases";
                return connection.ExecuteScalar<decimal>(sql);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب المدفوع: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// الحصول على إجمالي المستحق للموردين
        /// </summary>
        public static decimal GetTotalDueToSuppliers()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = "SELECT COALESCE(SUM(TotalAmount - Discount - PaidAmount), 0) FROM Purchases WHERE PaymentStatus != 'Paid'";
                return connection.ExecuteScalar<decimal>(sql);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب المستحق: {ex.Message}", ex);
            }
        }
    }
}

