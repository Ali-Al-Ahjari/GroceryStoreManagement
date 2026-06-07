using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using GroceryStoreManagement.Helpers;
using System.Threading.Tasks;

namespace GroceryStoreManagement.DAL
{
    public static class CustomerDAL
    {
        public static List<Customer> GetAllCustomers()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT c.*,
                               CAST(COALESCE(SUM((s.TotalAmount - s.Discount) + (s.TotalAmount * s.Tax / 100.0)), 0) AS REAL) as TotalPurchases,
                               CAST(COALESCE(SUM(s.RemainingAmount), 0) AS REAL) as CurrentDebt,
                               COUNT(s.SaleID) as PurchaseCount
                        FROM Customers c
                        LEFT JOIN Sales s ON c.CustomerID = s.CustomerID
                        GROUP BY c.CustomerID
                        ORDER BY c.Name";

                return [.. connection.Query<Customer>(query)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب العملاء: {ex.Message}", ex);
            }
        }

        public static Task<List<Customer>> GetAllCustomersAsync()
        {
            return Task.Run(GetAllCustomers);
        }

        public static Customer GetCustomerById(int customerId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT c.*,
                               CAST(COALESCE(SUM((s.TotalAmount - s.Discount) + (s.TotalAmount * s.Tax / 100.0)), 0) AS REAL) as TotalPurchases,
                               CAST(COALESCE(SUM(s.RemainingAmount), 0) AS REAL) as CurrentDebt,
                               COUNT(s.SaleID) as PurchaseCount
                        FROM Customers c
                        LEFT JOIN Sales s ON c.CustomerID = s.CustomerID
                        WHERE c.CustomerID = @CustomerID
                        GROUP BY c.CustomerID";

                return connection.QueryFirstOrDefault<Customer>(query, new { CustomerID = customerId });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب العميل: {ex.Message}", ex);
            }
        }

        public static List<Customer> SearchCustomers(string searchTerm)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT c.*,
                               CAST(COALESCE(SUM((s.TotalAmount - s.Discount) + (s.TotalAmount * s.Tax / 100.0)), 0) AS REAL) as TotalPurchases,
                               CAST(COALESCE(SUM(s.RemainingAmount), 0) AS REAL) as CurrentDebt,
                               COUNT(s.SaleID) as PurchaseCount
                        FROM Customers c
                        LEFT JOIN Sales s ON c.CustomerID = s.CustomerID
                        WHERE c.Name LIKE @SearchTerm 
                           OR c.Phone LIKE @SearchTerm 
                           OR c.Email LIKE @SearchTerm
                        GROUP BY c.CustomerID
                        ORDER BY c.Name";

                return [.. connection.Query<Customer>(query, new { SearchTerm = $"%{searchTerm}%" })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في بحث العملاء: {ex.Message}", ex);
            }
        }

        public static Task<List<Customer>> SearchCustomersAsync(string searchTerm)
        {
            return Task.Run(() => SearchCustomers(searchTerm));
        }

        public static int AddCustomer(Customer customer)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تعبئة حقول التدقيق
                Helpers.AuditHelper.SetFullAudit(customer);

                string query = @"
                        INSERT INTO Customers (Name, Phone, Email, Address, Notes, IsActive, CreditLimit, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                        VALUES (@Name, @Phone, @Email, @Address, @Notes, @IsActive, @CreditLimit, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy);
                        SELECT last_insert_rowid();";

                int newId = connection.ExecuteScalar<int>(query, new
                {
                    customer.Name,
                    customer.Phone,
                    customer.Email,
                    customer.Address,
                    customer.Notes,
                    customer.IsActive,
                    customer.CreditLimit,
                    customer.CreatedDate,
                    customer.CreatedBy,
                    customer.ModifiedDate,
                    customer.ModifiedBy
                });

                // تسجيل النشاط
                ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "تسجيل عميل جديد", $"تم إضافة العميل: {customer.Name}");

                return newId;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة العميل: {ex.Message}", ex);
            }
        }

        public static bool UpdateCustomer(Customer customer)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تعبئة حقول التعديل
                Helpers.AuditHelper.SetModificationAudit(customer);

                string query = @"
                        UPDATE Customers 
                        SET Name = @Name, 
                            Phone = @Phone, 
                            Email = @Email, 
                            Address = @Address,
                            Notes = @Notes,
                            IsActive = @IsActive,
                            CreditLimit = @CreditLimit,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE CustomerID = @CustomerID";

                int rowsAffected = connection.Execute(query, new
                {
                    customer.CustomerID,
                    customer.Name,
                    customer.Phone,
                    customer.Email,
                    customer.Address,
                    customer.Notes,
                    customer.IsActive,
                    customer.CreditLimit,
                    customer.ModifiedDate,
                    customer.ModifiedBy
                });

                if (rowsAffected > 0)
                {
                    // تسجيل النشاط
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "تعديل عميل", $"تم تعديل العميل: {customer.Name}");
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث العميل: {ex.Message}", ex);
            }
        }

        public static bool DeleteCustomer(int customerId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // التحقق من عدم وجود مبيعات للعميل
                string checkQuery = "SELECT COUNT(*) FROM Sales WHERE CustomerID = @CustomerID";
                int saleCount = connection.ExecuteScalar<int>(checkQuery, new { CustomerID = customerId });

                if (saleCount > 0)
                {
                    throw new Exception("لا يمكن حذف العميل لأنه مرتبط بمبيعات سابقة");
                }

                string query = "DELETE FROM Customers WHERE CustomerID = @CustomerID";
                int rowsAffected = connection.Execute(query, new { CustomerID = customerId });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف العميل: {ex.Message}", ex);
            }
        }

        public static List<Customer> GetTopCustomers(int limit = 10)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT 
                            c.CustomerID,
                            c.Name,
                            c.Phone,
                            c.Email,
                            c.Address,
                            CAST(COALESCE(SUM((s.TotalAmount - s.Discount) + (s.TotalAmount * s.Tax / 100.0)), 0) AS REAL) as TotalPurchases,
                            COUNT(s.SaleID) as PurchaseCount
                        FROM Customers c
                        LEFT JOIN Sales s ON c.CustomerID = s.CustomerID
                        GROUP BY c.CustomerID
                        HAVING TotalPurchases > 0
                        ORDER BY TotalPurchases DESC
                        LIMIT @Limit";

                return [.. connection.Query<Customer>(query, new { Limit = limit })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب العملاء الأكثر شراءً: {ex.Message}", ex);
            }
        }

        public static int GetTotalCustomersCount()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "SELECT COUNT(*) FROM Customers";
                return connection.ExecuteScalar<int>(query);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب عدد العملاء: {ex.Message}", ex);
            }
        }

        public static bool CustomerExists(string phone)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "SELECT COUNT(*) FROM Customers WHERE Phone = @Phone";
                int count = connection.ExecuteScalar<int>(query, new { Phone = phone });
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التحقق من وجود العميل: {ex.Message}", ex);
            }
        }
        public static bool UpdateCustomerDebt(int customerId, decimal amount)
        {
            try
            {
                var result = ApplyDebtPayment(customerId, Math.Abs(amount), "Cash", SessionContext.CurrentUserID);
                return result.AppliedAmount > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"خطأ في تحديث دين العميل: {customerId}");
                return false;
            }
        }

        /// <summary>
        /// تسديد ديون العميل وتوزيع المبلغ تلقائياً على الفواتير المفتوحة (الأقدم استحقاقاً ثم الأقدم تاريخاً).
        /// </summary>
        public static DebtPaymentResult ApplyDebtPayment(int customerId, decimal paymentAmount, string paymentMethod, int? userId = null)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException("معرف العميل غير صالح.");
            }

            if (paymentAmount <= 0)
            {
                throw new ArgumentException("مبلغ السداد يجب أن يكون أكبر من صفر.");
            }

            string normalizedPaymentMethod = NormalizePaymentMethod(paymentMethod);
            int actorUserId = userId ?? SessionContext.CurrentUserID;
            DateTime now = DateTime.Now;

            using var connection = Helpers.DatabaseHelper.GetConnection();
            using var transaction = connection.BeginTransaction();

            var result = new DebtPaymentResult
            {
                CustomerID = customerId,
                RequestedAmount = paymentAmount,
                PaymentMethod = normalizedPaymentMethod
            };

            try
            {
                var openInvoices = connection.Query<DebtSaleRow>(@"
                        SELECT
                            SaleID, SaleDate, DueDate, TotalAmount, Discount, Tax,
                            PaidAmount, COALESCE(ReturnedAmount, 0) AS ReturnedAmount
                        FROM Sales
                        WHERE CustomerID = @CustomerID
                          AND (((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)) - PaidAmount - COALESCE(ReturnedAmount, 0)) > 0.0001
                        ORDER BY CASE WHEN DueDate IS NULL THEN 1 ELSE 0 END, DueDate ASC, SaleDate ASC;",
                    new { CustomerID = customerId }, transaction).ToList();

                if (openInvoices.Count == 0)
                {
                    throw new Exception("لا توجد فواتير مدينة لهذا العميل.");
                }

                decimal remainingToApply = paymentAmount;

                foreach (var invoice in openInvoices)
                {
                    if (remainingToApply <= 0)
                    {
                        break;
                    }

                    decimal netTotal = invoice.TotalAmount - invoice.Discount + (invoice.TotalAmount * (invoice.Tax / 100m));
                    decimal previousRemaining = netTotal - invoice.PaidAmount - invoice.ReturnedAmount;
                    if (previousRemaining <= 0)
                    {
                        continue;
                    }

                    decimal appliedAmount = Math.Min(remainingToApply, previousRemaining);
                    decimal newPaidAmount = invoice.PaidAmount + appliedAmount;
                    decimal newRemaining = previousRemaining - appliedAmount;
                    string newPaymentStatus = newRemaining <= 0.0001m
                        ? "Paid"
                        : newPaidAmount > 0 ? "Partial" : "Unpaid";

                    _ = connection.Execute(@"
                            UPDATE Sales
                            SET PaidAmount = @PaidAmount,
                                RemainingAmount = @RemainingAmount,
                                PaymentStatus = @PaymentStatus,
                                PaymentMethod = @PaymentMethod,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy
                            WHERE SaleID = @SaleID;",
                        new
                        {
                            PaidAmount = newPaidAmount,
                            RemainingAmount = Math.Max(0, newRemaining),
                            PaymentStatus = newPaymentStatus,
                            PaymentMethod = normalizedPaymentMethod,
                            ModifiedDate = now,
                            ModifiedBy = actorUserId,
                            SaleID = invoice.SaleID
                        }, transaction);

                    remainingToApply -= appliedAmount;
                    result.AppliedAmount += appliedAmount;
                    result.AffectedInvoicesCount++;
                    result.Allocations.Add(new DebtPaymentAllocation
                    {
                        SaleID = invoice.SaleID,
                        AppliedAmount = appliedAmount,
                        PreviousRemaining = previousRemaining,
                        RemainingAfterAllocation = Math.Max(0, newRemaining)
                    });
                }

                result.UnappliedAmount = Math.Max(0, remainingToApply);
                result.TotalRemainingAfterPayment = connection.ExecuteScalar<decimal>(@"
                        SELECT COALESCE(SUM(((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)) - PaidAmount - COALESCE(ReturnedAmount, 0)), 0)
                        FROM Sales
                        WHERE CustomerID = @CustomerID
                          AND (((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)) - PaidAmount - COALESCE(ReturnedAmount, 0)) > 0.0001;",
                    new { CustomerID = customerId }, transaction);

                transaction.Commit();
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    Logger.LogError(rollbackEx, $"فشل التراجع عن سداد ديون العميل رقم {customerId}");
                }

                throw;
            }

            try
            {
                ActivityLogDAL.AddLog(actorUserId, "سداد دين",
                    $"تم تسجيل سداد {result.AppliedAmount.ToDisplayCurrency()} للعميل رقم {customerId} على {result.AffectedInvoicesCount} فاتورة. طريقة الدفع: {result.PaymentMethod}");
            }
            catch (Exception logEx)
            {
                Logger.LogWarning($"تم السداد بنجاح لكن تعذر تسجيل سجل النشاط: {logEx.Message}");
            }

            return result;
        }

        private static string NormalizePaymentMethod(string paymentMethod)
        {
            return paymentMethod switch
            {
                "Cash" => "Cash",
                "Card" => "Card",
                "Transfer" => "Transfer",
                _ => "Cash"
            };
        }

        private sealed class DebtSaleRow
        {
            public int SaleID { get; set; }
            public DateTime SaleDate { get; set; }
            public DateTime? DueDate { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal Discount { get; set; }
            public decimal Tax { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal ReturnedAmount { get; set; }
        }
    }
}

