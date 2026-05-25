# Lesson 26.5: Benchmarking & Profiling

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 26.4

## Overview

Learn to systematically benchmark and profile Surgewave applications using BenchmarkDotNet, PerfView, and dotTrace. Measure P50/P90/P99 latencies accurately, identify performance bottlenecks, and validate optimization results with statistical rigor.

## Learning Objectives

- Use BenchmarkDotNet for micro-benchmarks of Surgewave operations
- Understand statistical analysis of benchmark results
- Measure P50/P90/P99 latency percentiles accurately
- Identify CPU and memory bottlenecks using profiling tools
- Build a comprehensive performance test suite

## Benchmarking Fundamentals

| Practice | Guidelines |
|----------|-----------|
| **Warm-Up Phase** | JIT compilation on first run distorts results; run 100–1000 iterations before measuring; ensure steady-state before collecting data |
| **Statistical Rigor** | Run multiple iterations (not just one); calculate mean, median, standard deviation; report confidence intervals; watch for outliers (GC pauses, OS scheduling) |
| **Isolation** | Minimize background processes; pin to CPU cores when possible; use consistent hardware/VM specs; run Release mode, not Debug |
| **Meaningful Metrics** | Throughput: messages/second, MB/second; Latency: P50, P90, P95, P99, P99.9; Resource usage: CPU%, memory, GC collections; Scalability: how metrics change with load |

## Profiling Tools

| Tool | Characteristics |
|------|----------------|
| **BenchmarkDotNet** | Gold standard for .NET micro-benchmarks; automatic warm-up, iteration, and statistics; memory allocation diagnostics; comparison across configurations |
| **PerfView** | Free Microsoft tool for CPU and GC analysis; ETW-based, low overhead; stack traces, flame graphs; GC heap analysis |
| **dotTrace / dotMemory** (JetBrains) | Commercial CPU and memory profilers; timeline view for threading analysis; snapshot comparison for memory leaks; call tree and hot spot analysis |
| **dotnet-counters / dotnet-trace** | Built-in .NET diagnostic tools; real-time counter monitoring; trace collection for offline analysis; EventPipe-based, cross-platform |

## Latency Percentiles

Distribution of 1,000 requests:

| Percentile | Latency | Meaning |
|-----------|---------|---------|
| P50 (500th fastest) | 1.2 ms | Half of requests are faster |
| P90 (900th fastest) | 3.5 ms | 90% of requests are faster |
| P95 (950th fastest) | 5.8 ms | 95% of requests are faster |
| P99 (990th fastest) | 12.4 ms | 99% of requests are faster |
| P99.9 (999th fastest) | 45.2 ms | 99.9% of requests are faster |

**Why P99 matters:** at 1,000 req/s, P99 affects 10 users every second; at 100K req/s, 1,000 users per second; tail latency often indicates systemic issues (GC, IO); average hides these problems completely.

## Exercise

In this exercise, you will:

1. Build a benchmarking harness for Surgewave operations
2. Measure producer and consumer throughput with warm-up
3. Calculate accurate latency percentiles with HDR histogram
4. Profile GC behavior during workloads
5. Compare optimization strategies with statistical analysis

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
