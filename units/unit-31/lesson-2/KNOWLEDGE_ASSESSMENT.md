# Knowledge Assessment: Lesson 21.2 - In-Memory Deduplication

## Multiple Choice

### 1. What is the time complexity of lookup operations in an LRU cache?
- [ ] A) O(n)
- [ ] B) O(log n)
- [ ] C) O(1)
- [ ] D) O(n log n)

<details>
<summary>Answer</summary>
**C)** LRU cache achieves O(1) lookup time by combining a Dictionary for fast key lookups with a LinkedList for maintaining access order.
</details>

### 2. What does a Bloom filter return when checking for a key that was definitely NOT added?
- [ ] A) "Definitely seen"
- [ ] B) "Maybe seen"
- [ ] C) "Definitely not seen"
- [ ] D) It throws an exception

<details>
<summary>Answer</summary>
**C)** A Bloom filter guarantees "definitely not seen" when a key was never added (no false negatives). It only returns "maybe seen" for keys that were added or due to hash collisions.
</details>

### 3. What is the approximate memory usage of a Bloom filter for 1 million keys?
- [ ] A) ~150 KB
- [ ] B) ~1.5 MB
- [ ] C) ~15 MB
- [ ] D) ~50 MB

<details>
<summary>Answer</summary>
**A)** A Bloom filter uses approximately 1.2 bits per key, so 1 million keys requires about 1.2 million bits = 150 KB. This is dramatically less than HashSet (~50 MB) or LRU Cache (~70 MB).
</details>

### 4. In a multi-layer deduplication pipeline, why is the Bloom filter placed first?
- [ ] A) It has the highest accuracy
- [ ] B) It provides the fastest negative result for definitely-new keys
- [ ] C) It uses the most memory
- [ ] D) It can delete old entries

<details>
<summary>Answer</summary>
**B)** The Bloom filter is placed first because it can quickly determine "definitely not seen" with minimal memory and O(k) time, avoiding the need for more expensive checks for most new keys.
</details>

### 5. Which cleanup strategy for time-based expiry checks entry validity only when the entry is accessed?
- [ ] A) Eager cleanup
- [ ] B) Background cleanup
- [ ] C) Lazy cleanup
- [ ] D) Sliding cleanup

<details>
<summary>Answer</summary>
**C)** Lazy cleanup checks expiry only when an entry is accessed. This avoids background thread overhead but may keep expired entries in memory until accessed.
</details>

## True/False

### 6. A Bloom filter can produce false negatives (saying a key wasn't added when it was).
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - A Bloom filter NEVER produces false negatives. If it says "definitely not seen," the key was definitely never added. It can only produce false positives (saying "maybe seen" for a key that wasn't added due to hash collisions).
</details>

### 7. An LRU cache automatically removes expired entries based on time.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - A basic LRU cache evicts based on access recency, not time. To add time-based expiry, you need to combine LRU with timestamp tracking and implement cleanup logic.
</details>

## Code Challenge

### 8. What's the issue with this Bloom filter usage?

```csharp
var bloomFilter = new BloomFilter(capacity: 1000000, errorRate: 0.01);

async Task ProcessMessage(Message message)
{
    if (bloomFilter.Contains(message.IdempotencyKey))
    {
        Console.WriteLine("Duplicate detected, skipping");
        return;
    }

    await HandleBusinessLogic(message);
    bloomFilter.Add(message.IdempotencyKey);
}
```

<details>
<summary>Answer</summary>
The code treats Bloom filter "contains" as a definitive duplicate check, but Bloom filters have false positives. A key that was never added might still return true:

```csharp
var bloomFilter = new BloomFilter(capacity: 1000000, errorRate: 0.01);
var exactCache = new LruCache<string, bool>(capacity: 100000);

async Task ProcessMessage(Message message)
{
    var key = message.IdempotencyKey;

    // Layer 1: Bloom filter for fast "definitely not seen" check
    if (!bloomFilter.Contains(key))
    {
        // Definitely new - process it
        await HandleBusinessLogic(message);
        bloomFilter.Add(key);
        exactCache.Add(key, true);
        return;
    }

    // Layer 2: "Maybe seen" - check exact cache to confirm
    if (exactCache.Contains(key))
    {
        Console.WriteLine("Confirmed duplicate, skipping");
        return;
    }

    // False positive from Bloom filter - process it
    await HandleBusinessLogic(message);
    bloomFilter.Add(key);  // Already in filter due to collision
    exactCache.Add(key, true);
}
```

Bloom filter should only be used as a pre-filter. When it returns "maybe seen," you need a secondary exact check (LRU cache or database) to confirm.
</details>

## Score
- 8/8: Excellent!
- 6-7/8: Good understanding
- 4-5/8: Review the lesson
- <4/8: Re-do the lesson
