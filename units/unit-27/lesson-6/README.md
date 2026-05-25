# Lesson 27.6: Alternative Storage Engines

Surgewave supports pluggable storage engines beyond local disk and object stores. Each engine is optimized for different access patterns and workloads.

## Learning Objectives

- Compare storage engines across the write-optimized to read-optimized spectrum
- Configure RocksDB, SQLite, DuckDB, LMDB, Parquet, NVMe-Direct, and Arrow
- Run OLAP queries on stored messages using DuckDB
- Select the right engine for your target workload

## Available Engines

| Engine | Type | Best For |
|--------|------|----------|
| Local (default) | Log-based | General purpose messaging |
| RocksDB | LSM-tree | Stream processing state stores |
| SQLite | B-tree | Edge / IoT deployments |
| DuckDB | OLAP columnar | Analytics / SQL queries on messages |
| LMDB | Memory-mapped B+ tree | Ultra-fast reads, zero-copy |
| Parquet | Columnar files | Data lake integration |
| NVMe-Direct | Raw block I/O | Maximum raw performance |
| Arrow | In-memory columnar | Inter-process data sharing |

## DuckDB: SQL on Messages

```sql
SELECT
    json_extract(value, '$.customerId') AS customer_id,
    SUM(json_extract(value, '$.total')) AS total_revenue
FROM messages
WHERE topic = 'orders'
  AND timestamp >= NOW() - INTERVAL '1 hour'
GROUP BY customer_id
ORDER BY total_revenue DESC
LIMIT 10;
```

## Engine Selection Guide

| Workload | Recommended Engine |
|----------|-------------------|
| General purpose messaging | Local (default) |
| Cloud-native / Kubernetes | S3Direct / AzureBlob / GCS |
| Stream processing state | RocksDB |
| Edge / IoT deployment | SQLite |
| Analytics / OLAP queries | DuckDB |
| Ultra-fast reads | LMDB |
| Data lake integration | Parquet |
| Maximum raw performance | NVMe-Direct |
| Inter-process data sharing | Arrow |

## Exercise

1. Configure Surgewave with the DuckDB storage engine
2. Produce 10,000 JSON order messages with varied fields
3. Query total revenue by region, top customers, and hourly averages using SQL
4. Compare query performance against application-level processing

---

[Back to Unit 30](../README.md)
