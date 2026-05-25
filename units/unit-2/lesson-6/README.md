# Lesson 2.6: Security & Governance

The Security and Governance sections provide UI-driven management for access control, quotas, schemas, and data governance.

## Learning Objectives

- Create and manage ACL rules through the UI
- Define RBAC roles with fine-grained permissions
- Configure resource quotas for producers, consumers, and connections
- Use the data catalog for topic metadata and lineage
- Manage dead letter queues and the schema registry

## ACL Management

Create and manage Access Control Lists with fields for principal, resource type, resource name, operation, and permission (Allow/Deny).

## Role Management (RBAC)

Define roles with fine-grained permissions and assign them to users or groups:

| Built-in Role | Permissions |
|---------------|-------------|
| Admin | Full access to all resources |
| Operator | Manage topics, groups, connectors; read-only security |
| Developer | Read/write topics, manage own consumer groups |
| Viewer | Read-only access to all resources |
| Pipeline-Manager | Full pipeline CRUD, read topics |

Custom roles can be created by combining individual permissions.

## Quota Management

| Quota Type | Description | Example |
|------------|-------------|---------|
| Producer Byte Rate | Max bytes/sec a producer can send | 10 MB/s per client |
| Consumer Byte Rate | Max bytes/sec a consumer can fetch | 20 MB/s per client |
| Request Rate | Max requests/sec per connection | 100 req/s |
| Connection Count | Max simultaneous connections | 50 per user |

## Data Catalog

The data catalog provides a searchable registry of all topics with:

- Topic descriptions and ownership
- Schema association
- Data classification (Public, Internal, Confidential, Restricted)
- Data lineage visualization
- Team/individual ownership assignment

## Dead Letter Queue Management

Manage DLQs with actions to inspect failed messages, replay selected or all messages, and purge investigated queues.

## Schema Registry Browser

Browse, compare, and manage schemas with a visual interface including a diff viewer for comparing schema versions and a reference graph for schema dependencies.

## Exercise

1. Navigate to Security > Roles
2. Create a custom role `data-engineer` with Read/Write on `etl-*` topics
3. Create a custom role `analyst` with Read-only on `analytics-*` topics
4. Assign the roles to test users and verify access is enforced

---

[Back to Unit 2](../README.md)
