namespace Nexus.AdminApi.Tests;

public sealed class AdminApiAuthorizationRulesTests
{
    [Theory]
    [InlineData("/health", null)]
    [InlineData("/trust/inventory.transfers", NexusAdminRole.Viewer)]
    [InlineData("/dqs", NexusAdminRole.Auditor)]
    [InlineData("/audit", NexusAdminRole.Auditor)]
    [InlineData("/unknown", NexusAdminRole.Admin)]
    public void RequiredRoleFor_ReturnsExpectedRole(string path, NexusAdminRole? expected)
    {
        Assert.Equal(expected, AdminApiAuthorizationRules.RequiredRoleFor(path));
    }

    [Theory]
    [InlineData(NexusAdminRole.Viewer, NexusAdminRole.Viewer, true)]
    [InlineData(NexusAdminRole.Auditor, NexusAdminRole.Viewer, true)]
    [InlineData(NexusAdminRole.Admin, NexusAdminRole.Viewer, true)]
    [InlineData(NexusAdminRole.Viewer, NexusAdminRole.Auditor, false)]
    [InlineData(NexusAdminRole.Auditor, NexusAdminRole.Auditor, true)]
    [InlineData(NexusAdminRole.Admin, NexusAdminRole.Auditor, true)]
    [InlineData(NexusAdminRole.Viewer, NexusAdminRole.Admin, false)]
    [InlineData(NexusAdminRole.Auditor, NexusAdminRole.Admin, false)]
    [InlineData(NexusAdminRole.Admin, NexusAdminRole.Admin, true)]
    public void HasRequiredRole_EnforcesRoleHierarchy(NexusAdminRole actual, NexusAdminRole required, bool expected)
    {
        Assert.Equal(expected, AdminApiAuthorizationRules.HasRequiredRole(actual, required));
    }

    [Fact]
    public void ResolveRole_IgnoresMissingAndPlaceholderKeys()
    {
        var keys = AdminApiAuthorizationRules.BuildConfiguredKeys(
            adminKey: "CHANGE_ME_LONG_RANDOM",
            auditorKey: "",
            viewerKey: "   ");

        Assert.Empty(keys);
    }

    [Theory]
    [InlineData("admin-key", NexusAdminRole.Admin)]
    [InlineData("auditor-key", NexusAdminRole.Auditor)]
    [InlineData("viewer-key", NexusAdminRole.Viewer)]
    public void ResolveRole_ReturnsMatchedRole(string provided, NexusAdminRole expected)
    {
        var keys = AdminApiAuthorizationRules.BuildConfiguredKeys(
            adminKey: "admin-key",
            auditorKey: "auditor-key",
            viewerKey: "viewer-key");

        Assert.Equal(expected, AdminApiAuthorizationRules.ResolveRole(keys, provided));
    }

    [Fact]
    public void ResolveRole_ReturnsNullForInvalidKey()
    {
        var keys = AdminApiAuthorizationRules.BuildConfiguredKeys(
            adminKey: "admin-key",
            auditorKey: "auditor-key",
            viewerKey: "viewer-key");

        Assert.Null(AdminApiAuthorizationRules.ResolveRole(keys, "wrong-key"));
    }
}

internal static class AdminApiAuthorizationRules
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

    public static bool HasRequiredRole(NexusAdminRole actual, NexusAdminRole required)
    {
        return actual >= required;
    }

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
