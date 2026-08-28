$ErrorActionPreference = "Stop"

# v0.2.1 keeps the v0.2 packaging contract while fixing publish identity
# propagation through deps.json and the assembly metadata.
$previous = $env:FSKG_PACKAGE_VERSION
try {
    $env:FSKG_PACKAGE_VERSION = "0.2.1-alpha"
    & (Join-Path $PSScriptRoot "package-v0.2.0-alpha.ps1")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    if ($null -eq $previous) {
        Remove-Item Env:FSKG_PACKAGE_VERSION -ErrorAction SilentlyContinue
    } else {
        $env:FSKG_PACKAGE_VERSION = $previous
    }
}
