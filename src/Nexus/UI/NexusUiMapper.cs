using Invyra.Nexus.Contracts;

namespace Invyra.Nexus.UI;

public enum NexusUiSurface { None, Card, Banner, Drawer }

public sealed record NexusUiPayload(
    NexusUiSurface Surface,
    string Title,
    string Message,
    IReadOnlyList<(string Label,string Intent)> Actions
);

public static class NexusUiMapper
{
    public static NexusUiPayload? Map(NexusDecisionResponse resp) =>
        resp.Outcome switch
        {
            NexusOutcome.HighRisk => new(NexusUiSurface.Banner,"High Risk",resp.Explain.Summary,
                new List<(string,string)>{("Explain","open_drawer")}),
            NexusOutcome.Advisory => new(NexusUiSurface.Card,"Advisory",resp.Explain.Summary,
                new List<(string,string)>{("Explain","open_drawer")}),
            _ => null
        };
}
