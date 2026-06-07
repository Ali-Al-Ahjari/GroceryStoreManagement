using System; // استيراد الوظائف الأساسية للوقت والتاريخ

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج الإشعار - يمثل التنبيهات التي تظهر للمستخدم
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// المعرف الفريد للإشعار
        /// </summary>
        public int NotificationID { get; set; }

        /// <summary>
        /// عنوان الإشعار (مختصر)
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// رسالة الإشعار التفصيلية
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// نوع الإشعار: Warning (تحذير), Error (خطأ), Info (معلومة), Success (نجاح)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// مصدر الإشعار: Inventory (مخزون), Sales (مبيعات), System (نظام), Backup (نسخ احتياطي)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// هل تم قراءة الإشعار من قبل المستخدم؟
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// وقت إنشاء الإشعار
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // --- حقول للربط مع الكيانات الأخرى ---

        /// <summary>
        /// نوع الكيان المرتبط (مثلاً: "Product" إذا كان تنبيه مخزون، "Invoice" إذا كان فاتورة)
        /// </summary>
        public string RelatedEntity { get; set; }

        /// <summary>
        /// رقم الكيان المرتبط (لفتح التفاصيل عند النقر)
        /// </summary>
        public int? RelatedID { get; set; }

        // --- خصائص للعرض (UI Properties) ---

        /// <summary>
        /// أيقونة تعبيرية بناءً على نوع الإشعار
        /// </summary>
        public string Icon
        {
            get
            {
                return Type switch
                {
                    "Warning" => "⚠️",// تحذير
                    "Error" => "❌",// خطأ
                    "Success" => "✅",// نجاح
                    _ => "ℹ️",// معلومة
                };
            }
        }

        /// <summary>
        /// لون الخلفية أو العلامة المميزة للإشعار
        /// </summary>
        public string Color
        {
            get
            {
                return Type switch
                {
                    "Warning" => "#FFC107",// أصفر (كهرماني)
                    "Error" => "#F44336",// أحمر
                    "Success" => "#4CAF50",// أخضر
                    _ => "#2196F3",// أزرق
                };
            }
        }

        /// <summary>
        /// لون الخلفية للإشعار بناءً على حالة القراءة
        /// </summary>
        public string BackgroundColor
        {
            get
            {
                return IsRead ? "Transparent" : "#F5F9FF"; // خلفية فاتحة للإشعارات غير المقروءة
            }
        }

        /// <summary>
        /// تاريخ الإنشاء بتنسيق مناسب للعرض
        /// </summary>
        public string CreatedAtDisplay
        {
            get
            {
                return CreatedAt.ToString("yyyy/MM/dd HH:mm");
            }
        }

        /// <summary>
        /// نص يعبر عن الوقت المنقضي منذ إنشاء الإشعار (مثلاً: منذ 5 دقائق)
        /// مفيد للعرض بدلاً من التاريخ الكامل
        /// </summary>
        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - CreatedAt;
                if (span.TotalMinutes < 1) return "الآن";
                if (span.TotalMinutes < 60) return $"منذ {span.Minutes} دقيقة";
                if (span.TotalHours < 24) return $"منذ {span.Hours} ساعة";
                return CreatedAt.ToString("yyyy/MM/dd"); // إذا مر أكثر من يوم، نعرض التاريخ
            }
        }
    }
}
