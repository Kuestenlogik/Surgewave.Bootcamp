# Knowledge Assessment: Lesson 17.2 - Compression

## Multiple Choice

### 1. Which compression codec provides the fastest compression speed?
- [ ] A) GZIP
- [ ] B) ZSTD
- [ ] C) LZ4
- [ ] D) Snappy

<details>
<summary>Answer</summary>
**C)** LZ4 provides the fastest compression speed (~650 MB/s) with low CPU usage, making it ideal for low-latency requirements.
</details>

### 2. Which codec should you use when storage and bandwidth are constrained and you need the best compression ratio?
- [ ] A) LZ4
- [ ] B) Snappy
- [ ] C) ZSTD
- [ ] D) None

<details>
<summary>Answer</summary>
**C)** ZSTD offers the best balance of compression ratio (~5:1) with reasonable speed. It's ideal when storage/bandwidth is limited and CPU is available.
</details>

### 3. When should you use NO compression?
- [ ] A) When messages are large JSON payloads
- [ ] B) When data is already compressed (images, video) or messages are very small
- [ ] C) When network bandwidth is limited
- [ ] D) When storage costs are high

<details>
<summary>Answer</summary>
**B)** Compression should be skipped when data is already compressed (images, video, encrypted data) or messages are very small (< 100 bytes), as compression overhead would be wasteful or even increase size.
</details>

## True/False

### 4. Messages are automatically decompressed by Surgewave when consumed, so consumers don't need to know the compression codec used.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Surgewave handles transparent decompression. The compression codec is stored with the message, and the consumer automatically decompresses using the appropriate codec.
</details>

### 5. Higher compression levels always result in smaller output sizes.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - While higher compression levels generally provide better ratios, they have diminishing returns and significantly slower speeds. Level 9 may only save a small percentage more than level 6 but take much longer. The difference depends on data characteristics.
</details>

## Code Challenge

### 6. What's wrong with this compression ratio calculation?
```csharp
var original = "Large JSON data...";
var compressed = Compress(original);
var ratio = original.Length / compressed.Length;
Console.WriteLine($"Compression ratio: {ratio}x");
```

<details>
<summary>Answer</summary>
The code compares string length (characters) with byte array length (bytes). For accurate ratio calculation:
```csharp
var original = "Large JSON data...";
var originalSize = Encoding.UTF8.GetByteCount(original);
var compressed = Compress(original);
var compressedSize = compressed.Length;
var ratio = (double)originalSize / compressedSize;
Console.WriteLine($"Compression ratio: {ratio:F1}x");
```
Also, the division should use floating-point to avoid integer truncation.
</details>

### 7. Based on the codec comparison, which codec would you choose for a real-time streaming application where latency is critical?
- [ ] A) GZIP - because it has the best compatibility
- [ ] B) ZSTD - because it has the best compression ratio
- [ ] C) LZ4 - because it has the lowest latency and CPU usage
- [ ] D) None - compression always adds latency

<details>
<summary>Answer</summary>
**C)** LZ4 is the best choice for real-time streaming where latency is critical. Its very fast compression/decompression speed (~650 MB/s compress, even faster decompress) with low CPU usage minimizes the latency impact, even though the compression ratio (~2:1) is lower than other codecs.
</details>

## Score
- 7/7: Excellent! You understand compression trade-offs
- 5-6/7: Good understanding of codec selection
- 3-4/7: Review codec comparison and use cases
- <3/7: Re-do the lesson focusing on when to use each codec
