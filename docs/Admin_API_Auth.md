# Admin API Auth (v1.8)

Auth model (v1):
- **API key** required for all endpoints except `/health`
- Header name: `X-Nexus-Admin-Key`
- Config key: `Nexus:AdminApi:AdminKey` (appsettings or env var)

## Set via environment variable
`Nexus__AdminApi__AdminKey`

## Example curl
```bash
curl -H "X-Nexus-Admin-Key: <YOUR_KEY>" http://localhost:5000/trust/inventory.transfers
```

## Governance
- Read-only API stays read-only
- Keep bound to localhost/internal network until you add full authN/authZ
