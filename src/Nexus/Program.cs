using Invyra.Nexus.Contracts;
using Invyra.Nexus.Policy;
using Invyra.Nexus.Services;

sealed class FakeIntel : INexusIntelligenceClient
{
    public Task<NexusDecisionResponse> EvaluateAsync(NexusDecisionRequest request, CancellationToken ct)
        => Task.FromResult(new NexusDecisionResponse(
            request.DecisionId,"fake-1.0.0",NexusOutcome.Advisory,
            0.55,0.75,new List<string>{"warning_density"},
            new NexusExplanation("Advisory example", new List<string>{"warning_density"}),
            Array.Empty<string>(),NexusRiskBand.Medium));
}

var engine = new NexusPolicyEngine(
    new NexusPolicyOptions(),
    new NexusKillSwitchService(),
    new FakeIntel(),
    new NexusAuditLog()
);

var req = new NexusDecisionRequest(
    Guid.NewGuid().ToString("N"),
    NexusDecisionType.PreFailureDetection,
    "inventory.transfers",
    DateTimeOffset.UtcNow,
    "S-001","Asia/Manila",
    new List<NexusSignal>{
        new("audit.reconciliation_present", true),
        new("transfer.retry_rate", 0.2, "6h"),
        new("patch.stack_divergence", false),
        new("logs.warning_density", 0.3, "24h"),
    }
);

var result = await engine.EvaluateAsync(req, CancellationToken.None);
Console.WriteLine($"{result.Disposition} | {result.Response?.Outcome}");
