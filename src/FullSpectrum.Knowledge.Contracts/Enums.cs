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

public enum KnowledgeGranularity
{
    Industry,
    Category,
    Series,
    Model,
    Feature
}

public enum KnowledgeMatchOutcome
{
    Selected,
    Excluded,
    Unresolved
}

public enum SlotCoverageStatus
{
    Covered,
    Partial,
    Missing
}

public enum OverallCoverageStatus
{
    Complete,
    Partial,
    Insufficient
}
