using Invyra.Nexus.Dqs.Contracts;

namespace Invyra.Nexus.Dqs.Services;

public interface IDqsStore
{
    void Append(DqsRecord record);
    IReadOnlyList<DqsRecord> All();
    IReadOnlyList<DqsRecord> ByModule(string module);
}
