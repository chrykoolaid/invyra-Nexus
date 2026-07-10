namespace Nexus.AdminApi.Auth;

public static class AdminApiAuthorizationRules
{
    public static IReadOnlyList<ApiKeyBinding> BuildConfiguredKeys(string adminKey, string auditorKey, string viewerKey)
    {
        var keys = new List<ApiKeyBinding>();
        AddKey(keys, adminKey, NexusAdminRole.Admin);
        AddKey(keys, auditorKey, NexusAdminRole.Auditor);
        AddKey(keys, viewerKey, NexusAdminRole.Viewer);
        return keys;
    }

    public static NexusAdminRole? ResolveRole(IReadOnlyList<ApiKeyBinding> keys, string provided)
    {
        if (string.IsNullOrWhiteSpace(provided)) return null;

        foreach (var key in keys)
        {
            if (CryptographicEquals(key.Key, provided)) return key.Role;
        }

        return null;
    }

    public static NexusAdminRole? RequiredRoleFor(string path)
    {
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)) return null;
        if (path.StartsWith("/trust", StringComparison.OrdinalIgnoreCase)) return NexusAdminRole.Viewer;
        if (path.StartsWith("/dqs", StringComparison.OrdinalIgnoreCase)) return NexusAdminRole.Auditor;
        if (path.StartsWith("/audit", StringComparison.OrdinalIgnoreCase)) return NexusAdminRole.Auditor;
        return NexusAdminRole.Admin;
    }

    public static bool HasRequiredRole(NexusAdminRole actual, NexusAdminRole required) => actual >= required;

    private static void AddKey(List<ApiKeyBinding> keys, string key, NexusAdminRole role)
    {
        if (string.IsNullOrWhiteSpace(key) || key == "CHANGE_ME_LONG_RANDOM") return;
        keys.Add(new ApiKeyBinding(key, role));
    }

    private static bool CryptographicEquals(string a, string b)
    {
        var ba = System.Text.Encoding.UTF8.GetBytes(a);
        var bb = System.Text.Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length) return false;
        var diff = 0;
        for (var i = 0; i < ba.Length; i++) diff |= ba[i] ^ bb[i];
        return diff == 0;
    }
}

public readonly record struct ApiKeyBinding(string Key, NexusAdminRole Role);

public enum NexusAdminRole
{
    Viewer = 1,
    Auditor = 2,
    Admin = 3
}
