#Requires -Version 5.1
param(
    [string]$Version = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root   = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $root 'AutodeskSoftwareManager.csproj'

[xml]$proj = Get-Content $csproj
$fileVer   = $proj.Project.PropertyGroup.AssemblyVersion
if (-not $Version) { $Version = $fileVer }

$outDir = Join-Path $root ('publish\v' + $Version)

Write-Host ''
Write-Host ('  Publishing AutodeskSoftwareManager v' + $Version) -ForegroundColor Cyan
Write-Host ('  Output: ' + $outDir) -ForegroundColor DarkGray
Write-Host ''

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

$sw = [System.Diagnostics.Stopwatch]::StartNew()
dotnet publish $csproj -c Release -o $outDir --nologo
$sw.Stop()

if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host '  PUBLISH FAILED' -ForegroundColor Red
} else {
    $exe = Get-ChildItem $outDir -Filter 'AutodeskSoftwareManager.exe' | Select-Object -First 1
    $mb  = [math]::Round($exe.Length / 1MB, 1)
    Write-Host ''
    Write-Host ('  Publish succeeded in ' + $sw.Elapsed.TotalSeconds.ToString('0.0') + 's') -ForegroundColor Green
    Write-Host ('  Executable : ' + $exe.FullName) -ForegroundColor White
    Write-Host ('  Size       : ' + $mb + ' MB') -ForegroundColor DarkGray
}

Write-Host ''
Write-Host '  Press any key to close...' -ForegroundColor DarkGray
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
