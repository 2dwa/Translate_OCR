param([Parameter(Mandatory = $true)][string] $Destination)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
foreach ($name in @('README.md', 'README.zh-CN.md', 'LICENSE', 'NOTICE', 'THIRD_PARTY_NOTICES.md', 'CHANGELOG.md', 'Setup-OCR.cmd')) {
    Copy-Item -LiteralPath (Join-Path $sourceRoot $name) -Destination $Destination -Force
}
foreach ($directory in @('licenses', 'docs')) {
    Copy-Item -LiteralPath (Join-Path $sourceRoot $directory) -Destination $Destination -Recurse -Force
}
$targetTools = Join-Path $Destination 'tools'
New-Item -ItemType Directory -Path $targetTools -Force | Out-Null
foreach ($name in @('install-ocr-runtime.ps1', 'runtime-sources.json', 'xiaomi-ocr-0-bf16.manifest.json')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $targetTools -Force
}
