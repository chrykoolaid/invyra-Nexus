using Invyra.Nexus.Persistence.Sqlite;
using Invyra.Nexus.Dqs.Services;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Config
var dbPath = builder.Configuration["Nexus:Sqlite:DbPath"] ?? "nexus-sim/nexus.db";
var adminKey = builder.Configuration["Nexus:AdminApi:AdminKey"] ?? "";
var auditorKey = builder.Configuration["Nexus:AdminApi:AuditorKey"] ?? "";
var viewerKey = builder.Configuration["Nexus:AdminApi:ViewerKey"] ?? "";

// Services
var db = new SqliteDb(dbPath);
SchemaInit.EnsureCreated(db);

builder.Services.AddSingleton(db);
builder.Services.AddSingleton<IDqsStore>(sp => new SqliteDqsStore(sp.GetRequiredService<SqliteDb>()));
builder.Services.AddSingleton<DqsTrustAggregator>();

var app = builder.Build();

// ---- AUTH (API Key + RBAC, read-only) ----
// Header: X-Nexus-Admin-Key
// - /health is open
// - all other endpoints require a valid configured key
// - role is inferred from the matched key
// - AdminKey is backward-compatible and grants full Admin access
var configuredKeys = BuildConfiguredKeys(adminKey, auditorKey, viewerKey);

if (configuredKeys.Count == 0)
{
    app.Logger.LogWarning("Nexus Admin API: no production Admin API keys are configured. Set Nexus:AdminApi keys before exposing beyond localhost.");
}

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    var provided = ctx.Request.Headers["X-Nexus-Admin-Key"].ToString();
    var role = ResolveRole(configuredKeys, provided);
    if (role is null)
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { error = "unauthorized" });
        return;
    }

    var requiredRole = RequiredRoleFor(ctx.Request.Path);
    if (!HasRequiredRole(role.Value, requiredRole))
    {
        ctx.Response.StatusCode = 403;
        await ctx.Response.WriteAsJsonAsync(new { error = "forbidden", required_role = requiredRole.ToString(), actual_role = role.Value.ToString() });
        return;
    }

    ctx.Items["NexusAdminRole"] = role.Value.ToString();
    await next();
});

static List<ApiKeyBinding> BuildConfiguredKeys(string adminKey, string auditorKey, string viewerKey)
{
    var keys = new List<ApiKeyBinding>();
    AddKey(keys, adminKey, NexusAdminRole.Admin);
    AddKey(keys, auditorKey, NexusAdminRole.Auditor);
    AddKey(keys, viewerKey, NexusAdminRole.Viewer);
    return keys;
}

static void AddKey(List<ApiKeyBinding> keys, string key, NexusAdminRole role)
{
    if (string.IsNullOrWhiteSpace(key) || key == "CHANGE_ME_LONG_RANDOM") return;
    keys.Add(new ApiKeyBinding(key, role));
}

static NexusAdminRole? ResolveRole(IReadOnlyList<ApiKeyBinding> keys, string provided)
{
    if (string.IsNullOrWhiteSpace(provided)) return null;

    foreach (var key in keys)
    {
        if (CryptographicEquals(key.Key, provided)) return key.Role;
    }

    return null;
}

static NexusAdminRole RequiredRoleFor(PathString path)
{
    if (path.StartsWithSegments("/trust")) return NexusAdminRole.Viewer;
    if (path.StartsWithSegments("/dqs")) return NexusAdminRole.Auditor;
    if (path.StartsWithSegments("/audit")) return NexusAdminRole.Auditor;
    return NexusAdminRole.Admin;
}

static bool HasRequiredRole(NexusAdminRole actual, NexusAdminRole required)
{
    return actual >= required;
}

static bool CryptographicEquals(string a, string b)
{
    // constant-time compare
    var ba = System.Text.Encoding.UTF8.GetBytes(a);
    var bb = System.Text.Encoding.UTF8.GetBytes(b);
    if (ba.Length != bb.Length) return false;
    var diff = 0;
    for (int i = 0; i < ba.Length; i++) diff |= ba[i] ^ bb[i];
    return diff == 0;
}

internal readonly record struct ApiKeyBinding(string Key, NexusAdminRole Role);

internal enum NexusAdminRole
{
    Viewer = 1,
    Auditor = 2,
    Admin = 3
}
// ---- END AUTH ----

app.MapGet("/health", (SqliteDb db) =>
{
    using var c = db.Open();
    using var cmd = c.CreateCommand();
    cmd.CommandText = "SELECT 1";
    cmd.ExecuteScalar();
    return Results.Ok(new { status = "ok" });
});

app.MapGet("/trust/{module}", (string module, IDqsStore store, DqsTrustAggregator agg) =>
{
    var recs = store.ByModule(module);
    var trust = agg.ComputeTrustScore(recs);
    return Results.Ok(new { module, trust_score = trust, sample_size = recs.Count });
});

app.MapGet("/dqs", (string? module, int? limit, IDqsStore store) =>
{
    var list = string.IsNullOrWhiteSpace(module) ? store.All() : store.ByModule(module);
    var take = limit is null ? 200 : Math.Clamp(limit.Value, 1, 2000);
    var page = list.TakeLast(take).ToList();
    return Results.Ok(page);
});

app.MapGet("/audit", (string? decisionId, string? module, int? limit, SqliteDb db) =>
{
    var take = limit is null ? 200 : Math.Clamp(limit.Value, 1, 2000);

    using var c = db.Open();
    using var cmd = c.CreateCommand();

    var where = new List<string>();
    if (!string.IsNullOrWhiteSpace(decisionId))
    {
        where.Add("decision_id = $decision_id");
        cmd.Parameters.AddWithValue("$decision_id", decisionId);
    }
    if (!string.IsNullOrWhiteSpace(module))
    {
        where.Add("module = $module");
        cmd.Parameters.AddWithValue("$module", module);
    }

    cmd.CommandText =
        "SELECT ts_utc,event_type,decision_id,module,request_json,response_json " +
        "FROM nexus_audit_log " +
        (where.Count > 0 ? "WHERE " + string.Join(" AND ", where) + " " : "") +
        "ORDER BY id DESC LIMIT $limit";
    cmd.Parameters.AddWithValue("$limit", take);

    using var r = cmd.ExecuteReader();
    var rows = new List<object>();

    while (r.Read())
    {
        rows.Add(new
        {
            ts_utc = r.GetString(0),
            event_type = r.GetString(1),
            decision_id = r.IsDBNull(2) ? null : r.GetString(2),
            module = r.IsDBNull(3) ? null : r.GetString(3),
            request_json = r.IsDBNull(4) ? null : r.GetString(4),
            response_json = r.IsDBNull(5) ? null : r.GetString(5)
        });
    }

    return Results.Ok(rows);
});

app.Run();
