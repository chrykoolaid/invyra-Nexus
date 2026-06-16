# Nexus gRPC Bundle v1

This bundle includes:
- `proto/nexus_intelligence.proto` (authoritative contract)
- Python gRPC intelligence service skeleton (`python-nexus-intel/`)
- C# gRPC client adapter (`src/Nexus/Grpc/`) implementing `INexusIntelligenceClient`
- Example demo snippet (`src/Nexus/Examples/GrpcPolicyDemo.cs`)

## Quick Start (local)
1) Start Python service:
   - `cd python-nexus-intel`
   - install requirements
   - generate pb2 stubs with grpc_tools.protoc (see README)
   - `python server.py`

2) Build C#:
   - from repo root: `dotnet build`

3) Wire `GrpcNexusIntelligenceClient` into your DI container (later) or use the demo snippet.
