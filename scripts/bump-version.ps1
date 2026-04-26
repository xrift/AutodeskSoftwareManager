#Requires -Version 5.1
param(
    [ValidateSet('Major','Minor','Patch','Build')]
    [string]$Part = 'Build',

    [string]$Set = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root    = Split-Path $PSScriptRoot -Parent
$csproj  = Join-Path $root 'AutodeskSoftwareManager.csproj'
$content = Get-Content $csproj -Raw

if ($content -notmatch '<AssemblyVersion>(\d+)\.(\d+)\.(\d+)\.(\d+)<\/AssemblyVersion>') {
    Write-Host 'Could not find AssemblyVersion in csproj.' -ForegroundColor Red
    exit 1
}
$major = [int]$Matches[1]; $minor = [int]$Matches[2]
$patch = [int]$Matches[3]; $build = [int]$Matches[4]
$old   = "$major.$minor.$patch.$build"

if ($Set) {
    $parts = $Set -split '\.'
    if ($parts.Count -ne 4) { Write-Host 'Version must be in x.x.x.x format.' -ForegroundColor Red; exit 1 }
    $major = [int]$parts[0]; $minor = [int]$parts[1]
    $patch = [int]$parts[2]; $build = [int]$parts[3]
} else {
    switch ($Part) {
        'Major' { $major++; $minor = 0; $patch = 0; $build = 0 }
        'Minor' { $minor++; $patch = 0; $build = 0 }
        'Patch' { $patch++; $build = 0 }
        'Build' { $build++ }
    }
}

$new    = "$major.$minor.$patch.$build"
$semver = "$major.$minor.$patch"

$content = $content -replace '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$new</AssemblyVersion>"
$content = $content -replace '<FileVersion>[^<]+</FileVersion>',         "<FileVersion>$new</FileVersion>"
$content = $content -replace '<Version>[^<]+</Version>',                 "<Version>$semver</Version>"

Set-Content $csproj $content -Encoding UTF8 -NoNewline

Write-Host ''
Write-Host ('  Version bumped: ' + $old + '  ->  ' + $new) -ForegroundColor Green
Write-Host ''

Push-Location $root
try {
    $status = git status --porcelain 2>$null
    if ($status) {
        git add $csproj 2>$null
        git commit -m ('chore: bump version to ' + $new) 2>$null
        Write-Host ('  Git commit created for v' + $new) -ForegroundColor DarkGray
    }
} catch { }
Pop-Location
