using FluentAssertions;
using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Xunit;

namespace GroceryStoreManagement.Tests
{
    public class ReturnDALTests
    {
        [Fact]
        public void ProcessReturn_Should_UpdateStockAndSaleAmounts()
        {
            var seed = SeedSaleScenario();

            var result = ReturnDAL.ProcessReturn(seed.SaleId,
            [
                new() { SaleItemID = seed.SaleItemId, Quantity = 1 }
            ], "منتج تالف", seed.UserId);

            result.ReturnID.Should().BeGreaterThan(0);
            result.TotalRefund.Should().Be(10m);
            result.UpdatedRemainingAmount.Should().Be(5m);
            result.UpdatedPaymentStatus.Should().Be("Partial");

            using var connection = DatabaseHelper.GetConnection();
            int returnsCount = ExecuteScalarInt(connection, "SELECT COUNT(*) FROM Returns WHERE SaleID = @SaleID",
                ("@SaleID", seed.SaleId));
            int returnItemsCount = ExecuteScalarInt(connection, "SELECT COUNT(*) FROM ReturnItems WHERE ReturnID = @ReturnID",
                ("@ReturnID", result.ReturnID));
            int returnedQty = ExecuteScalarInt(connection, "SELECT ReturnedQuantity FROM SaleItems WHERE SaleItemID = @SaleItemID",
                ("@SaleItemID", seed.SaleItemId));
            int productQty = ExecuteScalarInt(connection, "SELECT Quantity FROM Products WHERE ProductID = @ProductID",
                ("@ProductID", seed.ProductId));

            returnsCount.Should().Be(1);
            returnItemsCount.Should().Be(1);
            returnedQty.Should().Be(1);
            productQty.Should().Be(19); // 20 initial - 2 sold + 1 returned
        }

        [Fact]
        public void ProcessReturn_Should_RejectOverReturn_AndKeepDataUnchanged()
        {
            var seed = SeedSaleScenario();

            Action act = () => ReturnDAL.ProcessReturn(seed.SaleId,
            [
                new() { SaleItemID = seed.SaleItemId, Quantity = 3 } // sold qty is 2
            ], "كمية خاطئة", seed.UserId);

            act.Should().Throw<Exception>();

            using var connection = DatabaseHelper.GetConnection();
            int returnsCount = ExecuteScalarInt(connection, "SELECT COUNT(*) FROM Returns WHERE SaleID = @SaleID",
                ("@SaleID", seed.SaleId));
            int returnedQty = ExecuteScalarInt(connection, "SELECT ReturnedQuantity FROM SaleItems WHERE SaleItemID = @SaleItemID",
                ("@SaleItemID", seed.SaleItemId));
            int productQty = ExecuteScalarInt(connection, "SELECT Quantity FROM Products WHERE ProductID = @ProductID",
                ("@ProductID", seed.ProductId));

            returnsCount.Should().Be(0);
            returnedQty.Should().Be(0);
            productQty.Should().Be(18);
        }

        [Fact]
        public void ProcessReturn_Should_Rollback_WhenItemDoesNotBelongToSale()
        {
            var seed = SeedSaleScenario();

            Action act = () => ReturnDAL.ProcessReturn(seed.SaleId,
            [
                new() { SaleItemID = seed.SaleItemId, Quantity = 1 },
                new() { SaleItemID = 999999, Quantity = 1 } // invalid item id
            ], "اختبار rollback", seed.UserId);

            act.Should().Throw<Exception>();

            using var connection = DatabaseHelper.GetConnection();
            int returnsCount = ExecuteScalarInt(connection, "SELECT COUNT(*) FROM Returns WHERE SaleID = @SaleID",
                ("@SaleID", seed.SaleId));
            int returnItemsCount = ExecuteScalarInt(connection, "SELECT COUNT(*) FROM ReturnItems");
            int returnedQty = ExecuteScalarInt(connection, "SELECT ReturnedQuantity FROM SaleItems WHERE SaleItemID = @SaleItemID",
                ("@SaleItemID", seed.SaleItemId));
            int productQty = ExecuteScalarInt(connection, "SELECT Quantity FROM Products WHERE ProductID = @ProductID",
                ("@ProductID", seed.ProductId));

            returnsCount.Should().Be(0);
            returnItemsCount.Should().Be(0);
            returnedQty.Should().Be(0);
            productQty.Should().Be(18);
        }

