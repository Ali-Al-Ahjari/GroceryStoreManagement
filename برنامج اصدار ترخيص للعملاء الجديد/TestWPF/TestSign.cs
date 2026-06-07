using System;
using System.Security.Cryptography;
using System.Text;

namespace TestSign
{
    class Program
    {
        static void Main()
        {
            try
            {
                var rsaParams = new RSAParameters
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

                byte[] data = Encoding.UTF8.GetBytes("test_payload_string");
                byte[] signature;

                // Create CSP with provider 24 (PROV_RSA_AES) to support SHA256
                var cspParams = new CspParameters(24);
                using (var rsa = new RSACryptoServiceProvider(cspParams))
                {
                    rsa.ImportParameters(rsaParams);
                    signature = rsa.SignData(data, CryptoConfig.MapNameToOID("SHA256"));
                }

                Console.WriteLine("Signing succeeded!");
                Console.WriteLine("Signature length: " + signature.Length);
                
                // Verify signature using public parameters to double check
                using (var rsaPublic = new RSACryptoServiceProvider(cspParams))
                {
                    var publicParams = new RSAParameters
                    {
                        Modulus = rsaParams.Modulus,
                        Exponent = rsaParams.Exponent
                    };
                    rsaPublic.ImportParameters(publicParams);
                    bool isValid = rsaPublic.VerifyData(data, CryptoConfig.MapNameToOID("SHA256"), signature);
                    Console.WriteLine("Verification result: " + isValid);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }
    }
}
