namespace Invyra.Nexus.Services;

public sealed class NexusKillSwitchService
{
    private volatile bool _enabled = true;
    public bool IsEnabled() => _enabled;
    public void DisableGlobal() => _enabled = false;
    public void EnableGlobal() => _enabled = true;
}
