param([string] $Tag, [switch] $SkipTests)
. "$PSScriptRoot\WinApp.Common.ps1"
[xml] $project = Get-Content -LiteralPath $projectPath -Raw
$version = [string] $project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a stable semantic project version.' }
if ($Tag -and $Tag -ne "v$version") { throw "Tag $Tag does not match project version $version." }
if (-not $SkipTests) { Invoke-Dotnet @('run', '--project', (Join-Path $workspaceRoot 'Translate.Tests\Translate.Tests.csproj')) }
$stage = Join-Path $workspaceRoot ('.cache\packages\' + $version + '-' + [Guid]::NewGuid().ToString('N'))
$output = Join-Path $workspaceRoot 'dist\releases'
New-Item -ItemType Directory -Path $stage, $output -Force | Out-Null
Invoke-Dotnet @('publish', $projectPath, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-o', $stage, '--nologo')
& "$PSScriptRoot\copy-package-files.ps1" -Destination $stage
# Symbols are not needed by the portable app and may contain development paths.
Get-ChildItem -LiteralPath $stage -File -Filter '*.pdb' | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
$archive = Join-Path $output "Translate-$version-win-x64-portable.zip"
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $output 'SHA256SUMS.txt'), "$hash  $([IO.Path]::GetFileName($archive))`n", [Text.UTF8Encoding]::new($false))
Write-Host "Release package: $archive"
Write-Host "SHA256: $hash"
