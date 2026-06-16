namespace NexusSimRunner;

public sealed record CliOptions(
    bool IsValid,
    string Error,
    string PackRoot,
    string OutRoot,
    string Mode,
    string EngineVersion,
    bool UseGrpc,
    string GrpcAddress,
    int TimeoutMs,
    double TrustMin,
    int TrustMinSampleSize,
    string SqliteDbPath
);

public static class Cli
{
    public const string Usage =
@"Usage:
  dotnet run --project tools/NexusSimRunner/NexusSimRunner.csproj -- \
    --packRoot nexus-sim/scenario-packs/inventory_transfers_v1 \
    --outRoot nexus-sim/runs \
    --mode strict \
    --engineVersion local-dev \
    --useGrpc true \
    --grpcAddress http://127.0.0.1:50051 \
    --timeoutMs 2000 \
    --sqliteDb nexus-sim/nexus.db

Notes:
- --useGrpc false uses an in-process stub engine.
- In strict mode, thresholds are read from pack.json (success_thresholds).";

    public static CliOptions Parse(string[] args)
    {
        string get(string key, string def)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    return args[i + 1];
            return def;
        }

        bool getBool(string key, bool def)
        {
            var v = get(key, def.ToString());
            return bool.TryParse(v, out var b) ? b : def;
        }

        int getInt(string key, int def)
        {
            var v = get(key, def.ToString());
            return int.TryParse(v, out var n) ? n : def;
        }

        double getDouble(string key, double def)
        {
            var v = get(key, def.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return double.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : def;
        }

        var packRoot = get("--packRoot", "");
        if (string.IsNullOrWhiteSpace(packRoot))
            return new(false, "Missing --packRoot", "", "", "", "", false, "", 0, 0.7, 20, "");

        return new(true, "",
            PackRoot: packRoot,
            OutRoot: get("--outRoot", "nexus-sim/runs"),
            Mode: get("--mode", "strict"),
            EngineVersion: get("--engineVersion", "local-dev"),
            UseGrpc: getBool("--useGrpc", true),
            GrpcAddress: get("--grpcAddress", "http://127.0.0.1:50051"),
            TimeoutMs: getInt("--timeoutMs", 2000),
            TrustMin: getDouble("--trustMin", 0.70),
            TrustMinSampleSize: getInt("--trustMinSampleSize", 20),
            SqliteDbPath: get("--sqliteDb", "")
        );
    }
}
