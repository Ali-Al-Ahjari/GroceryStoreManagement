using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroceryStoreManagement.DAL
{
    /// <summary>
    /// عمليات إدارة الورديات (فتح/إغلاق/تقارير).
    /// </summary>
    public static class ShiftDAL
    {
        public static Shift GetOpenShift()
        {
            using var connection = Helpers.DatabaseHelper.GetConnection();
            const string sql = @"
                    SELECT s.*,
                           uOpen.FullName AS OpenedByName,
                           uClose.FullName AS ClosedByName
                    FROM Shifts s
                    LEFT JOIN Users uOpen ON uOpen.UserID = s.OpenedBy
                    LEFT JOIN Users uClose ON uClose.UserID = s.ClosedBy
                    WHERE s.Status = 'Open'
                    ORDER BY s.OpenedAt DESC
                    LIMIT 1;";

            Shift shift = connection.QueryFirstOrDefault<Shift>(sql);
            return shift;
        }

        public static Task<Shift> GetOpenShiftAsync()
        {
            return Task.Run(GetOpenShift);
        }

        public static bool HasOpenShift()
        {
            return GetOpenShift() != null;
        }

        public static int StartShift(decimal openingCash, string notes, int openedBy)
        {
            if (openedBy <= 0)
            {
                throw new InvalidOperationException("يجب تحديد المستخدم المسؤول عن فتح الوردية.");
            }

            if (openingCash < 0)
            {
                throw new ArgumentException("العهدة الافتتاحية لا يمكن أن تكون سالبة.");
            }

            if (HasOpenShift())
            {
                throw new InvalidOperationException("توجد وردية مفتوحة بالفعل. أغلقها أولاً قبل فتح وردية جديدة.");
            }

            using var connection = Helpers.DatabaseHelper.GetConnection();
            const string sql = @"
                    INSERT INTO Shifts (
                        OpenedBy, OpenedAt, OpeningCash, Notes, Status
                    )
                    VALUES (
                        @OpenedBy, @OpenedAt, @OpeningCash, @Notes, 'Open'
                    );
                    SELECT last_insert_rowid();";

            int shiftId = connection.ExecuteScalar<int>(sql, new
            {
                OpenedBy = openedBy,
                OpenedAt = DateTime.Now,
                OpeningCash = openingCash,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            });

            ActivityLogDAL.AddLog(openedBy, "فتح وردية", $"تم فتح وردية رقم #{shiftId} بعهدة افتتاحية {openingCash.ToDisplayCurrency()}");
            return shiftId;
        }

        public static Task<int> StartShiftAsync(decimal openingCash, string notes, int openedBy)
        {
            return Task.Run(() => StartShift(openingCash, notes, openedBy));
        }

        public static Shift CloseCurrentShift(decimal closingCash, string notes, int closedBy)
        {
            if (closedBy <= 0)
            {
                throw new InvalidOperationException("يجب تحديد المستخدم المسؤول عن إغلاق الوردية.");
            }

            if (closingCash < 0)
            {
                throw new ArgumentException("النقدية الفعلية لا يمكن أن تكون سالبة.");
            }

            using var connection = Helpers.DatabaseHelper.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string openSql = @"
                        SELECT *
                        FROM Shifts
                        WHERE Status = 'Open'
                        ORDER BY OpenedAt DESC
                        LIMIT 1;";

                Shift openShift = connection.QueryFirstOrDefault<Shift>(openSql, transaction: transaction)
                    ?? throw new InvalidOperationException("لا توجد وردية مفتوحة لإغلاقها.");

                var salesTotals = connection.QueryFirstOrDefault<ShiftSalesTotals>(@"
                        SELECT
                            COALESCE(SUM(CASE WHEN PaymentMethod = 'Cash' THEN PaidAmount ELSE 0 END), 0) AS CashSalesTotal,
                            COALESCE(SUM(CASE WHEN PaymentMethod = 'Card' THEN PaidAmount ELSE 0 END), 0) AS CardSalesTotal,
                            COALESCE(SUM(CASE WHEN PaymentMethod = 'Transfer' THEN PaidAmount ELSE 0 END), 0) AS TransferSalesTotal,
                            COALESCE(SUM(CASE WHEN PaymentMethod = 'Partial' THEN PaidAmount ELSE 0 END), 0) AS PartialPaidAsCash,
                            COALESCE(SUM(
                                CASE
                                    WHEN (((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)) - PaidAmount - COALESCE(ReturnedAmount, 0)) > 0
                                    THEN (((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)) - PaidAmount - COALESCE(ReturnedAmount, 0))
                                    ELSE 0
                                END
                            ), 0) AS CreditSalesTotal
                        FROM Sales
                        WHERE ShiftID = @ShiftID;",
                    new { ShiftID = openShift.ShiftID }, transaction);

                decimal cashRefunds = connection.ExecuteScalar<decimal>(@"
                        SELECT COALESCE(SUM(r.TotalRefund), 0)
                        FROM Returns r
                        LEFT JOIN Sales s ON s.SaleID = r.SaleID
                        WHERE COALESCE(r.ShiftID, s.ShiftID) = @ShiftID;",
                    new { ShiftID = openShift.ShiftID }, transaction);

                decimal cashSales = (salesTotals?.CashSalesTotal ?? 0m) + (salesTotals?.PartialPaidAsCash ?? 0m);
                decimal cardSales = salesTotals?.CardSalesTotal ?? 0m;
                decimal transferSales = salesTotals?.TransferSalesTotal ?? 0m;
                decimal creditSales = salesTotals?.CreditSalesTotal ?? 0m;
                decimal expectedCash = openShift.OpeningCash + cashSales - cashRefunds;
                decimal cashDifference = closingCash - expectedCash;

                const string closeSql = @"
                        UPDATE Shifts
                        SET ClosedBy = @ClosedBy,
                            ClosedAt = @ClosedAt,
                            ClosingCash = @ClosingCash,
                            CashSalesTotal = @CashSalesTotal,
                            CardSalesTotal = @CardSalesTotal,
                            TransferSalesTotal = @TransferSalesTotal,
                            CreditSalesTotal = @CreditSalesTotal,
                            CashRefundsTotal = @CashRefundsTotal,
                            ExpectedCash = @ExpectedCash,
                            CashDifference = @CashDifference,
                            Notes = @Notes,
                            Status = 'Closed'
                        WHERE ShiftID = @ShiftID;";

                connection.Execute(closeSql, new
                {
                    ShiftID = openShift.ShiftID,
                    ClosedBy = closedBy,
                    ClosedAt = DateTime.Now,
                    ClosingCash = closingCash,
                    CashSalesTotal = cashSales,
                    CardSalesTotal = cardSales,
                    TransferSalesTotal = transferSales,
                    CreditSalesTotal = creditSales,
                    CashRefundsTotal = cashRefunds,
                    ExpectedCash = expectedCash,
                    CashDifference = cashDifference,
                    Notes = string.IsNullOrWhiteSpace(notes) ? openShift.Notes : notes.Trim()
                }, transaction);

                transaction.Commit();

                Shift closedShift = GetShiftById(openShift.ShiftID);
                ActivityLogDAL.AddLog(closedBy, "إغلاق وردية",
                    $"تم إغلاق وردية #{openShift.ShiftID} | المتوقع: {expectedCash.ToDisplayCurrency()} | الفعلي: {closingCash.ToDisplayCurrency()} | الفرق: {cashDifference.ToDisplayCurrency()}");
                return closedShift;
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        }

        public static Task<Shift> CloseCurrentShiftAsync(decimal closingCash, string notes, int closedBy)
        {
            return Task.Run(() => CloseCurrentShift(closingCash, notes, closedBy));
        }

        public static Shift GetShiftById(int shiftId)
        {
            using var connection = Helpers.DatabaseHelper.GetConnection();
            const string sql = @"
                    SELECT s.*,
                           uOpen.FullName AS OpenedByName,
                           uClose.FullName AS ClosedByName
                    FROM Shifts s
                    LEFT JOIN Users uOpen ON uOpen.UserID = s.OpenedBy
                    LEFT JOIN Users uClose ON uClose.UserID = s.ClosedBy
                    WHERE s.ShiftID = @ShiftID
                    LIMIT 1;";

            return connection.QueryFirstOrDefault<Shift>(sql, new { ShiftID = shiftId });
        }

        public static List<Shift> GetRecentShifts(int limit = 30)
        {
            using var connection = Helpers.DatabaseHelper.GetConnection();
            const string sql = @"
                    SELECT s.*,
                           uOpen.FullName AS OpenedByName,
                           uClose.FullName AS ClosedByName
                    FROM Shifts s
                    LEFT JOIN Users uOpen ON uOpen.UserID = s.OpenedBy
                    LEFT JOIN Users uClose ON uClose.UserID = s.ClosedBy
                    ORDER BY s.ShiftID DESC
                    LIMIT @Limit;";

            return [.. connection.Query<Shift>(sql, new { Limit = limit })];
        }

        private sealed class ShiftSalesTotals
        {
            public decimal CashSalesTotal { get; set; }
            public decimal CardSalesTotal { get; set; }
            public decimal TransferSalesTotal { get; set; }
            public decimal PartialPaidAsCash { get; set; }
            public decimal CreditSalesTotal { get; set; }
        }
    }
}
