# Unit 27: Object Store & Zero-Disk Mode

Master cloud object stores as primary storage backends for Surgewave, enabling zero-disk architectures, hybrid tiered storage strategies, and alternative storage engines for specialized workloads.

## Learning Objectives

By the end of this unit, you will be able to:

- Understand the trade-offs between local disk and object store storage
- Configure S3-Direct, Azure Blob, and GCP Cloud Storage as primary storage engines
- Design hybrid storage strategies with hot and cold tiers
- Select the right alternative storage engine for your workload
- Run OLAP queries directly on stored messages using DuckDB

## Lessons

| Lesson | Topic | Description |
|--------|-------|-------------|
| [27.1](lesson-1/README.md) | Zero-Disk Architecture | Trade-offs, when to use, architecture overview |
| [27.2](lesson-2/README.md) | S3-Direct Storage | S3 config, MinIO, multipart upload, consistency |
| [27.3](lesson-3/README.md) | Azure Blob Storage | Container setup, managed identity, GRS |
| [27.4](lesson-4/README.md) | GCP Cloud Storage | Bucket setup, storage classes, cross-region replication |
| [27.5](lesson-5/README.md) | Hybrid Storage Strategies | Tiered policies, cost modeling, migration |
| [27.6](lesson-6/README.md) | Alternative Storage Engines | RocksDB, SQLite, DuckDB, LMDB, Parquet, NVMe, Arrow |

## Prerequisites

- Completed Unit 1: Surgewave Fundamentals
- Completed Unit 3: Topics & Partitions
- Completed Unit 5: Storage & Retention
- Access to a cloud provider account or MinIO for local testing

## Why Object Store Storage?

| | Local Disk (Traditional) | Object Store (S3, Azure Blob, GCS) |
|---|---|---|
| Latency | Ultra-low (sub-millisecond) | Higher (5–50ms per request) |
| Network overhead | No round-trips | Network round-trip per request |
| Configuration | Simple | Minimal (managed by provider) |
| Capacity | Limited by node hardware | Virtually unlimited |
| Durability | Depends on replication | Built-in (11 nines) |
| Cost | Expensive NVMe/SSD at scale | Pay only for what you store |
| Provisioning | Must provision for peak | Auto-scales on demand |
| Egress | Free | Cross-region reads incur costs |
| Consistency | Strong | Mostly strong (S3 since 2020) |

## Zero-Disk Architecture Overview

```mermaid
graph TD
    subgraph Producers
        A[App A]
        B[App B]
        C[App C]
    end
    Broker["Surgewave Broker (Stateless)<br/>No local disk —<br/>segments written directly to object store"]
    OS["Object Store<br/>S3 / Azure Blob / GCS<br/>11 nines durability"]
    subgraph Consumers
        X[Svc X]
        Y[Svc Y]
        Z[Svc Z]
    end
    Producers --> Broker
    Broker --> OS
    OS --> Consumers
```

---

## Lesson 27.1: Zero-Disk Architecture

In a zero-disk architecture, the Surgewave broker does not use local disk for message storage. Instead, all segments are written directly to a cloud object store. The broker becomes effectively stateless, making it easy to scale horizontally, replace instances, and run in serverless environments.

### When to Use Zero-Disk

| Scenario | Zero-Disk Fit |
|----------|---------------|
| Cloud-native / Kubernetes | Excellent -- no persistent volumes needed |
| Serverless / ephemeral compute | Excellent -- instances can be replaced freely |
| Cost optimization at scale | Good -- object storage is 10-50x cheaper per GB than SSD |
| Ultra-low latency required | Poor -- object store adds 5-50ms per operation |
| Edge / IoT with limited connectivity | Poor -- requires reliable network to object store |
| Compliance requiring data durability | Excellent -- 11 nines durability built in |

### Architecture: Broker as Stateless Gateway

**Traditional:** Producer → Broker → [Local Disk] → Consumer (replication to other brokers)

**Zero-Disk:** Producer → Broker (stateless) → [Object Store] → Consumer (built-in replication by cloud provider)

### Trade-offs

