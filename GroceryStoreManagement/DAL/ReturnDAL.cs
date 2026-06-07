using Dapper;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace GroceryStoreManagement.DAL
{
    /// <summary>
    /// طبقة بيانات عمليات المرتجعات.
    /// </summary>
    public static class ReturnDAL
    {
        public static ReturnProcessResult ProcessReturn(int saleId, IReadOnlyList<ReturnRequestItem> items, string reason, int userId)
        {
            if (saleId <= 0)
            {
                throw new ArgumentException("معرف الفاتورة غير صالح.");
            }

            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("لا توجد عناصر للإرجاع.");
            }

            string cleanReason = (reason ?? string.Empty).Trim();
            if (cleanReason.Length < 3)
            {
                throw new ArgumentException("سبب الإرجاع مطلوب ويجب ألا يقل عن 3 أحرف.");
            }

            var normalizedItems = items
                .Where(i => i != null && i.SaleItemID > 0 && i.Quantity > 0)
                .GroupBy(i => i.SaleItemID)
                .Select(g => new ReturnRequestItem
                {
                    SaleItemID = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            if (normalizedItems.Count == 0)
            {
                throw new ArgumentException("لا توجد كميات صالحة للإرجاع.");
            }

            using var connection = DatabaseHelper.GetConnection();
            using var transaction = connection.BeginTransaction();

            int returnId = 0;
            decimal totalRefund = 0;
            decimal updatedRemainingAmount = 0;
            string updatedPaymentStatus = "Unpaid";

            try
            {
                var sale = connection.QueryFirstOrDefault<SaleFinancialRow>(@"
                        SELECT SaleID, ShiftID, TotalAmount, Discount, Tax, PaidAmount, COALESCE(ReturnedAmount, 0) AS ReturnedAmount
                        FROM Sales
                        WHERE SaleID = @SaleID;",
                    new { SaleID = saleId }, transaction)
                    ?? throw new Exception("الفاتورة غير موجودة.");

                int? currentShiftId = connection.ExecuteScalar<int?>(@"
                        SELECT ShiftID
                        FROM Shifts
                        WHERE Status = 'Open'
                        ORDER BY OpenedAt DESC
                        LIMIT 1;",
                    transaction: transaction);
                int? returnShiftId = currentShiftId ?? sale.ShiftID;

                var saleItems = connection.Query<SaleItemRow>(@"
                        SELECT SaleItemID, ProductID, Quantity, COALESCE(ReturnedQuantity, 0) AS ReturnedQuantity,
                               UnitPrice, COALESCE(DiscountPercent, 0) AS DiscountPercent
                        FROM SaleItems
                        WHERE SaleID = @SaleID;",
                    new { SaleID = saleId }, transaction)
                    .ToDictionary(x => x.SaleItemID);

                var plannedRows = new List<PlannedReturnRow>(normalizedItems.Count);

                foreach (var item in normalizedItems)
                {
                    if (!saleItems.TryGetValue(item.SaleItemID, out var saleItemRow))
                    {
                        throw new Exception($"عنصر الفاتورة #{item.SaleItemID} غير موجود ضمن الفاتورة المحددة.");
                    }

                    int maxReturnable = saleItemRow.Quantity - saleItemRow.ReturnedQuantity;
                    if (item.Quantity > maxReturnable)
                    {
                        throw new Exception($"الكمية المطلوبة للإرجاع أكبر من المتاح للعنصر #{item.SaleItemID}. المتاح: {maxReturnable}");
                    }

                    decimal refundAmount = item.Quantity * saleItemRow.UnitPrice * (1 - saleItemRow.DiscountPercent / 100m);
                    totalRefund += refundAmount;

                    plannedRows.Add(new PlannedReturnRow
                    {
                        SaleItemID = saleItemRow.SaleItemID,
                        ProductID = saleItemRow.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = saleItemRow.UnitPrice,
                        DiscountPercent = saleItemRow.DiscountPercent,
                        RefundAmount = refundAmount,
                        NewReturnedQuantity = saleItemRow.ReturnedQuantity + item.Quantity
                    });
                }

                returnId = connection.ExecuteScalar<int>(@"
                        INSERT INTO Returns (
                            SaleID, ShiftID, ReturnDate, Reason, TotalRefund,
                            CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                        )
                        VALUES (
                            @SaleID, @ShiftID, @ReturnDate, @Reason, @TotalRefund,
                            @CreatedBy, @CreatedDate, @ModifiedBy, @ModifiedDate
                        );
                        SELECT last_insert_rowid();",
                    new
                    {
                        SaleID = saleId,
                        ShiftID = returnShiftId,
                        ReturnDate = DateTime.Now,
                        Reason = cleanReason,
                        TotalRefund = totalRefund,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now,
                        ModifiedBy = userId,
                        ModifiedDate = DateTime.Now
                    }, transaction);

                foreach (var row in plannedRows)
                {
                    _ = connection.Execute(@"
                            INSERT INTO ReturnItems (
                                ReturnID, SaleItemID, ProductID, Quantity,
                                UnitPrice, DiscountPercent, RefundAmount
                            )
                            VALUES (
                                @ReturnID, @SaleItemID, @ProductID, @Quantity,
                                @UnitPrice, @DiscountPercent, @RefundAmount
                            );",
                        new
                        {
                            ReturnID = returnId,
                            row.SaleItemID,
                            row.ProductID,
                            row.Quantity,
                            row.UnitPrice,
                            row.DiscountPercent,
                            row.RefundAmount
                        }, transaction);

                    _ = connection.Execute(@"
                            UPDATE SaleItems
                            SET ReturnedQuantity = @ReturnedQuantity
                            WHERE SaleItemID = @SaleItemID;",
                        new
                        {
                            ReturnedQuantity = row.NewReturnedQuantity,
                            row.SaleItemID
                        }, transaction);

                    _ = connection.Execute(@"
                            UPDATE Products
                            SET Quantity = Quantity + @Quantity,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy
                            WHERE ProductID = @ProductID;",
                        new
                        {
                            row.Quantity,
                            ModifiedDate = DateTime.Now,
                            ModifiedBy = userId,
                            row.ProductID
                        }, transaction);
                }

                decimal newReturnedAmount = sale.ReturnedAmount + totalRefund;
                decimal netTotal = sale.TotalAmount - sale.Discount + (sale.TotalAmount * (sale.Tax / 100m));
                updatedRemainingAmount = Math.Max(0, netTotal - sale.PaidAmount - newReturnedAmount);
                updatedPaymentStatus = updatedRemainingAmount <= 0
                    ? "Paid"
                    : sale.PaidAmount > 0 ? "Partial" : "Unpaid";

                _ = connection.Execute(@"
                        UPDATE Sales
                        SET ReturnedAmount = @ReturnedAmount,
                            RemainingAmount = @RemainingAmount,
                            PaymentStatus = @PaymentStatus,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE SaleID = @SaleID;",
                    new
                    {
                        ReturnedAmount = newReturnedAmount,
                        RemainingAmount = updatedRemainingAmount,
                        PaymentStatus = updatedPaymentStatus,
                        ModifiedDate = DateTime.Now,
                        ModifiedBy = userId,
                        SaleID = saleId
                    }, transaction);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    Logger.LogError(rollbackEx, $"فشل Rollback لعملية إرجاع الفاتورة #{saleId}");
                }

                Logger.LogError(ex, $"فشل تنفيذ عملية إرجاع الفاتورة #{saleId}");
                throw;
            }

            try
            {
                ActivityLogDAL.AddLog(userId, "إرجاع منتجات",
                    $"تم إنشاء مرتجع #{returnId} للفاتورة #{saleId} بقيمة {totalRefund.ToDisplayCurrency()}. السبب: {cleanReason}");
            }
            catch (Exception logEx)
            {
                Logger.LogWarning($"تمت عملية الإرجاع بنجاح لكن فشل تسجيل النشاط: {logEx.Message}");
            }

            return new ReturnProcessResult
            {
                ReturnID = returnId,
                SaleID = saleId,
                TotalRefund = totalRefund,
                ProcessedItemsCount = normalizedItems.Count,
                UpdatedRemainingAmount = updatedRemainingAmount,
                UpdatedPaymentStatus = updatedPaymentStatus
            };
        }

        public static List<ReturnRecord> GetReturnsBySaleId(int saleId)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                var returns = connection.Query<ReturnRecord>(@"
                        SELECT ReturnID, SaleID, ReturnDate, Reason, TotalRefund,
                               CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                        FROM Returns
                        WHERE SaleID = @SaleID
                        ORDER BY ReturnDate DESC;",
                    new { SaleID = saleId }).ToList();

                if (returns.Count == 0)
                {
                    return [];
                }

                var returnIds = returns.Select(x => x.ReturnID).ToArray();
                var itemsLookup = connection.Query<ReturnItemRecord>(@"
                        SELECT ri.ReturnItemID, ri.ReturnID, ri.SaleItemID, ri.ProductID, ri.Quantity,
                               ri.UnitPrice, ri.DiscountPercent, ri.RefundAmount,
                               p.Name AS ProductName
                        FROM ReturnItems ri
                        LEFT JOIN Products p ON p.ProductID = ri.ProductID
                        WHERE ri.ReturnID IN @ReturnIDs
                        ORDER BY ri.ReturnItemID;",
                    new { ReturnIDs = returnIds })
                    .GroupBy(x => x.ReturnID)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var item in returns)
                {
                    item.Items = itemsLookup.TryGetValue(item.ReturnID, out var values) ? values : [];
                }

                return returns;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المرتجعات للفاتورة #{saleId}: {ex.Message}", ex);
            }
        }

        public static List<ReturnItemRecord> GetReturnItems(int returnId)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                return [.. connection.Query<ReturnItemRecord>(@"
                        SELECT ri.ReturnItemID, ri.ReturnID, ri.SaleItemID, ri.ProductID, ri.Quantity,
                               ri.UnitPrice, ri.DiscountPercent, ri.RefundAmount,
                               p.Name AS ProductName
                        FROM ReturnItems ri
                        LEFT JOIN Products p ON p.ProductID = ri.ProductID
                        WHERE ri.ReturnID = @ReturnID
                        ORDER BY ri.ReturnItemID;",
                    new { ReturnID = returnId })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب عناصر المرتجع #{returnId}: {ex.Message}", ex);
            }
        }

        private sealed class SaleFinancialRow
        {
            public int SaleID { get; set; }
            public int? ShiftID { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal Discount { get; set; }
            public decimal Tax { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal ReturnedAmount { get; set; }
        }

        private sealed class SaleItemRow
        {
            public int SaleItemID { get; set; }
            public int ProductID { get; set; }
            public int Quantity { get; set; }
            public int ReturnedQuantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountPercent { get; set; }
        }

        private sealed class PlannedReturnRow
        {
            public int SaleItemID { get; set; }
            public int ProductID { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal RefundAmount { get; set; }
            public int NewReturnedQuantity { get; set; }
        }
    }
}
