using Nexus.AdminApi.Auth;

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
    public void BuildConfiguredKeys_IgnoresMissingAndPlaceholderKeys()
    {
        var keys = AdminApiAuthorizationRules.BuildConfiguredKeys("CHANGE_ME_LONG_RANDOM", "", "   ");
        Assert.Empty(keys);
    }

    [Theory]
    [InlineData("admin-key", NexusAdminRole.Admin)]
    [InlineData("auditor-key", NexusAdminRole.Auditor)]
    [InlineData("viewer-key", NexusAdminRole.Viewer)]
    public void ResolveRole_ReturnsMatchedRole(string provided, NexusAdminRole expected)
    {
        var keys = AdminApiAuthorizationRules.BuildConfiguredKeys("admin-key", "auditor-key", "viewer-key");
        Assert.Equal(expected, AdminApiAuthorizationRules.ResolveRole(keys, provided));
    }

    [Fact]
    public void ResolveRole_ReturnsNullForInvalidKey()
    {
        var keys = AdminApiAuthorizationRules.BuildConfiguredKeys("admin-key", "auditor-key", "viewer-key");
        Assert.Null(AdminApiAuthorizationRules.ResolveRole(keys, "wrong-key"));
    }
}
