# Lesson 10.4: Compatibility Modes

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 10.3

Ensure safe schema evolution with compatibility rules.

## Objectives

- Understand different compatibility modes
- Implement compatibility checking
- Choose the right mode for your use case
- Handle incompatible schema changes

## Concepts

### Why Compatibility Matters

When schemas evolve, existing data and applications must continue to work:
- Old consumers may read new messages
- New consumers may read old messages
- Rolling deployments mix old and new versions

### Compatibility Modes

| Mode | Rule | Safe Changes |
|------|------|--------------|
| NONE | No checking | Any change |
| BACKWARD | New reads old | Add optional fields, remove fields |
| FORWARD | Old reads new | Add fields, remove optional fields |
| FULL | Both directions | Add/remove optional fields |

### Backward Compatibility

New schema can read data written with old schema:

```
Old Schema (v1)          New Schema (v2)
{ id, name }      ->     { id, name, email? }
                              ^
                              optional (has default)

Consumer v2 reads message from Producer v1: OK
  - id: present
  - name: present
  - email: missing -> use default
```

### Forward Compatibility

Old schema can read data written with new schema:

```
Old Schema (v1)          New Schema (v2)
{ id, name }      <-     { id, name, email }
                              ^
                              ignored by old consumer

Consumer v1 reads message from Producer v2: OK
  - id: present
  - name: present
  - email: ignored
```

### Full Compatibility

Both directions work (most restrictive):

```
v1: { id, name }
v2: { id, name, email? }  <-- email must be optional
                              (can be omitted AND ignored)
```

## Exercise

In this lesson, you'll:
1. Implement compatibility checker
2. Test backward compatibility
3. Test forward compatibility
4. Enforce compatibility on registration

## Key Takeaways

- Backward: new consumers, old data
- Forward: old consumers, new data
- Full: both directions (safest)
- Default to BACKWARD for most cases
