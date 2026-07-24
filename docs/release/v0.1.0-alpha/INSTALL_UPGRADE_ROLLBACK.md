# Install, upgrade and rollback

Requirements: .NET SDK/runtime 10.0.301 and local SQLite support.

Restore and verify:

```powershell
dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore
dotnet run --project tests/FullSpectrum.Knowledge.Tests -c Release --no-build
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify-k0-05
```

This is the first alpha package; there is no supported in-place upgrade from an earlier public release. Preserve the SQLite database and artifact directory together before any change. Rollback means stopping the candidate, restoring both items from the same backup snapshot, and running the matching historical binary. Never mix metadata and artifact snapshots.
