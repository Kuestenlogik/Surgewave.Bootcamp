# Lesson 27.1: Zero-Disk Architecture

In a zero-disk architecture, the Surgewave broker does not use local disk for message storage. Instead, all segments are written directly to a cloud object store. The broker becomes effectively stateless, making it easy to scale horizontally, replace instances, and run in serverless environments.

## Learning Objectives

- Understand trade-offs between local disk and object store storage
- Identify when zero-disk architecture is appropriate
- Compare latency, throughput, durability, and cost characteristics
- Enable zero-disk mode in Surgewave configuration

## When to Use Zero-Disk

| Scenario | Zero-Disk Fit |
|----------|---------------|
| Cloud-native / Kubernetes | Excellent -- no persistent volumes needed |
| Serverless / ephemeral compute | Excellent -- instances can be replaced freely |
| Cost optimization at scale | Good -- object storage is 10-50x cheaper per GB than SSD |
| Ultra-low latency required | Poor -- object store adds 5-50ms per operation |
| Edge / IoT with limited connectivity | Poor -- requires reliable network to object store |
| Compliance requiring data durability | Excellent -- 11 nines durability built in |

## Trade-offs

| Dimension | Local Disk | Zero-Disk |
|-----------|-----------|-----------|
| Write Latency | < 1ms (NVMe) | 5-20ms (object store PUT) |
| Read Latency | < 1ms (cached) | 10-50ms (object store GET) |
| Throughput | Disk IOPS limited | Network bandwidth limited |
| Durability | Depends on replication | 99.999999999% (11 nines) |
| Cost per GB/month | $0.10-0.50 (SSD) | $0.01-0.03 (object store) |
| Scaling | Add disks or nodes | Unlimited, auto-scales |
| Recovery | Rebuild from replicas | Instant (no local state) |

## Enabling Zero-Disk Mode

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

[Back to Unit 30](../README.md)
