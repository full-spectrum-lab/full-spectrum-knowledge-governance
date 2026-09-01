# K2 controlled-source Golden and negative cases

Status: `INTERNAL TEST BASELINE / NOT RELEASED`

The fixture `examples/k2/controlled-source-golden.json` is synthetic and
offline-only. It is not evidence of network availability or Dynamic/Hybrid
runtime support.

Required positive path:

`DRAFT → REVIEW_REQUIRED → ACTIVE → retrieval COMPLETED → immutable snapshot`

Required negative cases:

1. revoked source cannot start a new retrieval;
2. source/version/adapter mismatch fails closed;
3. conflicting retry for one retrieval identity fails closed;
4. `PARTIAL`/`UNKNOWN` retrieval without explicit unresolved evidence fails;
5. snapshot without an existing retrieval fails closed;
6. snapshot from `FAILED` or `UNKNOWN` retrieval fails closed;
7. conflicting snapshot overwrite fails closed;
8. dynamic snapshot cannot be promoted automatically to released fixed knowledge.

Compatibility gate: all existing v0.1/v0.2 fixed-knowledge Golden cases must
remain unchanged and pass before K2 evidence is considered valid.
