using Invyra.Nexus.Dqs.Contracts;

namespace Invyra.Nexus.Dqs.Services;

public sealed class DqsEvaluator
{
    public DqsRecord Evaluate(
        string decisionId,
        string module,
        DateTimeOffset decisionTime,
        string expected,
        string actual,
        double confidence)
    {
        var correct = expected == actual;
        var score = correct ? confidence : Math.Max(0, 1 - confidence);

        var verdict =
            score > 0.85 ? DqsVerdict.Excellent :
            score > 0.70 ? DqsVerdict.Good :
            score > 0.50 ? DqsVerdict.Degraded :
            DqsVerdict.Poor;

        var notes = new List<string>
        {
            correct ? "Prediction correct" : "Prediction incorrect",
            $"Confidence={confidence:F2}"
        };

        return new DqsRecord(
            decisionId,
            module,
            decisionTime,
            DateTimeOffset.UtcNow,
            expected,
            actual,
            confidence,
            verdict,
            Math.Round(score, 3),
            notes
        );
    }
}
