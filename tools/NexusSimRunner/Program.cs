using System.Text.Json;
using Invyra.Nexus.Contracts;
using Invyra.Nexus.Policy;
using Invyra.Nexus.Services;
using Invyra.Nexus.Dqs.Services;
using Invyra.Nexus.Persistence.Sqlite;
using Invyra.Nexus.Outcomes;
using Invyra.Nexus.GrpcClient;
using NexusSimRunner.IO;
using NexusSimRunner.Metrics;

namespace NexusSimRunner;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var opts = Cli.Parse(args);
        if (!opts.IsValid)
        {
            Console.Error.WriteLine(opts.Error);
            Console.Error.WriteLine(Cli.Usage);
            return 2;
        }

        var kill = new NexusKillSwitchService();
        var audit = new NexusAuditLog();

        IDqsStore dqsStore;
        if (!string.IsNullOrWhiteSpace(opts.SqliteDbPath))
        {
            var db = new SqliteDb(opts.SqliteDbPath);
            SchemaInit.EnsureCreated(db);
            dqsStore = new SqliteDqsStore(db);
        }
        else
        {
            dqsStore = new InMemoryDqsStore();
        }
        var dqsEval = new DqsEvaluator();
        var trustAgg = new DqsTrustAggregator();
        var outcomeObs = new NexusOutcomeObserver(dqsEval, dqsStore);

        INexusIntelligenceClient intel = opts.UseGrpc
            ? new GrpcNexusIntelligenceClient(opts.GrpcAddress)
            : new StubIntelClient(opts.EngineVersion);

        var policy = new NexusPolicyEngine(
            new NexusPolicyOptions
            {
                TrustMin = opts.TrustMin,
                TrustMinSampleSize = opts.TrustMinSampleSize,
                IntelligenceTimeout = TimeSpan.FromMilliseconds(opts.TimeoutMs),
            },
            kill,
            intel,
            audit,
            dqsStore,
            trustAgg
        );

        var pack = PackLoader.LoadPack(opts.PackRoot);
        var runId = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}__{pack.PackId}__{opts.EngineVersion}";
        var runDir = Path.Combine(opts.OutRoot, runId);
        Directory.CreateDirectory(runDir);
        Directory.CreateDirectory(Path.Combine(runDir, "diffs"));

        File.WriteAllText(Path.Combine(runDir, "run.json"),
            JsonSerializer.Serialize(new
            {
                run_id = runId,
                pack_id = pack.PackId,
                pack_version = pack.PackVersion,
                engine_version = opts.EngineVersion,
                mode = opts.Mode,
                started_at = DateTime.UtcNow.ToString("O"),
                use_grpc = opts.UseGrpc,
                grpc_address = opts.GrpcAddress
            }, JsonOpts.Pretty));

        var rows = new List<RunRow>();

        foreach (var caseFile in pack.CaseIndex)
        {
            var c = PackLoader.LoadCase(opts.PackRoot, caseFile);
            var exp = PackLoader.LoadExpected(opts.PackRoot, caseFile);

            var req = RequestMapper.FromCase(c, pack.Module);
            var pr = await policy.EvaluateAsync(req, CancellationToken.None);

            ActualResult actual;
            if (pr.Response is null)
            {
                actual = new ActualResult("INCIDENT", 0, 0, Array.Empty<string>());
            }
            else
            {
                actual = new ActualResult(
                    pr.Response.Outcome.ToString().ToUpperInvariant(),
                    pr.Response.Confidence,
                    pr.Response.FailureLikelihood,
                    pr.Response.DominantFactors
                );
            }

            var cmp = Comparator.Compare(actual, exp);
            rows.Add(new RunRow(c.CaseId, caseFile, exp, actual, cmp));

            if (!cmp.Pass)
            {
                var diffPath = Path.Combine(runDir, "diffs", caseFile.Replace(".json", ".diff.json"));
                File.WriteAllText(diffPath, JsonSerializer.Serialize(new
                {
                    case_id = c.CaseId,
                    expected = exp.Expected,
                    actual,
                    fail_reasons = cmp.FailReasons
                }, JsonOpts.Pretty));
            }

            // Outcome observation -> DQS -> trust loop (simulation)
            outcomeObs.Observe(
                decisionId: req.DecisionId,
                module: pack.Module,
                decisionTime: req.Timestamp,
                expectedOutcome: exp.Expected.Outcome,
                actualOutcome: actual.Outcome,
                confidenceAtDecision: actual.Confidence
            );
        }

        // results.jsonl
        using (var sw = new StreamWriter(Path.Combine(runDir, "results.jsonl")))
        {
            foreach (var r in rows)
                sw.WriteLine(JsonSerializer.Serialize(r, JsonOpts.Compact));
        }

        var metrics = MetricComputer.Compute(rows);
        File.WriteAllText(Path.Combine(runDir, "metrics.json"), JsonSerializer.Serialize(metrics, JsonOpts.Pretty));

        var trust = trustAgg.ComputeTrustScore(dqsStore.All().Where(r => r.Module.Equals(pack.Module, StringComparison.OrdinalIgnoreCase)));
        File.WriteAllText(Path.Combine(runDir, "trust.json"), JsonSerializer.Serialize(new
        {
            module = pack.Module,
            trust_score = trust,
            sample_size = dqsStore.All().Count
        }, JsonOpts.Pretty));

        if (opts.Mode.Equals("strict", StringComparison.OrdinalIgnoreCase))
        {
            var failures = Gate.Check(pack.SuccessThresholds, metrics);
            if (failures.Count > 0)
            {
                File.WriteAllText(Path.Combine(runDir, "gate_failures.json"),
                    JsonSerializer.Serialize(new { failures }, JsonOpts.Pretty));
                Console.Error.WriteLine("STRICT MODE FAILED:");
                foreach (var f in failures) Console.Error.WriteLine(" - " + f);
                Console.Error.WriteLine("Run bundle: " + runDir);
                return 2;
            }
        }

        Console.WriteLine("OK: " + JsonSerializer.Serialize(metrics, JsonOpts.Compact));
        Console.WriteLine("Trust: " + trust);
        Console.WriteLine("Run bundle: " + runDir);
        return 0;
    }
}
