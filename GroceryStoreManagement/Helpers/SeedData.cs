using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace GroceryStoreManagement.Helpers
{
    public static class SeedData
    {
        private const int TargetRecords = 1000;

        public static void InitializeDatabase()
        {
            try
            {
                if (!ShouldSeedData())
                {
                    Logger.LogInfo("SeedData معطل - تم تجاوز تعبئة البيانات التجريبية");
                    return;
                }

                bool needsSeeding;
                using (var conn = DatabaseHelper.GetConnection())
                {
                    if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                    needsSeeding =
                        GetCount(conn, "Suppliers") < TargetRecords ||
                        GetCount(conn, "Products") < TargetRecords ||
                        GetCount(conn, "Customers") < TargetRecords ||
                        GetCount(conn, "Users") < TargetRecords ||
                        GetCount(conn, "Sales") < TargetRecords ||
                        GetCount(conn, "SaleItems") < TargetRecords ||
                        GetCount(conn, "Purchases") < TargetRecords ||
                        GetCount(conn, "PurchaseItems") < TargetRecords ||
                        GetCount(conn, "Promotions") < TargetRecords ||
                        GetCount(conn, "Notifications") < TargetRecords ||
                        GetCount(conn, "ActivityLogs") < TargetRecords ||
                        GetCount(conn, "Shifts") < TargetRecords ||
                        GetCount(conn, "SuspendedSales") < TargetRecords ||
                        GetCount(conn, "SuspendedSaleItems") < TargetRecords ||
                        GetCount(conn, "Returns") < TargetRecords ||
                        GetCount(conn, "ReturnItems") < TargetRecords;
                }

                if (needsSeeding)
                {
                    InsertDefaultData();
                    Logger.LogInfo($"تمت تعبئة قاعدة البيانات ببيانات تجريبية حتى {TargetRecords} سجل.");
                }
                else
                {
                    Logger.LogInfo($"SeedData: البيانات الحالية مكتملة (>= {TargetRecords}).");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "خطأ أثناء محاولة تعبئة البيانات التجريبية");
            }
        }

        private static bool ShouldSeedData()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "settings.ini");
                if (!File.Exists(settingsPath))
                {
                    return false;
                }

                foreach (var line in File.ReadAllLines(settingsPath))
                {
                    if (line.StartsWith("SeedDataEnabled=", StringComparison.OrdinalIgnoreCase))
                    {
                        string value = line.Split('=')[1].Trim();
                        if (bool.TryParse(value, out bool enabled))
                        {
                            return enabled;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل قراءة إعداد SeedDataEnabled");
            }

            return false;
        }

        private static void InsertDefaultData()
        {
            using var connection = DatabaseHelper.GetConnection();
            if (connection.State != System.Data.ConnectionState.Open) connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var command = new SQLiteCommand(connection) { Transaction = transaction };
                var rnd = new Random();

                string[] categories = ["معلبات", "مشروبات", "حلويات", "ألبان", "لحوم", "خضروات", "فواكه", "مكسرات", "بهارات", "منظفات"];
                string[] productPrefixes = ["ممتاز", "فاخر", "عادي", "مستورد", "محلي", "طازج", "عضوي"];
                string[] paymentMethods = ["Cash", "Card", "Transfer", "Credit"];
                string[] logActions = ["إضافة", "تعديل", "حذف", "تسجيل دخول", "طباعة تقرير"];
                string nowStamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                int supplierCount = GetCount(connection, "Suppliers");
                for (int i = supplierCount + 1; i <= TargetRecords; i++)
                {
                    command.CommandText = @"
                        INSERT INTO Suppliers (Name, Phone, Email, Address, CreatedDate)
                        VALUES (@Name, @Phone, @Email, @Address, @CreatedDate)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Name", $"مورد تجريبي {i}");
                    _ = command.Parameters.AddWithValue("@Phone", $"05{rnd.Next(10000000, 99999999)}");
                    _ = command.Parameters.AddWithValue("@Email", $"supplier.seed.{nowStamp}.{i}@example.com");
                    _ = command.Parameters.AddWithValue("@Address", $"العنوان التجاري {i}");
                    _ = command.Parameters.AddWithValue("@CreatedDate", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(1, 365))));
                    _ = command.ExecuteNonQuery();
                }

                List<int> supplierIds = GetIds(connection, "Suppliers", "SupplierID");

                int productCount = GetCount(connection, "Products");
                for (int i = productCount + 1; i <= TargetRecords; i++)
                {
                    if (supplierIds.Count == 0) break;

                    string category = categories[rnd.Next(categories.Length)];
                    string name = $"{category} {productPrefixes[rnd.Next(productPrefixes.Length)]} - {i}";
                    decimal price = RoundAmount((decimal)(rnd.NextDouble() * 120 + 5));
                    int quantity = rnd.Next(500, 5000);
                    int supplierId = supplierIds[rnd.Next(supplierIds.Count)];
                    decimal cost = RoundAmount(price * 0.75m);

                    command.CommandText = @"
                        INSERT INTO Products (Name, Price, Quantity, Category, SupplierID, Code, SellingPrice, PurchasePrice, MinQuantity, CreatedDate)
                        VALUES (@Name, @Price, @Quantity, @Category, @SupplierID, @Code, @SellingPrice, @PurchasePrice, @MinQuantity, @CreatedDate)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Name", name);
                    _ = command.Parameters.AddWithValue("@Price", price);
                    _ = command.Parameters.AddWithValue("@Quantity", quantity);
                    _ = command.Parameters.AddWithValue("@Category", category);
                    _ = command.Parameters.AddWithValue("@SupplierID", supplierId);
                    _ = command.Parameters.AddWithValue("@Code", $"PRD-{nowStamp}-{i:D5}-{rnd.Next(100, 999)}");
                    _ = command.Parameters.AddWithValue("@SellingPrice", price);
                    _ = command.Parameters.AddWithValue("@PurchasePrice", cost);
                    _ = command.Parameters.AddWithValue("@MinQuantity", rnd.Next(5, 40));
                    _ = command.Parameters.AddWithValue("@CreatedDate", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(1, 365))));
                    _ = command.ExecuteNonQuery();
                }

                List<ProductSeed> products = GetProductSeeds(connection);

                int customerCount = GetCount(connection, "Customers");
                string[] names = ["محمد", "أحمد", "علي", "سعيد", "فهد", "عبدالله", "سارة", "نورة", "خالد", "ريم"];
                for (int i = customerCount + 1; i <= TargetRecords; i++)
                {
                    string custName = $"{names[rnd.Next(names.Length)]} عميل {i}";
                    command.CommandText = @"
                        INSERT INTO Customers (Name, Phone, Email, Address, Notes, IsActive, CreatedDate)
                        VALUES (@Name, @Phone, @Email, @Address, @Notes, 1, @CreatedDate)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Name", custName);
                    _ = command.Parameters.AddWithValue("@Phone", $"07{rnd.Next(10000000, 99999999)}");
                    _ = command.Parameters.AddWithValue("@Email", $"customer.seed.{nowStamp}.{i}@mail.com");
                    _ = command.Parameters.AddWithValue("@Address", $"العنوان {i}");
                    _ = command.Parameters.AddWithValue("@Notes", "عميل تجريبي");
                    _ = command.Parameters.AddWithValue("@CreatedDate", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(1, 365))));
                    _ = command.ExecuteNonQuery();
                }

                command.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = 'cashier'";
                command.Parameters.Clear();
                int cashierCount = Convert.ToInt32(command.ExecuteScalar());
                if (cashierCount == 0)
                {
                    string cashierPassword = PasswordHelper.HashPassword("123");
                    command.CommandText = @"
                        INSERT INTO Users (Username, Password, FullName, RoleID, IsActive,
                            CanAccessDashboard, CanViewCustomers, CanAddCustomers, CanEditCustomers, CanDeleteCustomers,
                            CanManageProducts, CanManageInvoices, CanViewReports, CanManageSettings, CanBackup, CreatedDate)
                        VALUES ('cashier', @Password, 'Cashier User', 3, 1,
                            0, 1, 1, 0, 0,
                            0, 1, 0, 0, 0, @CreatedDate)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Password", cashierPassword);
                    _ = command.Parameters.AddWithValue("@CreatedDate", ToSqlDate(DateTime.Now));
                    _ = command.ExecuteNonQuery();
                }
                List<int> roleIds = GetIds(connection, "Roles", "RoleID");
                if (roleIds.Count == 0) roleIds.Add(1);

                int userCount = GetCount(connection, "Users");
                string seedPasswordHash = PasswordHelper.HashPassword("123");
                for (int i = userCount + 1; i <= TargetRecords; i++)
                {
                    int roleId = roleIds[rnd.Next(roleIds.Count)];
                    bool canManage = roleId is 1 or 2;
                    command.CommandText = @"
                        INSERT INTO Users (Username, Password, FullName, Phone, Email, RoleID, IsActive,
                            CanAccessDashboard, CanViewCustomers, CanAddCustomers, CanEditCustomers, CanDeleteCustomers,
                            CanManageProducts, CanManageInvoices, CanViewReports, CanManageSettings, CanBackup, CreatedDate)
                        VALUES (@Username, @Password, @FullName, @Phone, @Email, @RoleID, 1,
                            @CanAccessDashboard, 1, 1, @CanEditCustomers, @CanDeleteCustomers,
                            @CanManageProducts, 1, @CanViewReports, @CanManageSettings, @CanBackup, @CreatedDate)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Username", $"seed_user_{i}_{rnd.Next(1000, 9999)}");
                    _ = command.Parameters.AddWithValue("@Password", seedPasswordHash);
                    _ = command.Parameters.AddWithValue("@FullName", $"مستخدم تجريبي {i}");
                    _ = command.Parameters.AddWithValue("@Phone", $"07{rnd.Next(10000000, 99999999)}");
                    _ = command.Parameters.AddWithValue("@Email", $"seed.user.{nowStamp}.{i}@example.com");
                    _ = command.Parameters.AddWithValue("@RoleID", roleId);
                    _ = command.Parameters.AddWithValue("@CanAccessDashboard", canManage ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CanEditCustomers", canManage ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CanDeleteCustomers", canManage ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CanManageProducts", canManage ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CanViewReports", canManage ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CanManageSettings", canManage ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CanBackup", canManage ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CreatedDate", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(1, 120))));
                    _ = command.ExecuteNonQuery();
                }

                List<int> userIds = GetIds(connection, "Users", "UserID");
                List<int> customerIds = GetIds(connection, "Customers", "CustomerID");

                int shiftCount = GetCount(connection, "Shifts");
                for (int i = shiftCount + 1; i <= TargetRecords; i++)
                {
                    if (userIds.Count == 0) break;

                    DateTime openedAt = DateTime.Now.AddDays(-rnd.Next(1, 180)).AddHours(-rnd.Next(0, 8));
                    DateTime closedAt = openedAt.AddHours(rnd.Next(6, 12));
                    decimal openingCash = rnd.Next(100, 1200);
                    decimal cashSales = rnd.Next(500, 9000);
                    decimal refunds = rnd.Next(0, 500);
                    decimal expectedCash = openingCash + cashSales - refunds;
                    decimal diff = RoundAmount((decimal)((rnd.NextDouble() - 0.5) * 50));

                    command.CommandText = @"
                        INSERT INTO Shifts (
                            OpenedBy, OpenedAt, OpeningCash, ClosedBy, ClosedAt, ClosingCash,
                            CashSalesTotal, CardSalesTotal, TransferSalesTotal, CreditSalesTotal,
                            CashRefundsTotal, ExpectedCash, CashDifference, Notes, Status
                        )
                        VALUES (
                            @OpenedBy, @OpenedAt, @OpeningCash, @ClosedBy, @ClosedAt, @ClosingCash,
                            @CashSalesTotal, @CardSalesTotal, @TransferSalesTotal, @CreditSalesTotal,
                            @CashRefundsTotal, @ExpectedCash, @CashDifference, @Notes, 'Closed'
                        )";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@OpenedBy", userIds[rnd.Next(userIds.Count)]);
                    _ = command.Parameters.AddWithValue("@OpenedAt", ToSqlDate(openedAt));
                    _ = command.Parameters.AddWithValue("@OpeningCash", openingCash);
                    _ = command.Parameters.AddWithValue("@ClosedBy", userIds[rnd.Next(userIds.Count)]);
                    _ = command.Parameters.AddWithValue("@ClosedAt", ToSqlDate(closedAt));
                    _ = command.Parameters.AddWithValue("@ClosingCash", expectedCash + diff);
                    _ = command.Parameters.AddWithValue("@CashSalesTotal", cashSales);
                    _ = command.Parameters.AddWithValue("@CardSalesTotal", rnd.Next(200, 5000));
                    _ = command.Parameters.AddWithValue("@TransferSalesTotal", rnd.Next(0, 2500));
                    _ = command.Parameters.AddWithValue("@CreditSalesTotal", rnd.Next(0, 1500));
                    _ = command.Parameters.AddWithValue("@CashRefundsTotal", refunds);
                    _ = command.Parameters.AddWithValue("@ExpectedCash", expectedCash);
                    _ = command.Parameters.AddWithValue("@CashDifference", diff);
                    _ = command.Parameters.AddWithValue("@Notes", "وردية تجريبية");
                    _ = command.ExecuteNonQuery();
                }

                List<int> shiftIds = GetIds(connection, "Shifts", "ShiftID");

                int salesCount = GetCount(connection, "Sales");
                for (int i = salesCount + 1; i <= TargetRecords; i++)
                {
                    if (customerIds.Count == 0 || products.Count == 0) break;

                    int customerId = customerIds[rnd.Next(customerIds.Count)];
                    ProductSeed selectedProduct = products[rnd.Next(products.Count)];
                    int qty = rnd.Next(1, 4);
                    decimal lineTotal = RoundAmount(selectedProduct.UnitPrice * qty);
                    int? shiftId = shiftIds.Count == 0 ? null : shiftIds[rnd.Next(shiftIds.Count)];
                    DateTime saleDate = DateTime.Now.AddDays(-rnd.Next(0, 120));

                    command.CommandText = @"
                        INSERT INTO Sales (
                            CustomerID, TotalAmount, SaleDate, PaymentMethod, PaymentStatus, PaidAmount, RemainingAmount,
                            ItemCount, Discount, Tax, ShiftID, Notes
                        )
                        VALUES (
                            @CustomerID, 0, @SaleDate, @PaymentMethod, 'Paid', 0, 0,
                            0, 0, 0, @ShiftID, @Notes
                        )";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@CustomerID", customerId);
                    _ = command.Parameters.AddWithValue("@SaleDate", ToSqlDate(saleDate));
                    _ = command.Parameters.AddWithValue("@PaymentMethod", paymentMethods[rnd.Next(paymentMethods.Length)]);
                    _ = command.Parameters.AddWithValue("@ShiftID", shiftId.HasValue ? shiftId.Value : DBNull.Value);
                    _ = command.Parameters.AddWithValue("@Notes", "فاتورة تجريبية");
                    _ = command.ExecuteNonQuery();

                    long saleId = connection.LastInsertRowId;

                    command.CommandText = @"
                        INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, DiscountPercent, TotalPrice)
                        VALUES (@SaleID, @ProductID, @Quantity, @UnitPrice, 0, @TotalPrice)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@SaleID", saleId);
                    _ = command.Parameters.AddWithValue("@ProductID", selectedProduct.ProductID);
                    _ = command.Parameters.AddWithValue("@Quantity", qty);
                    _ = command.Parameters.AddWithValue("@UnitPrice", selectedProduct.UnitPrice);
                    _ = command.Parameters.AddWithValue("@TotalPrice", lineTotal);
                    _ = command.ExecuteNonQuery();

                    command.CommandText = @"
                        UPDATE Sales
                        SET TotalAmount = @Total, PaidAmount = @Total, RemainingAmount = 0, ItemCount = @ItemCount
                        WHERE SaleID = @SaleID";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Total", lineTotal);
                    _ = command.Parameters.AddWithValue("@ItemCount", qty);
                    _ = command.Parameters.AddWithValue("@SaleID", saleId);
                    _ = command.ExecuteNonQuery();
                }

                int purchaseCount = GetCount(connection, "Purchases");
                for (int i = purchaseCount + 1; i <= TargetRecords; i++)
                {
                    if (supplierIds.Count == 0 || products.Count == 0) break;

                    ProductSeed selectedProduct = products[rnd.Next(products.Count)];
                    int supplierId = supplierIds[rnd.Next(supplierIds.Count)];
                    int qty = rnd.Next(5, 80);
                    decimal unitPrice = RoundAmount(Math.Max(1, selectedProduct.UnitPrice * 0.7m));
                    decimal total = RoundAmount(unitPrice * qty);

                    command.CommandText = @"
                        INSERT INTO Purchases (SupplierID, TotalAmount, PaidAmount, Discount, PaymentStatus, PurchaseDate, Notes, InvoiceNumber, ItemCount, IsImported)
                        VALUES (@SupplierID, @Total, @Total, 0, 'Paid', @Date, @Notes, @InvNum, @ItemCount, 1)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@SupplierID", supplierId);
                    _ = command.Parameters.AddWithValue("@Total", total);
                    _ = command.Parameters.AddWithValue("@Date", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(1, 180))));
                    _ = command.Parameters.AddWithValue("@Notes", "طلبية توريد تجريبية");
                    _ = command.Parameters.AddWithValue("@InvNum", $"PUR-{nowStamp}-{i:D5}");
                    _ = command.Parameters.AddWithValue("@ItemCount", qty);
                    _ = command.ExecuteNonQuery();

                    long purchaseId = connection.LastInsertRowId;

                    command.CommandText = @"
                        INSERT INTO PurchaseItems (PurchaseID, ProductID, Quantity, UnitPrice, TotalPrice)
                        VALUES (@PurchaseID, @ProductID, @Quantity, @UnitPrice, @TotalPrice)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@PurchaseID", purchaseId);
                    _ = command.Parameters.AddWithValue("@ProductID", selectedProduct.ProductID);
                    _ = command.Parameters.AddWithValue("@Quantity", qty);
                    _ = command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    _ = command.Parameters.AddWithValue("@TotalPrice", total);
                    _ = command.ExecuteNonQuery();

                    command.CommandText = "UPDATE Products SET Quantity = COALESCE(Quantity, 0) + @Quantity WHERE ProductID = @ProductID";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Quantity", qty);
                    _ = command.Parameters.AddWithValue("@ProductID", selectedProduct.ProductID);
                    _ = command.ExecuteNonQuery();
                }
                int promotionCount = GetCount(connection, "Promotions");
                for (int i = promotionCount + 1; i <= TargetRecords; i++)
                {
                    string appliesTo = rnd.Next(100) switch
                    {
                        < 50 => "All",
                        < 80 => "Category",
                        _ => "Product"
                    };

                    int targetId = 0;
                    string targetName = null;
                    if (appliesTo == "Category")
                    {
                        targetName = categories[rnd.Next(categories.Length)];
                    }
                    else if (appliesTo == "Product" && products.Count > 0)
                    {
                        ProductSeed selectedProduct = products[rnd.Next(products.Count)];
                        targetId = selectedProduct.ProductID;
                        targetName = selectedProduct.Name;
                    }

                    string discountType = rnd.Next(100) < 70 ? "Percentage" : "Fixed";
                    decimal discountValue = discountType == "Percentage"
                        ? RoundAmount((decimal)(5 + rnd.NextDouble() * 35))
                        : RoundAmount((decimal)(2 + rnd.NextDouble() * 120));
                    DateTime startDate = DateTime.Now.AddDays(-rnd.Next(1, 120));
                    DateTime endDate = startDate.AddDays(rnd.Next(15, 120));

                    command.CommandText = @"
                        INSERT INTO Promotions (Name, DiscountType, DiscountValue, StartDate, EndDate, MinPurchase, AppliesTo, TargetID, TargetName, IsActive, CreatedDate)
                        VALUES (@Name, @DiscountType, @DiscountValue, @StartDate, @EndDate, @MinPurchase, @AppliesTo, @TargetID, @TargetName, @IsActive, @CreatedDate)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Name", $"عرض تجريبي {i}");
                    _ = command.Parameters.AddWithValue("@DiscountType", discountType);
                    _ = command.Parameters.AddWithValue("@DiscountValue", discountValue);
                    _ = command.Parameters.AddWithValue("@StartDate", ToSqlDate(startDate));
                    _ = command.Parameters.AddWithValue("@EndDate", ToSqlDate(endDate));
                    _ = command.Parameters.AddWithValue("@MinPurchase", rnd.Next(0, 2000));
                    _ = command.Parameters.AddWithValue("@AppliesTo", appliesTo);
                    _ = command.Parameters.AddWithValue("@TargetID", targetId);
                    _ = command.Parameters.AddWithValue("@TargetName", string.IsNullOrWhiteSpace(targetName) ? DBNull.Value : targetName);
                    _ = command.Parameters.AddWithValue("@IsActive", endDate >= DateTime.Now ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CreatedDate", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(1, 120))));
                    _ = command.ExecuteNonQuery();
                }

                List<int> saleIds = GetIds(connection, "Sales", "SaleID");
                List<int> purchaseIds = GetIds(connection, "Purchases", "PurchaseID");
                List<int> promotionIds = GetIds(connection, "Promotions", "PromotionID");

                int notificationCount = GetCount(connection, "Notifications");
                string[] notifTypes = ["Info", "Warning", "Success"];
                string[] notifSources = ["System", "Inventory", "Sales", "Purchases"];
                string[] entities = ["Product", "Sale", "Purchase", "Customer", "Supplier", "Promotion"];
                for (int i = notificationCount + 1; i <= TargetRecords; i++)
                {
                    string entity = entities[rnd.Next(entities.Length)];
                    int? relatedId = entity switch
                    {
                        "Product" when products.Count > 0 => products[rnd.Next(products.Count)].ProductID,
                        "Sale" when saleIds.Count > 0 => saleIds[rnd.Next(saleIds.Count)],
                        "Purchase" when purchaseIds.Count > 0 => purchaseIds[rnd.Next(purchaseIds.Count)],
                        "Customer" when customerIds.Count > 0 => customerIds[rnd.Next(customerIds.Count)],
                        "Supplier" when supplierIds.Count > 0 => supplierIds[rnd.Next(supplierIds.Count)],
                        "Promotion" when promotionIds.Count > 0 => promotionIds[rnd.Next(promotionIds.Count)],
                        _ => null
                    };

                    command.CommandText = @"
                        INSERT INTO Notifications (Title, Message, Type, Source, IsRead, CreatedAt, RelatedEntity, RelatedID)
                        VALUES (@Title, @Message, @Type, @Source, @IsRead, @CreatedAt, @RelatedEntity, @RelatedID)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@Title", $"إشعار تجريبي #{i}");
                    _ = command.Parameters.AddWithValue("@Message", $"رسالة تنبيه تجريبية مرتبطة بـ {entity}");
                    _ = command.Parameters.AddWithValue("@Type", notifTypes[rnd.Next(notifTypes.Length)]);
                    _ = command.Parameters.AddWithValue("@Source", notifSources[rnd.Next(notifSources.Length)]);
                    _ = command.Parameters.AddWithValue("@IsRead", rnd.Next(100) < 35 ? 1 : 0);
                    _ = command.Parameters.AddWithValue("@CreatedAt", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(0, 120))));
                    _ = command.Parameters.AddWithValue("@RelatedEntity", entity);
                    _ = command.Parameters.AddWithValue("@RelatedID", relatedId.HasValue ? relatedId.Value : DBNull.Value);
                    _ = command.ExecuteNonQuery();
                }

                int logCount = GetCount(connection, "ActivityLogs");
                for (int i = logCount + 1; i <= TargetRecords; i++)
                {
                    if (userIds.Count == 0) break;
                    string action = logActions[rnd.Next(logActions.Length)];
                    command.CommandText = @"
                        INSERT INTO ActivityLogs (UserID, Action, Details, LogDate)
                        VALUES (@UserID, @Action, @Details, @LogDate)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@UserID", userIds[rnd.Next(userIds.Count)]);
                    _ = command.Parameters.AddWithValue("@Action", action);
                    _ = command.Parameters.AddWithValue("@Details", $"{action} سجل تجريبي #{i}");
                    _ = command.Parameters.AddWithValue("@LogDate", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(0, 120))));
                    _ = command.ExecuteNonQuery();
                }

                int suspendedCount = GetCount(connection, "SuspendedSales");
                for (int i = suspendedCount + 1; i <= TargetRecords; i++)
                {
                    if (products.Count == 0) break;
                    ProductSeed selectedProduct = products[rnd.Next(products.Count)];
                    int qty = rnd.Next(1, 4);
                    decimal lineTotal = RoundAmount(selectedProduct.UnitPrice * qty);

                    int? customerId = customerIds.Count == 0 ? null : customerIds[rnd.Next(customerIds.Count)];
                    int? createdBy = userIds.Count == 0 ? null : userIds[rnd.Next(userIds.Count)];
                    int? shiftId = shiftIds.Count == 0 ? null : shiftIds[rnd.Next(shiftIds.Count)];

                    command.CommandText = @"
                        INSERT INTO SuspendedSales (CustomerID, Notes, Discount, Tax, PaymentMethod, CreatedAt, CreatedBy, ShiftID, Subtotal)
                        VALUES (@CustomerID, @Notes, 0, 0, @PaymentMethod, @CreatedAt, @CreatedBy, @ShiftID, @Subtotal)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@CustomerID", customerId.HasValue ? customerId.Value : DBNull.Value);
                    _ = command.Parameters.AddWithValue("@Notes", "فاتورة معلقة تجريبية");
                    _ = command.Parameters.AddWithValue("@PaymentMethod", paymentMethods[rnd.Next(paymentMethods.Length)]);
                    _ = command.Parameters.AddWithValue("@CreatedAt", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(0, 90))));
                    _ = command.Parameters.AddWithValue("@CreatedBy", createdBy.HasValue ? createdBy.Value : DBNull.Value);
                    _ = command.Parameters.AddWithValue("@ShiftID", shiftId.HasValue ? shiftId.Value : DBNull.Value);
                    _ = command.Parameters.AddWithValue("@Subtotal", lineTotal);
                    _ = command.ExecuteNonQuery();

                    long suspendedSaleId = connection.LastInsertRowId;
                    command.CommandText = @"
                        INSERT INTO SuspendedSaleItems (SuspendedSaleID, ProductID, ProductName, Quantity, UnitPrice, DiscountPercent, TotalPrice)
                        VALUES (@SuspendedSaleID, @ProductID, @ProductName, @Quantity, @UnitPrice, 0, @TotalPrice)";
                    command.Parameters.Clear();
                    _ = command.Parameters.AddWithValue("@SuspendedSaleID", suspendedSaleId);
                    _ = command.Parameters.AddWithValue("@ProductID", selectedProduct.ProductID);
                    _ = command.Parameters.AddWithValue("@ProductName", selectedProduct.Name);
                    _ = command.Parameters.AddWithValue("@Quantity", qty);
                    _ = command.Parameters.AddWithValue("@UnitPrice", selectedProduct.UnitPrice);
                    _ = command.Parameters.AddWithValue("@TotalPrice", lineTotal);
                    _ = command.ExecuteNonQuery();
                }

                int returnsCount = GetCount(connection, "Returns");
                int returnItemsCount = GetCount(connection, "ReturnItems");
                int returnsToAdd = Math.Max(TargetRecords - returnsCount, TargetRecords - returnItemsCount);
                if (returnsToAdd > 0)
                {
                    List<SaleItemSeed> saleItemSeeds = GetSaleItemSeeds(connection);
                    string[] reasons = ["تالف", "خطأ في الطلب", "إرجاع عميل", "منتج غير مطابق"];

                    for (int i = 0; i < returnsToAdd && saleItemSeeds.Count > 0; i++)
                    {
                        SaleItemSeed selected = saleItemSeeds[rnd.Next(saleItemSeeds.Count)];
                        if (selected.RemainingQuantity <= 0)
                        {
                            continue;
                        }

                        int qty = 1;
                        decimal refund = RoundAmount(selected.UnitPrice * qty);
                        int? shiftId = shiftIds.Count == 0 ? null : shiftIds[rnd.Next(shiftIds.Count)];
                        int? createdBy = userIds.Count == 0 ? null : userIds[rnd.Next(userIds.Count)];

                        command.CommandText = @"
                            INSERT INTO Returns (SaleID, ShiftID, ReturnDate, Reason, TotalRefund, CreatedBy, CreatedDate)
                            VALUES (@SaleID, @ShiftID, @ReturnDate, @Reason, @TotalRefund, @CreatedBy, @CreatedDate)";
                        command.Parameters.Clear();
                        _ = command.Parameters.AddWithValue("@SaleID", selected.SaleID);
                        _ = command.Parameters.AddWithValue("@ShiftID", shiftId.HasValue ? shiftId.Value : DBNull.Value);
                        _ = command.Parameters.AddWithValue("@ReturnDate", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(0, 90))));
                        _ = command.Parameters.AddWithValue("@Reason", reasons[rnd.Next(reasons.Length)]);
                        _ = command.Parameters.AddWithValue("@TotalRefund", refund);
                        _ = command.Parameters.AddWithValue("@CreatedBy", createdBy.HasValue ? createdBy.Value : DBNull.Value);
                        _ = command.Parameters.AddWithValue("@CreatedDate", ToSqlDate(DateTime.Now.AddDays(-rnd.Next(0, 90))));
                        _ = command.ExecuteNonQuery();

                        long returnId = connection.LastInsertRowId;
                        command.CommandText = @"
                            INSERT INTO ReturnItems (ReturnID, SaleItemID, ProductID, Quantity, UnitPrice, DiscountPercent, RefundAmount)
                            VALUES (@ReturnID, @SaleItemID, @ProductID, @Quantity, @UnitPrice, 0, @RefundAmount)";
                        command.Parameters.Clear();
                        _ = command.Parameters.AddWithValue("@ReturnID", returnId);
                        _ = command.Parameters.AddWithValue("@SaleItemID", selected.SaleItemID);
                        _ = command.Parameters.AddWithValue("@ProductID", selected.ProductID);
                        _ = command.Parameters.AddWithValue("@Quantity", qty);
                        _ = command.Parameters.AddWithValue("@UnitPrice", selected.UnitPrice);
                        _ = command.Parameters.AddWithValue("@RefundAmount", refund);
                        _ = command.ExecuteNonQuery();

                        command.CommandText = "UPDATE SaleItems SET ReturnedQuantity = COALESCE(ReturnedQuantity, 0) + @Qty WHERE SaleItemID = @SaleItemID";
                        command.Parameters.Clear();
                        _ = command.Parameters.AddWithValue("@Qty", qty);
                        _ = command.Parameters.AddWithValue("@SaleItemID", selected.SaleItemID);
                        _ = command.ExecuteNonQuery();

                        command.CommandText = "UPDATE Sales SET ReturnedAmount = COALESCE(ReturnedAmount, 0) + @Refund WHERE SaleID = @SaleID";
                        command.Parameters.Clear();
                        _ = command.Parameters.AddWithValue("@Refund", refund);
                        _ = command.Parameters.AddWithValue("@SaleID", selected.SaleID);
                        _ = command.ExecuteNonQuery();

                        selected.RemainingQuantity -= qty;
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception("فشل في إدراج البيانات: " + ex.Message);
            }
        }
        private static int GetCount(SQLiteConnection connection, string tableName)
        {
            using var command = new SQLiteCommand($"SELECT COUNT(*) FROM {tableName}", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static List<int> GetIds(SQLiteConnection connection, string tableName, string idColumn)
        {
            var ids = new List<int>();
            using var command = new SQLiteCommand($"SELECT {idColumn} FROM {tableName}", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                ids.Add(Convert.ToInt32(reader[0]));
            }
            return ids;
        }

        private static List<ProductSeed> GetProductSeeds(SQLiteConnection connection)
        {
            var items = new List<ProductSeed>();
            using var command = new SQLiteCommand("SELECT ProductID, Name, COALESCE(NULLIF(SellingPrice, 0), Price, 1) FROM Products", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new ProductSeed
                {
                    ProductID = Convert.ToInt32(reader[0]),
                    Name = reader[1]?.ToString() ?? "منتج",
                    UnitPrice = Math.Max(1m, Convert.ToDecimal(reader[2]))
                });
            }
            return items;
        }

        private static List<SaleItemSeed> GetSaleItemSeeds(SQLiteConnection connection)
        {
            var items = new List<SaleItemSeed>();
            using var command = new SQLiteCommand("SELECT SaleItemID, SaleID, ProductID, Quantity, COALESCE(ReturnedQuantity, 0), COALESCE(UnitPrice, 1) FROM SaleItems", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int quantity = Convert.ToInt32(reader[3]);
                int returned = Convert.ToInt32(reader[4]);
                items.Add(new SaleItemSeed
                {
                    SaleItemID = Convert.ToInt32(reader[0]),
                    SaleID = Convert.ToInt32(reader[1]),
                    ProductID = Convert.ToInt32(reader[2]),
                    RemainingQuantity = Math.Max(0, quantity - returned),
                    UnitPrice = Math.Max(1m, Convert.ToDecimal(reader[5]))
                });
            }
            return items;
        }

        private static string ToSqlDate(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static decimal RoundAmount(decimal value)
        {
            return Math.Round(value, 2);
        }

        private sealed class ProductSeed
        {
            public int ProductID { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
        }

        private sealed class SaleItemSeed
        {
            public int SaleItemID { get; set; }
            public int SaleID { get; set; }
            public int ProductID { get; set; }
            public int RemainingQuantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}
