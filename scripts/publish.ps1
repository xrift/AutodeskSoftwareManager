#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes a self-contained single-file release build.
.PARAMETER Version
    Override the version from the csproj (e.g. 0.2.0.0).
.EXAMPLE
    .\scripts\publish.ps1
    .\scripts\publish.ps1 -Version 0.2.0.0
#>
param(
    [string]$Version = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root   = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $root 'AutodeskSoftwareManager.csproj'

# Resolve version
[xml]$proj = Get-Content $csproj
$fileVer   = $proj.Project.PropertyGroup.AssemblyVersion
if (-not $Version) { $Version = $fileVer }

$outDir = Join-Path $root "publish\v$Version"

Write-Host ""
Write-Host "  Publishing AutodeskSoftwareManager v$Version" -ForegroundColor Cyan
Write-Host "  Output: $outDir" -ForegroundColor DarkGray
Write-Host ""

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

$sw = [System.Diagnostics.Stopwatch]::StartNew()
dotnet publish $csproj -c Release -o $outDir --nologo
$sw.Stop()

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n  PUBLISH FAILED" -ForegroundColor Red
    exit 1
}

$exe = Get-ChildItem $outDir -Filter 'AutodeskSoftwareManager.exe' | Select-Object -First 1
$mb  = [math]::Round($exe.Length / 1MB, 1)

Write-Host ""
Write-Host "  Publish succeeded in $($sw.Elapsed.TotalSeconds.ToString('0.0'))s" -ForegroundColor Green
Write-Host "  Executable : $($exe.FullName)"   -ForegroundColor White
Write-Host "  Size       : $mb MB"              -ForegroundColor DarkGray
Write-Host ""
