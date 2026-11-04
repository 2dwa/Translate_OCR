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

# Keep software and models in separate staging directories and separate archives.
$stage = Join-Path $repo ('.cache\offline-packages\' + $version + '-' + [Guid]::NewGuid().ToString('N'))
$appStage = Join-Path $stage 'app'
$modelStage = Join-Path $stage 'models'
New-Item -ItemType Directory -Path $appStage, $modelStage, $OutputDirectory -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($PortableZip, $appStage)
if (-not (Test-Path -LiteralPath (Join-Path $appStage 'MiOcrDesk.exe'))) { throw 'Missing portable executable.' }
& "$PSScriptRoot\copy-package-files.ps1" -Destination $appStage
Copy-Item -LiteralPath (Join-Path $repo 'docs\OFFLINE.md') -Destination (Join-Path $appStage 'README-OFFLINE.md')

$bundle = Join-Path $modelStage 'OcrRuntime'
$targetRuntime = Join-Path $bundle 'runtime'
$targetLib = Join-Path $targetRuntime 'lib\ollama'
New-Item -ItemType Directory -Path $targetLib -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $RuntimeRoot 'runtime\ollama.exe') -Destination $targetRuntime
$sourceLib = Join-Path $RuntimeRoot 'runtime\lib\ollama'
# Keep every CPU variant, CPU dependency and upstream notice. This distribution
# selects CPU + CUDA 13 rather than shipping every optional upstream GPU backend.
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
Copy-Item -LiteralPath (Join-Path $repo 'licenses') -Destination $bundle -Recurse
foreach ($name in @('LICENSE', 'NOTICE', 'THIRD_PARTY_NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $bundle
}
Copy-Item -LiteralPath (Join-Path $repo 'docs\OFFLINE.md') -Destination (Join-Path $bundle 'README-MODEL.md')
New-Item -ItemType Directory -Path (Join-Path $bundle 'tools') -Force | Out-Null
foreach ($name in @('runtime-sources.json', 'xiaomi-ocr-0-bf16.manifest.json')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $bundle 'tools')
}
& "$PSScriptRoot\install-ocr-runtime.ps1" -Destination $bundle -VerifyOnly

# Each package has its own inventory. Their contents never overlap on extraction.
function Write-Inventory([string] $Directory, [string] $Name) {
    $prefix = $Directory.TrimEnd('\') + '\'
    $lines = Get-ChildItem -LiteralPath $Directory -File -Recurse | Sort-Object FullName | ForEach-Object {
        $relative = $_.FullName.Substring($prefix.Length).Replace('\', '/')
        if ($relative -match '(^|/)(settings\.json|id_(rsa|ed25519)[^/]*|\.env)(/|$)|\.(log|pid|pdb|key|pem)$') { throw "Unexpected private/development file: $relative" }
        "$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())  $relative"
    }
    [IO.File]::WriteAllLines((Join-Path $Directory $Name), [string[]] $lines, [Text.UTF8Encoding]::new($false))
}
Write-Inventory $appStage 'APP-CONTENTS.sha256'
Write-Inventory $bundle 'MODEL-CONTENTS.sha256'
if (Test-Path -LiteralPath (Join-Path $appStage 'OcrRuntime')) { throw 'Software archive must not include a runtime or model.' }
if (Test-Path -LiteralPath (Join-Path $modelStage 'MiOcrDesk.exe')) { throw 'Model archive must not include the desktop application.' }
$checksums = @()
foreach ($package in @(@{ Name = 'app'; Directory = $appStage }, @{ Name = 'models'; Directory = $modelStage })) {
    $archive = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) "MiOCRDesk-$version-win-x64-$($package.Name).zip"
    if (Test-Path -LiteralPath $archive) { throw "Output already exists; choose a fresh output directory: $archive" }
    Write-Host "Compressing separate $($package.Name) ZIP from $($package.Directory) ..."
    [IO.Compression.ZipFile]::CreateFromDirectory($package.Directory, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
    if ((Get-Item -LiteralPath $archive).Length -ge 2GB) { throw 'Archive exceeds the GitHub Release per-asset limit.' }
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    $checksums += "$hash  $([IO.Path]::GetFileName($archive))"
    Write-Host "Package: $archive ($((Get-Item -LiteralPath $archive).Length) bytes); SHA256: $hash"
}
[IO.File]::WriteAllLines((Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) 'SHA256SUMS-OFFLINE.txt'), [string[]] $checksums, [Text.UTF8Encoding]::new($false))
