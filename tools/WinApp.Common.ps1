$ErrorActionPreference = 'Stop'
$workspaceRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$winConfig = Get-Content -LiteralPath (Join-Path $workspaceRoot '.vscode\win_app.json') -Raw -Encoding UTF8 | ConvertFrom-Json
function Resolve-WinAppPath([string] $value) {
    $expanded = $value.Replace('${workspaceFolder}', $workspaceRoot)
    if (-not [IO.Path]::IsPathRooted($expanded)) { $expanded = Join-Path $workspaceRoot $expanded }
    return [IO.Path]::GetFullPath($expanded)
}
$projectPath = Resolve-WinAppPath $winConfig.project
$runtimeRoot = Resolve-WinAppPath $winConfig.ocrRuntime
if (-not (Test-Path -LiteralPath (Join-Path $runtimeRoot 'runtime\ollama.exe')) -and
    $runtimeRoot -eq (Join-Path $workspaceRoot 'OcrRuntime') -and
    (Test-Path -LiteralPath (Join-Path $workspaceRoot 'runtime\ollama.exe'))) { $runtimeRoot = $workspaceRoot }
$publishPath = Resolve-WinAppPath $winConfig.publishFolder
$installerPath = Resolve-WinAppPath $winConfig.installerScript
$localSdk = Join-Path $workspaceRoot '.dotnet\dotnet.exe'
if (Test-Path -LiteralPath $localSdk) { $dotnetPath = $localSdk; $env:DOTNET_ROOT = Split-Path -Parent $localSdk }
else {
    $sdkCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $sdkCommand) { throw '.NET 10 SDK is required. Install it, or extract the SDK to .dotnet.' }
    $dotnetPath = $sdkCommand.Source
}
$env:MIOCR_RUNTIME_ROOT = $runtimeRoot
function Invoke-Dotnet([string[]] $Arguments) {
    & $dotnetPath @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed (exit $LASTEXITCODE)." }
}
