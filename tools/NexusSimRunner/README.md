# .NET Nexus Simulation Runner v1 (Recommended)

This adds a native C# simulation runner that:
- reads scenario packs,
- runs them through Nexus Policy Engine (gRPC or stub),
- writes an auditable run bundle,
- feeds outcomes into DQS via `NexusOutcomeObserver`,
- emits `trust.json`.

## Run (gRPC)
Start the Python gRPC server first (`python-nexus-intel/server.py`), then:

```bash
dotnet run --project tools/NexusSimRunner/NexusSimRunner.csproj -- \
  --packRoot nexus-sim/scenario-packs/inventory_transfers_v1 \
  --outRoot nexus-sim/runs \
  --mode strict \
  --engineVersion dev-grpc \
  --useGrpc true \
  --grpcAddress http://127.0.0.1:50051 \
  --timeoutMs 2000
```

## Run (offline stub)
```bash
dotnet run --project tools/NexusSimRunner/NexusSimRunner.csproj -- \
  --packRoot nexus-sim/scenario-packs/inventory_transfers_v1 \
  --outRoot nexus-sim/runs \
  --mode strict \
  --engineVersion dev-stub \
  --useGrpc false
```
