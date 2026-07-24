# K0-02 Author Self-Test Report

> Date: 2026-07-24  
> Verdict: `PASS`  
> Independent verification: `NOT YET EXECUTED`

## Environment

- Windows 10 `win-x64`
- .NET SDK `10.0.301`
- system `winsqlite3`
- target `net10.0`
- third-party NuGet packages: 0

## Results

| Gate | Result |
|---|---|
| Locked restore | PASS |
| Release build | PASS, 0 warnings, 0 errors |
| Automated tests | PASS, 27/27 |
| K0-01 regression tests | PASS, 16/16 |
| K0-02 tests | PASS, 11/11 |
| Schema dialect audit | PASS, 5/5 |
| K0-02 Golden CASE | PASS |
| Audit events | PASS, 4 |
| Final lifecycle state | PASS, `REVOKED` |
| Artifact readable after revoke | PASS |
| Observer/Engine isolation | PASS |

Golden canonical SHA-256:

```text
c1a491febf7520b95af32189744d607aee455fbe10854df5245d6a7f2060529a
```

Synthetic artifact SHA-256:

```text
44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a
```

This is author self-test evidence. Final remote clean-clone reproduction and a
separate third-party K0-02 retest are still required.
