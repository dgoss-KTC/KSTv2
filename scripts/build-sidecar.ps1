$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

$repoRoot = Get-RepoRoot
$backendProject = Join-Path $repoRoot 'src/backend/Kst.Api/Kst.Api.csproj'
$publishOutput = Join-Path $repoRoot 'publish/backend-sidecar'
$tauriBinDir = Join-Path $repoRoot 'src/tauri/binaries'
$targetTriple = 'x86_64-pc-windows-msvc'
$targetSidecarPath = Join-Path $tauriBinDir "Kst.Api-$targetTriple.exe"

if (-not (Test-Path $backendProject)) {
    throw "Backend project not found: $backendProject"
}

if (-not (Test-Path $tauriBinDir)) {
    New-Item -ItemType Directory -Path $tauriBinDir -Force | Out-Null
}

if (Test-Path $publishOutput) {
    Remove-Item -Path $publishOutput -Recurse -Force
}

Write-Host "Repository root: $repoRoot"
Write-Host "Backend project: $backendProject"
Write-Host "Publish output: $publishOutput"
Write-Host "Tauri sidecar target: $targetSidecarPath"

$dotnetArgs = @(
    'publish',
    $backendProject,
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'true',
    '/p:PublishSingleFile=true',
    '/p:PublishTrimmed=false',
    '/p:PublishAot=false',
    '/p:DebugType=None',
    '/p:DebugSymbols=false',
    # Required for PublishSingleFile: without this, native interop libraries (e.g. SqlClient's
    # SNI.dll, needed for Windows Integrated Auth) are not extracted and QAD connections fail
    # with System.DllNotFoundException at runtime.
    '/p:IncludeNativeLibrariesForSelfExtract=true',
    '-o', $publishOutput
)

Write-Host "Running: dotnet $($dotnetArgs -join ' ')"
& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$publishedExe = Join-Path $publishOutput 'Kst.Api.exe'
if (-not (Test-Path $publishedExe)) {
    throw "Published sidecar executable not found: $publishedExe"
}

if ((Get-Item $publishedExe).LastWriteTimeUtc -lt (Get-Date).ToUniversalTime().AddMinutes(-10)) {
    throw "Published sidecar appears stale: $publishedExe"
}

Copy-Item -Path $publishedExe -Destination $targetSidecarPath -Force
if (-not (Test-Path $targetSidecarPath)) {
    throw "Failed to copy sidecar to Tauri binaries path: $targetSidecarPath"
}

# The sidecar is launched by Tauri as a bare executable (no `dotnet publish` output folder
# alongside it), so its appsettings*.json files must be copied next to it explicitly - otherwise
# ASP.NET Core's optional JSON config providers silently find nothing and QadDatabase/etc. bind
# to their empty defaults regardless of ASPNETCORE_ENVIRONMENT.
$configFiles = @('appsettings.json', 'appsettings.Development.json')
foreach ($configFile in $configFiles) {
    $sourcePath = Join-Path $publishOutput $configFile
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination (Join-Path $tauriBinDir $configFile) -Force
    }
}

$publishedInfo = Get-Item $publishedExe
$targetInfo = Get-Item $targetSidecarPath

Write-Host ""
Write-Host "Sidecar build complete"
Write-Host "Published executable: $($publishedInfo.FullName)"
Write-Host ("Published size: {0:N0} bytes" -f $publishedInfo.Length)
Write-Host "Copied sidecar: $($targetInfo.FullName)"
Write-Host ("Copied size: {0:N0} bytes" -f $targetInfo.Length)
Write-Host "Copied config files: $($configFiles -join ', ')"
