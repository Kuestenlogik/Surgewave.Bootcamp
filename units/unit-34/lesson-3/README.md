# Lesson 34.3: SIMD & Hardware Acceleration

### CRC32C with SSE4.2 / ARM CRC Intrinsics

Surgewave uses hardware-accelerated CRC32C for message integrity checks:

```csharp
using System.Runtime.Intrinsics.X86;

// Check for hardware support at runtime
if (Sse42.IsSupported)
{
    // Hardware CRC32C: ~1 cycle per byte
    uint crc = 0xFFFFFFFF;
    crc = Sse42.Crc32(crc, data[0]);
    crc = Sse42.Crc32(crc, data[1]);
    // ...
}
else
{
    // Software fallback: ~10x slower
    crc = SoftwareCrc32C(data);
}
```

### Vector Cosine Similarity

The vector store uses SIMD for cosine similarity calculation:

```csharp
using System.Numerics;

static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
{
    if (Vector.IsHardwareAccelerated)
    {
        // Process Vector<float>.Count elements per iteration (8 on AVX2)
        var dotProduct = Vector<float>.Zero;
        var normA = Vector<float>.Zero;
        var normB = Vector<float>.Zero;

        int i = 0;
        int vectorSize = Vector<float>.Count;

        for (; i <= a.Length - vectorSize; i += vectorSize)
        {
            var va = new Vector<float>(a.Slice(i, vectorSize));
            var vb = new Vector<float>(b.Slice(i, vectorSize));

            dotProduct += va * vb;
            normA += va * va;
            normB += vb * vb;
        }

        float dot = Vector.Sum(dotProduct);
        float na = Vector.Sum(normA);
        float nb = Vector.Sum(normB);

        // Handle remaining elements
        for (; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        return dot / (MathF.Sqrt(na) * MathF.Sqrt(nb));
    }

    // Scalar fallback
    return ScalarCosineSimilarity(a, b);
}
```

### Checking Hardware Support

```csharp
// x86/x64
Console.WriteLine($"SSE2:    {Sse2.IsSupported}");
Console.WriteLine($"SSE4.2:  {Sse42.IsSupported}");
Console.WriteLine($"AVX2:    {Avx2.IsSupported}");
Console.WriteLine($"Vector:  {Vector.IsHardwareAccelerated}");
Console.WriteLine($"VecSize: {Vector<float>.Count} floats");

// On a modern x64 CPU with AVX2:
// SSE2:    True
// SSE4.2:  True
// AVX2:    True
// Vector:  True
// VecSize: 8 floats (256-bit SIMD)
```

---

[Back to Unit 33](../README.md)
