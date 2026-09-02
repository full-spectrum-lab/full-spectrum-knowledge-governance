$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sdk = if ($env:DOTNET_ROOT) { Join-Path $env:DOTNET_ROOT "dotnet.exe" } else { "dotnet" }
& $sdk run --project (Join-Path $root "src/FullSpectrum.Knowledge.TestHost") -c Release --no-build -- verify-k2
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
