param(
    [Parameter(Mandatory = $true)][string] $BundleDirectory,
    [ValidateRange(1024, 65535)][int] $Port = 11436,
    [switch] $ForceCpu
)
$ErrorActionPreference = 'Stop'
$bundle = (Resolve-Path -LiteralPath $BundleDirectory).Path
$verified = 0
foreach ($inventory in @(@{ Root = $bundle; Name = 'APP-CONTENTS.sha256' }, @{ Root = (Join-Path $bundle 'OcrRuntime'); Name = 'MODEL-CONTENTS.sha256' })) {
    $entries = @(Get-Content -LiteralPath (Join-Path $inventory.Root $inventory.Name) -Encoding UTF8)
    foreach ($entry in $entries) {
        if ($entry -notmatch '^([a-f0-9]{64})  (.+)$') { throw 'Invalid offline file inventory.' }
        $expectedHash = $Matches[1]
        $path = [IO.Path]::GetFullPath((Join-Path $inventory.Root $Matches[2]))
        if (-not $path.StartsWith($inventory.Root.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe inventory path.' }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $expectedHash) { throw "Offline file checksum mismatch: $path" }
    }
    $verified += $entries.Count
}
if (@(Get-ChildItem -LiteralPath $bundle -File -Recurse).Count -ne $verified + 2) { throw 'Unexpected files in offline packages.' }
Write-Host "Verified $verified packaged files from both archives."
$runtimeRoot = Join-Path $bundle 'OcrRuntime'
& "$PSScriptRoot\install-ocr-runtime.ps1" -Destination $runtimeRoot -VerifyOnly
$baseUrl = "http://127.0.0.1:$Port"
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
try { $listener.Start() } finally { $listener.Stop() }
$logDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) ('.cache\offline-tests\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$variables = @{ OLLAMA_HOST = "127.0.0.1:$Port"; OLLAMA_MODELS = (Join-Path $runtimeRoot 'models'); OLLAMA_NO_CLOUD = '1' }
if ($ForceCpu) { $variables.CUDA_VISIBLE_DEVICES = '-1'; $variables.ROCR_VISIBLE_DEVICES = '-1'; $variables.GGML_VK_VISIBLE_DEVICES = '-1' }
$previous = @{}
$daemon = $null
try {
    try {
        foreach ($key in $variables.Keys) {
            $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
            [Environment]::SetEnvironmentVariable($key, $variables[$key], 'Process')
        }
        $daemon = Start-Process -FilePath (Join-Path $runtimeRoot 'runtime\ollama.exe') -ArgumentList 'serve' -WorkingDirectory (Join-Path $runtimeRoot 'runtime') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logDirectory 'stdout.log') -RedirectStandardError (Join-Path $logDirectory 'stderr.log')
    } finally {
        foreach ($key in $previous.Keys) { [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process') }
    }
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($daemon.HasExited) { throw "Bundled Ollama exited; see $logDirectory" }
        try { $null = Invoke-RestMethod "$baseUrl/api/version" -TimeoutSec 2; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'Bundled Ollama did not become ready.' }
    $models = Invoke-RestMethod "$baseUrl/api/tags" -TimeoutSec 10
    if ('longwayxu/xiaomi-ocr-0:bf16' -notin $models.models.name) { throw 'Bundled model not discovered.' }
    Add-Type -AssemblyName System.Drawing
    $bitmap = [Drawing.Bitmap]::new(800, 200)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $font = [Drawing.Font]::new('Arial', 40)
    $stream = [IO.MemoryStream]::new()
    try {
        $graphics.Clear([Drawing.Color]::White)
        $graphics.DrawString('OFFLINE OCR 12345', $font, [Drawing.Brushes]::Black, 20, 60)
        $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
        $image = [Convert]::ToBase64String($stream.ToArray())
    } finally { $stream.Dispose(); $font.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
    $options = @{ temperature = 0; num_predict = 128 }
    if ($ForceCpu) { $options.num_gpu = 0 }
    $payload = @{ model = 'longwayxu/xiaomi-ocr-0:bf16'; stream = $false; keep_alive = '2m'; options = $options; messages = @(@{ role = 'user'; content = 'Extract all text from this image.'; images = @($image) }) } | ConvertTo-Json -Depth 8
    Write-Host "Testing extracted OCR runtime (CPU forced: $ForceCpu)..."
    $result = Invoke-RestMethod "$baseUrl/api/chat" -Method Post -ContentType 'application/json' -Body $payload -TimeoutSec 600
    if ($result.message.content -notmatch '12345' -or $result.message.content -notmatch 'OFFLINE') { throw "Unexpected OCR result: $($result.message.content)" }
    $loaded = Invoke-RestMethod "$baseUrl/api/ps" -TimeoutSec 10
    $gpuBytes = ($loaded.models | Measure-Object -Property size_vram -Sum).Sum
    if ($ForceCpu -and $gpuBytes -gt 0) { throw 'CPU-only test unexpectedly used GPU memory.' }
    Write-Host "OCR passed: $($result.message.content.Trim()); GPU bytes: $gpuBytes"
    $unload = @{ model = 'longwayxu/xiaomi-ocr-0:bf16'; keep_alive = 0 } | ConvertTo-Json
    $null = Invoke-RestMethod "$baseUrl/api/generate" -Method Post -ContentType 'application/json' -Body $unload -TimeoutSec 30
} finally {
    if ($null -ne $daemon -and -not $daemon.HasExited) {
        # Only terminate the daemon/process tree created by this test, never an existing service.
        if ($daemon.GetType().GetMethod('Kill', [type[]] @([bool]))) { $daemon.Kill($true) }
        else {
            $stopInfo = [Diagnostics.ProcessStartInfo]::new('taskkill.exe', "/PID $($daemon.Id) /T /F")
            $stopInfo.UseShellExecute = $false
            $stopInfo.CreateNoWindow = $true
            $stopInfo.RedirectStandardOutput = $true
            $stopInfo.RedirectStandardError = $true
            $stopProcess = [Diagnostics.Process]::Start($stopInfo)
            try {
                $null = $stopProcess.StandardOutput.ReadToEnd()
                $null = $stopProcess.StandardError.ReadToEnd()
                $stopProcess.WaitForExit()
            } finally { $stopProcess.Dispose() }
        }
        $daemon.WaitForExit(10000) | Out-Null
        if (-not $daemon.HasExited) { throw 'The test daemon did not stop.' }
    }
    if ($null -ne $daemon) { $daemon.Dispose() }
}
Write-Host 'Offline smoke test passed; owned daemon stopped.'
$global:LASTEXITCODE = 0
