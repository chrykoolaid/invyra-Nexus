# C# gRPC Client Adapter

This project generates gRPC client code from `proto/nexus_intelligence.proto` and provides
`GrpcNexusIntelligenceClient` which implements `INexusIntelligenceClient`.

## Usage

```csharp
using var intel = new GrpcNexusIntelligenceClient("http://127.0.0.1:50051");
var resp = await intel.EvaluateAsync(req, ct);
```

## Build
Requires .NET 8 SDK. `Grpc.Tools` will generate the client from the proto at build time.
