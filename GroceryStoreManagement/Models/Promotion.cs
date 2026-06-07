using System;

namespace GroceryStoreManagement.Models
{
    /// <summary>
    /// نموذج يمثل عرض ترويجي أو خصم
    /// </summary>
    public class Promotion
    {
        public int PromotionID { get; set; }
        public string Name { get; set; }
        public string DiscountType { get; set; } = "Percentage"; // Percentage, Fixed
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal MinPurchase { get; set; }
        public string AppliesTo { get; set; } = "All"; // All, Category, Product
        public int TargetID { get; set; } // ProductID if AppliesTo is Product
        public string TargetName { get; set; } // Category Name if AppliesTo is Category
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }

        // خاصية مساعدة للعرض
        public string Status
        {
            get
            {
                if (!IsActive) return "غير نشط";
                if (DateTime.Now < StartDate) return "مجدول";
                if (DateTime.Now > EndDate) return "منتهي";
                return "ساري";
            }
        }

        public string DisplayDiscount
        {
            get
            {
                if (DiscountType == "Percentage")
                    return $"{DiscountValue}%";
                return $"{DiscountValue.ToDisplayCurrency()}";
            }
        }

        public string DisplayType
        {
            get
            {
                return DiscountType == "Percentage" ? "نسبة" : "مبلغ";
            }
        }
    }
}

