# K0-01 Clean-Clone Reproduction Report

> Date: 2026-07-24  
> Result: `PASS`  
> Classification: implementing-instance clean-clone reproduction  
> Third-party independent verification: `NOT YET EXECUTED`  
> Release: `NOT CREATED`

## Candidate

- Repository: `full-spectrum/full-spectrum-knowledge-governance`
- Branch: `master`
- Candidate commit: `c8dbb5b7d2cb5ad4be9bc5e15d469041d0fb1a48`
- Remote head at verification: `c8dbb5b7d2cb5ad4be9bc5e15d469041d0fb1a48`
- Clone path was separate from the implementation worktree.

## Procedure

1. Clone the Gitee repository into an empty directory.
2. Check out the exact candidate commit in detached HEAD state.
3. Run `scripts/verify-k0-01.ps1` using .NET SDK `10.0.301`.
4. Confirm locked restore, Release build, tests, schema audit, and fixture digest.
5. Compare local HEAD with remote `master`.
6. Confirm the reproduced worktree remains clean.

## Results

| Gate | Result |
|---|---|
| Exact checkout | PASS |
| Locked restore | PASS |
| Release build | PASS, 0 warnings, 0 errors |
| Automated tests | PASS, 16/16 |
| Schema documents | PASS, 4/4 Draft 2020-12 |
| Synthetic fixture | PASS |
| Fixture canonical SHA-256 | `01311362262cd6faa8f2d202440deea2fcbffd0d243772cc727043bab40a400c` |
| Observer/Engine project isolation | PASS |
| Remote head match | PASS |
| Worktree clean | PASS |

## Boundary statement

The candidate does not change Observer requirements, product code, schemas, or
test baseline, and does not change Engine. No database, network service, LLM,
real regulatory knowledge, Observer adapter, tag, or public release is included.

## Verdict

`K0-01 CLEAN-CLONE REPRODUCTION = PASS`

This verdict does not replace third-party independent verification and does not
authorize a release.
