# Lesson 16.4: Instructions

Follow these step-by-step instructions to implement access control.

## Step 1: Define Permission Types

Create enums for resources and operations:

```csharp
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
```

## Step 2: Create ACL Entry Record

Define the ACL entry structure:

```csharp
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
```

## Step 3: Implement ACL Store

Create a store to manage ACLs per tenant:

```csharp
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
```

## Step 4: Create Authorization Service

Build a service to check permissions:

```csharp
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

        // Check for explicit deny first
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

        // Default deny
        var defaultDeny = new AuthorizationResult(false, "No matching ACL (default deny)");
        _auditLogger.LogAccess(tenantId, principal, resourceType, resource, operation, defaultDeny);
        return defaultDeny;
    }
}

public record AuthorizationResult(bool IsAllowed, string Reason);
```

## Step 5: Implement Audit Logger

Create an audit logger interface and implementation:

```csharp
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
```

## Step 6: Demonstrate Access Control

Test the authorization system:

```csharp
var aclStore = new AclStore();
var auditLogger = new InMemoryAuditLogger();
var authService = new AuthorizationService(aclStore, auditLogger);

// Set up tenant ACLs
aclStore.SetDefaultTenantAcls("acme");
aclStore.SetDefaultTenantAcls("globex");

// Add custom ACL: acme can't write to audit topics
aclStore.AddAcl("acme", new AclEntry(
    "tenant:acme",
    ResourceType.Topic,
    "acme.audit",
    Operation.Write,
    Permission.Deny));

// Test various access scenarios
var tests = new[]
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

    Console.WriteLine($"{tenant} -> {topic}:{op} = {(result.IsAllowed ? "ALLOWED" : "DENIED")}");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- Permission type definitions
- AclEntry with pattern matching
- AclStore for managing tenant ACLs
- AuthorizationService with audit logging
- Demonstration scenarios

## Knowledge Check

1. Why is "deny by default" important for security?
2. How does pattern matching work for topic ACLs?
3. Why should explicit deny rules take precedence over allow rules?
4. What information should audit logs capture?
5. How would you implement cross-tenant data sharing securely?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 16.4 - Access Control ===

--- Permission Types ---
Resource Types: Topic, ConsumerGroup, Cluster, TransactionalId
Operations: Read, Write, Create, Delete, Describe, Alter, All
Permissions: Allow, Deny

--- Setting Up ACLs ---
Created default ACLs for tenant: acme
Created default ACLs for tenant: globex
Added custom deny rule: acme cannot write to acme.audit

--- ACL Entries for 'acme' ---
  tenant:acme -> Topic:acme.* : All (Allow)
  tenant:acme -> ConsumerGroup:acme-* : All (Allow)
  tenant:acme -> Topic:acme.audit : Write (Deny)

--- Authorization Tests ---
[AUDIT] ALLOWED: acme/tenant:acme -> Topic:acme.orders:Write
acme -> acme.orders:Write = ALLOWED (Allowed by ACL)

[AUDIT] ALLOWED: acme/tenant:acme -> Topic:acme.audit:Read
acme -> acme.audit:Read = ALLOWED (Allowed by ACL)

[AUDIT] DENIED: acme/tenant:acme -> Topic:acme.audit:Write
acme -> acme.audit:Write = DENIED (Explicitly denied by ACL)

[AUDIT] DENIED: acme/tenant:acme -> Topic:globex.orders:Read
acme -> globex.orders:Read = DENIED (No matching ACL)

--- Audit Log Summary ---
Total entries: 4
Allowed: 2
Denied: 2

--- Key Takeaways ---
1. ACLs enable fine-grained tenant access control
2. Pattern matching simplifies bulk permissions
3. Explicit deny always wins over allow
4. Audit logging is essential for compliance

Lesson complete!
```

---

**Next:** [Lesson 16.5: Resource Management](../lesson-5/README.md)
