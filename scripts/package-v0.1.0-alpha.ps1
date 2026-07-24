$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dotnet = if ($env:DOTNET_EXE) { $env:DOTNET_EXE } else { "dotnet" }
$output = Join-Path $root "artifacts/release/v0.1.0-alpha"
$stage = Join-Path $output "package"

if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

Push-Location $root
try {
    & $dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $dotnet publish src/FullSpectrum.Knowledge.TestHost -c Release --no-restore -o (Join-Path $stage "bin")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Copy-Item schemas,examples -Destination $stage -Recurse
    Copy-Item LICENSE,NOTICE,README.md,README.en.md -Destination $stage
    Copy-Item "docs/release/v0.1.0-alpha/*" -Destination $stage
    $archive = Join-Path $output "full-spectrum-knowledge-governance-v0.1.0-alpha-win-x64.zip"
    Compress-Archive -Path "$stage/*" -DestinationPath $archive -CompressionLevel Optimal
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant()
    "$hash  $(Split-Path -Leaf $archive)" | Set-Content -Encoding ascii (Join-Path $output "SHA256SUMS")
    Write-Output "PACKAGE=$archive"
    Write-Output "SHA256=$hash"
}
finally { Pop-Location }
