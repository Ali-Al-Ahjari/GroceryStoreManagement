# Copy this file to: scripts/license-tool.local.ps1
# Then update the values below to your external tool/key locations.

$env:LICENSE_ISSUER_PATH = "C:\Path\To\LicenseIssuer"
$env:LICENSE_ISSUER_PROJECT = "C:\Path\To\LicenseIssuer\LicenseIssuer.csproj"
$env:LICENSE_PRIVATE_KEY_PATH = "C:\Path\To\license_private_key.pem"
$env:LICENSE_PUBLIC_KEY_PATH = "C:\Path\To\license_public_key.pem"
$env:LICENSE_DEFAULT_ISSUER = "StoreOwner"
