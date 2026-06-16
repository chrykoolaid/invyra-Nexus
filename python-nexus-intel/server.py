import time
from concurrent import futures

import grpc

# After running protoc, these will exist in this folder:
import nexus_intelligence_pb2 as pb2
import nexus_intelligence_pb2_grpc as pb2_grpc


ENGINE_VERSION = "nexus-py-skeleton-1.0.0"


def _signals_to_map(signals):
    out = {}
    for s in signals:
        # oneof: bool_value, number_value, string_value, null_value
        if s.null_value:
            out[s.type] = None
        elif s.HasField("bool_value"):
            out[s.type] = s.bool_value
        elif s.HasField("number_value"):
            out[s.type] = s.number_value
        elif s.HasField("string_value"):
            out[s.type] = s.string_value
        else:
            out[s.type] = None
    return out


class NexusIntelligenceServicer(pb2_grpc.NexusIntelligenceServicer):
    def HealthCheck(self, request, context):
        return pb2.HealthCheckResponse(status="ok", engine_version=ENGINE_VERSION)

    def EvaluateDecision(self, request, context):
        sig = _signals_to_map(request.signals)

        # Refusal if core signals missing (constitutional honesty)
        core = ["audit.reconciliation_present", "transfer.retry_rate", "patch.stack_divergence", "logs.warning_density"]
        missing = [k for k in core if sig.get(k) is None]
        if missing:
            explain = pb2.NexusExplanation(
                summary="Nexus cannot assess failure risk yet. Required signals are missing or incomplete.",
                factor_codes=["insufficient_signals"],
                evidence={}
            )
            return pb2.NexusDecisionResponse(
                decision_id=request.decision_id,
                engine_version=ENGINE_VERSION,
                outcome=pb2.NexusOutcome.REFUSAL,
                failure_likelihood=0.0,
                confidence=0.0,
                dominant_factors=["insufficient_signals"],
                explain=explain,
                missing_signals=missing,
                risk_band=pb2.NexusRiskBand.MEDIUM
            )

        recon = bool(sig.get("audit.reconciliation_present"))
        retry = float(sig.get("transfer.retry_rate") or 0.0)
        drift = bool(sig.get("patch.stack_divergence"))
        warn = float(sig.get("logs.warning_density") or 0.0)
        ttc = float(sig.get("transfers.time_to_complete_trend") or 0.0)

        # simple scoring stub
        score = 0.0
        factors = []
        if recon is False:
            score += 0.40; factors.append("missing_reconciliation_audit")
        if drift:
            score += 0.30; factors.append("patch_stack_divergence")
        if retry >= 0.18:
            score += 0.20; factors.append("elevated_retry_rate")
        if warn >= 0.20:
            score += 0.15; factors.append("warning_density")
        if ttc >= 0.05:
            score += 0.10; factors.append("time_to_complete_worsening")

        if score < 0.40:
            outcome = pb2.NexusOutcome.SILENT
            confidence = 0.65
            risk_band = pb2.NexusRiskBand.LOW
            summary = "No elevated pre-failure risk detected."
        elif score < 0.70:
            outcome = pb2.NexusOutcome.ADVISORY
            confidence = 0.72
            risk_band = pb2.NexusRiskBand.MEDIUM
            summary = "Some pre-failure signals detected; monitor and verify safeguards."
        else:
            outcome = pb2.NexusOutcome.HIGH_RISK
            confidence = 0.88
            risk_band = pb2.NexusRiskBand.HIGH
            summary = "High likelihood of failure under current conditions."

        explain = pb2.NexusExplanation(
            summary=summary,
            factor_codes=factors[:8],
            evidence={}
        )

        return pb2.NexusDecisionResponse(
            decision_id=request.decision_id,
            engine_version=ENGINE_VERSION,
            outcome=outcome,
            failure_likelihood=min(max(score, 0.0), 1.0),
            confidence=confidence,
            dominant_factors=factors[:4],
            explain=explain,
            missing_signals=[],
            risk_band=risk_band
        )


def serve(host="127.0.0.1", port=50051):
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=8))
    pb2_grpc.add_NexusIntelligenceServicer_to_server(NexusIntelligenceServicer(), server)
    server.add_insecure_port(f"{host}:{port}")
    server.start()
    print(f"[nexus-intel] gRPC listening on {host}:{port} ({ENGINE_VERSION})")
    try:
        while True:
            time.sleep(3600)
    except KeyboardInterrupt:
        print("[nexus-intel] shutting down")
        server.stop(0)


if __name__ == "__main__":
    serve()
