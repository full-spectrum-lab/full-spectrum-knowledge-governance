# Candidate-only tooling. Not RFC8785 and not a product runtime dependency.
function ConvertTo-KgStableJsonValue {
    param([AllowNull()]$Value, [int]$Depth = 0)
    if ($Depth -gt 32) { throw 'Candidate JSON depth exceeds 32.' }
    if ($null -eq $Value) { return $null }
    if ($Value -is [Collections.IDictionary] -or $Value -is [pscustomobject]) {
        $names = if ($Value -is [Collections.IDictionary]) { @($Value.Keys) } else { @($Value.PSObject.Properties.Name) }
        foreach ($name in $names) { if ($name -isnot [string]) { throw 'JSON object keys must be strings.' } }
        [string[]]$keys = @($names)
        [Array]::Sort($keys, [StringComparer]::Ordinal)
        $result = [Collections.Specialized.OrderedDictionary]::new([StringComparer]::Ordinal)
        foreach ($key in $keys) {
            $child = if ($Value -is [Collections.IDictionary]) { $Value[$key] } else { $Value.PSObject.Properties[$key].Value }
            $result.Add($key, (ConvertTo-KgStableJsonValue -Value $child -Depth ($Depth + 1)))
        }
        return ,$result
    }
    if ($Value -is [array] -or $Value -is [Collections.IList]) {
        $result = @()
        foreach ($child in $Value) { $result += ,(ConvertTo-KgStableJsonValue -Value $child -Depth ($Depth + 1)) }
        return ,$result
    }
    if ($Value -is [string] -or $Value -is [bool] -or $Value -is [byte] -or
        $Value -is [int16] -or $Value -is [int32] -or $Value -is [int64] -or
        $Value -is [uint16] -or $Value -is [uint32] -or $Value -is [uint64] -or $Value -is [decimal]) { return $Value }
    throw "Unsupported candidate JSON value type: $($Value.GetType().FullName)"
}

function Write-KgStableJson {
    param([Parameter(Mandatory)][AllowNull()]$Value, [Parameter(Mandatory)][string]$Path)
    if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Candidate tooling requires PowerShell 7 or later.' }
    $text = ConvertTo-Json -InputObject (ConvertTo-KgStableJsonValue -Value $Value) -Depth 100 -Compress
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd("`n") + "`n"
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($Path), $text, [Text.UTF8Encoding]::new($false))
}

function Get-KgOrdinalFiles {
    param([Parameter(Mandatory)][string]$Root)
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $map = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach ($item in @(Get-Item -LiteralPath $rootPath -Force) + @(Get-ChildItem -LiteralPath $rootPath -Recurse -Force)) {
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Package tree contains a reparse point.' }
    }
    foreach ($file in Get-ChildItem -LiteralPath $rootPath -File -Recurse) {
        if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Package file is a reparse point.' }
        $relative = $file.FullName.Substring($rootPath.Length + 1).Replace('\', '/')
        $map.Add($relative, $file)
    }
    [string[]]$keys = @($map.Keys)
    [Array]::Sort($keys, [StringComparer]::Ordinal)
    foreach ($key in $keys) { $map[$key] }
}

function Assert-KgCanonicalOrigin {
    param([Parameter(Mandatory)][string]$Origin)
    $allowed = @('https://github.com/full-spectrum-lab/full-spectrum-knowledge-governance.git',
        'https://github.com/full-spectrum-lab/full-spectrum-knowledge-governance',
        'git@github.com:full-spectrum-lab/full-spectrum-knowledge-governance.git')
    if (-not (@($allowed | Where-Object { [string]::Equals($_, $Origin, [StringComparison]::Ordinal) }).Count)) {
        throw 'Candidate origin is not an approved canonical KG address.'
    }
}

function Assert-KgCandidateSource {
    param([Parameter(Mandatory)][string]$Repository, [Parameter(Mandatory)][string]$ExpectedCommit)
    if ($ExpectedCommit -cnotmatch '^[0-9a-f]{40}$') { throw 'Expected commit must be full lowercase SHA.' }
    $head = & git -C $Repository rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or "$head".Trim() -cne $ExpectedCommit) { throw 'Candidate source commit mismatch.' }
    $dirty = & git -C $Repository status --porcelain
    if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Candidate packaging requires a clean committed input.' }
    $origin = & git -C $Repository remote get-url origin
    if ($LASTEXITCODE -ne 0) { throw 'Candidate origin missing.' }
    Assert-KgCanonicalOrigin -Origin "$origin".Trim()
}

