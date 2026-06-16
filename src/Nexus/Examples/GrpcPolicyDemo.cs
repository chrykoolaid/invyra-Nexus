using Invyra.Nexus.Contracts;
using Invyra.Nexus.Policy;
using Invyra.Nexus.Services;
using Invyra.Nexus.GrpcClient;

using var intel = new GrpcNexusIntelligenceClient("http://127.0.0.1:50051");

var policy = new NexusPolicyEngine(
    new NexusPolicyOptions(),
    new NexusKillSwitchService(),
    intel,
    new NexusAuditLog()
);

var req = new NexusDecisionRequest(
    DecisionId: Guid.NewGuid().ToString("N"),
    DecisionType: NexusDecisionType.PreFailureDetection,
    Module: "inventory.transfers",
    Timestamp: DateTimeOffset.UtcNow,
    StoreId: "S-001",
    Timezone: "Asia/Manila",
    Signals: new List<NexusSignal>
    {
        new("audit.reconciliation_present", false),
        new("transfer.retry_rate", 0.32, "6h"),
        new("patch.stack_divergence", true),
        new("logs.warning_density", 0.28, "24h"),
        new("transfers.time_to_complete_trend", 0.12, "7d")
    }
);

var result = await policy.EvaluateAsync(req, CancellationToken.None);
Console.WriteLine($"{result.Disposition} | {result.ReasonCode} | {result.Response?.Outcome} | conf={result.Response?.Confidence}");
