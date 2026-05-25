# Lesson 5.2: API Wrapper Migration

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 5.1

## Overview

Learn how to use Surgewave's Confluent.Kafka-compatible API wrapper for zero-code migration.

## Learning Objectives

- Understand the API wrapper approach
- Replace NuGet packages for migration
- Handle API differences
- Test migrated code

## Prerequisites

- Familiarity with Confluent.Kafka library
- Existing code using Confluent.Kafka

## The API Wrapper Approach

Surgewave provides a Confluent.Kafka-compatible NuGet package:

```
Before: Confluent.Kafka
After:  Kuestenlogik.Surgewave.Kafka.Compat
```

Your code stays the same - only the package reference changes.

## Key Benefits

| Benefit | Description |
|---------|-------------|
| **Zero code changes** | Same API, different implementation |
| **Gradual migration** | Test thoroughly before switching |
| **Fallback option** | Switch back if needed |

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
