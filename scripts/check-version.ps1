<#
.SYNOPSIS
Checks (or fixes) that Tauri/frontend version-bearing files match the authoritative .NET
application version defined in src/backend/Directory.Build.props.

.DESCRIPTION
See docs/development/VERSIONING.md for the full versioning design. Directory.Build.props
(VersionPrefix/VersionSuffix) is the single authoritative source; Cargo.toml, tauri.conf.json,
and the frontend package.json cannot read MSBuild XML directly, so this script is the documented,
repeatable procedure for propagating a version bump to them.

.PARAMETER Fix
When specified, rewrites the out-of-sync files instead of only reporting drift.

.EXAMPLE
.\scripts\check-version.ps1
.\scripts\check-version.ps1 -Fix
#>
param(
    [switch]$Fix
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

$repoRoot = Get-RepoRoot
$propsPath = Join-Path $repoRoot 'src/backend/Directory.Build.props'
$cargoPath = Join-Path $repoRoot 'src/tauri/Cargo.toml'
$tauriConfPath = Join-Path $repoRoot 'src/tauri/tauri.conf.json'
$packageJsonPath = Join-Path $repoRoot 'src/frontend/package.json'

$propsContent = Get-Content $propsPath -Raw
if ($propsContent -notmatch '<VersionPrefix>(.*?)</VersionPrefix>') {
    throw "Could not read VersionPrefix from $propsPath"
}
$versionPrefix = $Matches[1]

if ($propsContent -notmatch '<VersionSuffix>(.*?)</VersionSuffix>') {
    throw "Could not read VersionSuffix from $propsPath"
}
$versionSuffix = $Matches[1]

$authoritative = "$versionPrefix-$versionSuffix"
Write-Host "Authoritative version (from Directory.Build.props): $authoritative"

$mismatches = [System.Collections.Generic.List[string]]::new()

# --- Cargo.toml ---
$cargoContent = Get-Content $cargoPath -Raw
if ($cargoContent -notmatch '(?m)^version = "(.*?)"') {
    throw "Could not find version field in $cargoPath"
}
$cargoVersion = $Matches[1]

if ($cargoVersion -ne $authoritative) {
    $mismatches.Add("Cargo.toml: '$cargoVersion' != '$authoritative'")
    if ($Fix) {
        $replacement = 'version = "' + $authoritative + '"'
        $updated = $cargoContent -replace '(?m)^version = ".*?"', $replacement
        Set-Content -Path $cargoPath -Value $updated -NoNewline
        Write-Host "Fixed: $cargoPath -> $authoritative"
    }
}

# --- tauri.conf.json ---
# NOTE: tauri.conf.json's "version" drives the Windows MSI/WiX installer's ProductVersion,
# which only accepts numeric (major.minor.build) versions - it rejects non-numeric SemVer
# pre-release identifiers like "-alpha.1". So this file is intentionally checked/fixed against
# the numeric-only $versionPrefix, NOT the full $authoritative string. See docs/development/VERSIONING.md.
$tauriConfRaw = Get-Content $tauriConfPath -Raw
$tauriConfObj = $tauriConfRaw | ConvertFrom-Json
if ($tauriConfObj.version -ne $versionPrefix) {
    $mismatches.Add("tauri.conf.json: '$($tauriConfObj.version)' != '$versionPrefix' (numeric-only, see VERSIONING.md)")
    if ($Fix) {
        $replacement = '${1}"' + $versionPrefix + '"'
        $updated = $tauriConfRaw -replace '("version":\s*)"[^"]*"', $replacement
        Set-Content -Path $tauriConfPath -Value $updated -NoNewline
        Write-Host "Fixed: $tauriConfPath -> $versionPrefix"
    }
}

# --- frontend package.json ---
$packageJsonRaw = Get-Content $packageJsonPath -Raw
$packageJsonObj = $packageJsonRaw | ConvertFrom-Json
if ($packageJsonObj.version -ne $authoritative) {
    $mismatches.Add("package.json: '$($packageJsonObj.version)' != '$authoritative'")
    if ($Fix) {
        $replacement = '${1}"' + $authoritative + '"'
        $updated = $packageJsonRaw -replace '("version":\s*)"[^"]*"', $replacement
        Set-Content -Path $packageJsonPath -Value $updated -NoNewline
        Write-Host "Fixed: $packageJsonPath -> $authoritative"
    }
}

if ($mismatches.Count -eq 0) {
    Write-Host "All version-bearing files match (Cargo.toml/package.json: $authoritative; tauri.conf.json: $versionPrefix numeric-only, see VERSIONING.md)."
    exit 0
}

if ($Fix) {
    Write-Host "Fixed $($mismatches.Count) mismatch(es). Re-run without -Fix to verify."
    exit 0
}

Write-Host "Version drift detected:"
$mismatches | ForEach-Object { Write-Host "  - $_" }
exit 1
