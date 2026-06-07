using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace GroceryStoreManagement.Helpers
{
    /// <summary>
    /// تحميل وقراءة إعدادات النظام العامة من settings.ini
    /// </summary>
    public static class AppSettings
    {
        private static readonly Lock _lock = new();
        private static Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);

        static AppSettings()
        {
            Reload();
        }

        public static void Reload()
        {
            lock (_lock)
            {
                _settings = LoadSettingsFromFile();
            }
        }

        public static string GetString(string key, string defaultValue = "")
        {
            lock (_lock)
            {
                return _settings.TryGetValue(key, out string value) ? value : defaultValue;
            }
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            string value = GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture));
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : defaultValue;
        }

        public static decimal GetDecimal(string key, decimal defaultValue = 0m)
        {
            string value = GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture));

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedInvariant))
                return parsedInvariant;

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal parsedCurrent))
                return parsedCurrent;

            return defaultValue;
        }

        public static decimal GetVatPercent()
        {
            return GetDecimal("VATPercent", 0m);
        }

        public static int GetCurrencyIndex()
        {
            int index = GetInt("Currency", 0);
            return index switch
            {
                < 0 => 0,
                > 2 => 0,
                _ => index
            };
        }

        public static string GetCurrencySymbol()
        {
            return GetCurrencyIndex() switch
            {
                1 => "ر.س",
                2 => "$",
                _ => "ر.ي"
            };
        }

        public static CultureInfo BuildCulture()
        {
            int languageIndex = GetInt("Language", 0);
            string cultureName = languageIndex == 1 ? "en-US" : "ar-YE";

            var culture = (CultureInfo)new CultureInfo(cultureName).Clone();

            if (languageIndex == 0)
            {
                culture.NumberFormat.NativeDigits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];
                culture.NumberFormat.DigitSubstitution = DigitShapes.None;
            }

            culture.NumberFormat.CurrencySymbol = GetCurrencySymbol();
            return culture;
        }

        public static CultureInfo ApplyCulture()
        {
            var culture = BuildCulture();
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            return culture;
        }

        private static Dictionary<string, string> LoadSettingsFromFile()
        {
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "settings.ini");

            if (!File.Exists(path))
                return settings;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                int separatorIndex = rawLine.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                string key = rawLine[..separatorIndex].Trim();
                string value = rawLine[(separatorIndex + 1)..].Trim();

                if (!string.IsNullOrWhiteSpace(key))
                    settings[key] = value;
            }

            return settings;
        }
    }
}
