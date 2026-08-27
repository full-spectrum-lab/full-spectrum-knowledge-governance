# ADR-012: Separate release and production-readiness gates

- Status: Accepted
- Date: 2026-08-27
- Decision: `KG-CHANGE-K6 / Option B`

## Context

The original K6 design grouped release packaging, recovery, security and
license evidence, non-author reproduction, and public preview. Release
identity and reproducibility do not prove that a deployment can be operated
safely and continuously in production.

The v0.1.0-alpha record already follows the correct discipline: technical and
release evidence can pass while `PRODUCTION_READY=NO` remains explicit.

## Decision

K6 is split into two independent gates.

### K6-A: `KG6_RELEASE_PASS`

This gate proves release identity and reproducibility, including:

- tag, commit, version, contract, and artifact identity;
- machine-readable release manifest and file digests;
- locked build and test evidence;
- Golden and negative cases;
- installation, upgrade, rollback, and removal boundaries;
- non-author clean-environment reproduction;
- licenses, notices, dependency inventory, and SBOM;
- exact platform results and explicit NOT_EXECUTED limitations.

K6-A may authorize a release or prerelease. It does not authorize a production
deployment or a `PRODUCTION_READY=YES` statement.

### K6-B: `KG6_PRODUCTION_READY`

This gate is evaluated for an exact deployment profile and requires evidence
for:

- threat model, identity, authorization, secrets, and duty separation;
- data classification, privacy, retention, deletion, and exit;
- performance, concurrency, capacity, limits, and long-running stability;
- failure injection, degradation, backup, restore, RTO, and RPO;
- logging, metrics, monitoring, alerting, incident escalation, and runbooks;
- production change approval, rollback, and accountable operators;
- target-user or target-organization acceptance.

Production readiness does not transfer automatically between local, enterprise
intranet, dedicated service, and public-cloud deployment profiles.

## Public preview

A limited public preview requires K6-A plus a separately scoped operational
gate appropriate to its users, data, access, rate limits, logging, and exit
plan. It must remain labeled `LIMITED_PREVIEW` and must not be represented as
production ready unless the applicable K6-B profile also passes.

## Consequences

- Historical v0.1.0-alpha release facts remain unchanged.
- Its production status remains `NO`; no historical evidence is rewritten.
- Future release documents must state K6-A and K6-B independently.
- AI agents and independent reviewers may verify evidence but cannot replace
  the accountable production owner.