| Dimension | Local Disk | Zero-Disk |
|-----------|-----------|-----------|
| Write Latency | < 1ms (NVMe) | 5-20ms (object store PUT) |
| Read Latency | < 1ms (cached) | 10-50ms (object store GET) |
| Throughput | Disk IOPS limited | Network bandwidth limited |
| Durability | Depends on replication | 99.999999999% (11 nines) |
| Cost per GB/month | $0.10-0.50 (SSD) | $0.01-0.03 (object store) |
| Scaling | Add disks or nodes | Unlimited, auto-scales |
| Recovery | Rebuild from replicas | Instant (no local state) |

### Enabling Zero-Disk Mode

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "S3Direct",
            "S3": {
                "Bucket": "surgewave-cluster-prod",
                "Region": "us-east-1",
                "Prefix": "segments/"
            }
        }
    }
}
```

---

## Lesson 27.2: S3-Direct Storage

S3-Direct writes message segments directly to Amazon S3 or any S3-compatible service. This is the most widely supported object store backend.

### Configuration

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "S3Direct",
            "S3": {
                "Bucket": "surgewave-data",
                "Region": "us-east-1",
                "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
                "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                "ServiceUrl": null,
                "ForcePathStyle": false,
                "Prefix": "v1/segments/",
                "MultipartThresholdBytes": 5242880
            }
        }
    }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| Bucket | S3 bucket name | (required) |
| Region | AWS region | (required) |
| AccessKeyId | AWS access key (or use IAM role) | null |
| SecretAccessKey | AWS secret key (or use IAM role) | null |
| ServiceUrl | Custom endpoint for S3-compatible services | null (AWS) |
| ForcePathStyle | Use path-style URLs (required for MinIO) | false |
| Prefix | Key prefix for all objects | "" |
| MultipartThresholdBytes | Switch to multipart upload above this size | 5 MB |

### S3-Compatible Services

Surgewave's S3-Direct engine works with any S3-compatible service:

| Service | ServiceUrl | ForcePathStyle | Notes |
|---------|-----------|----------------|-------|
| AWS S3 | null | false | Native support |
| MinIO | `http://localhost:9000` | true | Local development |
| Backblaze B2 | `https://s3.us-west-004.backblazeb2.com` | false | Cost-effective |
| Wasabi | `https://s3.wasabisys.com` | false | No egress fees |
| DigitalOcean Spaces | `https://nyc3.digitaloceanspaces.com` | false | Simple pricing |
| Cloudflare R2 | `https://<account>.r2.cloudflarestorage.com` | true | Zero egress fees |

### Object Key Structure

Segments are stored with a predictable key structure for efficient listing and retrieval:

```
{prefix}/{topic}/{partition}/{segment-offset}.log
{prefix}/{topic}/{partition}/{segment-offset}.index
{prefix}/{topic}/{partition}/{segment-offset}.timeindex

Example:
v1/segments/orders/3/00000000000010000.log
v1/segments/orders/3/00000000000010000.index
v1/segments/orders/3/00000000000010000.timeindex
```

### Multipart Upload

For segments larger than `MultipartThresholdBytes` (default 5 MB), Surgewave uses S3 multipart upload to improve upload reliability and throughput. A 32 MB segment is split into 6 parts (5 × 5 MB + 1 × 2 MB), uploaded in parallel, then finalized with `CompleteMultipartUpload`.

Benefits:
- Parts uploaded in parallel (configurable concurrency)
- Failed parts can be retried independently
- Better throughput for large segments

### Read-After-Write Consistency

AWS S3 provides strong read-after-write consistency as of December 2020. After a successful PUT, subsequent GET requests always return the latest version. This eliminates the need for read-your-writes workarounds that older S3 integrations required.

### Local Development with MinIO

```bash
# Start MinIO in Docker
docker run -d \
  --name minio \
  -p 9000:9000 \
  -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  minio/minio server /data --console-address ":9001"

# Create the bucket
docker exec minio mc mb /data/surgewave-dev
```

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "S3Direct",
            "S3": {
                "Bucket": "surgewave-dev",
                "Region": "us-east-1",
                "AccessKeyId": "minioadmin",
                "SecretAccessKey": "minioadmin",
                "ServiceUrl": "http://localhost:9000",
                "ForcePathStyle": true
            }
        }
    }
}
```

---

## Lesson 27.3: Azure Blob Storage

Azure Blob Storage provides a native object store backend with strong integration into the Azure ecosystem, including managed identity, geo-redundant storage, and lifecycle management.

### Configuration

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "AzureBlobDirect",
            "AzureBlob": {
                "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=surgewavedata;AccountKey=...;EndpointSuffix=core.windows.net",
                "ContainerName": "surgewave-segments",
                "Prefix": "v1/segments/",
                "UseManagedIdentity": false,
                "ManagedIdentityClientId": null
            }
        }
    }
}
```

