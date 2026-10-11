$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$repo='C:\Users\wangjian0926\Desktop\codex专属仓库\worktrees\instance1-kg-packaging-p1-20261010'
$scripts=@('candidate-packaging-tools.ps1','package-v0.2.0-alpha.ps1','verify-package-v0.2.0-alpha.ps1','test-candidate-packaging-tools.ps1','test-candidate-artifact-identity.ps1')
$snapshots=Join-Path $root 'final-scripts'
if (Test-Path -LiteralPath $snapshots) {throw 'No overwrite of final snapshots.'}
New-Item -ItemType Directory -Path $snapshots | Out-Null
foreach ($file in $scripts) {Copy-Item -LiteralPath (Join-Path $repo "scripts/$file") -Destination (Join-Path $snapshots $file)}
$patch=& git -C $repo diff -- scripts/candidate-packaging-tools.ps1 scripts/package-v0.2.0-alpha.ps1 scripts/verify-package-v0.2.0-alpha.ps1 scripts/test-candidate-packaging-tools.ps1
if ($LASTEXITCODE -ne 0) {throw 'Diff failed.'}
[IO.File]::WriteAllText((Join-Path $root 'tracked-diff.patch'),($patch -join "`n")+"`n",[Text.UTF8Encoding]::new($false))
$paths=@()
foreach ($dir in @('identity-run-01','identity-run-02','identity-run-03','protection-run-01','diagnostic-inputs','final-scripts')) {
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root $dir) -File -Recurse) {$paths += $file.FullName}
}
foreach ($file in @('identity-run-03.log','protection-run-01.log','tracked-diff.patch','index-af1.ps1')) {$paths += Join-Path $root $file}
foreach ($file in $scripts) {$paths += Join-Path $repo "scripts/$file"}
$paths += Join-Path $repo 'docs/planning/V021_ARTIFACT_SERIALIZATION_MINIMAL_FIX_PLAN_20261011.md'
$paths += Join-Path $repo 'docs/reports/INSTANCE1_KG_ARTIFACT_AF1_20261011.md'
[string[]]$ordered=@($paths | Sort-Object -Unique)
[Array]::Sort($ordered,[StringComparer]::Ordinal)
$lines=@(foreach ($path in $ordered) {'{0}  {1}' -f (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash,$path.Replace('\','/')})
$manifest=Join-Path $root 'AF1_SHA256SUMS.txt'
[IO.File]::WriteAllText($manifest,($lines -join "`n")+"`n",[Text.UTF8Encoding]::new($false))
foreach ($line in Get-Content -LiteralPath $manifest) {
    if ($line -notmatch '^([A-F0-9]{64})  (.+)$') {throw 'Malformed AF1 manifest.'}
    if ((Get-FileHash -LiteralPath $Matches[2] -Algorithm SHA256).Hash -ne $Matches[1]) {throw 'AF1 evidence mismatch.'}
}
foreach ($file in $scripts) {if ((Get-FileHash -LiteralPath (Join-Path $repo "scripts/$file")).Hash -ne (Get-FileHash -LiteralPath (Join-Path $snapshots $file)).Hash) {throw 'Snapshot mismatch.'}}
'AF1_MANIFEST_ENTRIES='+$ordered.Count
'AF1_MANIFEST_CHECK=PASS'
'AF1_MANIFEST_SHA256='+(Get-FileHash -LiteralPath $manifest).Hash
