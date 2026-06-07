using Dapper;
using GroceryStoreManagement.Helpers;
using GroceryStoreManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GroceryStoreManagement.DAL
{
    public static class PromotionDAL
    {
        /// <summary>
        /// الحصول على جميع العروض
        /// </summary>
        public static List<Promotion> GetAllPromotions()
        {
            using var conn = DatabaseHelper.GetConnection();
            string sql = "SELECT * FROM Promotions ORDER BY CreatedDate DESC";
            return [.. conn.Query<Promotion>(sql)];
        }

        /// <summary>
        /// الحصول على العروض النشطة فقط (السارية حالياً)
        /// </summary>
        public static List<Promotion> GetActivePromotions()
        {
            using var conn = DatabaseHelper.GetConnection();
            var now = DateTime.Now;
            string sql = @"
                    SELECT * FROM Promotions 
                    WHERE IsActive = 1 
                    AND StartDate <= @Now 
                    AND EndDate >= @Now";
            return [.. conn.Query<Promotion>(sql, new { Now = now })];
        }

        /// <summary>
        /// إضافة عرض جديد
        /// </summary>
        public static void AddPromotion(Promotion promo)
        {
            using var conn = DatabaseHelper.GetConnection();
            string sql = @"
                    INSERT INTO Promotions (Name, DiscountType, DiscountValue, StartDate, EndDate, MinPurchase, AppliesTo, TargetID, TargetName, IsActive, CreatedDate)
                    VALUES (@Name, @DiscountType, @DiscountValue, @StartDate, @EndDate, @MinPurchase, @AppliesTo, @TargetID, @TargetName, @IsActive, @CreatedDate)";

            promo.CreatedDate = DateTime.Now;
            _ = conn.Execute(sql, promo);
        }

        /// <summary>
        /// تعديل عرض موجود
        /// </summary>
        public static void UpdatePromotion(Promotion promo)
        {
            using var conn = DatabaseHelper.GetConnection();
            string sql = @"
                    UPDATE Promotions 
                    SET Name = @Name, 
                        DiscountType = @DiscountType, 
                        DiscountValue = @DiscountValue, 
                        StartDate = @StartDate, 
                        EndDate = @EndDate, 
                        MinPurchase = @MinPurchase, 
                        AppliesTo = @AppliesTo, 
                        TargetID = @TargetID, 
                        TargetName = @TargetName,
                        IsActive = @IsActive
                    WHERE PromotionID = @PromotionID";

            _ = conn.Execute(sql, promo);
        }

        /// <summary>
        /// حذف عرض
        /// </summary>
        public static void DeletePromotion(int promotionId)
        {
            using var conn = DatabaseHelper.GetConnection();
            _ = conn.Execute("DELETE FROM Promotions WHERE PromotionID = @Id", new { Id = promotionId });
        }

        /// <summary>
        /// الحصول على أفضل عرض لمنتج معين
        /// </summary>
        /// <param name="productId">رقم المنتج</param>
        /// <param name="categoryName">فئة المنتج</param>
        /// <param name="price">سعر المنتج</param>
        /// <returns>كائن العرض الأفضل أو null</returns>
        public static Promotion GetBestPromotionForProduct(int productId, string categoryName, decimal price)
        {
            var activePromos = GetActivePromotions();

            // فلترة العروض المناسبة
            var applicablePromos = activePromos.Where(p =>
            {
                if (price < p.MinPurchase) return false;

                if (p.AppliesTo == "All") return true;
                if (p.AppliesTo == "Product" && p.TargetID == productId) return true;
                if (p.AppliesTo == "Category" && p.TargetName == categoryName) return true;

                return false;
            }).ToList();

            if (applicablePromos.Count == 0) return null;

            // حساب قيمة الخصم لكل عرض واختيار الأفضل
            Promotion bestPromo = null;
            decimal maxDiscount = -1;

            foreach (var promo in applicablePromos)
            {
                decimal currentDiscount = 0;
                if (promo.DiscountType == "Percentage")
                {
                    currentDiscount = price * (promo.DiscountValue / 100);
                }
                else
                {
                    currentDiscount = promo.DiscountValue;
                }

                if (currentDiscount > maxDiscount)
                {
                    maxDiscount = currentDiscount;
                    bestPromo = promo;
                }
            }

            return bestPromo;
        }
    }
}
