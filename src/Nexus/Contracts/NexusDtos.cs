namespace Invyra.Nexus.Contracts;

public enum NexusDecisionType { PreFailureDetection = 1, DecisionQualityScore = 2 }
public enum NexusOutcome { Silent = 0, Advisory = 1, HighRisk = 2, Refusal = 3, Incident = 4 }
public enum NexusRiskBand { Low = 0, Medium = 1, High = 2 }

public sealed record NexusSignal(string Type, object? Value, string? Window = null);

public sealed record NexusDecisionRequest(
    string DecisionId,
    NexusDecisionType DecisionType,
    string Module,
    DateTimeOffset Timestamp,
    string StoreId,
    string Timezone,
    IReadOnlyList<NexusSignal> Signals,
    IReadOnlyDictionary<string,string>? Context = null
);

public sealed record NexusExplanation(
    string Summary,
    IReadOnlyList<string> FactorCodes,
    IReadOnlyDictionary<string,string>? Evidence = null
);

public sealed record NexusDecisionResponse(
    string DecisionId,
    string EngineVersion,
    NexusOutcome Outcome,
    double FailureLikelihood,
    double Confidence,
    IReadOnlyList<string> DominantFactors,
    NexusExplanation Explain,
    IReadOnlyList<string> MissingSignals,
    NexusRiskBand RiskBand
);
