using System.Text.Json;

namespace NexusSimRunner.IO;

public static class PackLoader
{
    public static PackMeta LoadPack(string packRoot)
    {
        var packPath = Path.Combine(packRoot, "pack.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(packPath));
        var root = doc.RootElement;

        var packId = root.GetProperty("pack_id").GetString()!;
        var packVersion = root.GetProperty("pack_version").GetString()!;
        var module = root.GetProperty("module").GetString()!;
        var caseIndex = root.GetProperty("case_index").EnumerateArray().Select(x => x.GetString()!).ToList();

        var th = root.GetProperty("success_thresholds");
        var thresholds = new SuccessThresholds(
            th.GetProperty("accuracy_min").GetDouble(),
            th.GetProperty("false_negative_max").GetDouble(),
            th.GetProperty("overconfidence_max").GetDouble()
        );

        return new PackMeta(packId, packVersion, module, caseIndex, thresholds);
    }

    public static ScenarioCase LoadCase(string packRoot, string caseFile)
    {
        var path = Path.Combine(packRoot, "cases", caseFile);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var r = doc.RootElement;

        var caseId = r.GetProperty("case_id").GetString()!;
        var ts = DateTimeOffset.Parse(r.GetProperty("timestamp").GetString()!);

        var store = r.GetProperty("store_context").GetProperty("store_id").GetString()!;
        var tz = r.GetProperty("store_context").GetProperty("timezone").GetString()!;

        var signals = new List<Signal>();
        foreach (var s in r.GetProperty("signals").EnumerateArray())
        {
            var type = s.GetProperty("type").GetString()!;
            object? value = null;
            if (s.TryGetProperty("value", out var v))
            {
                value = v.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => v.GetDouble(),
                    JsonValueKind.String => v.GetString(),
                    JsonValueKind.Null => null,
                    _ => v.ToString()
                };
            }
            var window = s.TryGetProperty("window", out var w) ? w.GetString() : null;
            signals.Add(new Signal(type, value, window));
        }

        return new ScenarioCase(caseId, ts, store, tz, signals);
    }

    public static ExpectedEnvelope LoadExpected(string packRoot, string caseFile)
    {
        var path = Path.Combine(packRoot, "expected", caseFile.Replace(".json", ".expected.json"));
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ExpectedEnvelope>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
