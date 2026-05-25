# Knowledge Assessment: Lesson 20.2 - REST Produce/Consume

## Multiple Choice

### 1. How are message keys and values encoded in REST API produce requests?
- [ ] A) UTF-8 strings
- [ ] B) Base64 encoded
- [ ] C) Hexadecimal strings
- [ ] D) Raw binary in multipart form

<details>
<summary>Answer</summary>
**B)** Message keys and values are Base64 encoded in REST API requests and responses. This allows binary data to be safely transmitted in JSON format.
</details>

### 2. What HTTP method is used for fetching messages via the REST API?
- [ ] A) GET /v3/topics/{topic}/records
- [ ] B) POST /v3/consumers/fetch
- [ ] C) GET /v3/consumers/{group}/records
- [ ] D) POST /v3/topics/{topic}/consume

<details>
<summary>Answer</summary>
**B)** POST /v3/consumers/fetch is used for fetching messages. A POST is used because the request body contains the partition offsets and fetch parameters.
</details>

### 3. What does the "high_watermark" field in a fetch response represent?
- [ ] A) The highest offset ever written to the partition
- [ ] B) The last committed consumer offset
- [ ] C) The highest offset that has been replicated to all ISRs
- [ ] D) The memory usage threshold

<details>
<summary>Answer</summary>
**C)** The high_watermark represents the highest offset that has been replicated to all in-sync replicas. Consumers cannot read beyond this point under read_committed isolation.
</details>

### 4. Which error code indicates that a requested topic does not exist?
- [ ] A) UNKNOWN
- [ ] B) TOPIC_NOT_FOUND
- [ ] C) PARTITION_NOT_FOUND
- [ ] D) LEADER_NOT_AVAILABLE

<details>
<summary>Answer</summary>
**B)** TOPIC_NOT_FOUND indicates the requested topic does not exist. PARTITION_NOT_FOUND would indicate an invalid partition number for an existing topic.
</details>

### 5. What is the purpose of the "metadata" field when committing offsets?
- [ ] A) To specify the consumer group version
- [ ] B) To store arbitrary consumer-defined information
- [ ] C) To indicate the compression type used
- [ ] D) To authenticate the commit request

<details>
<summary>Answer</summary>
**B)** The metadata field allows storing arbitrary consumer-defined information with the committed offset, such as "processed" status or checkpoint information.
</details>

## True/False

### 6. The REST API supports real-time streaming of messages similar to WebSocket.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - REST API uses a request/response pattern and does not support real-time streaming. For real-time streaming, use WebSocket or gRPC streaming endpoints.
</details>

## Code Challenge

### 7. What's wrong with this fetch request?

```json
POST /v3/consumers/fetch
Content-Type: application/json

{
  "partitions": [
    {
      "topic": "orders",
      "partition": 0,
      "offset": -1,
      "max_bytes": 1048576
    }
  ],
  "max_wait_ms": 5000
}
```

<details>
<summary>Answer</summary>
The issue is the offset value of -1. In the REST API fetch request, offset should be a valid non-negative offset position, not a special value:

```json
POST /v3/consumers/fetch
Content-Type: application/json

{
  "partitions": [
    {
      "topic": "orders",
      "partition": 0,
      "offset": 0,  // Start from beginning, or use ListOffsets to find valid offset
      "max_bytes": 1048576
    }
  ],
  "max_wait_ms": 5000
}
```

To fetch from the latest or earliest offset, you should first call the ListOffsets API to get the appropriate offset value, then use that value in the fetch request. Unlike some native client APIs that accept special values like -1 or -2, the REST API expects explicit offset values.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 3-4/7: Review the lesson
- <3/7: Re-do the lesson
