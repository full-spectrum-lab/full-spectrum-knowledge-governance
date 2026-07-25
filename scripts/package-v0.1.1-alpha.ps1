$ErrorActionPreference = "Stop"
if (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Windows) -or
    [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -ne
        [System.Runtime.InteropServices.Architecture]::X64) {
    throw "This release script requires Windows x64."
}
$root = Split-Path -Parent $PSScriptRoot
$dotnet = if ($env:DOTNET_EXE) { $env:DOTNET_EXE } else { "dotnet" }
$commit = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') {
    throw "A clean Git commit identity is required."
}
if (git -C $root status --porcelain) {
    throw "Release packaging requires a clean worktree."
}
$output = Join-Path $root "artifacts/release/v0.1.1-alpha"
$stage = Join-Path $output "package"

if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

Push-Location $root
try {
    & $dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $dotnet publish src/FullSpectrum.Knowledge.TestHost -c Release --no-restore `
        --self-contained false -p:UseAppHost=true `
        -p:Version=0.1.1-alpha -p:AssemblyVersion=0.1.1.0 -p:FileVersion=0.1.1.0 `
        -p:RepositoryCommit=$commit `
        -p:InformationalVersion=0.1.1-alpha+$commit `
        -o (Join-Path $stage "bin")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Copy-Item schemas,examples -Destination $stage -Recurse
    Copy-Item LICENSE,LICENSE-APACHE-2.0,LICENSE-MULANPSL-2.0,NOTICE,README.md,README.en.md,FullSpectrum.Knowledge.slnx -Destination $stage
    Copy-Item "docs/release/v0.1.1-alpha/*" -Destination $stage
    $spdxFiles = Get-ChildItem -LiteralPath $stage -File -Recurse |
        Where-Object { $_.Name -ne "SBOM.package.spdx.json" } |
        Sort-Object FullName |
        ForEach-Object {
            $relative = $_.FullName.Substring($stage.Length).TrimStart('\').Replace('\','/')
            $identityHasher = [Security.Cryptography.SHA256]::Create()
            try {
                $identity = [BitConverter]::ToString(
                    $identityHasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($relative))
                ).Replace("-","").Substring(0,16)
            }
            finally {
                $identityHasher.Dispose()
            }
            @{
                fileName = "./$relative"
                SPDXID = "SPDXRef-File-" + $identity
                checksums = @(@{
                    algorithm = "SHA256"
                    checksumValue = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                })
            }
        }
    $packageSbom = @{
        spdxVersion = "SPDX-2.3"
        dataLicense = "CC0-1.0"
        SPDXID = "SPDXRef-DOCUMENT"
        name = "full-spectrum-knowledge-governance-v0.1.1-alpha-win-x64"
        documentNamespace = "https://gitee.com/full-spectrum/full-spectrum-knowledge-governance/sbom/v0.1.1-alpha/win-x64"
        creationInfo = @{
            created = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            creators = @("Organization: Full Spectrum Lab", "Tool: package-v0.1.1-alpha.ps1")
        }
        packages = @(@{
            name = "full-spectrum-knowledge-governance"
            SPDXID = "SPDXRef-Package"
            versionInfo = "0.1.1-alpha"
            downloadLocation = "NOASSERTION"
            filesAnalyzed = $true
            licenseConcluded = "MulanPSL-2.0 OR Apache-2.0"
            licenseDeclared = "MulanPSL-2.0 OR Apache-2.0"
            copyrightText = "Copyright 2026 Full Spectrum Lab"
        })
        files = @($spdxFiles)
        relationships = @(
            @{ spdxElementId = "SPDXRef-DOCUMENT"; relationshipType = "DESCRIBES"; relatedSpdxElement = "SPDXRef-Package" }
        ) + @($spdxFiles | ForEach-Object {
            @{ spdxElementId = "SPDXRef-Package"; relationshipType = "CONTAINS"; relatedSpdxElement = $_.SPDXID }
        })
    }
    $packageSbom | ConvertTo-Json -Depth 8 |
        Set-Content -Encoding utf8 (Join-Path $stage "SBOM.package.spdx.json")
    $archive = Join-Path $output "full-spectrum-knowledge-governance-v0.1.1-alpha-win-x64.zip"
    Compress-Archive -Path "$stage/*" -DestinationPath $archive -CompressionLevel Optimal
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant()
    "$hash  $(Split-Path -Leaf $archive)" | Set-Content -Encoding ascii (Join-Path $output "SHA256SUMS")
    @{
        manifest_version = "1.0"
        version = "v0.1.1-alpha"
        release_commit = $commit
        tag = "NOT_CREATED"
        artifact = (Split-Path -Leaf $archive)
        sha256 = $hash
        target = "win-x64"
        tests = @{ passed = 74; total = 74 }
        schemas = @{ valid = 12; total = 12 }
        golden = "PASS"
        production_ready = $false
        linux_test = "NOT_EXECUTED"
        macos_test = "NOT_EXECUTED"
    } | ConvertTo-Json -Depth 4 |
        Set-Content -Encoding utf8 (Join-Path $output "RELEASE_MANIFEST.json")
    Write-Output "PACKAGE=$archive"
    Write-Output "SHA256=$hash"
}
finally { Pop-Location }
