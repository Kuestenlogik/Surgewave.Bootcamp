# Lesson 27.3: Azure Blob Storage

Azure Blob Storage provides a native object store backend with strong integration into the Azure ecosystem, including managed identity, geo-redundant storage, and lifecycle management.

## Learning Objectives

- Configure Azure Blob storage with connection strings or managed identity
- Understand Block Blob vs Page Blob selection
- Set up lifecycle management policies for automatic tiering
- Choose the right geo-redundancy level (LRS, ZRS, GRS, GZRS)

## Configuration

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "AzureBlobDirect",
            "AzureBlob": {
                "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=surgewavedata;...",
                "ContainerName": "surgewave-segments",
                "Prefix": "v1/segments/"
            }
        }
    }
}
```

## Managed Identity (Recommended)

Use Azure Managed Identity for secure, credential-free authentication. Assign the `Storage Blob Data Contributor` role to the managed identity on the storage account.

## Block Blob vs Page Blob

| Feature | Block Blob | Page Blob |
|---------|-----------|-----------|
| Max Size | 190.7 TiB | 8 TiB |
| Write Pattern | Append/replace blocks | Random read/write |
| Ideal For | Log segments (.log) | Index files (.index) |

## Geo-Redundant Storage (GRS)

| Redundancy | Copies | Regions | Durability |
|------------|--------|---------|------------|
| LRS | 3 | 1 | 11 nines |
| ZRS | 3 | 1 (3 zones) | 12 nines |
| GRS | 6 | 2 | 16 nines |
| GZRS | 6 | 2 (3 zones primary) | 16 nines |

---

[Back to Unit 30](../README.md)
