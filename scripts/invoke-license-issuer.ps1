[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("open-ui", "gen-keys", "gen-token", "verify-token")]
    [string]$Action,

    [string]$LicenseIssuerPath = $env:LICENSE_ISSUER_PATH,
    [string]$PythonCommand = "py",
    [string]$OutputDir = "artifacts\license-keys",
    [string]$PrivateKeyPath = $env:LICENSE_PRIVATE_KEY_PATH,
    [string]$PublicKeyPath = $env:LICENSE_PUBLIC_KEY_PATH,
    [string]$MachineFingerprint,
    [string]$Issuer = $env:LICENSE_DEFAULT_ISSUER,
    [int]$Days,
    [string]$ExpiresAt,
    [string]$Token
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$licenseIssuerPathWasBound = $PSBoundParameters.ContainsKey("LicenseIssuerPath")
$privateKeyPathWasBound = $PSBoundParameters.ContainsKey("PrivateKeyPath")
$publicKeyPathWasBound = $PSBoundParameters.ContainsKey("PublicKeyPath")
$issuerWasBound = $PSBoundParameters.ContainsKey("Issuer")

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

function Require-Path {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not (Test-Path $Path)) {
        throw $Message
    }
}

function Resolve-LicenseIssuerRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CandidatePath
    )

    $resolvedCandidate = Resolve-Path $CandidatePath
    if ($resolvedCandidate -is [Array]) {
        $resolvedCandidate = $resolvedCandidate[0]
    }

    $fullCandidate = [System.IO.Path]::GetFullPath($resolvedCandidate.Path)

    if (Test-Path $fullCandidate -PathType Leaf) {
        if ([System.IO.Path]::GetFileName($fullCandidate) -ieq "app.py") {
            return Split-Path -Parent $fullCandidate
        }

        throw "License issuer path must be a directory or app.py file: $fullCandidate"
    }

    $appPy = Get-ChildItem -Path $fullCandidate -Recurse -File -Filter "app.py" -ErrorAction SilentlyContinue |
        Where-Object { Test-Path (Join-Path $_.Directory.FullName "crypto_service.py") -PathType Leaf } |
        Select-Object -First 1

    if ($null -eq $appPy) {
        throw "Could not locate Python license issuer app.py under: $fullCandidate"
    }

    return $appPy.Directory.FullName
}

function Resolve-OptionalPath {
    param(
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    return Resolve-FullPath -BasePath $RepoRoot -Path $Path
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir ".."))
$localConfigPath = Join-Path $scriptDir "license-tool.local.ps1"

if (Test-Path $localConfigPath -PathType Leaf) {
    . $localConfigPath
}

if (-not $licenseIssuerPathWasBound -and [string]::IsNullOrWhiteSpace($LicenseIssuerPath) -and -not [string]::IsNullOrWhiteSpace($env:LICENSE_ISSUER_PATH)) {
    $LicenseIssuerPath = $env:LICENSE_ISSUER_PATH
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

Require-Path -Path $LicenseIssuerPath -Message "License issuer path is required. Use -LicenseIssuerPath or LICENSE_ISSUER_PATH."

$licenseIssuerRoot = Resolve-LicenseIssuerRoot -CandidatePath $LicenseIssuerPath
$appPyPath = Join-Path $licenseIssuerRoot "app.py"

$resolvedOutputDir = Resolve-OptionalPath -Path $OutputDir -RepoRoot $repoRoot
$resolvedPrivateKeyPath = Resolve-OptionalPath -Path $PrivateKeyPath -RepoRoot $repoRoot
$resolvedPublicKeyPath = Resolve-OptionalPath -Path $PublicKeyPath -RepoRoot $repoRoot

$bootstrap = @"
import sys

sys.path.insert(0, r'''$licenseIssuerRoot''')
"@

switch ($Action) {
    "open-ui" {
        Write-Host "Opening license issuer UI from: $licenseIssuerRoot" -ForegroundColor Cyan
        & $PythonCommand $appPyPath
        break
    }

    "gen-keys" {
        if ($null -eq $resolvedOutputDir) {
            throw "OutputDir is required for gen-keys."
        }

        New-Item -ItemType Directory -Force -Path $resolvedOutputDir | Out-Null

        $code = @"
$bootstrap
from pathlib import Path
from crypto_service import generate_rsa_keypair

keys = generate_rsa_keypair(Path(r'''$resolvedOutputDir'''))
print(keys.private_key_path)
print(keys.public_key_path)
"@

        & $PythonCommand -c $code
        break
    }

    "gen-token" {
        if ([string]::IsNullOrWhiteSpace($resolvedPrivateKeyPath)) {
            throw "PrivateKeyPath is required for gen-token."
        }

        if ([string]::IsNullOrWhiteSpace($MachineFingerprint)) {
            throw "MachineFingerprint is required for gen-token."
        }

        if ($Days -le 0 -and [string]::IsNullOrWhiteSpace($ExpiresAt)) {
            throw "Specify -Days or -ExpiresAt for gen-token."
        }

        if ($Days -gt 0) {
            $expiryBlock = @"
from datetime import timedelta
from utils import utc_now
expiry_utc = utc_now() + timedelta(days=$Days)
"@
        }
        else {
            $expiryBlock = @"
from utils import parse_local_or_iso_datetime_to_utc
expiry_utc = parse_local_or_iso_datetime_to_utc(r'''$ExpiresAt''')
"@
        }

        $code = @"
$bootstrap
from pathlib import Path
from crypto_service import create_activation_token
$expiryBlock

token, payload = create_activation_token(
    private_key_path=Path(r'''$resolvedPrivateKeyPath'''),
    machine_fingerprint=r'''$MachineFingerprint''',
    expiry_utc=expiry_utc,
    issuer=r'''$Issuer''',
)
print(token)
"@

        & $PythonCommand -c $code
        break
    }

    "verify-token" {
        if ([string]::IsNullOrWhiteSpace($resolvedPublicKeyPath)) {
            throw "PublicKeyPath is required for verify-token."
        }

        if ([string]::IsNullOrWhiteSpace($Token)) {
            throw "Token is required for verify-token."
        }

        $verifyArgs = @(
            (Join-Path $licenseIssuerRoot "verify_token.py"),
            "--public-key", $resolvedPublicKeyPath,
            "--token", $Token
        )

        if (-not [string]::IsNullOrWhiteSpace($MachineFingerprint)) {
            $verifyArgs += @("--machine", $MachineFingerprint)
        }

        & $PythonCommand @verifyArgs
        break
    }
}
