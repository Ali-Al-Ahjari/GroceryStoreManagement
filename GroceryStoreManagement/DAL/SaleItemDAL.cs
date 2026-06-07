using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace GroceryStoreManagement.DAL
{
    public static class SaleItemDAL
    {
        public static List<SaleItem> GetSaleItemsBySaleId(int saleId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT si.*, p.Name as ProductName
                        FROM SaleItems si
                        INNER JOIN Products p ON si.ProductID = p.ProductID
                        WHERE si.SaleID = @SaleID
                        ORDER BY si.SaleItemID";

                return [.. connection.Query<SaleItem>(query, new { SaleID = saleId })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب تفاصيل الفاتورة: {ex.Message}", ex);
            }
        }

        public static SaleItem GetSaleItemById(int saleItemId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT si.*, p.Name as ProductName
                        FROM SaleItems si
                        INNER JOIN Products p ON si.ProductID = p.ProductID
                        WHERE si.SaleItemID = @SaleItemID";

                return connection.QueryFirstOrDefault<SaleItem>(query, new { SaleItemID = saleItemId });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب عنصر الفاتورة: {ex.Message}", ex);
            }
        }

        public static int AddSaleItem(SaleItem saleItem)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, DiscountPercent, TotalPrice)
                        VALUES (@SaleID, @ProductID, @Quantity, @UnitPrice, @DiscountPercent, @TotalPrice);
                        SELECT last_insert_rowid();";

                return connection.ExecuteScalar<int>(query, new
                {
                    saleItem.SaleID,
                    saleItem.ProductID,
                    saleItem.Quantity,
                    saleItem.UnitPrice,
                    saleItem.DiscountPercent,
                    saleItem.TotalPrice
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة عنصر الفاتورة: {ex.Message}", ex);
            }
        }

        public static bool UpdateSaleItem(SaleItem saleItem)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        UPDATE SaleItems 
                        SET ProductID = @ProductID, 
                            Quantity = @Quantity, 
                            UnitPrice = @UnitPrice, 
                            DiscountPercent = @DiscountPercent,
                            TotalPrice = @TotalPrice
                        WHERE SaleItemID = @SaleItemID";

                int rowsAffected = connection.Execute(query, new
                {
                    saleItem.SaleItemID,
                    saleItem.ProductID,
                    saleItem.Quantity,
                    saleItem.UnitPrice,
                    saleItem.DiscountPercent,
                    saleItem.TotalPrice
                });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث عنصر الفاتورة: {ex.Message}", ex);
            }
        }

        public static bool UpdateReturnedQuantity(int saleItemId, int returnedQuantity)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        UPDATE SaleItems 
                        SET ReturnedQuantity = @ReturnedQuantity 
                        WHERE SaleItemID = @SaleItemID";

                int rowsAffected = connection.Execute(query, new
                {
                    SaleItemID = saleItemId,
                    ReturnedQuantity = returnedQuantity
                });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث الكمية المرتجعة: {ex.Message}", ex);
            }
        }

        public static bool DeleteSaleItem(int saleItemId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "DELETE FROM SaleItems WHERE SaleItemID = @SaleItemID";
                int rowsAffected = connection.Execute(query, new { SaleItemID = saleItemId });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف عنصر الفاتورة: {ex.Message}", ex);
            }
        }

        public static bool DeleteAllSaleItems(int saleId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "DELETE FROM SaleItems WHERE SaleID = @SaleID";
                int rowsAffected = connection.Execute(query, new { SaleID = saleId });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف جميع عناصر الفاتورة: {ex.Message}", ex);
            }
        }

        public static List<SaleItem> GetTopSellingItems(int limit = 10)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT 
                            p.ProductID,
                            p.Name as ProductName,
                            CAST(SUM(si.Quantity) AS INTEGER) as Quantity,
                            CAST(AVG(si.UnitPrice) AS REAL) as UnitPrice,
                            CAST(SUM(si.TotalPrice) AS REAL) as TotalPrice
                        FROM SaleItems si
                        INNER JOIN Products p ON si.ProductID = p.ProductID
                        GROUP BY si.ProductID
                        ORDER BY Quantity DESC
                        LIMIT @Limit";

                return [.. connection.Query<SaleItem>(query, new { Limit = limit })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب العناصر الأكثر مبيعاً: {ex.Message}", ex);
            }
        }

        public static List<SaleItem> GetSaleItemsByProduct(int productId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT si.*, p.Name as ProductName
                        FROM SaleItems si
                        INNER JOIN Products p ON si.ProductID = p.ProductID
                        WHERE si.ProductID = @ProductID
                        ORDER BY si.SaleItemID DESC";

                return [.. connection.Query<SaleItem>(query, new { ProductID = productId })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب مبيعات المنتج: {ex.Message}", ex);
            }
        }

        public static decimal GetTotalSalesByProduct(int productId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT COALESCE(SUM(TotalPrice), 0)
                        FROM SaleItems
                        WHERE ProductID = @ProductID";

                return connection.ExecuteScalar<decimal>(query, new { ProductID = productId });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب إجمالي مبيعات المنتج: {ex.Message}", ex);
            }
        }

        public static int GetTotalQuantitySoldByProduct(int productId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT COALESCE(SUM(Quantity), 0)
                        FROM SaleItems
                        WHERE ProductID = @ProductID";

                return connection.ExecuteScalar<int>(query, new { ProductID = productId });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب الكمية المباعة من المنتج: {ex.Message}", ex);
            }
        }

        public static List<SaleItem> GetTopProductsForCustomer(int customerId, int limit = 5)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT 
                            p.ProductID,
                            p.Name as ProductName,
                            CAST(SUM(si.Quantity) AS INTEGER) as Quantity,
                            CAST(AVG(si.UnitPrice) AS REAL) as UnitPrice,
                            CAST(SUM(si.TotalPrice) AS REAL) as TotalPrice
                        FROM SaleItems si
                        INNER JOIN Sales s ON si.SaleID = s.SaleID
                        INNER JOIN Products p ON si.ProductID = p.ProductID
                        WHERE s.CustomerID = @CustomerID
                        GROUP BY si.ProductID
                        ORDER BY Quantity DESC
                        LIMIT @Limit";

                return [.. connection.Query<SaleItem>(query, new { CustomerID = customerId, Limit = limit })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب منتجات العميل الأكثر شراءً: {ex.Message}", ex);
            }
        }
    }
}

