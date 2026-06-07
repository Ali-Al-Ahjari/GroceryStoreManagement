using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LicenseIssuerCSharp
{
    public sealed class LicensePayload
    {
        public string machine { get; set; }
        public string exp { get; set; }
        public string iat { get; set; }
        public string issuer { get; set; }
        public string nonce { get; set; }
    }

    public static class LicenseEngine
    {
        // Embedded default RSA private key parameters matching the store app's default public key
        private static readonly RSAParameters DefaultPrivateParams = new RSAParameters
        {
            Modulus = Convert.FromBase64String("l8SHxCHvrUM0zvQouNwxMP2nity3N3//D0E2dNbDbdiLdVW9h2b0KBHU3kNSTyM8bQ7/8EFeDpXMoDI0pq3o66YNNjVPWZphfxrvtn3p3rWCRYE5A6jeLe35+FgUfAbZktswoG4YIYtNHKIqIuh3mAgX8j6/3eqlWFULCPZUK/58eCz+UNurm+eKgqd7KuDJazv/HwVwMCofliFNgpBXOKqDoE1ECQXVkT1yqmJ0vZbNNQa0YyWWlI/z9Ih4vTGUjZkFZNyD9ygQPGpOWmTrIWcFAJqyO2DeFuMnL0LCjh7AvcANq6y4sFA9M6a4EAgT19kiqFwRofhUp5bfU3YSzZ36u1sFqGX9mwNFmt12V9YZPlTV2n1ocoGxhTbAvczBREGByLhAProAhIufdDH4nmYJgFYaeqVkM6jtwLAaaWzOlVqjpNgNoOIOcJdMLbNkbig9Seiaxx7mpBWSxK9YRKGnnsWZsTAxCCSgK/v5AyWc3R2soRaNjsqQBecn52tR"),
            Exponent = Convert.FromBase64String("AQAB"),
            D = Convert.FromBase64String("A9+8gvNGT88GvK8jEHfvagOZiJwCccBayYAFxLT8M1Q7GBVGk5ubSSAOZdeEVPiOaO7AdfinLtpgSSkK6sPG7af1D6CL/FqqWTEd9BOx+fE6aG2IX+lqNXQtBKuz5ygSGvNtIfU5eLa6cswJZQ93yQnQ2apEIcUk8BSopdOK2b4i3XrxiW0YC/BcuuHNTS6EebAj/p3YC6SxnnaOaNIoJc5FcpyyS0Qqu8y8nfCKMBI2vf8kMOEzXyqvBJuMsHCg4u04rZqpIlQikHha5tek3kJr2mgubcOdxHVhqDAmDQyxOyAo4zamiOjaZCeoBGZ1cNXML1SMcSxFE7BL1fpgy6+x4Rh2NvjPLAN6pWB3/6Q2Smu+HUrAX+UlnS2BzVvEoUdLuKzsd2BYJfVW4YbxxiU5g6O3dV5DCIbSWy1L64O58UbzEam8XxQprXNBO53lp0BSPBR8Xzx/iM8wTqkacVBh14DmU0RngFPyGI3ZUAVUDKsuv2QOLrAlHsknOIxZ"),
            P = Convert.FromBase64String("yVnqCsNxFVkZoFNeQt1wrZB3qcDqruzzCZXSfoIf9uigKbTDYVD4mt01QpWia1cEk8s6izC9n7JbKMOfgeLeh/XtvEBJf5LNZyUYJWFgzjV0ntsfXPzRZ0FIJVIfBPn9tvrm4kmF2Dcwv2ChkUywDQmvn7PP6TwZwZSv1I46xnP7ZcaTOTUjTEtHjRoNMBaQ1T7QA7FcUHAbffBd7ohOOnCUf2GzcVd3amSc7lGc/mCZiKJYHA1Z7KcuPgFuX5FZ"),
            Q = Convert.FromBase64String("wPWApW63BweySO4eGsYrVOafRSHX7mKVK+xtEaZzFTHC4BQWYBU0KrzXUK8eeN8RyLurTZ/4ycq/SwF16TlB1/lC6gyDW3R/hLO02ugpVo9NvS1iPfBaWFs1qHy/H9IiZ3g5naAdsKnx+8NlWuDz2huJn93wWAeTvbeZpRphIP0nlNMAi6ZG2KYKajktVdpNophI7PcwbWf05fQQ4ODXDiPQUspcK/SwPu/dhN8AxnvkZjeDLrzxfLXmM1vrlDK5"),
            DP = Convert.FromBase64String("Q251Tv19FaUtS+AfpUz7u2SybotJDSQVkJQ4Vl3Fzq7BVLZQ6Hpxh4ullpL+Pex6f/SDurGsD5tvpAs/lAQiem4GHBF7i9niKDToDTy7atPEJp9DtaSFjIr0WmtSMBx4t5r3T7lHtc1l6fG2qi6Alx0zT8ysSHP0PDxLVmf7jxKwkrkZ/QKqu5ZKdBrRZ9Vm08Ohblsri1TAIEErdAL6D+A8GxzhN4gPWfYwCDAU36wCPxv/XgWqo/KpLeLUz/Sh"),
            DQ = Convert.FromBase64String("HAAECP2F5alCP77981iqVQmaNwfwM5FuoA8QzKzgqkSsSKPhk1PAXCtG+1hODbAg/oXtF7iM+4tGMvYlTBCY82QiR6BFN3IyRulk5xclWIA3AaqiROap1YR2xtpDSbTOdUFG6w6fAFHI4YW7IXLfL9krIV2tULjYwYDw9LpdOdJCeiAcRid8xFdjz4I4h+rDtUJv4qMEDfw112CRSnCWBGFCC6F9uospQBjChlNt4197BzIrAQ8946DppK5EAx9R"),
            InverseQ = Convert.FromBase64String("o055F4SE6uZZ4XTq/2JbqTlQTX0g9e/Xf4cEmGzZkPDT+wayyOnb6a0JYCF0yc2lauyj/8F1CCU1C9M2FB1pyGcqWEBOsx2x4gKZSH3XL5t8puD9dRW5CYDFqJx7gsNXDmgnevce767SE7xqE1g80ryohS+tTGy9lCwtAUDAfxh610y3k4aXRehNcvcLYXhmQePUvVQDwRFxv/LXNFZTgfcWwYQ06i7oLzOFg3G868pZiqzBAYKuJIlgyi1pdQsN")
        };

        public static string Base64UrlEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            return base64.Split('=')[0].Replace('+', '-').Replace('/', '_');
        }

        public static byte[] Base64UrlDecode(string input)
        {
            string padded = input.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }

        public static string NormalizeFingerprint(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string text = value.Trim();

            // Stripping standard prefixes (Arabic and English)
            const string arPrefix = "بصمة الجهاز:";
            const string enPrefix = "MACHINE FINGERPRINT:";
            const string fpPrefix = "FINGERPRINT:";

            if (text.StartsWith(arPrefix, StringComparison.OrdinalIgnoreCase))
                text = text.Substring(arPrefix.Length).Trim();
            else if (text.StartsWith(enPrefix, StringComparison.OrdinalIgnoreCase))
                text = text.Substring(enPrefix.Length).Trim();
            else if (text.StartsWith(fpPrefix, StringComparison.OrdinalIgnoreCase))
                text = text.Substring(fpPrefix.Length).Trim();

            text = text.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            return text.ToUpper();
        }

        public static string GenerateToken(string machineFingerprint, int expiryDays, string customPrivateKeyPem = null, string issuer = "StoreOwner")
        {
            string machine = NormalizeFingerprint(machineFingerprint);
            if (string.IsNullOrEmpty(machine))
                throw new ArgumentException("بصمة الجهاز مطلوبة ولا يمكن أن تكون فارغة.");

            DateTime now = DateTime.UtcNow;
            DateTime expiry = now.AddDays(expiryDays);

            string iatStr = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string expStr = expiry.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string nonce = Guid.NewGuid().ToString("N");

            // Format JSON payload manually to guarantee structure and ordering
            string payloadJson = string.Format(
                "{{\"machine\":\"{0}\",\"exp\":\"{1}\",\"iat\":\"{2}\",\"issuer\":\"{3}\",\"nonce\":\"{4}\"}}",
                machine, expStr, iatStr, issuer ?? "StoreOwner", nonce
            );

            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            string payloadB64Url = Base64UrlEncode(payloadBytes);

            // Determine key to use
            RSAParameters rsaParams;
            if (string.IsNullOrEmpty(customPrivateKeyPem))
            {
                rsaParams = DefaultPrivateParams;
            }
            else
            {
                rsaParams = ParsePrivateKeyPem(customPrivateKeyPem);
            }

            // Create RSA provider with AES provider to support SHA256 signing
            byte[] signature;
            var cspParams = new CspParameters(24); // PROV_RSA_AES
            using (var rsa = new RSACryptoServiceProvider(cspParams))
            {
                rsa.ImportParameters(rsaParams);
                byte[] dataToSign = Encoding.UTF8.GetBytes(payloadB64Url);
                signature = rsa.SignData(dataToSign, CryptoConfig.MapNameToOID("SHA256"));
            }

            string signatureB64Url = Base64UrlEncode(signature);
            return string.Format("{0}.{1}", payloadB64Url, signatureB64Url);
        }

        public static void GenerateNewKeypair(string outDir, out string privateKeyPem, out string publicKeyPem)
        {
            var cspParams = new CspParameters(24);
            using (var rsa = new RSACryptoServiceProvider(3072, cspParams))
            {
                // Export Private key in PKCS#8 format (WPF/Mono or RSACryptoServiceProvider doesn't have native ExportPkcs8PrivateKey in .NET 4.5)
                // We will export the Private Key in XML or generate PKCS#1 PEM parameters.
                // Let's write a standard helper to export RSA Private/Public Key in PEM format.
                RSAParameters privateParams = rsa.ExportParameters(true);
                RSAParameters publicParams = rsa.ExportParameters(false);

                privateKeyPem = ExportPrivateKeyToPem(privateParams);
                publicKeyPem = ExportPublicKeyToPem(publicParams);

                if (!string.IsNullOrEmpty(outDir))
                {
                    Directory.CreateDirectory(outDir);
                    File.WriteAllText(Path.Combine(outDir, "license_private_key.pem"), privateKeyPem, Encoding.ASCII);
                    File.WriteAllText(Path.Combine(outDir, "license_public_key.pem"), publicKeyPem, Encoding.ASCII);
                }
            }
        }

        #region PEM Parsing and Exporting Helpers

        private static RSAParameters ParsePrivateKeyPem(string pem)
        {
            string base64 = pem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Trim();

            byte[] der = Convert.FromBase64String(base64);
            return DecodeRsaPrivateKey(der);
        }

        // Standard ASN.1 DER Decoder for RSA Private Key (supporting PKCS#1 and PKCS#8 formats)
        private static RSAParameters DecodeRsaPrivateKey(byte[] der)
        {
            using (var ms = new MemoryStream(der))
            using (var reader = new BinaryReader(ms))
            {
                byte tag = reader.ReadByte();
                if (tag != 0x30) throw new InvalidDataException("Invalid DER structure.");

                int length = ReadDerLength(reader);

                // PKCS#8 wrapping contains AlgorithmIdentifier before the PrivateKey OctetString.
                // Let's check if the next tag is Integer (Version) and has a value of 0, which is standard for both PKCS#1 and PKCS#8.
                byte nextTag = reader.ReadByte();
                if (nextTag != 0x02) throw new InvalidDataException("Unsupported private key structure.");

                int versionLen = ReadDerLength(reader);
                byte[] version = reader.ReadBytes(versionLen);

                // In PKCS#8, the version is followed by a Sequence of AlgorithmIdentifier (0x30).
                // In PKCS#1, the version is followed by Modulus (Integer, 0x02).
                byte structTag = reader.ReadByte();
                if (structTag == 0x30)
                {
                    // PKCS#8 Format detected. Skip the AlgorithmIdentifier sequence.
                    int algoLen = ReadDerLength(reader);
                    reader.ReadBytes(algoLen);

                    // Next should be an Octet String (0x04) containing the RSAPrivateKey structure.
                    byte octetTag = reader.ReadByte();
                    if (octetTag != 0x04) throw new InvalidDataException("Expected OCTET STRING in PKCS#8.");

                    int octetLen = ReadDerLength(reader);
                    byte[] innerDer = reader.ReadBytes(octetLen);

                    // Recursively decode the PKCS#1 inner structure
                    return DecodeRsaPrivateKey(innerDer);
                }
                else if (structTag == 0x02)
                {
                    // PKCS#1 Format. The current structTag is the Modulus Integer.
                    // Read RSAPrivateKey fields: Modulus, PublicExponent, PrivateExponent, Prime1, Prime2, Exponent1, Exponent2, Coefficient
                    int modLen = ReadDerLength(reader);
                    byte[] modulus = reader.ReadBytes(modLen);

                    reader.ReadByte(); // Exponent tag (0x02)
                    int expLen = ReadDerLength(reader);
                    byte[] exponent = reader.ReadBytes(expLen);

                    reader.ReadByte(); // D tag (0x02)
                    int dLen = ReadDerLength(reader);
                    byte[] d = reader.ReadBytes(dLen);

                    reader.ReadByte(); // P tag (0x02)
                    int pLen = ReadDerLength(reader);
                    byte[] p = reader.ReadBytes(pLen);

                    reader.ReadByte(); // Q tag (0x02)
                    int qLen = ReadDerLength(reader);
                    byte[] q = reader.ReadBytes(qLen);

                    reader.ReadByte(); // DP tag (0x02)
                    int dpLen = ReadDerLength(reader);
                    byte[] dp = reader.ReadBytes(dpLen);

                    reader.ReadByte(); // DQ tag (0x02)
                    int dqLen = ReadDerLength(reader);
                    byte[] dq = reader.ReadBytes(dqLen);

                    reader.ReadByte(); // InverseQ tag (0x02)
                    int iqLen = ReadDerLength(reader);
                    byte[] iq = reader.ReadBytes(iqLen);

                    // Align byte arrays by stripping leading zero bytes added by DER encoding for positive integers
                    return new RSAParameters
                    {
                        Modulus = AlignDERInteger(modulus),
                        Exponent = AlignDERInteger(exponent),
                        D = AlignDERInteger(d),
                        P = AlignDERInteger(p),
                        Q = AlignDERInteger(q),
                        DP = AlignDERInteger(dp),
                        DQ = AlignDERInteger(dq),
                        InverseQ = AlignDERInteger(iq)
                    };
                }
                else
                {
                    throw new InvalidDataException("Unsupported key format.");
                }
            }
        }

        private static int ReadDerLength(BinaryReader reader)
        {
            byte lenByte = reader.ReadByte();
            if ((lenByte & 0x80) == 0)
            {
                return lenByte;
            }
            int numBytes = lenByte & 0x7F;
            int length = 0;
            for (int i = 0; i < numBytes; i++)
            {
                length = (length << 8) | reader.ReadByte();
            }
            return length;
        }

        private static byte[] AlignDERInteger(byte[] data)
        {
            // DER integers may have a leading zero byte if the highest bit is set to prevent it being treated as negative.
            // In RSAParameters, we must strip this leading zero byte if it exists.
            if (data.Length > 1 && data[0] == 0x00)
            {
                byte[] aligned = new byte[data.Length - 1];
                Buffer.BlockCopy(data, 1, aligned, 0, aligned.Length);
                return aligned;
            }
            return data;
        }

        private static string ExportPrivateKeyToPem(RSAParameters rsaParams)
        {
            // Export in PKCS#1 RSA Private Key format
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // Struct: Sequence of Modulus, Exponent, D, P, Q, DP, DQ, InverseQ
                    // Let's write the ASN.1 DER manually.
                    var innerStream = new MemoryStream();
                    using (var innerWriter = new BinaryWriter(innerStream))
                    {
                        WriteDerInteger(innerWriter, new byte[] { 0x00 }); // Version 0
                        WriteDerInteger(innerWriter, rsaParams.Modulus);
                        WriteDerInteger(innerWriter, rsaParams.Exponent);
                        WriteDerInteger(innerWriter, rsaParams.D);
                        WriteDerInteger(innerWriter, rsaParams.P);
                        WriteDerInteger(innerWriter, rsaParams.Q);
                        WriteDerInteger(innerWriter, rsaParams.DP);
                        WriteDerInteger(innerWriter, rsaParams.DQ);
                        WriteDerInteger(innerWriter, rsaParams.InverseQ);
                    }

                    byte[] innerBytes = innerStream.ToArray();
                    writer.Write((byte)0x30); // SEQUENCE
                    WriteDerLength(writer, innerBytes.Length);
                    writer.Write(innerBytes);
                }

                string base64 = Convert.ToBase64String(stream.ToArray(), Base64FormattingOptions.InsertLineBreaks);
                var sb = new StringBuilder();
                sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
                sb.AppendLine(base64);
                sb.AppendLine("-----END RSA PRIVATE KEY-----");
                return sb.ToString();
            }
        }

        private static string ExportPublicKeyToPem(RSAParameters rsaParams)
        {
            // Export in SubjectPublicKeyInfo (X.509) format
            // Algorithm OID: 1.2.840.113549.1.1.1 (rsaEncryption)
            byte[] oid = new byte[] { 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 };
            byte[] nullParams = new byte[] { 0x05, 0x00 };

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // RSA Public Key structure inside Bit String: Sequence of Modulus, Exponent
                    var keyStream = new MemoryStream();
                    using (var keyWriter = new BinaryWriter(keyStream))
                    {
                        WriteDerInteger(keyWriter, rsaParams.Modulus);
                        WriteDerInteger(keyWriter, rsaParams.Exponent);
                    }
                    byte[] keyBytes = keyStream.ToArray();

                    var innerKeyStream = new MemoryStream();
                    using (var innerKeyWriter = new BinaryWriter(innerKeyStream))
                    {
                        innerKeyWriter.Write((byte)0x30);
                        WriteDerLength(innerKeyWriter, keyBytes.Length);
                        innerKeyWriter.Write(keyBytes);
                    }
                    byte[] rsaPublicKeyDer = innerKeyStream.ToArray();

                    // Sequence: AlgorithmIdentifier, SubjectPublicKey
                    var bodyStream = new MemoryStream();
                    using (var bodyWriter = new BinaryWriter(bodyStream))
                    {
                        // AlgorithmIdentifier Sequence
                        bodyWriter.Write((byte)0x30);
                        WriteDerLength(bodyWriter, oid.Length + nullParams.Length);
                        bodyWriter.Write(oid);
                        bodyWriter.Write(nullParams);

                        // SubjectPublicKey Bit String (starts with 0x00 padding byte)
                        bodyWriter.Write((byte)0x03); // BIT STRING
                        WriteDerLength(bodyWriter, rsaPublicKeyDer.Length + 1);
                        bodyWriter.Write((byte)0x00); // no unused bits
                        bodyWriter.Write(rsaPublicKeyDer);
                    }

                    byte[] bodyBytes = bodyStream.ToArray();
                    writer.Write((byte)0x30);
                    WriteDerLength(writer, bodyBytes.Length);
                    writer.Write(bodyBytes);
                }

                string base64 = Convert.ToBase64String(stream.ToArray(), Base64FormattingOptions.InsertLineBreaks);
                var sb = new StringBuilder();
                sb.AppendLine("-----BEGIN PUBLIC KEY-----");
                sb.AppendLine(base64);
                sb.AppendLine("-----END PUBLIC KEY-----");
                return sb.ToString();
            }
        }

        private static void WriteDerInteger(BinaryWriter writer, byte[] data)
        {
            writer.Write((byte)0x02); // INTEGER

            // If the highest bit of the byte array is set, we must prepend a zero byte to indicate positive value
            bool prependZero = data.Length > 0 && (data[0] & 0x80) == 0x80;
            int length = data.Length + (prependZero ? 1 : 0);

            WriteDerLength(writer, length);
            if (prependZero)
            {
                writer.Write((byte)0x00);
            }
            writer.Write(data);
        }

        private static void WriteDerLength(BinaryWriter writer, int length)
        {
            if (length < 128)
            {
                writer.Write((byte)length);
            }
            else
            {
                int temp = length;
                int numBytes = 0;
                while (temp > 0)
                {
                    numBytes++;
                    temp >>= 8;
                }

                writer.Write((byte)(0x80 | numBytes));
                for (int i = numBytes - 1; i >= 0; i--)
                {
                    writer.Write((byte)((length >> (i * 8)) & 0xFF));
                }
            }
        }

        #endregion
    }
}
