using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Library;

public sealed class ContractFixedKnowledgeAdapter
    : IFixedKnowledgeAdapter<ContractFixedRequest, ContractFixedResponse>
{
    public FixedKnowledgeCall ToFixedCall(ContractFixedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new FixedKnowledgeCall(request.Request, request.Candidates);
    }

    public ContractFixedResponse FromFixedResult(KnowledgeResolutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ContractFixedResponse(result);
    }
}

public static class FixedKnowledgeAdapterExtensions
{
    public static TExternalResponse Resolve<TExternalRequest, TExternalResponse>(
        this IKnowledgeLibrary library,
        TExternalRequest request,
        IFixedKnowledgeAdapter<TExternalRequest, TExternalResponse> adapter)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(adapter);
        var call = adapter.ToFixedCall(request);
        return adapter.FromFixedResult(library.ResolveFixed(call));
    }
}
