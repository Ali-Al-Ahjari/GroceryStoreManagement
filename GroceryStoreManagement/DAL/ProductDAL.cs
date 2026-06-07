using Dapper; // استيراد مكتبة Dapper لتبسيط التعامل مع قاعدة البيانات
using GroceryStoreManagement.Models; // استيراد نماذج البيانات مثل كلاس المنتج Product
using System; // استيراد الوظائف الأساسية للنظام
using System.Collections.Generic; // استيراد القوائم
using System.Data.SQLite; // استيراد مكتبة SQLite
using System.Linq; // استيراد دوال الاستعلام والفلترة
using GroceryStoreManagement.Helpers; // استيراد المساعدات مثل DBHelper
using System.Threading.Tasks;
using System.Threading;

namespace GroceryStoreManagement.DAL // مجال طبقة الوصول للبيانات
{
    // كلاس ثابت لإدارة عمليات قاعدة البيانات الخاصة بالمنتجات
    public static class ProductDAL
    {
        private static List<Product> _cachedProducts;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private static readonly ReaderWriterLockSlim CacheLock = new();
        private static long _productCodeSequence = DateTime.UtcNow.Ticks;

        /// <summary>
        /// مسح الكاش لإجبار النظام على جلب البيانات من القاعدة في المرة القادمة
        /// </summary>
        public static void ClearCache()
        {
            CacheLock.EnterWriteLock();
            try
            {
                _cachedProducts = null;
                _lastCacheUpdate = DateTime.MinValue;
            }
            finally
            {
                CacheLock.ExitWriteLock();
            }
        }
        // دالة لجلب جميع المنتجات المسجلة في النظام
        public static List<Product> GetAllProducts()
        {
            DateTime now = DateTime.UtcNow;

            // استخدام الكاش إذا كان متاحاً ولم تنته صلاحيته
            CacheLock.EnterReadLock();
            try
            {
                if (_cachedProducts != null && (now - _lastCacheUpdate) < CacheDuration)
                {
                    return [.. _cachedProducts];
                }
            }
            finally
            {
                CacheLock.ExitReadLock();
            }

            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // استعلام لجلب بيانات المنتجات مع اسم المورد المرتبط
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName 
                        FROM Products p 
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        ORDER BY p.Name"; // ترتيب حسب الاسم

                List<Product> freshProducts = [.. connection.Query<Product>(query)];

                CacheLock.EnterWriteLock();
                try
                {
                    _cachedProducts = freshProducts;
                    _lastCacheUpdate = DateTime.UtcNow;
                }
                finally
                {
                    CacheLock.ExitWriteLock();
                }

                return [.. freshProducts];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتجات: {ex.Message}", ex);
            }
        }

        public static Task<List<Product>> GetAllProductsAsync()
        {
            return Task.Run(GetAllProducts);
        }

        public static List<Product> GetProductsPage(int limit = 120, int offset = 0, string searchTerm = null)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                bool hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName
                        FROM Products p
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        /**where**/
                        ORDER BY p.Name
                        LIMIT @Limit OFFSET @Offset";

                if (hasSearch)
                {
                    query = query.Replace("/**where**/", "WHERE p.Name LIKE @SearchTerm OR p.Code LIKE @SearchTerm OR p.Category LIKE @SearchTerm");
                }
                else
                {
                    query = query.Replace("/**where**/", string.Empty);
                }

