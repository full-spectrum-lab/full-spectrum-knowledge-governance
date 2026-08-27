# KG6 evidence-tier follow-up

Status: BACKLOG / NON-BLOCKING

Owner decision: deferred until an independent Windows x64 + compatible .NET environment is available
Scope: future design material; no v0.2 baseline change

## Problem

An external observer may verify the public artifact, manifest, SBOM and JSON files while
being unable to execute a Windows x64 framework-dependent binary. A complete KG6 verdict
must not silently turn those unavailable runtime checks into PASS.

## Proposed evidence tiers

| Tier | Evidence | Typical result |
|---|---|---|
| 0 | Static archive safety, metadata and hashes | Portable; no runtime required |
| 1 | Portable verifier and package structure | Minimal runtime/tooling |
| 2 | Windows x64 CLI and Golden behavior | Compatible .NET runtime |
| 3 | External Library consumer, reopen, upgrade, rollback, removal | .NET SDK/runtime and isolated workspace |
| 4 | Real external pilot, authorization, redaction, deletion/exit and production ownership | Human and operational evidence |

The tiers are reporting vocabulary, not a new release gate. A future protocol should expose
the highest completed tier plus explicit `NOT_EXECUTED`, `UNKNOWN`, and `EXTERNAL` reasons.

## Current decision

Do not modify v0.2 code, schemas, release assets, or production gates. Keep the external
runtime re-verification as a P2 backlog item. Continue approved downstream development
independently of this item.
