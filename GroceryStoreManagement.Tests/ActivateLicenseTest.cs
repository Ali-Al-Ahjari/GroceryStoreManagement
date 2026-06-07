using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Data.SQLite;
using Xunit;
using GroceryStoreManagement.Helpers;

namespace GroceryStoreManagement.Tests
{
    public class ActivateLicenseTest
    {
        private static string Base64UrlEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            return base64.Split('=')[0].Replace('+', '-').Replace('/', '_');
        }

        [Fact]
        public void SeedUsersAndActivate()
        {
            // 1. Scaffold database schema using the application's built-in helper
            DatabaseHelper.InitializeDatabase();

            // 2. Generate/Activate License in test DB
            GenerateAndActivateNewLicense();

            // 3. Path to DBs
            string baseDir = @"d:\D\SAM\3\نظام متكامل لادارة المتجر";
            string testDb = Path.Combine(baseDir, @"GroceryStoreManagement.Tests\bin\Debug\net10.0-windows7.0\Data\GroceryStore.db");
            string appDb = Path.Combine(baseDir, @"GroceryStoreManagement\bin\Debug\net10.0-windows7.0\Data\GroceryStore.db");

            SeedDb(testDb);
            
            // Copy test database to app database to overwrite it with both active license and seeded users
            if (File.Exists(testDb))
            {
                string appDbDir = Path.GetDirectoryName(appDb);
                if (!Directory.Exists(appDbDir))
                {
                    Directory.CreateDirectory(appDbDir);
                }
                File.Copy(testDb, appDb, true);
                Console.WriteLine("Copied database from test to app directory.");
            }
        }

        private void GenerateAndActivateNewLicense()
        {
            string baseDir = @"d:\D\SAM\3\نظام متكامل لادارة المتجر";
            string tempPrivateKeyPath = Path.Combine(baseDir, @"artifacts\license_private_key_temp.pem");
            
            if (!File.Exists(tempPrivateKeyPath))
            {
                using var rsaNew = RSA.Create(3072);
                string privateKeyPemNew = rsaNew.ExportPkcs8PrivateKeyPem();
                string publicKeyPemNew = rsaNew.ExportSubjectPublicKeyInfoPem();
                string sourcePublicKeyPath = Path.Combine(baseDir, @"GroceryStoreManagement\Security\default_license_public_key.pem");
                File.WriteAllText(sourcePublicKeyPath, publicKeyPemNew, Encoding.ASCII);
                File.WriteAllText(tempPrivateKeyPath, privateKeyPemNew, Encoding.ASCII);
            }

            string privateKeyPem = File.ReadAllText(tempPrivateKeyPath);
            string machine = LicenseService.GetMachineFingerprint();

            DateTime now = DateTime.UtcNow;
            DateTime expiry = now.AddDays(365);

            string iatStr = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string expStr = expiry.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string nonce = Guid.NewGuid().ToString("N");
            string issuer = "SAM";

            string payloadJson = string.Format(
                "{{\"machine\":\"{0}\",\"exp\":\"{1}\",\"iat\":\"{2}\",\"issuer\":\"{3}\",\"nonce\":\"{4}\"}}",
                machine, expStr, iatStr, issuer, nonce
            );

            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            string payloadB64Url = Base64UrlEncode(payloadBytes);

            byte[] signature;
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(privateKeyPem);
                byte[] dataToSign = Encoding.UTF8.GetBytes(payloadB64Url);
                signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            string signatureB64Url = Base64UrlEncode(signature);
            string token = string.Format("{0}.{1}", payloadB64Url, signatureB64Url);

            var actResult = LicenseService.ActivateLicense(token);
            Assert.True(actResult.IsSuccess);
        }

        private void SeedDb(string dbPath)
        {
            if (!File.Exists(dbPath)) return;

            string connStr = $"Data Source={dbPath};Version=3;";
            using var conn = new SQLiteConnection(connStr);
            conn.Open();

            // Check if user '123' exists
            using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users WHERE Username = '123'", conn))
            {
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count == 0)
                {
                    string passHash = PasswordHelper.HashPassword("123");
                    string insertSql = @"
                        INSERT INTO Users (
                            Username, Password, FullName, RoleID, IsActive,
                            CanAccessDashboard, CanViewCustomers, CanAddCustomers, CanEditCustomers, CanDeleteCustomers,
                            CanManageProducts, CanManageInvoices, CanViewReports, CanManageSettings, CanBackup, CreatedDate
                        )
                        VALUES (
                            '123', @Password, 'Test Admin User', 1, 1,
                            1, 1, 1, 1, 1,
                            1, 1, 1, 1, 1, DATETIME('now')
                        );";
                    using var insertCmd = new SQLiteCommand(insertSql, conn);
                    insertCmd.Parameters.AddWithValue("@Password", passHash);
                    insertCmd.ExecuteNonQuery();
                    Console.WriteLine($"Seeded '123' admin user in DB: {dbPath}");
                }
            }

            // Seed some basic items to make sure other screens don't throw empty reference/navigation errors
            using (var checkSupp = new SQLiteCommand("SELECT COUNT(*) FROM Suppliers", conn))
            {
                if (Convert.ToInt32(checkSupp.ExecuteScalar()) == 0)
                {
                    using var cmd = new SQLiteCommand("INSERT INTO Suppliers (Name, Phone, Address) VALUES ('Default Supplier', '0500000000', 'Default Address')", conn);
                    cmd.ExecuteNonQuery();
                }
            }

            using (var checkProd = new SQLiteCommand("SELECT COUNT(*) FROM Products", conn))
            {
                if (Convert.ToInt32(checkProd.ExecuteScalar()) == 0)
                {
                    using var cmd = new SQLiteCommand("INSERT INTO Products (Name, Price, Quantity, Category, SupplierID, Code, SellingPrice, PurchasePrice, MinQuantity) VALUES ('Default Product', 10.0, 100, 'Category', 1, 'PRD-01', 10.0, 7.0, 5)", conn);
                    cmd.ExecuteNonQuery();
                }
            }

            using (var checkCust = new SQLiteCommand("SELECT COUNT(*) FROM Customers", conn))
            {
                if (Convert.ToInt32(checkCust.ExecuteScalar()) == 0)
                {
                    using var cmd = new SQLiteCommand("INSERT INTO Customers (Name, Phone, Address) VALUES ('Default Customer', '0500000001', 'Default Address')", conn);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
