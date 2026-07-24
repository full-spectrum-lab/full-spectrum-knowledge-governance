param(
    [Parameter(Mandatory = $true)]
    [string]$DotNet
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $repo '.dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

& $DotNet restore (Join-Path $repo 'FullSpectrum.Knowledge.slnx') --locked-mode --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $DotNet build (Join-Path $repo 'FullSpectrum.Knowledge.slnx') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $DotNet run --project (Join-Path $repo 'tests\FullSpectrum.Knowledge.Tests\FullSpectrum.Knowledge.Tests.csproj') -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $DotNet run --project (Join-Path $repo 'src\FullSpectrum.Knowledge.TestHost\FullSpectrum.Knowledge.TestHost.csproj') -c Release --no-build -- verify
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $DotNet run --project (Join-Path $repo 'src\FullSpectrum.Knowledge.TestHost\FullSpectrum.Knowledge.TestHost.csproj') -c Release --no-build -- verify-k0-02
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $DotNet run --project (Join-Path $repo 'src\FullSpectrum.Knowledge.TestHost\FullSpectrum.Knowledge.TestHost.csproj') -c Release --no-build -- verify-k0-03
exit $LASTEXITCODE
