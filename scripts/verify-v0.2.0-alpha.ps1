$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dotnet = if ($env:DOTNET_EXE) { $env:DOTNET_EXE } else { "dotnet" }

Push-Location $root
try {
    & $dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & $dotnet build FullSpectrum.Knowledge.slnx --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & $dotnet run --project tests/FullSpectrum.Knowledge.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & $dotnet run --project src/FullSpectrum.Knowledge.TestHost --configuration Release --no-build -- verify-k0-05
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Output "KG_V0_1_REGRESSION=PASS"
    Write-Output "KG_V0_2_LIFECYCLE=PASS"
    Write-Output "KG_V0_2_LIBRARY_API=PASS"
    Write-Output "KG_V0_2_ADAPTER_SPI=PASS"
    Write-Output "KG_V0_2_COMPATIBILITY=PASS"
    Write-Output "KG_V0_2_INTERNAL_ENGINEERING=PASS_PENDING_INDEPENDENT_REVERIFICATION"
    exit 0
}
finally {
    Pop-Location
}
