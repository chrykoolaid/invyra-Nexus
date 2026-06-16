using Invyra.Nexus.Contracts;

namespace NexusSimRunner.IO;

public static class RequestMapper
{
    public static NexusDecisionRequest FromCase(ScenarioCase c, string module)
    {
        var signals = c.Signals.Select(s => new NexusSignal(s.Type, s.Value, s.Window)).ToList();
        return new NexusDecisionRequest(
            DecisionId: Guid.NewGuid().ToString("N"),
            DecisionType: NexusDecisionType.PreFailureDetection,
            Module: module,
            Timestamp: c.Timestamp,
            StoreId: c.StoreId,
            Timezone: c.Timezone,
            Signals: signals
        );
    }
}
