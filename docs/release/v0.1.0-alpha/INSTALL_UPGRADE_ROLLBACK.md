# Install, upgrade and rollback

Build requirement: .NET SDK 10.0.301. Runtime requirement: a compatible .NET 10 Runtime and local SQLite support.

The package is framework-dependent. Set `DOTNET_ROOT` to the installed .NET 10 runtime location when it is not registered system-wide.

Verify the extracted binary package:

```powershell
.\bin\FullSpectrum.Knowledge.TestHost.exe version
.\bin\FullSpectrum.Knowledge.TestHost.exe verify-k0-05
```

The equivalent framework-host command is:

```powershell
dotnet .\bin\FullSpectrum.Knowledge.TestHost.dll verify-k0-05
```

The following source verification commands apply only to a Git clone or source-code archive containing `src/` and `tests/`:

```powershell
dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore
dotnet run --project tests/FullSpectrum.Knowledge.Tests -c Release --no-build
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify-k0-05
```

This is the first alpha package; there is no supported in-place upgrade from an earlier public release. Preserve the SQLite database and artifact directory together before any change. Rollback means stopping the candidate, restoring both items from the same backup snapshot, and running the matching historical binary. Never mix metadata and artifact snapshots.
