using System.Text.Json;
using Invyra.Nexus.Contracts;
using Invyra.Nexus.Persistence.Sqlite;

namespace Invyra.Nexus.Services;

/// <summary>
/// Append-only SQLite audit log.
/// </summary>
public sealed class SqliteNexusAuditLog : NexusAuditLog
{
    private readonly SqliteDb _db;

    public SqliteNexusAuditLog(SqliteDb db) => _db = db;

    public override Task AppendAsync(NexusDecisionRequest req, NexusDecisionResponse? resp, string evt, CancellationToken ct)
    {
        using var c = _db.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO nexus_audit_log (ts_utc, event_type, decision_id, module, request_json, response_json)
VALUES ($ts, $evt, $decision_id, $module, $req, $resp)";
        cmd.Parameters.AddWithValue("$ts", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$evt", evt);
        cmd.Parameters.AddWithValue("$decision_id", req.DecisionId);
        cmd.Parameters.AddWithValue("$module", req.Module);
        cmd.Parameters.AddWithValue("$req", JsonSerializer.Serialize(req));
        cmd.Parameters.AddWithValue("$resp", resp is null ? null : JsonSerializer.Serialize(resp));
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }
}
