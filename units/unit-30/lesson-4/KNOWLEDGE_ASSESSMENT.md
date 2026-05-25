# Knowledge Assessment: Lesson 17.4 - Claim-Check Pattern

## Multiple Choice

### 1. What is the claim-check pattern analogous to?
- [ ] A) A library card system
- [ ] B) A coat check at a restaurant - leave the heavy item, carry a light ticket
- [ ] C) A file compression algorithm
- [ ] D) A database index

<details>
<summary>Answer</summary>
**B)** The claim-check pattern is like a coat check: you give your heavy coat (large payload) to storage, receive a light ticket (claim check reference), carry the ticket through the messaging system, and later present the ticket to retrieve your coat.
</details>

### 2. What is the typical size of a claim-check reference message?
- [ ] A) Same as the original payload
- [ ] B) Half the original payload
- [ ] C) A few hundred bytes (containing metadata and location)
- [ ] D) At least 1 MB

<details>
<summary>Answer</summary>
**C)** A claim-check reference is typically only a few hundred bytes (~200-500 bytes) containing metadata like storage type, location URL, size, content type, and expiration time - regardless of the original payload size.
</details>

### 3. When should you AVOID using the claim-check pattern?
- [ ] A) When messages are very large (> 10 MB)
- [ ] B) When you need Surgewave's retention guarantees and atomic delivery
- [ ] C) When external storage is available
- [ ] D) When processing files like images and videos

<details>
<summary>Answer</summary>
**B)** Avoid claim-check when you need Surgewave's retention guarantees, atomic message delivery, or when external storage is unreliable. Also avoid for small messages (< 1 MB) where the overhead isn't justified.
</details>

## True/False

### 4. The claim-check pattern reduces Surgewave's message throughput because payloads are still stored in the topic.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The claim-check pattern improves Surgewave throughput because only small reference messages (~200 bytes) flow through the topic instead of large payloads (50+ MB). The actual payload is stored in external storage like S3 or Azure Blob.
</details>

### 5. After consuming a claim-check reference, the consumer must fetch the actual payload from external storage before processing.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The consumer receives only a reference (URL/location). It must make a separate call to external storage to fetch the actual payload data before processing.
</details>

## Code Challenge

### 6. What's wrong with this claim-check reference?
```csharp
var reference = new ClaimCheckReference
{
    StorageType = "s3",
    Location = "s3://my-bucket/data.bin",
    Payload = largePayloadBytes  // The actual data
};
```

<details>
<summary>Answer</summary>
The reference should NOT contain the payload data. This defeats the purpose of claim-check by sending the large data through the messaging system. The correct approach:
```csharp
// First, store the payload externally
var location = await externalStorage.StoreAsync(largePayloadBytes);

// Then create a lightweight reference
var reference = new ClaimCheckReference
{
    Id = Guid.NewGuid(),
    StorageType = "s3",
    Location = location,  // Just the URL/path
    Size = largePayloadBytes.Length,
    ContentType = "application/octet-stream",
    Checksum = ComputeChecksum(largePayloadBytes),
    ExpiresAt = DateTime.UtcNow.AddDays(1)
};
```
</details>

### 7. In the claim-check flow, when should external storage cleanup occur?
- [ ] A) Immediately after storing the payload
- [ ] B) Before the consumer fetches the payload
- [ ] C) After the consumer successfully processes the payload
- [ ] D) Never - keep all data forever

<details>
<summary>Answer</summary>
**C)** Cleanup should occur after the consumer successfully retrieves and processes the payload. This ensures the data is available when needed. Alternatively, use time-based expiration (ExpiresAt field) for automatic cleanup of unprocessed items.
</details>

## Score
- 7/7: Excellent! You understand the claim-check pattern
- 5-6/7: Good grasp of external storage integration
- 3-4/7: Review when to use claim-check and reference structure
- <3/7: Re-do the lesson focusing on the pattern flow