### Managed Identity (Recommended for Production)

Instead of connection strings, use Azure Managed Identity for secure, credential-free authentication:

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "AzureBlobDirect",
            "AzureBlob": {
                "AccountName": "surgewavedata",
                "ContainerName": "surgewave-segments",
                "UseManagedIdentity": true,
                "ManagedIdentityClientId": "12345678-1234-1234-1234-123456789012"
            }
        }
    }
}
```

Assign the `Storage Blob Data Contributor` role to the managed identity on the storage account.

### Block Blob vs Page Blob

| Feature | Block Blob | Page Blob |
|---------|-----------|-----------|
| Max Size | 190.7 TiB | 8 TiB |
| Write Pattern | Append/replace blocks | Random read/write |
| Ideal For | Log segments (sequential writes) | Index files (random reads) |
| Cost | Lower | Higher |
| Surgewave Default | Segments (.log) | Index files (.index) |

Surgewave automatically selects the appropriate blob type based on the file being stored.

### Lifecycle Management Policies

Configure Azure Blob lifecycle rules to automatically tier or delete old segments:

```json
{
    "rules": [
        {
            "name": "tier-old-segments",
            "type": "Lifecycle",
            "definition": {
                "filters": {
                    "blobTypes": ["blockBlob"],
                    "prefixMatch": ["v1/segments/"]
                },
                "actions": {
                    "baseBlob": {
                        "tierToCool": { "daysAfterModificationGreaterThan": 30 },
                        "tierToArchive": { "daysAfterModificationGreaterThan": 90 },
                        "delete": { "daysAfterModificationGreaterThan": 365 }
                    }
                }
            }
        }
    ]
}
```

### Geo-Redundant Storage (GRS)

| Redundancy | Copies | Regions | Durability | Use Case |
|------------|--------|---------|------------|----------|
| LRS | 3 | 1 | 11 nines | Dev/Test |
| ZRS | 3 | 1 (3 zones) | 12 nines | Production (single region) |
| GRS | 6 | 2 | 16 nines | Disaster recovery |
| GZRS | 6 | 2 (3 zones primary) | 16 nines | Mission-critical production |

For disaster recovery, configure GRS or GZRS and set up a read-access secondary (RA-GRS/RA-GZRS) for failover reads.

---

## Lesson 27.4: GCP Cloud Storage

Google Cloud Storage provides a unified object storage service with multiple storage classes for cost optimization.

### Configuration

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "GcpCloudStorageDirect",
            "Gcp": {
                "ProjectId": "my-surgewave-project",
                "Bucket": "surgewave-segments-prod",
                "Prefix": "v1/segments/",
                "CredentialPath": "/etc/surgewave/gcp-sa-key.json",
                "UseWorkloadIdentity": false
            }
        }
    }
}
```

### Workload Identity (Recommended for GKE)

When running Surgewave on GKE, use Workload Identity for secure authentication without service account keys:

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "GcpCloudStorageDirect",
            "Gcp": {
                "ProjectId": "my-surgewave-project",
                "Bucket": "surgewave-segments-prod",
                "UseWorkloadIdentity": true
            }
        }
    }
}
```

Bind the Kubernetes service account to a GCP service account with `roles/storage.objectAdmin`.

### Storage Classes

| Class | Min Duration | Retrieval Cost | Storage Cost | Use Case |
|-------|-------------|----------------|--------------|----------|
| Standard | None | Free | $0.020/GB | Hot data (< 30 days) |
| Nearline | 30 days | $0.01/GB | $0.010/GB | Infrequent access (monthly) |
| Coldline | 90 days | $0.02/GB | $0.004/GB | Rare access (quarterly) |
| Archive | 365 days | $0.05/GB | $0.0012/GB | Compliance, long-term storage |

### Object Versioning

Enable object versioning on the bucket for point-in-time recovery:

```bash
gcloud storage buckets update gs://surgewave-segments-prod --versioning
```

With versioning enabled, overwritten or deleted segments can be recovered by listing object versions. Surgewave does not overwrite segments under normal operation, but versioning provides a safety net against accidental deletions.

### Cross-Region Replication

For multi-region disaster recovery, use dual-region or multi-region bucket locations:

| Location Type | Example | Latency | Availability | Cost |
|---------------|---------|---------|--------------|------|
| Region | us-east1 | Lowest | 99.9% | Lowest |
| Dual-Region | us-east1 + us-west1 | Low | 99.95% | +20% |
| Multi-Region | US (multiple regions) | Low | 99.95% | +20% |

```bash
# Create a dual-region bucket
gcloud storage buckets create gs://surgewave-segments-dr \
  --location=us-east1+us-west1 \
  --default-storage-class=STANDARD
