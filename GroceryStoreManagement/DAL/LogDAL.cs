using Dapper;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GroceryStoreManagement.DAL
{
    public static class LogDAL
    {
        public static void AddLog(ActivityLog log)
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            string query = @"
                    INSERT INTO ActivityLogs (UserID, Action, Details, LogDate)
                    VALUES (@UserID, @Action, @Details, @LogDate)";
            _ = conn.Execute(query, log);
        }

        public static List<ActivityLog> GetLogs(int limit = 100)
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            string query = @"
                    SELECT L.*, U.Username 
                    FROM ActivityLogs L
                    LEFT JOIN Users U ON L.UserID = U.UserID
                    ORDER BY LogDate DESC 
                    LIMIT @Limit";
            return [.. conn.Query<ActivityLog>(query, new { Limit = limit })];
        }

        public static void ClearLogs()
        {
            using var conn = Helpers.DatabaseHelper.GetConnection();
            _ = conn.Execute("DELETE FROM ActivityLogs");
        }
    }
}
