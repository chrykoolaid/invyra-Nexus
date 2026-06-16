using Invyra.Nexus.Contracts;

namespace Invyra.Nexus.Policy;

public enum NexusPolicyDisposition
{
    Suppressed = 0,
    ShadowOnly = 1,
    SurfaceAllowed = 2,
    Refused = 3,
    Incident = 4
}

public sealed record NexusPolicyResult(
    NexusPolicyDisposition Disposition,
    string ReasonCode,
    string ReasonSummary,
    NexusDecisionResponse? Response = null
);
