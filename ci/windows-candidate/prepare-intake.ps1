# Created/Updated: 2026-10-11T16:48:00+08:00; Timezone: Asia/Shanghai
# ID: KG-CI-INTAKE-20261011; Version: v0.1-preparation
# Status: LOCAL_PREPARATION_NOT_EXECUTED; Owner: Instance1
# Scope: Fixed existing ZIP/Manifest import only; Baseline: QPP b6e27e / KG 1295c27c
$ErrorActionPreference='Stop'
$sourceCommit='b6e27e656552e27c848000e695b2cb0b8d16ff6b'
$repo='https://gitee.com/full-spectrum/qpp.wiki.git'
$prefix='21_AI协作/03_Codex实例3/evidence/kg-af4-af5-20261011/instance1-kg-artifact-af5-20261011/inputs'
if($env:GITHUB_REPOSITORY -cne 'full-spectrum-lab/full-spectrum-knowledge-governance'){throw 'Wrong intake repository.'}
if(!$env:KG_INTAKE_ROOT -or !$env:RUNNER_TEMP){throw 'Explicit intake root required.'}
$expectedRoot=Join-Path $env:RUNNER_TEMP "kg-intake-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT"
if([IO.Path]::GetFullPath($env:KG_INTAKE_ROOT) -cne [IO.Path]::GetFullPath($expectedRoot) -or (Test-Path $env:KG_INTAKE_ROOT)){throw 'Fresh bounded intake root required.'}
$root=$env:KG_INTAKE_ROOT
$inputRoot=Join-Path $root 'input';$evidenceRoot=Join-Path $root 'evidence';$objectRoot=Join-Path $root 'qpp-objects'
New-Item -ItemType Directory -Path $inputRoot,$evidenceRoot | Out-Null
@{scope='TRANSFER_EXISTING_BYTES_ONLY';source_repo=$repo;source_commit=$sourceCommit;harness_commit=$env:GITHUB_SHA;run_id=$env:GITHUB_RUN_ID;attempt=$env:GITHUB_RUN_ATTEMPT;image_os=$env:ImageOS;image_version=$env:ImageVersion;candidate_execution='NOT_EXECUTED'} | ConvertTo-Json | Set-Content (Join-Path $evidenceRoot 'intake-environment.json') -Encoding utf8
& git init --bare $objectRoot *> (Join-Path $evidenceRoot 'init.log')
if($LASTEXITCODE -ne 0){throw 'Object repository init failed.'}
& git -C $objectRoot -c core.autocrlf=false fetch --depth=1 $repo $sourceCommit *> (Join-Path $evidenceRoot 'fetch.log')
if($LASTEXITCODE -ne 0 -or (& git -C $objectRoot rev-parse FETCH_HEAD) -cne $sourceCommit){throw 'Fixed remote QPP fetch mismatch; no fallback.'}
$inputs=@(
    @{name='full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip';blob='52332f2c333b262b6dcde49233d4eaa2beff387f';sha256='DB3FCA151F04CE016AC3E41C7E68A2B0B05ED76D69CB6248A9250AAF06C8543C'},
    @{name='RELEASE_MANIFEST.json';blob='1946ec077558400b933dce9b76d52435f5b1ca68';sha256='48AF44E7E6C7035C62D421F9D22FC89F7A019D89B79FA998ED81F5283AA05B66'}
)
$records=[Collections.Generic.List[object]]::new()
foreach($item in $inputs) {
    $spec="$sourceCommit`:$prefix/$($item.name)"
    $blob=& git -C $objectRoot rev-parse $spec
    if($LASTEXITCODE -ne 0 -or $blob -cne $item.blob){throw 'Approved source blob mismatch.'}
    $size=& git -C $objectRoot cat-file -s $blob
    if($LASTEXITCODE -ne 0 -or $size -notmatch '^[0-9]+$' -or [long]$size -gt 268435456){throw 'Unexpected source blob size.'}
    $psi=[Diagnostics.ProcessStartInfo]::new((Get-Command git).Source)
    foreach($arg in @('-C',$objectRoot,'cat-file','blob',$blob)){$psi.ArgumentList.Add($arg)}
    $psi.UseShellExecute=$false;$psi.RedirectStandardOutput=$true
    $p=[Diagnostics.Process]::Start($psi)
    $dest=Join-Path $inputRoot $item.name
    $stream=[IO.File]::Open($dest,[IO.FileMode]::CreateNew)
    try {$p.StandardOutput.BaseStream.CopyTo($stream)} finally {$stream.Dispose()}
    $p.WaitForExit()
    if($p.ExitCode -ne 0 -or (Get-FileHash -LiteralPath $dest).Hash -cne $item.sha256){throw 'Source bytes/hash mismatch; do not upload candidate.'}
    $records.Add(@{name=$item.name;git_blob=$blob;raw_sha256=$item.sha256;bytes=(Get-Item -LiteralPath $dest).Length})
}
ConvertTo-Json -InputObject @($records.ToArray()) | Set-Content (Join-Path $evidenceRoot 'intake-inputs.json') -Encoding utf8
@{status='PASS_TRANSFER_IDENTITY_ONLY';candidate_execution='NOT_EXECUTED';source_commit=$sourceCommit;harness_commit=$env:GITHUB_SHA} | ConvertTo-Json | Set-Content (Join-Path $evidenceRoot 'intake-status.json') -Encoding utf8

