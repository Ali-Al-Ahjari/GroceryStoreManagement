using System; // استيراد المكتبة الأساسية للتعامل مع الأنواع الأساسية مثل النصوص والأرقام
using System.Collections.Generic;
using System.Data; // استيراد مكتبة التعامل مع بيانات قاعدة البيانات والحالات (مثل حالة الاتصال)
using System.Data.SQLite; // استيراد مكتبة التعامل مع قاعدة بيانات SQLite المدمجة
using System.IO; // استيراد مكتبة التعامل مع الملفات والمجلدات
using System.Text.RegularExpressions;

namespace GroceryStoreManagement.Helpers // تحديد اسم المجال لهذا الملف داخل مجلد المساعدات
{
    // تعريف كلاس ثابت (Static) للمساعدة في عمليات قاعدة البيانات، لا يمكن إنشاء كائن منه (instance)
    public static class DatabaseHelper
    {
        // متغيرات خاصة لتخزين مسار قاعدة البيانات ونص الاتصال (Connection String)
#pragma warning disable IDE0044 // Add readonly modifier
        private static string _connectionString;
#pragma warning restore IDE0044 // Add readonly modifier
        private static readonly string _databasePath;

        // المُنشئ الثابت (Static Constructor) يتم تنفيذه مرة واحدة فقط عند تشغيل البرنامج
        static DatabaseHelper()
        {
            // تحديد مسار ملف الإعدادات الذي قد يحتوي على مسار قاعدة البيانات المخصص
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "db_config.txt");
            string dbPath; // متغير لتخزين المسار النهائي لقاعدة البيانات

            // التحقق مما إذا كان ملف الإعدادات موجوداً
            if (File.Exists(configPath))
            {
                // إذا وجد، نقرأ المسار منه
                dbPath = File.ReadAllText(configPath).Trim();
            }
            else
            {
                // إذا لم يوجد، نحدد المسار الافتراضي داخل مجلد البرنامج
                string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

                // إنشاء المجلد إذا لم يكن موجوداً
                _ = Directory.CreateDirectory(databasePath);

                // تحديد اسم ملف قاعدة البيانات
                dbPath = Path.Combine(databasePath, "GroceryStore.db");
            }

            if (string.IsNullOrWhiteSpace(dbPath))
            {
                string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                _ = Directory.CreateDirectory(databasePath);
                dbPath = Path.Combine(databasePath, "GroceryStore.db");
            }

            // التأكد من وجود مجلد قاعدة البيانات
            string dbDirectory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
            {
                _ = Directory.CreateDirectory(dbDirectory);
            }

            _databasePath = dbPath;

            // بناء نص الاتصال وتفعيل Foreign Keys بشكل افتراضي على كل اتصال
            _connectionString = $"Data Source={_databasePath};Version=3;Foreign Keys=True;";
        }

