namespace NexusSimRunner.IO;

public sealed record PackMeta(
    string PackId,
    string PackVersion,
    string Module,
    IReadOnlyList<string> CaseIndex,
    SuccessThresholds SuccessThresholds
);

public sealed record SuccessThresholds(double AccuracyMin, double FalseNegativeMax, double OverconfidenceMax);

public sealed record ScenarioCase(
    string CaseId,
    DateTimeOffset Timestamp,
    string StoreId,
    string Timezone,
    IReadOnlyList<Signal> Signals
);

public sealed record Signal(string Type, object? Value, string? Window);

public sealed record ExpectedEnvelope(Expected Expected);

public sealed record Expected(
    string DecisionType,
    string Module,
    string Outcome,
    double? ConfidenceMin,
    IReadOnlyList<string> DominantFactorsMustInclude,
    bool RefusalAllowed
);

public sealed record Comparison(bool Pass, IReadOnlyList<string> FailReasons);

public sealed record ActualResult(
    string Outcome,
    double Confidence,
    double FailureLikelihood,
    IReadOnlyList<string> DominantFactors
);

public sealed record RunRow(
    string CaseId,
    string CaseFile,
    ExpectedEnvelope Expected,
    ActualResult Actual,
    Comparison Comparison
);
