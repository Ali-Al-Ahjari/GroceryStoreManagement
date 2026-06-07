[CmdletBinding()]
param(
    [string]$MachineFingerprint,
    [int]$Days = 30,
    [string]$ExpiresAt,
    [string]$Issuer = $env:LICENSE_DEFAULT_ISSUER,
    [string]$PrivateKeyPath = $env:LICENSE_PRIVATE_KEY_PATH,
    [string]$PublicKeyPath = $env:LICENSE_PUBLIC_KEY_PATH,
    [switch]$NoClipboard,
    [switch]$TokenOnly,
    [switch]$SkipVerification
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$machineFingerprintWasBound = $PSBoundParameters.ContainsKey("MachineFingerprint")
$privateKeyPathWasBound = $PSBoundParameters.ContainsKey("PrivateKeyPath")
$publicKeyPathWasBound = $PSBoundParameters.ContainsKey("PublicKeyPath")
$issuerWasBound = $PSBoundParameters.ContainsKey("Issuer")

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$localConfigPath = Join-Path $scriptDir "license-tool.local.ps1"

if (Test-Path $localConfigPath -PathType Leaf) {
    . $localConfigPath
}

if (-not $privateKeyPathWasBound -and [string]::IsNullOrWhiteSpace($PrivateKeyPath) -and -not [string]::IsNullOrWhiteSpace($env:LICENSE_PRIVATE_KEY_PATH)) {
    $PrivateKeyPath = $env:LICENSE_PRIVATE_KEY_PATH
}

if (-not $publicKeyPathWasBound -and [string]::IsNullOrWhiteSpace($PublicKeyPath) -and -not [string]::IsNullOrWhiteSpace($env:LICENSE_PUBLIC_KEY_PATH)) {
    $PublicKeyPath = $env:LICENSE_PUBLIC_KEY_PATH
}

if (-not $issuerWasBound -and [string]::IsNullOrWhiteSpace($Issuer) -and -not [string]::IsNullOrWhiteSpace($env:LICENSE_DEFAULT_ISSUER)) {
    $Issuer = $env:LICENSE_DEFAULT_ISSUER
}

if ([string]::IsNullOrWhiteSpace($Issuer)) {
    $Issuer = "StoreOwner"
}

function Extract-MachineFingerprint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RawText
    )

    $text = $RawText.Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return ""
    }

    $patterns = @(
        '(?im)^\s*بصمة\s*الجهاز\s*[:：]\s*(?<fp>.+?)\s*$',
        '(?im)^\s*machine\s*fingerprint\s*[:：]\s*(?<fp>.+?)\s*$',
        '(?im)^\s*fingerprint\s*[:：]\s*(?<fp>.+?)\s*$'
    )

    foreach ($pattern in $patterns) {
        $match = [regex]::Match($text, $pattern)
        if ($match.Success) {
            return $match.Groups["fp"].Value.Trim().Trim("'").Trim('"')
        }
    }

    $hexMatches = [regex]::Matches($text, '(?i)\b[A-F0-9]{32,}\b')
    if ($hexMatches.Count -gt 0) {
        $best = $hexMatches | Sort-Object Length -Descending | Select-Object -First 1
        return $best.Value.Trim()
    }

    if (-not $text.Contains("`n") -and -not $text.Contains("`r") -and $text.Length -le 300) {
        return $text
    }

    return ""
}

function Decode-Base64UrlToText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Base64Url
    )

    $base64 = $Base64Url.Replace('-', '+').Replace('_', '/')
    switch ($base64.Length % 4) {
        0 { }
        2 { $base64 += "==" }
        3 { $base64 += "=" }
        default { throw "Invalid base64url payload." }
    }

    $bytes = [System.Convert]::FromBase64String($base64)
    return [System.Text.Encoding]::UTF8.GetString($bytes)
}

function Try-GetTokenExpiryText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Token
    )

    try {
        $parts = $Token.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)
        if ($parts.Length -ne 2) {
            return ""
        }

        $json = Decode-Base64UrlToText -Base64Url $parts[0]
        $payload = $json | ConvertFrom-Json
        $exp = [string]$payload.exp
        if ([string]::IsNullOrWhiteSpace($exp)) {
            return ""
        }

        $utc = [DateTime]::Parse($exp, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::AdjustToUniversal)
        return $utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", [System.Globalization.CultureInfo]::InvariantCulture)
    }
    catch {
        return ""
    }
}

if (-not $machineFingerprintWasBound -and [string]::IsNullOrWhiteSpace($MachineFingerprint)) {
    try {
        $clipboardText = Get-Clipboard -Raw
        if (-not [string]::IsNullOrWhiteSpace($clipboardText)) {
            $MachineFingerprint = $clipboardText.Trim()
        }
    }
    catch {
        # Clipboard may not be available in every environment.
    }
}

