using System;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace GroceryStoreManagement.Helpers
{
    public enum LicenseState
    {
        Active = 0,
        NotActivated = 1,
        Expired = 2,
        Locked = 3
    }

    public sealed class LicenseCheckResult
    {
        public LicenseState State { get; init; }
        public string Message { get; init; } = string.Empty;
        public string MachineFingerprint { get; init; } = string.Empty;
        public DateTime? ExpiresAtUtc { get; init; }
        public bool IsActive => State == LicenseState.Active;
    }

    public sealed class LicenseActivationResult
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; } = string.Empty;
        public DateTime? ExpiresAtUtc { get; init; }
    }

    public static class LicenseService
    {
        private const int ClockRollbackToleranceMinutes = 5;
        private const string EmbeddedPublicKeyResourceName = "GroceryStoreManagement.Security.default_license_public_key.pem";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private sealed class StoredLicense
        {
            public string PayloadBase64Url { get; init; } = string.Empty;
            public string SignatureBase64Url { get; init; } = string.Empty;
            public DateTime ExpiresAtUtc { get; init; }
            public DateTime LastValidatedUtc { get; init; }
            public bool IsLocked { get; init; }
            public string LockReason { get; init; } = string.Empty;
        }

        private sealed class LicensePayload
        {
            public string machine { get; set; } = string.Empty;
            public string exp { get; set; } = string.Empty;
            public string iat { get; set; } = string.Empty;
            public string issuer { get; set; } = string.Empty;
            public string nonce { get; set; } = string.Empty;
        }

        public static void EnsureSchema(SQLiteConnection connection)
        {
            string createLicenseTable = @"
                CREATE TABLE IF NOT EXISTS LicenseState (
                    LicenseID INTEGER PRIMARY KEY CHECK (LicenseID = 1),
                    PayloadBase64Url TEXT NOT NULL,
                    SignatureBase64Url TEXT NOT NULL,
                    ExpiresAtUtc TEXT NOT NULL,
                    LastValidatedUtc TEXT NOT NULL,
                    IsLocked INTEGER DEFAULT 0,
                    LockReason TEXT,
                    UpdatedAtUtc TEXT NOT NULL
                );";

            using var command = new SQLiteCommand(createLicenseTable, connection);
            _ = command.ExecuteNonQuery();
        }

        public static LicenseCheckResult GetCurrentStatus()
        {
            string machineFingerprint = GetMachineFingerprint();

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                EnsureSchema(connection);

                StoredLicense storedLicense = GetStoredLicense(connection);
                if (storedLicense == null)
                {
                    return new LicenseCheckResult
                    {
                        State = LicenseState.NotActivated,
                        MachineFingerprint = machineFingerprint,
                        Message = "البرنامج غير مفعل. أدخل كود تفعيل صالح للمتابعة."
                    };
                }

                if (storedLicense.IsLocked)
                {
                    string reason = string.IsNullOrWhiteSpace(storedLicense.LockReason)
                        ? "الترخيص مقفل. يلزم إدخال كود تفعيل جديد."
                        : storedLicense.LockReason;

                    return new LicenseCheckResult
                    {
                        State = LicenseState.Locked,
                        MachineFingerprint = machineFingerprint,
                        ExpiresAtUtc = storedLicense.ExpiresAtUtc,
                        Message = reason
                    };
                }

                if (!TryValidateToken(
                    storedLicense.PayloadBase64Url,
                    storedLicense.SignatureBase64Url,
                    machineFingerprint,
                    out DateTime signedExpiryUtc,
                    out string validationError))
                {
                    SetLocked(connection, $"فشل التحقق من الترخيص: {validationError}");
                    return new LicenseCheckResult
                    {
                        State = LicenseState.Locked,
                        MachineFingerprint = machineFingerprint,
                        ExpiresAtUtc = storedLicense.ExpiresAtUtc,
                        Message = $"الترخيص غير صالح: {validationError}"
                    };
                }

                if (Math.Abs((signedExpiryUtc - storedLicense.ExpiresAtUtc).TotalSeconds) > 1)
                {
                    SetLocked(connection, "تم اكتشاف تغيير غير صحيح في بيانات الترخيص.");
                    return new LicenseCheckResult
                    {
                        State = LicenseState.Locked,
                        MachineFingerprint = machineFingerprint,
                        ExpiresAtUtc = storedLicense.ExpiresAtUtc,
                        Message = "تم قفل النظام بسبب العبث ببيانات الترخيص."
                    };
                }

                DateTime nowUtc = DateTime.UtcNow;
                if (nowUtc.AddMinutes(ClockRollbackToleranceMinutes) < storedLicense.LastValidatedUtc)
                {
                    SetLocked(connection, "تم اكتشاف تعديل غير طبيعي في ساعة الجهاز.");
                    return new LicenseCheckResult
                    {
                        State = LicenseState.Locked,
                        MachineFingerprint = machineFingerprint,
                        ExpiresAtUtc = storedLicense.ExpiresAtUtc,
                        Message = "تم قفل النظام بسبب تغيير ساعة الجهاز للخلف."
                    };
                }

                if (nowUtc > storedLicense.ExpiresAtUtc)
                {
                    return new LicenseCheckResult
                    {
                        State = LicenseState.Expired,
                        MachineFingerprint = machineFingerprint,
                        ExpiresAtUtc = storedLicense.ExpiresAtUtc,
                        Message = "انتهت مدة الترخيص. يلزم إدخال كود تفعيل جديد."
                    };
                }

                if (nowUtc > storedLicense.LastValidatedUtc)
                {
                    UpdateLastValidated(connection, nowUtc);
                }

                return new LicenseCheckResult
                {
                    State = LicenseState.Active,
                    MachineFingerprint = machineFingerprint,
                    ExpiresAtUtc = storedLicense.ExpiresAtUtc,
                    Message = $"الترخيص فعال حتى: {ToLocalDisplay(storedLicense.ExpiresAtUtc)}"
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في التحقق من حالة الترخيص");
                return new LicenseCheckResult
                {
                    State = LicenseState.Locked,
                    MachineFingerprint = machineFingerprint,
                    Message = "تعذر التحقق من الترخيص. أدخل كود تفعيل صالح."
                };
            }
        }

        public static LicenseActivationResult ActivateLicense(string token)
        {
            string machineFingerprint = GetMachineFingerprint();

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return new LicenseActivationResult
                    {
                        IsSuccess = false,
                        Message = "أدخل كود التفعيل أولاً."
                    };
                }

                if (!TrySplitToken(token, out string payloadBase64Url, out string signatureBase64Url, out string splitError))
                {
                    return new LicenseActivationResult
                    {
                        IsSuccess = false,
                        Message = splitError
                    };
                }

                if (!TryValidateToken(payloadBase64Url, signatureBase64Url, machineFingerprint, out DateTime expiresAtUtc, out string validationError))
                {
                    return new LicenseActivationResult
                    {
                        IsSuccess = false,
                        Message = validationError
                    };
                }

                DateTime nowUtc = DateTime.UtcNow;
                if (expiresAtUtc <= nowUtc)
                {
                    return new LicenseActivationResult
                    {
                        IsSuccess = false,
                        Message = "هذا الكود منتهي الصلاحية."
                    };
                }

                using var connection = DatabaseHelper.GetConnection();
                EnsureSchema(connection);
                SaveLicense(connection, payloadBase64Url, signatureBase64Url, expiresAtUtc, nowUtc);

                Logger.LogInfo($"تم تفعيل الترخيص بنجاح حتى {expiresAtUtc:O}");

                return new LicenseActivationResult
                {
                    IsSuccess = true,
                    ExpiresAtUtc = expiresAtUtc,
                    Message = $"تم التفعيل بنجاح حتى: {ToLocalDisplay(expiresAtUtc)}"
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في تفعيل الترخيص");
                return new LicenseActivationResult
                {
                    IsSuccess = false,
                    Message = "حدث خطأ أثناء التفعيل. تحقق من الكود وأعد المحاولة."
                };
            }
        }

        public static string GetMachineFingerprint()
        {
            string machineGuid = TryReadMachineGuid();
            string raw = $"{machineGuid}|{Environment.MachineName}";
            byte[] bytes = Encoding.UTF8.GetBytes(raw);
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static StoredLicense GetStoredLicense(SQLiteConnection connection)
        {
            const string query = @"
                SELECT PayloadBase64Url, SignatureBase64Url, ExpiresAtUtc, LastValidatedUtc, IsLocked, COALESCE(LockReason, '') AS LockReason
                FROM LicenseState
                WHERE LicenseID = 1";

            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            string payload = reader["PayloadBase64Url"]?.ToString() ?? string.Empty;
            string signature = reader["SignatureBase64Url"]?.ToString() ?? string.Empty;
            string expiresText = reader["ExpiresAtUtc"]?.ToString() ?? string.Empty;
            string lastValidatedText = reader["LastValidatedUtc"]?.ToString() ?? string.Empty;
            bool isLocked = Convert.ToInt32(reader["IsLocked"]) == 1;
            string lockReason = reader["LockReason"]?.ToString() ?? string.Empty;

            if (!TryParseUtc(expiresText, out DateTime expiresAtUtc) ||
                !TryParseUtc(lastValidatedText, out DateTime lastValidatedUtc))
            {
                return new StoredLicense
                {
                    PayloadBase64Url = payload,
                    SignatureBase64Url = signature,
                    ExpiresAtUtc = DateTime.MinValue,
                    LastValidatedUtc = DateTime.MinValue,
                    IsLocked = true,
                    LockReason = "بيانات الترخيص المخزنة غير قابلة للقراءة."
                };
            }

            return new StoredLicense
            {
                PayloadBase64Url = payload,
                SignatureBase64Url = signature,
                ExpiresAtUtc = expiresAtUtc,
                LastValidatedUtc = lastValidatedUtc,
                IsLocked = isLocked,
                LockReason = lockReason
            };
        }

        private static void SaveLicense(SQLiteConnection connection, string payloadBase64Url, string signatureBase64Url, DateTime expiresAtUtc, DateTime validatedAtUtc)
        {
            using var transaction = connection.BeginTransaction();

            using (var deleteCommand = new SQLiteCommand("DELETE FROM LicenseState WHERE LicenseID = 1", connection, transaction))
            {
                _ = deleteCommand.ExecuteNonQuery();
            }

            const string insert = @"
                INSERT INTO LicenseState
                (
                    LicenseID,
                    PayloadBase64Url,
                    SignatureBase64Url,
                    ExpiresAtUtc,
                    LastValidatedUtc,
                    IsLocked,
                    LockReason,
                    UpdatedAtUtc
                )
                VALUES
                (
                    1,
                    @PayloadBase64Url,
                    @SignatureBase64Url,
                    @ExpiresAtUtc,
                    @LastValidatedUtc,
                    0,
                    NULL,
                    @UpdatedAtUtc
                );";

            using var insertCommand = new SQLiteCommand(insert, connection, transaction);
            _ = insertCommand.Parameters.AddWithValue("@PayloadBase64Url", payloadBase64Url);
            _ = insertCommand.Parameters.AddWithValue("@SignatureBase64Url", signatureBase64Url);
            _ = insertCommand.Parameters.AddWithValue("@ExpiresAtUtc", expiresAtUtc.ToString("O", CultureInfo.InvariantCulture));
            _ = insertCommand.Parameters.AddWithValue("@LastValidatedUtc", validatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            _ = insertCommand.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            _ = insertCommand.ExecuteNonQuery();

            transaction.Commit();
        }

        private static void SetLocked(SQLiteConnection connection, string reason)
        {
            const string update = @"
                UPDATE LicenseState
                SET
                    IsLocked = 1,
                    LockReason = @LockReason,
                    UpdatedAtUtc = @UpdatedAtUtc
                WHERE LicenseID = 1;";

            using var command = new SQLiteCommand(update, connection);
            _ = command.Parameters.AddWithValue("@LockReason", reason ?? string.Empty);
            _ = command.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            _ = command.ExecuteNonQuery();
        }

        private static void UpdateLastValidated(SQLiteConnection connection, DateTime validatedAtUtc)
        {
            const string update = @"
                UPDATE LicenseState
                SET
                    LastValidatedUtc = @LastValidatedUtc,
                    UpdatedAtUtc = @UpdatedAtUtc
                WHERE LicenseID = 1;";

            using var command = new SQLiteCommand(update, connection);
            _ = command.Parameters.AddWithValue("@LastValidatedUtc", validatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            _ = command.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            _ = command.ExecuteNonQuery();
        }

        private static bool TryValidateToken(
            string payloadBase64Url,
            string signatureBase64Url,
            string expectedMachineFingerprint,
            out DateTime expiresAtUtc,
            out string error)
        {
            expiresAtUtc = DateTime.MinValue;
            error = string.Empty;

            if (!TryReadPublicKey(out string publicKeyPem, out error))
            {
                return false;
            }

            if (!TryVerifySignature(publicKeyPem, payloadBase64Url, signatureBase64Url, out error))
            {
                return false;
            }

            if (!TryReadPayload(payloadBase64Url, out LicensePayload payload, out error))
            {
                return false;
            }

            string tokenMachine = payload.machine?.Trim() ?? string.Empty;
            if (!string.Equals(tokenMachine, expectedMachineFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                error = "هذا الكود ليس لهذا الجهاز.";
                return false;
            }

            if (!TryParseUtc(payload.exp, out expiresAtUtc))
            {
                error = "تاريخ انتهاء الترخيص داخل الكود غير صالح.";
                return false;
            }

            return true;
        }

        private static bool TrySplitToken(string token, out string payloadBase64Url, out string signatureBase64Url, out string error)
        {
            payloadBase64Url = string.Empty;
            signatureBase64Url = string.Empty;
            error = string.Empty;

            string normalized = token.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty);
            string[] parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                Match match = Regex.Match(
                    normalized,
                    @"(?<payload>[A-Za-z0-9_-]+)\.(?<signature>[A-Za-z0-9_-]+)",
                    RegexOptions.CultureInvariant);

                if (!match.Success)
                {
                    error = "صيغة كود التفعيل غير صحيحة.";
                    return false;
                }

                payloadBase64Url = match.Groups["payload"].Value;
                signatureBase64Url = match.Groups["signature"].Value;
                return true;
            }

            payloadBase64Url = parts[0];
            signatureBase64Url = parts[1];
            return true;
        }

        private static bool TryReadPayload(string payloadBase64Url, out LicensePayload payload, out string error)
        {
            payload = null;
            error = string.Empty;

            try
            {
                byte[] payloadBytes = Base64UrlDecode(payloadBase64Url);
                string json = Encoding.UTF8.GetString(payloadBytes);
                payload = JsonSerializer.Deserialize<LicensePayload>(json, JsonOptions);

                if (payload == null || string.IsNullOrWhiteSpace(payload.machine) || string.IsNullOrWhiteSpace(payload.exp))
                {
                    error = "بيانات الكود ناقصة.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"فشل قراءة كود الترخيص: {ex.Message}");
                error = "تعذر قراءة بيانات كود التفعيل.";
                return false;
            }
        }

        private static bool TryVerifySignature(string publicKeyPem, string payloadBase64Url, string signatureBase64Url, out string error)
        {
            error = string.Empty;

            try
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadBase64Url);
                byte[] signatureBytes = Base64UrlDecode(signatureBase64Url);

                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);

                bool isValid = rsa.VerifyData(
                    payloadBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                if (!isValid)
                {
                    error = "التوقيع الرقمي للكود غير صحيح.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل التحقق من توقيع الترخيص");
                error = "حدث خطأ أثناء التحقق من الكود.";
                return false;
            }
        }

        private static bool TryReadPublicKey(out string publicKeyPem, out string error)
        {
            publicKeyPem = string.Empty;
            error = string.Empty;

            if (TryReadEmbeddedPublicKey(out publicKeyPem))
            {
                return true;
            }

            string keyPath = GetPublicKeyPath();
            if (File.Exists(keyPath))
            {
                try
                {
                    publicKeyPem = File.ReadAllText(keyPath, Encoding.ASCII);
                    if (string.IsNullOrWhiteSpace(publicKeyPem))
                    {
                        error = "ملف المفتاح العام فارغ.";
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "فشل في قراءة المفتاح العام من الملف الخارجي");
                    error = "تعذر قراءة ملف المفتاح العام.";
                    return false;
                }
            }

            error = $"لم يتم العثور على المفتاح العام لا في الملف الخارجي ولا داخل التطبيق: {keyPath}";
            return false;
        }

        private static string GetPublicKeyPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "license_public_key.pem");
        }

        private static bool TryReadEmbeddedPublicKey(out string publicKeyPem)
        {
            publicKeyPem = string.Empty;

            try
            {
                using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedPublicKeyResourceName);
                if (resourceStream == null)
                {
                    return false;
                }

                using var reader = new StreamReader(resourceStream, Encoding.ASCII);
                publicKeyPem = reader.ReadToEnd();
                return !string.IsNullOrWhiteSpace(publicKeyPem);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "فشل في قراءة المفتاح العام المضمن داخل التطبيق");
                return false;
            }
        }

        private static string TryReadMachineGuid()
        {
            try
            {
                using RegistryKey localMachine64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using RegistryKey cryptoKey64 = localMachine64.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                string value64 = cryptoKey64?.GetValue("MachineGuid")?.ToString();
                if (!string.IsNullOrWhiteSpace(value64))
                {
                    return value64.Trim();
                }
            }
            catch
            {
                // Ignore and fallback.
            }

            try
            {
                using RegistryKey localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                using RegistryKey cryptoKey32 = localMachine32.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                string value32 = cryptoKey32?.GetValue("MachineGuid")?.ToString();
                if (!string.IsNullOrWhiteSpace(value32))
                {
                    return value32.Trim();
                }
            }
            catch
            {
                // Ignore and fallback.
            }

            return Environment.MachineName;
        }

        private static bool TryParseUtc(string value, out DateTime utcDateTime)
        {
            if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out DateTime parsed))
            {
                utcDateTime = parsed.ToUniversalTime();
                return true;
            }

            utcDateTime = DateTime.MinValue;
            return false;
        }

        private static string ToLocalDisplay(DateTime utcDateTime)
        {
            return utcDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string padded = input
                .Replace('-', '+')
                .Replace('_', '/');

            switch (padded.Length % 4)
            {
                case 0:
                    break;
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
                default:
                    throw new FormatException("Invalid Base64Url value.");
            }

            return Convert.FromBase64String(padded);
        }
    }
}
