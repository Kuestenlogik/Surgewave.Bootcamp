// Surgewave Bootcamp - Unit 16, Lesson 4: Access Control
// COMPLETED SOLUTION

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.4 - Access Control ===");
Console.WriteLine();

Console.WriteLine("--- Permission Types ---");
Console.WriteLine("Resource Types: Topic, ConsumerGroup, Cluster, TransactionalId");
Console.WriteLine("Operations: Read, Write, Create, Delete, Describe, Alter, All");
Console.WriteLine("Permissions: Allow, Deny");
Console.WriteLine();

// Step 6 - Set up ACLs
Console.WriteLine("--- Setting Up ACLs ---");
var aclStore = new AclStore();
var auditLogger = new InMemoryAuditLogger();
var authService = new AuthorizationService(aclStore, auditLogger);

aclStore.SetDefaultTenantAcls("acme");
Console.WriteLine("Created default ACLs for tenant: acme");
aclStore.SetDefaultTenantAcls("globex");
Console.WriteLine("Created default ACLs for tenant: globex");

// Add custom deny rule
aclStore.AddAcl("acme", new AclEntry(
    "tenant:acme",
    ResourceType.Topic,
    "acme.audit",
    Operation.Write,
    Permission.Deny));
Console.WriteLine("Added custom deny rule: acme cannot write to acme.audit");
Console.WriteLine();

// Step 7 - Display ACLs
Console.WriteLine("--- ACL Entries for 'acme' ---");
foreach (var acl in aclStore.GetAcls("acme"))
{
    Console.WriteLine($"  {acl.Principal} -> {acl.ResourceType}:{acl.ResourcePattern} : {acl.Operation} ({acl.Permission})");
}
Console.WriteLine();

// Step 8 - Test authorization
Console.WriteLine("--- Authorization Tests ---");
var tests = new (string Tenant, string Topic, Operation Op)[]
{
    ("acme", "acme.orders", Operation.Write),
    ("acme", "acme.audit", Operation.Read),
    ("acme", "acme.audit", Operation.Write),
    ("acme", "globex.orders", Operation.Read),
};

foreach (var (tenant, topic, op) in tests)
{
    var result = authService.Authorize(
        tenant,
        $"tenant:{tenant}",
        ResourceType.Topic,
        topic,
        op);
    Console.WriteLine($"{tenant} -> {topic}:{op} = {(result.IsAllowed ? "ALLOWED" : "DENIED")} ({result.Reason})");
    Console.WriteLine();
}

// Step 9 - Display audit summary
Console.WriteLine("--- Audit Log Summary ---");
var logs = auditLogger.GetRecentLogs(10).ToList();
Console.WriteLine($"Total entries: {logs.Count}");
Console.WriteLine($"Allowed: {logs.Count(l => l.Allowed)}");
Console.WriteLine($"Denied: {logs.Count(l => !l.Allowed)}");
Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. ACLs enable fine-grained tenant access control");
Console.WriteLine("2. Pattern matching simplifies bulk permissions");
Console.WriteLine("3. Explicit deny always wins over allow");
Console.WriteLine("4. Audit logging is essential for compliance");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Supporting Types ---

// Step 1 - Permission enums
public enum ResourceType
{
    Topic,
    ConsumerGroup,
    Cluster,
    TransactionalId
}

public enum Operation
{
    Read,
    Write,
    Create,
    Delete,
    Describe,
    Alter,
    All
}

public enum Permission
{
    Allow,
    Deny
}

// Step 2 - AclEntry record
public record AclEntry(
    string Principal,
    ResourceType ResourceType,
    string ResourcePattern,
    Operation Operation,
    Permission Permission)
{
    public bool Matches(string resource)
    {
        if (ResourcePattern == "*")
            return true;

        if (ResourcePattern.EndsWith("*"))
        {
            var prefix = ResourcePattern[..^1];
            return resource.StartsWith(prefix);
        }

        return resource == ResourcePattern;
    }
}

// Step 3 - AclStore
public class AclStore
{
    private readonly Dictionary<string, List<AclEntry>> _tenantAcls = new();

