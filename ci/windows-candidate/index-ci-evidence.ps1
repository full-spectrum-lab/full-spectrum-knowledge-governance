# Created: 2026-10-11T16:18:00+08:00; Updated: 2026-10-11T16:32:00+08:00; Timezone: Asia/Shanghai
# ID: KG-WIN-CI-INDEX-20261011; Version: v0.2-draft; Status: LOCAL_DRAFT_PENDING_REREVIEW_CI_NOT_EXECUTED
# Owner: Instance1; Scope: limited CI evidence only; Baseline: KG 1295c27c
$ErrorActionPreference='Stop'
if (!$env:KG_ROOT -or !$env:RUNNER_TEMP) {throw 'Explicit runner root required.'}
$expected=Join-Path $env:RUNNER_TEMP "kg-validation-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT"
if ([IO.Path]::GetFullPath($env:KG_ROOT) -cne [IO.Path]::GetFullPath($expected)) {throw 'Evidence root binding mismatch.'}
$root=Join-Path $env:KG_ROOT 'evidence'
if (!(Test-Path $root)) {New-Item -ItemType Directory -Path $root -Force | Out-Null}
function Read-KgEvidenceJson {
    param([string]$Name)
    $path=Join-Path $root $Name
    if(!(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing evidence: $Name"}
    return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -ErrorAction Stop)
}
function Assert-KgCompleteEvidence {
    $commit='1295c27cd3409cb5395c04cd29378cb2610ab2ed'
    $hash='DB3FCA151F04CE016AC3E41C7E68A2B0B05ED76D69CB6248A9250AAF06C8543C'
    $summary=Read-KgEvidenceJson 'verification-summary.json'
    $package=Read-KgEvidenceJson 'package-verification.json'
    $execution=Read-KgEvidenceJson 'execution-state.json'
    $environment=Read-KgEvidenceJson 'environment.json'
    $acquisition=Read-KgEvidenceJson 'acquisition.json'
    $recordPath=Join-Path $root 'repeat-and-negative-results.json'
    $records=Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json -NoEnumerate -ErrorAction Stop
    if($records -isnot [array] -or $records.Count -ne 23){throw 'Expected exactly 23 explicit records.'}
    if($summary.status -cne 'PASS_WITH_SCOPE_LIMITATION' -or $summary.validator_commit -cne $commit -or
        $summary.zip_sha256 -cne $hash -or $summary.production_ready -isnot [bool] -or $summary.production_ready -or
        $summary.released -isnot [bool] -or $summary.released){throw 'Summary identity/status mismatch.'}
    foreach($pair in @(@('package_hashes',63),@('sqlite',10),@('consumer',5),@('negative_hash_version',2),@('negative_artifact',6))) {
        $value=$summary.($pair[0])
        if(($value -isnot [int] -and $value -isnot [long]) -or $value -ne $pair[1]) {throw 'Summary counts incomplete.'}
    }
    if($package.status -cne 'PASS' -or $package.release_commit -cne $commit -or
        $package.archive_sha256.ToUpperInvariant() -cne $hash -or $package.package_file_count -ne 63 -or
        $package.package_hash_failures -isnot [array] -or $package.package_hash_failures.Count -ne 0){throw 'Package result identity/hash/count mismatch.'}
    foreach($line in @("VERSION=0.2.1-alpha+$commit","COMMIT=$commit",'TARGET=win-x64','PRODUCTION_READY=NO')) {
        if($line -cnotin $package.version_output){throw 'Embedded identity evidence missing.'}
    }
    if($execution.status -cne 'PASS' -or $execution.validator_commit -cne $commit -or $execution.zip_sha256 -cne $hash -or
        $execution.primary_error -or $execution.retention_error){throw 'Execution or retention failed.'}
    if($env:GITHUB_SHA -cnotmatch '^[0-9a-f]{40}$' -or $environment.validator_commit -cne $commit -or
        $environment.harness_commit -cne $env:GITHUB_SHA -or $environment.run_id -cne $env:GITHUB_RUN_ID -or
        $environment.run_attempt -cne $env:GITHUB_RUN_ATTEMPT -or $environment.job_name -cne $env:GITHUB_JOB){throw 'Run/harness environment identity mismatch.'}
    if($acquisition.zip_sha256 -cne $hash -or $acquisition.gate -cne 'PASS_HASH_BEFORE_PACKAGE_EXPANSION'){throw 'Acquisition identity gate missing.'}
    if((Get-Content -LiteralPath (Join-Path $root 'package-exit.txt') -Raw).Trim() -cne 'EXIT_CODE=0'){throw 'Package raw exit not zero.'}
    if(!(Test-Path -LiteralPath (Join-Path $root 'post-test-status.log')) -or
        (Get-Content -LiteralPath (Join-Path $root 'post-test-status.log') -Raw)){throw 'Validator post-test status not clean.'}
    $expectedRows=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($i in 1..10){$expectedRows.Add("SQLITE/$i",@{log="sqlite-$i.log";positive=$true})}
    foreach($i in 1..5){$expectedRows.Add("LIBRARY_CONSUMER/$i",@{log="consumer-$i.log";positive=$true})}
    $expectedRows.Add('WRONG_HASH',@{log='negative-WRONG_HASH.log';error='Archive SHA-256 mismatch';positive=$false})
    $expectedRows.Add('WRONG_VERSION',@{log='negative-WRONG_VERSION.log';error='Release manifest identity mismatch';positive=$false})
    foreach($case in @('OBJECT','NULL','MISSING','ARRAY','WRONG_NAME','TRAVERSAL')) {
        $message='Candidate artifact must be a nonempty string.'
        if($case -ceq 'WRONG_NAME'){$message='Candidate artifact filename mismatch.'}
        if($case -ceq 'TRAVERSAL'){$message='Candidate artifact must be a single safe ZIP filename.'}
        $expectedRows.Add("ARTIFACT/$case",@{log="negative-artifact-$case.log";error=$message;positive=$false})
    }
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($row in $records) {
        $key=[string]$row.gate
        if($row.gate -cin @('SQLITE','LIBRARY_CONSUMER')){$key+='/'+$row.attempt}
        if($row.gate -ceq 'ARTIFACT'){$key+='/'+$row.case}
        if(!$expectedRows.ContainsKey($key) -or !$seen.Add($key)){throw 'Unknown or duplicate record identity.'}
        if($row.pass -isnot [bool] -or !$row.pass -or ($row.exit_code -isnot [int] -and $row.exit_code -isnot [long])){throw 'Record pass/exit type or value invalid.'}
        $expectedRow=$expectedRows[$key]
        $raw=Get-Content -LiteralPath (Join-Path $root $expectedRow.log) -Raw
        if($expectedRow.positive) {
            if($row.exit_code -ne 0){throw 'Positive record failed.'}
            if($row.gate -ceq 'SQLITE') {
                $sqlite=$raw | ConvertFrom-Json -ErrorAction Stop
                if($sqlite.status -cne 'PASS' -or $sqlite.native_sqlite -cne 'winsqlite3' -or
                    $sqlite.errors -isnot [array] -or $sqlite.errors.Count -ne 0){throw 'SQLite raw result invalid.'}
            } else {
                foreach($marker in @('LIBRARY_LOAD=PASS','V01_STORAGE_REOPEN=PASS','CONTRACT_UPGRADE=PASS','SNAPSHOT_ROLLBACK=PASS')) {
                    if(!$raw.Contains($marker)){throw 'Consumer raw marker missing.'}
                }
            }
        } else {
            if($row.exit_code -eq 0 -or $row.expected_error -cne $expectedRow.error -or !$raw.Contains($expectedRow.error)){throw 'Negative rejection evidence missing.'}
            if($row.gate -ceq 'ARTIFACT' -and ($row.audit_created -isnot [bool] -or $row.audit_created)){throw 'Artifact rejection did not precede audit creation.'}
        }
    }
}
$passed=$false
$validationError=$null
try {Assert-KgCompleteEvidence; $passed=$true} catch {$validationError=$_.Exception.Message}
@{run_id=$env:GITHUB_RUN_ID;run_attempt=$env:GITHUB_RUN_ATTEMPT;job_name=$env:GITHUB_JOB;harness_commit=$env:GITHUB_SHA;verification_status=if($passed){'PASS_REQUIRES_INDEPENDENT_REVIEW'}else{'INCOMPLETE_OR_FAILED'};validation_error=$validationError;production_ready=$false} | ConvertTo-Json | Set-Content (Join-Path $root 'handoff.json') -Encoding utf8
# Copy scripts only; never recurse into validator source or application/cache/data trees.
foreach($name in @('acquire-candidate.ps1','verify-ci-candidate.ps1','index-ci-evidence.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $root "harness-$name")
}
$validator=Join-Path $env:GITHUB_WORKSPACE 'validator/scripts'
foreach($name in @('verify-package-v0.2.0-alpha.ps1','verify-package-v0.2.1-alpha.ps1','candidate-packaging-tools.ps1')) {
    $file=Join-Path $validator $name
    if(Test-Path $file){Copy-Item -LiteralPath $file -Destination (Join-Path $root "validator-$name")}
}
$allowed=@('intake-artifact.json','intake-run.json','acquisition.json','sdk-install.log','environment.json','dotnet-info.log','dotnet-runtimes.log','system-sqlite.json','global.json','NuGet.Config','RELEASE_MANIFEST.json','package-verifier.log','package-exit.txt','repeat-and-negative-results.json','post-test-status.log','package-verification.json','ConsumerProgram.cs','PackageConsumer.csproj','verification-summary.json','execution-state.json','handoff.json','negative-WRONG_HASH.log','negative-WRONG_VERSION.log')
foreach($i in 1..10){$allowed+="sqlite-$i.log"}
foreach($i in 1..5){$allowed+="consumer-$i.log"}
foreach($case in @('OBJECT','NULL','MISSING','ARRAY','WRONG_NAME','TRAVERSAL')) {$allowed+="negative-artifact-$case.log";$allowed+="negative-manifests/$case.json"}
foreach($name in @('acquire-candidate.ps1','verify-ci-candidate.ps1','index-ci-evidence.ps1')) {$allowed+="harness-$name"}
foreach($name in @('verify-package-v0.2.0-alpha.ps1','verify-package-v0.2.1-alpha.ps1','candidate-packaging-tools.ps1')) {$allowed+="validator-$name"}
$files=@(Get-ChildItem $root -File -Recurse)
$relative=@($files | ForEach-Object { $_.FullName.Substring($root.Length+1).Replace('\','/') })
foreach($rel in $relative){if($rel -cnotin $allowed){throw "Unexpected evidence file: $rel"}}
[string[]]$ordered=$relative
[Array]::Sort($ordered,[StringComparer]::Ordinal)
$lines=@(foreach($rel in $ordered){'{0}  {1}' -f (Get-FileHash (Join-Path $root $rel)).Hash,$rel})
[IO.File]::WriteAllText((Join-Path $root 'SHA256SUMS.txt'),($lines -join "`n")+"`n",[Text.UTF8Encoding]::new($false))
Write-Output "EVIDENCE_FILES=$($ordered.Count)"
Write-Output "EVIDENCE_MANIFEST_SHA256=$((Get-FileHash (Join-Path $root 'SHA256SUMS.txt')).Hash)"
if(!$passed){throw "Evidence failed closed: $validationError"}
