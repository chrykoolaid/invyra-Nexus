using System.Text.Json;
using Invyra.Nexus.Dqs.Contracts;
using Invyra.Nexus.Dqs.Services;

namespace Invyra.Nexus.Persistence.Sqlite;

public sealed class SqliteDqsStore : IDqsStore
{
    private readonly SqliteDb _db;

    public SqliteDqsStore(SqliteDb db)
    {
        _db = db;
    }

    public void Append(DqsRecord record)
    {
        using var c = _db.Open();
        using var cmd = c.CreateCommand();

        cmd.CommandText = @"
INSERT INTO nexus_dqs (
  ts_utc,
  decision_id,
  module,
  decision_time,
  outcome_observed_at,
  expected_outcome,
  actual_outcome,
  confidence,
  verdict,
  score,
  notes_json
) VALUES (
  $ts_utc,
  $decision_id,
  $module,
  $decision_time,
  $outcome_observed_at,
  $expected_outcome,
  $actual_outcome,
  $confidence,
  $verdict,
  $score,
  $notes_json
);";

        cmd.Parameters.AddWithValue("$ts_utc", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$decision_id", record.DecisionId);
        cmd.Parameters.AddWithValue("$module", record.Module);
        cmd.Parameters.AddWithValue("$decision_time", record.DecisionTime.ToString("O"));
        cmd.Parameters.AddWithValue("$outcome_observed_at", record.OutcomeObservedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$expected_outcome", record.ExpectedOutcome);
        cmd.Parameters.AddWithValue("$actual_outcome", record.ActualOutcome);
        cmd.Parameters.AddWithValue("$confidence", record.ConfidenceAtDecision);
        cmd.Parameters.AddWithValue("$verdict", record.Verdict.ToString());
        cmd.Parameters.AddWithValue("$score", record.Score);
        cmd.Parameters.AddWithValue("$notes_json", JsonSerializer.Serialize(record.Notes ?? Array.Empty<string>()));

        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<DqsRecord> All()
    {
        using var c = _db.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
SELECT decision_id, module, decision_time, outcome_observed_at, expected_outcome,
       actual_outcome, confidence, verdict, score, notes_json
FROM nexus_dqs
ORDER BY id ASC;";

        using var r = cmd.ExecuteReader();
        return ReadRecords(r);
    }

    public IReadOnlyList<DqsRecord> ByModule(string module)
    {
        using var c = _db.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
SELECT decision_id, module, decision_time, outcome_observed_at, expected_outcome,
       actual_outcome, confidence, verdict, score, notes_json
FROM nexus_dqs
WHERE module = $module
ORDER BY id ASC;";
        cmd.Parameters.AddWithValue("$module", module);

        using var r = cmd.ExecuteReader();
        return ReadRecords(r);
    }

    private static IReadOnlyList<DqsRecord> ReadRecords(Microsoft.Data.Sqlite.SqliteDataReader r)
    {
        var rows = new List<DqsRecord>();

        while (r.Read())
        {
            var notesJson = r.IsDBNull(9) ? "[]" : r.GetString(9);
            var notes = JsonSerializer.Deserialize<List<string>>(notesJson) ?? new List<string>();
            var verdict = Enum.TryParse<DqsVerdict>(r.GetString(7), out var parsedVerdict)
                ? parsedVerdict
                : DqsVerdict.Degraded;

            rows.Add(new DqsRecord(
                DecisionId: r.GetString(0),
                Module: r.GetString(1),
                DecisionTime: DateTimeOffset.Parse(r.GetString(2)),
                OutcomeObservedAt: r.IsDBNull(3) ? null : DateTimeOffset.Parse(r.GetString(3)),
                ExpectedOutcome: r.GetString(4),
                ActualOutcome: r.GetString(5),
                ConfidenceAtDecision: r.GetDouble(6),
                Verdict: verdict,
                Score: r.GetDouble(8),
                Notes: notes
            ));
        }

        return rows;
    }
}
