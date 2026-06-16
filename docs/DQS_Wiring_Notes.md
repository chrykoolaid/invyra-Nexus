# Wire DQS into NexusPolicyEngine (DI)

Register these singletons (or appropriate lifetimes):

- `DqsStore` (singleton, append-only in-memory placeholder)
- `DqsTrustAggregator` (singleton)
- `NexusPolicyEngine` now requires `DqsStore` + `DqsTrustAggregator` in constructor.

Example:

```csharp
builder.Services.AddSingleton<DqsStore>();
builder.Services.AddSingleton<DqsTrustAggregator>();
builder.Services.AddSingleton<NexusPolicyEngine>();
```

Notes:
- This v1.2 gate defaults to ShadowOnly until `TrustMinSampleSize` is reached.
- Replace `DqsStore` with DB/EventStore later; keep append-only semantics.
