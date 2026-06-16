# Nexus Decision Quality Score (DQS) v1

This service evaluates Nexus decisions *after outcomes are known*.

## Responsibilities
- Score accuracy vs confidence
- Produce verdicts (Excellent → Poor)
- Aggregate trust score per module

## Governance
- Append-only
- No auto-learning
- Feeds trust gating in NexusPolicyEngine
