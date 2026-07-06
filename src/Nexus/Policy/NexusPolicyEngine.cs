using Invyra.Nexus.Contracts;
using Invyra.Nexus.Services;
using Invyra.Nexus.Dqs.Services;
using Invyra.Nexus.Dqs.Contracts;

namespace Invyra.Nexus.Policy;

public sealed class NexusPolicyEngine
{
    private readonly NexusPolicyOptions _opt;
    private readonly NexusKillSwitchService _kill;
    private readonly INexusIntelligenceClient _intel;
    private readonly NexusAuditLog _audit;
    private readonly IDqsStore _dqsStore;
    private readonly DqsTrustAggregator _trustAgg;

    public NexusPolicyEngine(
        NexusPolicyOptions options,
        NexusKillSwitchService killSwitch,
        INexusIntelligenceClient intelligenceClient,
        NexusAuditLog auditLog,
        IDqsStore dqsStore,
        DqsTrustAggregator trustAgg)
    {
        _opt = options;
        _kill = killSwitch;
        _intel = intelligenceClient;
        _audit = auditLog;
        _dqsStore = dqsStore;
        _trustAgg = trustAgg;
    }

    public async Task<NexusPolicyResult> EvaluateAsync(NexusDecisionRequest req, CancellationToken ct)
    {
        if (!_kill.IsEnabled())
            return new NexusPolicyResult(NexusPolicyDisposition.Suppressed,"killswitch_active","Nexus disabled");

        if (!_opt.AllowedModules.Contains(req.Module))
            return new NexusPolicyResult(NexusPolicyDisposition.Suppressed,"module_blocked","Module not allowed");

        var missing = GetMissingCoreSignals(req);
        if (missing.Count > 0)
        {
            var refusal = MakePolicyRefusal(req.DecisionId, missing);
            await _audit.AppendAsync(req, refusal, "decision.refused_policy", ct);
            return new NexusPolicyResult(NexusPolicyDisposition.Refused,"insufficient_signals","Missing required signals", refusal);
        }

        var resp = await _intel.EvaluateAsync(req, ct);

        if (resp.Confidence < _opt.ConfidenceMin)
        {
            var forced = resp with { Outcome = NexusOutcome.Refusal };
            await _audit.AppendAsync(req, forced, "decision.refused_confidence", ct);
            return new NexusPolicyResult(NexusPolicyDisposition.Refused,"low_confidence","Confidence too low", forced);
        }

        await _audit.AppendAsync(req, resp, "decision.made", ct);

        // TRUST GATE (DQS-driven):
        // - If we don't have enough scored history, default to shadow-only.
        // - If trust score drops below threshold, suppress surfacing (shadow-only).
        var moduleRecords = _dqsStore.ByModule(req.Module).ToList();
        if (moduleRecords.Count < _opt.TrustMinSampleSize)
        {
            return new NexusPolicyResult(
                NexusPolicyDisposition.ShadowOnly,
                "trust_insufficient_sample",
                $"Trust sample too small ({moduleRecords.Count}/{_opt.TrustMinSampleSize}). Shadow-only.",
                resp
            );
        }

        var trust = _trustAgg.ComputeTrustScore(moduleRecords);
        if (trust < _opt.TrustMin)
        {
            return new NexusPolicyResult(
                NexusPolicyDisposition.ShadowOnly,
                "trust_low",
                $"Trust score low ({trust:F2} < {_opt.TrustMin:F2}). Shadow-only.",
                resp
            );
        }

        return new NexusPolicyResult(NexusPolicyDisposition.SurfaceAllowed,"ok","Decision evaluated", resp);
    }

    private static List<string> GetMissingCoreSignals(NexusDecisionRequest req)
    {
        var required = new[]
        {
            "audit.reconciliation_present",
            "transfer.retry_rate",
            "patch.stack_divergence",
            "logs.warning_density"
        };

        var present = req.Signals.ToDictionary(s => s.Type, s => s.Value, StringComparer.OrdinalIgnoreCase);
        return required.Where(r => !present.ContainsKey(r) || present[r] is null).ToList();
    }

    private static NexusDecisionResponse MakePolicyRefusal(string decisionId, List<string> missing)
        => new(
            decisionId,"policy",NexusOutcome.Refusal,0,0,
            new List<string>{"insufficient_signals"},
            new NexusExplanation("Missing required signals", new List<string>{"insufficient_signals"}),
            missing,NexusRiskBand.Medium
        );
}
