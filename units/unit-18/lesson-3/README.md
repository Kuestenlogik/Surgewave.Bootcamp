# Lesson 22.3: Vector Databases - Qdrant

## Overview

Learn how to store and query vector embeddings using Qdrant, a high-performance vector database. Vector databases are essential for building scalable semantic search and AI applications.

## Learning Objectives

- Understand what vector databases are and why they're needed
- Set up and configure Qdrant
- Create collections with proper vector configuration
- Insert and query vectors with metadata
- Implement filtered vector search

## Why Vector Databases?

| | Traditional Database (SQL) | Vector Database (Qdrant) |
|-|---------------------------|--------------------------|
| **Queries** | Exact match (`WHERE title = '...'`); range queries; full-text search with inverted indices | Similarity search ("find semantically similar to X"); Approximate Nearest Neighbor (ANN) |
| **Index** | B-tree, inverted index | HNSW (Hierarchical Navigable Small World) |
| **Designed for** | Structured, relational data | High-dimensional vectors (1000+ dimensions) |

## Qdrant Architecture

A Qdrant collection ("documents") contains points, each with an ID, a vector, and a metadata payload:

| Field | Point 1 | Point 2 |
|-------|---------|---------|
| ID | `"doc-001"` | `"doc-002"` |
| Vector | `[0.12, -0.34, 0.56, ...]` (1536 dims) | `[0.45, 0.23, -0.12, ...]` (1536 dims) |
| Payload | `{ "category": "tech", ... }` | `{ "category": "ai", ... }` |

The HNSW index enables O(log n) approximate nearest-neighbor search vs O(n) brute force.

## Running Qdrant

```bash
# Docker (recommended)
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant

# Or with persistent storage
docker run -p 6333:6333 -p 6334:6334 \
    -v $(pwd)/qdrant_storage:/qdrant/storage \
    qdrant/qdrant

# Verify it's running
curl http://localhost:6333/collections
```

## Qdrant API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `PUT` | `/collections/{name}` | Create collection |
| `GET` | `/collections/{name}` | Get collection info |
| `DELETE` | `/collections/{name}` | Delete collection |
| `PUT` | `/collections/{name}/points` | Upsert points |
| `POST` | `/collections/{name}/points/search` | Search vectors |
| `GET` | `/collections/{name}/points/{id}` | Get point by ID |

## Distance Metrics

| Metric | Description | Best For |
|--------|-------------|----------|
| Cosine | Angle between vectors (normalized) | Text embeddings |
| Euclidean | Straight-line distance | Image embeddings |
| Dot Product | Raw dot product | Pre-normalized vectors |

## Filtered Search

Query: `"machine learning applications"`, Filter: `category = "tech" AND date > "2024-01-01"`

1. Apply metadata filter to reduce search space
2. Search vectors only within filtered subset
3. Return top-k most similar results

Benefits: faster search (smaller search space), more relevant results (semantic + metadata filtering), supports complex filter conditions.

## Best Practices

| Practice | Description |
|----------|-------------|
| Choose correct distance metric | Cosine for normalized embeddings |
| Use payload filtering | Combine semantic search with metadata |
| Batch upserts | Insert multiple points at once |
| Set collection size | Pre-allocate for known dataset sizes |
| Monitor performance | Track query latency and throughput |
| Use HNSW parameters | Tune ef and m for speed/accuracy trade-off |

## Exercise

In this exercise, you will:

1. Connect to Qdrant
2. Create a collection with vector configuration
3. Insert documents with embeddings
4. Perform vector similarity search
5. Implement filtered search with metadata

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
