# Lesson 26.4: Memory Management & GC Tuning

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 26.3

## Overview

Master .NET memory management techniques to minimize garbage collection pressure in Surgewave applications. Learn to use ArrayPool<T>, Memory<T>, Span<T>, and zero-copy patterns to achieve GC-free hot paths for maximum throughput and consistent low latency.

## Learning Objectives

- Use ArrayPool<T> to eliminate repeated buffer allocations
- Leverage Memory<T> and Span<T> for zero-copy data processing
- Configure GC latency modes for real-time workloads
- Understand ServerGC vs WorkstationGC trade-offs
- Profile and reduce GC pressure in message processing pipelines

## Memory Management Concepts

| Approach | Steps | Result |
|----------|-------|--------|
| **Traditional (allocation-heavy)** | 1. Receive → allocate `byte[]`; 2. Deserialize → allocate `string/object`; 3. Process → allocate intermediates; 4. Serialize → allocate `byte[]`; 5. Send → GC collects all | Millions of short-lived allocations → frequent GC |
| **Pool, Reuse, Zero-Copy** | 1. Receive → rent from `ArrayPool<byte>`; 2. Deserialize → use `Span<T>` over rented buffer; 3. Process → operate on `Span<T>` slices; 4. Serialize → write to rented buffer; 5. Send → return buffer to pool | Zero allocations on hot path → no GC pauses |

## Key Types

| Type | Characteristics |
|------|----------------|
| **`ArrayPool<T>`** | Shared pool of reusable arrays; `Rent()` / `Return()` instead of `new` / GC; reduces Gen0 allocations dramatically; thread-safe, lock-free for common sizes |
| **`Span<T>`** | Stack-only view over contiguous memory; zero-copy slicing with `Slice()`; cannot be stored on heap (`ref struct`); best for synchronous, short-lived operations |
| **`Memory<T>`** | Heap-safe wrapper for memory regions; can be stored in fields and passed to async methods; `.Span` property provides `Span<T>` access; works with `ArrayPool` via `MemoryPool<T>` |
| **`MemoryPool<T>`** | Higher-level pool returning `IMemoryOwner<T>`; disposable pattern for automatic return; built on top of `ArrayPool<T>` |

## GC Modes

**GC Modes:**

| Mode | Characteristics | Best For |
|------|----------------|---------|
| **ServerGC** (recommended for Surgewave) | One GC heap per logical processor; higher throughput, more memory; background GC for Gen2 | Multi-core servers, high-throughput workloads |
| **WorkstationGC** | Single GC heap shared across processors; lower memory, more frequent pauses | Client apps, low-memory environments |

**GC Latency Modes:**

| Mode | Description |
|------|-------------|
| `Interactive` | Default, balanced |
| `LowLatency` | Minimize GC pauses (avoid Gen2) |
| `SustainedLowLatency` | Aggressive latency reduction |
| `Batch` | Maximum throughput, no concurrency |
| `NoGCRegion` | Temporarily disable GC entirely |

## Key Configuration Settings

| Setting | Description | Default | Recommended |
|---------|-------------|---------|-------------|
| `ServerGarbageCollection` | Use server GC | false | true |
| `ConcurrentGarbageCollection` | Background GC | true | true |
| `GCLatencyLevel` | GC pause priority | Interactive | SustainedLowLatency |
| `GCHeapCount` | Number of GC heaps | CPU count | Match CPU count |
| `RetainVM` | Keep virtual memory | false | true for stability |

## Exercise

In this exercise, you will:

1. Compare allocation-heavy vs pooled buffer approaches
2. Use Span<T> for zero-copy message parsing
3. Implement a buffer pool for message processing
4. Configure and compare GC modes
5. Measure GC impact on latency percentiles

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
