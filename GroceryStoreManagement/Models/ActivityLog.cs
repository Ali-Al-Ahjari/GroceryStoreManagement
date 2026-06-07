using System; // دوال الوقت والتاريخ

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج سجل النشاط - يمثل عملية قام بها مستخدم في النظام
    /// يستخدم للمراجعة والتدقيق وتتبع التغييرات
    /// </summary>
    public class ActivityLog
    {
        /// <summary>
        /// المعرف الفريد للسجل
        /// </summary>
        public int LogID { get; set; }

        /// <summary>
        /// معرف المستخدم الذي قام بالعملية
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// اسم المستخدم (يتم جلبه عبر JOIN للعرض المباشر)
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// نوع العملية: Login (دخول), Add (إضافة), Update (تعديل), Delete (حذف), Backup (نسخ)
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// تفاصيل إضافية حول العملية (مثلاً: تم تعديل سعر المنتج س)
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// وقت وتاريخ العملية
        /// </summary>
        public DateTime LogDate { get; set; }

        // --- خصائص للعرض (UI Helper) ---

        /// <summary>
        /// ترجمة عربية لنوع العملية
        /// </summary>
        public string ActionTypeAR
        {
            get
            {
                return Action switch
                {
                    "Login" => "تسجيل دخول",
                    "Logout" => "تسجيل خروج",
                    "Add" => "إضافة",
                    "Update" => "تعديل",
                    "Delete" => "حذف",
                    "Backup" => "نسخ احتياطي",
                    "Restore" => "استعادة نسخة",
                    _ => Action,
                };
            }
        }

        /// <summary>
        /// لون لنوع العملية
        /// </summary>
        public string ActionTypeColor
        {
            get
            {
                return Action switch
                {
                    "Login" => "#3B82F6",// Blue
                    "Logout" => "#6B7280",// Gray
                    "Add" => "#10B981",// Green
                    "Update" => "#F59E0B",// Orange
                    "Delete" => "#EF4444",// Red
                    "Backup" => "#8B5CF6",// Purple
                    "Restore" => "#EC4899",// Pink
                    _ => "#6366F1",// Indigo
                };
            }
        }

        /// <summary>
        /// أيقونة لنوع العملية
        /// </summary>
        public string ActionIcon
        {
            get
            {
                return Action switch
                {
                    "Login" => "🔑",
                    "Logout" => "🚪",
                    "Add" => "➕",
                    "Update" => "📝",
                    "Delete" => "🗑️",
                    "Backup" => "💾",
                    "Restore" => "🔄",
                    _ => "📋",
                };
            }
        }

        /// <summary>
        /// خاصية للعرض في التقارير (توافق مع بعض البايندينج القديم)
        /// </summary>
        public DateTime Timestamp => LogDate;

        /// <summary>
        /// تنسيق الوقت للعرض (مثلاً: منذ 5 دقائق أو التاريخ)
        /// </summary>
        public string DisplayDate
        {
            get
            {
                var diff = DateTime.Now - LogDate;
                if (diff.TotalSeconds < 60) return "الآن";
                if (diff.TotalMinutes < 60) return $"منذ {Math.Floor(diff.TotalMinutes)} د";
                if (diff.TotalHours < 24) return $"منذ {Math.Floor(diff.TotalHours)} س";
                if (diff.TotalDays < 7) return $"منذ {Math.Floor(diff.TotalDays)} يوم";
                return LogDate.ToString("yyyy/MM/dd");
            }
        }

        /// <summary>
        /// خاصية للعرض في الجداول (تستخدم في البايندينج)
        /// </summary>
        public string LogDateDisplay => DisplayDate;
    }
}
