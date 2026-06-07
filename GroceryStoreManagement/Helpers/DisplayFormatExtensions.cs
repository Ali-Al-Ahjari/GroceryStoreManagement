using System;
using System.Globalization;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// تنسيقات عرض الأرقام والمبالغ بشكل واضح (بدون .00 عند عدم الحاجة).
    /// </summary>
    public static class DisplayFormatExtensions
    {
        public static string ToDisplayCurrency(this decimal value) => FormatCurrency(value);
        public static string ToDisplayCurrency(this double value) => FormatCurrency((decimal)value);
        public static string ToDisplayCurrency(this float value) => FormatCurrency((decimal)value);

        public static string ToDisplayNumber(this decimal value) => value.ToString("#,##0.##", CultureInfo.CurrentCulture);
        public static string ToDisplayNumber(this double value) => value.ToString("#,##0.##", CultureInfo.CurrentCulture);
        public static string ToDisplayNumber(this float value) => value.ToString("#,##0.##", CultureInfo.CurrentCulture);

        public static string FormatCurrency(decimal value)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            NumberFormatInfo format = culture.NumberFormat;

            string number = Math.Abs(value).ToString("#,##0.##", culture);
            string formatted = format.CurrencyPositivePattern switch
            {
                0 => $"{format.CurrencySymbol}{number}",
                1 => $"{number}{format.CurrencySymbol}",
                2 => $"{format.CurrencySymbol} {number}",
                3 => $"{number} {format.CurrencySymbol}",
                _ => $"{number}{format.CurrencySymbol}",
            };

            return value < 0 ? $"{format.NegativeSign}{formatted}" : formatted;
        }
    }
}
