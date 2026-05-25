# Lesson 19.4: Protocol Debugging

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 19.3

## Overview

Learn how to debug Kafka protocol issues using hex dumps, Wireshark analysis, and troubleshooting techniques. This lesson covers the tools and methods for diagnosing protocol-level problems.

## Learning Objectives

- Analyze protocol traffic with hex dumps
- Use Wireshark to capture and decode Kafka traffic
- Identify common protocol errors
- Build debugging utilities for protocol analysis

## Why Learn Protocol Debugging?

Protocol debugging skills enable:

| Benefit | Description |
|---------|-------------|
| Root cause analysis | Find exact byte causing issues |
| Client compatibility | Debug interoperability problems |
| Performance tuning | Identify protocol overhead |
| Security auditing | Verify encrypted traffic |
| Development support | Test custom clients |

## Key Concepts

### Hex Dump Analysis

Example hex dump of a Metadata request:

```
00000000 00 00 00 2e 00 03 00 00 00 00 00 01 00 0c 6d 69  |.......mi|
00000010 6e 69 6d 61 6c 2d 63 6c 69 65 6e 74 ff ff ff ff  |nimal-client....|
```

| Bytes | Value | Meaning |
|-------|-------|---------|
| `00 00 00 2e` | 46 | Length (big-endian) |
| `00 03` | 3 | API Key (Metadata) |
| `00 00` | 0 | API Version 0 |
| `00 00 00 01` | 1 | Correlation ID |
| `00 0c` | 12 | Client ID length |
| `6d 69...` | "minimal-client" | Client ID string |
| `ff ff ff ff` | -1 | Topics array (all topics) |

### Wireshark Kafka Dissector

Use filter `kafka || tcp.port == 9092`. Sample packet list:

| No. | Source | Dest | Port | Info |
|-----|--------|------|------|------|
| 1 | Client | Broker | 9092 | Metadata Request (API 3) |
| 2 | Broker | Client | 9092 | Metadata Response |
| 3 | Client | Broker | 9092 | Produce Request (API 0) |
| 4 | Broker | Client | 9092 | Produce Response |

### Common Protocol Errors

| Error | Symptom | Fix |
|-------|---------|-----|
| Wrong byte order | `2e 00 00 00` instead of `00 00 00 2e` | Use big-endian encoding |
| Missing length prefix | Data starts immediately — broker can't parse | Prepend 4-byte big-endian length |
| Wrong string encoding | Null-terminated instead of length-prefixed | Use `int16 length + UTF-8 bytes` |
| Truncated message | Header says 100 bytes, only 80 sent | Broker waits or times out |
| Invalid API version | Version 99 not supported by broker | Use `UNSUPPORTED_VERSION` error response |

### Error Response Format

Error response bytes: `00 00 00 12` (length 18), `00 00 00 01` (Correlation ID), `00 03` (error code), `00 00 00 00` (empty array).

| Code | Error |
|------|-------|
| 0 | No error (success) |
| 1 | OFFSET_OUT_OF_RANGE |
| 3 | UNKNOWN_TOPIC_OR_PARTITION |
| 5 | LEADER_NOT_AVAILABLE |
| 6 | NOT_LEADER_OR_FOLLOWER |
| 7 | REQUEST_TIMED_OUT |
| 35 | UNSUPPORTED_VERSION |
| 36 | TOPIC_ALREADY_EXISTS |
| 37 | INVALID_PARTITIONS |

### Protocol Trace Structure

```
[2024-01-15 10:30:45.123] -> REQUEST  API: Produce (0) v0  CorrelationId: 42  Size: 156 bytes
[2024-01-15 10:30:45.156] <- RESPONSE  CorrelationId: 42  Size: 34 bytes  Latency: 33ms
```

## Debugging Tools

| Tool | Purpose |
|------|---------|
| Wireshark | Network packet capture and analysis |
| tcpdump | Command-line packet capture |
| xxd | Hex dump utility |
| netcat | Raw TCP connection testing |
| Custom logger | In-application protocol tracing |

## Exercise

In this exercise, you'll:

1. Build a protocol tracer that logs all requests/responses
2. Create a hex dump utility for message analysis
3. Implement error code lookup and description
4. Analyze and fix a broken protocol message

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
