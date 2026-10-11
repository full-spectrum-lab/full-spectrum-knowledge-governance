# Created: 2026-10-11T16:18:00+08:00; Updated: 2026-10-11T16:32:00+08:00; Timezone: Asia/Shanghai
# ID: KG-WIN-CI-VERIFY-20261011; Version: v0.2-draft; Status: LOCAL_DRAFT_PENDING_REREVIEW_CI_NOT_EXECUTED
# Owner: Instance1; Scope: existing package, not product rebuild; Baseline: 1295c27c / DB3FCA15
param([Parameter(Mandatory)][string]$ValidatorRoot)
$ErrorActionPreference='Stop'
function Save-KgGeneratedEvidence {
    param([Parameter(Mandatory)][string]$AuditRoot,[Parameter(Mandatory)][string]$EvidenceRoot)
    $map=[ordered]@{
        'package-verification.json'='package-verification.json'
        'consumer/Program.cs'='ConsumerProgram.cs'
        'consumer/PackageConsumer.csproj'='PackageConsumer.csproj'
    }
    foreach($rel in $map.Keys) {
        $origin=Join-Path $AuditRoot $rel
        if(Test-Path -LiteralPath $origin -PathType Leaf) {
            if((Get-Item -LiteralPath $origin).Attributes -band [IO.FileAttributes]::ReparsePoint) {throw 'Generated evidence must not be a reparse point.'}
            $target=Join-Path $EvidenceRoot $map[$rel]
            Copy-Item -LiteralPath $origin -Destination $target
            if((Get-FileHash -LiteralPath $origin).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {throw 'Generated evidence copy hash mismatch.'}
        }
    }
}
$source=(Resolve-Path -LiteralPath $ValidatorRoot).Path
$taskCommit='1295c27cd3409cb5395c04cd29378cb2610ab2ed'
$taskHash='DB3FCA151F04CE016AC3E41C7E68A2B0B05ED76D69CB6248A9250AAF06C8543C'
$taskRoot=Join-Path $env:KG_ROOT 'evidence'
$package=Join-Path $env:KG_ROOT 'input/full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip'
$release=Join-Path $env:KG_ROOT 'input/RELEASE_MANIFEST.json'
if ((& git -C $source rev-parse HEAD) -cne $taskCommit -or
    (& git -C $source rev-parse HEAD^) -cne '81029a24026b450763ecc2cfdf3a509d0c67791e' -or
    (& git -C $source status --porcelain)) {throw 'Fixed validator source/parent/clean-state mismatch.'}
if ((Get-FileHash $package).Hash -ne $taskHash -or
    (Get-FileHash $release).Hash -ne '48AF44E7E6C7035C62D421F9D22FC89F7A019D89B79FA998ED81F5283AA05B66') {throw 'Input raw identity mismatch.'}
$z=[IO.Compression.ZipFile]::OpenRead($package)
try {
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($entry in $z.Entries) {
        $rel=$entry.FullName.Replace('\','/')
        if (!$seen.Add($rel) -or $rel -match '(^/|:|(^|/)\.\.(/|$))' -or
            $rel -match '[\x00-\x1f]' -or $rel.EndsWith('/') -or $entry.Length -gt 268435456) {throw 'Unsafe or duplicate package ZIP entry.'}
    }
    if ($seen.Count -ne 64) {throw 'Unexpected package entry count.'}
} finally {$z.Dispose()}
foreach ($name in @('nuget-packages','nuget-http','nuget-plugins','dotnet-home','temp')) {
    $path=Join-Path $env:KG_ROOT $name
    if(Test-Path $path){throw 'Cache reuse forbidden.'}
    New-Item -ItemType Directory -Path $path | Out-Null
}
$env:DOTNET_CLI_HOME=Join-Path $env:KG_ROOT 'dotnet-home'
$env:NUGET_PACKAGES=Join-Path $env:KG_ROOT 'nuget-packages'
$env:NUGET_HTTP_CACHE_PATH=Join-Path $env:KG_ROOT 'nuget-http'
$env:NUGET_PLUGINS_CACHE_PATH=Join-Path $env:KG_ROOT 'nuget-plugins'
$env:TEMP=Join-Path $env:KG_ROOT 'temp';$env:TMP=$env:TEMP
'{"sdk":{"version":"10.0.301","rollForward":"disable","allowPrerelease":false}}' | Set-Content (Join-Path $env:KG_ROOT 'global.json') -Encoding utf8
'<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear /></packageSources></configuration>' | Set-Content (Join-Path $env:KG_ROOT 'NuGet.Config') -Encoding utf8
$shell=(Get-Command pwsh).Source
$env:GH_TOKEN=$null
@{validator_commit=$taskCommit;harness_commit=$env:GITHUB_SHA;run_id=$env:GITHUB_RUN_ID;run_attempt=$env:GITHUB_RUN_ATTEMPT;job_name=$env:GITHUB_JOB;runner_os=$env:RUNNER_OS;runner_arch=$env:RUNNER_ARCH;image_os=$env:ImageOS;image_version=$env:ImageVersion;windows=[Environment]::OSVersion.VersionString;powershell=$PSVersionTable.PSVersion.ToString();dotnet=$env:DOTNET_EXE;scope='HOSTED_WINDOWS_ACTUAL_IMAGE_ONLY';source_build='NOT_EXECUTED'} | ConvertTo-Json | Set-Content (Join-Path $taskRoot 'environment.json') -Encoding utf8
& $env:DOTNET_EXE --info *> (Join-Path $taskRoot 'dotnet-info.log')
& $env:DOTNET_EXE --list-runtimes *> (Join-Path $taskRoot 'dotnet-runtimes.log')
Get-Item "$env:SystemRoot/System32/winsqlite3.dll" | Select-Object Name,@{n='FileVersion';e={$_.VersionInfo.FileVersion}},Length | ConvertTo-Json | Set-Content (Join-Path $taskRoot 'system-sqlite.json') -Encoding utf8
Copy-Item (Join-Path $env:KG_ROOT 'global.json'),(Join-Path $env:KG_ROOT 'NuGet.Config'),$release -Destination $taskRoot
$audit=Join-Path $env:KG_ROOT 'package-audit'
$records=[Collections.Generic.List[object]]::new()
$primaryFailure=$null
$retentionFailure=$null
try {
& $shell -NoProfile -File (Join-Path $source 'scripts/verify-package-v0.2.1-alpha.ps1') -Package $package -ReleaseManifest $release -ExpectedSha256 $taskHash -AuditRoot $audit *> (Join-Path $taskRoot 'package-verifier.log')
$packageCode=$LASTEXITCODE
"EXIT_CODE=$packageCode" | Set-Content -LiteralPath (Join-Path $taskRoot 'package-exit.txt') -Encoding utf8
Write-Output "PACKAGE_GATE_EXIT=$packageCode"
if ($packageCode -ne 0) {throw 'Package gate failed; preserve logs and stop.'}
$packageResult=Get-Content (Join-Path $audit 'package-verification.json') -Raw | ConvertFrom-Json
if ($packageResult.status -cne 'PASS' -or $packageResult.package_file_count -ne 63 -or
    @($packageResult.package_hash_failures).Count -ne 0 -or $packageResult.release_commit -cne $taskCommit) {throw 'Package result identity/count mismatch.'}
$install=Join-Path $audit 'install-v0.2.1-alpha'
$cli=Join-Path $install 'bin/FullSpectrum.Knowledge.TestHost.exe'
$consumer=Join-Path $audit 'consumer/PackageConsumer.csproj'
Push-Location $install
try {
    foreach ($attempt in 1..10) {
        $raw=@(& $cli verify-k0-02 2>&1);$code=$LASTEXITCODE
        $raw | Set-Content -LiteralPath (Join-Path $taskRoot "sqlite-$attempt.log") -Encoding utf8
        $data=($raw -join "`n") | ConvertFrom-Json
        $ok=($code -eq 0 -and $data.status -eq 'PASS' -and $data.native_sqlite -eq 'winsqlite3' -and @($data.errors).Count -eq 0)
        $records.Add(@{gate='SQLITE';attempt=$attempt;exit_code=$code;pass=$ok})
        if (!$ok) {throw "SQLite $attempt failed."}
    }
    foreach ($attempt in 1..5) {
        $dataRoot=Join-Path $env:KG_ROOT "consumer-data-$attempt"
        if (Test-Path -LiteralPath $dataRoot) {throw 'Consumer data reuse prohibited.'}
        $raw=@(& $env:DOTNET_EXE run --no-build --no-restore --project $consumer --configuration Release -- $dataRoot 2>&1);$code=$LASTEXITCODE
        $raw | Set-Content -LiteralPath (Join-Path $taskRoot "consumer-$attempt.log") -Encoding utf8
        $ok=($code -eq 0)
        foreach ($marker in @('LIBRARY_LOAD=PASS','V01_STORAGE_REOPEN=PASS','CONTRACT_UPGRADE=PASS','SNAPSHOT_ROLLBACK=PASS')) {if (!(($raw -join "`n").Contains($marker))) {$ok=$false}}
        $records.Add(@{gate='LIBRARY_CONSUMER';attempt=$attempt;exit_code=$code;pass=$ok})
        if (!$ok) {throw "Consumer $attempt failed."}
    }
    foreach ($gate in @('WRONG_HASH','WRONG_VERSION')) {
        $hash=$taskHash;$version='0.2.1-alpha';$expected='Archive SHA-256 mismatch'
        if ($gate -eq 'WRONG_HASH') {$hash='0'*64} else {$version='0.2.0-alpha';$expected='Release manifest identity mismatch'}
        $raw=@(& $shell -NoProfile -File (Join-Path $source 'scripts/verify-package-v0.2.0-alpha.ps1') -Package $package -ReleaseManifest $release -ExpectedSha256 $hash -ExpectedVersion $version -AuditRoot (Join-Path $env:KG_ROOT "negative-$gate") 2>&1);$code=$LASTEXITCODE
        $raw | Set-Content -LiteralPath (Join-Path $taskRoot "negative-$gate.log") -Encoding utf8
        $ok=($code -ne 0 -and ($raw -join "`n").Contains($expected))
        $records.Add(@{gate=$gate;exit_code=$code;pass=$ok;expected_error=$expected})
        if (!$ok) {throw "Negative $gate failed."}
    }

    $negativeRoot=Join-Path $taskRoot 'negative-manifests'
    New-Item -ItemType Directory -Path $negativeRoot | Out-Null
    foreach ($case in @('OBJECT','NULL','MISSING','ARRAY','WRONG_NAME','TRAVERSAL')) {
        $m=Get-Content -LiteralPath $release -Raw | ConvertFrom-Json -AsHashtable
        $expected='Candidate artifact must be a nonempty string.'
        switch ($case) {
            'OBJECT' {$m.artifact=@{Length=59}}
            'NULL' {$m.artifact=$null}
            'MISSING' {$m.Remove('artifact')}
            'ARRAY' {$m.artifact=@('full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip')}
            'WRONG_NAME' {$m.artifact='incorrect.zip';$expected='Candidate artifact filename mismatch.'}
            'TRAVERSAL' {$m.artifact='../full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip';$expected='Candidate artifact must be a single safe ZIP filename.'}
        }
        $mutated=Join-Path $negativeRoot "$case.json"
        $m | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $mutated -Encoding utf8
        $negativeAudit=Join-Path $env:KG_ROOT "negative-artifact-$case"
        $raw=@(& $shell -NoProfile -File (Join-Path $source 'scripts/verify-package-v0.2.1-alpha.ps1') -Package $package -ReleaseManifest $mutated -ExpectedSha256 $taskHash -AuditRoot $negativeAudit 2>&1);$code=$LASTEXITCODE
        $raw | Set-Content -LiteralPath (Join-Path $taskRoot "negative-artifact-$case.log") -Encoding utf8
        $ok=($code -ne 0 -and ($raw -join "`n").Contains($expected) -and !(Test-Path -LiteralPath $negativeAudit))
        $records.Add(@{gate='ARTIFACT';case=$case;exit_code=$code;pass=$ok;expected_error=$expected;audit_created=(Test-Path -LiteralPath $negativeAudit)})
        if (!$ok) {throw "Artifact negative $case failed; stop without repair."}
    }
} finally {
    Pop-Location
    ConvertTo-Json -InputObject @($records.ToArray()) -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'repeat-and-negative-results.json') -Encoding utf8
}
& git -C $source status --porcelain *> (Join-Path $taskRoot 'post-test-status.log')
if ((Get-Content (Join-Path $taskRoot 'post-test-status.log') -Raw)) {throw 'Validator source changed.'}
if ($records.Count -ne 23 -or @($records | Where-Object { !$_.pass }).Count -ne 0) {throw 'Missing or failed repeat/negative gates.'}
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $taskHash) {throw 'Package mutated.'}
@{status='PASS_WITH_SCOPE_LIMITATION';validator_commit=$taskCommit;zip_sha256=$taskHash;package_hashes=63;sqlite=10;consumer=5;negative_hash_version=2;negative_artifact=6;second_windows_desktop='NOT_EXECUTED';production_ready=$false;released=$false} | ConvertTo-Json | Set-Content (Join-Path $taskRoot 'verification-summary.json') -Encoding utf8
} catch {
    $primaryFailure=$_
} finally {
    # Preserve only already-generated whitelist files, including on package/loop failure.
    try {
        Save-KgGeneratedEvidence -AuditRoot $audit -EvidenceRoot $taskRoot
        ConvertTo-Json -InputObject @($records.ToArray()) -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'repeat-and-negative-results.json') -Encoding utf8
    } catch {$retentionFailure=$_}
    try {
        @{status=if($primaryFailure -or $retentionFailure){'FAILED'}else{'PASS'};validator_commit=$taskCommit;zip_sha256=$taskHash;primary_error=if($primaryFailure){$primaryFailure.Exception.Message}else{$null};retention_error=if($retentionFailure){$retentionFailure.Exception.Message}else{$null}} | ConvertTo-Json | Set-Content (Join-Path $taskRoot 'execution-state.json') -Encoding utf8
    } catch {if(!$retentionFailure){$retentionFailure=$_}}
}
if($primaryFailure){throw $primaryFailure}
if($retentionFailure){throw $retentionFailure}
Write-Output 'CI_EXISTING_PACKAGE_GATES=PASS'
