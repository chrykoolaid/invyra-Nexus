namespace Invyra.Nexus.Dqs.Contracts;

public sealed record DqsRecord(
    string DecisionId,
    string Module,
    DateTimeOffset DecisionTime,
    DateTimeOffset? OutcomeObservedAt,
    string ExpectedOutcome,
    string ActualOutcome,
    double Confidence,
    string Verdict,
    double Score,
    IReadOnlyDictionary<string, string>? Notes = null
);
