# Lesson 25.3: Connector Nodes and Configuration

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 25.2 completion

## Overview

Master the configuration of connector nodes in visual pipelines. Learn how to configure source connectors for data ingestion, transform nodes for processing, and sink connectors for data output.

## Learning Objectives

- Configure source connectors with various data sources
- Understand transform node types and their configurations
- Set up sink connectors for different output destinations
- Implement custom node configurations with validation
- Create reusable connector templates

## Source Connector Types

| Source Type | Config Properties |
|-------------|-------------------|
| **Topic Source** | `topic`, `format` (json/avro/protobuf/raw), `startOffset` (earliest/latest/timestamp), `consumerGroup` (optional) |
| **File Source** | `path` (glob), `format` (json/csv/parquet), `pollInterval`, `processedDir` |
| **HTTP Source (Webhook)** | `endpoint`, `port`, `authentication` (bearer/basic/none) |
| **Timer Source** | `interval` (1s/1m/1h), `payload` (JSON template) |

## Transform Node Types

| Transform | Purpose | Key Config |
|-----------|---------|------------|
| **Filter** | Pass only records matching a condition | `expression`, `dropOnError` |
| **Map** | Transform record values | `expression`, `outputFormat` |
| **FlatMap** | One-to-many transformation | `expression`, `unwrapArray` |
| **Aggregate** | Group and aggregate records | `groupBy`, `aggregation`, `windowType` (tumbling/sliding/session), `windowSize` |
| **Join** | Combine records from multiple sources | `leftKey`, `rightKey`, `joinType` (inner/left/outer), `windowSize` |

## Sink Connector Types

| Sink Type | Key Config |
|-----------|------------|
| **Topic Sink** | `topic`, `format` (json/avro), `keyExpression`, `partitioner` (roundRobin/byKey/custom) |
| **File Sink** | `path`, `format` (json/csv/parquet), `fileNaming` (date/sequence/custom), `rolloverSize`, `rolloverInterval` |
| **HTTP Sink** | `url`, `method`, `headers`, `batchSize`, `retryPolicy` (maxRetries, backoff) |
| **Console Sink (Debug)** | `format` (pretty/compact/raw), `includeTimestamp`, `includeMetadata` |

## Remote Worker Connectors

When remote workers are connected to the cluster, the Node Palette includes connectors advertised by those workers. Remote-capable connectors display a **worker availability badge** showing how many workers can run them. When configuring a remote-capable node, the Properties Panel includes a **Worker Selector** dropdown where you choose the target worker for execution. This enables resource isolation (e.g., running an LLM node on a GPU-equipped worker) without changing the pipeline topology.

## Node Configuration Patterns

| Pattern | Description | Use Case |
|---------|-------------|----------|
| Template | Pre-configured node with defaults | Rapid development |
| Parameterized | Configuration with variables | Environment-specific settings |
| Validated | Config with validation rules | Production safety |
| Dynamic | Runtime-configurable settings | A/B testing, feature flags |

## Exercise

In this exercise, you will:

1. Create detailed node configuration classes
2. Implement configuration validation for each node type
3. Build a pipeline with properly configured connectors
4. Create reusable connector templates
5. Execute the pipeline with real data

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
