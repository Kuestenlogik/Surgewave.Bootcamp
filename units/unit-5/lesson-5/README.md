# Lesson 7.5: State Stores

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 7.4

## Learning Objectives

- Understand state store types and their use cases
- Create and access KeyValue stores
- Use Window stores for time-based state
- Implement interactive queries for serving layer

## Key Concepts

### State Store Types

| Store Type | Use Case | Access Pattern |
|------------|----------|----------------|
| KeyValueStore | Current state | Point lookup, range scan |
| WindowStore | Time-windowed state | Key + time range |
| SessionStore | Session state | Key + session bounds |

### State Store Lifecycle

```
Input Topic → Processor → State Store
                ↑            ↓
            Changelog ← Persistence
```

State stores are:
- Backed by changelog topics for fault tolerance
- Restored on restart from changelog
- Queryable for interactive queries

## Exercise

Build a real-time analytics service:
1. Aggregate metrics into state stores
2. Expose state via interactive queries
3. Query windowed and session state

## Time Estimate

~45 minutes
