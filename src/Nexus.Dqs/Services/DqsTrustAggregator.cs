using Invyra.Nexus.Dqs.Contracts;

namespace Invyra.Nexus.Dqs.Services;

public sealed class DqsTrustAggregator
{
    public double ComputeTrustScore(IEnumerable<DqsRecord> records)
    {
        var list = records.ToList();
        if (!list.Any()) return 0.0;

        return Math.Round(list.Average(r => r.Score), 3);
    }
}
