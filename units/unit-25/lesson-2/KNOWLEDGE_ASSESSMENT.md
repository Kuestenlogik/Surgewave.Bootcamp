# Knowledge Assessment: Lesson 16.2 - Topic Namespacing

## Multiple Choice

### 1. What is the primary benefit of topic namespacing in multi-tenant systems?
- [ ] A) Faster message delivery
- [ ] B) Logical isolation with data separation and easy ACL application
- [ ] C) Reduced storage requirements
- [ ] D) Automatic load balancing

<details>
<summary>Answer</summary>
**B)** Topic namespacing provides logical isolation at the messaging level, enabling data separation, easy ACL application based on prefixes, monitoring aggregation by tenant, and simplified cleanup of tenant resources.
</details>

### 2. Which naming convention strategy includes environment information in the topic name?
- [ ] A) Simple Prefix: `{tenant-id}.{topic-name}`
- [ ] B) Hierarchical Namespace: `{tenant-id}.{domain}.{topic-name}`
- [ ] C) Environment-Aware: `{env}.{tenant-id}.{topic-name}`
- [ ] D) Flat Naming: `{topic-name}`

<details>
<summary>Answer</summary>
**C)** The Environment-Aware strategy uses the format `{env}.{tenant-id}.{topic-name}` (e.g., `prod.acme.orders`) to enable environment separation and safe testing across dev, staging, and production.
</details>

### 3. In the namespace validation pipeline, what should happen when a topic name contains an unregistered tenant prefix?
- [ ] A) Create the tenant automatically
- [ ] B) Allow the operation with a warning
- [ ] C) Reject the request as an unknown tenant
- [ ] D) Route to a default tenant

<details>
<summary>Answer</summary>
**C)** The validation pipeline should reject the request if the tenant is not registered. This prevents unauthorized topic creation and maintains proper tenant isolation.
</details>

## True/False

### 4. Topic namespacing makes it harder to identify and remove all resources belonging to a specific tenant during offboarding.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Topic namespacing actually makes cleanup easier because all tenant resources share a common prefix. You can identify all tenant topics by filtering on the prefix (e.g., `acme.*`) and remove them during offboarding.
</details>

### 5. The hierarchical namespace strategy `{tenant-id}.{domain}.{topic-name}` is more complex but supports sub-domain organization within a tenant.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The hierarchical namespace (e.g., `acme.sales.orders`, `acme.inventory.stock-levels`) provides better organization for tenants with multiple domains or business areas, though it requires more complex naming rules.
</details>

## Code Challenge

### 6. What's wrong with this topic validation logic?
```csharp
public bool ValidateTopicName(string topicName)
{
    var parts = topicName.Split('.');
    var tenantId = parts[0];

    // Check if tenant exists
    return _tenantRegistry.Contains(tenantId);
}
```

<details>
<summary>Answer</summary>
The validation is incomplete. Issues include:
1. No null/empty check for topicName
2. No check that parts array has at least 2 elements (tenant + topic)
3. No validation of allowed characters (alphanumeric, dots, hyphens only)
4. No check for spaces or invalid characters

Improved version:
```csharp
public bool ValidateTopicName(string topicName)
{
    if (string.IsNullOrWhiteSpace(topicName))
        return false;

    var parts = topicName.Split('.');
    if (parts.Length < 2)
        return false;

    var tenantId = parts[0];
    if (!_tenantRegistry.Contains(tenantId))
        return false;

    // Validate characters (alphanumeric, dots, hyphens)
    return Regex.IsMatch(topicName, @"^[a-zA-Z0-9][a-zA-Z0-9.\-]*$");
}
```
</details>

### 7. Which component in the Namespace Manager Architecture is responsible for translating tenant and base topic into a fully qualified topic name?
- [ ] A) Surgewave Broker
- [ ] B) Naming Strategy
- [ ] C) Topic Registry
- [ ] D) ACL Manager

<details>
<summary>Answer</summary>
**B)** The Naming Strategy component within the Namespace Manager is responsible for operations like `BuildTopicName(tenant, base)`, `ParseTopicName(fullName)`, and `ValidateTopicName(fullName)`.
</details>

## Score
- 7/7: Excellent! You have mastered topic namespacing
- 5-6/7: Good understanding of namespace concepts
- 3-4/7: Review naming conventions and validation
- <3/7: Re-do the lesson focusing on namespacing strategies
