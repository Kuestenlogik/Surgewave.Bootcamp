# Lesson 25.4: DAG Patterns - Fan-out, Fan-in

> **Difficulty:** Advanced | **Duration:** 50 min | **Prerequisites:** Lesson 25.3 completion

## Overview

Master advanced DAG patterns for visual pipelines including fan-out (splitting data to multiple paths), fan-in (merging multiple paths), parallel processing, and complex multi-stage topologies.

## Learning Objectives

- Implement fan-out patterns to distribute data to multiple processors
- Implement fan-in patterns to merge results from parallel paths
- Design parallel processing pipelines for performance
- Create complex multi-stage DAG topologies
- Understand pattern trade-offs and use cases

## Fan-Out Pattern

```mermaid
graph TD
    S["Source (S)"] --> T1["Transform T1"] & T2["Transform T2"] & T3["Transform T3"] & T4["Transform T4"]
    T1 --> K1["Sink K1"]
    T2 --> K2["Sink K2"]
    T3 --> K3["Sink K3"]
    T4 --> K4["Sink K4"]
```

Use cases: route orders to different fulfillment centers, send events to multiple analytics systems, parallel enrichment with different data sources.

## Fan-In Pattern

```mermaid
graph TD
    S1["Source S1"] --> T1["Transform T1"]
    S2["Source S2"] --> T2["Transform T2"]
    S3["Source S3"] --> T3["Transform T3"]
    S4["Source S4"] --> T4["Transform T4"]
    T1 & T2 & T3 & T4 --> M["Merge (M)"]
    M --> K["Single Sink (K)"]
```

Use cases: aggregate data from multiple regions, combine results from parallel processing, unified logging from multiple sources.

## Diamond Pattern (Fan-Out + Fan-In)

```mermaid
graph TD
    S["Source (S)"] --> T1["T1: Fast Path"] & T2["T2: Enrichment Path"]
    T1 & T2 --> M["Merge & Combine (M)"]
    M --> K["Sink (K)"]
```

## Conditional Branch Pattern

```mermaid
graph TD
    S["Source (S)"] --> B["Branch Router (B)"]
    B -->|high| T1["T1"] --> K1["K1: Priority"]
    B -->|medium| T2["T2"] --> K2["K2: Standard"]
    B -->|low| T3["T3"] --> K3["K3: Batch"]
    B -->|error| T4["T4"] --> K4["K4: Dead-Letter"]
```

## Parallel Processing Pattern

```mermaid
graph TD
    subgraph Source["Source Topic (Partitioned)"]
        P0S["P0"]
        P1S["P1"]
        P2S["P2"]
        P3S["P3"]
    end
    P0S --> W0["Worker W0"]
    P1S --> W1["Worker W1"]
    P2S --> W2["Worker W2"]
    P3S --> W3["Worker W3"]
    subgraph Sink["Sink Topic (Partitioned)"]
        P0K["P0"]
        P1K["P1"]
        P2K["P2"]
        P3K["P3"]
    end
    W0 --> P0K
    W1 --> P1K
    W2 --> P2K
    W3 --> P3K
```

## Exercise

In this exercise, you will:

1. Build a fan-out pipeline that routes orders to different processors
2. Implement a fan-in pipeline that aggregates results
3. Create a diamond pattern for parallel enrichment
4. Implement conditional branching based on data content
5. Execute all patterns with real Surgewave data flow

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
