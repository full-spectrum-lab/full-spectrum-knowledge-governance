# Knowledge Governance v0.2.1-alpha
# Emergency Handoff and Recovery Record

Date: 2026-08-31 (Asia/Shanghai)
Owner: Project Owner (wangjian0926)
Primary operator: Codex
Independent reviewer: WorkBuddy

## 1. Purpose

This document is the recovery entry point after an unexpected Codex or tool
disconnection. It records the verified project state, the remaining work, the
authorization boundary, and the exact order for resuming work. A successor
agent must read this file before taking action.

## 2. Current repository

- Local path:
  `C:\Users\wangjian0926\Desktop\codex专属仓库\_public_narrative_batch_b2\full-spectrum-knowledge-governance`
- Current branch: `main`
- Current HEAD at handoff: `2517afc` (`fix: synchronize sqlite disposal and finalize`)
- Remote publication status: do not assume any new push, tag, or release.
- Baseline: `v0.2.0-alpha` remains frozen and must not be modified.

Before any write, verify `git status`, `git branch --show-current`, and
`git rev-parse HEAD`. Preserve unrelated user changes.

## 3. Verified work already completed

The local v0.2.1-alpha repair and candidate verification were completed before
the interruption:

- Source tests: `92/92` passed.
- Release build: `0 warnings / 0 errors`.
- All 11 `FullSpectrum.Knowledge.*` dependency identities resolve to
  `0.2.1-alpha`.
- Native SQLite preflight: PASS using operating-system `winsqlite3.dll`.
- Golden, library consumer, v0.1 storage reopen, contract upgrade, snapshot
  rollback, removal probe, and negative SHA checks passed.
- Passing an old v0.2.0 package to the v0.2.1 verifier failed as expected.
- Repeated builds from the same commit produced identical ZIP SHA-256 values.

Candidate package:

`artifacts/release/v0.2.1-alpha/full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip`

Candidate SHA-256:

`9b59b40eae6c1866c214db0d6bda9214221f8755abf44f58a4c4e24d9baece5d`

Build input commit recorded in the candidate evidence:

`8c5db9897b026ca84eca4a8b863acfa34e1860ec`

Status vocabulary:

```text
V021_LOCAL_CANDIDATE = PASS_PENDING_INDEPENDENT_REVERIFY
V021_PUBLIC_RELEASE = NOT_AUTHORIZED
V021_PRODUCTION_READY = NO
NATIVE_SQLITE_EXTERNAL_REVERIFY = EXTERNAL_REQUIRED
```

## 4. WorkBuddy evidence

Expected independent-reverification outputs:

- `C:\Users\wangjian0926\WorkBuddy\2026-08-28\kg-v021-independent-package-reverification.md`
- `C:\Users\wangjian0926\WorkBuddy\2026-08-28\kg-v021-independent-package-reverification.json`

The Chinese audit prompt is versioned in:

`docs/reviews/V021_WORKBUDDY_INDEPENDENT_PACKAGE_REVERIFY_PROMPT_2026-08-28-ZH.md`

The prompt requires read-only black-box verification, explicit
PASS/FAIL/UNKNOWN/NOT_EXECUTED distinctions, SHA-256 for both reports, and no
push, release, deployment, secret changes, or destructive actions.

## 5. Remaining work, in order

1. Read and hash the two WorkBuddy reports, then compare their claims with the
   candidate manifest, package hash, and local evidence.
2. Record an evidence decision:
   `PASS_WITH_EXTERNAL_LIMITS`, `REVERIFY_REQUIRED`, or
   `BLOCKED_BY_MISSING_ARTIFACT`.
3. If a factual discrepancy exists, prepare a narrowly scoped repair note and
   run the affected gates again. Do not change the frozen v0.2.0 baseline.
4. Keep the second-host Windows SQLite run as an external, non-blocking
   evidence task unless the Owner explicitly changes that decision.
5. Present a release recommendation to the Owner. A public release requires a
   fresh explicit authorization; previous local implementation approval does
   not authorize push, tag, GitHub/Gitee Release, deployment, or production use.

## 6. Stop conditions requiring Owner authorization

Stop and ask the Owner before:

- pushing to GitHub or Gitee;
- creating or moving tags;
- creating a Release or publishing a download;
- deploying or enabling production use;
- changing secrets, tokens, credentials, or MCP configuration;
- changing the frozen v0.2.0 baseline;
- deleting, overwriting, or migrating material data;
- changing the production-readiness gate or converting EXTERNAL/UNKNOWN into
  PASS.

## 7. Recovery communication protocol

Every resumed session must begin with a short status message containing:

1. whether the repository and candidate artifact were found;
2. the current HEAD and worktree state;
3. the last completed evidence step;
4. the next non-destructive step;
5. any authorization required.

Every completed step must leave a durable record in `docs/reports/` or
`docs/reviews/`, including date, operator, inputs, result vocabulary, and
SHA-256 where applicable. Chat messages are not the system of record.

## 8. Handoff conclusion

The interruption did not invalidate the local engineering work. The candidate
is locally verified but is not publicly released and is not production-ready.
The next safe action is independent evidence reconciliation, followed by a
clear Owner decision.
