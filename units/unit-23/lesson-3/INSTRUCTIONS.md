# Lesson 14.3: Instructions

Follow these step-by-step instructions to deploy Surgewave on Kubernetes.

## Step 1: Configure for Kubernetes Environment

In Kubernetes, configuration comes from ConfigMaps and Secrets mounted as environment variables:

```csharp
// Configuration from ConfigMap
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
    ?? "surgewave-broker-headless:9092";
var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
    ?? "k8s-topic";

// Pod identity information
var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "unknown";
var podNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "default";
```

## Step 2: Create the Kubernetes Manifests

### ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: surgewave-config
data:
  Surgewave_BOOTSTRAP_SERVERS: "surgewave-broker-headless.surgewave.svc.cluster.local:9092"
  Surgewave_TOPIC: "orders"
  Surgewave_MAX_RETRIES: "30"
  Surgewave_RETRY_DELAY_MS: "2000"
```

### Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: surgewave-credentials
type: Opaque
stringData:
  Surgewave_USERNAME: "admin"
  Surgewave_PASSWORD: "secret-password"
```

### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: surgewave-producer
  labels:
    app: surgewave-producer
spec:
  replicas: 3
  selector:
    matchLabels:
      app: surgewave-producer
  template:
    metadata:
      labels:
        app: surgewave-producer
    spec:
      containers:
      - name: surgewave-producer
        image: myregistry/surgewave-producer:v1
        envFrom:
        - configMapRef:
            name: surgewave-config
        - secretRef:
            name: surgewave-credentials
        env:
        - name: POD_NAME
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        - name: POD_NAMESPACE
          valueFrom:
            fieldRef:
              fieldPath: metadata.namespace
        ports:
        - containerPort: 8080
          name: health
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 3
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "500m"
```

## Step 3: Implement Health Endpoints

Create a simple HTTP health server for Kubernetes probes:

```csharp
// Simple health endpoint server
var healthServer = new HealthServer(8080);
await healthServer.StartAsync();

// Update readiness when broker connection is established
healthServer.SetReady(true);
```

## Step 4: Handle Pod Lifecycle

Kubernetes sends SIGTERM before killing pods. Handle graceful shutdown:

```csharp
var cts = new CancellationTokenSource();

// Handle SIGTERM from Kubernetes
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Console.WriteLine($"[{podName}] Received SIGTERM, initiating graceful shutdown...");
    cts.Cancel();
    // Give time for in-flight messages
    Thread.Sleep(5000);
};
```

## Step 5: Create StatefulSet for Brokers

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: surgewave-broker
spec:
  serviceName: surgewave-broker-headless
  replicas: 3
  selector:
    matchLabels:
      app: surgewave-broker
  template:
    metadata:
      labels:
        app: surgewave-broker
    spec:
      containers:
      - name: surgewave-broker
        image: myregistry/surgewave-broker:v1
        ports:
        - containerPort: 9092
          name: broker
        - containerPort: 8080
          name: health
        env:
        - name: Surgewave_BROKER_ID
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        volumeMounts:
        - name: data
          mountPath: /var/surgewave/data
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
  volumeClaimTemplates:
  - metadata:
      name: data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 10Gi
```

## Step 6: Create Headless Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: surgewave-broker-headless
spec:
  clusterIP: None
  selector:
    app: surgewave-broker
  ports:
  - port: 9092
    name: broker
```

## Step 7: Deploy and Test

```bash
# Apply all manifests
kubectl apply -f configmap.yaml
kubectl apply -f secret.yaml
kubectl apply -f statefulset.yaml
kubectl apply -f service.yaml
kubectl apply -f deployment.yaml

# Check pod status
kubectl get pods -l app=surgewave-producer

# View logs
kubectl logs -f deployment/surgewave-producer

# Check health endpoints
kubectl port-forward deployment/surgewave-producer 8080:8080
curl http://localhost:8080/health/live
curl http://localhost:8080/health/ready

# Scale deployment
kubectl scale deployment surgewave-producer --replicas=5
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- Kubernetes-aware configuration
- Health probe endpoints
- Pod identity awareness
- Graceful shutdown handling

## Knowledge Check

1. When would you use StatefulSet vs Deployment?
2. What's the difference between a headless service and a ClusterIP service?
3. Why do you need both liveness and readiness probes?
4. How do ConfigMaps differ from Secrets?
5. What happens when a readiness probe fails?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 14.3 - Kubernetes Fundamentals ===

Pod Information:
  Name: surgewave-producer-7d9f8b6c4d-x2k4j
  Namespace: surgewave

Configuration (from ConfigMap):
  Bootstrap Servers: surgewave-broker-headless.surgewave.svc.cluster.local:9092
  Topic: orders

Health server started on port 8080
  Liveness: /health/live
  Readiness: /health/ready

Connecting to brokers...
Connected! Setting readiness to true.

Starting message producer...
[surgewave-producer-7d9f8b6c4d-x2k4j] Sent message 1 to orders
[surgewave-producer-7d9f8b6c4d-x2k4j] Sent message 2 to orders
...
Received SIGTERM, initiating graceful shutdown...
Flushing pending messages...
Shutdown complete.

Lesson complete!
```

---

**Next:** [Lesson 14.4: Helm Charts](../lesson-4/README.md)