        // دالة للحصول على كائن الاتصال بقاعدة البيانات
        public static SQLiteConnection GetConnection()
        {
            // إنشاء اتصال جديد باستخدام نص الاتصال المحفوظ
            var connection = new SQLiteConnection(_connectionString);

            // التأكد من أن الاتصال مفتوح، وإذا كان مغلقاً نقوم بفتحه
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();

                // تفعيل قيود Foreign Keys لضمان سلامة البيانات المرجعية
                using var pragmaCmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection);
                pragmaCmd.ExecuteNonQuery();
            }

            // إرجاع كائن الاتصال ليستخدمه الكود الطالب
            return connection;
        }

        /// <summary>
        /// الحصول على مسار ملف قاعدة البيانات
        /// </summary>
        /// <returns>المسار الكامل لملف قاعدة البيانات</returns>
        public static string GetDatabasePath()
        {
            return _databasePath;
        }

        // دالة لتهيئة قاعدة البيانات (إنشاء الملف والجداول لأول مرة)
        public static void InitializeDatabase()
        {
            string migrationBackupPath = null;
            try // بداية كتلة التعامل مع الأخطاء
            {
                // التأكد من وجود مجلد ومسار قاعدة البيانات
                string dbPath = GetDatabasePath();
                string dbDirectory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    _ = Directory.CreateDirectory(dbDirectory);
                }

                // التحقق من وجود ملف قاعدة البيانات
                if (!File.Exists(dbPath))
                {
                    // إذا لم يوجد، نقوم بإنشائه (ملف فارغ)
                    SQLiteConnection.CreateFile(dbPath);
                }

                // فتح اتصال بقاعدة البيانات لتنفيذ أوامر الإنشاء
                using var connection = GetConnection();
                // تعريف استعلام SQL لإنشاء جدول الموردين (Suppliers)
                string createSuppliers = @"
                        CREATE TABLE IF NOT EXISTS Suppliers (
                            SupplierID INTEGER PRIMARY KEY AUTOINCREMENT, -- رقم المعرف التلقائي
                            Name TEXT NOT NULL, -- اسم المورد (إلزامي)
                            Phone TEXT, -- رقم الهاتف
                            Email TEXT, -- البريد الإلكتروني
                            Address TEXT -- العنوان
                        );";

                // تعريف استعلام لإنشاء جدول المنتجات (Products)
                string createProducts = @"
                        CREATE TABLE IF NOT EXISTS Products (
                            ProductID INTEGER PRIMARY KEY AUTOINCREMENT, -- المعرف
                            Name TEXT NOT NULL, -- اسم المنتج
                            Price REAL NOT NULL, -- السعر
                            Quantity INTEGER DEFAULT 0, -- الكمية المتوفرة
                            Category TEXT, -- التصنيف
                            SupplierID INTEGER, -- معرف المورد
                            FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID) -- ربط مع جدول الموردين
                        );";

                // تعريف استعلام لإنشاء جدول العملاء (Customers)
                string createCustomers = @"
                        CREATE TABLE IF NOT EXISTS Customers (
                            CustomerID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Phone TEXT,
                            Email TEXT,
                            Address TEXT
                        );";

                // تعريف استعلام لإنشاء جدول المبيعات/الفواتير (Sales)
                string createSales = @"
                        CREATE TABLE IF NOT EXISTS Sales (
                            SaleID INTEGER PRIMARY KEY AUTOINCREMENT,
                            CustomerID INTEGER,
                            SaleDate DATETIME DEFAULT CURRENT_TIMESTAMP, -- تاريخ البيع (الافتراضي هو الوقت الحالي)
                            TotalAmount REAL, -- المبلغ الإجمالي
                            FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID) -- ربط مع العميل
                        );";

                // تعريف استعلام لإنشاء جدول عناصر الفاتورة (SaleItems)
                string createSaleItems = @"
                        CREATE TABLE IF NOT EXISTS SaleItems (
                            SaleItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                            SaleID INTEGER, -- معرف الفاتورة
                            ProductID INTEGER, -- معرف المنتج المباع
                            Quantity INTEGER, -- الكمية المباعة
                            UnitPrice REAL, -- سعر الوحدة وقت البيع
                            TotalPrice REAL, -- الإجمالي (السعر * الكمية)
                            FOREIGN KEY (SaleID) REFERENCES Sales(SaleID) ON DELETE CASCADE, -- إذا حذفت الفاتورة تحذف عناصرها
                            FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
                        );";

                // تعريف استعلام لإنشاء جدول المستخدمين (Users)
                string createUsers = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT UNIQUE NOT NULL, -- اسم مستخدم فريد
                            Password TEXT NOT NULL, -- كلمة المرور
                            FullName TEXT, -- الاسم الكامل
                            Phone TEXT,
                            Email TEXT,
                            IsActive INTEGER DEFAULT 1, -- هل الحساب نشط؟ (1=نعم)
                            CanAccessDashboard INTEGER DEFAULT 0, -- صلاحية: دخول لوحة التحكم
                            CanViewCustomers INTEGER DEFAULT 0, -- صلاحية: عرض العملاء
                            CanAddCustomers INTEGER DEFAULT 0, -- صلاحية: إضافة عملاء
                            CanEditCustomers INTEGER DEFAULT 0, -- صلاحية: تعديل العملاء
                            CanDeleteCustomers INTEGER DEFAULT 0, -- صلاحية: حذف العملاء
                            CanManageProducts INTEGER DEFAULT 0, -- صلاحية: إدارة المنتجات
                            CanManageInvoices INTEGER DEFAULT 0, -- صلاحية: إدارة الفواتير
                            CanViewReports INTEGER DEFAULT 0, -- صلاحية: عرض التقارير
                            CanManageSettings INTEGER DEFAULT 0, -- صلاحية: إدارة الإعدادات
                            CanBackup INTEGER DEFAULT 0 -- صلاحية: النسخ الاحتياطي
                        );";

                // تعريف استعلام لإنشاء جدول سجل النشاطات (ActivityLogs) لتتبع حركات المستخدمين
                string createActivityLogs = @"
                        CREATE TABLE IF NOT EXISTS ActivityLogs (
                            LogID INTEGER PRIMARY KEY AUTOINCREMENT,
                            UserID INTEGER, -- من قام بالعملية
                            Action TEXT, -- نوع العملية (إضافة، حذف...)
                            Details TEXT, -- التفاصيل
                            LogDate DATETIME DEFAULT CURRENT_TIMESTAMP, -- وقت العملية
                            FOREIGN KEY (UserID) REFERENCES Users(UserID)
                        );";

                // تعريف استعلام لإنشاء المحفزات (Triggers) لتحديث المخزون تلقائياً
                string createTriggers = @"
                        -- trigger يعمل بعد إضافة عنصر بيع لينقص الكمية من المنتج
                        CREATE TRIGGER IF NOT EXISTS update_inventory_after_sale
                        AFTER INSERT ON SaleItems
                        BEGIN
                            UPDATE Products 
                            SET Quantity = Quantity - NEW.Quantity
                            WHERE ProductID = NEW.ProductID;
                        END;
                        
                        -- trigger يعمل بعد حذف عنصر بيع (إرجاع منتج) لزيادة الكمية مرة أخرى
                        CREATE TRIGGER IF NOT EXISTS restore_inventory_after_delete
                        AFTER DELETE ON SaleItems
                        BEGIN
                            UPDATE Products 
                            SET Quantity = Quantity + OLD.Quantity
                            WHERE ProductID = OLD.ProductID;
                        END;";

                // تنفيذ جميع الاستعلامات السابقة لإنشاء الجداول
                ExecuteNonQuery(createSuppliers, connection);
                ExecuteNonQuery(createProducts, connection);
                ExecuteNonQuery(createCustomers, connection);
                ExecuteNonQuery(createSales, connection);
                ExecuteNonQuery(createSaleItems, connection);
                ExecuteNonQuery(createUsers, connection);
                ExecuteNonQuery(createActivityLogs, connection);
                ExecuteNonQuery(createTriggers, connection);

                // استعلام جدول المشتريات (لشراء بضاعة من الموردين)
                string createPurchases = @"
                        CREATE TABLE IF NOT EXISTS Purchases (
                            PurchaseID INTEGER PRIMARY KEY AUTOINCREMENT,
                            SupplierID INTEGER,
                            TotalAmount REAL DEFAULT 0,
                            PaidAmount REAL DEFAULT 0,
                            Discount REAL DEFAULT 0,
                            PaymentStatus TEXT DEFAULT 'Unpaid',
                            PurchaseDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            Notes TEXT,
                            InvoiceNumber TEXT,
                            ItemCount INTEGER DEFAULT 0,
                            IsImported INTEGER DEFAULT 0, -- هل تم استلام البضاعة؟
                            FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID)
                        );";
                ExecuteNonQuery(createPurchases, connection);

                // استعلام جدول تفاصيل المشتريات
                string createPurchaseItems = @"
                        CREATE TABLE IF NOT EXISTS PurchaseItems (
                            PurchaseItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                            PurchaseID INTEGER,
                            ProductID INTEGER,
                            Quantity INTEGER,
                            UnitPrice REAL,
                            TotalPrice REAL,
                            FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
                        );";
                ExecuteNonQuery(createPurchaseItems, connection);

                // استعلام جدول الإشعارات (للتنبيهات داخل النظام)
                string createNotifications = @"
                        CREATE TABLE IF NOT EXISTS Notifications (
                            NotificationID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT NOT NULL,
                            Message TEXT,
                            Type TEXT,
                            Source TEXT,
                            IsRead INTEGER DEFAULT 0, -- هل تمت قراءته؟
                            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                            RelatedEntity TEXT,
                            RelatedID INTEGER
                        );";
                ExecuteNonQuery(createNotifications, connection);

                // جداول الورديات والفواتير المعلقة (POS Workflow)
                string createShifts = @"
                        CREATE TABLE IF NOT EXISTS Shifts (
                            ShiftID INTEGER PRIMARY KEY AUTOINCREMENT,
                            OpenedBy INTEGER NOT NULL,
                            OpenedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            OpeningCash REAL NOT NULL DEFAULT 0,
                            ClosedBy INTEGER,
                            ClosedAt DATETIME,
                            ClosingCash REAL,
                            CashSalesTotal REAL DEFAULT 0,
                            CardSalesTotal REAL DEFAULT 0,
                            TransferSalesTotal REAL DEFAULT 0,
                            CreditSalesTotal REAL DEFAULT 0,
                            CashRefundsTotal REAL DEFAULT 0,
                            ExpectedCash REAL DEFAULT 0,
                            CashDifference REAL DEFAULT 0,
                            Notes TEXT,
                            Status TEXT NOT NULL DEFAULT 'Open',
                            FOREIGN KEY (OpenedBy) REFERENCES Users(UserID),
                            FOREIGN KEY (ClosedBy) REFERENCES Users(UserID)
                        );";
                ExecuteNonQuery(createShifts, connection);

                string createSuspendedSales = @"
                        CREATE TABLE IF NOT EXISTS SuspendedSales (
                            SuspendedSaleID INTEGER PRIMARY KEY AUTOINCREMENT,
                            CustomerID INTEGER,
                            Notes TEXT,
                            Discount REAL DEFAULT 0,
                            Tax REAL DEFAULT 0,
                            PaymentMethod TEXT DEFAULT 'Cash',
                            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            CreatedBy INTEGER,
                            ShiftID INTEGER,
                            Subtotal REAL DEFAULT 0,
                            FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
                            FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
                            FOREIGN KEY (ShiftID) REFERENCES Shifts(ShiftID)
                        );";
                ExecuteNonQuery(createSuspendedSales, connection);

                string createSuspendedSaleItems = @"
                        CREATE TABLE IF NOT EXISTS SuspendedSaleItems (
                            SuspendedSaleItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                            SuspendedSaleID INTEGER NOT NULL,
                            ProductID INTEGER NOT NULL,
                            ProductName TEXT,
                            Quantity INTEGER NOT NULL,
                            UnitPrice REAL NOT NULL,
                            DiscountPercent REAL DEFAULT 0,
                            TotalPrice REAL NOT NULL,
                            FOREIGN KEY (SuspendedSaleID) REFERENCES SuspendedSales(SuspendedSaleID) ON DELETE CASCADE,
                            FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
                        );";
                ExecuteNonQuery(createSuspendedSaleItems, connection);

                // نظام الصلاحيات المتقدم (Phase 1.2)

                // جدول تعريف الصلاحيات
                string createPermissions = @"
                        CREATE TABLE IF NOT EXISTS Permissions (
                            PermissionID INTEGER PRIMARY KEY AUTOINCREMENT,
                            PermissionKey TEXT UNIQUE NOT NULL, -- مفتاح الصلاحية (SystemName)
                            DisplayName TEXT NOT NULL, -- الاسم المعروض
                            Description TEXT,
                            Category TEXT
                        );";
                ExecuteNonQuery(createPermissions, connection);

                // جدول الأدوار (Roles)
                string createRoles = @"
                        CREATE TABLE IF NOT EXISTS Roles (
                            RoleID INTEGER PRIMARY KEY AUTOINCREMENT,
                            RoleName TEXT UNIQUE NOT NULL,
                            Description TEXT,
                            IsSystemRole INTEGER DEFAULT 0 -- أدوار أساسية لا يمكن حذفها
                        );";
                ExecuteNonQuery(createRoles, connection);

                // جدول صلاحيات الأدوار (RolePermissions) بدلاً من صلاحيات المستخدمين المباشرة
                string createRolePermissions = @"
                        CREATE TABLE IF NOT EXISTS RolePermissions (
                            RolePermissionID INTEGER PRIMARY KEY AUTOINCREMENT,
                            RoleID INTEGER,
                            PermissionKey TEXT,
                            FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE CASCADE,
                            FOREIGN KEY (PermissionKey) REFERENCES Permissions(PermissionKey)
                        );";
                ExecuteNonQuery(createRolePermissions, connection);

                // إضافة عمود RoleID لجدول المستخدمين
                AddColumnIfNotExists(connection, "Users", "RoleID", "INTEGER");

                // =================================================================
                // تهيئة البيانات الأولية للأدوار والصلاحيات
                // =================================================================

                // 1. إضافة الأدوار الافتراضية
                string checkRoles = "SELECT COUNT(*) FROM Roles";
                using (var cmd = new SQLiteCommand(checkRoles, connection))
                {
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    {
                        // Admin - مدير النظام (كامل الصلاحيات)
                        ExecuteNonQuery("INSERT INTO Roles (RoleName, Description, IsSystemRole) VALUES ('Admin', 'مدير النظام - كامل الصلاحيات', 1)", connection);

                        // Manager - مدير (صلاحيات إدارية)
                        ExecuteNonQuery("INSERT INTO Roles (RoleName, Description, IsSystemRole) VALUES ('Manager', 'مدير المتجر - صلاحيات واسعة', 0)", connection);

                        // Cashier - كاشير (صلاحيات بيع فقط)
                        ExecuteNonQuery("INSERT INTO Roles (RoleName, Description, IsSystemRole) VALUES ('Cashier', 'كاشير - نقطة بيع فقط', 0)", connection);
                    }
                }

                // 2. تحديث المستخدم Admin ليكون دوره Admin (RoleID = 1)
                ExecuteNonQuery("UPDATE Users SET RoleID = 1 WHERE Username = 'admin'", connection);

                // نظام الخصومات المتقدم (Phase 2.3)
                string createPromotions = @"
                        CREATE TABLE IF NOT EXISTS Promotions (
                            PromotionID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            DiscountType TEXT DEFAULT 'Percentage', -- Percentage or Fixed
                            DiscountValue REAL NOT NULL,
                            StartDate DATETIME,
                            EndDate DATETIME,
                            MinPurchase REAL DEFAULT 0,
                            AppliesTo TEXT DEFAULT 'All', -- All, Category, Product
                            TargetID INTEGER DEFAULT 0, -- ProductID or 0 for Category (needs logic)
                            TargetName TEXT, -- Category Name if AppliesTo = Category
                            IsActive INTEGER DEFAULT 1,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                        );";
                ExecuteNonQuery(createPromotions, connection);

                // دوال مساعدة لإضافة أعمدة جديدة (لضمان التوافق عند تحديث البرنامج دون فقدان البيانات)
                // جدول المبيعات
                AddColumnIfNotExists(connection, "Sales", "PaidAmount", "REAL DEFAULT 0");
                AddColumnIfNotExists(connection, "Sales", "Discount", "REAL DEFAULT 0");
                AddColumnIfNotExists(connection, "Sales", "Tax", "REAL DEFAULT 0");
                AddColumnIfNotExists(connection, "Sales", "PaymentStatus", "TEXT DEFAULT 'Unpaid'");
                AddColumnIfNotExists(connection, "Sales", "PaymentMethod", "TEXT DEFAULT 'Cash'");
                AddColumnIfNotExists(connection, "Sales", "Notes", "TEXT");
                AddColumnIfNotExists(connection, "Sales", "ItemCount", "INTEGER DEFAULT 0");
                AddColumnIfNotExists(connection, "Sales", "RemainingAmount", "REAL DEFAULT 0");
                AddColumnIfNotExists(connection, "Sales", "DueDate", "DATETIME"); // تاريخ الاستحقاق للفواتير الآجلة
                AddColumnIfNotExists(connection, "Sales", "ReturnedAmount", "REAL DEFAULT 0"); // المبلغ المرتجع
                AddColumnIfNotExists(connection, "Sales", "ShiftID", "INTEGER"); // الوردية المرتبطة بعملية البيع

                // جدول المنتجات
                AddColumnIfNotExists(connection, "Products", "Code", "TEXT");
                AddColumnIfNotExists(connection, "Products", "Unit", "TEXT");
                AddColumnIfNotExists(connection, "Products", "PurchasePrice", "REAL DEFAULT 0"); // سعر الشراء
                AddColumnIfNotExists(connection, "Products", "SellingPrice", "REAL DEFAULT 0"); // سعر البيع
                AddColumnIfNotExists(connection, "Products", "MinQuantity", "INTEGER DEFAULT 5"); // حد الطلب
                AddColumnIfNotExists(connection, "Products", "ImagePath", "TEXT");
                AddColumnIfNotExists(connection, "Products", "CreatedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Products", "ExpiryDate", "DATETIME"); // تاريخ انتهاء الصلاحية (Phase 3.1)

                // جدول تفاصيل المبيعات
                AddColumnIfNotExists(connection, "SaleItems", "DiscountPercent", "REAL DEFAULT 0");
                AddColumnIfNotExists(connection, "SaleItems", "ReturnedQuantity", "INTEGER DEFAULT 0"); // الكمية المرتجعة

                // جدول العملاء - أعمدة إضافية
                AddColumnIfNotExists(connection, "Customers", "Notes", "TEXT");
                AddColumnIfNotExists(connection, "Customers", "IsActive", "INTEGER DEFAULT 1");
                AddColumnIfNotExists(connection, "Customers", "CreatedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Customers", "TotalPurchases", "REAL DEFAULT 0");
                AddColumnIfNotExists(connection, "Customers", "PurchaseCount", "INTEGER DEFAULT 0");
                AddColumnIfNotExists(connection, "Customers", "CreditLimit", "REAL DEFAULT 0"); // الحد الائتماني
                AddColumnIfNotExists(connection, "Customers", "TotalPoints", "INTEGER DEFAULT 0"); // نقاط الولاء
                AddColumnIfNotExists(connection, "Customers", "PointsValue", "REAL DEFAULT 0"); // قيمة النقاط

                // حقول التدقيق (Audit Fields) - لتتبع من أنشأ وعدل كل سجل

                // جدول المنتجات - حقول التدقيق
                AddColumnIfNotExists(connection, "Products", "CreatedBy", "INTEGER"); // من أنشأ
                AddColumnIfNotExists(connection, "Products", "ModifiedDate", "DATETIME"); // تاريخ آخر تعديل
                AddColumnIfNotExists(connection, "Products", "ModifiedBy", "INTEGER"); // من عدل

                // جدول العملاء - حقول التدقيق
                AddColumnIfNotExists(connection, "Customers", "CreatedBy", "INTEGER");
                AddColumnIfNotExists(connection, "Customers", "ModifiedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Customers", "ModifiedBy", "INTEGER");

                // جدول الموردين - حقول التدقيق
                AddColumnIfNotExists(connection, "Suppliers", "CreatedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Suppliers", "CreatedBy", "INTEGER");
                AddColumnIfNotExists(connection, "Suppliers", "ModifiedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Suppliers", "ModifiedBy", "INTEGER");

                // جدول المبيعات - حقول التدقيق

                AddColumnIfNotExists(connection, "Sales", "CreatedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Sales", "CreatedBy", "INTEGER");
                AddColumnIfNotExists(connection, "Sales", "ModifiedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Sales", "ModifiedBy", "INTEGER");

                // جدول المستخدمين - حقول التدقيق
                AddColumnIfNotExists(connection, "Users", "CreatedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Users", "CreatedBy", "INTEGER");
                AddColumnIfNotExists(connection, "Users", "ModifiedDate", "DATETIME");
                AddColumnIfNotExists(connection, "Users", "ModifiedBy", "INTEGER");
                AddColumnIfNotExists(connection, "Users", "LastLoginDate", "DATETIME");

                // ترحيلات هيكلية آمنة + فهارس الأداء
                if (ShouldRunStructuralMigration(connection))
                {
                    Logger.LogInfo("تم اكتشاف حاجة لترحيل هيكلي لقاعدة البيانات.");
                    try
                    {
                        migrationBackupPath = BackupHelper.CreateBackup("Auto backup before structural migration");
                        Logger.LogInfo($"تم إنشاء نسخة احتياطية قبل الترحيل: {migrationBackupPath}");
                    }
                    catch (Exception backupEx)
                    {
                        // في حالة فشل النسخ الاحتياطي نوقف الترحيل لحماية البيانات
                        throw new Exception($"فشل إنشاء النسخة الاحتياطية قبل الترحيل: {backupEx.Message}", backupEx);
                    }
                }

                Logger.LogInfo("بدء تنفيذ الترحيل الهيكلي والفهارس...");
                RunStructuralMigrations(connection);
                Logger.LogInfo("اكتمل تنفيذ الترحيل الهيكلي بنجاح.");


            }
            catch (Exception ex) // في حالة حدوث خطأ
            {
                // محاولة الاستعادة من النسخة الاحتياطية إذا كانت موجودة
                if (!string.IsNullOrWhiteSpace(migrationBackupPath) && File.Exists(migrationBackupPath))
                {
                    try
                    {
                        SQLiteConnection.ClearAllPools();
                        BackupHelper.RestoreBackup(migrationBackupPath);
                        Logger.LogWarning($"تمت استعادة قاعدة البيانات من النسخة الاحتياطية بعد فشل الترحيل: {migrationBackupPath}");
                    }
                    catch (Exception restoreEx)
                    {
                        Logger.LogError(restoreEx, "فشل استعادة النسخة الاحتياطية بعد خطأ الترحيل");
                    }
                }

                // رمي استثناء جديد مع التفاصيل
                throw new Exception($"خطأ في تهيئة قاعدة البيانات: {ex.Message}", ex);
            }
        }

        // دالة مساعدة لتنفيذ أوامر SQL التي لا ترجع بيانات (مثل CREATE, INSERT, UPDATE)
        private static void ExecuteNonQuery(string query, SQLiteConnection connection)
        {
            using var command = new SQLiteCommand(query, connection);
            _ = command.ExecuteNonQuery(); // التنفيذ الفعلي
        }

        // قائمة بيضاء بأسماء الجداول المسموح بإضافة أعمدة لها (حماية ضد SQL Injection)
        private static readonly HashSet<string> AllowedTableNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Sales", "Products", "Customers", "Suppliers", "Users",
            "SaleItems", "Purchases", "PurchaseItems", "Returns", "ReturnItems",
            "Promotions", "Permissions", "Roles", "RolePermissions", "Notifications",
            "ActivityLogs", "Shifts", "SuspendedSales", "SuspendedSaleItems"
        };

        // نمط Regex للتحقق من صحة أسماء الأعمدة (أحرف وأرقام وشرطة سفلية فقط)
        private static readonly Regex SafeIdentifierPattern = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        // نمط Regex للتحقق من نوع العمود (صيغة آمنة ومحدودة)
        private static readonly Regex SafeColumnTypePattern = new(
            @"^(INTEGER|REAL|TEXT|BLOB|NUMERIC|DATETIME|DATE|BOOLEAN)(\s+NOT\s+NULL)?(\s+DEFAULT\s+('([^']|'')*'|-?\d+(\.\d+)?|CURRENT_TIMESTAMP|NULL|TRUE|FALSE))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// دالة ذكية لإضافة عمود لجدول إذا لم يكن موجوداً، لتجنب الأخطاء عند تحديث قاعدة البيانات.
        /// محمية ضد SQL Injection عبر Whitelist و Regex.
        /// </summary>
        private static void AddColumnIfNotExists(SQLiteConnection connection, string tableName, string columnName, string columnType)
        {
            try
            {
                // التحقق من أن اسم الجدول ضمن القائمة البيضاء
                if (!AllowedTableNames.Contains(tableName))
                {
                    Logger.LogWarning($"محاولة إضافة عمود لجدول غير مصرح به: {tableName}");
                    return;
                }

                // التحقق من أن اسم العمود يحتوي على أحرف آمنة فقط
                if (!SafeIdentifierPattern.IsMatch(columnName))
                {
                    Logger.LogWarning($"محاولة إضافة عمود باسم غير صالح: {columnName}");
                    return;
                }

                if (!IsSafeColumnType(columnType))
                {
                    Logger.LogWarning($"محاولة إضافة عمود بتعريف نوع غير آمن: {columnType}");
                    return;
                }

                string normalizedColumnType = Regex.Replace(columnType.Trim(), @"\s+", " ");

                // التحقق من أعمدة الجدول الحالي
                string checkQuery = $"PRAGMA table_info(\"{tableName}\")";
                using var cmd = new SQLiteCommand(checkQuery, connection);
                using var reader = cmd.ExecuteReader();
                bool columnExists = false;
                // المرور على كل الأعمدة
                while (reader.Read())
                {
                    // إذا وجدنا العمود المطلوب
                    if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        columnExists = true;
                        break;
                    }
                }

                // إذا لم يكن موجوداً، نقوم بإضافته
                if (!columnExists)
                {
                    string alterQuery = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {normalizedColumnType}";
                    ExecuteNonQuery(alterQuery, connection);
                }
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ بدلاً من تجاهله بالكامل
                Logger.LogWarning($"خطأ أثناء محاولة إضافة العمود {columnName} للجدول {tableName}: {ex.Message}");
            }
        }

        private static bool IsSafeColumnType(string columnType)
        {
            if (string.IsNullOrWhiteSpace(columnType))
            {
                return false;
            }

            string normalized = Regex.Replace(columnType.Trim(), @"\s+", " ");

            if (normalized.Contains(';')
                || normalized.Contains("--", StringComparison.Ordinal)
                || normalized.Contains("/*", StringComparison.Ordinal)
                || normalized.Contains("*/", StringComparison.Ordinal))
            {
                return false;
            }

            return SafeColumnTypePattern.IsMatch(normalized);
        }

        // دالة للتحقق مما إذا كانت قاعدة البيانات تحتوي على أي بيانات (منتجات)
        public static bool HasData()
        {
            using var connection = GetConnection();
            string query = "SELECT COUNT(*) FROM Products";
            using var command = new SQLiteCommand(query, connection);
            var result = command.ExecuteScalar();
            return Convert.ToInt32(result) > 0; // إرجاع true إذا كان العدد أكبر من صفر
        }

        // دالة لعمل نسخة احتياطية من قاعدة البيانات
        public static void BackupDatabase(string backupPath)
        {
            string sourcePath = GetDatabasePath(); // المسار الحالي
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, backupPath, true); // نسخ الملف للمسار الجديد (مع السماح بالاستبدال)
            }
        }

        private static bool ShouldRunStructuralMigration(SQLiteConnection connection)
        {
            return !TableExists(connection, "Returns")
                || !TableExists(connection, "ReturnItems")
                || !TableExists(connection, "Shifts")
                || !TableExists(connection, "SuspendedSales")
                || !TableExists(connection, "SuspendedSaleItems")
                || !IndexExists(connection, "idx_returns_saleid")
                || !IndexExists(connection, "idx_returns_date")
                || !IndexExists(connection, "idx_returnitems_returnid")
                || !IndexExists(connection, "idx_returnitems_saleitemid")
                || !IndexExists(connection, "idx_sales_saledate")
                || !IndexExists(connection, "idx_sales_customerid")
                || !IndexExists(connection, "idx_sales_shiftid")
                || !IndexExists(connection, "idx_returns_shiftid")
                || !IndexExists(connection, "idx_saleitems_saleid")
                || !IndexExists(connection, "idx_saleitems_productid")
                || !IndexExists(connection, "idx_products_name")
                || !IndexExists(connection, "idx_products_code")
                || !IndexExists(connection, "idx_activitylogs_logdate")
                || !IndexExists(connection, "idx_shifts_status")
                || !IndexExists(connection, "idx_suspendedsales_createdat");
        }

        private static void RunStructuralMigrations(SQLiteConnection connection)
        {
            Logger.LogInfo("التحقق من جداول المرتجعات (Returns/ReturnItems)...");
            // جداول المرتجعات
            string createReturns = @"
                    CREATE TABLE IF NOT EXISTS Returns (
                        ReturnID INTEGER PRIMARY KEY AUTOINCREMENT,
                        SaleID INTEGER NOT NULL,
                        ShiftID INTEGER,
                        ReturnDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Reason TEXT NOT NULL,
                        TotalRefund REAL DEFAULT 0,
                        CreatedBy INTEGER,
                        CreatedDate DATETIME,
                        ModifiedBy INTEGER,
                        ModifiedDate DATETIME,
                        FOREIGN KEY (SaleID) REFERENCES Sales(SaleID),
                        FOREIGN KEY (ShiftID) REFERENCES Shifts(ShiftID)
                    );";
            ExecuteNonQuery(createReturns, connection);
            Logger.LogInfo("تم التحقق من جدول Returns.");

            string createReturnItems = @"
                    CREATE TABLE IF NOT EXISTS ReturnItems (
                        ReturnItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                        ReturnID INTEGER NOT NULL,
                        SaleItemID INTEGER NOT NULL,
                        ProductID INTEGER NOT NULL,
                        Quantity INTEGER NOT NULL,
                        UnitPrice REAL NOT NULL,
                        DiscountPercent REAL DEFAULT 0,
                        RefundAmount REAL NOT NULL,
                        FOREIGN KEY (ReturnID) REFERENCES Returns(ReturnID) ON DELETE CASCADE,
                        FOREIGN KEY (SaleItemID) REFERENCES SaleItems(SaleItemID),
                        FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
                    );";
            ExecuteNonQuery(createReturnItems, connection);
            Logger.LogInfo("تم التحقق من جدول ReturnItems.");

            string createShifts = @"
                    CREATE TABLE IF NOT EXISTS Shifts (
                        ShiftID INTEGER PRIMARY KEY AUTOINCREMENT,
                        OpenedBy INTEGER NOT NULL,
                        OpenedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        OpeningCash REAL NOT NULL DEFAULT 0,
                        ClosedBy INTEGER,
                        ClosedAt DATETIME,
                        ClosingCash REAL,
                        CashSalesTotal REAL DEFAULT 0,
                        CardSalesTotal REAL DEFAULT 0,
                        TransferSalesTotal REAL DEFAULT 0,
                        CreditSalesTotal REAL DEFAULT 0,
                        CashRefundsTotal REAL DEFAULT 0,
                        ExpectedCash REAL DEFAULT 0,
                        CashDifference REAL DEFAULT 0,
                        Notes TEXT,
                        Status TEXT NOT NULL DEFAULT 'Open',
                        FOREIGN KEY (OpenedBy) REFERENCES Users(UserID),
                        FOREIGN KEY (ClosedBy) REFERENCES Users(UserID)
                    );";
            ExecuteNonQuery(createShifts, connection);
            Logger.LogInfo("تم التحقق من جدول Shifts.");

            string createSuspendedSales = @"
                    CREATE TABLE IF NOT EXISTS SuspendedSales (
                        SuspendedSaleID INTEGER PRIMARY KEY AUTOINCREMENT,
                        CustomerID INTEGER,
                        Notes TEXT,
                        Discount REAL DEFAULT 0,
                        Tax REAL DEFAULT 0,
                        PaymentMethod TEXT DEFAULT 'Cash',
                        CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CreatedBy INTEGER,
                        ShiftID INTEGER,
                        Subtotal REAL DEFAULT 0,
                        FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
                        FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
                        FOREIGN KEY (ShiftID) REFERENCES Shifts(ShiftID)
                    );";
            ExecuteNonQuery(createSuspendedSales, connection);
            Logger.LogInfo("تم التحقق من جدول SuspendedSales.");

            string createSuspendedSaleItems = @"
                    CREATE TABLE IF NOT EXISTS SuspendedSaleItems (
                        SuspendedSaleItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                        SuspendedSaleID INTEGER NOT NULL,
                        ProductID INTEGER NOT NULL,
                        ProductName TEXT,
                        Quantity INTEGER NOT NULL,
                        UnitPrice REAL NOT NULL,
                        DiscountPercent REAL DEFAULT 0,
                        TotalPrice REAL NOT NULL,
                        FOREIGN KEY (SuspendedSaleID) REFERENCES SuspendedSales(SuspendedSaleID) ON DELETE CASCADE,
                        FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
                    );";
            ExecuteNonQuery(createSuspendedSaleItems, connection);
            Logger.LogInfo("تم التحقق من جدول SuspendedSaleItems.");

            AddColumnIfNotExists(connection, "Sales", "ShiftID", "INTEGER");
            AddColumnIfNotExists(connection, "Returns", "ShiftID", "INTEGER");
            AddColumnIfNotExists(connection, "SuspendedSales", "Subtotal", "REAL DEFAULT 0");

            // فهارس المرتجعات
            EnsureIndex(connection, "idx_returns_saleid", "CREATE INDEX idx_returns_saleid ON Returns(SaleID);");
            EnsureIndex(connection, "idx_returns_date", "CREATE INDEX idx_returns_date ON Returns(ReturnDate);");
            EnsureIndex(connection, "idx_returns_shiftid", "CREATE INDEX idx_returns_shiftid ON Returns(ShiftID);");
            EnsureIndex(connection, "idx_returnitems_returnid", "CREATE INDEX idx_returnitems_returnid ON ReturnItems(ReturnID);");
            EnsureIndex(connection, "idx_returnitems_saleitemid", "CREATE INDEX idx_returnitems_saleitemid ON ReturnItems(SaleItemID);");

            // فهارس أداء عامة
            EnsureIndex(connection, "idx_sales_saledate", "CREATE INDEX idx_sales_saledate ON Sales(SaleDate);");
            EnsureIndex(connection, "idx_sales_customerid", "CREATE INDEX idx_sales_customerid ON Sales(CustomerID);");
            EnsureIndex(connection, "idx_sales_shiftid", "CREATE INDEX idx_sales_shiftid ON Sales(ShiftID);");
            EnsureIndex(connection, "idx_saleitems_saleid", "CREATE INDEX idx_saleitems_saleid ON SaleItems(SaleID);");
            EnsureIndex(connection, "idx_saleitems_productid", "CREATE INDEX idx_saleitems_productid ON SaleItems(ProductID);");
            EnsureIndex(connection, "idx_products_name", "CREATE INDEX idx_products_name ON Products(Name);");
            EnsureIndex(connection, "idx_products_code", "CREATE INDEX idx_products_code ON Products(Code);");
            EnsureIndex(connection, "idx_activitylogs_logdate", "CREATE INDEX idx_activitylogs_logdate ON ActivityLogs(LogDate);");
            EnsureIndex(connection, "idx_shifts_status", "CREATE INDEX idx_shifts_status ON Shifts(Status, OpenedAt);");
            EnsureIndex(connection, "idx_suspendedsales_createdat", "CREATE INDEX idx_suspendedsales_createdat ON SuspendedSales(CreatedAt);");
        }

        private static void EnsureIndex(SQLiteConnection connection, string indexName, string createIndexSql)
        {
            if (!IndexExists(connection, indexName))
            {
                ExecuteNonQuery(createIndexSql, connection);
                Logger.LogInfo($"تم إنشاء الفهرس: {indexName}");
            }
        }

        private static bool TableExists(SQLiteConnection connection, string tableName)
        {
            string sql = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = @TableName";
            using var cmd = new SQLiteCommand(sql, connection);
            _ = cmd.Parameters.AddWithValue("@TableName", tableName);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static bool IndexExists(SQLiteConnection connection, string indexName)
        {
            string sql = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'index' AND name = @IndexName";
            using var cmd = new SQLiteCommand(sql, connection);
            _ = cmd.Parameters.AddWithValue("@IndexName", indexName);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // دالة لاستعادة نسخة احتياطية
        public static void RestoreDatabase(string backupPath)
        {
            string destinationPath = GetDatabasePath(); // المسار الهدف
            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, destinationPath, true); // نسخ ملف النسخة الاحتياطية للمكان الأصلي
            }
        }

        /// <summary>
        /// حذف قاعدة البيانات الحالية وإعادة تهيئتها
        /// </summary>
        public static void ResetDatabase()
        {
            try
            {
                string dbPath = GetDatabasePath();

                // تنظيف أي اتصالات مفتوحة
                SQLiteConnection.ClearAllPools();

                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                InitializeDatabase();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في إعادة تهيئة قاعدة البيانات");
                throw;
            }
        }
    }
}
