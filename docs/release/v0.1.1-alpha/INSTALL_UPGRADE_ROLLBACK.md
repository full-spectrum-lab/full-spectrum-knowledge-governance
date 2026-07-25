# Install, source verification and rollback

Build requirement: .NET SDK 10.0.301. Runtime requirement: a compatible .NET 10 Runtime and local SQLite support. The package is framework-dependent; set `DOTNET_ROOT` when the runtime is not registered system-wide.

Binary package verification:

```powershell
.\bin\FullSpectrum.Knowledge.TestHost.exe version
.\bin\FullSpectrum.Knowledge.TestHost.exe verify-k0-05
```

Source-only verification:

```powershell
dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore
dotnet run --project tests/FullSpectrum.Knowledge.Tests -c Release --no-build
```

Preserve the SQLite database and artifact directory together. Rollback must restore both from the same snapshot and run the matching historical binary.
