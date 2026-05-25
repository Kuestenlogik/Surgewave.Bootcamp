# Knowledge Assessment: Lesson 20.3 - WebSocket Streaming

## Multiple Choice

### 1. What is the primary advantage of WebSocket over REST for consuming messages?
- [ ] A) Better compression
- [ ] B) Real-time push instead of polling
- [ ] C) Stronger authentication
- [ ] D) Smaller message sizes

<details>
<summary>Answer</summary>
**B)** WebSocket enables real-time push from server to client, eliminating the need for repeated polling requests. The server pushes messages immediately as they arrive.
</details>

### 2. What message type should a client send to start receiving messages from a topic?
- [ ] A) connect
- [ ] B) consume
- [ ] C) subscribe
- [ ] D) listen

<details>
<summary>Answer</summary>
**C)** The client sends a message with type "subscribe" containing the topic, partition, and optional offset to start receiving messages.
</details>

### 3. What is the purpose of the correlation_id field in WebSocket messages?
- [ ] A) To track message offsets
- [ ] B) To match responses to requests
- [ ] C) To identify the consumer group
- [ ] D) To prevent duplicate messages

<details>
<summary>Answer</summary>
**B)** The correlation_id allows matching responses to their corresponding requests, similar to the Correlation ID in the Kafka wire protocol. It's optional but useful for request-response patterns.
</details>

### 4. What happens when a slow consumer cannot keep up with message delivery?
- [ ] A) The connection is automatically closed
- [ ] B) Messages are queued indefinitely
- [ ] C) Messages may be dropped from the buffer
- [ ] D) The producer is throttled

<details>
<summary>Answer</summary>
**C)** When a consumer is slow and the MessageBufferSize is exceeded, messages may be dropped. The configuration allows tuning buffer sizes to balance memory usage vs message retention.
</details>

### 5. What message type does the server send when a new message arrives on a subscribed topic?
- [ ] A) record
- [ ] B) data
- [ ] C) message
- [ ] D) event

<details>
<summary>Answer</summary>
**C)** The server sends messages with type "message" containing the topic, partition, offset, key, value, and timestamp of the received message.
</details>

## True/False

### 6. WebSocket connections can have multiple active subscriptions to different topics simultaneously.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - A single WebSocket session can have multiple subscriptions, up to the configured MaxSubscriptionsPerSession limit (default 10). Each subscription receives its own stream of messages.
</details>

### 7. The heartbeat mechanism in WebSocket is only used for keeping the connection alive, not for detecting disconnections.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The heartbeat mechanism serves both purposes: keeping the connection alive AND detecting disconnections. Missing heartbeats trigger session cleanup, indicating a lost connection.
</details>

## Code Challenge

### 8. What's wrong with this WebSocket subscription request?

```json
{
  "type": "subscribe",
  "payload": {
    "topic": "orders"
  },
  "correlation_id": "sub-1"
}
```

<details>
<summary>Answer</summary>
While the message may work with defaults, it's missing important fields for precise subscription control:

```json
{
  "type": "subscribe",
  "payload": {
    "topic": "orders",
    "partition": 0,    // Should specify which partition to consume
    "offset": -1       // Should specify starting offset (-1 for latest, 0 for earliest, or specific offset)
  },
  "correlation_id": "sub-1"
}
```

Without specifying partition, you might receive messages from all partitions or the default partition. Without specifying offset, you might miss messages or start from an unexpected position. Always be explicit about these values for predictable behavior.
</details>

## Score
- 8/8: Excellent!
- 6-7/8: Good understanding
- 4-5/8: Review the lesson
- <4/8: Re-do the lesson
