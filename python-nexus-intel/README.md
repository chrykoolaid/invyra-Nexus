# Python Nexus Intelligence (gRPC) — skeleton

This is a **boxed** advisory service. It only returns decisions and explanations.
C# remains the authority (kill switch + policy gating).

## Setup
```bash
python -m venv .venv
. .venv/bin/activate  # (Windows: .venv\Scripts\activate)
pip install -r requirements.txt
python -m grpc_tools.protoc -I ../proto --python_out=. --grpc_python_out=. ../proto/nexus_intelligence.proto
python server.py
```

Service listens on `127.0.0.1:50051` by default.
