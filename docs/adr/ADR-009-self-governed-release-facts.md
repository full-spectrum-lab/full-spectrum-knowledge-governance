# ADR-009: Self-governed release facts

Status: Accepted

The project must govern its own release claims with the same discipline applied to knowledge inputs.

`docs/release/<version>/RELEASE_MANIFEST.json` is the machine-readable source of truth for an immutable release. Tags, release pages, README, CHANGELOG, Wiki, binaries, checksums and independent reports are projections or evidence and must agree with it.

When sources conflict, the release page and immutable artifact identity have the highest authority, followed by the release manifest and Git tag. Human-facing documents must not silently override those facts. Conflicts are explicit evidence and require correction plus an audit record.

The first real case is `KG-GC-RELEASE-STATE-CONFLICT`, created from the v0.1.0-alpha mismatch between Gitee Release, README and CHANGELOG.
