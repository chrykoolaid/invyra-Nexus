# SQLite Persistence (v1.6)

This build adds a new project:
- `src/Nexus.Persistence` (SQLite DB + schema init)

## Tables
- `nexus_audit_log` (append-only)
- `nexus_dqs` (append-only)

## How to enable
### Simulation runner
Pass:
- `--sqliteDb nexus-sim/nexus.db`

### Production
Create `SqliteDb` with your chosen path, call:
- `SchemaInit.EnsureCreated(db)`

Then use:
- `SqliteDqsStore(db)` as `IDqsStore`
- `SqliteNexusAuditLog(db)` as `NexusAuditLog`

## Governance
- Append-only inserts only
- No UPDATEs
- No deletes
