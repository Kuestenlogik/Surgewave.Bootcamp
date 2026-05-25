# Knowledge Assessment: Lesson 14.3 - Kubernetes Fundamentals

## Multiple Choice

### 1. Which Kubernetes resource type should be used for Surgewave brokers that need stable network identities and persistent storage?
- [ ] A) Deployment
- [ ] B) StatefulSet
- [ ] C) DaemonSet
- [ ] D) Job

<details>
<summary>Answer</summary>
**B)** StatefulSet is designed for stateful applications that need stable, unique network identifiers (pod-0, pod-1), ordered deployment/scaling, and stable persistent storage. This is ideal for Surgewave brokers.
</details>

### 2. What is the difference between a liveness probe and a readiness probe?
- [ ] A) They are the same thing
- [ ] B) Liveness checks if the pod should be restarted; readiness checks if it should receive traffic
- [ ] C) Liveness checks network; readiness checks storage
- [ ] D) Readiness is faster than liveness

<details>
<summary>Answer</summary>
**B)** Liveness probes determine if the container is alive (fail = restart the pod). Readiness probes determine if the container is ready to receive traffic (fail = remove from Service endpoints). Both serve different purposes.
</details>

### 3. How does a headless Service (clusterIP: None) differ from a regular ClusterIP Service?
- [ ] A) It doesn't have an IP address
- [ ] B) It returns pod IPs directly instead of load balancing, enabling direct pod addressing
- [ ] C) It only works with Deployments
- [ ] D) It doesn't support DNS

<details>
<summary>Answer</summary>
**B)** A headless Service returns the individual pod IPs directly instead of a single virtual IP. This enables direct addressing of pods (e.g., surgewave-broker-0.surgewave-broker.namespace.svc), which is essential for StatefulSets.
</details>

## True/False

### 4. ConfigMaps and Secrets can be mounted as files or exposed as environment variables in pods.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Both ConfigMaps and Secrets can be consumed either as environment variables (envFrom or individual env entries) or as mounted files (volumeMounts). The choice depends on how the application reads configuration.
</details>

### 5. Secrets in Kubernetes are encrypted by default at rest.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - By default, Secrets are only base64 encoded, not encrypted. Encryption at rest must be explicitly enabled in the Kubernetes API server configuration. Many production clusters and cloud providers enable this, but it's not automatic.
</details>

## Code Challenge

### 6. What issues exist in this Kubernetes deployment?

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: surgewave-broker
spec:
  replicas: 3
  selector:
    matchLabels:
      app: surgewave-broker
  template:
    spec:
      containers:
      - name: broker
        image: surgewave-broker:latest
        ports:
        - containerPort: 9092
```

<details>
<summary>Answer</summary>
Issues:

1. **Deployment for stateful app**: Brokers need StatefulSet for stable identities and storage
2. **No persistent storage**: Broker data will be lost on pod restart
3. **Missing pod labels**: Template needs labels matching selector
4. **No health probes**: No liveness/readiness probes
5. **No resource limits**: Could cause resource contention
6. **Using :latest tag**: Not recommended for production

Fixed version:
```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: surgewave-broker
spec:
  serviceName: surgewave-broker  # Required for StatefulSet
  replicas: 3
  selector:
    matchLabels:
      app: surgewave-broker
  template:
    metadata:
      labels:
        app: surgewave-broker  # Must match selector
    spec:
      containers:
      - name: broker
        image: surgewave-broker:v1.2.0  # Specific version
        ports:
        - containerPort: 9092
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
        volumeMounts:
        - name: data
          mountPath: /data
  volumeClaimTemplates:
  - metadata:
      name: data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 10Gi
```
</details>

### 7. Write a ConfigMap that provides Surgewave configuration and show how to consume it in a pod:

```yaml
# Create ConfigMap and show how to use it in a pod
```

<details>
<summary>Answer</summary>
```yaml
# ConfigMap definition
apiVersion: v1
kind: ConfigMap
metadata:
  name: surgewave-config
  namespace: surgewave
data:
  BOOTSTRAP_SERVERS: "surgewave-broker-0.surgewave-broker.surgewave.svc:9092,surgewave-broker-1.surgewave-broker.surgewave.svc:9092"
  TOPIC: "orders"
  GROUP_ID: "order-processors"
  # File-based config
  appsettings.json: |
    {
      "Surgewave": {
        "BootstrapServers": "surgewave-broker.surgewave.svc:9092",
        "ClientId": "surgewave-client"
      }
    }
---
# Pod using ConfigMap
apiVersion: v1
kind: Pod
metadata:
  name: surgewave-consumer
  namespace: surgewave
spec:
  containers:
  - name: consumer
    image: surgewave-consumer:v1.0.0
    # Option 1: All ConfigMap entries as env vars
    envFrom:
    - configMapRef:
        name: surgewave-config
    # Option 2: Specific entries as env vars
    env:
    - name: Surgewave_BOOTSTRAP_SERVERS
      valueFrom:
        configMapKeyRef:
          name: surgewave-config
          key: BOOTSTRAP_SERVERS
    # Option 3: Mount as files
    volumeMounts:
    - name: config-volume
      mountPath: /app/config
      readOnly: true
  volumes:
  - name: config-volume
    configMap:
      name: surgewave-config
      items:
      - key: appsettings.json
        path: appsettings.json
```

In the application:
```csharp
// Read from environment (Options 1 or 2)
var servers = Environment.GetEnvironmentVariable("BOOTSTRAP_SERVERS");

// Read from mounted file (Option 3)
var config = new ConfigurationBuilder()
    .AddJsonFile("/app/config/appsettings.json")
    .Build();
```
</details>

## Score
- 7/7: Excellent! You understand Kubernetes fundamentals for Surgewave
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
