namespace Invyra.Nexus.Policy;

public sealed class NexusPolicyOptions
{
    public double SilentMax { get; init; } = 0.40;
    public double AdvisoryMax { get; init; } = 0.70;
    public double ConfidenceMin { get; init; } = 0.60;
    public double TrustMin { get; init; } = 0.70;
    public int TrustMinSampleSize { get; init; } = 20;

    public TimeSpan IntelligenceTimeout { get; init; } = TimeSpan.FromSeconds(2);

    public HashSet<string> AllowedModules { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "inventory.transfers"
    };
}