        [Fact]
        public void ProcessReturn_Should_NotStoreNegativeRemainingAmount_WhenInvoiceAlreadyPaid()
        {
            var seed = SeedSaleScenario(
                paidAmount: 20m,
                paymentStatus: "Paid",
                remainingAmount: 0m);

            var result = ReturnDAL.ProcessReturn(seed.SaleId,
            [
                new() { SaleItemID = seed.SaleItemId, Quantity = 1 }
            ], "فاتورة مدفوعة", seed.UserId);

            result.UpdatedRemainingAmount.Should().Be(0m);
            result.UpdatedPaymentStatus.Should().Be("Paid");

            using var connection = DatabaseHelper.GetConnection();
            decimal remainingInDb = ExecuteScalarDecimal(connection, @"
                    SELECT RemainingAmount
                    FROM Sales
                    WHERE SaleID = @SaleID;",
                ("@SaleID", seed.SaleId));
            string paymentStatus = ExecuteScalarString(connection, @"
                    SELECT PaymentStatus
                    FROM Sales
                    WHERE SaleID = @SaleID;",
                ("@SaleID", seed.SaleId));

            remainingInDb.Should().Be(0m);
            paymentStatus.Should().Be("Paid");
        }

        private static SeedResult SeedSaleScenario()
        {
            return SeedSaleScenario(
                paidAmount: 5m,
                paymentStatus: "Partial",
                remainingAmount: 15m);
        }

        private static SeedResult SeedSaleScenario(decimal paidAmount, string paymentStatus, decimal remainingAmount)
        {
            DatabaseHelper.ResetDatabase();

            using var connection = DatabaseHelper.GetConnection();

            int userId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Users (Username, Password, FullName, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('test_user', '123', 'Test User', 1, @Now, 1, @Now, 1);",
                ("@Now", DateTime.Now));

            int customerId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Customers (Name, Phone, Email, Address, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('عميل اختبار', '777', 'c@test.local', 'addr', @Now, @UserID, @Now, @UserID);",
                ("@Now", DateTime.Now), ("@UserID", userId));

            int productId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Products (
                        Name, Price, PurchasePrice, SellingPrice, Quantity, MinQuantity, Unit, Category,
                        CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
                    )
                    VALUES (
                        'منتج اختبار', 10, 7, 10, 20, 2, 'قطعة', 'عام',
                        @Now, @UserID, @Now, @UserID
                    );",
                ("@Now", DateTime.Now), ("@UserID", userId));

            int saleId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Sales (
                        CustomerID, TotalAmount, SaleDate, PaidAmount, Discount, Tax, PaymentStatus, PaymentMethod,
                        Notes, ItemCount, RemainingAmount, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, ReturnedAmount
                    )
                    VALUES (
                        @CustomerID, 20, @Now, @PaidAmount, 0, 0, @PaymentStatus, 'Cash',
                        'seed', 1, @RemainingAmount, @UserID, @Now, @UserID, @Now, 0
                    );",
                ("@CustomerID", customerId),
                ("@Now", DateTime.Now),
                ("@UserID", userId),
                ("@PaidAmount", paidAmount),
                ("@PaymentStatus", paymentStatus),
                ("@RemainingAmount", remainingAmount));

            int saleItemId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPercent, ReturnedQuantity)
                    VALUES (@SaleID, @ProductID, 2, 10, 20, 0, 0);",
                ("@SaleID", saleId), ("@ProductID", productId));

            return new SeedResult
            {
                UserId = userId,
                CustomerId = customerId,
                ProductId = productId,
                SaleId = saleId,
                SaleItemId = saleItemId
            };
        }

        private static int ExecuteInsertAndGetId(SQLiteConnection connection, string sql, params (string Key, object Value)[] parameters)
        {
            using var cmd = new SQLiteCommand(sql + "SELECT last_insert_rowid();", connection);
            foreach (var (key, value) in parameters)
            {
                _ = cmd.Parameters.AddWithValue(key, value);
            }
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static int ExecuteScalarInt(SQLiteConnection connection, string sql, params (string Key, object Value)[] parameters)
        {
            using var cmd = new SQLiteCommand(sql, connection);
            foreach (var (key, value) in parameters)
            {
                _ = cmd.Parameters.AddWithValue(key, value);
            }
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static decimal ExecuteScalarDecimal(SQLiteConnection connection, string sql, params (string Key, object Value)[] parameters)
        {
            using var cmd = new SQLiteCommand(sql, connection);
            foreach (var (key, value) in parameters)
            {
                _ = cmd.Parameters.AddWithValue(key, value);
            }
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        private static string ExecuteScalarString(SQLiteConnection connection, string sql, params (string Key, object Value)[] parameters)
        {
            using var cmd = new SQLiteCommand(sql, connection);
            foreach (var (key, value) in parameters)
            {
                _ = cmd.Parameters.AddWithValue(key, value);
            }
            return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        private sealed class SeedResult
        {
            public int UserId { get; set; }
            public int CustomerId { get; set; }
            public int ProductId { get; set; }
            public int SaleId { get; set; }
            public int SaleItemId { get; set; }
        }
    }
}
