using Dapper; // استيراد Dapper للتعامل السهل مع SQL
using GroceryStoreManagement.Models; // استيراد النماذج
using System; // استيراد الوظائف الأساسية
using System.Collections.Generic; // استيراد القوائم
using System.Data.SQLite; // استيراد SQLite
using System.Linq; // استيراد Linq
using System.Threading.Tasks;

namespace GroceryStoreManagement.DAL // مجال الوصول للبيانات
{
    // كلاس إدارة عمليات قاعدة البيانات للفواتير والمبيعات
    public static class SaleDAL
    {
        // دالة لجلب جميع الفواتير مرتبة من الأحدث للأقدم
        // تتضمن بيانات العميل وعدد العناصر في كل فاتورة
        public static List<Sale> GetAllSales()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // استعلام يربط جدول المبيعات بالعملاء وتفاصيل المبيعات (للعد فقط)
                string query = @"
                        SELECT s.*, c.Name as CustomerName,
                               COUNT(si.SaleItemID) as ItemCount
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                        LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                        GROUP BY s.SaleID
                        ORDER BY s.SaleDate DESC";

                return [.. connection.Query<Sale>(query)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المبيعات: {ex.Message}", ex);
            }
        }

        public static Task<List<Sale>> GetAllSalesAsync()
        {
            return Task.Run(GetAllSales);
        }

