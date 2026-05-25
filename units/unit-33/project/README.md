# Unit 26 Project: Performance Optimization Suite

## Overview

Build a comprehensive performance test suite that compares different optimization strategies for Surgewave producer and consumer operations. The suite should measure throughput, latency percentiles, GC impact, and memory allocation across multiple configurations, producing a clear comparison report.

## Objectives

- Apply producer tuning (batch size, linger, async patterns)
- Apply consumer tuning (parallelism, channels, batch processing)
- Apply memory optimization (ArrayPool, Span<T>, zero-copy)
- Benchmark all strategies with statistical rigor
- Generate a comprehensive performance comparison report

## Requirements

### 1. Benchmark Framework
- Warm-up phase before each benchmark
- Multiple runs for statistical significance
- Latency percentile calculation (P50, P90, P99, P99.9)
- GC collection tracking per benchmark

### 2. Producer Benchmarks
- Sequential vs async batched production
- Different batch sizes (1KB, 16KB, 64KB)
- Different linger times (0ms, 5ms, 20ms)
- With and without buffer pooling

### 3. Consumer Benchmarks
- Sequential vs parallel processing
- Channel-based processing with varying worker counts
- Batch processing with varying batch sizes
- With simulated processing delay

### 4. Memory Benchmarks
- Allocation-heavy vs pooled buffer approaches
- String.Split vs Span<T> parsing
- GC latency mode comparison

### 5. Report
- Summary table comparing all strategies
- Best configuration recommendation
- Latency distribution histogram for the best strategy

## Difficulty

Advanced | Estimated Duration: 90 min

## Getting Started

1. Open `start/Program.cs`
2. Implement the benchmark framework
3. Add producer, consumer, and memory benchmarks
4. Generate the comparison report
5. Compare with `completed/Program.cs`

---

**Next Unit:** [Unit 32: Hybrid Cloud](../../unit-32/README.md)
