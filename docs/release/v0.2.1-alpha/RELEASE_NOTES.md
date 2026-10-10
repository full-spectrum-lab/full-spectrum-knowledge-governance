# v0.2.1-alpha release candidate notes

- Created: 2026-08-28T15:02:05+08:00 (first recorded Git introduction; original authoring time unknown)
- Last updated: 2026-10-10T23:04:00+08:00
- Timezone: Asia/Shanghai (UTC+08:00)
- Document ID: KG-V021-CANDIDATE-NOTES-001
- Document version: v1.1-candidate
- Document status: LOCAL_TOOL_CHANGE_PENDING_REVIEW_NOT_A_NEW_PACKAGE
- Responsible instance: Instance 1 / Knowledge Governance
- Scope: candidate packaging prerequisites and identity; no public contract or runtime capability change
- Fact baseline: 2517afc7ca315ec0d02f9f52bdbe3f39bc7f0a50 plus uncommitted P1 tooling changes; QPP evidence registration 3c7bd8f68adddba688e74ef1d1c6119581c31945

```text
STATUS = RELEASE_CANDIDATE
RELEASED = NO
TAG_PUBLISHED = NO
PUBLIC_ARTIFACT = NO
PRODUCTION_READY = NO
```

This candidate is limited to release identity consistency and clearer runtime
prerequisite evidence. It does not add dynamic knowledge acquisition, LLM
execution, vector retrieval, production authorization, or automatic legal or
compliance decisions.

The Windows x64 storage path uses the operating system `winsqlite3.dll`. The
DLL is a prerequisite and is not bundled in the package. `verify-k0-02` is the
required preflight before storage use.

Restore, publish, assembly, package-manifest, release-manifest, SBOM, and
`deps.json` identities are generated from one version value. Packaging and
independent verification fail closed if a `FullSpectrum.Knowledge.*`
dependency carries a different version.

The candidate remains subject to independent package verification and the
separate production-readiness gate. A second independent Windows runtime run
is still `EXTERNAL_REQUIRED`; it does not block local engineering, but no broad
Windows compatibility claim may be made without that evidence.

## Proposed candidate revision tooling (P1 only)

The modified v0.2.1 entry requires an explicit expected full commit and an
existing isolated output root. It rejects dirty inputs, unapproved origins,
incorrect SourceLink, and existing output directories. It does not alter origin
automatically or replace the old candidate. Candidate JSON uses recursively
Ordinal-sorted object keys, preserved array order, UTF-8 without BOM, and LF.
This is a bounded stable representation, not RFC8785 certification.

Candidate manifests record build environment and `NOT_EXECUTED` test/schema,
Golden and reproducibility states. They are local candidate records, not the
formal RELEASED manifest defined by the v1.0 schema. Actual verification evidence
must be separately bound to the new build commit and artifact digest; packaging
does not execute the tests or turn historical counts into current PASS.

Any future candidate revision requires its actual new committed source identity
and new ZIP digest. The old 2517afc / 78a621 candidate and evidence remain intact.
This uncommitted tooling change has only tool-level checks: no new product
candidate has been built, no tag or release created, and no second-host or CI
verification performed. Formal builds and independent package verification
still require separate authorization.
