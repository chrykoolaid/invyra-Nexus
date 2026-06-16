namespace NexusSimRunner.IO;

public static class Comparator
{
    public static Comparison Compare(ActualResult actual, ExpectedEnvelope expected)
    {
        var reasons = new List<string>();
        var exp = expected.Expected;

        var expOutcome = exp.Outcome.ToUpperInvariant();
        var actOutcome = actual.Outcome.ToUpperInvariant();

        if (actOutcome != expOutcome)
        {
            if (exp.RefusalAllowed && actOutcome == "REFUSAL")
            {
                // allowed
            }
            else
            {
                reasons.Add($"Outcome mismatch: expected={expOutcome} actual={actOutcome}");
            }
        }

        if (exp.ConfidenceMin is not null && actual.Confidence + 1e-9 < exp.ConfidenceMin.Value)
            reasons.Add($"Confidence below min: expected>={exp.ConfidenceMin} actual={actual.Confidence}");

        foreach (var must in exp.DominantFactorsMustInclude ?? Array.Empty<string>())
        {
            if (!actual.DominantFactors.Contains(must))
                reasons.Add($"Missing required dominant factor: {must}");
        }

        return new Comparison(reasons.Count == 0, reasons);
    }
}
