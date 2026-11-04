param([switch] $AppOnly)
. "$PSScriptRoot\WinApp.Common.ps1"
# Only generated contents below dist may be replaced; never trust a configured root as a deletion target.
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $workspaceRoot 'dist')) + [IO.Path]::DirectorySeparatorChar
if (-not $publishPath.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'publishFolder must be a subdirectory of workspace/dist.' }
if (-not $AppOnly) {
    foreach ($component in @('runtime\ollama.exe', 'runtime\lib', 'models\blobs', 'models\manifests\registry.ollama.ai\longwayxu\xiaomi-ocr-0\bf16')) {
        if (-not (Test-Path -LiteralPath (Join-Path $runtimeRoot $component))) { throw "Missing offline runtime component: $component" }
    }
}
if (Test-Path -LiteralPath $publishPath) {
    # Resolve links before recursive deletion as well as checking the lexical path above.
    $existing = Get-Item -LiteralPath $publishPath -Force
    if ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing to replace a linked publish directory.' }
    $parent = $existing.Parent
    while ($parent -and $parent.FullName.Length -gt $workspaceRoot.Length) {
        if ($parent.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Publish parent must not be a link.' }
        $parent = $parent.Parent
    }
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}
New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
Push-Location $workspaceRoot
try { Invoke-Dotnet @('publish', $projectPath, '-c', 'Release', '-f', $winConfig.framework, '-r', $winConfig.runtime, '--self-contained', 'true', '-p:PublishSingleFile=true', '-o', $publishPath) }
finally { Pop-Location }
if (-not $AppOnly) {
    $destination = Join-Path $publishPath 'OcrRuntime'
    New-Item -ItemType Directory -Path (Join-Path $destination 'runtime') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $runtimeRoot 'runtime\ollama.exe') -Destination (Join-Path $destination 'runtime')
    Copy-Item -LiteralPath (Join-Path $runtimeRoot 'runtime\lib') -Destination (Join-Path $destination 'runtime') -Recurse
    Copy-Item -LiteralPath (Join-Path $runtimeRoot 'models') -Destination $destination -Recurse
    Copy-Item -LiteralPath (Join-Path $workspaceRoot 'licenses') -Destination $destination -Recurse -Force
}
& "$PSScriptRoot\copy-package-files.ps1" -Destination $publishPath
Write-Host "Ready: $publishPath"