        // دالة لجلب تفاصيل فاتورة واحدة محددة برقم المعرف
        public static Sale GetSaleById(int saleId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*, c.Name as CustomerName,
                               COUNT(si.SaleItemID) as ItemCount
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                        LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                        WHERE s.SaleID = @SaleID
                        GROUP BY s.SaleID";

                return connection.QueryFirstOrDefault<Sale>(query, new { SaleID = saleId });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب الفاتورة: {ex.Message}", ex);
            }
        }

        // دالة لجلب فواتير يوم محدد
        public static List<Sale> GetSalesByDate(DateTime date)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*, c.Name as CustomerName,
                               COUNT(si.SaleItemID) as ItemCount
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                        LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                        WHERE DATE(s.SaleDate) = DATE(@SaleDate)
                        GROUP BY s.SaleID
                        ORDER BY s.SaleDate DESC";

                return [.. connection.Query<Sale>(query, new { SaleDate = date })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب مبيعات اليوم: {ex.Message}", ex);
            }
        }

        // دالة لجلب جميع فواتير عميل معين
        public static List<Sale> GetSalesByCustomer(int customerId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*, c.Name as CustomerName,
                               COUNT(si.SaleItemID) as ItemCount
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                        LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                        WHERE s.CustomerID = @CustomerID
                        GROUP BY s.SaleID
                        ORDER BY s.SaleDate DESC";

                return [.. connection.Query<Sale>(query, new { CustomerID = customerId })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب مبيعات العميل: {ex.Message}", ex);
            }
        }

        // دالة لجلب الفواتير خلال فترة زمنية محددة (من تاريخ - إلى تاريخ)
        // تستخدم في التقارير بشكل أساسي
        public static List<Sale> GetSalesByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*, c.Name as CustomerName,
                               COUNT(si.SaleItemID) as ItemCount
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                        LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                        WHERE DATE(s.SaleDate) BETWEEN DATE(@StartDate) AND DATE(@EndDate)
                        GROUP BY s.SaleID
                        ORDER BY s.SaleDate DESC";

                return [.. connection.Query<Sale>(query, new
                {
                    StartDate = startDate,
                    EndDate = endDate
                })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المبيعات حسب النطاق الزمني: {ex.Message}", ex);
            }
        }

        public static Task<List<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return Task.Run(() => GetSalesByDateRange(startDate, endDate));
        }

        // دالة لإضافة فاتورة جديدة
        // تقوم بإدراج الرأس (Sale) وإرجاع الرقم المعرف لإضافة التفاصيل لاحقاً
        public static int AddSale(Sale sale)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تعبئة حقول التدقيق (الإنشاء فقط لأنها فاتورة جديدة)
                Helpers.AuditHelper.SetCreationAudit(sale);

                // إذا لم يتم تحديد تاريخ، نستخدم الوقت الحالي
                if (sale.SaleDate == default)
                    sale.SaleDate = DateTime.Now;

                NormalizeAndValidateSale(sale);

                string query = @"
                        INSERT INTO Sales (
                            CustomerID, ShiftID, TotalAmount, SaleDate, 
                            PaidAmount, Discount, Tax, 
                            PaymentStatus, PaymentMethod, Notes, 
                            ItemCount, RemainingAmount, DueDate,
                            CreatedBy, CreatedDate)
                        VALUES (
                            @CustomerID, @ShiftID, @TotalAmount, @SaleDate, 
                            @PaidAmount, @Discount, @Tax, 
                            @PaymentStatus, @PaymentMethod, @Notes, 
                            @ItemCount, @RemainingAmount, @DueDate,
                            @CreatedBy, @CreatedDate);
                        SELECT last_insert_rowid();";

                var id = connection.ExecuteScalar<int>(query, new
                {
                    sale.CustomerID,
                    sale.ShiftID,
                    sale.TotalAmount,
                    sale.SaleDate,
                    sale.PaidAmount,
                    sale.Discount,
                    sale.Tax,
                    sale.PaymentStatus,
                    sale.PaymentMethod,
                    sale.Notes,
                    sale.ItemCount,
                    sale.RemainingAmount,
                    sale.DueDate,
                    sale.CreatedBy,
                    sale.CreatedDate
                });

                // تسجيل العملية
                Helpers.Logger.LogInfo($"تم إنشاء فاتورة مبيعات جديدة بقيمة {sale.TotalAmount.ToDisplayCurrency()}");
                return id;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة الفاتورة: {ex.Message}", ex);
            }
        }

        // دالة لتعديل بيانات فاتورة (مثل تغيير العميل أو الإجمالي)
        public static bool UpdateSale(Sale sale)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                NormalizeAndValidateSale(sale);
                string query = @"
                        UPDATE Sales 
                        SET CustomerID = @CustomerID, 
                            ShiftID = @ShiftID,
                            TotalAmount = @TotalAmount,
                            SaleDate = @SaleDate,
                            DueDate = @DueDate,
                            PaidAmount = @PaidAmount,
                            Discount = @Discount,
                            Tax = @Tax,
                            PaymentStatus = @PaymentStatus,
                            PaymentMethod = @PaymentMethod,
                            Notes = @Notes,
                            ItemCount = @ItemCount,
                            RemainingAmount = @RemainingAmount,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE SaleID = @SaleID";

                Helpers.AuditHelper.SetModificationAudit(sale);

                int rowsAffected = connection.Execute(query, new
                {
                    sale.SaleID,
                    sale.CustomerID,
                    sale.ShiftID,
                    sale.TotalAmount,
                    sale.SaleDate,
                    sale.DueDate,
                    sale.PaidAmount,
                    sale.Discount,
                    sale.Tax,
                    sale.PaymentStatus,
                    sale.PaymentMethod,
                    sale.Notes,
                    sale.ItemCount,
                    sale.RemainingAmount,
                    sale.ModifiedDate,
                    sale.ModifiedBy
                });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث الفاتورة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// حفظ الفاتورة وعناصرها داخل معاملة واحدة لضمان الاتساق.
        /// </summary>
        /// <param name="sale">رأس الفاتورة</param>
        /// <param name="saleItems">عناصر الفاتورة</param>
        /// <param name="isEditMode">true للتعديل، false للإضافة</param>
        /// <returns>رقم الفاتورة</returns>
        public static int SaveSaleWithItems(Sale sale, IReadOnlyCollection<SaleItem> saleItems, bool isEditMode)
        {
            if (sale is null)
            {
                throw new ArgumentNullException(nameof(sale));
            }

            if (saleItems is null || saleItems.Count == 0)
            {
                throw new ArgumentException("لا توجد عناصر في الفاتورة.", nameof(saleItems));
            }

            if (isEditMode && sale.SaleID <= 0)
            {
                throw new ArgumentException("معرف الفاتورة غير صالح للتعديل.", nameof(sale));
            }

            if (!isEditMode && sale.SaleDate == default)
            {
                sale.SaleDate = DateTime.Now;
            }

            NormalizeAndValidateSale(sale);

            using var connection = Helpers.DatabaseHelper.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                int saleId = sale.SaleID;

                if (isEditMode)
                {
                    Helpers.AuditHelper.SetModificationAudit(sale);
                    int updatedRows = connection.Execute(@"
                            UPDATE Sales 
                            SET CustomerID = @CustomerID, 
                                ShiftID = @ShiftID,
                                TotalAmount = @TotalAmount,
                                SaleDate = @SaleDate,
                                DueDate = @DueDate,
                                PaidAmount = @PaidAmount,
                                Discount = @Discount,
                                Tax = @Tax,
                                PaymentStatus = @PaymentStatus,
                                PaymentMethod = @PaymentMethod,
                                Notes = @Notes,
                                ItemCount = @ItemCount,
                                RemainingAmount = @RemainingAmount,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy
                            WHERE SaleID = @SaleID;",
                        new
                        {
                            sale.SaleID,
                            sale.CustomerID,
                            sale.ShiftID,
                            sale.TotalAmount,
                            sale.SaleDate,
                            sale.DueDate,
                            sale.PaidAmount,
                            sale.Discount,
                            sale.Tax,
                            sale.PaymentStatus,
                            sale.PaymentMethod,
                            sale.Notes,
                            sale.ItemCount,
                            RemainingAmount = Math.Max(0, sale.RemainingAmount),
                            sale.ModifiedDate,
                            sale.ModifiedBy
                        }, transaction);

                    if (updatedRows == 0)
                    {
                        throw new Exception("الفاتورة غير موجودة أو لم يتم تحديثها.");
                    }

                    _ = connection.Execute("DELETE FROM SaleItems WHERE SaleID = @SaleID;", new { SaleID = saleId }, transaction);
                }
                else
                {
                    Helpers.AuditHelper.SetCreationAudit(sale);
                    saleId = connection.ExecuteScalar<int>(@"
                            INSERT INTO Sales (
                                CustomerID, ShiftID, TotalAmount, SaleDate, 
                                PaidAmount, Discount, Tax, 
                                PaymentStatus, PaymentMethod, Notes, 
                                ItemCount, RemainingAmount, DueDate,
                                CreatedBy, CreatedDate)
                            VALUES (
                                @CustomerID, @ShiftID, @TotalAmount, @SaleDate, 
                                @PaidAmount, @Discount, @Tax, 
                                @PaymentStatus, @PaymentMethod, @Notes, 
                                @ItemCount, @RemainingAmount, @DueDate,
                                @CreatedBy, @CreatedDate);
                            SELECT last_insert_rowid();",
                        new
                        {
                            sale.CustomerID,
                            sale.ShiftID,
                            sale.TotalAmount,
                            sale.SaleDate,
                            sale.PaidAmount,
                            sale.Discount,
                            sale.Tax,
                            sale.PaymentStatus,
                            sale.PaymentMethod,
                            sale.Notes,
                            sale.ItemCount,
                            RemainingAmount = Math.Max(0, sale.RemainingAmount),
                            sale.DueDate,
                            sale.CreatedBy,
                            sale.CreatedDate
                        }, transaction);
                }

                foreach (var item in saleItems)
                {
                    if (item is null)
                    {
                        throw new Exception("تعذر حفظ عنصر فارغ داخل الفاتورة.");
                    }

                    if (item.ProductID <= 0)
                    {
                        throw new Exception("عنصر فاتورة يحتوي على منتج غير صالح.");
                    }

                    if (item.Quantity <= 0)
                    {
                        throw new Exception("كمية المنتج يجب أن تكون أكبر من صفر.");
                    }

                    if (item.UnitPrice < 0)
                    {
                        throw new Exception("سعر الوحدة لا يمكن أن يكون سالباً.");
                    }

                    if (item.DiscountPercent < 0 || item.DiscountPercent > 100)
                    {
                        throw new Exception("نسبة خصم الصنف يجب أن تكون بين 0 و 100.");
                    }

                    int? availableQuantity = connection.ExecuteScalar<int?>(@"
                            SELECT Quantity
                            FROM Products
                            WHERE ProductID = @ProductID;",
                        new { item.ProductID }, transaction);

                    if (!availableQuantity.HasValue)
                    {
                        throw new Exception($"المنتج رقم {item.ProductID} غير موجود.");
                    }

                    if (item.Quantity > availableQuantity.Value)
                    {
                        throw new Exception(
                            $"الكمية المطلوبة غير متوفرة للمنتج '{item.ProductName ?? item.ProductID.ToString()}'. " +
                            $"المتوفر: {availableQuantity.Value}، المطلوب: {item.Quantity}");
                    }

                    decimal lineTotal = item.Quantity * item.UnitPrice * (1 - item.DiscountPercent / 100m);
                    item.TotalPrice = lineTotal;
                    item.SaleID = saleId;

                    _ = connection.Execute(@"
                            INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, DiscountPercent, TotalPrice)
                            VALUES (@SaleID, @ProductID, @Quantity, @UnitPrice, @DiscountPercent, @TotalPrice);",
                        new
                        {
                            SaleID = saleId,
                            item.ProductID,
                            item.Quantity,
                            item.UnitPrice,
                            item.DiscountPercent,
                            TotalPrice = lineTotal
                        }, transaction);
                }

                transaction.Commit();

                Helpers.Logger.LogInfo(
                    isEditMode
                        ? $"تم تعديل الفاتورة #{saleId} وحفظ عناصرها بنجاح."
                        : $"تم إنشاء الفاتورة #{saleId} وحفظ عناصرها بنجاح.");

                return saleId;
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    Helpers.Logger.LogError(rollbackEx, "فشل التراجع عن عملية حفظ الفاتورة");
                }

                throw new Exception($"تعذر حفظ الفاتورة: {ex.Message}", ex);
            }
        }

        // دالة لحذف فاتورة بالكامل
        // ملاحظة: الحذف يتم بشكل متسلسل (Cascade)، نحذف التفاصيل أولاً ثم الفاتورة الرئيسية
        public static bool DeleteSale(int saleId)
        {
            using var connection = Helpers.DatabaseHelper.GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. حذف تفاصيل الفاتورة أولاً (لتجنب بقاء بيانات يتيمة)
                string deleteItemsQuery = "DELETE FROM SaleItems WHERE SaleID = @SaleID";
                _ = connection.Execute(deleteItemsQuery, new { SaleID = saleId }, transaction);

                // 2. حذف الفاتورة نفسها
                string query = "DELETE FROM Sales WHERE SaleID = @SaleID";
                int rowsAffected = connection.Execute(query, new { SaleID = saleId }, transaction);

                transaction.Commit();

                if (rowsAffected > 0)
                    Helpers.Logger.LogInfo($"تم حذف فاتورة المبيعات رقم {saleId}");

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { /* تجاهل خطأ Rollback */ }
                throw new Exception($"خطأ في حذف الفاتورة: {ex.Message}", ex);
            }
        }

        // دالة مساعدة لحساب مجموع مبيعات يوم محدد
        public static decimal GetDailySalesTotal(DateTime date)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // COALESCE لضمان إرجاع 0 بدلاً من null إذا لم تكن هناك مبيعات
                string query = @"
                        SELECT COALESCE(SUM((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)), 0)
                        FROM Sales 
                        WHERE DATE(SaleDate) = DATE(@Date)";

                return connection.ExecuteScalar<decimal>(query, new { Date = date });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب مبيعات اليوم: {ex.Message}", ex);
            }
        }

        public static List<DailySalesAggregate> GetDailySalesTotalsInRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT DATE(SaleDate) AS SaleDate,
                               COALESCE(SUM((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)), 0) AS TotalSales
                        FROM Sales
                        WHERE DATE(SaleDate) BETWEEN DATE(@StartDate) AND DATE(@EndDate)
                        GROUP BY DATE(SaleDate)
                        ORDER BY DATE(SaleDate)";

                return [.. connection.Query<DailySalesAggregate>(query, new { StartDate = startDate, EndDate = endDate })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب تجميع المبيعات اليومية: {ex.Message}", ex);
            }
        }

        public static Task<List<DailySalesAggregate>> GetDailySalesTotalsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            return Task.Run(() => GetDailySalesTotalsInRange(startDate, endDate));
        }

        // دالة لحساب المبيعات لشهر كامل (للإحصائيات الشهرية)
        public static decimal GetMonthlySalesTotal(DateTime date)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // استخدام strftime لمقارنة السنة والشهر
                string query = @"
                        SELECT COALESCE(SUM((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)), 0) 
                        FROM Sales 
                        WHERE strftime('%Y-%m', SaleDate) = strftime('%Y-%m', @Date)";

                return connection.ExecuteScalar<decimal>(query, new { Date = date });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب مبيعات الشهر: {ex.Message}", ex);
            }
        }

        // دالة لحساب المبيعات السنوية
        public static decimal GetYearlySalesTotal(DateTime date)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT COALESCE(SUM((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)), 0) 
                        FROM Sales 
                        WHERE strftime('%Y', SaleDate) = strftime('%Y', @Date)";

                return connection.ExecuteScalar<decimal>(query, new { Date = date });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب مبيعات السنة: {ex.Message}", ex);
            }
        }

        // دالة لحساب عدد الفواتير التي تمت اليوم
        public static int GetDailySalesCount(DateTime date)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT COUNT(*) 
                        FROM Sales 
                        WHERE DATE(SaleDate) = DATE(@Date)";

                return connection.ExecuteScalar<int>(query, new { Date = date });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب عدد فواتير اليوم: {ex.Message}", ex);
            }
        }

        // دالة لجلب آخر عدد محدد من الفواتير (مثلاً آخر 10 فواتير للعرض في اللوحة الرئيسية)
        public static List<Sale> GetRecentSales(int limit = 10)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*, c.Name as CustomerName,
                               COUNT(si.SaleItemID) as ItemCount
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                        LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                        GROUP BY s.SaleID
                        ORDER BY s.SaleDate DESC
                        LIMIT @Limit"; // تحديد العدد الأقصى

                return [.. connection.Query<Sale>(query, new { Limit = limit })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب أحدث المبيعات: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تحديث مبلغ الدفع وحالة الفاتورة
        /// تُستخدم لتسديد الفواتير (كلي أو جزئي)
        /// </summary>
        /// <param name="saleId">رقم الفاتورة</param>
        /// <param name="newPaidAmount">المبلغ المدفوع الجديد (الإجمالي)</param>
        /// <param name="newPaymentStatus">حالة الدفع الجديدة (Paid, Partial, Unpaid)</param>
        /// <param name="paymentMethod">طريقة الدفع</param>
        /// <returns>true إذا نجح التحديث</returns>
        public static bool UpdatePayment(int saleId, decimal newPaidAmount, string newPaymentStatus, string paymentMethod)
        {
            try
            {
                if (saleId <= 0)
                {
                    throw new ArgumentException("معرف الفاتورة غير صالح.");
                }

                if (newPaidAmount < 0)
                {
                    throw new ArgumentException("المبلغ المدفوع لا يمكن أن يكون سالباً.");
                }

                using var connection = Helpers.DatabaseHelper.GetConnection();
                var snapshot = connection.QueryFirstOrDefault<SalePaymentSnapshot>(@"
                        SELECT TotalAmount, Discount, Tax, PaidAmount, COALESCE(ReturnedAmount, 0) AS ReturnedAmount
                        FROM Sales
                        WHERE SaleID = @SaleID;",
                    new { SaleID = saleId });

                if (snapshot == null)
                {
                    throw new Exception("الفاتورة غير موجودة.");
                }

                decimal netTotal = snapshot.TotalAmount - snapshot.Discount + (snapshot.TotalAmount * (snapshot.Tax / 100m));
                decimal totalDue = Math.Max(0, netTotal - snapshot.ReturnedAmount);
                decimal normalizedPaidAmount = Math.Min(newPaidAmount, totalDue);
                decimal normalizedRemainingAmount = Math.Max(0, totalDue - normalizedPaidAmount);
                string normalizedStatus = ResolvePaymentStatus(normalizedPaidAmount, totalDue, 0m);
                string normalizedMethod = NormalizePaymentMethod(paymentMethod);

                string query = @"
                        UPDATE Sales 
                        SET PaidAmount = @PaidAmount,
                            RemainingAmount = @RemainingAmount,
                            PaymentStatus = @PaymentStatus,
                            PaymentMethod = @PaymentMethod,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE SaleID = @SaleID";

                int rowsAffected = connection.Execute(query, new
                {
                    SaleID = saleId,
                    PaidAmount = normalizedPaidAmount,
                    RemainingAmount = normalizedRemainingAmount,
                    PaymentStatus = normalizedStatus,
                    PaymentMethod = normalizedMethod,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = Helpers.SessionContext.CurrentUserID
                });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogError(ex, $"SaleDAL.UpdatePayment - SaleID: {saleId}");
                throw new Exception($"خطأ في تحديث الدفعة: {ex.Message}", ex);
            }
        }

        public static bool UpdateReturnedAmount(int saleId, decimal returnedAmount)
        {
            try
            {
                if (saleId <= 0)
                {
                    throw new ArgumentException("معرف الفاتورة غير صالح.");
                }

                if (returnedAmount < 0)
                {
                    throw new ArgumentException("مبلغ المرتجع لا يمكن أن يكون سالباً.");
                }

                using var connection = Helpers.DatabaseHelper.GetConnection();
                var snapshot = connection.QueryFirstOrDefault<SalePaymentSnapshot>(@"
                        SELECT TotalAmount, Discount, Tax, PaidAmount, COALESCE(ReturnedAmount, 0) AS ReturnedAmount
                        FROM Sales
                        WHERE SaleID = @SaleID;",
                    new { SaleID = saleId });

                if (snapshot == null)
                {
                    throw new Exception("الفاتورة غير موجودة.");
                }

                decimal netTotal = snapshot.TotalAmount - snapshot.Discount + (snapshot.TotalAmount * (snapshot.Tax / 100m));
                decimal normalizedReturnedAmount = Math.Min(returnedAmount, Math.Max(0, netTotal));
                decimal normalizedRemaining = Math.Max(0, netTotal - snapshot.PaidAmount - normalizedReturnedAmount);
                string normalizedStatus = ResolvePaymentStatus(snapshot.PaidAmount, netTotal, normalizedReturnedAmount);

                string query = @"
                        UPDATE Sales 
                        SET ReturnedAmount = @ReturnedAmount,
                            RemainingAmount = @RemainingAmount,
                            PaymentStatus = @PaymentStatus,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE SaleID = @SaleID";

                int rowsAffected = connection.Execute(query, new
                {
                    SaleID = saleId,
                    ReturnedAmount = normalizedReturnedAmount,
                    RemainingAmount = normalizedRemaining,
                    PaymentStatus = normalizedStatus,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = Helpers.SessionContext.CurrentUserID
                });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogError(ex, $"SaleDAL.UpdateReturnedAmount - SaleID: {saleId}");
                throw new Exception($"خطأ في تحديث مبلغ المرجع: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// جلب الفواتير غير المسددة (للتقارير والتنبيهات)
        /// </summary>
        public static List<Sale> GetUnpaidSales()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*, c.Name as CustomerName,
                               COUNT(si.SaleItemID) as ItemCount
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                        LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                        WHERE s.PaymentStatus IN ('Unpaid', 'Partial')
                        GROUP BY s.SaleID
                        ORDER BY s.SaleDate ASC";

                return [.. connection.Query<Sale>(query)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب الفواتير غير المسددة: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// جلب الفواتير المستحقة (التي تجاوزت تاريخ الاستحقاق)
        /// </summary>
        public static List<Sale> GetOverdueSales()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT s.*, c.Name as CustomerName,
                               COUNT(si.SaleItemID) as ItemCount
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerID = c.CustomerID
                        LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                        WHERE s.PaymentStatus IN ('Unpaid', 'Partial')
                          AND s.DueDate IS NOT NULL
                          AND s.DueDate < DATE('now')
                        GROUP BY s.SaleID
                        ORDER BY s.DueDate ASC";

                return [.. connection.Query<Sale>(query)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب الفواتير المستحقة: {ex.Message}", ex);
            }
        }

        public static List<MonthlySpendingData> GetCustomerMonthlySpending(int customerId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT 
                            strftime('%Y-%m', SaleDate) as Month,
                            CAST(SUM((TotalAmount - Discount) + (TotalAmount * Tax / 100.0)) AS REAL) as Amount,
                            COUNT(*) as TransactionCount
                        FROM Sales
                        WHERE CustomerID = @CustomerID
                        GROUP BY strftime('%Y-%m', SaleDate)
                        ORDER BY Month DESC
                        LIMIT 12";

                List<MonthlySpendingData> value = [.. connection.Query<MonthlySpendingData>(query, new { CustomerID = customerId })];
                return value;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب الصرف الشهري العميل: {ex.Message}", ex);
            }
        }

        // دالة لجلب المنتجات الأكثر مبيعاً
        public static List<ProductSalesReport> GetTopSellingProducts(DateTime startDate, DateTime endDate, int limit = 50)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT 
                            p.ProductID, 
                            p.Name as ProductName, 
                            p.Category,
                            CAST(COALESCE(SUM(si.Quantity), 0) AS INTEGER) as QuantitySold, 
                            CAST(COALESCE(SUM(si.TotalPrice), 0) AS REAL) as TotalRevenue, 
                            CAST(COALESCE(AVG(si.UnitPrice), 0) AS REAL) as AveragePrice,
                            p.Quantity as CurrentStock,
                            CAST((p.Quantity * p.PurchasePrice) AS REAL) as StockValue
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        JOIN Products p ON si.ProductID = p.ProductID
                        WHERE DATE(s.SaleDate) BETWEEN DATE(@StartDate) AND DATE(@EndDate)
                        GROUP BY p.ProductID, p.Name, p.Category, p.Quantity, p.PurchasePrice
                        ORDER BY QuantitySold DESC
                        LIMIT @Limit";

                return [.. connection.Query<ProductSalesReport>(query, new { StartDate = startDate, EndDate = endDate, Limit = limit })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب تقرير المنتجات الأكثر مبيعاً: {ex.Message}", ex);
            }
        }

        public static Task<List<ProductSalesReport>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int limit = 50)
        {
            return Task.Run(() => GetTopSellingProducts(startDate, endDate, limit));
        }

        private static void NormalizeAndValidateSale(Sale sale)
        {
            if (sale is null)
            {
                throw new ArgumentNullException(nameof(sale));
            }

            if (sale.TotalAmount < 0)
            {
                throw new ArgumentException("إجمالي المنتجات لا يمكن أن يكون سالباً.");
            }

            if (sale.Discount < 0)
            {
                throw new ArgumentException("الخصم لا يمكن أن يكون سالباً.");
            }

            if (sale.Tax < 0)
            {
                throw new ArgumentException("نسبة الضريبة لا يمكن أن تكون سالبة.");
            }

            if (sale.PaidAmount < 0)
            {
                throw new ArgumentException("المبلغ المدفوع لا يمكن أن يكون سالباً.");
            }

            decimal netTotal = sale.TotalAmount - sale.Discount + (sale.TotalAmount * (sale.Tax / 100m));
            if (netTotal < -0.0001m)
            {
                throw new ArgumentException("إجمالي الفاتورة بعد الخصم والضريبة لا يمكن أن يكون سالباً.");
            }

            if (sale.PaidAmount > netTotal + 0.0001m)
            {
                throw new ArgumentException("المبلغ المدفوع لا يمكن أن يتجاوز إجمالي الفاتورة.");
            }

            sale.PaymentMethod = NormalizePaymentMethod(sale.PaymentMethod);
            sale.PaymentStatus = ResolvePaymentStatus(sale.PaidAmount, netTotal, sale.ReturnedAmount);

            if (sale.RemainingAmount <= 0)
            {
                sale.DueDate = null;
            }
        }

        private static string NormalizePaymentMethod(string paymentMethod)
        {
            return paymentMethod switch
            {
                "Cash" => "Cash",
                "Card" => "Card",
                "Transfer" => "Transfer",
                "Partial" => "Partial",
                "Credit" => "Credit",
                _ => "Cash"
            };
        }

        private static string ResolvePaymentStatus(decimal paidAmount, decimal totalAmountAfterTaxAndDiscount, decimal returnedAmount)
        {
            decimal remaining = totalAmountAfterTaxAndDiscount - paidAmount - returnedAmount;
            if (remaining <= 0.0001m)
            {
                return "Paid";
            }

            return paidAmount > 0 ? "Partial" : "Unpaid";
        }

        private sealed class SalePaymentSnapshot
        {
            public decimal TotalAmount { get; set; }
            public decimal Discount { get; set; }
            public decimal Tax { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal ReturnedAmount { get; set; }
        }
    }
}


