using FluentAssertions;
using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using System;
using System.Data.SQLite;
using Xunit;

namespace GroceryStoreManagement.Tests
{
    public class DebtPaymentTests
    {
        [Fact]
        public void ApplyDebtPayment_Should_DistributeAmount_ByDueDateThenSaleDate()
        {
            var seed = SeedTwoDebtInvoices();

            var result = CustomerDAL.ApplyDebtPayment(seed.CustomerId, 50m, "Transfer", seed.UserId);

            result.AppliedAmount.Should().Be(50m);
            result.UnappliedAmount.Should().Be(0m);
            result.AffectedInvoicesCount.Should().Be(2);
            result.TotalRemainingAfterPayment.Should().Be(20m);
            result.Allocations.Should().HaveCount(2);
            result.Allocations[0].SaleID.Should().Be(seed.FirstSaleId);
            result.Allocations[0].AppliedAmount.Should().Be(30m);
            result.Allocations[1].SaleID.Should().Be(seed.SecondSaleId);
            result.Allocations[1].AppliedAmount.Should().Be(20m);

            using var connection = DatabaseHelper.GetConnection();
            var first = QuerySaleSnapshot(connection, seed.FirstSaleId);
            var second = QuerySaleSnapshot(connection, seed.SecondSaleId);

            first.PaidAmount.Should().Be(30m);
            first.RemainingAmount.Should().Be(0m);
            first.PaymentStatus.Should().Be("Paid");
            first.PaymentMethod.Should().Be("Transfer");

            second.PaidAmount.Should().Be(20m);
            second.RemainingAmount.Should().Be(20m);
            second.PaymentStatus.Should().Be("Partial");
            second.PaymentMethod.Should().Be("Transfer");
        }

        [Fact]
        public void ApplyDebtPayment_Should_ReportUnappliedAmount_WhenPaymentExceedsDebt()
        {
            var seed = SeedSingleDebtInvoice(remainingAmount: 25m);

            var result = CustomerDAL.ApplyDebtPayment(seed.CustomerId, 40m, "Cash", seed.UserId);

            result.AppliedAmount.Should().Be(25m);
            result.UnappliedAmount.Should().Be(15m);
            result.TotalRemainingAfterPayment.Should().Be(0m);

            using var connection = DatabaseHelper.GetConnection();
            var invoice = QuerySaleSnapshot(connection, seed.FirstSaleId);
            invoice.PaymentStatus.Should().Be("Paid");
            invoice.RemainingAmount.Should().Be(0m);
        }

        [Fact]
        public void ApplyDebtPayment_Should_Throw_WhenCustomerHasNoDebtInvoices()
        {
            var seed = SeedSingleDebtInvoice(remainingAmount: 0m);

            Action act = () => CustomerDAL.ApplyDebtPayment(seed.CustomerId, 10m, "Card", seed.UserId);

            act.Should().Throw<Exception>().WithMessage("*لا توجد فواتير مدينة*");
        }

        private static SeedResult SeedTwoDebtInvoices()
        {
            DatabaseHelper.ResetDatabase();
            using var connection = DatabaseHelper.GetConnection();

            int userId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Users (Username, Password, FullName, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('debt_user', '123', 'Debt User', 1, @Now, 1, @Now, 1);",
                ("@Now", DateTime.Now));

            int customerId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Customers (Name, Phone, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('عميل دين', '700', @Now, @UserID, @Now, @UserID);",
                ("@Now", DateTime.Now), ("@UserID", userId));

            int firstSaleId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Sales (
                        CustomerID, TotalAmount, SaleDate, DueDate, PaidAmount, Discount, Tax,
                        PaymentStatus, PaymentMethod, ItemCount, RemainingAmount, ReturnedAmount,
                        CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                    )
                    VALUES (
                        @CustomerID, 30, @SaleDate, @DueDate, 0, 0, 0,
                        'Unpaid', 'Cash', 1, 30, 0,
                        @UserID, @Now, @UserID, @Now
                    );",
                ("@CustomerID", customerId),
                ("@SaleDate", DateTime.Today.AddDays(-6)),
                ("@DueDate", DateTime.Today.AddDays(-2)),
                ("@UserID", userId),
                ("@Now", DateTime.Now));

            int secondSaleId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Sales (
                        CustomerID, TotalAmount, SaleDate, DueDate, PaidAmount, Discount, Tax,
                        PaymentStatus, PaymentMethod, ItemCount, RemainingAmount, ReturnedAmount,
                        CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                    )
                    VALUES (
                        @CustomerID, 40, @SaleDate, @DueDate, 0, 0, 0,
                        'Unpaid', 'Cash', 1, 40, 0,
                        @UserID, @Now, @UserID, @Now
                    );",
                ("@CustomerID", customerId),
                ("@SaleDate", DateTime.Today.AddDays(-3)),
                ("@DueDate", DateTime.Today.AddDays(3)),
                ("@UserID", userId),
                ("@Now", DateTime.Now));

            return new SeedResult
            {
                UserId = userId,
                CustomerId = customerId,
                FirstSaleId = firstSaleId,
                SecondSaleId = secondSaleId
            };
        }

        private static SeedResult SeedSingleDebtInvoice(decimal remainingAmount)
        {
            DatabaseHelper.ResetDatabase();
            using var connection = DatabaseHelper.GetConnection();

            int userId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Users (Username, Password, FullName, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('single_debt_user', '123', 'Single Debt User', 1, @Now, 1, @Now, 1);",
                ("@Now", DateTime.Now));

            int customerId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Customers (Name, Phone, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('عميل واحد', '701', @Now, @UserID, @Now, @UserID);",
                ("@Now", DateTime.Now), ("@UserID", userId));

            decimal paidAmount = 25m - remainingAmount;
            string status = remainingAmount <= 0 ? "Paid" : "Partial";

            int saleId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Sales (
                        CustomerID, TotalAmount, SaleDate, DueDate, PaidAmount, Discount, Tax,
                        PaymentStatus, PaymentMethod, ItemCount, RemainingAmount, ReturnedAmount,
                        CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                    )
                    VALUES (
                        @CustomerID, 25, @SaleDate, @DueDate, @PaidAmount, 0, 0,
                        @PaymentStatus, 'Cash', 1, @RemainingAmount, 0,
                        @UserID, @Now, @UserID, @Now
                    );",
                ("@CustomerID", customerId),
                ("@SaleDate", DateTime.Today.AddDays(-3)),
                ("@DueDate", DateTime.Today.AddDays(1)),
                ("@PaidAmount", paidAmount),
                ("@PaymentStatus", status),
                ("@RemainingAmount", remainingAmount),
                ("@UserID", userId),
                ("@Now", DateTime.Now));

            return new SeedResult
            {
                UserId = userId,
                CustomerId = customerId,
                FirstSaleId = saleId
            };
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

        private sealed class SeedResult
        {
            public int UserId { get; set; }
            public int CustomerId { get; set; }
            public int FirstSaleId { get; set; }
            public int SecondSaleId { get; set; }
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