                return [.. connection.Query<Product>(query, new
                {
                    Limit = limit,
                    Offset = offset,
                    SearchTerm = $"%{searchTerm}%"
                })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب صفحة المنتجات: {ex.Message}", ex);
            }
        }

        public static Task<List<Product>> GetProductsPageAsync(int limit = 120, int offset = 0, string searchTerm = null)
        {
            return Task.Run(() => GetProductsPage(limit, offset, searchTerm));
        }

        // دالة للبحث عن منتج معين باستخدام معرفه (ID)
        public static Product GetProductById(int productId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName 
                        FROM Products p 
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        WHERE p.ProductID = @ProductID"; // شرط البحث بالمعرف

                return connection.QueryFirstOrDefault<Product>(query, new { ProductID = productId });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتج: {ex.Message}", ex);
            }
        }

        // دالة للبحث عن منتج معين باستخدام الكود (الباركود)
        public static Product GetProductByCode(string code)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName 
                        FROM Products p 
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        WHERE p.Code = @Code";

                return connection.QueryFirstOrDefault<Product>(query, new { Code = code });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتج بالكود: {ex.Message}", ex);
            }
        }

        // دالة لجلب قائمة المنتجات التابعة لفئة معينة (مثل: مشروبات، معلبات...)
        public static List<Product> GetProductsByCategory(string category)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName 
                        FROM Products p 
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        WHERE p.Category = @Category
                        ORDER BY p.Name";

                return [.. connection.Query<Product>(query, new { Category = category })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتجات حسب الفئة: {ex.Message}", ex);
            }
        }

        // دالة لجلب المنتجات التي يقل مخزونها عن حد معين (لإصدار التنبيهات)
        public static List<Product> GetLowStockProducts(int threshold = 10)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName 
                        FROM Products p 
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        WHERE p.Quantity <= @Threshold
                        ORDER BY p.Quantity"; // ترتيب تصاعدي حسب الكمية (الأقل أولاً)

                return [.. connection.Query<Product>(query, new { Threshold = threshold })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتجات منخفضة المخزون: {ex.Message}", ex);
            }
        }

        // دالة للبحث عن منتج بالاسم/الكود/الفئة مع حد أقصى للنتائج
        public static List<Product> SearchProducts(string searchTerm, int limit = 120)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return GetProductsPage(limit: limit, offset: 0);
                }

                using var connection = Helpers.DatabaseHelper.GetConnection();
                // استخدام LIKE للبحث الجزئي (يحتوي على النص)
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName 
                        FROM Products p 
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        WHERE p.Name LIKE @SearchTerm 
                           OR p.Code LIKE @SearchTerm
                           OR p.Category LIKE @SearchTerm
                        ORDER BY p.Name
                        LIMIT @Limit";

                // إضافة علامات % للبحث في أي مكان داخل النص
                return [.. connection.Query<Product>(query, new
                {
                    SearchTerm = $"%{searchTerm}%",
                    Limit = limit
                })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في بحث المنتجات: {ex.Message}", ex);
            }
        }

        // للحفاظ على التوافق مع النداءات القديمة
        public static List<Product> SearchProducts(string searchTerm)
        {
            return SearchProducts(searchTerm, 120);
        }

        public static Task<List<Product>> SearchProductsAsync(string searchTerm)
        {
            return SearchProductsAsync(searchTerm, 120);
        }

        public static Task<List<Product>> SearchProductsAsync(string searchTerm, int limit)
        {
            return Task.Run(() => SearchProducts(searchTerm, limit));
        }

        // دالة لجلب جميع أسماء الفئات المستخدمة (لتعبئة القوائم المنسدلة)
        public static List<string> GetAllCategories()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // DISTINCT لمنع تكرار أسماء الفئات
                string query = "SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL ORDER BY Category";
                return [.. connection.Query<string>(query)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب الفئات: {ex.Message}", ex);
            }
        }

        // دالة لإضافة منتج جديد
        public static int AddProduct(Product product)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تعبئة حقول التدقيق تلقائياً
                Helpers.AuditHelper.SetFullAudit(product);

                string query = @"

                        INSERT INTO Products (Name, Price, PurchasePrice, SellingPrice, Quantity, MinQuantity, Unit, Category, SupplierID, ImagePath, CreatedDate, Code, CreatedBy, ModifiedDate, ModifiedBy, ExpiryDate)
                        VALUES (@Name, @SellingPrice, @PurchasePrice, @SellingPrice, @Quantity, @MinQuantity, @Unit, @Category, @SupplierID, @ImagePath, @CreatedDate, @Code, @CreatedBy, @ModifiedDate, @ModifiedBy, @ExpiryDate);
                        SELECT last_insert_rowid();";

                int newId = connection.ExecuteScalar<int>(query, new
                {
                    product.Name,
                    product.SellingPrice, // Price column
                    product.PurchasePrice,
                    product.Quantity,
                    product.MinQuantity,
                    product.Unit,
                    product.Category,
                    product.SupplierID,
                    product.ImagePath,
                    product.Code,
                    product.CreatedDate,
                    product.CreatedBy,
                    product.ModifiedDate,
                    product.ModifiedBy,
                    product.ExpiryDate
                });

                // تسجيل العملية في سجل النشاطات
                ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "إضافة منتج", $"تم إضافة المنتج: {product.Name}");

                ClearCache();
                return newId;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة المنتج: {ex.Message}", ex);
            }
        }

        // دالة لتعديل بيانات منتج موجود
        public static bool UpdateProduct(Product product)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // تعبئة حقول التعديل تلقائياً
                Helpers.AuditHelper.SetModificationAudit(product);

                string query = @"
                        UPDATE Products 
                        SET Name = @Name, 
                            Price = @SellingPrice,
                            PurchasePrice = @PurchasePrice,
                            SellingPrice = @SellingPrice,
                            Quantity = @Quantity, 
                            MinQuantity = @MinQuantity,
                            Unit = @Unit,
                            Category = @Category, 
                            SupplierID = @SupplierID,
                            ImagePath = @ImagePath,
                            Code = @Code,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy,
                            ExpiryDate = @ExpiryDate
                        WHERE ProductID = @ProductID";

                int rowsAffected = connection.Execute(query, new
                {
                    product.ProductID,
                    product.Name,
                    product.SellingPrice,
                    product.PurchasePrice,
                    product.Quantity,
                    product.MinQuantity,
                    product.Unit,
                    product.Category,
                    product.SupplierID,
                    product.ImagePath,
                    product.Code,
                    product.ModifiedDate,
                    product.ModifiedBy,
                    product.ExpiryDate
                });

                if (rowsAffected > 0)
                {
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "تعديل منتج", $"تم تعديل المنتج: {product.Name}");
                    ClearCache();
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث المنتج: {ex.Message}", ex);
            }
        }

        // دالة لحذف منتج (مع التحقق من عدم بيعه سابقاً)
        public static bool DeleteProduct(int productId)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // التحقق من جدول عناصر الفواتير (SaleItems)
                string checkQuery = "SELECT COUNT(*) FROM SaleItems WHERE ProductID = @ProductID";
                int saleCount = connection.ExecuteScalar<int>(checkQuery, new { ProductID = productId });

                if (saleCount > 0)
                {
                    throw new Exception("لا يمكن حذف المنتج لأنه مرتبط بمبيعات سابقة");
                }

                // الحذف إذا لم يكن مرتبطاً
                string query = "DELETE FROM Products WHERE ProductID = @ProductID";
                int rowsAffected = connection.Execute(query, new { ProductID = productId });

                if (rowsAffected > 0)
                {
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, "حذف منتج", $"تم حذف المنتج رقم: {productId}");
                    ClearCache();
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف المنتج: {ex.Message}", ex);
            }
        }

        // دالة لتحديث كمية منتج (زيادة أو نقصان)
        // quantityChange: قيمة موجبة للإضافة، وقيمة سالبة للخصم
        public static bool UpdateProductQuantity(int productId, int quantityChange)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        UPDATE Products 
                        SET Quantity = Quantity + @QuantityChange 
                        WHERE ProductID = @ProductID";

                int rowsAffected = connection.Execute(query, new
                {
                    ProductID = productId,
                    QuantityChange = quantityChange
                });

                if (rowsAffected > 0)
                {
                    string action = quantityChange > 0 ? "زيادة مخزون" : "نقص مخزون";
                    ActivityLogDAL.AddLog(SessionContext.CurrentUserID, action, $"تم تحديث كمية المنتج رقم {productId} بمقدار {quantityChange}");
                    ClearCache();
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث كمية المنتج: {ex.Message}", ex);
            }
        }

        // دالة لحساب القيمة الإجمالية للمخزون (سعر * كمية لكل المنتجات)
        public static decimal GetTotalStockValue()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "SELECT SUM(Price * Quantity) FROM Products";
                var result = connection.ExecuteScalar<decimal?>(query);
                return result ?? 0; // إرجاع 0 إذا كانت النتيجة فارغة
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب قيمة المخزون: {ex.Message}", ex);
            }
        }

        // دالة لحساب العدد الكلي للأصناف
        public static int GetTotalProductsCount()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = "SELECT COUNT(*) FROM Products";
                return connection.ExecuteScalar<int>(query);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حساب عدد المنتجات: {ex.Message}", ex);
            }
        }

        // دالة لجلب أكثر المنتجات مبيعاً فعلياً بناءً على جدول تفاصيل المبيعات
        public static List<Product> GetTopSellingProducts(int limit = 10)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT 
                            p.ProductID,
                            p.Code,
                            p.Name,
                            p.Unit,
                            p.PurchasePrice,
                            p.SellingPrice,
                            p.Price,
                            p.MinQuantity,
                            p.Quantity,
                            p.ImagePath,
                            p.Category,
                            p.SupplierID,
                            p.ExpiryDate,
                            p.CreatedBy,
                            p.CreatedDate,
                            p.ModifiedBy,
                            p.ModifiedDate,
                            s.Name as SupplierName
                        FROM Products p
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        LEFT JOIN (
                            SELECT 
                                si.ProductID,
                                SUM(si.Quantity - COALESCE(si.ReturnedQuantity, 0)) AS TotalSold
                            FROM SaleItems si
                            GROUP BY si.ProductID
                        ) sales ON p.ProductID = sales.ProductID
                        ORDER BY COALESCE(sales.TotalSold, 0) DESC, p.Name ASC
                        LIMIT @Limit";

                return [.. connection.Query<Product>(query, new { Limit = limit })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتجات: {ex.Message}", ex);
            }
        }

        // دالة لجلب المنتجات التي ستنتهي صلاحيتها قريباً
        public static List<Product> GetExpiringProducts(int daysThreshold = 30)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                // جلب المنتجات التي تاريخ صلاحيتها بين اليوم وتاريخ الحد الأقصى (المستقبل القريب)
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName 
                        FROM Products p 
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        WHERE p.ExpiryDate IS NOT NULL 
                          AND p.ExpiryDate >= date('now') 
                          AND p.ExpiryDate <= date('now', '+' || @Days || ' days')
                        ORDER BY p.ExpiryDate";

                return [.. connection.Query<Product>(query, new { Days = daysThreshold })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتجات قريبة الانتهاء: {ex.Message}", ex);
            }
        }

        // دالة لجلب المنتجات منتهية الصلاحية
        public static List<Product> GetExpiredProducts()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT p.ProductID, p.Code, p.Name, p.Unit, p.PurchasePrice, p.SellingPrice, p.Price,
                               p.MinQuantity, p.Quantity, p.ImagePath, p.Category, p.SupplierID, p.ExpiryDate,
                               p.CreatedBy, p.CreatedDate, p.ModifiedBy, p.ModifiedDate,
                               s.Name as SupplierName 
                        FROM Products p 
                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                        WHERE p.ExpiryDate IS NOT NULL 
                          AND p.ExpiryDate < date('now')
                        ORDER BY p.ExpiryDate";

                return [.. connection.Query<Product>(query)];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتجات منتهية الصلاحية: {ex.Message}", ex);
            }
        }
        // دالة لتوليد كود منتج تلقائي وتفادي التكرار
        public static string GenerateProductCode()
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();

                // الصيغة: P + UTC Timestamp بالمللي + عداد متزامن من 3 أرقام
                // مع تحقق فعلي من عدم التكرار داخل قاعدة البيانات.
                const int maxAttempts = 200;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    string timestampPart = DateTime.UtcNow.ToString("yyMMddHHmmssfff");
                    long sequence = Interlocked.Increment(ref _productCodeSequence) % 1000;
                    string candidate = $"P{timestampPart}{sequence:D3}";

                    int exists = connection.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM Products WHERE Code = @Code",
                        new { Code = candidate });

                    if (exists == 0)
                    {
                        return candidate;
                    }
                }

                throw new InvalidOperationException("تعذر توليد كود منتج فريد بعد عدة محاولات.");
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في توليد كود المنتج: {ex.Message}", ex);
            }
        }
    }
}

