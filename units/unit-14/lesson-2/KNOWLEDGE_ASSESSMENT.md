# Knowledge Assessment: Lesson 19.2 - Message Format

## Multiple Choice

### 1. What is the magic byte value that indicates the Record Batch format (v2)?
- [ ] A) 0
- [ ] B) 1
- [ ] C) 2
- [ ] D) 3

<details>
<summary>Answer</summary>
**C)** Magic byte value 2 indicates the Record Batch format. Magic 0 and 1 are for legacy MessageSet formats.
</details>

### 2. Which compression algorithm provides the best balance of compression ratio and speed for most use cases?
- [ ] A) GZIP
- [ ] B) Snappy
- [ ] C) LZ4
- [ ] D) ZSTD

<details>
<summary>Answer</summary>
**D)** ZSTD provides the best overall balance with high compression ratio and fast speed. GZIP has the highest ratio but is slow, LZ4 is very fast but has medium compression, and Snappy is balanced but older.
</details>

### 3. What does the CRC32C in a Record Batch cover?
- [ ] A) The entire batch including all header fields
- [ ] B) Only the records array
- [ ] C) Attributes through the end of the records array
- [ ] D) Only the compressed data

<details>
<summary>Answer</summary>
**C)** The CRC32C covers from the Attributes field through the end of the Records Array. It does NOT cover Base Offset, Batch Length, Partition Leader Epoch, or Magic.
</details>

### 4. How are timestamps stored within individual records in a Record Batch?
- [ ] A) As absolute Unix timestamps (8 bytes each)
- [ ] B) As deltas from the batch's first timestamp (VarInt)
- [ ] C) Not stored at record level, only at batch level
- [ ] D) As ISO 8601 formatted strings

<details>
<summary>Answer</summary>
**B)** Individual records store timestamps as VarInt deltas from the batch's First Timestamp field. This reduces storage size compared to storing full timestamps for each record.
</details>

### 5. Which bits in the Record Batch attributes specify the compression type?
- [ ] A) Bits 0-1
- [ ] B) Bits 0-2
- [ ] C) Bits 3-5
- [ ] D) Bits 4-6

<details>
<summary>Answer</summary>
**B)** Bits 0-2 of the attributes specify the compression type (0=None, 1=GZIP, 2=Snappy, 3=LZ4, 4=ZSTD).
</details>

## True/False

### 6. In the Record Batch format, message headers are stored as key-value pairs where both key and value are UTF-8 strings.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Header keys are UTF-8 strings, but header values are binary data (bytes). This allows headers to carry arbitrary binary payloads, not just text.
</details>

## Code Challenge

### 7. What's wrong with this record batch attributes calculation?

```csharp
short attributes = 0;
// Set compression to LZ4 (value 3)
attributes = 3;
// Set timestamp type to LogAppendTime
attributes |= (1 << 2);
// Set transactional flag
attributes |= (1 << 4);
```

<details>
<summary>Answer</summary>
The issue is that LogAppendTime should be set at bit 3, not bit 2. Bits 0-2 are for compression:

```csharp
short attributes = 0;
// Set compression to LZ4 (value 3) - bits 0-2
attributes = 3;
// Set timestamp type to LogAppendTime - bit 3 (not bit 2)
attributes |= (1 << 3);
// Set transactional flag - bit 4
attributes |= (1 << 4);
```

With the incorrect code, setting bit 2 would change the compression type from LZ4 (011) to ZSTD (111) instead of setting the timestamp type.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 3-4/7: Review the lesson
- <3/7: Re-do the lesson
