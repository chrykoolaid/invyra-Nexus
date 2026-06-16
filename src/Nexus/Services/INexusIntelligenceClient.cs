using Invyra.Nexus.Contracts;

namespace Invyra.Nexus.Services;

public interface INexusIntelligenceClient
{
    Task<NexusDecisionResponse> EvaluateAsync(NexusDecisionRequest request, CancellationToken ct);
}
