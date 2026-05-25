# Knowledge Assessment: Lesson 24.3 - S3/MinIO Integration

## Multiple Choice

### 1. Which S3 storage class provides the best balance of cost and instant retrieval for cold tier data?
- [ ] A) S3 Standard
- [ ] B) S3 Standard-IA
- [ ] C) S3 Glacier Instant Retrieval
- [ ] D) S3 Glacier Deep Archive

<details>
<summary>Answer</summary>
**C)** S3 Glacier Instant Retrieval provides millisecond access (like Standard) at approximately 68% lower cost than Standard. It's ideal for cold tier data that needs occasional fast access.
</details>

### 2. What is the minimum size threshold for multipart upload in S3?
- [ ] A) 1 MB
- [ ] B) 5 MB
- [ ] C) 100 MB
- [ ] D) 1 GB

<details>
<summary>Answer</summary>
**B)** S3 requires multipart upload for objects larger than 5 GB, but it's recommended to use multipart for anything over 5 MB for better reliability and the ability to retry individual parts.
</details>

### 3. Why is path-style URL access required for MinIO?
- [ ] A) MinIO doesn't support virtual-hosted style
- [ ] B) It's more secure
- [ ] C) It's faster
- [ ] D) Path-style is the S3 default

<details>
<summary>Answer</summary>
**A)** MinIO and other S3-compatible services often don't support virtual-hosted style URLs (bucket.endpoint.com) and require path-style URLs (endpoint.com/bucket) instead. AWS S3 supports both but defaults to virtual-hosted style.
</details>

### 4. What is the recommended approach for segment metadata in S3?
- [ ] A) Store in a separate database
- [ ] B) Include in the segment filename only
- [ ] C) Use S3 object metadata headers
- [ ] D) Don't store metadata

<details>
<summary>Answer</summary>
**C)** Using S3 object metadata headers (x-amz-meta-*) keeps metadata co-located with the object, survives object copies/moves, and can be retrieved without downloading the full object.
</details>

### 5. What happens if a multipart upload is not completed or aborted?
- [ ] A) Parts are automatically deleted after 24 hours
- [ ] B) Parts remain and accrue storage costs indefinitely
- [ ] C) Parts are merged automatically
- [ ] D) S3 returns an error

<details>
<summary>Answer</summary>
**B)** Incomplete multipart uploads remain and continue to accrue storage costs until explicitly aborted. Always configure lifecycle rules to automatically clean up incomplete uploads after a specified period.
</details>

## True/False

### 6. S3 Glacier Deep Archive provides instant retrieval like S3 Standard.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - S3 Glacier Deep Archive has a retrieval time of 12-48 hours (standard retrieval) and is meant for data accessed once or twice per year. Use Glacier Instant Retrieval for data that needs fast access.
</details>

### 7. Compressing segments before uploading to S3 reduces both storage costs and data transfer costs.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Compression reduces the amount of data stored (lowering storage costs) and the amount of data transferred during upload/download (lowering data transfer costs). This is especially important for S3 where egress costs can be significant.
</details>

### 8. MinIO can be used as a drop-in replacement for S3 in production without any code changes.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - MinIO implements the S3 API and can be used as a drop-in replacement. Code that works with S3 will work with MinIO by simply changing the endpoint configuration. This makes MinIO ideal for local development and on-premises deployments.
</details>

## Code Challenge

### 9. What's wrong with this S3 key generation?

```csharp
public static string GenerateKey(string topic, int partition, long offset)
{
    return $"{topic}/{partition}/{offset}.log";
}
```

<details>
<summary>Answer</summary>
There are several issues:

1. **No padding on offset** - Will sort incorrectly (1, 10, 11, 2, 20, 21...)
2. **No prefix** - Makes it hard to organize multiple applications
3. **No file extension for compression** - Unclear if compressed
4. **No leading zeros on partition** - Minor, but inconsistent

Corrected version:
```csharp
public static string GenerateKey(string prefix, string topic, int partition, long offset)
{
    // 20 digits handles offsets up to 10^20
    // Include compression extension
    return $"{prefix}/{topic}/{partition:D4}/{offset:D20}.log.zst";
}

// Example outputs:
// surgewave/topics/orders/0000/00000000000000000000.log.zst
// surgewave/topics/orders/0000/00000000000001048576.log.zst
```

This ensures:
- Proper lexicographic sorting
- Clear organization with prefix
- Compression indicator
- Consistent formatting
</details>

### 10. Design a lifecycle policy for Surgewave segments in S3 with the following requirements:
- Hot data: accessed frequently for 7 days
- Warm data: accessed occasionally for 30 days
- Cold data: rarely accessed, kept for 1 year
- Archive: compliance requirement to keep for 7 years

<details>
<summary>Answer</summary>
```json
{
  "Rules": [
    {
      "ID": "surgewave-segment-lifecycle",
      "Status": "Enabled",
      "Filter": {
        "Prefix": "surgewave/topics/"
      },
      "Transitions": [
        {
          "Days": 7,
          "StorageClass": "STANDARD_IA"
        },
        {
          "Days": 30,
          "StorageClass": "GLACIER_IR"
        },
        {
          "Days": 365,
          "StorageClass": "GLACIER"
        },
        {
          "Days": 730,
          "StorageClass": "DEEP_ARCHIVE"
        }
      ],
      "Expiration": {
        "Days": 2555
      }
    },
    {
      "ID": "cleanup-incomplete-uploads",
      "Status": "Enabled",
      "Filter": {
        "Prefix": "surgewave/topics/"
      },
      "AbortIncompleteMultipartUpload": {
        "DaysAfterInitiation": 7
      }
    }
  ]
}
```

Timeline:
- Days 0-7: S3 Standard ($0.023/GB) - Hot
- Days 7-30: Standard-IA ($0.0125/GB) - Warm
- Days 30-365: Glacier IR ($0.004/GB) - Cold
- Days 365-730: Glacier ($0.0036/GB) - Archive
- Days 730-2555: Deep Archive ($0.00099/GB) - Compliance
- Day 2555+: Deleted (7 years retention met)

Cost savings vs. keeping all in Standard: ~85%
</details>

## Score
- 10/10: Excellent! Ready for production S3 integration
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson
