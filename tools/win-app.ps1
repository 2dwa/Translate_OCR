param(
    [ValidateSet('restore', 'build', 'run', 'test', 'publish', 'installer')]
    [string] $Action = 'run'
)
. "$PSScriptRoot\WinApp.Common.ps1"
$configuration = $winConfig.configuration
if (-not $configuration) { $configuration = 'Debug' }
Push-Location $workspaceRoot
try {
    switch ($Action) {
        'restore' { Invoke-Dotnet @('restore', $projectPath); break }
        'build' { Invoke-Dotnet @('build', $projectPath, '-c', $configuration, '-f', $winConfig.framework, '-r', $winConfig.runtime); break }
        'run' { Invoke-Dotnet @('run', '--project', $projectPath, '-c', $configuration, '-f', $winConfig.framework, '-r', $winConfig.runtime); break }
        'test' { Invoke-Dotnet @('run', '--project', (Join-Path $workspaceRoot 'MiOcrDesk.Tests\MiOcrDesk.Tests.csproj')); break }
        'publish' { & "$PSScriptRoot\publish-bundle.ps1"; break }
        'installer' {
            $localCompiler = Join-Path $workspaceRoot '.tools\InnoSetup\ISCC.exe'
            $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
            if (Test-Path -LiteralPath $localCompiler) { $compilerPath = $localCompiler }
            elseif ($compiler) { $compilerPath = $compiler.Source }
            else { throw 'Inno Setup 6 is required. Add ISCC.exe to PATH or install to .tools\InnoSetup.' }
            & "$PSScriptRoot\publish-bundle.ps1"
            & $compilerPath "/DBundleDir=$publishPath" $installerPath
            if ($LASTEXITCODE -ne 0) { throw "Installer build failed (exit $LASTEXITCODE)." }
            break
        }
    }
}
finally { Pop-Location }
