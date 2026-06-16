using System.Globalization;
using Grpc.Net.Client;
using Invyra.Nexus.Contracts;
using Invyra.Nexus.Services;
using Invyra.Nexus.Grpc;

namespace Invyra.Nexus.GrpcClient;

/// <summary>
/// C# authority-side adapter. Calls Python Nexus Intelligence via gRPC.
/// This client MUST remain advisory-only: it returns decisions, never executes actions.
/// </summary>
public sealed class GrpcNexusIntelligenceClient : INexusIntelligenceClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly NexusIntelligence.NexusIntelligenceClient _client;

    public GrpcNexusIntelligenceClient(string address)
    {
        // address example: "http://127.0.0.1:50051"
        _channel = GrpcChannel.ForAddress(address);
        _client = new NexusIntelligence.NexusIntelligenceClient(_channel);
    }

    public async Task<NexusDecisionResponse> EvaluateAsync(NexusDecisionRequest request, CancellationToken ct)
    {
        var grpcReq = MapToGrpc(request);
        var grpcResp = await _client.EvaluateDecisionAsync(grpcReq, cancellationToken: ct);
        return MapFromGrpc(grpcResp);
    }

    public void Dispose() => _channel.Dispose();

    private static Invyra.Nexus.Grpc.NexusDecisionRequest MapToGrpc(NexusDecisionRequest r)
    {
        var req = new Invyra.Nexus.Grpc.NexusDecisionRequest
        {
            DecisionId = r.DecisionId,
            DecisionType = r.DecisionType switch
            {
                NexusDecisionType.PreFailureDetection => Invyra.Nexus.Grpc.NexusDecisionType.PreFailureDetection,
                NexusDecisionType.DecisionQualityScore => Invyra.Nexus.Grpc.NexusDecisionType.DecisionQualityScore,
                _ => Invyra.Nexus.Grpc.NexusDecisionType.NexusDecisionTypeUnspecified
            },
            Module = r.Module,
            TimestampIso = r.Timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture),
            StoreId = r.StoreId,
            Timezone = r.Timezone
        };

        if (r.Context is not null)
            req.Context.Add(r.Context);

        foreach (var s in r.Signals)
        {
            var gs = new Invyra.Nexus.Grpc.NexusSignal { Type = s.Type, Window = s.Window ?? "" };

            // Null support: send null_value=true
            if (s.Value is null)
            {
                gs.NullValue = true;
            }
            else if (s.Value is bool b)
            {
                gs.BoolValue = b;
            }
            else if (s.Value is int i)
            {
                gs.NumberValue = i;
            }
            else if (s.Value is long l)
            {
                gs.NumberValue = l;
            }
            else if (s.Value is float fl)
            {
                gs.NumberValue = fl;
            }
            else if (s.Value is double d)
            {
                gs.NumberValue = d;
            }
            else
            {
                gs.StringValue = s.Value.ToString() ?? "";
            }

            req.Signals.Add(gs);
        }

        return req;
    }

    private static NexusDecisionResponse MapFromGrpc(Invyra.Nexus.Grpc.NexusDecisionResponse r)
    {
        var outcome = r.Outcome switch
        {
            Invyra.Nexus.Grpc.NexusOutcome.Silent => NexusOutcome.Silent,
            Invyra.Nexus.Grpc.NexusOutcome.Advisory => NexusOutcome.Advisory,
            Invyra.Nexus.Grpc.NexusOutcome.HighRisk => NexusOutcome.HighRisk,
            Invyra.Nexus.Grpc.NexusOutcome.Refusal => NexusOutcome.Refusal,
            Invyra.Nexus.Grpc.NexusOutcome.Incident => NexusOutcome.Incident,
            _ => NexusOutcome.Incident
        };

        var risk = r.RiskBand switch
        {
            Invyra.Nexus.Grpc.NexusRiskBand.Low => NexusRiskBand.Low,
            Invyra.Nexus.Grpc.NexusRiskBand.Medium => NexusRiskBand.Medium,
            Invyra.Nexus.Grpc.NexusRiskBand.High => NexusRiskBand.High,
            _ => NexusRiskBand.Medium
        };

        var explain = new NexusExplanation(
            Summary: r.Explain?.Summary ?? "",
            FactorCodes: r.Explain?.FactorCodes?.ToList() ?? new List<string>(),
            Evidence: r.Explain?.Evidence?.Count > 0 ? r.Explain.Evidence : null
        );

        return new NexusDecisionResponse(
            DecisionId: r.DecisionId,
            EngineVersion: r.EngineVersion,
            Outcome: outcome,
            FailureLikelihood: r.FailureLikelihood,
            Confidence: r.Confidence,
            DominantFactors: r.DominantFactors.ToList(),
            Explain: explain,
            MissingSignals: r.MissingSignals.ToList(),
            RiskBand: risk
        );
    }
}
