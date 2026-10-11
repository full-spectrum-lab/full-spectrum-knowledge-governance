param([Parameter(Mandatory)][string]$EvidenceRoot, [string]$Case='all', [int]$Order=0)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'candidate-packaging-tools.ps1')
$filename='full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip'
$shell=(Get-Process -Id $PID).Path

function Emit-ActualProducerManifest([string]$Output, [bool]$Candidate) {
    # Execute only the committed packager's final manifest assignment and routing
    # ASTs, never its restore/publish/ZIP construction or product entry points.
    $errors=$null
    $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'package-v0.2.0-alpha.ps1'),[ref]$null,[ref]$errors)
    if ($errors) { throw 'Producer parse failed.' }
    $assignment=$ast.Find({param($n) $n -is [Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -ceq '$releaseRecord'},$true)
    $routing=$ast.Find({param($n) $n -is [Management.Automation.Language.IfStatementAst] -and $n.Extent.Text -match 'Write-KgCandidateReleaseManifest'},$true)
    if (!$assignment -or !$routing) { throw 'Actual producer manifest AST unavailable.' }
    $version='v0.2.1-alpha';$commit='81029a24026b450763ecc2cfdf3a509d0c67791e'
    $archive=Join-Path $Output $filename;$archiveHash='0'*64
    $output=$Output;$DeterministicCandidate=$Candidate
    $candidateToolchain=@{sdk='10.0.301';scope='SYNTHETIC_TOOL_METADATA_ONLY'}
    $fn=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Write-KgPackagingJson'},$true)
    . ([scriptblock]::Create($fn.Extent.Text))
    . ([scriptblock]::Create($assignment.Extent.Text))
    . ([scriptblock]::Create($routing.Extent.Text))
    return ,$releaseRecord
}

