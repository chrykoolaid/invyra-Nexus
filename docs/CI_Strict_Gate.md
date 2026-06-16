# CI Strict Gate

A GitHub Actions workflow is included at:
`.github/workflows/nexus_sim_strict_gate.yml`

Default behavior:
- Runs the .NET NexusSimRunner in **strict** mode
- Uses **offline stub** (`--useGrpc false`) for deterministic gating
- Uploads run bundles as artifacts (even on failure)

To run against Python gRPC in CI:
- Add Python setup steps
- `pip install -r python-nexus-intel/requirements.txt`
- generate protobuf stubs (or commit them)
- start `python python-nexus-intel/server.py &`
- set `--useGrpc true --grpcAddress http://127.0.0.1:50051`
