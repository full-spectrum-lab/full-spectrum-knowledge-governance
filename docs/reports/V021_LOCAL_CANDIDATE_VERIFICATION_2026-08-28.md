# v0.2.1-alpha local candidate verification

- Date: 2026-08-28 (Asia/Shanghai)
- Build-input commit: `8c5db9897b026ca84eca4a8b863acfa34e1860ec`
- Status: `LOCAL_CANDIDATE_PASS_PENDING_INDEPENDENT_REVERIFY`
- Public Release: `NOT_AUTHORIZED / NOT_CREATED`
- Production readiness: `NO`

## Scope

This candidate repairs release identity propagation and makes the Windows
SQLite runtime prerequisite explicit. It does not change the knowledge
contracts, schemas, runtime capabilities, or the frozen public
`v0.2.0-alpha` ZIP.

The evidence-archive commit containing this report is intentionally separate
from the build-input commit. The package manifest, executable, and dependency
metadata identify the build-input commit above.

## Environment

- Windows 10 `10.0.19045`, x64
- .NET SDK `10.0.301`
- .NET runtime `10.0.9`
- Windows system SQLite: `winsqlite3.dll`
- Source working tree: clean at candidate construction

## Candidate identity

| Item | Value |
| --- | --- |
| ZIP | `artifacts/release/v0.2.1-alpha/full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip` |
| SHA-256 | `9b59b40eae6c1866c214db0d6bda9214221f8755abf44f58a4c4e24d9baece5d` |
| Release commit | `8c5db9897b026ca84eca4a8b863acfa34e1860ec` |
| Target | `win-x64` |
| Package files | 63 |
| Public tag | `NOT_CREATED` |

The previously published `v0.2.0-alpha` ZIP remains unchanged at SHA-256
`730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c`.

## Results

| Gate | Result | Evidence |
| --- | --- | --- |
| PowerShell parse and `git diff --check` | PASS | Four package/verification scripts parsed |
| Release build | PASS | 0 warnings, 0 errors |
| Source tests | PASS | 92/92 |
| v0.1 regression and v0.2 compatibility | PASS | Source gate output |
| CLI and Library dependency identity | PASS | All 11 `FullSpectrum.Knowledge.*` identities are `0.2.1-alpha` |
| Embedded executable identity | PASS | Version and commit match package manifest |
| External ZIP SHA-256 | PASS | Matches candidate manifest |
| Package `SHA256SUMS` | PASS | 63 files, 0 failures |
| Windows native SQLite preflight | PASS | `native_sqlite=winsqlite3` |
| K0-05 Golden | PASS | No errors |
| Independent Library consumer | PASS | Load, v0.1 reopen, contract upgrade, snapshot rollback |
| Removal probe | PASS | Extracted probe removed without touching source data |
| Wrong external hash | EXPECTED_FAIL | Rejected with `Archive SHA-256 mismatch` |
| v0.2.0 package presented as v0.2.1 | EXPECTED_FAIL | Rejected with `Release manifest identity mismatch` |
| Same-commit ZIP byte reproducibility | PASS | Two builds produced the same SHA-256 |
| Standard JSON Schema validator | NOT_EXECUTED | Existing subset validator only |
| Independent second Windows host | EXTERNAL_REQUIRED | Deferred, non-blocking for local engineering |

`RELEASE_MANIFEST.json` is generated before a second build can be compared, so
its embedded reproducibility field conservatively remains
`ARCHIVE_BYTE_REPRODUCIBILITY_NOT_EXECUTED`. The post-build PASS above is a
separate verification fact recorded by this report; the candidate ZIP and its
hash were not changed after the comparison.

## Evidence paths

- Source gate log:
  `C:/obs-verify-evidence-hbg/kg-v021-source-gate-20260828.log`
- Package verification JSON:
  `C:/obs-verify-evidence-hbg/kg-v021-package-audit-20260828/package-verification.json`
- Metadata probe:
  `C:/obs-verify-evidence-hbg/kg-v021-metadata-probe-20260828/`
- v0.2.0 verifier regression:
  `C:/obs-verify-evidence-hbg/kg-v020-verifier-regression-sqlite-20260828/`
- Negative hash audit root:
  `C:/obs-verify-evidence-hbg/kg-v021-negative-hash-20260828/`
- Old-as-new identity audit root:
  `C:/obs-verify-evidence-hbg/kg-v021-negative-old-identity-20260828/`

## Boundary and decision

```text
V021_LOCAL_CANDIDATE = PASS_PENDING_INDEPENDENT_REVERIFY
V021_PUBLIC_RELEASE = NOT_AUTHORIZED
V021_PRODUCTION_READY = NO
NATIVE_SQLITE_LOCAL_REPRODUCTION = PASS
NATIVE_SQLITE_EXTERNAL_REVERIFY = EXTERNAL_REQUIRED
```

No push, tag, GitHub/Gitee Release, deployment, secret operation, or
production authorization was performed. The second-host task remains useful
external evidence, but its absence is not converted into either PASS or FAIL
and does not block subsequent source engineering.
