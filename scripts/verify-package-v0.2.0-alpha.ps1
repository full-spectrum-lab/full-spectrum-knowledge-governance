param(
    [Parameter(Mandatory = $true)][string]$Package,
    [Parameter(Mandatory = $true)][string]$ReleaseManifest,
    [Parameter(Mandatory = $true)][string]$ExpectedSha256,
    [Parameter(Mandatory = $true)][string]$AuditRoot
)

$ErrorActionPreference = "Stop"
$dotnet = if ($env:DOTNET_EXE) { $env:DOTNET_EXE } else { "dotnet" }
$dotnetCommand = Get-Command $dotnet -ErrorAction Stop
if (-not $env:DOTNET_ROOT) { $env:DOTNET_ROOT = Split-Path -Parent $dotnetCommand.Source }
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$releaseManifestPath = (Resolve-Path -LiteralPath $ReleaseManifest).Path
$auditPath = [IO.Path]::GetFullPath($AuditRoot)
if (Test-Path -LiteralPath $auditPath) {
    throw "AuditRoot must not already exist: $auditPath"
}
New-Item -ItemType Directory -Path $auditPath | Out-Null
$install = Join-Path $auditPath "install-v0.2.0-alpha"
Expand-Archive -LiteralPath $packagePath -DestinationPath $install

$actualArchiveHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualArchiveHash -ne $ExpectedSha256.ToLowerInvariant()) {
    throw "Archive SHA-256 mismatch."
}
$release = Get-Content -LiteralPath $releaseManifestPath -Raw | ConvertFrom-Json
$manifest = Get-Content -LiteralPath (Join-Path $install "PACKAGE_MANIFEST.json") -Raw | ConvertFrom-Json
if ($release.sha256 -ne $actualArchiveHash -or $release.version -ne "v0.2.0-alpha") {
    throw "Release manifest identity mismatch."
}
if ($manifest.version -ne $release.version -or $manifest.release_commit -ne $release.release_commit -or
    $manifest.production_ready -ne $false) { throw "Package and release manifest mismatch." }

$hashFailures = @()
$sumLines = Get-Content -LiteralPath (Join-Path $install "SHA256SUMS")
foreach ($line in $sumLines) {
    $parts = $line -split '  ', 2
    if ($parts.Count -ne 2 -or -not $parts[1].StartsWith("./")) {
        $hashFailures += "malformed:$line"
        continue
    }
    $relative = $parts[1].Substring(2)
    $target = [IO.Path]::GetFullPath((Join-Path $install $relative))
    if (-not $target.StartsWith($install + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        $hashFailures += "path_escape:$relative"
        continue
    }
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
        $hashFailures += "missing:$relative"
        continue
    }
    $actual = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $parts[0]) { $hashFailures += "hash:$relative" }
}
if ($hashFailures.Count -ne 0) {
    throw "Package file verification failed: $($hashFailures -join ', ')"
}

$cli = Join-Path $install "bin/FullSpectrum.Knowledge.TestHost.exe"
Push-Location $install
try {
    $versionOutput = & $cli version 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Package version command failed." }
    $goldenOutput = & $cli verify-k0-05 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Package K0-05 Golden verification failed." }
}
finally { Pop-Location }
if (($versionOutput -join "`n") -notmatch [regex]::Escape("VERSION=0.2.0-alpha+$($manifest.release_commit)")) {
    throw "Embedded version does not match the manifest commit."
}

