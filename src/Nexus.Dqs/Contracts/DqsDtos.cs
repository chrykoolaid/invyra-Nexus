namespace Invyra.Nexus.Dqs.Contracts;

public enum DqsVerdict { Excellent, Good, Degraded, Poor }

public sealed record DqsRecord(
    string DecisionId,
    string Module,
    DateTimeOffset DecisionTime,
    DateTimeOffset? OutcomeObservedAt,
    string ExpectedOutcome,
    string ActualOutcome,
    double ConfidenceAtDecision,
    DqsVerdict Verdict,
    double Score,
    IReadOnlyList<string> Notes
);
