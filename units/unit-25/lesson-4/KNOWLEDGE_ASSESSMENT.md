# Knowledge Assessment: Lesson 16.4 - Access Control

## Multiple Choice

### 1. What is the primary purpose of tenant-scoped ACLs in multi-tenant systems?
- [ ] A) Improve message throughput
- [ ] B) Ensure tenants can only access their own resources while enabling controlled sharing
- [ ] C) Reduce storage costs
- [ ] D) Speed up topic creation

<details>
<summary>Answer</summary>
**B)** Tenant-scoped ACLs ensure data isolation by restricting tenants to their own resources, while still allowing controlled cross-tenant access when needed. This prevents data leakage and unauthorized access.
</details>

### 2. In ACL pattern matching, which pattern type would match both "acme.orders" and "acme.events"?
- [ ] A) Literal Match: "acme.orders"
- [ ] B) Prefix Match: "acme.*"
- [ ] C) Wildcard Match: "orders"
- [ ] D) Exact Match: "acme.events"

<details>
<summary>Answer</summary>
**B)** Prefix Match with "acme.*" matches any topic starting with "acme." including "acme.orders", "acme.events", and "acme.anything".
</details>

### 3. In the permission model, which resource type would you use to control who can read from or write to a specific message stream?
- [ ] A) CLUSTER
- [ ] B) GROUP
- [ ] C) TOPIC
- [ ] D) TRANSACT

<details>
<summary>Answer</summary>
**C)** TOPIC resource type controls Read, Write, Create, Delete, Describe, and Alter operations on message topics/streams.
</details>

## True/False

### 4. When evaluating ACLs, an explicit Deny rule takes precedence over an Allow rule for the same resource.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - In ACL evaluation, explicit Deny always wins over Allow. For example, if a tenant has "acme.*:Write Allow" but also "acme.audit:Write Deny", write access to acme.audit will be denied despite the broader allow rule.
</details>

### 5. Audit logs should only record successful access attempts, not denied requests.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Audit logs should record ALL access attempts, both allowed and denied. Denied requests are especially important for security monitoring as they may indicate unauthorized access attempts or misconfigurations.
</details>

## Code Challenge

### 6. What's wrong with this ACL entry?
```csharp
var aclEntry = new AclEntry
{
    Principal = "tenant:acme",
    ResourceType = "Topic",
    ResourcePattern = "*",
    Operation = "Write",
    Permission = "Allow"
};
```

<details>
<summary>Answer</summary>
The ResourcePattern is set to "*" which matches ALL topics across ALL tenants. This is a security vulnerability as it would allow tenant "acme" to write to any tenant's topics.

The correct pattern should be tenant-scoped:
```csharp
var aclEntry = new AclEntry
{
    Principal = "tenant:acme",
    ResourceType = "Topic",
    ResourcePattern = "acme.*",  // Only acme's topics
    Operation = "Write",
    Permission = "Allow"
};
```
</details>

### 7. According to the audit log architecture, which of the following is NOT a typical destination for audit logs?
- [ ] A) Local File (audit.log)
- [ ] B) Surgewave Topic (audit.events)
- [ ] C) External SIEM (Splunk, etc.)
- [ ] D) Direct email to administrators

<details>
<summary>Answer</summary>
**D)** Direct email to administrators is not a typical audit log destination. The standard destinations are: Local File for simple storage, Surgewave Topic for event-driven processing, and External SIEM systems (like Splunk) for enterprise security monitoring and compliance.
</details>

## Score
- 7/7: Excellent! You have mastered access control concepts
- 5-6/7: Good understanding of ACLs and authorization
- 3-4/7: Review pattern matching and audit logging
- <3/7: Re-do the lesson focusing on ACL architecture
