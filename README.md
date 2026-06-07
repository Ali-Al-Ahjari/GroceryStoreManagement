# 🛒 نظام إدارة متجر البقالة - Grocery Store Management System

<div align="center">

![Version](https://img.shields.io/badge/version-2.0.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![Framework](https://img.shields.io/badge/.NET-10.0--windows-purple.svg)
![IDE](https://img.shields.io/badge/Visual_Studio-2026-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

**نظام متكامل لإدارة محلات البقالة والسوبرماركت**

[المميزات](#-المميزات) • [المتطلبات](#-المتطلبات) • [التثبيت](#-التثبيت) • [هيكل المشروع](#-هيكل-المشروع) • [الاستخدام](#-الاستخدام)

</div>

---

## 📋 نظرة عامة

نظام إدارة متجر البقالة هو تطبيق سطح مكتب متكامل مبني على تقنية **WPF (Windows Presentation Foundation)** باستخدام **.NET 10.0** و **Visual Studio 2026**. يوفر النظام حلاً شاملاً لإدارة جميع جوانب العمل في محلات البقالة والسوبرماركت.

### 🎯 الأهداف الرئيسية

- إدارة المنتجات والمخزون بكفاءة
- تتبع المبيعات والمشتريات
- إدارة العملاء والموردين
- توليد التقارير والإحصائيات
- واجهة مستخدم عربية حديثة وسهلة الاستخدام

---

## ✨ المميزات

### 📊 لوحة التحكم (Dashboard)

- عرض إحصائيات المبيعات اليومية والشهرية
- رسوم بيانية توضيحية للأداء
- تنبيهات المخزون المنخفض
- ملخص سريع لأهم المؤشرات

### 📦 إدارة المنتجات

| الميزة | الوصف |
|--------|-------|
| ✅ إضافة منتجات | إضافة منتجات جديدة مع جميع التفاصيل |
| ✅ تعديل المنتجات | تحديث بيانات المنتجات الموجودة |
| ✅ حذف المنتجات | حذف المنتجات مع التحقق من الارتباطات |
| ✅ البحث والفلترة | البحث بالاسم، الكود، الفئة، أو المورد |
| ✅ تصنيف المنتجات | تنظيم المنتجات حسب الفئات |
| ✅ تتبع الأسعار | سعر الشراء وسعر البيع |
| ✅ إدارة الكميات | تحديث الكميات يدوياً أو آلياً |
| ✅ استيراد/تصدير | دعم ملفات Excel |

### 👥 إدارة العملاء

- قاعدة بيانات شاملة للعملاء
- معلومات الاتصال (الهاتف، البريد، العنوان)
- سجل المشتريات لكل عميل
- إحصائيات العملاء

### 🏢 إدارة الموردين

- تسجيل بيانات الموردين
- ربط المنتجات بالموردين
- تتبع التوريدات
- سجل التعاملات

### 🛒 نظام المشتريات

| الميزة | الوصف |
|--------|-------|
| ✅ فواتير الشراء | إنشاء فواتير شراء من الموردين |
| ✅ اختيار المورد | اختيار المورد مع إمكانية إضافة مورد جديد |
| ✅ إضافة المنتجات | إضافة منتجات متعددة للفاتورة |
| ✅ الخصومات | خصم عام على الفاتورة |
| ✅ المدفوعات | تسجيل المبلغ المدفوع |
| ✅ حالة الدفع | تتبع (مدفوع، جزئي، غير مدفوع) |
| ✅ استيراد للمخزون | إضافة الكميات للمخزون تلقائياً |
| ✅ رقم فاتورة المورد | تسجيل رقم الفاتورة الأصلي |

### 💰 نظام المبيعات

| الميزة | الوصف |
|--------|-------|
| ✅ فواتير البيع | إنشاء فواتير مبيعات للعملاء |
| ✅ اختيار العميل | اختيار العميل أو البيع النقدي |
| ✅ إضافة المنتجات | إضافة منتجات متعددة للفاتورة |
| ✅ الخصومات | خصم على المنتج أو خصم عام |
| ✅ الضريبة | حساب الضريبة تلقائياً |
| ✅ طرق الدفع | نقدي، بطاقة، تحويل |
| ✅ حالة الدفع | تتبع المدفوعات والمستحقات |
| ✅ الطباعة | طباعة الفواتير |

### 📋 إدارة المخزون

- عرض جميع المنتجات مع الكميات
- تحديث الكميات يدوياً
- فلترة حسب حالة المخزون (منخفض، متوسط، جيد)
- تنبيهات المخزون المنخفض
- حساب قيمة المخزون الإجمالية

### 📈 التقارير

- تقارير المبيعات اليومية/الشهرية/السنوية
- تقارير المشتريات
- تقارير المخزون
- تقارير العملاء والموردين
- رسوم بيانية توضيحية

### 👤 إدارة المستخدمين

- تسجيل الدخول وتسجيل الخروج
- صلاحيات المستخدمين
- سجل النشاطات

---

## 💻 المتطلبات

### متطلبات النظام

| المكون | المتطلبات |
|--------|-----------|
| **نظام التشغيل** | Windows 7 SP1 أو أحدث |
| **المعالج** | Intel Core i3 أو ما يعادله |
| **الذاكرة** | 4 GB RAM (يُفضل 8 GB) |
| **القرص الصلب** | 500 MB مساحة فارغة |
| **الشاشة** | دقة 1366×768 أو أعلى |

### متطلبات البرمجيات

| المكون | الإصدار |
|--------|---------|
| **.NET** | 10.0-windows |
| **Visual Studio** | 2026 |
| **SQLite** | مضمن مع المشروع |

### الاعتماديات المستخدمة

- **NuGet (المشروع الرئيسي):** `QuestPDF` (الإصدار `2026.2.1`)
- **NuGet (مشروع الاختبارات):** `xUnit`, `FluentAssertions`, `Microsoft.NET.Test.Sdk`
- **مكتبات محلية داخل `lib/`:** `System.Data.SQLite`, `Dapper`, `EPPlus`, `LiveCharts`, `AForge`, `OpenCvSharp`, `ZXing`, `Microsoft.Extensions.Configuration.*`

---

## 🚀 التثبيت

### الطريقة الأولى: استخدام Visual Studio

```bash
# 1. استنساخ المستودع
git clone https://github.com/yourusername/GroceryStoreManagement.git

# 2. فتح المشروع في Visual Studio
# افتح الملف: GroceryStoreManagement.sln

# 3. استعادة حزم NuGet
# من القائمة: Tools > NuGet Package Manager > Restore NuGet Packages

# 4. بناء المشروع
# اضغط: Ctrl + Shift + B

# 5. تشغيل المشروع
# اضغط: F5
```

### الطريقة الثانية: استخدام سطر الأوامر

```powershell
# 1. استنساخ المستودع
git clone https://github.com/yourusername/GroceryStoreManagement.git
cd GroceryStoreManagement

# 2. استعادة الحزم وبناء المشروع
dotnet restore
dotnet build

# 3. تشغيل المشروع
dotnet run --project GroceryStoreManagement
```

### إعداد قاعدة البيانات
>
> ⚠️ **ملاحظة:** قاعدة البيانات تُنشأ تلقائياً عند التشغيل الأول!

يتم إنشاء قاعدة البيانات SQLite في:

```
[مجلد التطبيق]\Data\GroceryStore.db
```

### تسجيل الدخول لأول مرة

عند أول تشغيل (مع ترخيص فعّال) سيطلب النظام إنشاء حساب المدير من نافذة **Initial Setup**.
لا تعتمد على بيانات افتراضية ثابتة في الإنتاج.

### تجهيز نسخة النشر مع الترخيص

الإعداد الافتراضي الآن أبسط:

- التطبيق يحتوي المفتاح العام الافتراضي داخله.
- لا تحتاج نقل `license_public_key.pem` يدويًا لكل نسخة طالما تستخدم نفس زوج المفاتيح.
- كل ما تحتاجه لإصدار كود جديد هو بصمة الجهاز + المفتاح الخاص الموجود عندك.

يمكنك ربط أداة التفعيل الخارجية مباشرة عبر سكربت النشر:

```powershell
.\scripts\publish-release.ps1 `
  -LicenseIssuerPath "C:\Path\To\LicenseIssuer" `
  -LicensePublicKeyPath "C:\Path\To\license_public_key.pem"
```

يمكنك أيضاً استخدام متغيرات البيئة:

```powershell
$env:LICENSE_ISSUER_PATH="C:\Path\To\LicenseIssuer"
$env:LICENSE_PUBLIC_KEY_PATH="C:\Path\To\license_public_key.pem"
.\scripts\publish-release.ps1
```

للربط الدائم على جهازك:

```powershell
Copy-Item .\scripts\license-tool.local.example.ps1 .\scripts\license-tool.local.ps1
# عدّل القيم داخل الملف ثم شغّل:
.\scripts\publish-release.ps1
```

إذا كانت أداة الإصدار الخارجية مبنية بـ Python كما في إعدادك الحالي، يمكنك استخدامها مباشرة من داخل المشروع:

```powershell
.\scripts\invoke-license-issuer.ps1 -Action gen-keys -OutputDir artifacts\license-keys
.\scripts\invoke-license-issuer.ps1 -Action gen-token -PrivateKeyPath artifacts\license-keys\license_private_key.pem -MachineFingerprint "PASTE_FULL_FINGERPRINT" -Days 30 -Issuer "SAM"
```

والطريقة الأبسط يوميًا لإصدار مفتاح:

```powershell
.\scripts\issue-activation.ps1 -Days 30
```

إذا كانت رسالة طلب التفعيل منسوخة من العميل في الحافظة، نفس الأمر يكفي.
السكربت يستخرج البصمة تلقائيًا، ويُنشئ **رسالة رد جاهزة للإرسال** تحتوي كود التفعيل، ثم ينسخها للحافظة.

ولإخراج التوكن فقط (بدون رسالة جاهزة):

```powershell
.\scripts\issue-activation.ps1 -MachineFingerprint "PASTE_FULL_FINGERPRINT" -Days 30 -TokenOnly
```

> ملاحظة: السكربت يتأكد من وجود ونسخ `license_public_key.pem` داخل مجلد `publish\Data`.
> إذا كان المفتاح غير موجود، سيفشل النشر برسالة واضحة.

---

## 📁 هيكل المشروع

```
GroceryStoreManagement/
├── 📂 GroceryStoreManagement/              # المشروع الرئيسي
│   ├── 📂 Assets/                          # الموارد (صور، أيقونات)
│   │   └── 📂 Fonts/
│   │       └── NotoNaskhArabic-Regular.ttf # خط الواجهة العربية
│   │
│   ├── 📂 DAL/                             # طبقة الوصول للبيانات (Data Access Layer)
│   │   ├── CustomerDAL.cs                  # عمليات العملاء
│   │   ├── ProductDAL.cs                   # عمليات المنتجات
│   │   ├── PurchaseDAL.cs                  # عمليات المشتريات
│   │   ├── PurchaseItemDAL.cs              # عمليات عناصر المشتريات
│   │   ├── SaleDAL.cs                      # عمليات المبيعات
│   │   ├── SaleItemDAL.cs                  # عمليات عناصر المبيعات
│   │   ├── SupplierDAL.cs                  # عمليات الموردين
│   │   └── UserDAL.cs                      # عمليات المستخدمين
│   │
│   ├── 📂 Helpers/                         # الأدوات المساعدة
│   │   ├── DatabaseHelper.cs               # إدارة قاعدة البيانات
│   │   ├── PrintHelper.cs                  # طباعة الفواتير
│   │   ├── ReportHelper.cs                 # توليد التقارير
│   │   └── DisplayFormatExtensions.cs      # تنسيق عرض الأرقام والعملات
│   │
│   ├── 📂 Models/                          # نماذج البيانات
│   │   ├── Customer.cs                     # نموذج العميل
│   │   ├── Product.cs                      # نموذج المنتج
│   │   ├── Purchase.cs                     # نموذج فاتورة الشراء
│   │   ├── PurchaseItem.cs                 # نموذج عنصر الشراء
│   │   ├── ReportData.cs                   # نماذج التقارير
│   │   ├── Sale.cs                         # نموذج فاتورة البيع
│   │   ├── SaleItem.cs                     # نموذج عنصر البيع
│   │   ├── Supplier.cs                     # نموذج المورد
│   │   └── User.cs                         # نموذج المستخدم
│   │
│   ├── 📂 Windows/                         # نوافذ التطبيق
│   │   ├── MainWindow.xaml                 # النافذة الرئيسية
│   │   ├── MainWindow.xaml.cs
│   │   ├── LoginWindow.xaml                # نافذة تسجيل الدخول
│   │   ├── LoginWindow.xaml.cs
│   │   ├── DashboardWindow.xaml            # لوحة التحكم
│   │   ├── DashboardWindow.xaml.cs
│   │   ├── ProductsWindow.xaml             # إدارة المنتجات
│   │   ├── ProductsWindow.xaml.cs
│   │   ├── ProductDialog.xaml              # نافذة إضافة/تعديل منتج
│   │   ├── ProductDialog.xaml.cs
│   │   ├── CustomersWindow.xaml            # إدارة العملاء
│   │   ├── CustomersWindow.xaml.cs
│   │   ├── CustomerDialog.xaml             # نافذة إضافة/تعديل عميل
│   │   ├── CustomerDialog.xaml.cs
│   │   ├── SuppliersWindow.xaml            # إدارة الموردين
│   │   ├── SuppliersWindow.xaml.cs
│   │   ├── SupplierDialog.xaml             # نافذة إضافة/تعديل مورد
│   │   ├── SupplierDialog.xaml.cs
│   │   ├── SalesWindow.xaml                # إدارة المبيعات
│   │   ├── SalesWindow.xaml.cs
│   │   ├── SaleDialog.xaml                 # نافذة فاتورة البيع
│   │   ├── SaleDialog.xaml.cs
│   │   ├── PurchasesWindow.xaml            # إدارة المشتريات
│   │   ├── PurchasesWindow.xaml.cs
│   │   ├── PurchaseDialog.xaml             # نافذة فاتورة الشراء
│   │   ├── PurchaseDialog.xaml.cs
│   │   ├── InventoryWindow.xaml            # إدارة المخزون
│   │   ├── InventoryWindow.xaml.cs
│   │   ├── ReportsWindow.xaml              # التقارير
│   │   ├── ReportsWindow.xaml.cs
│   │   ├── UsersWindow.xaml                # إدارة المستخدمين
│   │   ├── UsersWindow.xaml.cs
│   │   └── UpdateQuantityDialog.xaml       # تحديث الكمية
│   │
│   ├── 📂 Styles/                          # أنماط التصميم
│   │   └── Styles.xaml                     # الأنماط العامة
│   │
│   ├── App.xaml                            # إعدادات التطبيق
│   ├── App.xaml.cs                         # كود بدء التطبيق
│   └── GroceryStoreManagement.csproj       # ملف المشروع
│
├── 📂 Data/                                # قاعدة البيانات (تُنشأ تلقائياً)
│   └── GroceryStore.db                     # ملف SQLite
│
├── README.md                               # هذا الملف
└── GroceryStoreManagement.sln              # ملف الحل
```

---

## 🗄️ هيكل قاعدة البيانات

### جدول المنتجات (Products)

```sql
CREATE TABLE Products (
    ProductID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,                 -- اسم المنتج
    Code TEXT,                          -- كود المنتج
    Unit TEXT,                          -- الوحدة (قطعة، كيلو، علبة)
    Price REAL NOT NULL,                -- السعر (للتوافق القديم)
    PurchasePrice REAL DEFAULT 0,       -- سعر الشراء
    SellingPrice REAL DEFAULT 0,        -- سعر البيع
    Quantity INTEGER DEFAULT 0,         -- الكمية المتوفرة
    MinQuantity INTEGER DEFAULT 5,      -- الحد الأدنى للتنبيه
    Category TEXT,                      -- الفئة
    SupplierID INTEGER,                 -- معرف المورد
    ImagePath TEXT,                     -- مسار الصورة
    CreatedDate DATETIME,               -- تاريخ الإضافة
    FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID)
);
```

### جدول العملاء (Customers)

```sql
CREATE TABLE Customers (
    CustomerID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,                 -- اسم العميل
    Phone TEXT,                         -- رقم الهاتف
    Email TEXT,                         -- البريد الإلكتروني
    Address TEXT                        -- العنوان
);
```

### جدول الموردين (Suppliers)

```sql
CREATE TABLE Suppliers (
    SupplierID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,                 -- اسم المورد
    Phone TEXT,                         -- رقم الهاتف
    Email TEXT,                         -- البريد الإلكتروني
    Address TEXT                        -- العنوان
);
```

### جدول المبيعات (Sales)

```sql
CREATE TABLE Sales (
    SaleID INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerID INTEGER,                 -- معرف العميل
    SaleDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    TotalAmount REAL,                   -- المبلغ الإجمالي
    PaidAmount REAL DEFAULT 0,          -- المبلغ المدفوع
    Discount REAL DEFAULT 0,            -- الخصم
    Tax REAL DEFAULT 0,                 -- الضريبة
    PaymentStatus TEXT DEFAULT 'Unpaid', -- حالة الدفع
    PaymentMethod TEXT DEFAULT 'Cash',  -- طريقة الدفع
    Notes TEXT,                         -- ملاحظات
    ItemCount INTEGER DEFAULT 0,        -- عدد العناصر
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
);
```

### جدول عناصر المبيعات (SaleItems)

```sql
CREATE TABLE SaleItems (
    SaleItemID INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleID INTEGER,                     -- معرف الفاتورة
    ProductID INTEGER,                  -- معرف المنتج
    Quantity INTEGER,                   -- الكمية
    UnitPrice REAL,                     -- سعر الوحدة
    TotalPrice REAL,                    -- السعر الإجمالي
    DiscountPercent REAL DEFAULT 0,     -- نسبة الخصم
    FOREIGN KEY (SaleID) REFERENCES Sales(SaleID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);
```

### جدول المشتريات (Purchases)

```sql
CREATE TABLE Purchases (
    PurchaseID INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierID INTEGER,                 -- معرف المورد
    TotalAmount REAL DEFAULT 0,         -- المبلغ الإجمالي
    PaidAmount REAL DEFAULT 0,          -- المبلغ المدفوع
    Discount REAL DEFAULT 0,            -- الخصم
    PaymentStatus TEXT DEFAULT 'Unpaid', -- حالة الدفع
    PurchaseDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Notes TEXT,                         -- ملاحظات
    InvoiceNumber TEXT,                 -- رقم فاتورة المورد
    ItemCount INTEGER DEFAULT 0,        -- عدد العناصر
    IsImported INTEGER DEFAULT 0,       -- هل تم الاستيراد للمخزون
    FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID)
);
```

### جدول عناصر المشتريات (PurchaseItems)

```sql
CREATE TABLE PurchaseItems (
    PurchaseItemID INTEGER PRIMARY KEY AUTOINCREMENT,
    PurchaseID INTEGER,                 -- معرف الفاتورة
    ProductID INTEGER,                  -- معرف المنتج
    Quantity INTEGER,                   -- الكمية
    UnitPrice REAL,                     -- سعر الوحدة
    TotalPrice REAL,                    -- السعر الإجمالي
    FOREIGN KEY (PurchaseID) REFERENCES Purchases(PurchaseID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);
```

### جدول المستخدمين (Users)

```sql
CREATE TABLE Users (
    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT UNIQUE NOT NULL,      -- اسم المستخدم
    Password TEXT NOT NULL,             -- كلمة المرور
    FullName TEXT,                      -- الاسم الكامل
    Phone TEXT,                         -- الهاتف
    Email TEXT,                         -- البريد
    IsActive INTEGER DEFAULT 1,         -- نشط/غير نشط
    -- صلاحيات المستخدم
    CanAccessDashboard INTEGER DEFAULT 0,
    CanViewCustomers INTEGER DEFAULT 0,
    CanAddCustomers INTEGER DEFAULT 0,
    CanEditCustomers INTEGER DEFAULT 0,
    CanDeleteCustomers INTEGER DEFAULT 0,
    CanManageProducts INTEGER DEFAULT 0,
    CanManageInvoices INTEGER DEFAULT 0,
    CanViewReports INTEGER DEFAULT 0,
    CanManageSettings INTEGER DEFAULT 0,
    CanBackup INTEGER DEFAULT 0
);
```

### جدول سجل النشاطات (ActivityLogs)

```sql
CREATE TABLE ActivityLogs (
    LogID INTEGER PRIMARY KEY AUTOINCREMENT,
    UserID INTEGER,                     -- معرف المستخدم
    Action TEXT,                        -- نوع العملية
    Details TEXT,                       -- التفاصيل
    LogDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
```

---

## 📖 الاستخدام

### تسجيل الدخول

1. شغّل التطبيق
2. أدخل اسم المستخدم: `admin`
3. أدخل كلمة المرور: `admin`
4. اضغط "تسجيل الدخول"

### إضافة منتج جديد

1. اذهب إلى قسم **المنتجات**
2. اضغط على **➕ إضافة منتج**
3. املأ البيانات:
   - الكود (اختياري)
   - اسم المنتج
   - الفئة
   - الوحدة
   - سعر الشراء
   - سعر البيع
   - الكمية
   - الحد الأدنى
   - المورد (اختياري)
4. اضغط **حفظ**

### إنشاء فاتورة مبيعات

1. اذهب إلى قسم **الفواتير**
2. اضغط على **➕ فاتورة جديدة**
3. اختر العميل (أو اتركه فارغاً للبيع النقدي)
4. ابحث عن المنتج وأضفه:
   - اختر المنتج
   - أدخل الكمية
   - أدخل السعر (يُملأ تلقائياً)
   - اضغط **إضافة**
5. كرر الخطوة 4 لكل منتج
6. أدخل الخصم إن وجد
7. أدخل المبلغ المدفوع
8. اضغط **حفظ**

### إنشاء فاتورة مشتريات

1. اذهب إلى قسم **المشتريات**
2. اضغط على **➕ فاتورة شراء جديدة**
3. اختر المورد
4. أدخل رقم فاتورة المورد (اختياري)
5. أضف المنتجات:
   - اختر المنتج
   - أدخل الكمية
   - أدخل سعر الشراء
   - اضغط **إضافة**
6. أدخل الخصم والمبلغ المدفوع
7. اضغط **حفظ**
8. لإضافة الكميات للمخزون، اضغط **📦 استيراد للمخزون**

### النسخ الاحتياطي

1. اذهب إلى **الإعدادات**
2. اضغط **نسخ احتياطي**
3. اختر مكان الحفظ
4. اضغط **حفظ**

---

## ⌨️ اختصارات لوحة المفاتيح

| الاختصار | الوظيفة |
|----------|---------|
| `Ctrl + 1` | لوحة التحكم |
| `Ctrl + 2` | المنتجات |
| `Ctrl + 3` | العملاء |
| `Ctrl + 4` | الموردين |
| `Ctrl + 5` | المبيعات |
| `Ctrl + 6` | المخزون |
| `Ctrl + 7` | التقارير |
| `Ctrl + Q` | الخروج |

---

## 🎨 التصميم والألوان

### نظام الألوان

| اللون | الكود | الاستخدام |
|-------|-------|-----------|
| الأزرق الأساسي | `#4880FF` | الأزرار الرئيسية، العناوين |
| الأخضر | `#27AE60` | النجاح، القيم الإيجابية |
| الأحمر | `#E74C3C` | الخطأ، التحذيرات، الحذف |
| البرتقالي | `#F39C12` | التنبيهات، المخزون المنخفض |
| الرمادي الفاتح | `#F5F6FA` | الخلفيات |
| الرمادي | `#606060` | النصوص الثانوية |

### الخطوط

- **العناوين:** Bold, Size 20-24
- **النصوص العادية:** Regular, Size 13-14
- **التسميات:** Size 11-12, Secondary Color

---

## 🔧 الصيانة وحل المشاكل

### مشاكل شائعة وحلولها

#### 1. خطأ "قاعدة البيانات غير موجودة"

```
الحل: تأكد من وجود مجلد Data في مجلد التطبيق
      أو أعد تشغيل التطبيق لإنشائها تلقائياً
```

#### 2. خطأ "لم يتم تعيين مرجع كائن"

```
الحل: تم إصلاح هذه المشكلة. تأكد من استخدام أحدث إصدار
```

#### 3. خطأ في الاتصال بقاعدة البيانات

```
الحل: تأكد من عدم فتح ملف قاعدة البيانات في برنامج آخر
```

#### 4. التطبيق لا يعمل

```
الحل: تأكد من تثبيت .NET Desktop Runtime 10.0 (Windows)
```

### النسخ الاحتياطي اليدوي

لعمل نسخة احتياطية يدوية، انسخ ملف:

```
[مجلد التطبيق]\Data\GroceryStore.db
```

### استعادة النسخة الاحتياطية

لاستعادة نسخة احتياطية:

1. أغلق التطبيق
2. استبدل ملف `GroceryStore.db` بالنسخة الاحتياطية
3. أعد تشغيل التطبيق

---

## 🔄 سجل التحديثات

### الإصدار 1.0.0 (ديسمبر 2024)

- ✅ الإصدار الأول
- ✅ إدارة المنتجات
- ✅ إدارة العملاء
- ✅ إدارة الموردين
- ✅ نظام المبيعات
- ✅ نظام المشتريات
- ✅ إدارة المخزون
- ✅ التقارير
- ✅ لوحة التحكم
- ✅ إدارة المستخدمين

---

## 🤝 المساهمة

نرحب بمساهماتكم! للمساهمة:

1. Fork المستودع
2. أنشئ فرع جديد (`git checkout -b feature/amazing-feature`)
3. Commit التغييرات (`git commit -m 'Add amazing feature'`)
4. Push للفرع (`git push origin feature/amazing-feature`)
5. افتح Pull Request

---

## 📄 الترخيص

هذا المشروع مرخص بموجب رخصة MIT - راجع ملف [LICENSE](LICENSE) للتفاصيل.

---

## 📞 الدعم والتواصل

- 📧 البريد الإلكتروني: <support@example.com>
- 🐛 الإبلاغ عن المشاكل: [GitHub Issues](https://github.com/yourusername/GroceryStoreManagement/issues)
- 💬 المناقشات: [GitHub Discussions](https://github.com/yourusername/GroceryStoreManagement/discussions)

---

<div align="center">

**صنع بـ ❤️ للمجتمع العربي**

⭐ إذا أعجبك المشروع، لا تنسَ إعطاءه نجمة! ⭐

</div>