    public void AddAcl(string tenantId, AclEntry acl)
    {
        if (!_tenantAcls.ContainsKey(tenantId))
            _tenantAcls[tenantId] = new List<AclEntry>();

        _tenantAcls[tenantId].Add(acl);
    }

    public IEnumerable<AclEntry> GetAcls(string tenantId)
    {
        return _tenantAcls.TryGetValue(tenantId, out var acls)
            ? acls
            : Enumerable.Empty<AclEntry>();
    }

    public void SetDefaultTenantAcls(string tenantId)
    {
        // Default: tenant can read/write their own namespace
        AddAcl(tenantId, new AclEntry(
            $"tenant:{tenantId}",
            ResourceType.Topic,
            $"{tenantId}.*",
            Operation.All,
            Permission.Allow));

        AddAcl(tenantId, new AclEntry(
            $"tenant:{tenantId}",
            ResourceType.ConsumerGroup,
            $"{tenantId}-*",
            Operation.All,
            Permission.Allow));
    }
}

// Step 4 - Audit logging
public interface IAuditLogger
{
    void LogAccess(
        string tenantId,
        string principal,
        ResourceType resourceType,
        string resource,
        Operation operation,
        AuthorizationResult result);

    IEnumerable<AuditLogEntry> GetRecentLogs(int count);
}

public record AuditLogEntry(
    DateTime Timestamp,
    string TenantId,
    string Principal,
    ResourceType ResourceType,
    string Resource,
    Operation Operation,
    bool Allowed,
    string Reason);

public class InMemoryAuditLogger : IAuditLogger
{
    private readonly List<AuditLogEntry> _logs = new();
    private readonly object _lock = new();

    public void LogAccess(
        string tenantId,
        string principal,
        ResourceType resourceType,
        string resource,
        Operation operation,
        AuthorizationResult result)
    {
        var entry = new AuditLogEntry(
            DateTime.UtcNow,
            tenantId,
            principal,
            resourceType,
            resource,
            operation,
            result.IsAllowed,
            result.Reason);

        lock (_lock)
        {
            _logs.Add(entry);
        }

        var status = result.IsAllowed ? "ALLOWED" : "DENIED";
        Console.WriteLine($"[AUDIT] {status}: {tenantId}/{principal} -> {resourceType}:{resource}:{operation}");
    }

    public IEnumerable<AuditLogEntry> GetRecentLogs(int count)
    {
        lock (_lock)
        {
            return _logs.TakeLast(count).ToList();
        }
    }
}

// Step 5 - AuthorizationService
public record AuthorizationResult(bool IsAllowed, string Reason);

public class AuthorizationService
{
    private readonly AclStore _aclStore;
    private readonly IAuditLogger _auditLogger;

    public AuthorizationService(AclStore aclStore, IAuditLogger auditLogger)
    {
        _aclStore = aclStore;
        _auditLogger = auditLogger;
    }

    public AuthorizationResult Authorize(
        string tenantId,
        string principal,
        ResourceType resourceType,
        string resource,
        Operation operation)
    {
        var acls = _aclStore.GetAcls(tenantId)
            .Where(a => a.ResourceType == resourceType)
            .Where(a => a.Matches(resource))
            .Where(a => a.Operation == operation || a.Operation == Operation.All)
            .ToList();

        // Check for explicit deny first (deny takes precedence)
        if (acls.Any(a => a.Permission == Permission.Deny))
        {
            var result = new AuthorizationResult(false, "Explicitly denied by ACL");
            _auditLogger.LogAccess(tenantId, principal, resourceType, resource, operation, result);
            return result;
        }

        // Check for allow
        if (acls.Any(a => a.Permission == Permission.Allow))
        {
            var result = new AuthorizationResult(true, "Allowed by ACL");
            _auditLogger.LogAccess(tenantId, principal, resourceType, resource, operation, result);
            return result;
        }

        // Default deny - no matching ACL
        var defaultDeny = new AuthorizationResult(false, "No matching ACL (default deny)");
        _auditLogger.LogAccess(tenantId, principal, resourceType, resource, operation, defaultDeny);
        return defaultDeny;
    }
}
