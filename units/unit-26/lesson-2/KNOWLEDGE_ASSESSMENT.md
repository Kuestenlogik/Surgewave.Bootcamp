# Knowledge Assessment: Lesson 24.2 - Local File System Tier

## Multiple Choice

### 1. Which file type contains the actual message data in a segment?
- [ ] A) .index
- [ ] B) .timeindex
- [ ] C) .log
- [ ] D) .snapshot

<details>
<summary>Answer</summary>
**C)** The .log file contains the actual message data in append-only format. It stores the raw messages with their offsets, timestamps, keys, and values.
</details>

### 2. What compression algorithm is recommended for cold tier storage?
- [ ] A) No compression (speed priority)
- [ ] B) LZ4 (balanced)
- [ ] C) Zstd with high compression level
- [ ] D) RLE (Run-Length Encoding)

<details>
<summary>Answer</summary>
**C)** Zstd with high compression level is recommended for cold tier because storage cost savings outweigh the CPU cost of decompression, and cold tier data is accessed infrequently.
</details>

### 3. What triggers a segment to roll (create a new segment)?
- [ ] A) Only when the segment reaches its size limit
- [ ] B) Only when the segment reaches its age limit
- [ ] C) Either size limit OR age limit, whichever comes first
- [ ] D) Only through manual administrator action

<details>
<summary>Answer</summary>
**C)** A segment rolls when either the size limit (e.g., 1GB) OR the age limit (e.g., 24 hours) is reached, whichever comes first. This ensures both bounded file sizes and time-based organization.
</details>

### 4. What is the purpose of the .index file?
- [ ] A) Store compressed message data
- [ ] B) Map message offset to physical file position
- [ ] C) Track consumer group offsets
- [ ] D) Store message checksums

<details>
<summary>Answer</summary>
**B)** The .index file maps message offsets to physical positions in the .log file. This enables efficient random access by offset without scanning the entire log file.
</details>

### 5. How many active segments can a partition have at any given time?
- [ ] A) Zero
- [ ] B) One
- [ ] C) Multiple (configurable)
- [ ] D) Unlimited

<details>
<summary>Answer</summary>
**B)** Each partition has exactly one active segment at a time that receives writes. All other segments are sealed (read-only). When the active segment rolls, a new one becomes active.
</details>

## True/False

### 6. Index files should be stored on slower HDDs along with the data files to reduce costs.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Index files should be kept on SSDs even when data files are on HDDs. Index files are small but frequently accessed for message lookups. Keeping them on fast storage significantly improves read performance.
</details>

### 7. Segment compression should be applied before the segment is sealed (while it's still active).
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Compression should be applied after a segment is sealed. Compressing an active segment would require decompression for every write, which would be extremely slow. Seal first, then compress.
</details>

### 8. The .timeindex file allows efficient queries like "fetch messages from the last hour".
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The .timeindex file maps timestamps to offsets, enabling efficient time-based queries. Instead of scanning all messages, Surgewave can use the time index to find the starting offset for a given timestamp.
</details>

## Code Challenge

### 9. What's wrong with this segment cleanup implementation?

```csharp
public void Cleanup(TimeSpan retention)
{
    var cutoff = DateTime.UtcNow - retention;
    foreach (var file in Directory.GetFiles(_basePath, "*.log"))
    {
        if (File.GetCreationTimeUtc(file) < cutoff)
        {
            File.Delete(file);
        }
    }
}
```

<details>
<summary>Answer</summary>
There are multiple issues:

1. **Uses CreationTime instead of LastWriteTime** - CreationTime is unreliable across file systems and when files are moved. Use LastWriteTimeUtc which reflects when the segment was last modified.

2. **Only matches .log files** - This misses compressed files (.log.gz, .log.zst) and index files (.index, .timeindex).

3. **Deletes log without cleaning up index files** - Orphaned index files will remain on disk.

4. **No safety check for active segment** - Could delete a segment that's currently being written to.

Corrected version:
```csharp
public void Cleanup(TimeSpan retention)
{
    var cutoff = DateTime.UtcNow - retention;
    // Get all segment files (including compressed)
    var segmentFiles = Directory.GetFiles(_basePath, "*.log*");

    foreach (var file in segmentFiles)
    {
        var fileInfo = new FileInfo(file);
        // Use LastWriteTime, not CreationTime
        if (fileInfo.LastWriteTimeUtc < cutoff)
        {
            // Delete the log file
            File.Delete(file);

            // Also delete associated index files
            var baseName = Path.GetFileNameWithoutExtension(file);
            var indexFile = Path.Combine(_basePath, baseName + ".index");
            var timeIndexFile = Path.Combine(_basePath, baseName + ".timeindex");
            if (File.Exists(indexFile)) File.Delete(indexFile);
            if (File.Exists(timeIndexFile)) File.Delete(timeIndexFile);
        }
    }
}
```
</details>

### 10. Design a segment naming scheme that supports both offset-based and time-based organization.

<details>
<summary>Answer</summary>
A good naming scheme encodes both the base offset and creation timestamp:

```
{base_offset:20d}-{timestamp:yyyyMMddHHmmss}.log

Examples:
00000000000000000000-20240115103000.log  # Offset 0, Jan 15 2024 10:30:00
00000000000001048576-20240115110000.log  # Offset 1048576, Jan 15 2024 11:00:00
00000000000002097152-20240115113000.log  # Offset 2097152, Jan 15 2024 11:30:00
```

Benefits:
1. **Sortable by offset** - Lexicographic sort gives offset order
2. **Human-readable timestamp** - Easy to identify segment age
3. **Time-based queries** - Can quickly find segments in a time range
4. **Padding ensures consistent length** - 20 digits handles offsets up to 10^20

The associated index files would be:
```
00000000000000000000-20240115103000.index
00000000000000000000-20240115103000.timeindex
```
</details>

## Score
- 10/10: Excellent! Ready for production segment management
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson
