// Surgewave Bootcamp - Unit 16, Lesson 4: Access Control
//
// In this lesson, you'll learn:
// - Per-tenant ACL systems
// - Resource authorization policies
// - Audit logging for compliance
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.4 - Access Control ===");
Console.WriteLine();

// TODO: Step 1 - Define permission enums
// public enum ResourceType { Topic, ConsumerGroup, Cluster, TransactionalId }
// public enum Operation { Read, Write, Create, Delete, Describe, Alter, All }
// public enum Permission { Allow, Deny }

Console.WriteLine("--- Permission Types ---");
Console.WriteLine("Resource Types: Topic, ConsumerGroup, Cluster, TransactionalId");
Console.WriteLine("Operations: Read, Write, Create, Delete, Describe, Alter, All");
Console.WriteLine("Permissions: Allow, Deny");
Console.WriteLine();

// TODO: Step 2 - Create AclEntry record
// public record AclEntry(
//     string Principal,
//     ResourceType ResourceType,
//     string ResourcePattern,
//     Operation Operation,
//     Permission Permission)
// {
//     public bool Matches(string resource) { ... }
// }

// TODO: Step 3 - Implement AclStore
// public class AclStore
// {
//     private readonly Dictionary<string, List<AclEntry>> _tenantAcls = new();
//     public void AddAcl(string tenantId, AclEntry acl) { ... }
//     public IEnumerable<AclEntry> GetAcls(string tenantId) { ... }
//     public void SetDefaultTenantAcls(string tenantId) { ... }
// }

// TODO: Step 4 - Create audit logging
// public interface IAuditLogger
// {
//     void LogAccess(...);
//     IEnumerable<AuditLogEntry> GetRecentLogs(int count);
// }
//
// public record AuditLogEntry(...);
//
// public class InMemoryAuditLogger : IAuditLogger { ... }

// TODO: Step 5 - Create AuthorizationService
// public class AuthorizationService
// {
//     public AuthorizationResult Authorize(
//         string tenantId,
//         string principal,
//         ResourceType resourceType,
//         string resource,
//         Operation operation) { ... }
// }

// TODO: Step 6 - Set up ACLs
// Console.WriteLine("--- Setting Up ACLs ---");
// var aclStore = new AclStore();
// var auditLogger = new InMemoryAuditLogger();
// var authService = new AuthorizationService(aclStore, auditLogger);
//
// aclStore.SetDefaultTenantAcls("acme");
// aclStore.SetDefaultTenantAcls("globex");
// Console.WriteLine("Created default ACLs for tenants: acme, globex");
//
// // Add custom deny rule
// aclStore.AddAcl("acme", new AclEntry(
//     "tenant:acme",
//     ResourceType.Topic,
//     "acme.audit",
//     Operation.Write,
//     Permission.Deny));
// Console.WriteLine("Added custom deny rule: acme cannot write to acme.audit");

// TODO: Step 7 - Display ACLs
// Console.WriteLine("\n--- ACL Entries for 'acme' ---");
// foreach (var acl in aclStore.GetAcls("acme"))
// {
//     Console.WriteLine($"  {acl.Principal} -> {acl.ResourceType}:{acl.ResourcePattern} : {acl.Operation} ({acl.Permission})");
// }

// TODO: Step 8 - Test authorization
// Console.WriteLine("\n--- Authorization Tests ---");
// var tests = new[]
// {
//     ("acme", "acme.orders", Operation.Write),
//     ("acme", "acme.audit", Operation.Read),
//     ("acme", "acme.audit", Operation.Write),
//     ("acme", "globex.orders", Operation.Read),
// };
//
// foreach (var (tenant, topic, op) in tests)
// {
//     var result = authService.Authorize(
//         tenant,
//         $"tenant:{tenant}",
//         ResourceType.Topic,
//         topic,
//         op);
//     Console.WriteLine($"{tenant} -> {topic}:{op} = {(result.IsAllowed ? "ALLOWED" : "DENIED")} ({result.Reason})");
//     Console.WriteLine();
// }

// TODO: Step 9 - Display audit summary
// Console.WriteLine("--- Audit Log Summary ---");
// var logs = auditLogger.GetRecentLogs(10);
// Console.WriteLine($"Total entries: {logs.Count()}");
// Console.WriteLine($"Allowed: {logs.Count(l => l.Allowed)}");
// Console.WriteLine($"Denied: {logs.Count(l => !l.Allowed)}");

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. ACLs enable fine-grained tenant access control");
Console.WriteLine("2. Pattern matching simplifies bulk permissions");
Console.WriteLine("3. Explicit deny always wins over allow");
Console.WriteLine("4. Audit logging is essential for compliance");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
