using System;
using System.IO;
using System.Xml.Linq;

namespace LicenseIssuerCSharp
{
    public class AppSettings
    {
        public string CustomPrivateKeyPem { get; set; }
        public string DefaultIssuer { get; set; }
        public int DefaultExpiryDays { get; set; }

        public AppSettings()
        {
            CustomPrivateKeyPem = string.Empty;
            DefaultIssuer = "StoreOwner";
            DefaultExpiryDays = 30;
        }
    }

    public static class SettingsStorage
    {
        private static string GetSettingsPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "LicenseIssuerCSharp");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "settings.xml");
        }

        public static AppSettings LoadSettings()
        {
            string path = GetSettingsPath();
            var settings = new AppSettings();

            if (!File.Exists(path))
            {
                SaveSettings(settings);
                return settings;
            }

            try
            {
                XDocument doc = XDocument.Load(path);
                XElement root = doc.Element("Settings");
                if (root != null)
                {
                    settings.CustomPrivateKeyPem = root.Element("CustomPrivateKeyPem") != null ? root.Element("CustomPrivateKeyPem").Value : string.Empty;
                    settings.DefaultIssuer = root.Element("DefaultIssuer") != null ? root.Element("DefaultIssuer").Value : "StoreOwner";

                    int days;
                    var defaultExpiryDaysElem = root.Element("DefaultExpiryDays");
                    if (defaultExpiryDaysElem != null && int.TryParse(defaultExpiryDaysElem.Value, out days))
                    {
                        settings.DefaultExpiryDays = days > 0 ? days : 30;
                    }
                }
            }
            catch
            {
                // Fallback to defaults
            }

            return settings;
        }

        public static void SaveSettings(AppSettings settings)
        {
            string path = GetSettingsPath();
            try
            {
                var doc = new XDocument(new XElement("Settings",
                    new XElement("CustomPrivateKeyPem", settings.CustomPrivateKeyPem ?? string.Empty),
                    new XElement("DefaultIssuer", settings.DefaultIssuer ?? "StoreOwner"),
                    new XElement("DefaultExpiryDays", settings.DefaultExpiryDays.ToString())
                ));
                doc.Save(path);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
