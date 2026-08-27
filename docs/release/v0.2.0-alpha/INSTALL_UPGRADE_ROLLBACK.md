# Install, upgrade, rollback and removal

## Requirements

- Windows x64.
- .NET SDK `10.0.301` to rebuild from source, or a compatible .NET 10 runtime to run the framework-dependent package.
- Local SQLite support is included by the published application dependencies.

## Install

Extract the archive to a new directory. Keep `bin/`, `library/`, `schemas/`, `examples/`,
the license files, `PACKAGE_MANIFEST.json`, and `SHA256SUMS` together. Verify the archive
against the external `SHA256SUMS` and `RELEASE_MANIFEST.json`, then verify package files with:

```powershell
Get-Content .\SHA256SUMS | ForEach-Object {
  $parts = $_ -split '  ', 2
  if ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path . $parts[1].Substring(2))).Hash.ToLowerInvariant() -ne $parts[0]) { throw "Hash mismatch: $($parts[1])" }
}
```

Run the CLI checks:

```powershell
.\bin\FullSpectrum.Knowledge.TestHost.exe version
.\bin\FullSpectrum.Knowledge.TestHost.exe verify-k0-05
```

The `library/` directory is a separately published in-process API payload. It is not a
network service and does not grant authorization to change or publish knowledge.

## Upgrade

Stop consumers, snapshot the existing SQLite database and artifact directory together,
install the new package beside the old one, and run the same CLI checks before switching
the consumer to the new `library/` payload. Existing v0.1.x records must be reopened by
the compatibility gate before use.

## Rollback

Stop consumers and restore the binary, SQLite database, and artifact directory from the
same pre-upgrade snapshot. Never mix a database snapshot with an unrelated binary.
Re-run `version` and `verify-k0-05` after rollback.

## Removal

Stop all consumers, export any required audit evidence, then remove the extracted package
directory. Removal is an operator action; this package does not silently delete databases,
artifacts, or audit history.
