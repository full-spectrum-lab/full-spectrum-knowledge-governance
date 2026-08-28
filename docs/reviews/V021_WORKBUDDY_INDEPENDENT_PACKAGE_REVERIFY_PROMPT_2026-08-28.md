# WorkBuddy prompt: v0.2.1-alpha independent package re-verification

You are the independent engineering reviewer for Full Spectrum Knowledge
Governance `v0.2.1-alpha`. Perform the review yourself and remain read-only
with respect to the repository and candidate artifact.

Do not modify source, schemas, manifests, ZIP contents, documentation, Git
history, configuration, or secrets. Do not commit, push, tag, create a
Release, deploy, delete project data, or replace the candidate. Temporary
audit output may be created only under a new directory in
`C:/obs-verify-evidence-hbg/`.

## Fixed review objects

- Repository:
  `C:/Users/wangjian0926/Desktop/codex专属仓库/_public_narrative_batch_b2/full-spectrum-knowledge-governance`
- Build-input commit:
  `8c5db9897b026ca84eca4a8b863acfa34e1860ec`
- Candidate ZIP:
  `artifacts/release/v0.2.1-alpha/full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip`
- Expected SHA-256:
  `9b59b40eae6c1866c214db0d6bda9214221f8755abf44f58a4c4e24d9baece5d`
- Release manifest:
  `artifacts/release/v0.2.1-alpha/RELEASE_MANIFEST.json`
- Verification entry point:
  `scripts/verify-package-v0.2.1-alpha.ps1`
- Publisher report under review:
  `docs/reports/V021_LOCAL_CANDIDATE_VERIFICATION_2026-08-28.md`

The current HEAD may contain only a later evidence-report commit. Do not
assume HEAD is the binary build commit. Verify that the build-input commit
exists and that package manifest, executable identity, and dependency
metadata all bind to it. Report any source changes after the build-input
commit separately.

## Required checks

1. Record current branch, HEAD, worktree state, remotes, and whether the fixed
   build-input commit exists.
2. Recompute the ZIP SHA-256. Reject any mismatch.
3. Inspect ZIP paths before extraction and reject absolute paths, drive paths,
   `..` traversal, duplicate normalized names, or entries escaping the audit
   root.
4. Verify `PACKAGE_MANIFEST.json`, external `RELEASE_MANIFEST.json`,
   `SHA256SUMS`, and the SPDX SBOM file hashes.
5. Confirm both manifests say:
   `version=v0.2.1-alpha`, `production_ready=false`,
   `windows_system_sqlite=winsqlite3.dll`, and
   `native_sqlite_external_reverify=EXTERNAL_REQUIRED`.
6. Inspect every packaged `*.deps.json`. Every
   `FullSpectrum.Knowledge.*/*` library identity must end in
   `/0.2.1-alpha`; list every identity in the report.
7. On Windows x64 with a compatible .NET 10 runtime, run the package verifier
   into a new audit root. Independently capture `version`, `verify-k0-02`, and
   `verify-k0-05`. `verify-k0-02` must report
   `native_sqlite=winsqlite3` and no errors.
8. Confirm the Library consumer results: load, v0.1 storage reopen, contract
   upgrade, snapshot rollback, and removal probe.
9. Run two expected-failure cases into separate new audit roots:
   a wrong external SHA-256, and the `v0.2.0-alpha` ZIP presented to the
   v0.2.1 verifier. Both must fail closed for the documented reason.
10. Recompute the unchanged public v0.2.0 ZIP SHA-256 and confirm it remains
    `730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c`.
11. Review the publisher report against actual evidence. Do not treat
    `NOT_EXECUTED` or `EXTERNAL_REQUIRED` as PASS.

Use the configured .NET SDK/runtime if available. If runtime execution is not
possible, mark only those checks `NOT_EXECUTED`; do not inject a Python SQLite
DLL, rebuild the package, or infer PASS from static evidence.

## Required decision vocabulary

Return all of these explicitly:

```text
V021_ARTIFACT_INTEGRITY = PASS | FAIL | NOT_EXECUTED
V021_METADATA_IDENTITY = PASS | FAIL | NOT_EXECUTED
V021_WINDOWS_RUNTIME = PASS | FAIL | NOT_EXECUTED
V021_LIBRARY_CONSUMER = PASS | FAIL | NOT_EXECUTED
V021_NEGATIVE_GATES = PASS | FAIL | NOT_EXECUTED
V021_INDEPENDENT_REVERIFY = PASS | FAIL | PASS_WITH_FINDINGS
V021_PUBLIC_RELEASE = NOT_AUTHORIZED
V021_PRODUCTION_READY = NO
NATIVE_SQLITE_EXTERNAL_REVERIFY = EXTERNAL_REQUIRED
```

Findings must be ordered by severity with file/evidence references. Separate
code defects, packaging defects, environment limitations, and missing
external evidence. Deliver:

- `C:/Users/wangjian0926/WorkBuddy/2026-08-28/kg-v021-independent-package-reverification.md`
- a machine-readable JSON result beside it;
- SHA-256 values for both deliverables.
