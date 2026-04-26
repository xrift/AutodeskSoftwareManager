#Requires -Version 5.1
<#
.SYNOPSIS
    Builds AutodeskSoftwareManager.
.PARAMETER Configuration
    Debug (default) or Release.
.EXAMPLE
    .\scripts\build.ps1
    .\scripts\build.ps1 -Configuration Release
#>
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root   = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $root 'AutodeskSoftwareManager.csproj'

# Read version from csproj
[xml]$proj = Get-Content $csproj
$version   = $proj.Project.PropertyGroup.AssemblyVersion

Write-Host ""
Write-Host "  AutodeskSoftwareManager v$version — $Configuration build" -ForegroundColor Cyan
Write-Host "  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor DarkGray
Write-Host ""

$sw = [System.Diagnostics.Stopwatch]::StartNew()
dotnet build $csproj -c $Configuration --nologo
$sw.Stop()

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n  BUILD FAILED" -ForegroundColor Red
    exit 1
}

$outDir = Join-Path $root "bin\$Configuration\net9.0-windows\win-x64"
$exe    = Join-Path $outDir 'AutodeskSoftwareManager.exe'

Write-Host ""
Write-Host "  Build succeeded in $($sw.Elapsed.TotalSeconds.ToString('0.0'))s" -ForegroundColor Green
Write-Host "  Output: $outDir" -ForegroundColor DarkGray
Write-Host ""
