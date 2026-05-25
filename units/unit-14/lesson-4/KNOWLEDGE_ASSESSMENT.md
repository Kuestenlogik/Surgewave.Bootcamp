# Knowledge Assessment: Lesson 19.4 - Protocol Debugging

## Multiple Choice

### 1. In a hex dump, what do the bytes "00 03" represent when found in the API Key position?
- [ ] A) Produce request
- [ ] B) Fetch request
- [ ] C) Metadata request
- [ ] D) ApiVersions request

<details>
<summary>Answer</summary>
**C)** The bytes "00 03" represent API Key 3 in big-endian format, which is the Metadata request.
</details>

### 2. What is the correct Wireshark filter to capture Kafka protocol traffic?
- [ ] A) tcp.port == 9092
- [ ] B) kafka
- [ ] C) kafka || tcp.port == 9092
- [ ] D) protocol == kafka

<details>
<summary>Answer</summary>
**C)** Using "kafka || tcp.port == 9092" captures both recognized Kafka protocol packets and any traffic on the default Kafka port, providing comprehensive coverage.
</details>

### 3. What error pattern does the hex sequence "2e 00 00 00" at the start of a message indicate?
- [ ] A) Correct message format
- [ ] B) Wrong byte order (little-endian instead of big-endian)
- [ ] C) Corrupted CRC
- [ ] D) Invalid API version

<details>
<summary>Answer</summary>
**B)** The sequence "2e 00 00 00" represents little-endian encoding of value 46, whereas Kafka requires big-endian "00 00 00 2e". This is a common byte order error.
</details>

### 4. What does error code 35 (UNSUPPORTED_VERSION) indicate?
- [ ] A) The broker is running an older version
- [ ] B) The request uses a protocol version not supported by the broker
- [ ] C) The client library is outdated
- [ ] D) SSL/TLS version mismatch

<details>
<summary>Answer</summary>
**B)** Error code 35 indicates the request uses an API version that the broker does not support. The client should use ApiVersions to discover supported versions.
</details>

### 5. Which tool would you use for command-line packet capture without a GUI?
- [ ] A) Wireshark
- [ ] B) tcpdump
- [ ] C) xxd
- [ ] D) netcat

<details>
<summary>Answer</summary>
**B)** tcpdump is the command-line packet capture tool. xxd is for hex dumps of files, netcat is for raw TCP connections, and Wireshark is GUI-based.
</details>

## True/False

### 6. If a message header specifies 100 bytes but only 80 bytes are sent, the broker will return an immediate error.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The broker will wait for the remaining bytes until a timeout occurs. This is a truncated message error where the broker cannot parse the incomplete data, leading to connection timeout rather than an immediate error.
</details>

## Code Challenge

### 7. Analyze this hex dump and identify the issue:

```
00000000 00 00 00 12 00 03 00 00 00 00 00 01 00 0c 6d 69
00000010 6e 69 6d 61 6c 00 63 6c 69 65 6e 74 ff ff ff ff
```

<details>
<summary>Answer</summary>
The issue is in the Client ID string. The string "minimal-client" should be continuous, but there's a null byte (00) inserted between "minimal" and "client" at position 0x16:

- Bytes 0x0E-0x15: "minimal" (7 chars)
- Byte 0x16: 00 (null - incorrect!)
- Bytes 0x17-0x1C: "client" (6 chars)

The Client ID length says 12 bytes (00 0c), but "minimal" + null + "client" = 14 bytes of data before the topic array marker (ff ff ff ff).

Correct hex should be:
```
00000000 00 00 00 1e 00 03 00 00 00 00 00 01 00 0e 6d 69
00000010 6e 69 6d 61 6c 2d 63 6c 69 65 6e 74 ff ff ff ff
```
Where 0x2d is the hyphen "-" character and length is 14 (0x0e).
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 3-4/7: Review the lesson
- <3/7: Re-do the lesson
