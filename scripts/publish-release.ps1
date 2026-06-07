[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$ProjectPath = "GroceryStoreManagement\GroceryStoreManagement.csproj",
    [string]$OutputDir = "artifacts\release\publish",
    [string]$LicenseIssuerPath = $env:LICENSE_ISSUER_PATH,
    [string]$LicenseIssuerProjectPath = $env:LICENSE_ISSUER_PROJECT,
    [string]$LicensePublicKeyPath = $env:LICENSE_PUBLIC_KEY_PATH,
    [switch]$GenerateLicenseKeys,
    [string]$LicenseKeysOutputDir = "artifacts\license-keys"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$licenseIssuerPathWasBound = $PSBoundParameters.ContainsKey("LicenseIssuerPath")
$licenseIssuerProjectPathWasBound = $PSBoundParameters.ContainsKey("LicenseIssuerProjectPath")
$licensePublicKeyPathWasBound = $PSBoundParameters.ContainsKey("LicensePublicKeyPath")

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

function Require-File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not (Test-Path $FilePath -PathType Leaf)) {
        throw $Message
    }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir ".."))
$localConfigPath = Join-Path $scriptDir "license-tool.local.ps1"

if (Test-Path $localConfigPath -PathType Leaf) {
    Write-Host "Loading local license tool config: $localConfigPath" -ForegroundColor Cyan
    . $localConfigPath
}

if (-not $licenseIssuerProjectPathWasBound -and [string]::IsNullOrWhiteSpace($LicenseIssuerProjectPath) -and -not [string]::IsNullOrWhiteSpace($env:LICENSE_ISSUER_PROJECT)) {
    $LicenseIssuerProjectPath = $env:LICENSE_ISSUER_PROJECT
}

if (-not $licenseIssuerPathWasBound -and [string]::IsNullOrWhiteSpace($LicenseIssuerPath) -and -not [string]::IsNullOrWhiteSpace($env:LICENSE_ISSUER_PATH)) {
    $LicenseIssuerPath = $env:LICENSE_ISSUER_PATH
}

if (-not $licensePublicKeyPathWasBound -and [string]::IsNullOrWhiteSpace($LicensePublicKeyPath) -and -not [string]::IsNullOrWhiteSpace($env:LICENSE_PUBLIC_KEY_PATH)) {
    $LicensePublicKeyPath = $env:LICENSE_PUBLIC_KEY_PATH
}

$projectFullPath = Resolve-FullPath -BasePath $repoRoot -Path $ProjectPath
Require-File -FilePath $projectFullPath -Message "Project file not found: $projectFullPath"

$outputFullPath = Resolve-FullPath -BasePath $repoRoot -Path $OutputDir
$keysOutputFullPath = Resolve-FullPath -BasePath $repoRoot -Path $LicenseKeysOutputDir

Write-Host "Publishing application..." -ForegroundColor Cyan
dotnet publish $projectFullPath -c $Configuration -o $outputFullPath

$publishDataDir = Join-Path $outputFullPath "Data"
New-Item -ItemType Directory -Force -Path $publishDataDir | Out-Null

$invokeLicenseIssuerScript = Join-Path $scriptDir "invoke-license-issuer.ps1"
$resolvedLicenseIssuerProjectPath = $null
if (-not [string]::IsNullOrWhiteSpace($LicenseIssuerProjectPath)) {
    $resolvedLicenseIssuerProjectPath = Resolve-FullPath -BasePath $repoRoot -Path $LicenseIssuerProjectPath
    Require-File -FilePath $resolvedLicenseIssuerProjectPath -Message "License issuer project not found: $resolvedLicenseIssuerProjectPath"
}

$resolvedLicenseIssuerPath = $null
if (-not [string]::IsNullOrWhiteSpace($LicenseIssuerPath)) {
    $resolvedLicenseIssuerPath = Resolve-FullPath -BasePath $repoRoot -Path $LicenseIssuerPath
    if (-not (Test-Path $resolvedLicenseIssuerPath)) {
        throw "License issuer path not found: $resolvedLicenseIssuerPath"
    }
}

$resolvedPublicKeySource = $null

if (-not [string]::IsNullOrWhiteSpace($LicensePublicKeyPath)) {
    $resolvedPublicKeySource = Resolve-FullPath -BasePath $repoRoot -Path $LicensePublicKeyPath
    Require-File -FilePath $resolvedPublicKeySource -Message "Public key file not found: $resolvedPublicKeySource"
}
elseif (-not $GenerateLicenseKeys) {
    $repoPublicKey = Join-Path $repoRoot "GroceryStoreManagement\Data\license_public_key.pem"
    if (Test-Path $repoPublicKey -PathType Leaf) {
        $resolvedPublicKeySource = $repoPublicKey
    }
    else {
        $embeddedPublicKey = Join-Path $repoRoot "GroceryStoreManagement\Security\default_license_public_key.pem"
        if (Test-Path $embeddedPublicKey -PathType Leaf) {
            $resolvedPublicKeySource = $embeddedPublicKey
        }
        else {
            $defaultUserPublicKey = Join-Path $HOME "Documents\license_public_key.pem"
            if (Test-Path $defaultUserPublicKey -PathType Leaf) {
                $resolvedPublicKeySource = $defaultUserPublicKey
            }
        }
    }
}

if ($GenerateLicenseKeys -and $null -eq $resolvedPublicKeySource) {
    if ($null -eq $resolvedLicenseIssuerProjectPath -and $null -eq $resolvedLicenseIssuerPath) {
        throw "Cannot generate keys without license issuer path. Use -LicenseIssuerPath, -LicenseIssuerProjectPath, LICENSE_ISSUER_PATH, or LICENSE_ISSUER_PROJECT."
    }

    New-Item -ItemType Directory -Force -Path $keysOutputFullPath | Out-Null
    Write-Host "Generating license keys using external tool..." -ForegroundColor Cyan

    if ($null -ne $resolvedLicenseIssuerPath) {
        & $invokeLicenseIssuerScript -Action gen-keys -LicenseIssuerPath $resolvedLicenseIssuerPath -OutputDir $keysOutputFullPath
    }
    else {
        dotnet run --project $resolvedLicenseIssuerProjectPath -- gen-keys --out $keysOutputFullPath
    }

    $generatedPublicKey = Join-Path $keysOutputFullPath "license_public_key.pem"
    Require-File -FilePath $generatedPublicKey -Message "Generated public key was not found: $generatedPublicKey"
    $resolvedPublicKeySource = $generatedPublicKey
}

if ($null -eq $resolvedPublicKeySource) {
    throw @"
Missing license public key.
Provide one of the following:
  1) -LicensePublicKeyPath <path\to\license_public_key.pem>
  2) LICENSE_PUBLIC_KEY_PATH environment variable
  3) -GenerateLicenseKeys with -LicenseIssuerPath / -LicenseIssuerProjectPath
"@
}

$targetPublicKey = Join-Path $publishDataDir "license_public_key.pem"
Copy-Item -Path $resolvedPublicKeySource -Destination $targetPublicKey -Force

Write-Host "Publish output: $outputFullPath" -ForegroundColor Green
Write-Host "Public key copied: $targetPublicKey" -ForegroundColor Green

if ($resolvedLicenseIssuerPath) {
    Write-Host "External Python license issuer linked: $resolvedLicenseIssuerPath" -ForegroundColor Green
}
elseif ($resolvedLicenseIssuerProjectPath) {
    Write-Host "External .NET license issuer linked: $resolvedLicenseIssuerProjectPath" -ForegroundColor Green
}
