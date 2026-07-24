# ADR-008: K0-05 Domain Profile and deterministic planning

Status: Accepted for K0-05 candidate

K0-05 introduces a versioned `DomainProfile` containing a taxonomy, slot definitions and exact knowledge bindings. A `SubjectProfile` is matched against that released profile to produce a deterministic `DomainResolutionPlan` for the existing FIXED_ONLY resolver.

The profile is configuration, not executable policy. Validation is fail-closed for unknown references, taxonomy cycles, duplicate identities, non-feature feature references, non-released profiles and unbound required slots. Planning never performs network access, fuzzy search, AI inference or automatic fallback.

This keeps Knowledge Governance independent of Observer and Engine. Adapter integration remains a later, separately reviewed stage.
