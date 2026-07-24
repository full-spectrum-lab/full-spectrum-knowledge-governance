# ADR-004: Observer v0.4 adapter boundary

- Status: Accepted
- Date: 2026-07-24

## Decision

The future `Integration.ObserverV04` component belongs to this repository. It may translate between frozen Observer v0.4 public contracts and Knowledge Governance contracts.

It must not:

- reference Observer internals;
- start or call Engine directly;
- alter Observer schemas or product behavior;
- generate Observer final governance conclusions;
- run before Observer v0.4 is formally frozen.
