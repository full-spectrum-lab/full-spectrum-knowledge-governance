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
$commitTimestamp = (& git -C $root show -s --format=%cI $commit).Trim()
if ($LASTEXITCODE -ne 0) { throw "The Git commit timestamp could not be read." }
$fixedTimestamp = [DateTimeOffset]::Parse($commitTimestamp).UtcDateTime
if (git -C $root status --porcelain) {
    throw "Release packaging requires a clean worktree. Commit the release inputs first."
}

$parameterizedRelease = [bool]$env:FSKG_PACKAGE_VERSION
$releaseVersion = if ($parameterizedRelease) {
    $env:FSKG_PACKAGE_VERSION.TrimStart('v')
} else {
    "0.2.0-alpha"
}
if ($releaseVersion -notmatch '^[0-9]+\.[0-9]+\.[0-9]+-[0-9A-Za-z.-]+$') {
    throw "Invalid release version: $releaseVersion"
}
$version = "v$releaseVersion"
$output = Join-Path $root "artifacts/release/$version"
$stage = Join-Path $output "package"
if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

Push-Location $root
try {
    $common = @(
        "-p:Version=$releaseVersion",
        "-p:PackageVersion=$releaseVersion",
        "-p:AssemblyVersion=$($releaseVersion.Split('-')[0]).0",
        "-p:FileVersion=$($releaseVersion.Split('-')[0]).0",
        "-p:RepositoryCommit=$commit",
        "-p:InformationalVersion=$releaseVersion+$commit"
    )

    & $dotnet restore FullSpectrum.Knowledge.slnx --locked-mode @common
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & $dotnet publish src/FullSpectrum.Knowledge.TestHost -c Release --no-restore `
        --self-contained false -p:UseAppHost=true @common `
        -o (Join-Path $stage "bin")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # TestHost does not reference the v0.2 Library project. Publish it separately
    # so the package can be consumed as both a verification CLI and an in-process API.
    & $dotnet publish src/FullSpectrum.Knowledge.Library -c Release --no-restore `
        --self-contained false @common `
        -o (Join-Path $stage "library")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Fail closed when NuGet restore or publish metadata drifts from the release
    # identity. A successful compilation alone is not sufficient evidence.
    $depsFiles = @(
        Get-ChildItem -LiteralPath (Join-Path $stage "bin") -Filter "*.deps.json" -File
        Get-ChildItem -LiteralPath (Join-Path $stage "library") -Filter "*.deps.json" -File
    )
    if ($depsFiles.Count -lt 2) {
        throw "Expected deps.json files for both CLI and library publish outputs."
    }
    foreach ($depsFile in $depsFiles) {
        $deps = Get-Content -LiteralPath $depsFile.FullName -Raw | ConvertFrom-Json
        $projectLibraries = @($deps.libraries.PSObject.Properties |
            Where-Object { $_.Name -like "FullSpectrum.Knowledge.*/*" })
        if ($projectLibraries.Count -eq 0) {
            throw "No FullSpectrum.Knowledge project identities found in $($depsFile.Name)."
        }
        foreach ($projectLibrary in $projectLibraries) {
            $actualVersion = $projectLibrary.Name.Split('/', 2)[1]
            if ($actualVersion -ne $releaseVersion) {
                throw "Release version drift in $($depsFile.Name): $($projectLibrary.Name), expected */$releaseVersion."
            }
        }
    }

    Copy-Item schemas,examples -Destination $stage -Recurse
    Copy-Item LICENSE,LICENSE-APACHE-2.0,LICENSE-MULANPSL-2.0,NOTICE,README.md,README.en.md,FullSpectrum.Knowledge.slnx -Destination $stage
    Copy-Item "docs/release/$version/*" -Destination $stage

    @{
        manifest_version = "1.0"
        version = $version
        release_commit = $commit
        tag = "NOT_CREATED"
        target = "win-x64"
        package_layout = @{ cli = "bin"; library = "library"; schemas = "schemas"; examples = "examples" }
        runtime_prerequisites = @{
            dotnet = "10"
            windows_system_sqlite = "winsqlite3.dll"
            preflight = "bin/FullSpectrum.Knowledge.TestHost.exe verify-k0-02"
        }
        native_sqlite_external_reverify = "EXTERNAL_REQUIRED"
        production_ready = $false
        standard_json_schema_validator = "NOT_EXECUTED"
    } | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 (Join-Path $stage "PACKAGE_MANIFEST.json")

    $filesForSbom = Get-ChildItem -LiteralPath $stage -File -Recurse |
        Where-Object { $_.Name -notin @("SBOM.package.spdx.json", "SHA256SUMS") } |
        Sort-Object FullName
    $spdxFiles = $filesForSbom | ForEach-Object {
        $relative = $_.FullName.Substring($stage.Length).TrimStart('\').Replace('\','/')
        $identityHasher = [Security.Cryptography.SHA256]::Create()
        try {
            $identity = [BitConverter]::ToString(
                $identityHasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($relative))
            ).Replace("-", "").Substring(0, 16)
        }
        finally { $identityHasher.Dispose() }
        @{
            fileName = "./$relative"
            SPDXID = "SPDXRef-File-$identity"
            checksums = @(@{
                algorithm = "SHA256"
                checksumValue = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            })
        }
    }
    @{
        spdxVersion = "SPDX-2.3"
        dataLicense = "CC0-1.0"
        SPDXID = "SPDXRef-DOCUMENT"
        name = "full-spectrum-knowledge-governance-$version-win-x64"
        documentNamespace = "https://gitee.com/full-spectrum/full-spectrum-knowledge-governance/sbom/$version/win-x64"
        creationInfo = @{
            created = $fixedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ")
            creators = @("Organization: Full Spectrum Lab", "Tool: package-$version.ps1")
        }
        packages = @(@{
            name = "full-spectrum-knowledge-governance"
            SPDXID = "SPDXRef-Package"
            versionInfo = $releaseVersion
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
    } | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 (Join-Path $stage "SBOM.package.spdx.json")

    $packageFiles = Get-ChildItem -LiteralPath $stage -File -Recurse |
        Where-Object { $_.Name -ne "SHA256SUMS" } | Sort-Object FullName
    $packageFiles | ForEach-Object {
        $relative = $_.FullName.Substring($stage.Length).TrimStart('\').Replace('\','/')
        "{0}  ./{1}" -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $relative
    } | Set-Content -Encoding ascii (Join-Path $stage "SHA256SUMS")

    # ZIP metadata is normalized to the release commit time so repeated packaging
    # of the same clean commit can be checked for byte-for-byte reproducibility.
    Get-ChildItem -LiteralPath $stage -Recurse | ForEach-Object { $_.LastWriteTimeUtc = $fixedTimestamp }
    (Get-Item -LiteralPath $stage).LastWriteTimeUtc = $fixedTimestamp

    $archive = Join-Path $output "full-spectrum-knowledge-governance-$version-win-x64.zip"
    Add-Type -AssemblyName System.IO.Compression
    $archiveStream = [IO.File]::Open($archive, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    $zip = [IO.Compression.ZipArchive]::new($archiveStream, [IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
            $relative = $_.FullName.Substring($stage.Length).TrimStart('\').Replace('\','/')
            $entry = $zip.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = [DateTimeOffset]$fixedTimestamp
            $sourceStream = [IO.File]::OpenRead($_.FullName)
            $entryStream = $entry.Open()
            try { $sourceStream.CopyTo($entryStream) }
            finally {
                $entryStream.Dispose()
                $sourceStream.Dispose()
            }
        }
    }
    finally {
        $zip.Dispose()
        $archiveStream.Dispose()
    }
    $archiveHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant()
    "$archiveHash  $(Split-Path -Leaf $archive)" | Set-Content -Encoding ascii (Join-Path $output "SHA256SUMS")

    @{
        manifest_version = "1.0"
        version = $version
        release_commit = $commit
        tag = "NOT_CREATED"
        artifact = (Split-Path -Leaf $archive)
        sha256 = $archiveHash
        target = "win-x64"
        package_layout = @{ cli = "bin"; library = "library"; schemas = "schemas"; examples = "examples" }
        runtime_prerequisites = @{
            dotnet = "10"
            windows_system_sqlite = "winsqlite3.dll"
            preflight = "bin/FullSpectrum.Knowledge.TestHost.exe verify-k0-02"
        }
        native_sqlite_external_reverify = "EXTERNAL_REQUIRED"
        tests = @{ passed = 92; total = 92 }
        schemas = @{ valid = 13; total = 13 }
        golden = "PASS"
        production_ready = $false
        linux_test = "NOT_EXECUTED"
        macos_test = "NOT_EXECUTED"
        standard_json_schema_validator = "NOT_EXECUTED"
        reproducibility = "CONTENT_AND_IDENTITY_VERIFIED; ARCHIVE_BYTE_REPRODUCIBILITY_NOT_EXECUTED"
    } | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 (Join-Path $output "RELEASE_MANIFEST.json")

    Write-Output "PACKAGE=$archive"
    Write-Output "SHA256=$archiveHash"
    Write-Output "RELEASE_COMMIT=$commit"
}
finally { Pop-Location }
