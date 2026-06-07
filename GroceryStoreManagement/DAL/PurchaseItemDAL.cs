using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GroceryStoreManagement.DAL
{
    /// <summary>
    /// طبقة الوصول لبيانات عناصر المشتريات
    /// </summary>
    public static class PurchaseItemDAL
    {
        /// <summary>
        /// جلب عناصر فاتورة شراء
        /// </summary>
        public static List<PurchaseItem> GetPurchaseItemsByPurchaseId(int purchaseId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = @"SELECT pi.*, p.Name as ProductName, p.Code as ProductCode 
                               FROM PurchaseItems pi 
                               LEFT JOIN Products p ON pi.ProductID = p.ProductID 
                               WHERE pi.PurchaseID = @PurchaseID";
                return [.. connection.Query<PurchaseItem>(sql, new { PurchaseID = purchaseId })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب عناصر الفاتورة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// إضافة عنصر لفاتورة شراء
        /// </summary>
        public static int AddPurchaseItem(PurchaseItem item)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                var sql = @"INSERT INTO PurchaseItems (PurchaseID, ProductID, Quantity, UnitPrice, TotalPrice) 
                               VALUES (@PurchaseID, @ProductID, @Quantity, @UnitPrice, @TotalPrice);
                               SELECT last_insert_rowid();";
                return connection.ExecuteScalar<int>(sql, item);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة عنصر الفاتورة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// حذف جميع عناصر فاتورة شراء
        /// </summary>
        public static bool DeleteAllPurchaseItems(int purchaseId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                return connection.Execute("DELETE FROM PurchaseItems WHERE PurchaseID = @PurchaseID",
                    new { PurchaseID = purchaseId }) >= 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف عناصر الفاتورة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// استيراد عناصر المشتريات للمخزون
        /// </summary>
        public static bool ImportToInventory(int purchaseId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // جلب عناصر الفاتورة
                var items = GetPurchaseItemsByPurchaseId(purchaseId);

                foreach (var item in items)
                {
                    // تحديث كمية المنتج في المخزون
                    var sql = "UPDATE Products SET Quantity = Quantity + @Quantity WHERE ProductID = @ProductID";
                    _ = connection.Execute(sql, new { item.Quantity, item.ProductID });
                }

                // تحديث حالة الاستيراد في الفاتورة
                _ = connection.Execute("UPDATE Purchases SET IsImported = 1 WHERE PurchaseID = @PurchaseID",
                    new { PurchaseID = purchaseId });

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في استيراد المنتجات للمخزون: {ex.Message}", ex);
            }
        }
    }
}