```

---

## Lesson 27.5: Hybrid Storage Strategies

Most production deployments benefit from a hybrid approach: fast local storage for recent (hot) data and cheap object storage for older (cold) data.

### Tiered Storage Architecture

| Tier | Storage | Data | Latency | Cost/GB/month |
|------|---------|------|---------|--------------|
| Hot | Local NVMe/SSD | Recent segments (last N hours/days) | Sub-millisecond | $0.10–0.50 |
| Cold | Object Store | Older segments (beyond retention window) | 10–50ms | $0.01–0.03 |

**Tiering Policies:**
- **Age-based:** Move segments older than 24 hours
- **Size-based:** Move when local storage exceeds 80%
- **Combined:** Move if older than 6 hours AND local > 60%

### Configuration

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "Tiered",
            "Hot": {
                "Engine": "Local",
                "DataDirectory": "/var/surgewave/data"
            },
            "Cold": {
                "Engine": "S3Direct",
                "S3": {
                    "Bucket": "surgewave-archive",
                    "Region": "us-east-1"
                }
            },
            "TieringPolicy": {
                "Type": "AgeBased",
                "MaxHotAge": "24:00:00",
                "CheckInterval": "01:00:00"
            }
        }
    }
}
```

### Tiering Policy Types

| Policy | Trigger | Configuration | Best For |
|--------|---------|---------------|----------|
| AgeBased | Segment age exceeds threshold | `MaxHotAge: 24:00:00` | Predictable workloads |
| SizeBased | Local storage exceeds percentage | `MaxHotSizePercent: 80` | Limited local storage |
| Combined | Age OR size threshold hit | Both settings | Production clusters |

### Cost Modeling

Cost comparison for 1 TB retained over 30 days:

| Strategy | Storage | Requests | Total/month | Savings vs All-Local |
|----------|---------|----------|-------------|---------------------|
| All-Local (NVMe SSD) | 1 TB × $0.20/GB = $200 | — | ~$200 | — |
| All-Object-Store (S3 Standard) | 1 TB × $0.023/GB = $23 | ~$0.90 (PUT + GET) | ~$24 | 88% |
| Hybrid (100 GB local + 900 GB S3) | $20 + $20.70 | ~$1 | ~$42 | 79% |

### Migration Between Storage Engines

Surgewave supports online migration between storage engines without downtime:

```bash
# Start migration from local to S3
surgewave-admin storage migrate \
  --from Local \
  --to S3Direct \
  --s3-bucket surgewave-prod \
  --s3-region us-east-1 \
  --parallel 4 \
  --throttle 100MB/s
```

The migration runs in the background, copying segments to the new storage engine while the broker continues serving reads and writes. Once all segments are migrated, the broker switches to the new engine atomically.

### Performance Benchmarks

| Metric | Local NVMe | S3-Direct | Hybrid (Hot) | Hybrid (Cold) |
|--------|-----------|-----------|--------------|---------------|
| Write Latency P50 | 0.3ms | 12ms | 0.3ms | 12ms |
| Write Latency P99 | 1.2ms | 45ms | 1.2ms | 45ms |
| Read Latency P50 | 0.2ms | 18ms | 0.2ms | 18ms |
| Read Latency P99 | 0.8ms | 80ms | 0.8ms | 80ms |
| Throughput (Write) | 1.2 GB/s | 200 MB/s | 1.2 GB/s | 200 MB/s |
| Throughput (Read) | 2.0 GB/s | 400 MB/s | 2.0 GB/s | 400 MB/s |

Hot-tier reads in a hybrid setup match local disk performance. Cold-tier reads have object store latency but are typically for historical queries where higher latency is acceptable.

---

## Lesson 27.6: Alternative Storage Engines

