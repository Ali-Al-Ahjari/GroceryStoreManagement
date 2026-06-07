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
    public class SaleDALTests
    {
        [Fact]
        public void UpdatePayment_Should_ClampPaidAmount_WhenExceedingTotalDue()
        {
            DatabaseHelper.ResetDatabase();
            using var connection = DatabaseHelper.GetConnection();

            int customerId = SeedCustomer(connection);
            int saleId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Sales (
                        CustomerID, TotalAmount, SaleDate, PaidAmount, Discount, Tax,
                        PaymentStatus, PaymentMethod, ItemCount, RemainingAmount, ReturnedAmount,
                        CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                    )
                    VALUES (
                        @CustomerID, 100, @Now, 20, 0, 0,
                        'Partial', 'Cash', 1, 80, 0,
                        1, @Now, 1, @Now
                    );",
                ("@CustomerID", customerId), ("@Now", DateTime.Now));

            bool updated = SaleDAL.UpdatePayment(saleId, 150m, "Paid", "Cash");

            updated.Should().BeTrue();
            var row = QuerySaleSnapshot(connection, saleId);
            row.PaidAmount.Should().Be(100m);
            row.RemainingAmount.Should().Be(0m);
            row.PaymentStatus.Should().Be("Paid");
        }

        [Fact]
        public void UpdatePayment_Should_RespectReturnedAmount_WhenCalculatingRemaining()
        {
            DatabaseHelper.ResetDatabase();
            using var connection = DatabaseHelper.GetConnection();

            int customerId = SeedCustomer(connection);
            int saleId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Sales (
                        CustomerID, TotalAmount, SaleDate, PaidAmount, Discount, Tax,
                        PaymentStatus, PaymentMethod, ItemCount, RemainingAmount, ReturnedAmount,
                        CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                    )
                    VALUES (
                        @CustomerID, 100, @Now, 20, 0, 0,
                        'Partial', 'Cash', 1, 50, 30,
                        1, @Now, 1, @Now
                    );",
                ("@CustomerID", customerId), ("@Now", DateTime.Now));

            bool updated = SaleDAL.UpdatePayment(saleId, 40m, "Partial", "Card");

            updated.Should().BeTrue();
            var row = QuerySaleSnapshot(connection, saleId);
            row.PaidAmount.Should().Be(40m);
            row.RemainingAmount.Should().Be(30m);
            row.PaymentStatus.Should().Be("Partial");
            row.PaymentMethod.Should().Be("Card");
        }

        [Fact]
        public void SaveSaleWithItems_Should_Rollback_WhenStockIsInsufficient()
        {
            DatabaseHelper.ResetDatabase();
            using var connection = DatabaseHelper.GetConnection();

            int productId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Products (
                        Name, Price, PurchasePrice, SellingPrice, Quantity, MinQuantity, Unit, Category,
                        CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
                    )
                    VALUES (
                        'منتج اختبار', 10, 6, 10, 1, 1, 'قطعة', 'عام',
                        @Now, 1, @Now, 1
                    );",
                ("@Now", DateTime.Now));

            var sale = new Sale
            {
                CustomerID = null,
                TotalAmount = 20m,
                Discount = 0m,
                Tax = 0m,
                PaidAmount = 0m,
                PaymentMethod = "Cash",
                PaymentStatus = "Unpaid",
                Notes = "stock-check",
                ItemCount = 1,
                SaleDate = DateTime.Now
            };

            var items = new List<SaleItem>
            {
                new()
                {
                    ProductID = productId,
                    ProductName = "منتج اختبار",
                    Quantity = 2,
                    UnitPrice = 10m,
                    DiscountPercent = 0m,
                    TotalPrice = 20m
                }
            };

            Action act = () => SaleDAL.SaveSaleWithItems(sale, items, isEditMode: false);

            act.Should().Throw<Exception>().WithMessage("*الكمية المطلوبة غير متوفرة*");
            ExecuteScalarInt(connection, "SELECT COUNT(*) FROM Sales").Should().Be(0);
            ExecuteScalarInt(connection, "SELECT COUNT(*) FROM SaleItems").Should().Be(0);
            ExecuteScalarInt(connection, "SELECT Quantity FROM Products WHERE ProductID = @ProductID",
                ("@ProductID", productId)).Should().Be(1);
        }

        private static int SeedCustomer(SQLiteConnection connection)
        {
            return ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Customers (Name, Phone, Email, Address, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('عميل اختبار', '700', 'sale@test.local', 'addr', @Now, 1, @Now, 1);",
                ("@Now", DateTime.Now));
        }

        private static SaleSnapshot QuerySaleSnapshot(SQLiteConnection connection, int saleId)
        {
            using var cmd = new SQLiteCommand(@"
                    SELECT PaidAmount, RemainingAmount, PaymentStatus, PaymentMethod
                    FROM Sales
                    WHERE SaleID = @SaleID;", connection);
            _ = cmd.Parameters.AddWithValue("@SaleID", saleId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                throw new Exception("Sale not found");
            }

            return new SaleSnapshot
            {
                PaidAmount = Convert.ToDecimal(reader["PaidAmount"]),
                RemainingAmount = Convert.ToDecimal(reader["RemainingAmount"]),
                PaymentStatus = reader["PaymentStatus"]?.ToString() ?? string.Empty,
                PaymentMethod = reader["PaymentMethod"]?.ToString() ?? string.Empty
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

        private sealed class SaleSnapshot
        {
            public decimal PaidAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public string PaymentStatus { get; set; } = string.Empty;
            public string PaymentMethod { get; set; } = string.Empty;
        }
    }
}
