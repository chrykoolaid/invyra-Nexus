using Microsoft.Data.Sqlite;

namespace Invyra.Nexus.Persistence.Sqlite;

public static class SchemaInit
{
    public static void EnsureCreated(SqliteDb db)
    {
        using var c = db.Open();
        using var cmd = c.CreateCommand();

        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS nexus_audit_log (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  ts_utc TEXT NOT NULL,
  event_type TEXT NOT NULL,
  decision_id TEXT,
  module TEXT,
  request_json TEXT,
  response_json TEXT
);

CREATE TABLE IF NOT EXISTS nexus_dqs (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  ts_utc TEXT NOT NULL,
  decision_id TEXT NOT NULL,
  module TEXT NOT NULL,
  decision_time TEXT NOT NULL,
  outcome_observed_at TEXT,
  expected_outcome TEXT NOT NULL,
  actual_outcome TEXT NOT NULL,
  confidence REAL NOT NULL,
  verdict TEXT NOT NULL,
  score REAL NOT NULL,
  notes_json TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_nexus_audit_decision_id ON nexus_audit_log(decision_id);
CREATE INDEX IF NOT EXISTS idx_nexus_dqs_module ON nexus_dqs(module);
";
        cmd.ExecuteNonQuery();
    }
}
