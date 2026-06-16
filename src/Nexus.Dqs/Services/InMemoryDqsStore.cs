using Invyra.Nexus.Dqs.Contracts;

namespace Invyra.Nexus.Dqs.Services;

/// <summary>
/// Append-only in-memory store (default for tests).
/// </summary>
public sealed class InMemoryDqsStore : IDqsStore
{
    private readonly List<DqsRecord> _records = new();

    public void Append(DqsRecord record) => _records.Add(record);

    public IReadOnlyList<DqsRecord> All() => _records.AsReadOnly();

    public IReadOnlyList<DqsRecord> ByModule(string module)
        => _records.Where(r => string.Equals(r.Module, module, StringComparison.OrdinalIgnoreCase)).ToList();
}