function Get-KgCandidateToolchain {
    param([Parameter(Mandatory)][string]$Repository, [Parameter(Mandatory)][string]$Dotnet)
    if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Candidate tooling requires PowerShell 7 or later.' }
    $pin = Get-Content -LiteralPath (Join-Path $Repository 'global.json') -Raw | ConvertFrom-Json
    if ($pin.sdk.rollForward -cne 'disable' -or $pin.sdk.allowPrerelease -ne $false) { throw 'SDK pin must disable roll-forward and prerelease.' }
    Push-Location $Repository
    try { $sdk = & $Dotnet --version; if ($LASTEXITCODE -ne 0 -or "$sdk".Trim() -cne $pin.sdk.version) { throw 'Candidate SDK does not match global.json.' } }
    finally { Pop-Location }
    $runtimes = @(& $Dotnet --list-runtimes)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot record installed runtimes.' }
    $core = @($runtimes | Where-Object { $_ -match '^Microsoft.NETCore.App ' } | ForEach-Object { ($_ -split ' ')[1] })
    if ($core.Count -eq 0) { throw 'No Microsoft.NETCore.App runtime recorded.' }
    [Array]::Sort($core, [StringComparer]::Ordinal)
    $git = & git --version
    if ($LASTEXITCODE -ne 0) { throw 'Cannot record Git version.' }
    return @{ sdk = "$sdk".Trim(); installed_netcore_runtimes = $core; powershell = $PSVersionTable.PSVersion.ToString(); packaging_runtime = [Environment]::Version.ToString(); git = "$git".Trim() }
}

function Assert-KgNewOutputDirectory {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$RelativeName)
    if ([IO.Path]::IsPathRooted($RelativeName) -or $RelativeName -match '[\\/]' -or $RelativeName -in @('.', '..')) {
        throw 'Candidate output name must be a single relative directory name.'
    }
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $rootPath -PathType Container)) { throw 'An explicit existing isolated output root is required.' }
    $item = Get-Item -LiteralPath $rootPath -Force
    while ($null -ne $item) {
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Output ancestors cannot be reparse points.' }
        $item = $item.Parent
    }
    $path = [IO.Path]::GetFullPath((Join-Path $rootPath $RelativeName))
    if (-not $path.StartsWith($rootPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output containment failed.' }
    if (Test-Path -LiteralPath $path) { throw 'Candidate output already exists; refusing replacement.' }
    return $path
}

function Assert-KgSourceLinkJson {
    param([Parameter(Mandatory)][string]$Json, [Parameter(Mandatory)][string]$ExpectedCommit)
    if ($ExpectedCommit -cnotmatch '^[0-9a-f]{40}$') { throw 'SourceLink expected commit is invalid.' }
    $value = ConvertFrom-Json -InputObject $Json -AsHashtable
    $expected = "https://raw.githubusercontent.com/full-spectrum-lab/full-spectrum-knowledge-governance/$ExpectedCommit/*"
    if ($value.Count -ne 1 -or $value.documents -isnot [Collections.IDictionary] -or
        $value.documents.Count -ne 1 -or $value.documents['/_/*'] -cne $expected) { throw 'SourceLink repository, mapping or commit mismatch.' }
}

function Assert-KgPortablePdbSourceLink {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$ExpectedCommit)
    $stream = [IO.File]::OpenRead($Path)
    $provider = $null
    try {
        $provider = [System.Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($stream)
        $reader = $provider.GetMetadataReader()
        $found = 0
        foreach ($handle in $reader.CustomDebugInformation) {
            $info = $reader.GetCustomDebugInformation($handle)
            if ($reader.GetGuid($info.Kind) -eq [Guid]'cc110556-a091-4d38-9fec-25ab9a351a6a') {
                if ($info.Parent.Kind.ToString() -ne 'ModuleDefinition') { throw 'SourceLink must be module-level.' }
                Assert-KgSourceLinkJson -Json ([Text.Encoding]::UTF8.GetString($reader.GetBlobBytes($info.Value))) -ExpectedCommit $ExpectedCommit
                $found++
            }
        }
        if ($found -ne 1) { throw 'Exactly one SourceLink record is required per PDB.' }
    } finally { if ($provider) { $provider.Dispose() }; $stream.Dispose() }
}
