using GroceryStoreManagement.Models;

namespace GroceryStoreManagement.Helpers
{
    public static class SessionContext
    {
        public static User CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;
        public static int CurrentUserID => CurrentUser?.UserID ?? 0;
        public static string CurrentUsername => CurrentUser?.Username ?? "System";

        // رقم الوردية المفتوحة (يُستخدم لتسريع الوصول من الواجهات).
        public static int? CurrentShiftID { get; set; }
    }
}
