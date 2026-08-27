# ADR-011: Knowledge resolution and hybrid comparison boundary

- Status: Accepted direction / not implemented
- Date: 2026-08-27
- Decision: `KG-CHANGE-K3 / Option B`

## Context

The original K3 design combined a three-mode router, hybrid comparison,
country data, review candidates, promotion, and degradation under a general
"governance runtime" label. Those capabilities are useful, but the label and
output boundary can overlap with Full Spectrum Engine computation and Full
Spectrum Observer observation and reporting.

The original design did not assign final business-rule execution, automatic
legal or compliance judgment, or real-world action to Knowledge Governance.
This ADR preserves that limit.

## Decision

K3 is defined as the **Knowledge Resolution and Hybrid Comparison Runtime**.

The three resolution modes remain:

- `FIXED_ONLY`: resolve exact released fixed-knowledge candidates without
  network or dynamic-snapshot access;
- `DYNAMIC_ONLY`: resolve claims within exact immutable dynamic knowledge
  snapshots while preserving source, freshness, and unverified status;
- `HYBRID`: independently resolve fixed and dynamic tracks, then compare
  normalized knowledge claims and applicability without merging provenance.

The runtime may output:

- selected, excluded, and unresolved knowledge bindings;
- match trace, coverage, missing slots, and UNKNOWN;
- consistent, dynamic supplement, applicability difference, fixed/dynamic
  conflict, possible fixed staleness, and unresolved comparison states;
- human-review and promotion candidates;
- exact input, resolver, comparator, and result digests.

It must not output:

- assumed real-world facts or general Observation;
- general business-risk scores or Engine-owned deterministic computation;
- final legal, compliance, market-entry, production, or enforcement decisions;
- authorized real-world actions or production writes;
- product-specific Consumer or Enterprise report behavior.

## Country and background data

Versioned country or market datasets may be supplied as separate background
snapshots. They must remain distinguishable from product knowledge and cannot
replace a required product knowledge slot or directly prove a compliance or
market-success conclusion.

## Capability availability

Contract enum presence is not an availability claim. Until a mode passes its
own contract, negative, Golden, replay, and independent verification gates,
requests for that mode must fail closed with an explicit unavailable status.

## Consequences

- Existing `FIXED_ONLY` behavior and Golden outputs remain unchanged.
- Future Hybrid comparators operate on knowledge claims or bindings, not final
  business findings.
- Observer and Engine integrations remain optional external adapters; the core
  must remain independently usable.
- Consumer, Enterprise, Skill, and report differences stay in downstream
  profiles, adapters, and product contracts.

This ADR records an approved future boundary. No Dynamic or Hybrid runtime is
implemented or released by this document.
