using System;

namespace LicenseIssuerCSharp
{
    public class LicenseRecord
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string MachineFingerprint { get; set; }
        public string Issuer { get; set; }
        public string IssueDate { get; set; }
        public string ExpiryDate { get; set; }
        public string Token { get; set; }
    }
}
