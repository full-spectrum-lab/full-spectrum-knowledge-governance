# v0.2.0-alpha release candidate notes

```text
STATUS = RELEASE_CANDIDATE
RELEASED = NO
TAG_PUBLISHED = NO
PUBLIC_ARTIFACT = NO
PRODUCTION_READY = NO
CANDIDATE_TEST_SCOPE = WINDOWS_X64
OFFICIAL_RELEASE_VERIFICATION = NO
```

This candidate completes the approved v0.2.0-alpha knowledge-governance scope:

- fixed lifecycle transitions, immutable artifacts, audit and replay;
- the narrow in-process `FIXED_ONLY` Library API and Adapter SPI;
- v0.1.x storage and contract compatibility;
- v1.0 and v1.1 schemas, deterministic synthetic examples, and K0-05 Golden verification;
- a separately published TestHost verification CLI and Library assembly set.

The package is framework-dependent and Windows x64 only for this candidate. It does not
enable dynamic knowledge acquisition, LLM execution, vector retrieval, production
authorization, or automatic legal/compliance decisions. All examples are synthetic.
`PRODUCTION_READY` remains `NO`; no Git tag, GitHub/Gitee Release, or deployment is implied.