$rawMachineFingerprintText = if ($null -eq $MachineFingerprint) { "" } else { [string]$MachineFingerprint }
$MachineFingerprint = Extract-MachineFingerprint -RawText $rawMachineFingerprintText
if ([string]::IsNullOrWhiteSpace($MachineFingerprint)) {
    throw "Machine fingerprint was not detected. Pass -MachineFingerprint directly, or copy the activation request message to clipboard first."
}

if ([string]::IsNullOrWhiteSpace($PrivateKeyPath)) {
    throw "Private key path is required. Set it in scripts\\license-tool.local.ps1 or pass -PrivateKeyPath."
}

$invokeLicenseIssuerScript = Join-Path $scriptDir "invoke-license-issuer.ps1"
$invokeParams = @{
    Action = "gen-token"
    PrivateKeyPath = $PrivateKeyPath
    MachineFingerprint = $MachineFingerprint
    Issuer = $Issuer
}

if ([string]::IsNullOrWhiteSpace($ExpiresAt)) {
    if ($Days -lt 1) {
        throw "Days must be greater than zero when -ExpiresAt is not provided."
    }

    $invokeParams.Days = $Days
}
else {
    $invokeParams.ExpiresAt = $ExpiresAt
}

$tokenOutput = & $invokeLicenseIssuerScript @invokeParams
$token = ($tokenOutput | Select-Object -Last 1).ToString().Trim()

if ([string]::IsNullOrWhiteSpace($token)) {
    throw "Activation token generation failed."
}

if (-not $SkipVerification) {
    $resolvedPublicKeyPath = $PublicKeyPath
    if ([string]::IsNullOrWhiteSpace($resolvedPublicKeyPath)) {
        $fallbackPublicKeyPath = Join-Path (Join-Path $scriptDir "..") "GroceryStoreManagement\Security\default_license_public_key.pem"
        if (Test-Path $fallbackPublicKeyPath -PathType Leaf) {
            $resolvedPublicKeyPath = [System.IO.Path]::GetFullPath($fallbackPublicKeyPath)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($resolvedPublicKeyPath)) {
        try {
            $verifyParams = @{
                Action = "verify-token"
                PublicKeyPath = $resolvedPublicKeyPath
                Token = $token
                MachineFingerprint = $MachineFingerprint
            }
            $null = & $invokeLicenseIssuerScript @verifyParams
        }
        catch {
            throw "Generated token does not match the app public key. Check private/public key pair. Details: $($_.Exception.Message)"
        }
    }
}

$expiryText = Try-GetTokenExpiryText -Token $token

if ($TokenOnly) {
    $clipboardValue = $token
    if (-not $NoClipboard) {
        try {
            Set-Clipboard -Value $clipboardValue
            Write-Host "Activation token copied to clipboard." -ForegroundColor Green
        }
        catch {
            Write-Host "Activation token generated, but clipboard copy failed." -ForegroundColor Yellow
        }
    }

    Write-Host "Issuer: $Issuer" -ForegroundColor Cyan
    Write-Output $token
    return
}

$durationText = if ([string]::IsNullOrWhiteSpace($ExpiresAt)) { "$Days يوم" } else { "حتى $ExpiresAt" }
$expiryLine = if ([string]::IsNullOrWhiteSpace($expiryText)) { "" } else { "تاريخ الانتهاء: $expiryText" }

$responseLines = @(
    "تم إصدار كود التفعيل بنجاح.",
    "الجهة المصدرة: $Issuer",
    "المدة: $durationText",
    "بصمة الجهاز: $MachineFingerprint"
)

if (-not [string]::IsNullOrWhiteSpace($expiryLine)) {
    $responseLines += $expiryLine
}

$responseLines += @(
    "كود التفعيل:",
    $token,
    "مهم: الصق الكود فقط داخل نافذة التفعيل."
)

$responseMessage = ($responseLines -join [Environment]::NewLine).Trim()

if (-not $NoClipboard) {
    try {
        Set-Clipboard -Value $responseMessage
        Write-Host "Activation reply message copied to clipboard." -ForegroundColor Green
    }
    catch {
        Write-Host "Reply message generated, but clipboard copy failed." -ForegroundColor Yellow
    }
}

Write-Host "Issuer: $Issuer" -ForegroundColor Cyan
if (-not [string]::IsNullOrWhiteSpace($expiryText)) {
    Write-Host "Expiry: $expiryText" -ForegroundColor Cyan
}
Write-Output $responseMessage
