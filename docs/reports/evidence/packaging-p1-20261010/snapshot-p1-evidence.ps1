$ErrorActionPreference='Stop'
$repository=Split-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) -Parent
$manifest=Join-Path $PSScriptRoot 'SHA256SUMS.txt'
if(Test-Path -LiteralPath $manifest){throw 'Preserve existing snapshot; choose a new version before overwriting'}
$planSource='C:\Users\wangjian0926\Desktop\codex专属仓库\worktrees\instance1-kg-v02-k2-task-plan-20260923\docs\planning\V021_DETERMINISTIC_PACKAGING_CANDIDATE_PLAN_20261010.md'
$planCopy=Join-Path $repository 'docs/planning/V021_DETERMINISTIC_PACKAGING_CANDIDATE_PLAN_20261010.md'
if(Test-Path -LiteralPath $planCopy){throw 'Preserve existing plan copy'}
Copy-Item -LiteralPath $planSource -Destination $planCopy
if((Get-FileHash -LiteralPath $planSource).Hash -ne (Get-FileHash -LiteralPath $planCopy).Hash){throw 'Plan copy mismatch'}
& git -C $repository diff -- scripts/package-v0.2.0-alpha.ps1 scripts/package-v0.2.1-alpha.ps1 docs/release/v0.2.1-alpha/RELEASE_NOTES.md | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'tracked-diff.patch') -Encoding utf8
$code=@('scripts/package-v0.2.0-alpha.ps1','scripts/package-v0.2.1-alpha.ps1','scripts/candidate-packaging-tools.ps1','scripts/test-candidate-packaging-tools.ps1','docs/release/v0.2.1-alpha/RELEASE_NOTES.md','docs/planning/V021_DETERMINISTIC_PACKAGING_CANDIDATE_PLAN_20261010.md','docs/reports/INSTANCE1_KG_PACKAGING_P1_20261010.md')
$files=@($code|ForEach-Object {Get-Item -LiteralPath (Join-Path $repository $_)})+@(Get-ChildItem -LiteralPath $PSScriptRoot -File -Recurse)
$lines=$files|Sort-Object FullName|ForEach-Object {'{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName).Hash,$_.FullName.Substring($repository.Length+1).Replace('\','/')}
[IO.File]::WriteAllText($manifest,($lines -join "`n")+"`n",[Text.UTF8Encoding]::new($false))
$count=0
foreach($line in Get-Content -LiteralPath $manifest){if($line -notmatch '^([A-F0-9]{64})  (.+)$'){throw 'Bad manifest'};$hash=$Matches[1];$relative=$Matches[2];$full=[IO.Path]::GetFullPath((Join-Path $repository $relative));if(-not $full.StartsWith($repository+[IO.Path]::DirectorySeparatorChar)){throw 'Unsafe manifest path'};if((Get-FileHash -LiteralPath $full).Hash -ne $hash){throw 'Digest mismatch'};$count++}
"MANIFEST=PASS_$($count)_OF_$count"
