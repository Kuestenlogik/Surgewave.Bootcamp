# Knowledge Assessment: Lesson 24.4 - Azure Blob Storage

## Multiple Choice

### 1. What is the minimum storage duration for Azure Cool tier before early deletion penalty applies?
- [ ] A) No minimum
- [ ] B) 30 days
- [ ] C) 90 days
- [ ] D) 180 days

<details>
<summary>Answer</summary>
**B)** Azure Cool tier has a minimum storage duration of 30 days. Deleting or changing the tier of a blob before 30 days incurs an early deletion charge equivalent to storing the blob for the remaining days.
</details>

### 2. Which Azure tier provides instant access but has significantly higher read operation costs?
- [ ] A) Hot
- [ ] B) Cool
- [ ] C) Cold
- [ ] D) Archive

<details>
<summary>Answer</summary>
**D)** Archive tier has instant read... wait, that's incorrect! Archive tier requires rehydration (hours). The question is tricky - actually **C) Cold** tier provides instant access but with higher operation costs than Cool ($0.01 per 10k reads vs $0.001). Archive tier does NOT provide instant access.
</details>

### 3. What happens when you try to read a blob in Azure Archive tier?
- [ ] A) It reads instantly
- [ ] B) It returns an error requiring rehydration first
- [ ] C) It automatically rehydrates
- [ ] D) It redirects to a cached copy

<details>
<summary>Answer</summary>
**B)** Reading a blob in Archive tier returns an error (409 Conflict). You must first initiate rehydration by setting the blob to Hot or Cool tier, wait for the operation to complete (up to 15 hours for standard priority), and then read the blob.
</details>

### 4. Which authentication method is recommended for Azure Blob Storage in production?
- [ ] A) Connection strings with SAS tokens
- [ ] B) Account keys stored in environment variables
- [ ] C) Managed Identity (DefaultAzureCredential)
- [ ] D) Anonymous public access

<details>
<summary>Answer</summary>
**C)** Managed Identity with DefaultAzureCredential is the recommended approach. It eliminates the need to store credentials in code or configuration, automatically rotates credentials, and integrates with Azure's identity platform for fine-grained access control.
</details>

### 5. What is the cost difference between Azure Archive and S3 Glacier Deep Archive for storage?
- [ ] A) Azure is significantly cheaper
- [ ] B) S3 is significantly cheaper
- [ ] C) They are approximately the same cost
- [ ] D) Cannot be compared

<details>
<summary>Answer</summary>
**C)** Both Azure Archive and S3 Glacier Deep Archive cost approximately $0.00099/GB/month, making them comparable for archive storage costs. The main differences are in retrieval costs and retrieval time options.
</details>

## True/False

### 6. Azure lifecycle management policies can only transition blobs to colder tiers, never to warmer tiers.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Azure lifecycle management can only transition blobs from warmer to colder tiers (Hot -> Cool -> Cold -> Archive) and delete blobs. To move a blob to a warmer tier, you must use the Set Blob Tier API directly or copy the blob. This is by design to prevent accidental cost increases.
</details>

### 7. Setting the access tier at upload time to Cool instead of Hot can save money even for short-lived data.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Due to the minimum storage duration (30 days for Cool, 90 days for Cold), setting a colder tier at upload for short-lived data can actually cost MORE due to early deletion penalties. For data that will be deleted or transitioned within 30 days, Hot tier is often more cost-effective.
</details>

### 8. Azure Blob Storage supports immutable storage (WORM) for compliance scenarios.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Azure Blob Storage supports both time-based retention policies (WORM - Write Once, Read Many) and legal holds for compliance scenarios. This is important for industries with regulatory requirements like healthcare (HIPAA) and finance (SEC Rule 17a-4).
</details>

## Code Challenge

### 9. What's wrong with this lifecycle policy?

```json
{
  "rules": [{
    "name": "archive-immediately",
    "definition": {
      "filters": {
        "prefixMatch": ["topics/"]
      },
      "actions": {
        "baseBlob": {
          "tierToArchive": {
            "daysAfterModificationGreaterThan": 1
          }
        }
      }
    }
  }]
}
```

<details>
<summary>Answer</summary>
There are several issues:

1. **Too aggressive archival** - Moving to Archive after just 1 day means blobs will likely need rehydration frequently, which is expensive ($0.02-0.10 per GB to rehydrate).

2. **Missing intermediate tiers** - Should transition through Cool -> Cold -> Archive for gradual cost optimization.

3. **Archive minimum is 180 days** - Deleting or transitioning archived blobs before 180 days incurs early deletion penalty.

4. **No delete rule** - Without a delete rule, blobs accumulate forever in Archive.

Corrected version:
```json
{
  "rules": [{
    "name": "surgewave-tiering-policy",
    "definition": {
      "filters": {
        "prefixMatch": ["topics/"]
      },
      "actions": {
        "baseBlob": {
          "tierToCool": {
            "daysAfterModificationGreaterThan": 7
          },
          "tierToCold": {
            "daysAfterModificationGreaterThan": 30
          },
          "tierToArchive": {
            "daysAfterModificationGreaterThan": 90
          },
          "delete": {
            "daysAfterModificationGreaterThan": 365
          }
        }
      }
    }
  }]
}
```
</details>

### 10. Calculate the monthly cost for the following Azure storage scenario:

- Storage: 500 GB
- Access tier: Cold
- Monthly operations: 50,000 reads, 5,000 writes

Given costs:
- Cold storage: $0.0036/GB/month
- Cold read operations: $0.10 per 10,000
- Cold write operations: $0.18 per 10,000

<details>
<summary>Answer</summary>
**Storage cost:**
500 GB × $0.0036/GB = $1.80

**Read operations cost:**
50,000 reads ÷ 10,000 × $0.10 = $0.50

**Write operations cost:**
5,000 writes ÷ 10,000 × $0.18 = $0.09

**Total monthly cost: $2.39**

Comparison with other tiers:
- Hot: 500 × $0.0184 + 5 × $0.00044 + 0.5 × $0.00055 = $9.20
- Cool: 500 × $0.01 + 5 × $0.001 + 0.5 × $0.01 = $5.01
- Archive: 500 × $0.00099 + 5 × $0.55 + 0.5 × $0.011 = $3.26 (but requires rehydration!)

Cold tier is optimal for this scenario - it provides instant access at lower cost than Archive (which would require expensive rehydration for 50k reads).
</details>

## Score
- 10/10: Excellent! Ready for production Azure integration
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson
