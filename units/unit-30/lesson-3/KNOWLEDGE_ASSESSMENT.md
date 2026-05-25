# Knowledge Assessment: Lesson 17.3 - Chunking

## Multiple Choice

### 1. What information must be included in a chunk header for successful reassembly?
- [ ] A) Only the chunk data
- [ ] B) MessageId, ChunkIndex, TotalChunks, and data size information
- [ ] C) Only the total message size
- [ ] D) Just a sequence number

<details>
<summary>Answer</summary>
**B)** A chunk header must include: MessageId (to link chunks together), ChunkIndex (order of this chunk), TotalChunks (to know when complete), TotalSize (original message size), and optionally a Checksum for integrity verification.
</details>

### 2. Why is it recommended to use the MessageId as the message key when sending chunks?
- [ ] A) To make debugging easier
- [ ] B) To ensure all chunks go to the same partition for ordered delivery
- [ ] C) To reduce storage space
- [ ] D) To enable compression

<details>
<summary>Answer</summary>
**B)** Using MessageId as the key ensures all chunks for the same message are routed to the same partition. This guarantees ordered delivery (chunk0 -> chunk1 -> chunk2), simplifying reassembly since chunks arrive in sequence.
</details>

### 3. In the reassembly state machine, what should happen when a chunk arrives for a new MessageId?
- [ ] A) Reject it as invalid
- [ ] B) Create new message state and start collecting chunks
- [ ] C) Wait for the first chunk before accepting
- [ ] D) Send an acknowledgment immediately

<details>
<summary>Answer</summary>
**B)** When a chunk arrives for a new MessageId, the reassembler should create new message state to track that message and begin collecting chunks. Chunks may arrive out of order, so the reassembler buffers until all chunks are received.
</details>

## True/False

### 4. If chunks arrive out of order (chunk2 -> chunk0 -> chunk1), the reassembler cannot reconstruct the original message.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The reassembler can handle out-of-order chunks by buffering them and sorting by ChunkIndex before concatenation. Each chunk contains its sequence number, enabling correct reassembly regardless of arrival order.
</details>

### 5. A 5 MB message split into 1 MB chunks will produce exactly 5 chunks.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - A 5 MB message divided by 1 MB chunk size equals exactly 5 chunks (5,242,880 / 1,048,576 = 5). However, if the division isn't exact, you would need one additional smaller chunk for the remainder.
</details>

## Code Challenge

### 6. What's wrong with this chunk reassembly code?
```csharp
var reassembled = new MemoryStream();
foreach (var chunk in receivedChunks)
{
    reassembled.Write(chunk.Data, 0, chunk.Data.Length);
}
return reassembled.ToArray();
```

<details>
<summary>Answer</summary>
The chunks are not sorted by index before concatenation. If chunks arrived out of order, the data would be corrupted. Fixed version:
```csharp
var reassembled = new MemoryStream();
var sortedChunks = receivedChunks.OrderBy(c => c.Header.ChunkIndex);
foreach (var chunk in sortedChunks)
{
    reassembled.Write(chunk.Data, 0, chunk.Data.Length);
}
return reassembled.ToArray();
```
Additionally, you should verify that you have all chunks (count == TotalChunks) and optionally validate checksums.
</details>

### 7. What happens if chunk 3 of 5 never arrives?
- [ ] A) The message is delivered with missing data
- [ ] B) The reassembler waits indefinitely or times out, message cannot be completed
- [ ] C) The other chunks fill in the gap automatically
- [ ] D) Surgewave automatically retransmits the missing chunk

<details>
<summary>Answer</summary>
**B)** The reassembler must wait for all chunks before delivering the message. If chunk 3 never arrives, the message cannot be completed. The reassembler should implement a timeout mechanism to eventually discard incomplete messages and handle this as a failure case.
</details>

## Score
- 7/7: Excellent! You understand message chunking thoroughly
- 5-6/7: Good grasp of chunking and reassembly
- 3-4/7: Review ordering considerations and reassembly
- <3/7: Re-do the lesson focusing on chunk headers and reassembly
