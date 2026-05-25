# Unit 14 Project — Completed Solution

This directory contains the completed solution for the **Unit 14 Project: Kubernetes Deployment**.

## Files

- `deployment.yaml` — Kubernetes Deployment manifest
- `service.yaml` — Kubernetes Service manifest
- `configmap.yaml` — Kubernetes ConfigMap manifest

## Deploying

```bash
kubectl apply -f configmap.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
```

> **Note:** Make sure you have a running Kubernetes cluster and `kubectl` configured.
