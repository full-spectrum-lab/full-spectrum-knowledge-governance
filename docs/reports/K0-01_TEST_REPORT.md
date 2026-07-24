# K0-01 Self-Test Report

> Date: 2026-07-24  
> Test type: author self-test  
> Verdict: `PASS`  
> Independent verification: `NOT YET EXECUTED`

## Environment

- Windows 10 `win-x64`
- .NET SDK `10.0.301`
- Target framework `net10.0`
- NuGet package sources cleared
- Third-party package count: 0

## Results

| Gate | Result |
|---|---|
| Restore | PASS |
| Release build | PASS, 0 warnings, 0 errors |
| Automated tests | PASS, 16/16 |
| Schema dialect audit | PASS, 4/4 |
| Synthetic fixture schema subset | PASS |
| Deterministic digest | PASS |
| Serialization round trip | PASS |
| Explicit version / latest rejection | PASS |
| UNKNOWN preservation | PASS |
| Observer/Engine project isolation | PASS |

Synthetic fixture canonical SHA-256:

```text
01311362262cd6faa8f2d202440deea2fcbffd0d243772cc727043bab40a400c
```

## Important qualification

The local validator executes the explicitly used K0 subset of JSON Schema keywords and audits the Draft 2020-12 dialect declaration. It is not represented as a complete general-purpose JSON Schema implementation.

## Next gate

Push an exact candidate commit, clone it into a clean directory, run `scripts/verify-k0-01.ps1`, compare the remote commit, confirm a clean worktree, and publish an independent verification report. No K0-02 implementation may start before this reproduction succeeds.
