# Knowledge Assessment: Lesson 19.1 - Protocol Basics

## Multiple Choice

### 1. What is the size of the API Key field in a Kafka request header?
- [ ] A) 1 byte
- [ ] B) 2 bytes
- [ ] C) 4 bytes
- [ ] D) 8 bytes

<details>
<summary>Answer</summary>
**B)** The API Key field is 2 bytes (INT16), which identifies the request type (e.g., 0=Produce, 1=Fetch, 3=Metadata).
</details>

### 2. What is the purpose of the Correlation ID in the Kafka protocol?
- [ ] A) To identify the broker handling the request
- [ ] B) To track message offsets within a partition
- [ ] C) To match responses to their corresponding requests
- [ ] D) To specify the API version being used

<details>
<summary>Answer</summary>
**C)** The Correlation ID is a 4-byte client-provided ID used to match responses to their corresponding requests, which is especially important since responses may arrive out of order.
</details>

### 3. How is a null string encoded in the Kafka protocol?
- [ ] A) Length of 0 followed by no bytes
- [ ] B) Length of -1 (0xFFFF)
- [ ] C) A null terminator byte (0x00)
- [ ] D) An empty JSON object {}

<details>
<summary>Answer</summary>
**B)** A null string is encoded with a length of -1, which is represented as 0xFFFF in two bytes. An empty string uses length 0 (0x0000).
</details>

### 4. Which API Key is used for the Metadata request?
- [ ] A) 0
- [ ] B) 1
- [ ] C) 3
- [ ] D) 18

<details>
<summary>Answer</summary>
**C)** API Key 3 is used for Metadata requests. API Key 0 is Produce, 1 is Fetch, and 18 is ApiVersions.
</details>

## True/False

### 5. The Length field in a Kafka request includes the 4 bytes used by the Length field itself.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The Length field specifies the total message size excluding the Length field itself.
</details>

### 6. API version negotiation always starts by sending ApiVersions request with version 0.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Version 0 of the ApiVersions request is always supported by brokers for bootstrapping, allowing clients to discover which versions of each API the broker supports.
</details>

## Code Challenge

### 7. What is wrong with this request header construction?

```csharp
var header = new byte[14];
// Length field (will be set later)
BitConverter.GetBytes(10).CopyTo(header, 0);  // Length
BitConverter.GetBytes((short)3).CopyTo(header, 4);  // API Key
BitConverter.GetBytes((short)0).CopyTo(header, 6);  // Version
BitConverter.GetBytes(1).CopyTo(header, 8);  // Correlation ID
// Client ID: null
header[12] = 0xFF;
header[13] = 0xFF;
```

<details>
<summary>Answer</summary>
The issue is byte order. The Kafka protocol uses big-endian byte order, but BitConverter on Windows uses little-endian. The code should convert to big-endian:

```csharp
// Correct approach using big-endian
BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), 10);
BinaryPrimitives.WriteInt16BigEndian(header.AsSpan(4), 3);
BinaryPrimitives.WriteInt16BigEndian(header.AsSpan(6), 0);
BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8), 1);
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 3-4/7: Review the lesson
- <3/7: Re-do the lesson
