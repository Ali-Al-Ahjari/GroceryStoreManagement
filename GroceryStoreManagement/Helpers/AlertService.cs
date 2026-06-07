using GroceryStoreManagement.DAL; // استيراد طبقة الوصول للبيانات لجلب المنتجات والفواتير
using GroceryStoreManagement.Models; // استيراد نماذج البيانات (مثل Notification)
using System; // استيراد المكتبة الأساسية
using System.Collections.Generic; // لاستخدام القوائم (غير مستخدمة هنا حالياً لكنها مفيدة)
using System.Linq; // لاستخدام دوال الاستعلام مثل Where

namespace GroceryStoreManagement.Helpers // تحديد المجال
{
    // كلاس خدمة التنبيهات - مسؤول عن فحص قواعد العمل وتوليد إشعارات تلقائية
    public static class AlertService
    {
        // حد الأمان الافتراضي عندما لا يتم ضبط MinQuantity بشكل صحيح على المنتج
        private const int DefaultLowStockThreshold = 5;

        // الدالة الرئيسية التي تشغل جميع الفحوصات
        public static void RunChecks()
        {
            try
            {
                CheckLowStock(); // فحص المخزون المنخفض
                CheckUnpaidInvoices(); // فحص الفواتير غير المدفوعة
                CheckExpiringProducts(); // فحص المنتجات قريبة الانتهاء
            }
            catch (Exception ex)
            {
                // في حال حدوث خطأ، نكتفي بطباعته في نافذة الـ Debug لعدم إزعاج المستخدم
                // (التنبيهات ليست حيوية لدرجة إيقاف البرنامج)
                System.Diagnostics.Debug.WriteLine($"Error running alert checks: {ex.Message}");
            }
        }

        // دالة لفحص المنتجات التي قاربت على النفاد
        private static void CheckLowStock()
        {
            // جلب جميع المنتجات
            var products = ProductDAL.GetAllProducts();

            foreach (var product in products)
            {
                int threshold = Math.Max(product.MinQuantity, DefaultLowStockThreshold);

                // توحيد تعريف "منخفض المخزون" مع باقي النظام:
                // أي منتج <= MinQuantity أو <= 5 يعتبر منخفضاً.
                if (product.IsLowStock)
                {
                    // قبل إنشاء تنبيه جديد، نتأكد أنه لا يوجد تنبيه مشابه خلال الـ 24 ساعة الماضية
                    // (تجنباً لإغراق المستخدم بمئات التنبيهات لنفس المنتج حتى لو تم تعليمه كمقروء)
                    if (!NotificationDAL.ExistsSimilarRecent("Inventory", product.ProductID, "Warning", 24))
                    {
                        string unit = string.IsNullOrWhiteSpace(product.Unit) ? "وحدة" : product.Unit;

                        // إنشاء وإضافة إشعار جديد لقاعدة البيانات
                        NotificationDAL.AddNotification(new Notification
                        {
                            Title = "تنبيه مخزون منخفض", // عنوان التنبيه
                            Message = $"المنتج {product.Name} منخفض المخزون: الكمية الحالية {product.Quantity} {unit} (حد التنبيه: {threshold} {unit}).", // نص الرسالة
                            Type = "Warning", // نوع التنبيه (تحذير)
                            Source = "Inventory", // مصدر التنبيه (المخزون)
                            RelatedEntity = "Product", // الكيان المرتبط (منتج)
                            RelatedID = product.ProductID // معرف المنتج لفتح صفحته عند النقر
                        });
                    }
                }
            }
        }

        // دالة لفحص فواتير المبيعات التي لم تدفع وتأخرت
        private static void CheckUnpaidInvoices()
        {
            // جلب المبيعات وفلترة غير المدفوعة منها
            // ملاحظة: هذا يعتمد على وجود PaymentStatus في نموذج Sale
            var sales = SaleDAL.GetAllSales().Where(s => s.PaymentStatus != "Paid");

            foreach (var sale in sales)
            {
                // حساب عدد الأيام المنقضية منذ تاريخ الفاتورة
                var daysLate = (DateTime.Now - sale.SaleDate).TotalDays;

                // إذا مر أكثر من 30 يوم
                if (daysLate > 30)
                {
                    // التحقق من عدم وجود تنبيه سابق خلال 24 ساعة
                    if (!NotificationDAL.ExistsSimilarRecent("Sales", sale.SaleID, "Warning", 24))
                    {
                        NotificationDAL.AddNotification(new Notification
                        {
                            Title = "فاتورة مبيعات مستحقة",
                            Message = $"الفاتورة رقم {sale.SaleID} للعميل {sale.CustomerName} متأخرة منذ {Math.Floor(daysLate)} يوم.",
                            Type = "Warning",
                            Source = "Sales",
                            RelatedEntity = "Sale",
                            RelatedID = sale.SaleID
                        });
                    }
                }
            }
        }

        // دالة لفحص صلاحية المنتجات
        private static void CheckExpiringProducts()
        {
            var expiringProducts = ProductDAL.GetExpiringProducts(30); // 30 يوماً
            foreach (var product in expiringProducts)
            {
                // تحقق مما إذا كان هناك تنبيه مشابه خلال 24 ساعة لنفس المنتج ونفس النوع
                if (!NotificationDAL.ExistsSimilarRecent("Inventory", product.ProductID, "Expiry", 24))
                {
                    string msg = product.IsExpired
                        ? $"المنتج {product.Name} انتهت صلاحيته في {product.ExpiryDate?.ToShortDateString()}"
                        : $"المنتج {product.Name} ستنتهي صلاحيته قريباً ({product.ExpiryDate?.ToShortDateString()})";

                    NotificationDAL.AddNotification(new Notification
                    {
                        Title = product.IsExpired ? "منتج منتهي الصلاحية" : "قرب انتهاء الصلاحية",
                        Message = msg,
                        Type = product.IsExpired ? "Critical" : "Warning",
                        Source = "Inventory",
                        RelatedEntity = "Product",
                        RelatedID = product.ProductID
                    });
                }
            }
        }
    }
}
