using Invyra.Nexus.Contracts;

namespace Invyra.Nexus.Services;

public class NexusAuditLog
{
    public virtual Task AppendAsync(NexusDecisionRequest req, NexusDecisionResponse? resp, string evt, CancellationToken ct)
        => Task.CompletedTask;
}
