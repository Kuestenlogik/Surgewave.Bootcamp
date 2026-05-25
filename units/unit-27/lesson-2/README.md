# Lesson 27.2: S3-Direct Storage

S3-Direct writes message segments directly to Amazon S3 or any S3-compatible service. This is the most widely supported object store backend.

## Learning Objectives

- Configure S3-Direct storage with bucket, region, and credentials
- Use S3-compatible services (MinIO, Backblaze B2, Cloudflare R2)
- Understand object key structure and multipart upload
- Set up MinIO for local development

## Configuration

```json
{
    "Surgewave": {
        "Storage": {
            "Engine": "S3Direct",
            "S3": {
                "Bucket": "surgewave-data",
                "Region": "us-east-1",
                "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
                "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                "Prefix": "v1/segments/",
                "MultipartThresholdBytes": 5242880
            }
        }
    }
}
```

## S3-Compatible Services

| Service | ServiceUrl | ForcePathStyle | Notes |
|---------|-----------|----------------|-------|
| AWS S3 | null | false | Native support |
| MinIO | `http://localhost:9000` | true | Local development |
| Backblaze B2 | `https://s3.us-west-004.backblazeb2.com` | false | Cost-effective |
| Wasabi | `https://s3.wasabisys.com` | false | No egress fees |
| Cloudflare R2 | `https://<account>.r2.cloudflarestorage.com` | true | Zero egress fees |

## Object Key Structure

```
{prefix}/{topic}/{partition}/{segment-offset}.log
{prefix}/{topic}/{partition}/{segment-offset}.index
{prefix}/{topic}/{partition}/{segment-offset}.timeindex
```

## Multipart Upload

For segments larger than `MultipartThresholdBytes` (default 5 MB), Surgewave uses S3 multipart upload for improved reliability and throughput. Parts are uploaded in parallel and failed parts can be retried independently.

## Local Development with MinIO

```bash
docker run -d --name minio -p 9000:9000 -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin -e MINIO_ROOT_PASSWORD=minioadmin \
  minio/minio server /data --console-address ":9001"
```

---

[Back to Unit 30](../README.md)
