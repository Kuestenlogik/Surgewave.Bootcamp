# Lesson 14.4: Instructions

Follow these step-by-step instructions to create and use Helm charts for Surgewave deployment.

## Step 1: Understand Helm Values

Helm charts use values.yaml for configuration. Your app reads these via environment variables:

```csharp
// These values come from Helm's values.yaml -> ConfigMap -> Environment
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
    ?? "surgewave-broker:9092";
var releaseName = Environment.GetEnvironmentVariable("HELM_RELEASE_NAME")
    ?? "surgewave";
var releaseNamespace = Environment.GetEnvironmentVariable("HELM_RELEASE_NAMESPACE")
    ?? "default";
```

## Step 2: Create Chart Structure

```bash
# Create chart scaffold
helm create surgewave-chart

# Structure:
# surgewave-chart/
# ├── Chart.yaml
# ├── values.yaml
# ├── templates/
# │   ├── deployment.yaml
# │   ├── service.yaml
# │   ├── configmap.yaml
# │   └── _helpers.tpl
```

## Step 3: Define values.yaml

```yaml
# values.yaml
replicaCount: 1

image:
  repository: myregistry/surgewave-client
  tag: "latest"
  pullPolicy: IfNotPresent

surgewave:
  bootstrapServers: "surgewave-broker:9092"
  topic: "helm-topic"
  groupId: "helm-consumers"
  maxRetries: 30
  retryDelayMs: 2000

resources:
  limits:
    cpu: 500m
    memory: 256Mi
  requests:
    cpu: 100m
    memory: 128Mi

healthCheck:
  port: 8080
  livenessPath: /health/live
  readinessPath: /health/ready

nodeSelector: {}
tolerations: []
affinity: {}
```

## Step 4: Create ConfigMap Template

```yaml
# templates/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: {{ include "surgewave.fullname" . }}-config
  labels:
    {{- include "surgewave.labels" . | nindent 4 }}
data:
  Surgewave_BOOTSTRAP_SERVERS: {{ .Values.surgewave.bootstrapServers | quote }}
  Surgewave_TOPIC: {{ .Values.surgewave.topic | quote }}
  Surgewave_GROUP_ID: {{ .Values.surgewave.groupId | quote }}
  Surgewave_MAX_RETRIES: {{ .Values.surgewave.maxRetries | quote }}
  Surgewave_RETRY_DELAY_MS: {{ .Values.surgewave.retryDelayMs | quote }}
  HELM_RELEASE_NAME: {{ .Release.Name | quote }}
  HELM_RELEASE_NAMESPACE: {{ .Release.Namespace | quote }}
```

## Step 5: Create Deployment Template

```yaml
# templates/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "surgewave.fullname" . }}
  labels:
    {{- include "surgewave.labels" . | nindent 4 }}
spec:
  replicas: {{ .Values.replicaCount }}
  selector:
    matchLabels:
      {{- include "surgewave.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      labels:
        {{- include "surgewave.selectorLabels" . | nindent 8 }}
      annotations:
        checksum/config: {{ include (print $.Template.BasePath "/configmap.yaml") . | sha256sum }}
    spec:
      containers:
        - name: {{ .Chart.Name }}
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
          imagePullPolicy: {{ .Values.image.pullPolicy }}
          envFrom:
            - configMapRef:
                name: {{ include "surgewave.fullname" . }}-config
          env:
            - name: POD_NAME
              valueFrom:
                fieldRef:
                  fieldPath: metadata.name
          ports:
            - name: health
              containerPort: {{ .Values.healthCheck.port }}
              protocol: TCP
          livenessProbe:
            httpGet:
              path: {{ .Values.healthCheck.livenessPath }}
              port: health
            initialDelaySeconds: 10
            periodSeconds: 5
          readinessProbe:
            httpGet:
              path: {{ .Values.healthCheck.readinessPath }}
              port: health
            initialDelaySeconds: 5
            periodSeconds: 3
          resources:
            {{- toYaml .Values.resources | nindent 12 }}
```

## Step 6: Create Helper Templates

```yaml
# templates/_helpers.tpl
{{/*
Expand the name of the chart.
*/}}
{{- define "surgewave.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "surgewave.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "surgewave.labels" -}}
helm.sh/chart: {{ include "surgewave.chart" . }}
{{ include "surgewave.selectorLabels" . }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "surgewave.selectorLabels" -}}
app.kubernetes.io/name: {{ include "surgewave.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Chart label
*/}}
{{- define "surgewave.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}
```

## Step 7: Deploy with Helm

```bash
# Install with default values
helm install my-surgewave ./surgewave-chart

# Install with custom values
helm install my-surgewave ./surgewave-chart -f production-values.yaml

# Install with overrides
helm install my-surgewave ./surgewave-chart \
  --set replicaCount=5 \
  --set surgewave.topic=production-orders

# Upgrade existing release
helm upgrade my-surgewave ./surgewave-chart --set replicaCount=10

# Rollback to previous version
helm rollback my-surgewave 1

# View release history
helm history my-surgewave

# Uninstall
helm uninstall my-surgewave
```

## Step 8: Create Environment-Specific Values

```yaml
# values-production.yaml
replicaCount: 5

surgewave:
  bootstrapServers: "surgewave-broker-prod:9092"
  topic: "production-orders"

resources:
  limits:
    cpu: 2000m
    memory: 2Gi
  requests:
    cpu: 500m
    memory: 512Mi
```

```bash
# Deploy to production
helm install surgewave-prod ./surgewave-chart -f values-production.yaml
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- Helm release awareness
- Configuration checksum verification
- Proper labeling support
- Release metadata logging

## Knowledge Check

1. What's the difference between Chart.version and appVersion?
2. How does the checksum annotation trigger pod recreation?
3. When would you use --set vs -f values.yaml?
4. What's the purpose of _helpers.tpl?
5. How do you manage secrets in Helm charts?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 14.4 - Helm Charts ===

Helm Release Information:
  Release Name: my-surgewave
  Release Namespace: surgewave-production
  Pod Name: my-surgewave-client-7d9f8b6c4d-x2k4j

Configuration (from Helm values):
  Bootstrap Servers: surgewave-broker:9092
  Topic: helm-topic
  Group ID: helm-consumers

Starting message producer...
[my-surgewave/helm-topic] Sent message 1
[my-surgewave/helm-topic] Sent message 2
...

Lesson complete!
```

---

**Next:** [Lesson 14.5: Cloud Providers](../lesson-5/README.md)
