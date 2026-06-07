using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GroceryStoreManagement.DAL
{
    public static class NotificationDAL
    {
        public static List<Notification> GetUnreadNotifications()
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            return [.. conn.Query<Notification>(
                "SELECT * FROM Notifications WHERE IsRead = 0 ORDER BY CreatedAt DESC")];
        }

        public static List<Notification> GetAllNotifications(int limit = 50)
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            return [.. conn.Query<Notification>(
                "SELECT * FROM Notifications ORDER BY CreatedAt DESC LIMIT @Limit", new { Limit = limit })];
        }

        public static void AddNotification(Notification note)
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            note.CreatedAt = DateTime.Now;
            string query = @"
                    INSERT INTO Notifications (Title, Message, Type, Source, IsRead, CreatedAt, RelatedEntity, RelatedID)
                    VALUES (@Title, @Message, @Type, @Source, 0, @CreatedAt, @RelatedEntity, @RelatedID)";
            _ = conn.Execute(query, note);
        }

        public static void MarkAsRead(int notificationId)
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            _ = conn.Execute("UPDATE Notifications SET IsRead = 1 WHERE NotificationID = @ID", new { ID = notificationId });
        }

        public static void MarkAllAsRead()
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            _ = conn.Execute("UPDATE Notifications SET IsRead = 1");
        }

        public static void DeleteNotification(int notificationId)
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            _ = conn.Execute("DELETE FROM Notifications WHERE NotificationID = @ID", new { ID = notificationId });
        }

        // التحقق من وجود إشعار مشابه (لمنع التكرار، مثلاً تنبيه المخزون لنفس المنتج)
        public static bool ExistsSimilarUnread(string source, int? relatedId, string type)
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            int count = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Notifications WHERE Source = @Source AND RelatedID = @RelatedID AND Type = @Type AND IsRead = 0",
                new { Source = source, RelatedID = relatedId, Type = type });
            return count > 0;
        }

        // التحقق من وجود إشعار مشابه تم إنشاؤه مؤخراً (مثلاً خلال 24 ساعة الماضية) لمنع التكرار المزعج
        public static bool ExistsSimilarRecent(string source, int? relatedId, string type, int hours = 24)
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            DateTime cutoffTime = DateTime.Now.AddHours(-hours);
            int count = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Notifications WHERE Source = @Source AND RelatedID = @RelatedID AND Type = @Type AND CreatedAt >= @CutoffTime",
                new { Source = source, RelatedID = relatedId, Type = type, CutoffTime = cutoffTime });
            return count > 0;
        }
    }
}
