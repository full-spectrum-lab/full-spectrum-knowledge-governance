param(
    [Parameter(Mandatory = $true)][string]$Package,
    [Parameter(Mandatory = $true)][string]$ReleaseManifest,
    [Parameter(Mandatory = $true)][string]$ExpectedSha256,
    [Parameter(Mandatory = $true)][string]$AuditRoot
)

$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "verify-package-v0.2.0-alpha.ps1") `
    -Package $Package `
    -ReleaseManifest $ReleaseManifest `
    -ExpectedSha256 $ExpectedSha256 `
    -AuditRoot $AuditRoot `
    -ExpectedVersion "0.2.1-alpha"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
