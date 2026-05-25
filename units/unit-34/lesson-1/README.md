# Lesson 34.1: Performance Architecture

### Key Metrics

| Metric | What It Measures | Unit | Why It Matters |
|--------|-----------------|------|----------------|
| Throughput | Sustainable message rate | msg/s, MB/s | System capacity |
| P50 Latency | Median delivery time | ms | Typical user experience |
| P90 Latency | 90th percentile | ms | Degraded experience threshold |
| P99 Latency | Tail latency | ms | SLA compliance |
| P99.9 Latency | Extreme tail | ms | Worst-case scenarios |
| Allocations | Memory allocated per operation | B/op | GC pressure |
| GC Pauses | Stop-the-world duration | ms | Latency spikes |

### Architecture for Speed

Surgewave achieves its performance targets through several architectural decisions:

1. **Zero-copy message handling** -- Messages are passed as `ReadOnlyMemory<byte>` from socket to storage without intermediate copies
2. **Memory pooling** -- Buffers are rented from `ArrayPool` and returned after use
3. **SIMD acceleration** -- CRC32C, vector similarity, and batch operations use hardware intrinsics
4. **Lock-free data structures** -- `ConcurrentDictionary`, `Channel<T>`, and atomic operations
5. **ValueTask for sync completion** -- Avoids async state machine allocation when results are immediately available
6. **Object pooling** -- Reusable request/response objects avoid per-message allocation

### Kafka vs Surgewave Performance

| Metric | Kafka (Baseline) | Surgewave (Optimized) | Factor |
|--------|------------------|-------------------|--------|
| Throughput | 100K msg/s | 1M+ msg/s | 10x |
| P50 Latency | 2-5ms | <1ms | 2-5x |
| P99 Latency | 10-50ms | <5ms | 2-10x |
| Memory/msg | 500-1000 B | 0 (pooled) | Eliminated |
| GC Pauses | 10-50ms | <1ms | 10-50x |

---

[Back to Unit 33](../README.md)
