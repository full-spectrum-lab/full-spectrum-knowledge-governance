# K2 controlled sources and snapshots acceptance matrix

Status: `DRAFT / NOT EXECUTED / NOT A RELEASE GATE`

Authority: `docs/adr/ADR-010-controlled-knowledge-sources-and-snapshots.md`

Scope: future K2 planning only; v0.2.0-alpha is excluded

| ID | Future check | Required evidence | Current status |
|---|---|---|---|
| K2-01 | Exact source identity and semantic version | registration schema + digest | NOT IMPLEMENTED |
| K2-02 | Terms/license and access-policy declaration | source evidence record | NOT IMPLEMENTED |
| K2-03 | ACTIVE-only retrieval gate | revoked/draft negative cases | NOT IMPLEMENTED |
| K2-04 | Adapter allow-list and bounded request | policy and negative tests | NOT IMPLEMENTED |
| K2-05 | Sanitization and normalization determinism | fixture digests and replay | NOT IMPLEMENTED |
| K2-06 | Immutable canonical artifacts | content-addressed artifact test | NOT IMPLEMENTED |
| K2-07 | Immutable dynamic snapshot | snapshot digest and replay test | NOT IMPLEMENTED |
| K2-08 | Selected/excluded/unresolved/UNKNOWN survival | partial and contradiction fixtures | NOT IMPLEMENTED |
| K2-09 | Change relationship between snapshots | exact parent/as-of test | NOT IMPLEMENTED |
| K2-10 | Idempotent same retrieval identity | same-input retry test | NOT IMPLEMENTED |
| K2-11 | Conflicting retry fail-closed | changed digest/outcome negative test | NOT IMPLEMENTED |
| K2-12 | Source revoke preserves history | old snapshot readable, new retrieval blocked | NOT IMPLEMENTED |
| K2-13 | No automatic promotion to fixed knowledge | promotion boundary negative test | NOT IMPLEMENTED |
| K2-14 | No Observer/Engine coupling | dependency and runtime inspection | NOT IMPLEMENTED |
| K2-15 | Secrets and unrelated personal data excluded | fixture/data-classification review | NOT IMPLEMENTED |
| K2-16 | Independent review on exact platform | external report and hashes | NOT IMPLEMENTED |

No row in this matrix is evidence of a shipped capability. A future K2 release
must add positive, negative, replay, privacy, license and independent-verification
evidence before changing any row to `PASS`.
