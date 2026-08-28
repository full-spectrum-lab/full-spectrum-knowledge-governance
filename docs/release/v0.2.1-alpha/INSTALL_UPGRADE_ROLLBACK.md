# Install, upgrade, rollback and removal

## Requirements

- Windows x64.
- .NET SDK `10.0.301` to rebuild from source, or a compatible .NET 10 runtime
  to run the framework-dependent package.
- Windows system SQLite: `C:\Windows\System32\winsqlite3.dll`.

The SQLite DLL is supplied by Windows and is not included in the ZIP. Run the
package preflight before opening a registry:

```powershell
.\bin\FullSpectrum.Knowledge.TestHost.exe verify-k0-02
```

The output must contain `status=PASS` and `native_sqlite=winsqlite3`.

## Install

Extract the archive into a new directory, verify the external archive SHA-256
and package `SHA256SUMS`, then run `version`, `verify-k0-02`, and
`verify-k0-05` before switching a consumer to the package.

## Upgrade

Stop consumers, snapshot the SQLite database and artifact directory together,
install the candidate beside the old package, run all preflight checks, then
switch the consumer only after the compatibility gate passes.

## Rollback

Stop consumers and restore the binary, database, and artifact directory from
the same snapshot. Re-run `version`, `verify-k0-02`, and `verify-k0-05`.

## Removal

Stop consumers and export required audit evidence before removing the extracted
package directory. The package does not silently delete databases or artifacts.
