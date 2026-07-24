namespace FullSpectrum.Knowledge.Contracts;

public enum KnowledgeLifecycleState
{
    Draft,
    ReviewRequired,
    Released,
    Superseded,
    Revoked
}

public enum KnowledgeBindingStatus
{
    Bound,
    Unbound,
    Unresolved,
    Excluded
}

public enum KnowledgeResolutionMode
{
    FixedOnly,
    DynamicOnly,
    Hybrid
}

public enum KnowledgeResolutionStatus
{
    Succeeded,
    Partial,
    Failed
}
