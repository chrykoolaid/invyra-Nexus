using System.Text.Json;
using Invyra.Nexus.Dqs.Contracts;
using Invyra.Nexus.Persistence.Sqlite;
using Microsoft.Data.Sqlite;

namespace Invyra.Nexus.Dqs.Services;

/// <summary>
/// Append-only SQLite-backed DQS store.
/// </summary>
public sealed class SqliteDqsStore : IDqsStore
{
    private readonly SqliteDb _db;

    public SqliteDqsStore(SqliteDb db)
    {
        _db = db;
    }

    public void Append(DqsRecord r)
    {
        using var c = _db.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO nexus_dqs (
  ts_utc, decision_id, module, decision_time, outcome_observed_at,
  expected_outcome, actual_outcome, confidence, verdict, score, notes_json
) VALUES (
  $ts, $decision_id, $module, $decision_time, $outcome_observed_at,
  $expected, $actual, $confidence, $verdict, $score, $notes_json
)";
        cmd.Parameters.AddWithValue("$ts", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$decision_id", r.DecisionId);
        cmd.Parameters.AddWithValue("$module", r.Module);
        cmd.Parameters.AddWithValue("$decision_time", r.DecisionTime.ToString("O"));
        cmd.Parameters.AddWithValue("$outcome_observed_at", r.OutcomeObservedAt?.ToString("O"));
        cmd.Parameters.AddWithValue("$expected", r.ExpectedOutcome);
        cmd.Parameters.AddWithValue("$actual", r.ActualOutcome);
        cmd.Parameters.AddWithValue("$confidence", r.ConfidenceAtDecision);
        cmd.Parameters.AddWithValue("$verdict", r.Verdict.ToString());
        cmd.Parameters.AddWithValue("$score", r.Score);
        cmd.Parameters.AddWithValue("$notes_json", JsonSerializer.Serialize(r.Notes));

        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<DqsRecord> All()
    {
        using var c = _db.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT decision_id,module,decision_time,outcome_observed_at,expected_outcome,actual_outcome,confidence,verdict,score,notes_json FROM nexus_dqs ORDER BY id ASC";
        using var r = cmd.ExecuteReader();
        return ReadRows(r);
    }

    public IReadOnlyList<DqsRecord> ByModule(string module)
    {
        using var c = _db.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT decision_id,module,decision_time,outcome_observed_at,expected_outcome,actual_outcome,confidence,verdict,score,notes_json FROM nexus_dqs WHERE module=$m ORDER BY id ASC";
        cmd.Parameters.AddWithValue("$m", module);
        using var r = cmd.ExecuteReader();
        return ReadRows(r);
    }

    private static List<DqsRecord> ReadRows(SqliteDataReader r)
    {
        var list = new List<DqsRecord>();
        while (r.Read())
        {
            var decisionId = r.GetString(0);
            var module = r.GetString(1);
            var decisionTime = DateTimeOffset.Parse(r.GetString(2));
            var obsAt = r.IsDBNull(3) ? null : DateTimeOffset.Parse(r.GetString(3));
            var expected = r.GetString(4);
            var actual = r.GetString(5);
            var confidence = r.GetDouble(6);
            var verdict = Enum.TryParse<DqsVerdict>(r.GetString(7), out var v) ? v : DqsVerdict.Degraded;
            var score = r.GetDouble(8);
            var notes = JsonSerializer.Deserialize<List<string>>(r.GetString(9)) ?? new List<string>();

            list.Add(new DqsRecord(decisionId, module, decisionTime, obsAt, expected, actual, confidence, verdict, score, notes));
        }
        return list;
    }
}
