param(
    [Parameter(Mandatory = $true)][string] $PortableZip,
    [Parameter(Mandatory = $true)][string] $PortableChecksums,
    [Parameter(Mandatory = $true)][string] $RuntimeRoot,
    [string] $OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'dist\releases' }
[xml] $project = Get-Content -LiteralPath (Join-Path $repo 'MiOcrDesk\MiOcrDesk.csproj') -Raw
$version = [string] $project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a stable semantic project version.' }
$PortableZip = (Resolve-Path -LiteralPath $PortableZip).Path
$RuntimeRoot = (Resolve-Path -LiteralPath $RuntimeRoot).Path
$expectedName = "MiOCRDesk-$version-win-x64-portable.zip"
if ([IO.Path]::GetFileName($PortableZip) -ne $expectedName) { throw 'Portable archive version/name does not match the project.' }
$checksumLine = @(Get-Content -LiteralPath $PortableChecksums | Where-Object { $_ -match ('^[a-fA-F0-9]{64}\s+\*?' + [regex]::Escape($expectedName) + '$') })
if ($checksumLine.Count -ne 1) { throw 'Expected exactly one portable archive checksum.' }
if ((Get-FileHash -LiteralPath $PortableZip -Algorithm SHA256).Hash -ne $checksumLine[0].Substring(0, 64)) { throw 'Portable ZIP checksum mismatch.' }
& "$PSScriptRoot\install-ocr-runtime.ps1" -Destination $RuntimeRoot -VerifyOnly

# A fresh directory avoids touching an existing installation or user settings.
$stage = Join-Path $repo ('.cache\offline-packages\' + $version + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage, $OutputDirectory -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($PortableZip, $stage)
if (-not (Test-Path -LiteralPath (Join-Path $stage 'MiOcrDesk.exe'))) { throw 'Missing portable executable.' }
& "$PSScriptRoot\copy-package-files.ps1" -Destination $stage
Copy-Item -LiteralPath (Join-Path $repo 'docs\OFFLINE.md') -Destination (Join-Path $stage 'README-OFFLINE.md')

$bundle = Join-Path $stage 'OcrRuntime'
$targetRuntime = Join-Path $bundle 'runtime'
$targetLib = Join-Path $targetRuntime 'lib\ollama'
New-Item -ItemType Directory -Path $targetLib -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $RuntimeRoot 'runtime\ollama.exe') -Destination $targetRuntime
$sourceLib = Join-Path $RuntimeRoot 'runtime\lib\ollama'
# Keep every CPU variant, CPU dependency and upstream notice. CUDA 12 and Vulkan
# are optional backends; omitting them keeps this standard ZIP below GitHub's 2 GiB limit.
Get-ChildItem -LiteralPath $sourceLib -File | Where-Object {
    $_.Extension -in @('.dll', '.exe') -or $_.Name -match 'LICENSE|NOTICE|COPYING'
} | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $targetLib }
$cuda = Join-Path $sourceLib 'cuda_v13'
if (-not (Test-Path -LiteralPath (Join-Path $cuda 'ggml-cuda.dll'))) { throw 'The pinned CUDA 13 backend is missing.' }
$targetCuda = Join-Path $targetLib 'cuda_v13'
New-Item -ItemType Directory -Path $targetCuda -Force | Out-Null
Get-ChildItem -LiteralPath $cuda -File | Where-Object {
    $_.Extension -eq '.dll' -or $_.Name -match 'LICENSE|NOTICE|COPYING'
} | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $targetCuda }
foreach ($required in @('llama-server.exe', 'ggml-cpu-x64.dll', 'LLAMA_CPP_LICENSE', 'GO_LICENSE', 'CPP_HTTPLIB_LICENSE', 'LLAMA_CPP_VENDORS_LICENSE')) {
    if (-not (Test-Path -LiteralPath (Join-Path $targetLib $required))) { throw "Missing runtime component: $required" }
}

# Copy only the pinned model, never other local models, logs, keys or configuration.
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'xiaomi-ocr-0-bf16.manifest.json') -Raw | ConvertFrom-Json
$targetBlobs = Join-Path $bundle 'models\blobs'
$targetManifest = Join-Path $bundle 'models\manifests\registry.ollama.ai\longwayxu\xiaomi-ocr-0'
New-Item -ItemType Directory -Path $targetBlobs, $targetManifest -Force | Out-Null
foreach ($blob in (@($manifest.layers) + @($manifest.config))) {
    $name = $blob.digest.Replace(':', '-')
    Copy-Item -LiteralPath (Join-Path $RuntimeRoot "models\blobs\$name") -Destination $targetBlobs
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'xiaomi-ocr-0-bf16.manifest.json') -Destination (Join-Path $targetManifest 'bf16')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'runtime-sources.json') -Destination $bundle
& "$PSScriptRoot\install-ocr-runtime.ps1" -Destination $bundle -VerifyOnly

# A per-file manifest makes the exact offline contents independently auditable.
$inventory = Join-Path $stage 'OFFLINE-CONTENTS.sha256'
$prefix = $stage.TrimEnd('\') + '\'
$lines = Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($prefix.Length).Replace('\', '/')
    if ($relative -match '(^|/)(settings\.json|id_(rsa|ed25519)[^/]*|\.env)(/|$)|\.(log|pid|pdb|key|pem)$') { throw "Unexpected private/development file: $relative" }
    "$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())  $relative"
}
[IO.File]::WriteAllLines($inventory, [string[]] $lines, [Text.UTF8Encoding]::new($false))
$archive = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) "MiOCRDesk-$version-win-x64-offline.zip"
if (Test-Path -LiteralPath $archive) { throw "Output already exists; choose a fresh output directory: $archive" }
Write-Host "Compressing offline ZIP from $stage ..."
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
if ((Get-Item -LiteralPath $archive).Length -ge 2GB) { throw 'Offline archive exceeds the GitHub Release per-asset limit.' }
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path (Split-Path -Parent $archive) 'SHA256SUMS-OFFLINE.txt'), "$hash  $([IO.Path]::GetFileName($archive))`n", [Text.UTF8Encoding]::new($false))
Write-Host "Offline package: $archive"
Write-Host "Size: $((Get-Item -LiteralPath $archive).Length) bytes"
Write-Host "SHA256: $hash"
