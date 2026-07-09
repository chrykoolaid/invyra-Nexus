# Admin API RBAC Deployment Notes (v1.9)

## Purpose

This note explains how to configure the Nexus Admin API role keys introduced in v1.9.

The Admin API remains read-only. This document does not introduce JWT, user management, write APIs, dashboard access, or external identity provider integration.

## Header

All protected Admin API requests continue to use:

```http
X-Nexus-Admin-Key: <configured-role-key>
```

`/health` remains open and does not require a key.

## Configuration keys

The API supports three role keys:

| Configuration key | Role | Intended use |
| --- | --- | --- |
| `Nexus:AdminApi:AdminKey` | Admin | Full Admin API access |
| `Nexus:AdminApi:AuditorKey` | Auditor | Evidence, DQS, and audit review |
| `Nexus:AdminApi:ViewerKey` | Viewer | Operational trust visibility |

The existing `AdminKey` remains backward-compatible and grants full Admin access.

Placeholder values using `CHANGE_ME_LONG_RANDOM` are ignored and should never be used in production.

## Environment variable form

For .NET configuration, nested keys can be supplied through environment variables using double underscores:

```bash
Nexus__AdminApi__AdminKey="<long-random-admin-key>"
Nexus__AdminApi__AuditorKey="<long-random-auditor-key>"
Nexus__AdminApi__ViewerKey="<long-random-viewer-key>"
```

SQLite path may also be supplied through environment variables:

```bash
Nexus__Sqlite__DbPath="/var/lib/invyra-nexus/nexus.db"
```

## Recommended key handling

- Generate long random secrets for each role.
- Do not reuse the same key across roles.
- Store keys in the deployment secret manager or host environment, not in source control.
- Rotate keys when staff access changes or when a key may have been exposed.
- Keep Admin keys restricted to trusted operators only.
- Prefer Auditor or Viewer keys for day-to-day review workflows.

## Endpoint access matrix

| Endpoint | Viewer | Auditor | Admin |
| --- | --- | --- | --- |
| `/health` | Open | Open | Open |
| `/trust/{module}` | Allowed | Allowed | Allowed |
| `/dqs` | Denied | Allowed | Allowed |
| `/audit` | Denied | Allowed | Allowed |
| Unknown protected endpoints | Denied | Denied | Allowed |

## Expected responses

Missing or invalid key:

```json
{ "error": "unauthorized" }
```

Valid key with insufficient role:

```json
{ "error": "forbidden", "required_role": "Auditor", "actual_role": "Viewer" }
```

## Example curl requests

Viewer access to trust:

```bash
curl -H "X-Nexus-Admin-Key: $NEXUS_VIEWER_KEY" \
  http://127.0.0.1:5000/trust/inventory.transfers
```

Auditor access to DQS:

```bash
curl -H "X-Nexus-Admin-Key: $NEXUS_AUDITOR_KEY" \
  http://127.0.0.1:5000/dqs?module=inventory.transfers
```

Auditor access to audit records:

```bash
curl -H "X-Nexus-Admin-Key: $NEXUS_AUDITOR_KEY" \
  http://127.0.0.1:5000/audit?module=inventory.transfers
```

## Deployment guardrails

- Keep the Admin API bound to localhost or an internal trusted network unless a later authentication layer is added.
- Do not expose the Admin API publicly with only API-key authentication.
- Do not add write endpoints under this v1.9 RBAC deployment model.
- Do not bypass RBAC by reading the SQLite database directly from dashboards or external tools.
- Keep authorization checks centralized at the Admin API boundary.

## Future phase boundary

JWT, SSO, identity provider integration, per-user audit trails, and dashboard sessions should be planned as a separate security phase.
