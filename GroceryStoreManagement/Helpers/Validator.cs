// =====================================================
// Validator.cs - فئة التحقق من صحة البيانات
// تحتوي على دوال للتحقق من صحة المدخلات المختلفة
// =====================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// فئة ثابتة للتحقق من صحة البيانات
    /// تستخدم في جميع أنحاء التطبيق للتحقق من المدخلات
    /// </summary>
    public static partial class Validator
    {
        #region قواعد التحقق العامة

        /// <summary>
        /// التحقق من أن القيمة ليست فارغة أو مسافات فقط
        /// </summary>
        public static bool IsRequired(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// التحقق من صحة صيغة البريد الإلكتروني
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            // البريد اختياري - إذا كان فارغاً نعتبره صحيحاً
            if (string.IsNullOrWhiteSpace(email))
                return true;

            try
            {
                // استخدام تعبير نظامي للتحقق من صيغة البريد
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// التحقق من صحة رقم الهاتف السعودي أو الدولي
        /// </summary>
        /// </summary>
        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // تم تخفيف القيود لقبول أي رقم هاتف
            // التحقق فقط من أنه يحتوي على أرقام
            return MyRegex().IsMatch(phone);
        }



        /// <summary>
        /// التحقق من صحة السعر (رقم موجب)
        /// </summary>
        public static bool IsValidPrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price))
                return false;

            if (decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result >= 0;
            }
            return false;
        }

        /// <summary>
        /// التحقق من صحة الكمية (رقم صحيح موجب)
        /// </summary>
        public static bool IsValidQuantity(string quantity)
        {
            if (string.IsNullOrWhiteSpace(quantity))
                return false;

            if (int.TryParse(quantity, out int result))
            {
                return result >= 0;
            }
            return false;
        }

        /// <summary>
        /// التحقق من صحة الباركود
        /// </summary>
        public static bool IsValidBarcode(string barcode)
        {
            // الباركود اختياري
            if (string.IsNullOrWhiteSpace(barcode))
                return true;

            // قبول أي قيمة حتى 50 حرفاً
            return barcode.Length <= 50;
        }

        /// <summary>
        /// التحقق من صحة التاريخ
        /// </summary>
        public static bool IsValidDate(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return true;

            return DateTime.TryParse(date, out _);
        }

        #endregion

        #region دوال التحقق مع رسائل الخطأ (out errorMessage)

        /// <summary>
        /// التحقق من صحة الاسم مع رسالة خطأ
        /// </summary>
        public static bool ValidateName(string name, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "الاسم مطلوب";
                return false;
            }

            if (name.Length < 2)
            {
                errorMessage = "الاسم يجب أن يكون على الأقل حرفين";
                return false;
            }

            return true;
        }

        /// <summary>
        /// التحقق من صحة رقم الهاتف مع رسالة خطأ
        /// </summary>
        public static bool ValidatePhone(string phone, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(phone))
            {
                errorMessage = "رقم الهاتف مطلوب";
                return false;
            }

            if (!IsValidPhone(phone))
            {
                errorMessage = "رقم الهاتف غير صحيح";
                return false;
            }

            return true;
        }

        /// <summary>
        /// التحقق من صحة البريد الإلكتروني مع رسالة خطأ (اختياري)
        /// </summary>
        public static bool ValidateEmail(string email, out string errorMessage)
        {
            errorMessage = null;

            // البريد اختياري
            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            {
                errorMessage = "البريد الإلكتروني غير صحيح";
                return false;
            }

            return true;
        }

        /// <summary>
        /// التحقق من صحة العنوان مع رسالة خطأ (اختياري)
        /// </summary>
        public static bool ValidateAddress(string address, out string errorMessage)
        {
            _ = address;
            errorMessage = null;
            // العنوان اختياري - لا حاجة للتحقق
            return true;
        }

        /// <summary>
        /// التحقق من صحة السعر مع رسالة خطأ
        /// </summary>
        public static bool ValidatePrice(string price, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(price))
            {
                errorMessage = "السعر مطلوب";
                return false;
            }

            if (!decimal.TryParse(price, out decimal result) || result < 0)
            {
                errorMessage = "السعر يجب أن يكون رقماً موجباً";
                return false;
            }

            return true;
        }

        /// <summary>
        /// التحقق من صحة الكمية مع رسالة خطأ
        /// </summary>
        public static bool ValidateQuantity(string quantity, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(quantity))
            {
                errorMessage = "الكمية مطلوبة";
                return false;
            }

            if (!int.TryParse(quantity, out int result) || result < 0)
            {
                errorMessage = "الكمية يجب أن تكون رقماً صحيحاً موجباً";
                return false;
            }

            return true;
        }

        #endregion

        #region تحقق متقدم للمنتجات

        /// <summary>
        /// التحقق من صحة اسم المنتج (مع ValidationResult)
        /// </summary>
        public static ValidationResult ValidateProductName(string name)
        {
            if (!IsRequired(name))
                return new ValidationResult(false, "اسم المنتج مطلوب");

            if (name.Length < 2)
                return new ValidationResult(false, "اسم المنتج يجب أن يكون على الأقل حرفين");

            if (name.Length > 100)
                return new ValidationResult(false, "اسم المنتج يجب ألا يتجاوز 100 حرف");

            return ValidationResult.ValidResult;
        }

        /// <summary>
        /// التحقق من صحة سعر المنتج
        /// </summary>
        public static ValidationResult ValidateProductPrice(decimal? price)
        {
            if (price == null)
                return new ValidationResult(false, "السعر مطلوب");

            if (price < 0)
                return new ValidationResult(false, "السعر لا يمكن أن يكون سالباً");

            if (price > 999999.99m)
                return new ValidationResult(false, "السعر كبير جداً");

            return ValidationResult.ValidResult;
        }

        /// <summary>
        /// التحقق من صحة كمية المنتج
        /// </summary>
        public static ValidationResult ValidateProductQuantity(int? quantity)
        {
            if (quantity == null)
                return new ValidationResult(false, "الكمية مطلوبة");

            if (quantity < 0)
                return new ValidationResult(false, "الكمية لا يمكن أن تكون سالبة");

            if (quantity > 999999)
                return new ValidationResult(false, "الكمية كبيرة جداً");

            return ValidationResult.ValidResult;
        }

        #endregion

        #region تحقق متقدم للعملاء

        public static ValidationResult ValidateCustomerName(string name)
        {
            if (!IsRequired(name))
                return new ValidationResult(false, "اسم العميل مطلوب");

            if (name.Length < 2)
                return new ValidationResult(false, "اسم العميل يجب أن يكون على الأقل حرفين");

            if (name.Length > 100)
                return new ValidationResult(false, "اسم العميل يجب ألا يتجاوز 100 حرف");

            return ValidationResult.ValidResult;
        }

        public static ValidationResult ValidateCustomerPhone(string phone)
        {
            if (!IsRequired(phone))
                return new ValidationResult(false, "رقم الهاتف مطلوب");

            if (!IsValidPhone(phone))
                return new ValidationResult(false, "رقم الهاتف غير صحيح. يجب أن يبدأ بـ 05 ويتكون من 10 أرقام");

            return ValidationResult.ValidResult;
        }

        public static ValidationResult ValidateCustomerEmail(string email)
        {
            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                return new ValidationResult(false, "البريد الإلكتروني غير صحيح");

            return ValidationResult.ValidResult;
        }

        #endregion

        #region تحقق متقدم للموردين

        public static ValidationResult ValidateSupplierName(string name)
        {
            if (!IsRequired(name))
                return new ValidationResult(false, "اسم المورد مطلوب");

            if (name.Length < 2)
                return new ValidationResult(false, "اسم المورد يجب أن يكون على الأقل حرفين");

            return ValidationResult.ValidResult;
        }

        public static ValidationResult ValidateSupplierPhone(string phone)
        {
            if (!IsRequired(phone))
                return new ValidationResult(false, "رقم هاتف المورد مطلوب");

            if (!IsValidPhone(phone))
                return new ValidationResult(false, "رقم هاتف المورد غير صحيح");

            return ValidationResult.ValidResult;
        }

        #endregion

        #region تحقق متقدم للمستخدمين

        public static ValidationResult ValidateUsername(string username)
        {
            if (!IsRequired(username))
                return new ValidationResult(false, "اسم المستخدم مطلوب");

            if (username.Length < 3)
                return new ValidationResult(false, "اسم المستخدم يجب أن يكون على الأقل 3 أحرف");

            if (username.Length > 50)
                return new ValidationResult(false, "اسم المستخدم يجب ألا يتجاوز 50 حرفاً");

            if (MyRegex2().IsMatch(username))
                return ValidationResult.ValidResult;

            return new ValidationResult(false, "اسم المستخدم يمكن أن يحتوي على أحرف إنجليزية وأرقام وشرطة سفلية فقط");
        }

        public static ValidationResult ValidatePassword(string password)
        {
            if (!IsRequired(password))
                return new ValidationResult(false, "كلمة المرور مطلوبة");

            if (password.Length < 6)
                return new ValidationResult(false, "كلمة المرور يجب أن تكون على الأقل 6 أحرف");

            return ValidationResult.ValidResult;
        }

        public static ValidationResult ValidateFullName(string fullName)
        {
            if (!IsRequired(fullName))
                return new ValidationResult(false, "الاسم الكامل مطلوب");

            if (fullName.Length < 2)
                return new ValidationResult(false, "الاسم الكامل يجب أن يكون على الأقل حرفين");

            return ValidationResult.ValidResult;
        }

        #endregion

        #region تحقق متقدم للمبيعات

        public static ValidationResult ValidateSaleQuantity(int quantity, int availableQuantity)
        {
            if (quantity <= 0)
                return new ValidationResult(false, "الكمية يجب أن تكون أكبر من صفر");

            if (quantity > availableQuantity)
                return new ValidationResult(false, $"الكمية المطلوبة ({quantity}) أكبر من الكمية المتاحة ({availableQuantity})");

            return ValidationResult.ValidResult;
        }

        public static ValidationResult ValidatePaymentAmount(decimal amount, decimal total)
        {
            if (amount <= 0)
                return new ValidationResult(false, "المبلغ يجب أن يكون أكبر من صفر");

            if (amount < total)
                return new ValidationResult(false, "المبلغ المدفوع أقل من المبلغ الإجمالي");

            return ValidationResult.ValidResult;
        }

        #endregion

        #region أدوات مساعدة

        /// <summary>
        /// تنظيف رقم الهاتف وتحويله للصيغة الدولية
        /// </summary>
        public static string CleanPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // إزالة الأحرف غير المرغوب فيها فقط
            // السماح بالأرقام و علامة +
            return MyRegex11().Replace(phone, "");
        }

        /// <summary>
        /// تنسيق رقم الهاتف للعرض
        /// </summary>
        public static string FormatPhoneNumber(string phone)
        {
            phone = CleanPhoneNumber(phone);

            if (phone.StartsWith("+9665"))
            {
                // تنسيق: +966 5X XXX XXXX
                return $"{phone[..4]} {phone.Substring(4, 1)} {phone.Substring(5, 3)} {phone[8..]}";
            }

            return phone;
        }

        /// <summary>
        /// تحويل نص إلى رقم عشري
        /// </summary>
        public static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0;
        }

        /// <summary>
        /// تحويل نص إلى رقم صحيح
        /// </summary>
        public static int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (int.TryParse(value, out int result))
                return result;

            return 0;
        }

        /// <summary>
        /// التحقق من أن القيمة رقمية
        /// </summary>
        public static bool IsNumeric(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        /// <summary>
        /// الحصول على قائمة أخطاء التحقق
        /// </summary>
        public static List<string> GetValidationErrors(Dictionary<string, Func<bool>> validations)
        {
            var errors = new List<string>();

            foreach (var validation in validations)
            {
                if (!validation.Value())
                {
                    errors.Add(validation.Key);
                }
            }

            return errors;
        }

        /// <summary>
        /// تنسيق قائمة الأخطاء للعرض
        /// </summary>
        public static string FormatValidationErrors(List<string> errors)
        {
            if (errors == null || errors.Count == 0)
                return string.Empty;

            return "• " + string.Join("\n• ", errors);
        }

        [GeneratedRegex(@"[0-9]")]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"[^\d+]")]
        private static partial Regex MyRegex11();
        [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
        private static partial Regex MyRegex2();

        #endregion
    }

}
