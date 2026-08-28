# v0.2.0-alpha native SQLite runtime reproduction

- Date: 2026-08-28
- Scope: read-only runtime reproduction of the published v0.2.0-alpha Windows x64 package
- Package: `artifacts/release/v0.2.0-alpha/full-spectrum-knowledge-governance-v0.2.0-alpha-win-x64.zip`
- Package SHA-256: `730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c`
- Source baseline: `42733a87745e5c60eddf0eb48ffe33545805805b`

## Purpose

An external black-box report observed that storage tests passed only after a
Python-provided `sqlite3.dll` was injected into its process, and therefore
raised a medium-level concern that a clean Windows installation might fail to
load SQLite. This record tests the published package in a minimal local
runtime environment without a source build or Python injection.

## Environment

- OS: Windows x64
- .NET host: the bundled standalone runtime host at
  `WorkBuddy/2026-08-21-16-56-00/观测者 UAT 包 v0.3.0 m3/runtime/dotnet/dotnet.exe`
- Runtime versions available: .NET 10.0.9 and 10.0.10
- SDK: none
- `PATH`: only the .NET runtime directory, `C:\Windows\System32`, and
  `C:\Windows`
- `DOTNET_ROOT` and `DOTNET_ROOT_X64`: set to the standalone runtime directory
- `PYTHONPATH` and `PYTHONHOME`: unset
- System SQLite: `C:\Windows\System32\winsqlite3.dll` present

The test invoked the package DLL through the standalone .NET host. It did not
use the development SDK, the source tree, a Python interpreter, or a Python
SQLite DLL.

## Commands and results

From the extracted package `bin/` directory:

```text
FullSpectrum.Knowledge.TestHost.dll version
VERSION=0.2.0-alpha+42733a87745e5c60eddf0eb48ffe33545805805b
COMMIT=42733a87745e5c60eddf0eb48ffe33545805805b
TARGET=win-x64
BUILD_CONFIGURATION=Release
PRODUCTION_READY=NO

FullSpectrum.Knowledge.TestHost.dll verify-k0-02
status=PASS
native_sqlite=winsqlite3
final_state=REVOKED
audit_events=4
errors=[]

FullSpectrum.Knowledge.TestHost.dll verify-k0-05
status=PASS
errors=[]
```

Exit codes were `0`, `0`, and `0` respectively.

## Finding

`winsqlite3.dll` loading and the K0-02 storage path were reproduced
successfully in this minimal environment. The external report's medium-level
statement that the package necessarily fails on a clean Windows host is **not
reproduced locally**.

This does not prove every Windows image has the same system component. It does
change the evidence classification:

```text
NATIVE_SQLITE_LOCAL_REPRODUCTION = PASS
EXTERNAL_CLEAN_WINDOWS_REPRODUCTION = REQUIRED
EXTERNAL_REPORT_OBSERVATION = NOT_REPRODUCED_LOCALLY
```

The correct next action is a second independent run on a separately prepared
Windows x64 host with only a compatible .NET runtime and the operating-system
SQLite component. The current v0.2.0-alpha Release remains frozen; this record
does not authorize a baseline change.

## Separate confirmed observation

The package `deps.json` still carries the `0.1.1-alpha` project identity while
the release scripts request `0.2.0-alpha`/`0.2.0.0` during publish. This is a
real release metadata consistency issue and should be corrected in the next
candidate, independently of the SQLite result.

## Evidence boundary

- No source files, schemas, release assets, or tags were modified.
- No commit, push, Release, deployment, secret, or deletion operation was run.
- This is a runtime reproduction record, not a production-readiness approval.
