# Knowledge Assessment: Lesson 17.1 - Message Size Limits

## Multiple Choice

### 1. Why do message size limits exist in Surgewave?
- [ ] A) To reduce licensing costs
- [ ] B) To prevent memory pressure, network saturation, and increased latency
- [ ] C) To force users to purchase premium features
- [ ] D) To simplify the codebase

<details>
<summary>Answer</summary>
**B)** Message size limits prevent memory pressure on brokers/consumers, network saturation that affects other messages, replication lag, and increased end-to-end latency for large messages.
</details>

### 2. What is the default maximum message size in Surgewave?
- [ ] A) 100 KB
- [ ] B) 1 MB
- [ ] C) 10 MB
- [ ] D) Unlimited

<details>
<summary>Answer</summary>
**B)** The default maximum message size is 1 MB (1,048,576 bytes) at both the producer and broker level.
</details>

### 3. At which point in the message flow is the producer-side size check performed?
- [ ] A) After network send
- [ ] B) During broker replication
- [ ] C) Before serialization and network send
- [ ] D) At consumer fetch time

<details>
<summary>Answer</summary>
**C)** The producer-side size check (MaxMessageSize) is performed before the message is sent to the network, preventing oversized messages from consuming bandwidth only to be rejected.
</details>

## True/False

### 4. If you increase the producer's MaxMessageSize but not the broker's message.max.bytes, large messages will still be rejected.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Both the producer AND broker size limits must accommodate the message. Even if the producer allows a larger message, the broker will reject it if it exceeds message.max.bytes.
</details>

### 5. The total message size only includes the value/payload data.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Total message size includes: Record Header (~12 bytes), Key Length (4 bytes), Key Data (variable), Value Length (4 bytes), Value Data (variable), Headers (variable), and CRC/Checksum (4 bytes). A 1 MB payload with key and headers may exceed the 1 MB limit.
</details>

## Code Challenge

### 6. What's wrong with this size validation?
```csharp
var message = "Hello World";
if (message.Length > maxSize)
{
    throw new MessageSizeException("Too large");
}
```

<details>
<summary>Answer</summary>
The code checks string length (character count), not byte size. UTF-8 encoding can use multiple bytes per character. The correct approach:
```csharp
var message = "Hello World";
var serializedSize = Encoding.UTF8.GetByteCount(message);
if (serializedSize > maxSize)
{
    throw new MessageSizeException($"Message size {serializedSize} exceeds limit {maxSize}");
}
```
</details>

### 7. A message has a key of 8 bytes, a value of exactly 1,048,576 bytes (1 MB), and 100 bytes of headers. Given a 1 MB limit, what will happen?
- [ ] A) Message sent successfully - it's exactly at the limit
- [ ] B) Message rejected - total size exceeds 1 MB when including overhead
- [ ] C) Message truncated to fit
- [ ] D) Headers are automatically removed to fit

<details>
<summary>Answer</summary>
**B)** The message will be rejected. Total size = 8 (key) + 1,048,576 (value) + 100 (headers) + ~20 (record overhead) = approximately 1,048,704 bytes, which exceeds the 1 MB limit.
</details>

## Score
- 7/7: Excellent! You understand message size limits thoroughly
- 5-6/7: Good understanding of size validation
- 3-4/7: Review size components and validation flow
- <3/7: Re-do the lesson focusing on why limits exist
