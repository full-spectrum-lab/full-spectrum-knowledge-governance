# v0.2.1-alpha release candidate notes

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
