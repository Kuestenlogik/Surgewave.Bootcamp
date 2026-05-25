# Lesson 19.2: Message Format

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 19.1

## Overview

Learn how messages are formatted in the Kafka protocol, including record batches, message headers, compression options, and CRC validation. Understanding the message format is essential for building high-performance producers and consumers.

## Learning Objectives

- Understand record batch structure and format
- Learn about message headers and their uses
- Master compression algorithms and when to use them
- Implement CRC validation for data integrity

## Why Learn Message Format?

Understanding message format enables:

| Benefit | Description |
|---------|-------------|
| Performance | Optimize batch sizes for throughput |
| Compression | Choose right algorithm for your data |
| Validation | Ensure data integrity with CRC |
| Debugging | Parse and inspect raw message data |
| Custom clients | Implement producers/consumers from scratch |

## Key Concepts

### Record Batch Structure (v2+)

| Field | Size | Description |
|-------|------|-------------|
| Base Offset | 8B | First record's offset in this batch |
| Batch Length | 4B | Total size in bytes (after this field) |
| Partition Leader Epoch | 4B | Leader epoch for fencing |
| Magic | 1B | Version = 2 for record batch format |
| CRC32C | 4B | CRC of attributes through end of batch |
| Attributes | 2B | Compression, timestamp type, transactional |
| Last Offset Delta | 4B | Delta from base offset |
| First Timestamp | 8B | Timestamp of first record |
| Max Timestamp | 8B | Max timestamp in batch |
| Producer ID | 8B | For exactly-once semantics |
| Producer Epoch | 2B | For exactly-once semantics |
| Base Sequence | 4B | First sequence number |
| Records Array | variable | Array of individual records |

### Individual Record Format

| Field | Size | Description |
|-------|------|-------------|
| Length | VarInt | Total record size |
| Attributes | 1B | Currently unused (reserved for future) |
| Timestamp Delta | VarInt | Difference from batch first timestamp |
| Offset Delta | VarInt | Difference from batch base offset |
| Key Length | VarInt | Key size in bytes (-1 = null) |
| Key | variable | Message key data |
| Value Length | VarInt | Value size in bytes (-1 = null) |
| Value | variable | Message value data |
| Headers Count | VarInt | Number of headers |
| Headers Array | variable | Key-value pairs for metadata |

### Message Headers

**Header Entry Format:**

| Field | Size | Description |
|-------|------|-------------|
| Key Length | VarInt | Header key size |
| Key | variable | Header key (UTF-8 string) |
| Value Length | VarInt | Header value size (-1 = null) |
| Value | variable | Header value (binary data) |

**Common Header Uses:**

| Header | Purpose |
|--------|---------|
| `trace-id` | Distributed tracing context |
| `correlation-id` | Request correlation |
| `content-type` | Message content type (e.g., application/json) |
| `source` | Origin system identifier |
| `schema-id` | Schema registry reference |

### Batch Attributes

**Bits 0-2: Compression**

| Value | Codec |
|-------|-------|
| 0 | None |
| 1 | GZIP |
| 2 | Snappy |
| 3 | LZ4 |
| 4 | ZSTD |

**Bit 3: Timestamp Type** — 0 = CreateTime, 1 = LogAppendTime

**Bit 4:** Is Transactional | **Bit 5:** Is Control Batch | **Bit 6:** Has Delete Horizon

### CRC Validation

**CRC32C covers:** Attributes, Last Offset Delta, First Timestamp, through end of Records Array.

**CRC32C does NOT cover:** Base Offset, Batch Length, Partition Leader Epoch, Magic.

CRC32C (Castagnoli) is used because it has hardware acceleration on modern CPUs (SSE4.2), better error detection than CRC32, and is commonly used in storage systems.

### Compression Comparison

| Algorithm | Ratio | CPU Speed | Best For |
|-----------|-------|-----------|----------|
| None | 1:1 | N/A | Low latency, already compressed |
| GZIP | High | Slow | Archival, bandwidth-limited |
| Snappy | Medium | Fast | General purpose, balanced |
| LZ4 | Medium | Very Fast | High throughput, low latency |
| ZSTD | High | Fast | Best overall, modern systems |

## Legacy Message Format (v0/v1)

| Field | Size | Description |
|-------|------|-------------|
| Offset | 8 bytes | Message offset |
| Message Size | 4 bytes | Size of message structure |
| CRC32 | 4 bytes | CRC of message content |
| Magic | 1 byte | Version (0 or 1) |
| Attributes | 1 byte | Compression type |
| Timestamp | 8 bytes | Only in v1 (Magic = 1) |
| Key Length | 4 bytes | -1 = null |
| Key | variable | Message key |
| Value Length | 4 bytes | -1 = null |
| Value | variable | Message value |

## Exercise

In this exercise, you'll:

1. Build a record batch with proper structure
2. Add message headers to records
3. Implement CRC32C calculation
4. Compress messages with LZ4/GZIP

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
