# Nexus Admin API (Read-only) v1.8

## Auth (API key)
All endpoints except `/health` require:
- Header: `X-Nexus-Admin-Key`
- Config: `Nexus:AdminApi:AdminKey` (set a long random value)

## Endpoints
- `GET /health`
- `GET /trust/{module}`
- `GET /dqs?module=inventory.transfers&limit=200`
- `GET /audit?decisionId=<id>&module=<module>&limit=200`

## Run
```bash
dotnet run --project src/Nexus.AdminApi/Nexus.AdminApi.csproj
```
