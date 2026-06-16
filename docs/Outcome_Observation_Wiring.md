# Outcome Observation Wiring (v1.3)

## Purpose
This connects *real-world outcomes* back into Nexus via DQS.

## Flow
Decision made →
Outcome occurs later →
Domain module calls NexusOutcomeObserver.Observe →
DQS record appended →
Trust score updated →
Policy engine trust gate adjusts automatically

## DI Registration
```csharp
builder.Services.AddSingleton<DqsEvaluator>();
builder.Services.AddSingleton<NexusOutcomeObserver>();
```

## Usage Example
```csharp
_outcomeObserver.Observe(
    decisionId,
    "inventory.transfers",
    decisionTime,
    expectedOutcome: "no_failure",
    actualOutcome: "failure",
    confidenceAtDecision: 0.82
);
```

## Governance
- No retroactive edits
- Append-only
- No model self-training
