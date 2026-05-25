# Knowledge Assessment: Lesson 14.4 - Helm Charts

## Multiple Choice

### 1. What is the purpose of values.yaml in a Helm chart?
- [ ] A) To define the chart version
- [ ] B) To provide default configuration values that templates use
- [ ] C) To list chart dependencies
- [ ] D) To specify the Kubernetes API version

<details>
<summary>Answer</summary>
**B)** values.yaml provides default configuration values that are used in templates via `{{ .Values.* }}` syntax. These defaults can be overridden at install/upgrade time using `--set` or `-f` with custom values files.
</details>

### 2. What is the correct priority order for Helm values (highest to lowest)?
- [ ] A) values.yaml, -f custom.yaml, --set flags
- [ ] B) --set flags, -f custom.yaml, values.yaml
- [ ] C) -f custom.yaml, --set flags, values.yaml
- [ ] D) All have equal priority

<details>
<summary>Answer</summary>
**B)** Values priority from highest to lowest: `--set` flags > `-f` values files > chart's values.yaml. This allows command-line overrides for specific values while using files for environment-specific configurations.
</details>

### 3. What command would you use to see the Kubernetes manifests that Helm would generate without actually deploying them?
- [ ] A) helm install --preview
- [ ] B) helm template
- [ ] C) helm show
- [ ] D) helm generate

<details>
<summary>Answer</summary>
**B)** `helm template` renders the chart templates locally and outputs the resulting Kubernetes manifests. This is useful for debugging templates, reviewing what would be deployed, or generating manifests for GitOps workflows.
</details>

## True/False

### 4. Helm tracks release history, allowing you to rollback to previous versions.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Helm maintains a history of releases. You can use `helm history <release>` to see past versions and `helm rollback <release> <revision>` to revert to a previous version if an upgrade causes issues.
</details>

### 5. The _helpers.tpl file in a Helm chart is deployed to Kubernetes along with other templates.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Files starting with underscore (_) are not rendered as Kubernetes manifests. _helpers.tpl contains reusable template functions (defines) that other templates use, but the file itself is not deployed.
</details>

## Code Challenge

### 6. What issues exist in this Helm template?

```yaml
# templates/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: surgewave-consumer
spec:
  replicas: 3
  selector:
    matchLabels:
      app: surgewave-consumer
  template:
    metadata:
      labels:
        app: surgewave-consumer
    spec:
      containers:
      - name: consumer
        image: surgewave-consumer:latest
        env:
        - name: BOOTSTRAP_SERVERS
          value: localhost:9092
```

<details>
<summary>Answer</summary>
Issues:

1. **Hardcoded values**: Replicas, image tag, bootstrap servers should come from values.yaml
2. **No Helm labels**: Missing standard Helm labels for release management
3. **Hardcoded name**: Should use release name for uniqueness
4. **No resource configuration**: No way to configure resources via values

Fixed version:
```yaml
# templates/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "surgewave.fullname" . }}-consumer
  labels:
    {{- include "surgewave.labels" . | nindent 4 }}
    app.kubernetes.io/component: consumer
spec:
  replicas: {{ .Values.consumer.replicas | default 1 }}
  selector:
    matchLabels:
      {{- include "surgewave.selectorLabels" . | nindent 6 }}
      app.kubernetes.io/component: consumer
  template:
    metadata:
      labels:
        {{- include "surgewave.selectorLabels" . | nindent 8 }}
        app.kubernetes.io/component: consumer
    spec:
      containers:
      - name: consumer
        image: "{{ .Values.consumer.image.repository }}:{{ .Values.consumer.image.tag }}"
        env:
        - name: BOOTSTRAP_SERVERS
          value: {{ .Values.surgewave.bootstrapServers | quote }}
        resources:
          {{- toYaml .Values.consumer.resources | nindent 12 }}
```

With values.yaml:
```yaml
consumer:
  replicas: 3
  image:
    repository: surgewave-consumer
    tag: v1.0.0
  resources:
    requests:
      memory: "256Mi"
      cpu: "100m"
    limits:
      memory: "512Mi"
      cpu: "500m"

surgewave:
  bootstrapServers: "surgewave-broker:9092"
```
</details>

### 7. Create a values.yaml structure for a Surgewave deployment that supports different environments (dev, staging, prod):

<details>
<summary>Answer</summary>
**values.yaml (defaults)**:
```yaml
# Default values for surgewave chart
global:
  environment: dev

broker:
  replicas: 1
  image:
    repository: surgewave-broker
    tag: latest
    pullPolicy: IfNotPresent
  resources:
    requests:
      memory: "256Mi"
      cpu: "100m"
    limits:
      memory: "512Mi"
      cpu: "500m"
  persistence:
    enabled: false
    size: 1Gi
    storageClass: ""

consumer:
  replicas: 1
  image:
    repository: surgewave-consumer
    tag: latest
  resources:
    requests:
      memory: "128Mi"
      cpu: "50m"

producer:
  replicas: 1
  image:
    repository: surgewave-producer
    tag: latest

surgewave:
  bootstrapServers: "{{ .Release.Name }}-broker:9092"
```

**values-dev.yaml**:
```yaml
global:
  environment: dev

broker:
  replicas: 1
  persistence:
    enabled: false

consumer:
  replicas: 1
```

**values-staging.yaml**:
```yaml
global:
  environment: staging

broker:
  replicas: 2
  image:
    tag: v1.2.0
  persistence:
    enabled: true
    size: 10Gi

consumer:
  replicas: 2
  image:
    tag: v1.2.0
```

**values-prod.yaml**:
```yaml
global:
  environment: production

broker:
  replicas: 3
  image:
    tag: v1.2.0
    pullPolicy: Always
  resources:
    requests:
      memory: "2Gi"
      cpu: "1000m"
    limits:
      memory: "4Gi"
      cpu: "2000m"
  persistence:
    enabled: true
    size: 100Gi
    storageClass: fast-ssd

consumer:
  replicas: 5
  image:
    tag: v1.2.0
  resources:
    requests:
      memory: "512Mi"
      cpu: "250m"
```

**Usage**:
```bash
# Development
helm install surgewave ./surgewave-chart -f values-dev.yaml

# Staging
helm install surgewave ./surgewave-chart -f values-staging.yaml

# Production
helm install surgewave ./surgewave-chart -f values-prod.yaml
```
</details>

## Score
- 7/7: Excellent! You understand Helm charts for Surgewave deployments
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