if ($Case -eq 'serialize') {
    if (Test-Path -LiteralPath $EvidenceRoot) {throw 'No overwrite of child evidence.'}
    New-Item -ItemType Directory -Path $EvidenceRoot | Out-Null
    $name=Split-Path -Leaf (Join-Path $EvidenceRoot $filename)
    $number=Write-Output ([int]7)
    $boolean=Write-Output $true
    $data=[Collections.Specialized.OrderedDictionary]::new([StringComparer]::Ordinal)
    $values=@{artifact=$name;number=$number;boolean=$boolean;array=@($name);empty=@();nested=[pscustomobject]@{z=2;a=1}}
    $keys=if ($Order -eq 0) {@('artifact','number','boolean','array','empty','nested')} else {@('nested','empty','array','boolean','number','artifact')}
    foreach ($key in $keys) {$data.Add($key,$values[$key])}
    Write-KgStableJson -Value $data -Path (Join-Path $EvidenceRoot 'scalars.json')
    $null=Emit-ActualProducerManifest -Output $EvidenceRoot -Candidate $true
    exit 0
}
if ($Case -ne 'all') {throw 'Unknown tool case.'}
if (Test-Path -LiteralPath $EvidenceRoot) {throw 'No overwrite of evidence root.'}
New-Item -ItemType Directory -Path $EvidenceRoot | Out-Null
$results=[Collections.Generic.List[object]]::new()
function Record([string]$Name,[bool]$Passed,[string]$Detail) {
    $results.Add([ordered]@{name=$Name;pass=$Passed;detail=$Detail})
    Write-Output "TEST=$Name PASS=$Passed $Detail"
    if (!$Passed) {throw "Artifact tool test failed: $Name"}
}
try {
    foreach ($order in 0..1) {
        & $shell -NoProfile -File $PSCommandPath -Case serialize -EvidenceRoot (Join-Path $EvidenceRoot "process-$order") -Order $order *> (Join-Path $EvidenceRoot "process-$order.log")
        Record "serialize-process-$order" ($LASTEXITCODE -eq 0) 'real command output and actual producer AST executed'
    }
    foreach ($file in @('scalars.json','RELEASE_MANIFEST.json')) {
        Record "two-process-$file" ((Get-FileHash (Join-Path $EvidenceRoot "process-0/$file")).Hash -eq (Get-FileHash (Join-Path $EvidenceRoot "process-1/$file")).Hash) 'raw JSON bytes'
    }
    $scalars=Get-Content -LiteralPath (Join-Path $EvidenceRoot 'process-0/scalars.json') -Raw | ConvertFrom-Json
    Record 'wrapped-scalar-types' ($scalars.artifact -is [string] -and $scalars.artifact -ceq $filename -and $scalars.boolean -is [bool] -and $scalars.boolean -eq $true -and $scalars.number -eq 7 -and $scalars.number -isnot [string]) 'cmdlet strings and boxed scalar values'
    Record 'array-semantics' ($scalars.array -is [array] -and $scalars.array.Count -eq 1 -and $scalars.array[0] -ceq $filename -and $scalars.empty -is [array] -and $scalars.empty.Count -eq 0) 'empty and singleton arrays'
    $text=Get-Content -LiteralPath (Join-Path $EvidenceRoot 'process-0/scalars.json') -Raw
    Record 'ordinal-custom-object-and-format' ($text.Contains('"nested":{"a":1,"z":2}') -and !$text.Contains("`r") -and $text.EndsWith("`n") -and !$text.EndsWith("`n`n")) 'Ordinal nested object and LF'
    $producer=Get-Content -LiteralPath (Join-Path $EvidenceRoot 'process-0/RELEASE_MANIFEST.json') -Raw | ConvertFrom-Json
    Record 'actual-producer-artifact-and-status' ($producer.artifact -is [string] -and $producer.artifact -ceq $filename -and $producer.tests.status -ceq 'NOT_EXECUTED' -and $producer.reproducibility -ceq 'NOT_EXECUTED') 'actual assignment and candidate routing, no candidate build'
    $rejected=$false
    try {$null=ConvertTo-KgStableJsonValue -Value (Get-Date)} catch {$rejected=$_.Exception.Message.Contains('Unsupported candidate JSON value type')}
    Record 'wrapped-unsupported-type-rejected' $rejected 'DateTime not converted to string/object'
    $valid=@{artifact=(Split-Path -Leaf (Join-Path $EvidenceRoot $filename));version='v0.2.1-alpha';target='win-x64'}
    $pathRejected=$false
    try {Assert-KgCandidateArtifactIdentity -Manifest $valid -ArchivePath (Join-Path $EvidenceRoot 'different.zip')}
    catch {$pathRejected=$_.Exception.Message.Contains('filename mismatch')}
    Record 'actual-input-filename-mismatch' $pathRejected 'valid declared artifact must match actual input name'
    # Simulate corruption at the disk-write boundary and execute the real
    # writer's readback gate. Never rewrite original candidate evidence.
    function Write-KgStableJson {
        param($Value,[string]$Path)
        [IO.File]::WriteAllText($Path,'{"artifact":{"Length":59},"version":"v0.2.1-alpha","target":"win-x64"}',[Text.UTF8Encoding]::new($false))
    }
    $readbackRejected=$false
    try {Write-KgCandidateReleaseManifest -Value $valid -Path (Join-Path $EvidenceRoot 'readback-corrupted.json') -ArchivePath (Join-Path $EvidenceRoot $filename)}
    catch {$readbackRejected=$_.Exception.Message.Contains('nonempty string')}
    finally {. (Join-Path $PSScriptRoot 'candidate-packaging-tools.ps1')}
    Record 'producer-readback-rejects-corrupted-json' $readbackRejected 'valid memory value is insufficient when disk field is invalid'

    Add-Type -AssemblyName System.IO.Compression
    $zipPath=Join-Path $EvidenceRoot $filename
    $stream=[IO.File]::Open($zipPath,[IO.FileMode]::CreateNew)
    $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
    try {
        $entry=$zip.CreateEntry('TOOL_FIXTURE_ONLY.txt');$writer=[IO.StreamWriter]::new($entry.Open())
        try {$writer.Write('No executable payload. Not a product candidate.')} finally {$writer.Dispose()}
    } finally {$zip.Dispose();$stream.Dispose()}
    $badValues=@(
        @{name='object';value=@{Length=59};reason='nonempty string'},
        @{name='missing';reason='nonempty string'},
        @{name='null';value=$null;reason='nonempty string'},
        @{name='empty';value='';reason='nonempty string'},
        @{name='whitespace';value=' ';reason='nonempty string'},
        @{name='number';value=59;reason='nonempty string'},
        @{name='boolean';value=$true;reason='nonempty string'},
        @{name='array';value=@($filename);reason='nonempty string'},
        @{name='wrong-name';value='wrong.zip';reason='filename mismatch'},
        @{name='wrong-version-name';value=$filename.Replace('0.2.1','0.2.0');reason='filename mismatch'},
        @{name='uppercase';value=$filename.ToUpperInvariant();reason='filename mismatch'},
        @{name='absolute';value='C:\bad.zip';reason='single safe ZIP filename'},
        @{name='parent';value='../bad.zip';reason='single safe ZIP filename'},
        @{name='separator';value='sub\bad.zip';reason='single safe ZIP filename'},
        @{name='colon';value='file:bad.zip';reason='single safe ZIP filename'},
        @{name='leading-space';value=" $filename";reason='single safe ZIP filename'},
        @{name='trailing-space';value="$filename ";reason='single safe ZIP filename'},
        @{name='control';value="bad`n.zip";reason='single safe ZIP filename'},
        @{name='wrong-version';value=$filename;version='v0.2.0-alpha';reason='version or target mismatch'},
        @{name='wrong-target';value=$filename;target='linux-x64';reason='version or target mismatch'}
    )
    foreach ($caseData in $badValues) {
        $manifest=@{version='v0.2.1-alpha';target='win-x64';sha256='0'*64}
        if ($caseData.ContainsKey('value')) {$manifest.artifact=$caseData.value}
        if ($caseData.version) {$manifest.version=$caseData.version}
        if ($caseData.target) {$manifest.target=$caseData.target}
        $manifestPath=Join-Path $EvidenceRoot "$($caseData.name).json"
        # Deliberately retain invalid JSON types; do not pass them through the fixer.
        $manifest | ConvertTo-Json -Depth 5 -Compress | Set-Content -LiteralPath $manifestPath -Encoding utf8
        $audit=Join-Path $EvidenceRoot "reject-$($caseData.name)"
        $log=Join-Path $EvidenceRoot "$($caseData.name).log"
        & $shell -NoProfile -File (Join-Path $PSScriptRoot 'verify-package-v0.2.1-alpha.ps1') -Package $zipPath -ReleaseManifest $manifestPath -ExpectedSha256 ('0'*64) -AuditRoot $audit *> $log
        $code=$LASTEXITCODE
        Record "verifier-reject-$($caseData.name)" ($code -ne 0 -and (Get-Content -LiteralPath $log -Raw).Contains($caseData.reason) -and !(Test-Path -LiteralPath $audit)) "exit=$code exact reason; audit not created and payload not executed"
    }
    $positive=Join-Path $EvidenceRoot 'process-0/RELEASE_MANIFEST.json'
    $audit=Join-Path $EvidenceRoot 'positive-to-hash-gate'
    $log=Join-Path $EvidenceRoot 'positive-to-hash-gate.log'
    & $shell -NoProfile -File (Join-Path $PSScriptRoot 'verify-package-v0.2.1-alpha.ps1') -Package $zipPath -ReleaseManifest $positive -ExpectedSha256 ('0'*64) -AuditRoot $audit *> $log
    Record 'correct-field-reaches-existing-hash-gate' ($LASTEXITCODE -ne 0 -and (Get-Content -LiteralPath $log -Raw).Contains('Archive SHA-256 mismatch') -and (Test-Path -LiteralPath $audit)) 'actual verifier accepts field; no executable fixture'
    $legacyAudit=Join-Path $EvidenceRoot 'legacy-default-probe'
    $legacyLog=Join-Path $EvidenceRoot 'legacy-default-probe.log'
    & $shell -NoProfile -File (Join-Path $PSScriptRoot 'verify-package-v0.2.0-alpha.ps1') -Package $zipPath -ReleaseManifest (Join-Path $EvidenceRoot 'object.json') -ExpectedSha256 ('0'*64) -AuditRoot $legacyAudit *> $legacyLog
    Record 'legacy-default-no-candidate-gate' ($LASTEXITCODE -ne 0 -and (Get-Content -LiteralPath $legacyLog -Raw).Contains('Archive SHA-256 mismatch') -and (Test-Path -LiteralPath $legacyAudit)) 'default 0.2.0 verifier behavior preserved'
    $legacyRoot=Join-Path $EvidenceRoot 'legacy-producer';New-Item -ItemType Directory -Path $legacyRoot | Out-Null
    $legacyRecord=Emit-ActualProducerManifest -Output $legacyRoot -Candidate $false
    $reference=Join-Path $legacyRoot 'reference.json'
    $legacyRecord | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reference -Encoding utf8
    Record 'legacy-producer-original-expression' ((Get-FileHash (Join-Path $legacyRoot 'RELEASE_MANIFEST.json')).Hash -eq (Get-FileHash $reference).Hash) 'actual legacy final routing matches original serialization expression'
    foreach ($file in @('candidate-packaging-tools.ps1','package-v0.2.0-alpha.ps1','verify-package-v0.2.0-alpha.ps1','test-candidate-packaging-tools.ps1','test-candidate-artifact-identity.ps1')) {
        $errors=$null;[void][Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $file),[ref]$null,[ref]$errors)
        Record "parse-$file" (!$errors) 'AST syntax'
    }
} finally {
    [ordered]@{created_at=(Get-Date -Format o);scope='AF1_TOOL_ONLY';candidate_built=$false;powershell=$PSVersionTable.PSVersion.ToString();tests=$results.ToArray()} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $EvidenceRoot 'results.json') -Encoding utf8
}
