using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace GroceryStoreManagement.DAL
{
    public static class ActivityLogDAL
    {
        public static void AddLog(int? userId, string action, string details)
        {
            try
            {
                using SQLiteConnection connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        INSERT INTO ActivityLogs (UserID, Action, Details)
                        VALUES (@UserID, @Action, @Details)";

                _ = connection.Execute(query, new { UserID = userId, Action = action, Details = details });
            }
            catch (Exception ex)
            {
                // Log silently or to a file
                Console.WriteLine($"Error logging activity: {ex.Message}");
            }
        }

        public static List<ActivityLog> GetRecentLogs(int limit = 10)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT l.*, u.Username
                        FROM ActivityLogs l
                        LEFT JOIN Users u ON l.UserID = u.UserID
                        ORDER BY l.LogDate DESC
                        LIMIT @Limit";

                return [.. connection.Query<ActivityLog>(query, new { Limit = limit })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب سجل النشاطات: {ex.Message}", ex);
            }
        }

        public static List<ActivityLog> GetLogsByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                using var connection = Helpers.DatabaseHelper.GetConnection();
                string query = @"
                        SELECT l.*, u.Username
                        FROM ActivityLogs l
                        LEFT JOIN Users u ON l.UserID = u.UserID
                        WHERE DATE(l.LogDate) BETWEEN DATE(@FromDate) AND DATE(@ToDate)
                        ORDER BY l.LogDate DESC";

                return [.. connection.Query<ActivityLog>(query, new { FromDate = fromDate, ToDate = toDate })];
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب سجل النشاطات حسب التاريخ: {ex.Message}", ex);
            }
        }
    }
}

