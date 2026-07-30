<#
.SYNOPSIS
    Builds Monocle for every actively supported Dynamo version.

.DESCRIPTION
    Reads the version matrix from versions.json at the repo root and runs one build per
    active version. Release builds drop MonocleViewExtension.dll into deploy\<version>\,
    which is where the loader extension downloads it from.

    Dynamo 2.x is frozen: those DLLs are immutable artifacts and are never rebuilt.

.PARAMETER Version
    Build only this Dynamo version (e.g. 3.6). Omit to build all active versions.

.PARAMETER Configuration
    Debug or Release. Defaults to Release.

.EXAMPLE
    .\scripts\build-all.ps1
    .\scripts\build-all.ps1 -Version 3.6 -Configuration Debug
#>
[CmdletBinding()]
param(
    [string]$Version,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\Monocle\Monocle.csproj'
$matrix = Get-Content (Join-Path $repoRoot 'versions.json') -Raw | ConvertFrom-Json

$targets = if ($Version) {
    if ($matrix.active -notcontains $Version) {
        throw "Dynamo $Version is not an active version. Active: $($matrix.active -join ', ')"
    }
    @($Version)
}
else {
    $matrix.active
}

$failed = @()
foreach ($v in $targets) {
    Write-Host "==> Building Monocle for Dynamo $v ($Configuration)" -ForegroundColor Cyan
    dotnet build $project -c $Configuration -p:DynamoVersion=$v --nologo
    if ($LASTEXITCODE -ne 0) {
        $failed += $v
        Write-Host "!!! Dynamo $v FAILED" -ForegroundColor Red
    }
}

Write-Host ''
if ($failed.Count -gt 0) {
    Write-Host "Failed: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host "Built $($targets.Count) version(s) successfully: $($targets -join ', ')" -ForegroundColor Green
