using Invyra.Nexus.Dqs.Services;
using Invyra.Nexus.Dqs.Contracts;

namespace Invyra.Nexus.Outcomes;

/// <summary>
/// Records real-world outcomes and feeds them into DQS.
/// Called by domain modules once outcome is known.
/// </summary>
public sealed class NexusOutcomeObserver
{
    private readonly DqsEvaluator _evaluator;
    private readonly IDqsStore _store;

    public NexusOutcomeObserver(DqsEvaluator evaluator, IDqsStore store)
    {
        _evaluator = evaluator;
        _store = store;
    }

    public void Observe(
        string decisionId,
        string module,
        DateTimeOffset decisionTime,
        string expectedOutcome,
        string actualOutcome,
        double confidenceAtDecision)
    {
        var record = _evaluator.Evaluate(
            decisionId,
            module,
            decisionTime,
            expectedOutcome,
            actualOutcome,
            confidenceAtDecision
        );

        _store.Append(record);
    }
}
