# Lesson 27.5: Hybrid Storage Strategies

Most production deployments benefit from a hybrid approach: fast local storage for recent (hot) data and cheap object storage for older (cold) data.

## Learning Objectives

- Design tiered storage architectures with hot and cold tiers
- Configure tiering policies (age-based, size-based, combined)
- Model storage costs for different strategies
- Perform online migration between storage engines

## Tiered Storage Configuration

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

## Tiering Policy Types

| Policy | Trigger | Best For |
|--------|---------|----------|
| AgeBased | Segment age exceeds threshold | Predictable workloads |
| SizeBased | Local storage exceeds percentage | Limited local storage |
| Combined | Age OR size threshold hit | Production clusters |

## Cost Comparison (1 TB retained, 30 days)

| Strategy | Monthly Cost |
|----------|-------------|
| All-Local (NVMe SSD) | ~$200 |
| All-Object-Store (S3) | ~$24 |
| Hybrid (100 GB hot + 900 GB cold) | ~$42 (79% savings vs all-local) |

## Online Migration

Surgewave supports online migration between storage engines without downtime using `surgewave-admin storage migrate`. The migration runs in the background while the broker continues serving traffic.

## Performance Benchmarks

| Metric | Local NVMe | S3-Direct | Hybrid (Hot) | Hybrid (Cold) |
|--------|-----------|-----------|--------------|---------------|
| Write Latency P50 | 0.3ms | 12ms | 0.3ms | 12ms |
| Read Latency P50 | 0.2ms | 18ms | 0.2ms | 18ms |
| Write Throughput | 1.2 GB/s | 200 MB/s | 1.2 GB/s | 200 MB/s |

---

[Back to Unit 30](../README.md)
