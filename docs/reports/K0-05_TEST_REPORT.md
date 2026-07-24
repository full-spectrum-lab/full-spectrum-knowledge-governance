# K0-05 test report

Candidate self-verification:

- Locked restore: pass
- Release build: pass, 0 warnings / 0 errors
- Automated tests: 70/70 (55 K0-01–K0-04 regression + 15 K0-05)
- K0-05 Golden: `examples/k0-05/domain-resolution-plan.golden.json`
- Schema count: 12
- Plan digest: `b6cd4e6647ef32f9ac4e0bee15e6e292340148231959b179862a49ac332e9087`
- Golden file digest: `e7b4282e40b0a1e3799de62304a1e17f2149acbccb8a0c2b1becee0ba492723d`
- Dedicated command: `scripts/verify-k0-05.ps1`
- Platforms: Windows executed; Linux/macOS not executed

The final commit, clean-clone evidence and exact digests are recorded after the candidate is pushed. This report is not an independent third-party verdict.
