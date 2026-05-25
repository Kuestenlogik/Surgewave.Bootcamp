# Lesson 27.4: GCP Cloud Storage

Google Cloud Storage provides a unified object storage service with multiple storage classes for cost optimization.

## Learning Objectives

- Configure GCP Cloud Storage with service account or workload identity
- Understand storage classes (Standard, Nearline, Coldline, Archive)
- Enable object versioning for point-in-time recovery
- Set up cross-region replication with dual-region buckets

## Configuration

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "GcpCloudStorageDirect",
            "Gcp": {
                "ProjectId": "my-surgewave-project",
                "Bucket": "surgewave-segments-prod",
                "Prefix": "v1/segments/",
                "CredentialPath": "/etc/surgewave/gcp-sa-key.json"
            }
        }
    }
}
```

## Workload Identity (Recommended for GKE)

When running on GKE, use Workload Identity to avoid service account keys. Bind the Kubernetes service account to a GCP service account with `roles/storage.objectAdmin`.

## Storage Classes

| Class | Min Duration | Retrieval Cost | Storage Cost | Use Case |
|-------|-------------|----------------|--------------|----------|
| Standard | None | Free | $0.020/GB | Hot data (< 30 days) |
| Nearline | 30 days | $0.01/GB | $0.010/GB | Monthly access |
| Coldline | 90 days | $0.02/GB | $0.004/GB | Quarterly access |
| Archive | 365 days | $0.05/GB | $0.0012/GB | Long-term storage |

## Cross-Region Replication

| Location Type | Example | Availability |
|---------------|---------|--------------|
| Region | us-east1 | 99.9% |
| Dual-Region | us-east1 + us-west1 | 99.95% |
| Multi-Region | US (multiple regions) | 99.95% |

---

[Back to Unit 30](../README.md)
