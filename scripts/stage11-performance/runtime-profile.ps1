$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$captures = Join-Path $PSScriptRoot 'captures'
$exe = Join-Path $root 'publish/backend-sidecar/Kst.Api.exe'
$stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
$stdout = Join-Path $captures "runtime-$stamp-stdout.log"
$stderr = Join-Path $captures "runtime-$stamp-stderr.log"
$resultsPath = Join-Path $captures "production-runtime-$stamp.jsonl"
$process = Start-Process $exe -WorkingDirectory (Split-Path $exe) -ArgumentList '--port=0' -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(120)
try {
    $base = $null
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    while (-not $base -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 100
        $stream = [IO.File]::Open($stdout, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
        $reader = New-Object IO.StreamReader($stream)
        try { $text = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $match = [regex]::Match($text, 'http://127\.0\.0\.1:\d+')
        if ($match.Success) { $base = $match.Value }
        if ($process.HasExited) { throw 'Sidecar exited before readiness.' }
    }
    if (-not $base) { throw 'No loopback startup handshake.' }
    $workspaces = [System.IO.File]::ReadAllText((Join-Path $env:LOCALAPPDATA 'KST/config/workspaces.json')) | ConvertFrom-Json
    foreach ($workspace in $workspaces | Where-Object { $_.productLineFrom -in @('2140','3230','1391') } | Sort-Object { [array]::IndexOf(@('2140','3230','1391'), $_.productLineFrom) }) {
        $path = "$base/api/v1/workspaces/$($workspace.assignmentId)"
        $mps = $client.GetStringAsync("$path/mps").GetAwaiter().GetResult() | ConvertFrom-Json
        $snapshot = $mps.snapshot.snapshotId
        if (-not $snapshot) { throw 'MPS snapshot unavailable.' }
        $part = $null
        foreach ($run in 0..1) {
            $timer = [Diagnostics.Stopwatch]::StartNew()
            $response = $client.GetAsync("$path/long-term-shortages/screen?snapshotId=$snapshot", [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
            $headersMs = $timer.Elapsed.TotalMilliseconds
            $response.EnsureSuccessStatusCode() | Out-Null
            $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
            $totalMs = $timer.Elapsed.TotalMilliseconds
            $report = [Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
            $part = $report.components[0].componentPart
            [IO.File]::WriteAllBytes((Join-Path $captures "$($workspace.productLineFrom)-live-$stamp-production-api.json"), $bytes)
            $result = [ordered]@{phase='runtime-shortages'; workspace=$workspace.productLineFrom; run=$run; headersMs=$headersMs; totalMs=$totalMs; bytes=$bytes.Length; components=$report.components.Count}
            $line = $result | ConvertTo-Json -Compress
            Write-Output $line
            [IO.File]::AppendAllText($resultsPath, $line + [Environment]::NewLine)
        }
        $timer = [Diagnostics.Stopwatch]::StartNew()
        $response = $client.GetAsync("$path/long-term-shortages/projection-detail?snapshotId=$snapshot&componentPart=$([Uri]::EscapeDataString($part))").GetAwaiter().GetResult()
        $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $result = [ordered]@{phase='runtime-projection-detail'; workspace=$workspace.productLineFrom; totalMs=$timer.Elapsed.TotalMilliseconds; status=[int]$response.StatusCode; bytes=$bytes.Length}
        $line = $result | ConvertTo-Json -Compress
        Write-Output $line
        [IO.File]::AppendAllText($resultsPath, $line + [Environment]::NewLine)
        # Read only the already-cached control shape for offline browser comparisons. No refresh.
        $control = $client.GetByteArrayAsync("$path/long-term-shortages?snapshotId=$snapshot&includeEvidence=false").GetAwaiter().GetResult()
        [IO.File]::WriteAllBytes((Join-Path $captures "$($workspace.productLineFrom)-live-$stamp-control-api.json"), $control)
        $timer = [Diagnostics.Stopwatch]::StartNew()
        $response = $client.GetAsync("$path/long-term-shortages/purchasing?snapshotId=$snapshot&componentPart=$([Uri]::EscapeDataString($part))").GetAwaiter().GetResult()
        $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $result = [ordered]@{phase='runtime-purchasing'; workspace=$workspace.productLineFrom; totalMs=$timer.Elapsed.TotalMilliseconds; status=[int]$response.StatusCode; bytes=$bytes.Length}
        $line = $result | ConvertTo-Json -Compress
        Write-Output $line
        [IO.File]::AppendAllText($resultsPath, $line + [Environment]::NewLine)
    }
}
finally {
    $client.Dispose()
    if (-not $process.HasExited) { Stop-Process -Id $process.Id }
}
