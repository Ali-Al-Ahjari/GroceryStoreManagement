using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroceryStoreManagement.DAL
{
    /// <summary>
    /// إدارة الفواتير المعلقة (Hold/Resume).
    /// </summary>
    public static class SuspendedSaleDAL
    {
        public static int SaveSuspendedSale(
            int? customerId,
            string notes,
            decimal discount,
            decimal tax,
            string paymentMethod,
            int? shiftId,
            int userId,
            IEnumerable<SaleItem> items)
        {
            var safeItems = items?.Where(x => x != null && x.ProductID > 0 && x.Quantity > 0).ToList()
                ?? throw new ArgumentNullException(nameof(items));

            if (safeItems.Count == 0)
            {
                throw new InvalidOperationException("لا يمكن تعليق فاتورة بدون أصناف.");
            }

            using var connection = Helpers.DatabaseHelper.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                decimal subtotal = safeItems.Sum(x => x.TotalPrice);
                const string headerSql = @"
                        INSERT INTO SuspendedSales (
                            CustomerID, Notes, Discount, Tax, PaymentMethod, CreatedAt, CreatedBy, ShiftID, Subtotal
                        )
                        VALUES (
                            @CustomerID, @Notes, @Discount, @Tax, @PaymentMethod, @CreatedAt, @CreatedBy, @ShiftID, @Subtotal
                        );
                        SELECT last_insert_rowid();";

                int suspendedId = connection.ExecuteScalar<int>(headerSql, new
                {
                    CustomerID = customerId,
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    Discount = discount,
                    Tax = tax,
                    PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod,
                    CreatedAt = DateTime.Now,
                    CreatedBy = userId > 0 ? (int?)userId : null,
                    ShiftID = shiftId,
                    Subtotal = subtotal
                }, transaction);

                const string itemSql = @"
                        INSERT INTO SuspendedSaleItems (
                            SuspendedSaleID, ProductID, ProductName, Quantity, UnitPrice, DiscountPercent, TotalPrice
                        )
                        VALUES (
                            @SuspendedSaleID, @ProductID, @ProductName, @Quantity, @UnitPrice, @DiscountPercent, @TotalPrice
                        );";

                foreach (var item in safeItems)
                {
                    connection.Execute(itemSql, new
                    {
                        SuspendedSaleID = suspendedId,
                        item.ProductID,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.DiscountPercent,
                        item.TotalPrice
                    }, transaction);
                }

                transaction.Commit();
                ActivityLogDAL.AddLog(userId, "تعليق فاتورة", $"تم تعليق فاتورة مؤقتة #{suspendedId} بعدد {safeItems.Count} أصناف.");
                return suspendedId;
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        }

        public static Task<int> SaveSuspendedSaleAsync(
            int? customerId,
            string notes,
            decimal discount,
            decimal tax,
            string paymentMethod,
            int? shiftId,
            int userId,
            IEnumerable<SaleItem> items)
        {
            return Task.Run(() => SaveSuspendedSale(customerId, notes, discount, tax, paymentMethod, shiftId, userId, items));
        }

        public static List<SuspendedSale> GetSuspendedSales(int? shiftId = null)
        {
            using var connection = Helpers.DatabaseHelper.GetConnection();
            string sql = @"
                    SELECT ss.*,
                           c.Name AS CustomerName
                    FROM SuspendedSales ss
                    LEFT JOIN Customers c ON c.CustomerID = ss.CustomerID
                    ORDER BY ss.CreatedAt DESC;";
            object parameters = null;
            if (shiftId.HasValue)
            {
                sql = @"
                    SELECT ss.*,
                           c.Name AS CustomerName
                    FROM SuspendedSales ss
                    LEFT JOIN Customers c ON c.CustomerID = ss.CustomerID
                    WHERE ss.ShiftID = @ShiftID
                    ORDER BY ss.CreatedAt DESC;";
                parameters = new { ShiftID = shiftId.Value };
            }

            return [.. connection.Query<SuspendedSale>(sql, parameters)];
        }

        public static Task<List<SuspendedSale>> GetSuspendedSalesAsync(int? shiftId = null)
        {
            return Task.Run(() => GetSuspendedSales(shiftId));
        }

        public static SuspendedSale GetSuspendedSaleById(int suspendedSaleId)
        {
            using var connection = Helpers.DatabaseHelper.GetConnection();
            const string headerSql = @"
                    SELECT ss.*,
                           c.Name AS CustomerName
                    FROM SuspendedSales ss
                    LEFT JOIN Customers c ON c.CustomerID = ss.CustomerID
                    WHERE ss.SuspendedSaleID = @SuspendedSaleID
                    LIMIT 1;";

            SuspendedSale sale = connection.QueryFirstOrDefault<SuspendedSale>(headerSql, new { SuspendedSaleID = suspendedSaleId });
            if (sale == null)
            {
                return null;
            }

            const string itemsSql = @"
                    SELECT *
                    FROM SuspendedSaleItems
                    WHERE SuspendedSaleID = @SuspendedSaleID
                    ORDER BY SuspendedSaleItemID;";

            sale.Items = [.. connection.Query<SuspendedSaleItem>(itemsSql, new { SuspendedSaleID = suspendedSaleId })];
            return sale;
        }

        public static SuspendedSale ResumeAndDelete(int suspendedSaleId, int actorUserId)
        {
            using var connection = Helpers.DatabaseHelper.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string headerSql = @"
                        SELECT ss.*,
                               c.Name AS CustomerName
                        FROM SuspendedSales ss
                        LEFT JOIN Customers c ON c.CustomerID = ss.CustomerID
                        WHERE ss.SuspendedSaleID = @SuspendedSaleID
                        LIMIT 1;";

                SuspendedSale sale = connection.QueryFirstOrDefault<SuspendedSale>(headerSql, new { SuspendedSaleID = suspendedSaleId }, transaction);
                if (sale == null)
                {
                    throw new InvalidOperationException("الفاتورة المعلقة غير موجودة.");
                }

                const string itemsSql = @"
                        SELECT *
                        FROM SuspendedSaleItems
                        WHERE SuspendedSaleID = @SuspendedSaleID
                        ORDER BY SuspendedSaleItemID;";
                sale.Items = [.. connection.Query<SuspendedSaleItem>(itemsSql, new { SuspendedSaleID = suspendedSaleId }, transaction)];

                connection.Execute("DELETE FROM SuspendedSales WHERE SuspendedSaleID = @SuspendedSaleID;",
                    new { SuspendedSaleID = suspendedSaleId }, transaction);

                transaction.Commit();
                ActivityLogDAL.AddLog(actorUserId, "استدعاء فاتورة معلقة", $"تم استدعاء الفاتورة المعلقة #{suspendedSaleId}.");
                return sale;
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        }

        public static bool DeleteSuspendedSale(int suspendedSaleId, int actorUserId)
        {
            using var connection = Helpers.DatabaseHelper.GetConnection();
            int affected = connection.Execute("DELETE FROM SuspendedSales WHERE SuspendedSaleID = @SuspendedSaleID;",
                new { SuspendedSaleID = suspendedSaleId });

            if (affected > 0)
            {
                ActivityLogDAL.AddLog(actorUserId, "حذف فاتورة معلقة", $"تم حذف الفاتورة المعلقة #{suspendedSaleId}.");
            }

            return affected > 0;
        }
    }
}
