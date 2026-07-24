# K0-01 Self-Test Report

> Date: 2026-07-24  
> Test type: author self-test  
> Verdict: `PASS`  
> Clean-clone reproduction: `PASS`  
> Third-party independent verification: `NOT YET EXECUTED`

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
| Gitee clean-clone reproduction | PASS |

Synthetic fixture canonical SHA-256:

```text
01311362262cd6faa8f2d202440deea2fcbffd0d243772cc727043bab40a400c
```

## Important qualification

The local validator executes the explicitly used K0 subset of JSON Schema keywords and audits the Draft 2020-12 dialect declaration. It is not represented as a complete general-purpose JSON Schema implementation.

## Reproduction identity

- Candidate commit: `c8dbb5b7d2cb5ad4be9bc5e15d469041d0fb1a48`
- Remote: Gitee `master`
- Exact detached checkout: PASS
- Remote head match: PASS
- Worktree clean after verification: PASS

This reproduction was executed by the implementing Codex instance from a clean
remote clone. It is evidence of reproducibility, but it is not represented as
an independent third-party acceptance.

## Next gate

Freeze K0-01 behavior and hand the exact candidate to another AI instance or a
human verifier. No release may be declared before independent verification.
