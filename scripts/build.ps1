#Requires -Version 5.1
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root   = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $root 'AutodeskSoftwareManager.csproj'

[xml]$proj = Get-Content $csproj
$version   = $proj.Project.PropertyGroup.AssemblyVersion

Write-Host ''
Write-Host ('  AutodeskSoftwareManager v' + $version + ' -- ' + $Configuration + ' build') -ForegroundColor Cyan
Write-Host ('  ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -ForegroundColor DarkGray
Write-Host ''

$sw = [System.Diagnostics.Stopwatch]::StartNew()
dotnet build $csproj -c $Configuration --nologo
$sw.Stop()

if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host '  BUILD FAILED' -ForegroundColor Red
} else {
    $outDir = Join-Path $root ('bin\' + $Configuration + '\net9.0-windows\win-x64')
    Write-Host ''
    Write-Host ('  Build succeeded in ' + $sw.Elapsed.TotalSeconds.ToString('0.0') + 's') -ForegroundColor Green
    Write-Host ('  Output: ' + $outDir) -ForegroundColor DarkGray
}

Write-Host ''
Write-Host '  Press any key to close...' -ForegroundColor DarkGray
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
