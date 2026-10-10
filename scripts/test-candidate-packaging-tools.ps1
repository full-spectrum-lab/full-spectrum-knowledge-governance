param(
    [string]$Case = 'all',
    [string]$EvidenceRoot,
    [string]$InputPath,
    [int]$Order = 0,
    [string]$SourceLinkPdb,
    [string]$MissingSourceLinkPdb
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'candidate-packaging-tools.ps1')
$commit = '2517afc7ca315ec0d02f9f52bdbe3f39bc7f0a50'

# Child cases never compile KG or create a product candidate.
if ($Case -ne 'all') {
    try {
        switch ($Case) {
            'fixture' {
                New-Item -ItemType Directory -Path $InputPath -ErrorAction Stop | Out-Null
                $keys = if ($Order -eq 0) { @('z','a','A','empty','array','null','text') } else { @('text','null','array','empty','A','a','z') }
                $values = @{ z=9; empty=@{}; array=@(3,1,2); null=$null; text="中文`nquote`"\" }
                # Case-sensitive keys exercise Ordinal rather than culture-based ordering.
                $data = [Collections.Specialized.OrderedDictionary]::new([StringComparer]::Ordinal)
                foreach ($key in $keys) {
                    $value = if ($key -ceq 'a') { 'lower' } elseif ($key -ceq 'A') { 'upper' } else { $values[$key] }
                    $data.Add($key,$value)
                }
                Write-KgStableJson -Value $data -Path (Join-Path $InputPath 'fixture.json')
                Write-KgStableJson -Value @(@{ b=2; a=1 }) -Path (Join-Path $InputPath 'single-array.json')
                Write-KgStableJson -Value @() -Path (Join-Path $InputPath 'empty-array.json')
                Add-Type -AssemblyName System.IO.Compression
                $files = @(Get-KgOrdinalFiles -Root $InputPath)
                $stream = [IO.File]::Open((Join-Path $InputPath 'tool-fixture.zip'),[IO.FileMode]::CreateNew)
                $zip = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
                try {
                    foreach ($file in $files) {
                        $entry = $zip.CreateEntry($file.Name,[IO.Compression.CompressionLevel]::Optimal)
                        $entry.LastWriteTime = [DateTimeOffset]'2020-01-01T00:00:00Z'
                        $src = [IO.File]::OpenRead($file.FullName); $dst = $entry.Open()
                        try { $src.CopyTo($dst) } finally { $src.Dispose(); $dst.Dispose() }
                    }
                } finally { $zip.Dispose(); $stream.Dispose() }
                'TOOL_FIXTURE_ONLY=PASS'
            }
            'bad-origin' { Assert-KgCanonicalOrigin -Origin 'C:\local\repo' }
            'wrong-repository' { Assert-KgCanonicalOrigin -Origin 'https://github.com/other/repo.git' }
            'credentials-origin' { Assert-KgCanonicalOrigin -Origin 'https://secret@github.com/full-spectrum-lab/full-spectrum-knowledge-governance.git' }
            'wrong-commit' { Assert-KgCandidateSource -Repository (Split-Path $PSScriptRoot -Parent) -ExpectedCommit ('0'*40) }
            'short-commit' { Assert-KgCandidateSource -Repository (Split-Path $PSScriptRoot -Parent) -ExpectedCommit '2517afc' }
            'dirty-input' { Assert-KgCandidateSource -Repository (Split-Path $PSScriptRoot -Parent) -ExpectedCommit $commit }
            'existing-output' { Assert-KgNewOutputDirectory -Root $EvidenceRoot -RelativeName 'existing' }
            'traversal-output' { Assert-KgNewOutputDirectory -Root $EvidenceRoot -RelativeName '../escape' }
            'absolute-output' { Assert-KgNewOutputDirectory -Root $EvidenceRoot -RelativeName $InputPath }
            'missing-root' { Assert-KgNewOutputDirectory -Root (Join-Path $EvidenceRoot 'absent-root') -RelativeName 'new' }
            'wrong-sourcelink' { Assert-KgSourceLinkJson -Json '{"documents":{"/_/*":"https://raw.githubusercontent.com/other/repo/0000000000000000000000000000000000000000/*"}}' -ExpectedCommit $commit }
            'missing-sourcelink' { Assert-KgPortablePdbSourceLink -Path $InputPath -ExpectedCommit $commit }
            'wrong-pdb-commit' { Assert-KgPortablePdbSourceLink -Path $InputPath -ExpectedCommit ('0'*40) }
            'dirty-entrypoint' { & (Join-Path $PSScriptRoot 'package-v0.2.1-alpha.ps1') -ExpectedCommit $commit -CandidateOutputRoot $EvidenceRoot }
            'wrong-sdk' { Get-KgCandidateToolchain -Repository (Split-Path $PSScriptRoot -Parent) -Dotnet 'git' }
            'unsupported-json' { Write-KgStableJson -Value ([DateTime]'2020-01-01') -Path (Join-Path $EvidenceRoot 'unsupported.json') }
            default { throw 'Unknown tool test case.' }
        }
        exit 0
    } catch { Write-Output "EXPECTED_REJECT=$($_.Exception.Message)"; exit 1 }
}

if (-not $EvidenceRoot) { throw 'Explicit new evidence root is required.' }
if (Test-Path -LiteralPath $EvidenceRoot) { throw 'Refusing to overwrite test evidence.' }
New-Item -ItemType Directory -Path $EvidenceRoot | Out-Null
$pwsh = (Get-Process -Id $PID).Path
$results = [Collections.Generic.List[object]]::new()
function Record($Name,$Passed,$Detail) {
    $results.Add([ordered]@{name=$Name; passed=[bool]$Passed; detail=$Detail})
    Write-Output "TEST=$Name PASS=$Passed $Detail"
    if (-not $Passed) { throw "Tool test failed: $Name" }
}
function Child($Name,$ExpectedExit,[string]$Path='',[int]$Sequence=0) {
    $log = Join-Path $EvidenceRoot "$Name-$Sequence.log"
    & $pwsh -NoLogo -NoProfile -File $PSCommandPath -Case $Name -EvidenceRoot $EvidenceRoot -InputPath $Path -Order $Sequence 2>&1 | Tee-Object -FilePath $log
    $code = $LASTEXITCODE
    $reasons = @{
        'bad-origin'='origin is not'; 'wrong-repository'='origin is not'; 'credentials-origin'='origin is not'
        'wrong-commit'='source commit mismatch'; 'short-commit'='full lowercase SHA'; 'dirty-input'='clean committed input'
        'existing-output'='already exists'; 'traversal-output'='single relative directory'; 'absolute-output'='single relative directory'
        'missing-root'='isolated output root'; 'wrong-sourcelink'='SourceLink repository'; 'dirty-entrypoint'='clean committed input'
        'missing-sourcelink'='Exactly one SourceLink'; 'wrong-pdb-commit'='SourceLink repository'
        'wrong-sdk'='SDK does not match'; 'unsupported-json'='Unsupported candidate JSON value type'
    }
    $reasonMatched = $ExpectedExit -eq 0 -or (Get-Content -LiteralPath $log -Raw).Contains($reasons[$Name])
    Record "$Name-$Sequence" ($code -eq $ExpectedExit -and $reasonMatched) "exit=$code expected=$ExpectedExit reason_match=$reasonMatched"
}

try {
    Child 'fixture' 0 (Join-Path $EvidenceRoot 'process-a') 0
    Child 'fixture' 0 (Join-Path $EvidenceRoot 'process-b') 1
    foreach ($file in @('fixture.json','single-array.json','empty-array.json','tool-fixture.zip')) {
        $a = (Get-FileHash -LiteralPath (Join-Path $EvidenceRoot "process-a/$file")).Hash
        $b = (Get-FileHash -LiteralPath (Join-Path $EvidenceRoot "process-b/$file")).Hash
        Record "two-process-$file" ($a -eq $b) "sha256=$a"
    }
    $bytes = [IO.File]::ReadAllBytes((Join-Path $EvidenceRoot 'process-a/fixture.json'))
    $text = [Text.Encoding]::UTF8.GetString($bytes)
    Record 'utf8-no-bom-lf-one-final-newline' ($bytes[0] -ne 0xEF -and -not $text.Contains("`r") -and $text.EndsWith("`n") -and -not $text.EndsWith("`n`n")) 'format fixed'
    $decoded = ConvertFrom-Json $text -AsHashtable
    Record 'ordinal-keys-and-array-preservation' ($text.StartsWith('{"A":') -and ($decoded.array -join ',') -eq '3,1,2' -and $decoded.empty.Count -eq 0 -and $null -eq $decoded.null -and $decoded['a'] -ceq 'lower' -and $decoded['A'] -ceq 'upper') 'case-sensitive keys and original arrays'
    Record 'empty-array' ((Get-Content (Join-Path $EvidenceRoot 'process-a/empty-array.json') -Raw).Trim() -ceq '[]') 'empty array kept'
    Record 'singleton-array' ((Get-Content (Join-Path $EvidenceRoot 'process-a/single-array.json') -Raw).Trim() -ceq '[{"a":1,"b":2}]') 'array not unwrapped'
    $null = New-Item -ItemType Directory -Path (Join-Path $EvidenceRoot 'existing')
    foreach ($name in @('bad-origin','wrong-repository','credentials-origin','wrong-commit','short-commit','dirty-input','existing-output','traversal-output','absolute-output','missing-root','wrong-sourcelink','dirty-entrypoint','wrong-sdk','unsupported-json')) { Child $name 1 $EvidenceRoot }
    if (-not $SourceLinkPdb -or -not $MissingSourceLinkPdb) { throw 'Explicit diagnostic PDB inputs required.' }
    Assert-KgPortablePdbSourceLink -Path $SourceLinkPdb -ExpectedCommit $commit
    Record 'real-pdb-sourcelink-positive' $true 'read-only metadata; no assembly execution'
    Child 'missing-sourcelink' 1 $MissingSourceLinkPdb
    Child 'wrong-pdb-commit' 1 $SourceLinkPdb
    foreach ($origin in @('https://github.com/full-spectrum-lab/full-spectrum-knowledge-governance.git','git@github.com:full-spectrum-lab/full-spectrum-knowledge-governance.git')) { Assert-KgCanonicalOrigin $origin }
    Record 'canonical-origin-positive' $true 'HTTPS and approved SSH accepted'
    $cleanRepository = Split-Path (Split-Path (Split-Path (Split-Path (Split-Path $SourceLinkPdb -Parent) -Parent) -Parent) -Parent) -Parent
    Assert-KgCandidateSource -Repository $cleanRepository -ExpectedCommit $commit
    Record 'clean-source-positive' $true 'existing diagnostic clean clone; no build'
    $toolchain = Get-KgCandidateToolchain -Repository (Split-Path $PSScriptRoot -Parent) -Dotnet 'C:\Users\wangjian0926\.dotnet\dotnet.exe'
    Record 'pinned-toolchain-positive' ($toolchain.sdk -ceq '10.0.301') "sdk=$($toolchain.sdk) powershell=$($toolchain.powershell) runtime=$($toolchain.packaging_runtime)"
    $new = Assert-KgNewOutputDirectory -Root $EvidenceRoot -RelativeName 'unused-output'
    Record 'new-output-nonmutating' (-not (Test-Path -LiteralPath $new)) 'validation does not create or delete outputs'
    $orderRoot = Join-Path $EvidenceRoot 'ordinal-order'
    New-Item -ItemType Directory -Path $orderRoot | Out-Null
    foreach ($name in @('a.json','_.json','B.json','á.json')) { Write-KgStableJson -Value @{} -Path (Join-Path $orderRoot $name) }
    $names = @(Get-KgOrdinalFiles -Root $orderRoot | ForEach-Object Name)
    Record 'file-ordinal-order' (($names -join '|') -ceq 'B.json|_.json|a.json|á.json') 'Ordinal order, not culture collation'

    # Extract only functions from the shared packager; do not run restore/publish.
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'package-v0.2.0-alpha.ps1'),[ref]$null,[ref]$null)
    foreach ($name in @('Write-KgPackagingJson','Get-KgPackagingFiles')) {
        $function = $ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name},$true)
        . ([scriptblock]::Create($function.Extent.Text))
    }
    $DeterministicCandidate = $false
    $sample = @{ identity='legacy'; nested=@{b=2;a=1} }
    $sample | Write-KgPackagingJson -Path (Join-Path $EvidenceRoot 'legacy-actual.json')
    $sample | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 (Join-Path $EvidenceRoot 'legacy-reference.json')
    Record 'legacy-serializer-default-equivalence' ((Get-FileHash (Join-Path $EvidenceRoot 'legacy-actual.json')).Hash -eq (Get-FileHash (Join-Path $EvidenceRoot 'legacy-reference.json')).Hash) 'same-process comparison to original expression'
    $actual = @(Get-KgPackagingFiles -Root (Join-Path $EvidenceRoot 'process-a') | ForEach-Object FullName)
    $expected = @(Get-ChildItem -LiteralPath (Join-Path $EvidenceRoot 'process-a') -File -Recurse | Sort-Object FullName | ForEach-Object FullName)
    Record 'legacy-files-default-equivalence' (($actual -join '|') -ceq ($expected -join '|')) 'matches original ordering expression'
    $DeterministicCandidate = $true
    $sample | Write-KgPackagingJson -Path (Join-Path $EvidenceRoot 'stable-wrapper.json')
    Write-KgStableJson -Value $sample -Path (Join-Path $EvidenceRoot 'stable-helper.json')
    Record 'candidate-stable-writer-routing' ((Get-FileHash (Join-Path $EvidenceRoot 'stable-wrapper.json')).Hash -eq (Get-FileHash (Join-Path $EvidenceRoot 'stable-helper.json')).Hash) 'shared wrapper selects stable helper'
    $source = Get-Content (Join-Path $PSScriptRoot 'package-v0.2.0-alpha.ps1') -Raw
    Record 'candidate-no-hardcoded-pass-static' ($source.Contains("tests = if (`$DeterministicCandidate) { @{ status = 'NOT_EXECUTED' }") -and $source.Contains("golden = if (`$DeterministicCandidate) { 'NOT_EXECUTED' }") -and $source.Contains("schemas = if (`$DeterministicCandidate) { @{ status = 'NOT_EXECUTED' }")) 'candidate branch only; not a full build'
    Record 'legacy-removal-is-candidate-disabled-static' ($source.Contains('if (-not $DeterministicCandidate -and (Test-Path -LiteralPath $output))')) 'destructive legacy branch unavailable to candidate mode'
    foreach ($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*packag*.ps1') {
        $parseErrors = $null
        [void][Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$null,[ref]$parseErrors)
        Record "parse-$($file.Name)" (-not $parseErrors) 'PowerShell AST parse'
    }
} finally {
    [ordered]@{created_at=(Get-Date -Format o); scope='P1_TOOL_TEST_ONLY'; powershell=$PSVersionTable.PSVersion.ToString(); candidate_built=$false; tests=$results.ToArray()} | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 (Join-Path $EvidenceRoot 'results.json')
}