Surgewave supports pluggable storage engines beyond local disk and object stores. Each engine is optimized for different access patterns and workloads.

### Engine Overview

**Write-Optimized → Read-Optimized spectrum:**

NVMe-Direct (raw blocks) → Local (log) → RocksDB (LSM) → LMDB (B+) → SQLite (B-tree) → DuckDB (OLAP) → Parquet (columnar)

**Latency-Optimized → Throughput-Optimized spectrum:**

NVMe-Direct (zero-copy) → LMDB (mmap) → Local (seq) → RocksDB (batch) → Arrow (IPC) → Parquet (scan) → DuckDB (OLAP)

### RocksDB

RocksDB uses a Log-Structured Merge-Tree (LSM) for high write throughput with background compaction.

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "RocksDb",
            "RocksDb": {
                "DataDirectory": "/var/surgewave/rocksdb",
                "WriteBufferSize": 67108864,
                "MaxWriteBufferNumber": 3,
                "TargetFileSizeBase": 67108864,
                "MaxBackgroundCompactions": 4,
                "CompressionType": "Snappy"
            }
        }
    }
}
```

| Strength | Weakness |
|----------|----------|
| High write throughput | Write amplification from compaction |
| Good compression ratios | Compaction can cause latency spikes |
| Kafka Streams compatible | Complex tuning for optimal performance |
| Point lookups and range scans | Higher CPU usage than simpler engines |

### SQLite

SQLite provides a single-file database, ideal for edge deployments and IoT devices where simplicity and reliability are paramount.

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "Sqlite",
            "Sqlite": {
                "DataDirectory": "/var/surgewave/sqlite",
                "JournalMode": "WAL",
                "SynchronousMode": "Normal",
                "CacheSize": 10000,
                "PageSize": 4096
            }
        }
    }
}
```

| Strength | Weakness |
|----------|----------|
| Zero configuration | Single-writer (no concurrent writes) |
| Single file per partition | Not suited for high-throughput |
| ACID compliant | Limited to ~1 TB per database |
| Ideal for edge/IoT | No built-in replication |

### DuckDB

DuckDB is an in-process OLAP database that enables SQL queries directly on stored messages. This is ideal for analytics workloads.

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "DuckDb",
            "DuckDb": {
                "DataDirectory": "/var/surgewave/duckdb",
                "ThreadCount": 4,
                "MemoryLimit": "4GB"
            }
        }
    }
}
```

Run OLAP queries on message data:

```sql
-- Query messages directly using SQL
SELECT
    key,
    json_extract(value, '$.customerId') AS customer_id,
    json_extract(value, '$.total') AS order_total,
    timestamp
FROM messages
WHERE topic = 'orders'
  AND timestamp >= NOW() - INTERVAL '1 hour'
GROUP BY customer_id
ORDER BY SUM(order_total) DESC
LIMIT 10;
```

| Strength | Weakness |
|----------|----------|
| Full SQL support | Higher write latency than log-based |
| Columnar storage (fast scans) | Not optimized for point lookups |
| Excellent compression | Higher memory usage for analytics |
| Apache Parquet import/export | Single-process (no multi-node) |

### LMDB

LMDB (Lightning Memory-Mapped Database) uses memory-mapped I/O for ultra-fast reads with zero-copy access.

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "Lmdb",
            "Lmdb": {
                "DataDirectory": "/var/surgewave/lmdb",
                "MapSize": 10737418240,
                "MaxReaders": 126,
                "NoSync": false
            }
        }
    }
}
```

| Strength | Weakness |
|----------|----------|
| Zero-copy reads via mmap | Database size limited by address space |
| No compaction needed | Single writer at a time |
| Crash-safe (MVCC) | Map size must be pre-configured |
| Extremely low read latency | Not ideal for high write throughput |

### Parquet

Apache Parquet provides columnar storage format for analytical workloads, with native integration into Spark, DuckDB, and other data processing frameworks.

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "Parquet",
            "Parquet": {
                "DataDirectory": "/var/surgewave/parquet",
                "RowGroupSize": 1048576,
                "CompressionCodec": "Snappy",
                "EnableDictionary": true
            }
        }
    }
}
```

| Strength | Weakness |
|----------|----------|
| Columnar (fast analytical scans) | High write latency (buffered) |
| Excellent compression (70-90%) | Cannot update individual records |
| Spark/DuckDB/Presto compatible | Not suited for real-time reads |
| Schema evolution support | Row-level access is slow |

### NVMe-Direct

NVMe-Direct bypasses the filesystem entirely, writing block-aligned I/O directly to NVMe devices for maximum throughput.

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "NvmeDirect",
            "NvmeDirect": {
                "DevicePath": "/dev/nvme0n1",
                "BlockSize": 4096,
                "WriteThrough": true,
                "IoDepth": 64,
                "UseDirectIo": true
            }
        }
    }
}
```

