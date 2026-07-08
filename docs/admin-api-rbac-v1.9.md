# Admin API Role-Based Authorization (v1.9)

## Scope

v1.9 introduces a narrow role-based authorization layer for the existing read-only Nexus Admin API.

This does not add JWT, user management, dashboards, write APIs, or broader platform authorization.

## Authentication

The Admin API continues to use the existing `X-Nexus-Admin-Key` header.

Role is inferred from the configured key that matches the request.

Configuration keys:

- `Nexus:AdminApi:AdminKey`
- `Nexus:AdminApi:AuditorKey`
- `Nexus:AdminApi:ViewerKey`

The existing `AdminKey` remains backward-compatible and grants full Admin access.

Unset keys and placeholder keys using `CHANGE_ME_LONG_RANDOM` are ignored.

## Roles

| Role | Purpose | Access |
| --- | --- | --- |
| Viewer | Operational read-only visibility | `/trust/{module}` |
| Auditor | Evidence and governance review | `/trust/{module}`, `/dqs`, `/audit` |
| Admin | Full Admin API access | All protected endpoints |

`/health` remains open for liveness checks.

## Responses

- Missing or invalid key: `401 unauthorized`
- Valid key with insufficient role: `403 forbidden`

## Governance rules

- Authorization remains centralized in the Admin API boundary.
- Do not scatter role checks through domain or persistence logic.
- Do not add write-side Admin API behavior in this phase.
- Do not introduce JWT or external identity providers until a later planned phase.
