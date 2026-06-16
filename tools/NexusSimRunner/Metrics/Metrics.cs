using NexusSimRunner.IO;

namespace NexusSimRunner.Metrics;

public sealed record RunMetrics(
    double Accuracy,
    double FalsePositiveRate,
    double FalseNegativeRate,
    double OverconfidenceRate,
    double AvgConfidence,
    int TotalCases
);

public static class MetricComputer
{
    public static RunMetrics Compute(IReadOnlyList<RunRow> rows)
    {
        var total = rows.Count;
        if (total == 0) return new RunMetrics(0,0,0,0,0,0);

        var pass = rows.Count(r => r.Comparison.Pass);
        var accuracy = (double)pass / total;

        int fp = 0, fn = 0, over = 0;
        foreach (var r in rows)
        {
            var exp = r.Expected.Expected.Outcome.ToUpperInvariant();
            var act = r.Actual.Outcome.ToUpperInvariant();
            var conf = r.Actual.Confidence;

            if (exp != act)
            {
                if (exp == "HIGH_RISK" && (act == "ADVISORY" || act == "SILENT")) fn++;
                if ((exp == "ADVISORY" || exp == "SILENT") && act == "HIGH_RISK") fp++;
                if (conf >= 0.80) over++;
            }
        }

        var avgConf = rows.Average(r => r.Actual.Confidence);

        return new RunMetrics(
            Accuracy: Math.Round(accuracy, 4),
            FalsePositiveRate: Math.Round((double)fp / total, 4),
            FalseNegativeRate: Math.Round((double)fn / total, 4),
            OverconfidenceRate: Math.Round((double)over / total, 4),
            AvgConfidence: Math.Round(avgConf, 4),
            TotalCases: total
        );
    }
}

public static class Gate
{
    public static List<string> Check(SuccessThresholds th, RunMetrics m)
    {
        var fails = new List<string>();
        if (m.Accuracy < th.AccuracyMin) fails.Add($"accuracy {m.Accuracy} < {th.AccuracyMin}");
        if (m.FalseNegativeRate > th.FalseNegativeMax) fails.Add($"false_negative_rate {m.FalseNegativeRate} > {th.FalseNegativeMax}");
        if (m.OverconfidenceRate > th.OverconfidenceMax) fails.Add($"overconfidence_rate {m.OverconfidenceRate} > {th.OverconfidenceMax}");
        return fails;
    }
}
