# v0.2.0-alpha test report

- Date: 2026-08-27
- Platform executed: Windows x64
- .NET SDK: repository-pinned 10.0.301 via local `.dotnet10`
- Test data: synthetic only
- Overall local result: PASS
- Independent re-verification: PASS_WITH_FINDINGS / NO BLOCKERS
- Linux/macOS: NOT EXECUTED

## Command

```powershell
$env:DOTNET_EXE='C:\Users\wangjian0926\.dotnet10\dotnet.exe'
powershell -ExecutionPolicy Bypass -File scripts\verify-v0.2.0-alpha.ps1
```

## Results

| Check | Result |
|---|---|
| locked restore | PASS |
| Release build | PASS, 0 warnings, 0 errors |
| combined v0.1 regression + v0.2 tests | PASS, 92/92 |
| K0-05 Golden | PASS |
| v1.0 Schema/Golden SHA-256 manifest | PASS |
| lifecycle positive/negative | PASS |
| restart and immutable history | PASS |
| two-connection SQLite terminal-state concurrency | PASS |
| Library API contract | PASS |
| Adapter SPI reference round-trip | PASS |
| unknown contract and non-fixed fail-closed | PASS |
| Observer/Engine project-reference isolation | PASS |
| `git diff --check` | PASS |
| WorkBuddy independent re-verification | PASS_WITH_FINDINGS / engineering gate YES |

The gate emitted:

```text
KG_V0_1_REGRESSION=PASS
KG_V0_2_LIFECYCLE=PASS
KG_V0_2_LIBRARY_API=PASS
KG_V0_2_ADAPTER_SPI=PASS
KG_V0_2_COMPATIBILITY=PASS
KG_V0_2_INTERNAL_ENGINEERING=PASS_PENDING_INDEPENDENT_REVERIFICATION
```

## Evidence integrity

| File | SHA-256 |
|---|---|
| `docs/reports/V02_FULL_GATE_2026-08-27.txt` | `6ff4d04907051588a2f71fb70c3147abc77006de331e1dac577cf528b8548764` |
| `docs/compatibility/v0.1.x-baseline-sha256.json` | `a9fbf93540678fc5d36c659642f8c5ba06bee08e8297c19c9b7d4dfd0f02010d` |

The raw log contains 92 `[PASS]` lines, the test total, Golden JSON output,
and the six gate status lines. The machine-readable negative-case index is
`docs/reports/V02_NEGATIVE_RESULTS_2026-08-27.json`.

Test lineage is 75 unchanged v0.1 tests, one lifecycle assertion upgraded for
the approved additive v1.1 state, and 16 new v0.2 tests. WorkBuddy independently
re-ran the suite and all other gates; its acceptance record is
`docs/reports/V02_INDEPENDENT_REVERIFICATION_RECORD_2026-08-27.md`.

## Limitations

The repository's `SchemaSubsetValidator` validates the subset used by existing
tests; all v1.0 and v1.1 schema documents also parsed as JSON successfully.
No separate standards-complete JSON Schema validator was available in the
workspace, so that check is `NOT_EXECUTED`, not silently treated as PASS.

This report proves local internal engineering behavior only. It does not prove
package installation, release identity, public availability, external target
user acceptance, legal erasure, production security, deployment, or Linux/macOS
portability.
