# Knowledge Assessment: Lesson 19.5 - Protocol Extensions

## Multiple Choice

### 1. What is the primary advantage of the flexible protocol format (v2+) over the old protocol?
- [ ] A) Smaller message sizes
- [ ] B) Faster parsing speed
- [ ] C) Ability to add optional tagged fields without breaking compatibility
- [ ] D) Better compression support

<details>
<summary>Answer</summary>
**C)** The flexible protocol format allows adding new optional tagged fields without breaking existing clients. Old clients simply ignore unknown tags.
</details>

### 2. According to the recommended tag ID allocation, which range is reserved for Surgewave proprietary extensions?
- [ ] A) 0-9999
- [ ] B) 10000-19999
- [ ] C) 20000-29999
- [ ] D) 30000+

<details>
<summary>Answer</summary>
**B)** Tag IDs 10000-19999 are reserved for Surgewave proprietary extensions. 0-9999 is reserved for Kafka protocol, 20000-29999 for organization-specific, and 30000+ for application-specific use.
</details>

### 3. How is the number of tagged fields encoded in the flexible protocol?
- [ ] A) Fixed 4-byte integer
- [ ] B) Fixed 2-byte integer
- [ ] C) VarInt (variable-length integer)
- [ ] D) As part of the message length

<details>
<summary>Answer</summary>
**C)** The number of tagged fields is encoded as a VarInt (unsigned), which allows efficient encoding of small numbers while supporting larger values when needed.
</details>

### 4. Which of the following is a SAFE (backward compatible) protocol change?
- [ ] A) Removing a required field
- [ ] B) Changing a field's data type
- [ ] C) Adding a new optional tagged field
- [ ] D) Changing a field's semantic meaning

<details>
<summary>Answer</summary>
**C)** Adding new optional tagged fields is backward compatible because existing clients will ignore unknown tags. All other options are breaking changes.
</details>

### 5. What information should a custom trace header (tag 10000) typically contain?
- [ ] A) Only the trace ID
- [ ] B) Trace ID, span ID, and trace flags
- [ ] C) Only timing information
- [ ] D) Client version and hostname

<details>
<summary>Answer</summary>
**B)** A trace context header typically contains trace_id (16-byte W3C trace ID), span_id (8-byte span ID), and trace_flags (1-byte flags) for distributed tracing integration.
</details>

## True/False

### 6. Tag IDs can be reused for different purposes across different API versions.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Tag IDs should NEVER be reused for different purposes. Once a tag ID is assigned a meaning, it must retain that meaning to maintain compatibility across versions.
</details>

## Code Challenge

### 7. What's wrong with this tagged field encoding?

```csharp
void WriteTaggedField(BinaryWriter writer, int tagId, byte[] data)
{
    // Write tag ID
    writer.Write(tagId);  // 4 bytes
    // Write data length
    writer.Write(data.Length);  // 4 bytes
    // Write data
    writer.Write(data);
}
```

<details>
<summary>Answer</summary>
The issue is that tagged fields use VarInt encoding for both the tag ID and the size, not fixed 4-byte integers:

```csharp
void WriteTaggedField(BinaryWriter writer, int tagId, byte[] data)
{
    // Write tag ID as unsigned VarInt
    WriteUnsignedVarInt(writer, tagId);
    // Write data length as unsigned VarInt
    WriteUnsignedVarInt(writer, data.Length);
    // Write data
    writer.Write(data);
}

void WriteUnsignedVarInt(BinaryWriter writer, int value)
{
    while ((value & ~0x7F) != 0)
    {
        writer.Write((byte)((value & 0x7F) | 0x80));
        value >>= 7;
    }
    writer.Write((byte)value);
}
```

Using fixed 4-byte integers would waste space (8 bytes overhead vs 2 bytes for small tags) and would not be parseable by compliant readers.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 3-4/7: Review the lesson
- <3/7: Re-do the lesson
