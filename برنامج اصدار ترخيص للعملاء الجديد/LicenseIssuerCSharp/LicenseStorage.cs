using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LicenseIssuerCSharp
{
    public static class LicenseStorage
    {
        private static string GetStoragePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "LicenseIssuerCSharp");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "licenses.xml");
        }

        private static XDocument LoadDocument()
        {
            string path = GetStoragePath();
            if (File.Exists(path))
            {
                try
                {
                    return XDocument.Load(path);
                }
                catch
                {
                    // Fallback to empty document if corrupted
                }
            }

            var doc = new XDocument(new XElement("Licenses"));
            doc.Save(path);
            return doc;
        }

        public static void SaveLicense(LicenseRecord record)
        {
            string path = GetStoragePath();
            XDocument doc = LoadDocument();
            XElement root = doc.Element("Licenses");

            // Find next ID
            int nextId = 1;
            if (root.Elements("License").Any())
            {
                nextId = root.Elements("License").Max(e => e.Attribute("Id") != null ? (int)e.Attribute("Id") : 0) + 1;
            }

            record.Id = nextId;

            XElement element = new XElement("License",
                new XAttribute("Id", record.Id),
                new XElement("CustomerName", record.CustomerName),
                new XElement("MachineFingerprint", record.MachineFingerprint),
                new XElement("Issuer", record.Issuer),
                new XElement("IssueDate", record.IssueDate),
                new XElement("ExpiryDate", record.ExpiryDate),
                new XElement("Token", record.Token)
            );

            root.AddFirst(element); // Add to the top of the history list
            doc.Save(path);
        }

        public static List<LicenseRecord> GetAllLicenses()
        {
            try
            {
                XDocument doc = LoadDocument();
                XElement root = doc.Element("Licenses");
                if (root == null) return new List<LicenseRecord>();

                return root.Elements("License").Select(e => new LicenseRecord
                {
                    Id = e.Attribute("Id") != null ? (int)e.Attribute("Id") : 0,
                    CustomerName = e.Element("CustomerName") != null ? e.Element("CustomerName").Value : string.Empty,
                    MachineFingerprint = e.Element("MachineFingerprint") != null ? e.Element("MachineFingerprint").Value : string.Empty,
                    Issuer = e.Element("Issuer") != null ? e.Element("Issuer").Value : string.Empty,
                    IssueDate = e.Element("IssueDate") != null ? e.Element("IssueDate").Value : string.Empty,
                    ExpiryDate = e.Element("ExpiryDate") != null ? e.Element("ExpiryDate").Value : string.Empty,
                    Token = e.Element("Token") != null ? e.Element("Token").Value : string.Empty
                }).ToList();
            }
            catch
            {
                return new List<LicenseRecord>();
            }
        }

        public static void GetDashboardStats(out int total, out int active, out int expired)
        {
            total = 0;
            active = 0;
            expired = 0;

            List<LicenseRecord> list = GetAllLicenses();
            total = list.Count;

            DateTime nowUtc = DateTime.UtcNow;

            foreach (var record in list)
            {
                try
                {
                    DateTime expDate;
                    if (DateTime.TryParse(record.ExpiryDate, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out expDate))
                    {
                        if (expDate.ToUniversalTime() > nowUtc)
                        {
                            active++;
                        }
                        else
                        {
                            expired++;
                        }
                    }
                    else
                    {
                        expired++;
                    }
                }
                catch
                {
                    expired++;
                }
            }
        }
    }
}
