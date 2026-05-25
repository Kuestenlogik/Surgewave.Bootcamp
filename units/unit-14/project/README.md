# Unit 19 Project: Mini Wire Protocol Client

Build a minimal TCP-based Surgewave client that communicates directly using the wire protocol.

## Scenario

You're creating a lightweight embedded client for IoT devices that need to send telemetry data to Surgewave. The client must be minimal, using raw TCP sockets without the full client library.

Your mini client will:
1. Connect to a Surgewave broker via TCP socket
2. Send a Metadata request to discover topics
3. Produce messages using the wire protocol
4. Receive and parse the broker's responses

## Requirements

### Functional Requirements

- [ ] Establish TCP connection to Surgewave broker
- [ ] Construct and send a Metadata request (API Key 3)
- [ ] Parse the Metadata response to get topic information
- [ ] Construct and send a Produce request (API Key 0)
- [ ] Handle the Produce response with offset confirmation

### Technical Requirements

- [ ] Use `System.Net.Sockets.TcpClient` for TCP communication
- [ ] Implement proper frame encoding (4-byte length prefix)
- [ ] Handle big-endian byte ordering for protocol fields
- [ ] Use `BinaryPrimitives` for efficient byte manipulation
- [ ] Implement request correlation ID tracking

## Wire Protocol Basics

**Request Frame:** `Length (4B)` + `API Key (2B)` + `API Version (2B)` + `Correlation ID (4B)` + `Client ID (string)` + `Request Body (varies)`

**Response Frame:** `Length (4B)` + `Correlation ID (4B)` + `Response Body (varies)`

## Expected Output

```
=== Mini Wire Protocol Client ===

[TCP] Connecting to localhost:9092...
[TCP] Connected to Surgewave broker

[Protocol] Sending Metadata request (API Key 3)
[Protocol] Request: CorrelationId=1, ClientId=mini-client
[Protocol] Frame size: 32 bytes

[Protocol] Received Metadata response
[Protocol] Response: CorrelationId=1
[Protocol] Topics discovered: 2
  - telemetry (3 partitions)
  - events (1 partition)

[Protocol] Sending Produce request (API Key 0)
[Protocol] Topic: telemetry, Partition: 0
[Protocol] Message: {"sensor":"temp-01","value":23.5}

[Protocol] Received Produce response
[Protocol] Offset assigned: 42
[Protocol] Timestamp: 2024-01-25T10:30:00Z

=== Mini Client Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-19/project/start
dotnet run
```

## Hints

1. Use `BinaryPrimitives.WriteInt32BigEndian()` for length prefix
2. API Keys: Metadata=3, Produce=0
3. Strings in Kafka protocol are length-prefixed (2 bytes for nullable, 1 byte for compact)
4. Always read the full response frame before parsing

## Protocol Reference

### Metadata Request (API Key 3, v0)

```
MetadataRequest => [topics]
  topics => NULLABLE_ARRAY(STRING)
    null = request all topics
```

### Produce Request (API Key 0, v0)

```
ProduceRequest => acks timeout [topic_data]
  acks => INT16
  timeout => INT32
  topic_data => topic [partition_data]
    topic => STRING
    partition_data => partition record_set
      partition => INT32
      record_set => RECORDS
```

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| TCP connection established | 15 |
| Metadata request correctly formatted | 20 |
| Metadata response parsed | 20 |
| Produce request correctly formatted | 25 |
| Produce response handled | 20 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
