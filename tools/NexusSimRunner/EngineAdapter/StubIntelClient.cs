using Invyra.Nexus.Contracts;
using Invyra.Nexus.Services;

namespace NexusSimRunner;

public sealed class StubIntelClient : INexusIntelligenceClient
{
    private readonly string _version;
    public StubIntelClient(string engineVersion) => _version = "stub-" + engineVersion;

    public Task<NexusDecisionResponse> EvaluateAsync(NexusDecisionRequest request, CancellationToken ct)
    {
        var sig = request.Signals.ToDictionary(s => s.Type, s => s.Value, StringComparer.OrdinalIgnoreCase);
        var core = new[] { "audit.reconciliation_present", "transfer.retry_rate", "patch.stack_divergence", "logs.warning_density" };
        var missing = core.Where(k => !sig.ContainsKey(k) || sig[k] is null).ToList();
        if (missing.Count > 0)
        {
            return Task.FromResult(new NexusDecisionResponse(
                request.DecisionId, _version, NexusOutcome.Refusal, 0, 0,
                new List<string> { "insufficient_signals" },
                new NexusExplanation("Nexus cannot assess failure risk yet. Required signals are missing or incomplete.", new List<string>{"insufficient_signals"}),
                missing, NexusRiskBand.Medium
            ));
        }

        bool recon = sig["audit.reconciliation_present"] is bool b && b;
        double retry = Convert.ToDouble(sig["transfer.retry_rate"]);
        bool drift = sig["patch.stack_divergence"] is bool bb && bb;
        double warn = Convert.ToDouble(sig["logs.warning_density"]);
        double ttc = sig.TryGetValue("transfers.time_to_complete_trend", out var t) && t is not null ? Convert.ToDouble(t) : 0.0;

        var score = 0.0;
        var factors = new List<string>();
        if (!recon) { score += 0.40; factors.Add("missing_reconciliation_audit"); }
        if (drift) { score += 0.30; factors.Add("patch_stack_divergence"); }
        if (retry >= 0.18) { score += 0.20; factors.Add("elevated_retry_rate"); }
        if (warn >= 0.20) { score += 0.15; factors.Add("warning_density"); }
        if (ttc >= 0.05) { score += 0.10; factors.Add("time_to_complete_worsening"); }

        NexusOutcome outcome;
        double conf;
        NexusRiskBand band;
        string summary;

        if (score < 0.40) { outcome = NexusOutcome.Silent; conf = 0.65; band = NexusRiskBand.Low; summary = "No elevated pre-failure risk detected."; }
        else if (score < 0.70) { outcome = NexusOutcome.Advisory; conf = 0.72; band = NexusRiskBand.Medium; summary = "Some pre-failure signals detected; monitor and verify safeguards."; }
        else { outcome = NexusOutcome.HighRisk; conf = 0.88; band = NexusRiskBand.High; summary = "High likelihood of failure under current conditions."; }

        return Task.FromResult(new NexusDecisionResponse(
            request.DecisionId, _version, outcome, Math.Clamp(score,0,1), conf,
            factors.Take(4).ToList(),
            new NexusExplanation(summary, factors.Take(8).ToList()),
            Array.Empty<string>(), band
        ));
    }
}
