param(
    [string] $Destination = (Join-Path (Split-Path -Parent $PSScriptRoot) 'OcrRuntime'),
    [switch] $VerifyOnly
)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$root = [IO.Path]::GetFullPath($Destination)
$sources = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'runtime-sources.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$manifestPath = Join-Path $PSScriptRoot 'xiaomi-ocr-0-bf16.manifest.json'
if ((Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash -ne $sources.modelManifestSha256) { throw 'Model manifest checksum mismatch.' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
function Test-Blob([string] $Path, [string] $Hash, [long] $Size) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    if ((Get-Item -LiteralPath $Path).Length -ne $Size) { return $false }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -eq $Hash
}
function Receive-VerifiedFile([string] $Url, [string] $Path, [string] $Hash, [long] $Size = -1) {
    if (-not $Url.StartsWith('https://', [StringComparison]::OrdinalIgnoreCase)) { throw 'Downloads must use HTTPS.' }
    $partial = $Path + '.partial'
    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($curl) {
        & $curl.Source --fail --location --retry 3 --proto '=https' --proto-redir '=https' --output $partial $Url
        if ($LASTEXITCODE -ne 0) { throw "Download failed: $Url" }
    } else { Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $partial }
    if ((Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash -ne $Hash) { throw "Checksum mismatch: $Path" }
    if ($Size -ge 0 -and (Get-Item -LiteralPath $partial).Length -ne $Size) { throw "Size mismatch: $Path" }
    Move-Item -LiteralPath $partial -Destination $Path -Force
}
if (-not $VerifyOnly) { New-Item -ItemType Directory -Path $root -Force | Out-Null }
$runtime = Join-Path $root 'runtime'
if (-not ((Test-Path -LiteralPath (Join-Path $runtime 'ollama.exe')) -and (Test-Path -LiteralPath (Join-Path $runtime 'lib')))) {
    if ($VerifyOnly) { throw 'Ollama runtime is missing.' }
    if (Test-Path -LiteralPath $runtime) { throw "Runtime folder is incomplete. Choose a new destination or repair this folder: $runtime" }
    Write-Host "Downloading Ollama $($sources.ollamaVersion) (about 1.6 GB)..."
    $archive = Join-Path $root 'ollama-windows-amd64.zip'
    Receive-VerifiedFile $sources.ollamaArchiveUrl $archive $sources.ollamaArchiveSha256
    $staging = [IO.Path]::GetFullPath((Join-Path $root ('.extract-' + [Guid]::NewGuid().ToString('N'))))
    $prefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $staging.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or -not ([IO.Path]::GetFullPath($runtime)).StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe extraction path.' }
    if ((Get-Item -LiteralPath $root).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The destination must not be a directory link.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $staging)
    if (-not (Test-Path -LiteralPath (Join-Path $staging 'ollama.exe'))) { throw 'Unexpected runtime archive layout.' }
    Move-Item -LiteralPath $staging -Destination $runtime
    Remove-Item -LiteralPath $archive -Force
}
$blobs = Join-Path $root 'models\blobs'
if (-not $VerifyOnly) { New-Item -ItemType Directory -Path $blobs -Force | Out-Null }
foreach ($blob in (@($manifest.layers) + @($manifest.config))) {
    if ($blob.digest -notmatch '^sha256:[a-f0-9]{64}$') { throw 'Invalid model digest.' }
    $hash = $blob.digest.Substring(7)
    $path = Join-Path $blobs $blob.digest.Replace(':', '-')
    if (Test-Blob $path $hash $blob.size) { Write-Host "Verified $($blob.digest)"; continue }
    if ($VerifyOnly) { throw "Missing or corrupt model file: $path" }
    Write-Host "Downloading model component ($([Math]::Round($blob.size / 1MB)) MB)..."
    Receive-VerifiedFile ($sources.modelBlobBaseUrl + $blob.digest) $path $hash $blob.size
}
$installedManifest = Join-Path $root 'models\manifests\registry.ollama.ai\longwayxu\xiaomi-ocr-0\bf16'
if ($VerifyOnly) {
    if (-not (Test-Path -LiteralPath $installedManifest) -or (Get-FileHash -LiteralPath $installedManifest -Algorithm SHA256).Hash -ne $sources.modelManifestSha256) { throw 'Installed model manifest does not match the pinned model.' }
} else {
    New-Item -ItemType Directory -Path (Split-Path -Parent $installedManifest) -Force | Out-Null
    Copy-Item -LiteralPath $manifestPath -Destination $installedManifest -Force
    $licenseSource = Join-Path (Split-Path -Parent $PSScriptRoot) 'licenses'
    Copy-Item -LiteralPath $licenseSource -Destination $root -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'runtime-sources.json') -Destination $root -Force
}
Write-Host "OCR runtime and pinned model are ready: $root"
