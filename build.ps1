param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$outDir = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $PSScriptRoot 'dist' }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$output = Join-Path $outDir "AlphaWolf's Deathloop Skin Unlocker.exe"
$source = Join-Path $PSScriptRoot 'OutfitUnlocker.cs'
$manifest = Join-Path $PSScriptRoot 'app.manifest'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$output" "/win32manifest:$manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll $source
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Write-Output $output