$consumer = Join-Path $auditPath "consumer"
New-Item -ItemType Directory -Path $consumer | Out-Null
$library = (Join-Path $install "library").Replace('\','/')
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="FullSpectrum.Knowledge.Contracts"><HintPath>$library/FullSpectrum.Knowledge.Contracts.dll</HintPath></Reference>
    <Reference Include="FullSpectrum.Knowledge.Fixed"><HintPath>$library/FullSpectrum.Knowledge.Fixed.dll</HintPath></Reference>
    <Reference Include="FullSpectrum.Knowledge.Library"><HintPath>$library/FullSpectrum.Knowledge.Library.dll</HintPath></Reference>
    <Reference Include="FullSpectrum.Knowledge.Storage"><HintPath>$library/FullSpectrum.Knowledge.Storage.dll</HintPath></Reference>
    <Reference Include="FullSpectrum.Knowledge.Trace"><HintPath>$library/FullSpectrum.Knowledge.Trace.dll</HintPath></Reference>
  </ItemGroup>
</Project>
"@ | Set-Content -Encoding utf8 (Join-Path $consumer "PackageConsumer.csproj")
@'
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Library;
using FullSpectrum.Knowledge.Storage;

var root = Path.GetFullPath(args[0]);
Directory.CreateDirectory(root);
var db = Path.Combine(root, "metadata.sqlite3");
var artifacts = Path.Combine(root, "artifacts");
var snapshot = Path.Combine(root, "snapshot");
var id = new KnowledgeId("KG-PACKAGE-COMPAT");
var version = new KnowledgeVersion("0.1.0");
var reference = new KnowledgeReference(id, version);
var at = DateTimeOffset.Parse("2026-08-27T00:00:00Z");
var pack = new KnowledgePack(
    KnowledgeContractVersions.V1_0, id, version, KnowledgeLifecycleState.Draft,
    "Synthetic package compatibility fixture", "No real-world conclusion.",
    [new KnowledgeArtifact("ART-001", "application/json", 2,
        DigestRef.Sha256("44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a"),
        "content.synthetic.json")],
    new Dictionary<string, string> { ["fixture_status"] = "SYNTHETIC_ONLY" }, at);

// This uses the v0.1 public registry/storage shape, then reopens it through the v0.2 Library API.
using (var registry = new KnowledgeRegistry(db, artifacts))
{
    registry.Register(pack, [new ArtifactRegistration("ART-001", "{}"u8.ToArray())], "author", at);
    registry.SubmitReview(id, version, "reviewer", at.AddMinutes(1));
    registry.Release(id, version, "publisher", at.AddMinutes(2));
}
Directory.CreateDirectory(snapshot);
File.Copy(db, Path.Combine(snapshot, "metadata.sqlite3"));
CopyDirectory(artifacts, Path.Combine(snapshot, "artifacts"));

using (IKnowledgeLibrary library = new KnowledgeLibrary(db, artifacts))
{
    Require(library.Get(reference).ContractVersion == KnowledgeContractVersions.V1_0, "v0.1 reopen");
    Require(System.Text.Encoding.UTF8.GetString(library.ReadArtifact(reference, "ART-001")) == "{}", "artifact reopen");
    library.UpgradeContract(reference, KnowledgeContractVersions.V1_1, "upgrader", at.AddMinutes(3));
    Require(library.Get(reference).ContractVersion == KnowledgeContractVersions.V1_1, "upgrade");
}

File.Delete(db);
Directory.Delete(artifacts, true);
File.Copy(Path.Combine(snapshot, "metadata.sqlite3"), db);
CopyDirectory(Path.Combine(snapshot, "artifacts"), artifacts);
using (IKnowledgeLibrary rolledBack = new KnowledgeLibrary(db, artifacts))
{
    Require(rolledBack.Get(reference).ContractVersion == KnowledgeContractVersions.V1_0, "rollback");
}
Console.WriteLine("LIBRARY_LOAD=PASS");
Console.WriteLine("V01_STORAGE_REOPEN=PASS");
Console.WriteLine("CONTRACT_UPGRADE=PASS");
Console.WriteLine("SNAPSHOT_ROLLBACK=PASS");

static void Require(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"Failed: {name}");
}
static void CopyDirectory(string source, string destination)
{
    Directory.CreateDirectory(destination);
    foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
    foreach (var child in Directory.GetDirectories(source)) CopyDirectory(child, Path.Combine(destination, Path.GetFileName(child)));
}
'@ | Set-Content -Encoding utf8 (Join-Path $consumer "Program.cs")

& $dotnet run --project (Join-Path $consumer "PackageConsumer.csproj") --configuration Release -- (Join-Path $auditPath "compat-data") 2>&1 |
    Tee-Object -Variable consumerOutput
if ($LASTEXITCODE -ne 0) { throw "Package Library consumer test failed." }

$removalProbe = Join-Path $auditPath "removal-probe"
Copy-Item -LiteralPath $install -Destination $removalProbe -Recurse
if (-not $removalProbe.StartsWith($auditPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Removal probe escaped AuditRoot."
}
Remove-Item -LiteralPath $removalProbe -Recurse -Force
if (Test-Path -LiteralPath $removalProbe) { throw "Package removal probe failed." }

$result = [ordered]@{
    status = "PASS"
    archive_sha256 = $actualArchiveHash
    release_commit = $manifest.release_commit
    package_file_count = $sumLines.Count
    package_hash_failures = @($hashFailures)
    version_output = @($versionOutput)
    golden_output = @($goldenOutput)
    consumer_output = @($consumerOutput)
    removal = "PASS"
    standard_json_schema_validator = "NOT_EXECUTED"
    production_ready = $false
}
$result | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 (Join-Path $auditPath "package-verification.json")
$result | ConvertTo-Json -Depth 8