| Strength | Weakness |
|----------|----------|
| Lowest possible latency | Requires dedicated NVMe device |
| Maximum IOPS utilization | No filesystem (manual space management) |
| Zero kernel buffering overhead | Complex operational requirements |
| Predictable latency (no page cache) | No portability across storage types |

### Apache Arrow

Apache Arrow provides columnar in-memory format with zero-copy IPC for sharing data between processes without serialization.

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "Arrow",
            "Arrow": {
                "DataDirectory": "/var/surgewave/arrow",
                "BatchSize": 8192,
                "EnableIpc": true,
                "IpcSocketPath": "/tmp/surgewave-arrow.sock"
            }
        }
    }
}
```

| Strength | Weakness |
|----------|----------|
| Zero-copy IPC between processes | Memory-bound (no disk persistence alone) |
| Columnar in-memory format | Requires Arrow-compatible consumers |
| Native integration with pandas/Spark | Higher memory usage |
| Vectorized processing | Not suited for long-term storage |

### Engine Selection Guide

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

---

## Exercises

### Exercise 1: Set Up S3-Direct Storage with MinIO

1. Start MinIO using Docker:
   ```bash
   docker run -d --name minio -p 9000:9000 -p 9001:9001 \
     -e MINIO_ROOT_USER=minioadmin -e MINIO_ROOT_PASSWORD=minioadmin \
     minio/minio server /data --console-address ":9001"
   ```
2. Create a bucket named `surgewave-dev` via the MinIO console at `http://localhost:9001`
3. Configure Surgewave broker with `S3Direct` engine pointing to MinIO
4. Start the broker, create a topic, produce 1,000 messages
5. Verify segments appear in MinIO by browsing the bucket contents
6. Consume the messages and verify data integrity

### Exercise 2: Configure Tiered Storage

1. Configure a hybrid storage setup:
   - Hot tier: Local disk with `MaxHotAge: 00:05:00` (5 minutes for testing)
   - Cold tier: MinIO S3
2. Produce messages continuously for 10 minutes
3. Observe segments moving from local disk to MinIO as they age past 5 minutes
4. Consume both hot (recent) and cold (older) messages and compare latency
5. Check the Surgewave Control UI metrics for hot vs cold read times

### Exercise 3: Compare Storage Engine Performance

1. Run the Surgewave benchmark tool against three storage engines:
   - Local (default)
   - RocksDB
   - S3Direct (via MinIO)
2. For each engine, measure:
   - Write throughput (messages/second)
   - Write latency (P50, P99)
   - Read throughput (messages/second)
   - Read latency (P50, P99)
3. Create a comparison table of your results
4. Analyze which engine is best for your target workload

### Exercise 4: Run OLAP Queries with DuckDB Engine

1. Configure Surgewave with the DuckDB storage engine
2. Create an `orders` topic and produce 10,000 JSON order messages with varied fields (customerId, productId, amount, region, timestamp)
3. Query the data using SQL:
   - Total revenue by region
   - Top 10 customers by order count
   - Average order amount per hour
   - Products with the highest total revenue
4. Compare query performance against consuming all messages and processing in application code

## Best Practices

| Practice | Description |
|----------|-------------|
| Start with local, tier later | Use local disk initially, add tiering when costs justify it |
| Use managed identity in cloud | Avoid access keys; use IAM roles, managed identity, or workload identity |
| Set lifecycle policies | Automatically archive or delete old segments to control costs |
| Monitor egress costs | Cross-region reads from object stores incur egress charges |
| Test failover | Regularly test disaster recovery by failing over to secondary regions |
| Match engine to workload | Use OLAP engines (DuckDB/Parquet) for analytics, not real-time messaging |
| Benchmark before deciding | Always benchmark with your actual workload before committing to an engine |

---

**Ready?** [Start Lesson 27.1: Zero-Disk Architecture](lesson-1/README.md)
