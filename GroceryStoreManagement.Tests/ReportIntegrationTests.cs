using FluentAssertions;
using GroceryStoreManagement.DAL;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Xunit;

namespace GroceryStoreManagement.Tests
{
    public class ReportIntegrationTests
    {
        [Fact]
        public void TopSellingReportQuery_Should_ReturnData_WithoutExceptions()
        {
            var seed = SeedReportScenario();

            var rows = SaleDAL.GetTopSellingProducts(
                startDate: seed.BaseDate.Date.AddDays(-1),
                endDate: seed.BaseDate.Date.AddDays(1),
                limit: 20);

            rows.Should().NotBeNull();
            rows.Should().NotBeEmpty();

            var productRow = rows.FirstOrDefault(r => r.ProductID == seed.ProductId);
            productRow.Should().NotBeNull();
            productRow!.QuantitySold.Should().Be(3);
        }

        [Fact]
        public void UnpaidReportPipeline_Should_BuildExpectedTypedRows_WithoutExceptions()
        {
            _ = SeedReportScenario();

            var unpaidSales = SaleDAL.GetAllSales()
                .Where(s => s.PaymentStatus != "Paid")
                .ToList();

            var unpaidPurchases = PurchaseDAL.GetAllPurchases()
                .Where(p => p.PaymentStatus != "Paid")
                .ToList();

            var rows = new List<UnpaidInvoiceRow>(unpaidSales.Count + unpaidPurchases.Count);
            rows.AddRange(unpaidSales.Select(s => new UnpaidInvoiceRow
            {
                Type = "مبيعات",
                ID = s.SaleID,
                Name = s.CustomerName ?? "عميل نقدي",
                Total = s.NetTotal,
                Paid = s.PaidAmount,
                Remaining = s.RemainingAmount,
                Date = s.SaleDate,
                CustomerID = s.CustomerID,
                DueDate = s.DueDate
            }));
            rows.AddRange(unpaidPurchases.Select(p => new UnpaidInvoiceRow
            {
                Type = "مشتريات",
                ID = p.PurchaseID,
                Name = p.SupplierName ?? "مورد",
                Total = p.TotalAmount,
                Paid = p.PaidAmount,
                Remaining = p.RemainingAmount,
                Date = p.PurchaseDate
            }));

            rows.Count(r => r.Type == "مبيعات").Should().Be(1);
            rows.Count(r => r.Type == "مشتريات").Should().Be(1);
            rows.Sum(r => r.Remaining).Should().Be(50m);
        }

        private static ReportSeed SeedReportScenario()
        {
            DatabaseHelper.ResetDatabase();
            using var connection = DatabaseHelper.GetConnection();
            DateTime now = DateTime.Now;

            int customerId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Customers (Name, Phone, Email, Address, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('عميل تقارير', '700', 'report@test.local', 'addr', @Now, 1, @Now, 1);",
                ("@Now", now));

            int supplierId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Suppliers (Name, Phone, Email, Address, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES ('مورد تقارير', '701', 'supplier@test.local', 'addr', @Now, 1, @Now, 1);",
                ("@Now", now));

            int productId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Products (
                        Name, Price, PurchasePrice, SellingPrice, Quantity, MinQuantity, Unit, Category,
                        CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
                    )
                    VALUES (
                        'منتج تقارير', 10, 6, 10, 100, 2, 'قطعة', 'عام',
                        @Now, 1, @Now, 1
                    );",
                ("@Now", now));

            int partialSaleId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Sales (
                        CustomerID, TotalAmount, SaleDate, PaidAmount, Discount, Tax, PaymentStatus, PaymentMethod,
                        Notes, ItemCount, RemainingAmount, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, ReturnedAmount
                    )
                    VALUES (
                        @CustomerID, 40, @Now, 20, 0, 0, 'Partial', 'Cash',
                        'seed partial', 1, 20, 1, @Now, 1, @Now, 0
                    );",
                ("@CustomerID", customerId), ("@Now", now));

            _ = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPercent, ReturnedQuantity)
                    VALUES (@SaleID, @ProductID, 2, 10, 20, 0, 0);",
                ("@SaleID", partialSaleId), ("@ProductID", productId));

            int paidSaleId = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Sales (
                        CustomerID, TotalAmount, SaleDate, PaidAmount, Discount, Tax, PaymentStatus, PaymentMethod,
                        Notes, ItemCount, RemainingAmount, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, ReturnedAmount
                    )
                    VALUES (
                        @CustomerID, 10, @Now, 10, 0, 0, 'Paid', 'Cash',
                        'seed paid', 1, 0, 1, @Now, 1, @Now, 0
                    );",
                ("@CustomerID", customerId), ("@Now", now));

            _ = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPercent, ReturnedQuantity)
                    VALUES (@SaleID, @ProductID, 1, 10, 10, 0, 0);",
                ("@SaleID", paidSaleId), ("@ProductID", productId));

            _ = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Purchases (
                        SupplierID, TotalAmount, PaidAmount, Discount, PaymentStatus, PurchaseDate, Notes, InvoiceNumber, ItemCount, IsImported
                    )
                    VALUES (
                        @SupplierID, 50, 20, 0, 'Partial', @Now, 'seed purchase unpaid', 'P-1', 1, 1
                    );",
                ("@SupplierID", supplierId), ("@Now", now));

            _ = ExecuteInsertAndGetId(connection, @"
                    INSERT INTO Purchases (
                        SupplierID, TotalAmount, PaidAmount, Discount, PaymentStatus, PurchaseDate, Notes, InvoiceNumber, ItemCount, IsImported
                    )
                    VALUES (
                        @SupplierID, 30, 30, 0, 'Paid', @Now, 'seed purchase paid', 'P-2', 1, 1
                    );",
                ("@SupplierID", supplierId), ("@Now", now));

            return new ReportSeed
            {
                ProductId = productId,
                BaseDate = now
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

        private sealed class ReportSeed
        {
            public int ProductId { get; set; }
            public DateTime BaseDate { get; set; }
        }
    }
}
