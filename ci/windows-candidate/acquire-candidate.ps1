# Created/Updated: 2026-10-11T16:18:00+08:00; Timezone: Asia/Shanghai
# ID: KG-WIN-CI-ACQUIRE-20261011; Version: v0.1-draft; Status: LOCAL_DRAFT_NOT_EXECUTED
# Owner: Instance1; Scope: authenticated existing artifact acquisition only
# Baseline: 1295c27cd3409cb5395c04cd29378cb2610ab2ed / DB3FCA15...06C8543C
$ErrorActionPreference='Stop'
$repo='full-spectrum-lab/full-spectrum-knowledge-governance'
if ($env:GITHUB_REPOSITORY -cne $repo) {throw 'Wrong repository.'}
foreach ($id in @($env:INTAKE_RUN_ID,$env:INTAKE_ARTIFACT_ID)) {
    if ($id -notmatch '^[1-9][0-9]*$') {throw 'Intake numeric identity required.'}
}
if ($env:INTAKE_TRANSPORT_SHA256 -notmatch '^[0-9a-fA-F]{64}$') {throw 'Transport hash approval required.'}
if (!$env:KG_ROOT -or (Test-Path -LiteralPath $env:KG_ROOT)) {throw 'Fresh explicit root required.'}
New-Item -ItemType Directory -Path $env:KG_ROOT | Out-Null
$evidence=Join-Path $env:KG_ROOT 'evidence'
New-Item -ItemType Directory -Path $evidence | Out-Null
$artifact=& gh api "repos/$repo/actions/artifacts/$env:INTAKE_ARTIFACT_ID"
if ($LASTEXITCODE -ne 0) {throw 'Artifact metadata query failed.'}
$meta=$artifact | ConvertFrom-Json
$artifact | Set-Content (Join-Path $evidence 'intake-artifact.json') -Encoding utf8
if ($meta.expired -or [string]$meta.id -cne $env:INTAKE_ARTIFACT_ID -or
    [string]$meta.workflow_run.id -cne $env:INTAKE_RUN_ID) {throw 'Artifact / Run mismatch or expiration.'}
$run=& gh api "repos/$repo/actions/runs/$env:INTAKE_RUN_ID"
if ($LASTEXITCODE -ne 0) {throw 'Intake Run query failed.'}
$run | Set-Content (Join-Path $evidence 'intake-run.json') -Encoding utf8
$runData=$run | ConvertFrom-Json
if ($runData.status -cne 'completed' -or $runData.conclusion -cne 'success' -or
    $runData.repository.full_name -cne $repo) {throw 'Intake Run not approved successful origin.'}
# gh writes binary response to stdout; copy its stream, do not decode as text.
$transport=Join-Path $env:KG_ROOT 'transport.zip'
$psi=[Diagnostics.ProcessStartInfo]::new((Get-Command gh).Source)
$psi.ArgumentList.Add('api');$psi.ArgumentList.Add("repos/$repo/actions/artifacts/$env:INTAKE_ARTIFACT_ID/zip")
$psi.UseShellExecute=$false;$psi.RedirectStandardOutput=$true
$p=[Diagnostics.Process]::Start($psi)
$stream=[IO.File]::Open($transport,[IO.FileMode]::CreateNew)
try {$p.StandardOutput.BaseStream.CopyTo($stream)} finally {$stream.Dispose()}
$p.WaitForExit()
if ($p.ExitCode -ne 0 -or (Get-FileHash $transport).Hash -ne $env:INTAKE_TRANSPORT_SHA256) {throw 'Transport download/hash failed.'}
$name='full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip'
$inputRoot=Join-Path $env:KG_ROOT 'input'
New-Item -ItemType Directory -Path $inputRoot | Out-Null
$archive=[IO.Compression.ZipFile]::OpenRead($transport)
try {
    $names=@($archive.Entries | ForEach-Object FullName)
    if ($names.Count -ne 2 -or @($names | Sort-Object -Unique).Count -ne 2 -or
        $name -cnotin $names -or 'RELEASE_MANIFEST.json' -cnotin $names) {throw 'Intake must contain exactly ZIP and original external manifest at root.'}
    foreach ($entry in $archive.Entries) {
        if ($entry.Length -gt 268435456) {throw 'Unexpected intake file size.'}
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,(Join-Path $inputRoot $entry.FullName),$false)
    }
} finally {$archive.Dispose()}
$expected='DB3FCA151F04CE016AC3E41C7E68A2B0B05ED76D69CB6248A9250AAF06C8543C'
if ((Get-FileHash (Join-Path $inputRoot $name)).Hash -ne $expected) {throw 'Existing candidate identity mismatch.'}
if ((Get-FileHash (Join-Path $inputRoot 'RELEASE_MANIFEST.json')).Hash -ne '48AF44E7E6C7035C62D421F9D22FC89F7A019D89B79FA998ED81F5283AA05B66') {throw 'External manifest raw identity mismatch.'}
@{zip_sha256=$expected;transport_sha256=$env:INTAKE_TRANSPORT_SHA256;source_run=$env:INTAKE_RUN_ID;source_artifact=$env:INTAKE_ARTIFACT_ID;gate='PASS_HASH_BEFORE_PACKAGE_EXPANSION'} | ConvertTo-Json | Set-Content (Join-Path $evidence 'acquisition.json') -Encoding utf8
