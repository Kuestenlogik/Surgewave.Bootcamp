# Lesson 14.5: Instructions

Follow these step-by-step instructions to deploy Surgewave across cloud providers.

## Step 1: Detect Cloud Environment

Implement cloud environment detection:

```csharp
enum CloudProvider { Local, AWS, Azure, GCP }

CloudProvider DetectCloudProvider()
{
    // Explicit environment variable
    var cloudEnv = Environment.GetEnvironmentVariable("CLOUD_PROVIDER");
    if (!string.IsNullOrEmpty(cloudEnv))
    {
        return cloudEnv.ToLower() switch
        {
            "aws" => CloudProvider.AWS,
            "azure" => CloudProvider.Azure,
            "gcp" => CloudProvider.GCP,
            _ => CloudProvider.Local
        };
    }

    // AWS detection
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_REGION")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV")))
        return CloudProvider.AWS;

    // Azure detection
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT")))
        return CloudProvider.Azure;

    // GCP detection
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GCE_METADATA_HOST")))
        return CloudProvider.GCP;

    return CloudProvider.Local;
}
```

## Step 2: Configure for AWS (EKS + MSK)

```csharp
// AWS MSK configuration
var awsConfig = new
{
    BootstrapServers = Environment.GetEnvironmentVariable("MSK_BOOTSTRAP_SERVERS")
        ?? "b-1.cluster.xxx.kafka.us-east-1.amazonaws.com:9092",
    SecurityProtocol = "SASL_SSL",
    SaslMechanism = "AWS_MSK_IAM",
    Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1"
};

// Kubernetes manifest for AWS
/*
apiVersion: v1
kind: ServiceAccount
metadata:
  name: surgewave-client
  annotations:
    eks.amazonaws.com/role-arn: arn:aws:iam::ACCOUNT:role/SurgewaveClientRole
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: surgewave-client
spec:
  template:
    spec:
      serviceAccountName: surgewave-client
      containers:
      - name: surgewave-client
        env:
        - name: CLOUD_PROVIDER
          value: "aws"
        - name: MSK_BOOTSTRAP_SERVERS
          valueFrom:
            secretKeyRef:
              name: msk-credentials
              key: bootstrap-servers
*/
```

## Step 3: Configure for Azure (AKS + Event Hubs)

```csharp
// Azure Event Hubs configuration
var azureConfig = new
{
    BootstrapServers = Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE")
        + ".servicebus.windows.net:9093",
    SecurityProtocol = "SASL_SSL",
    SaslMechanism = "PLAIN",
    // Connection string format: Endpoint=sb://namespace.servicebus.windows.net/;...
    SaslUsername = "$ConnectionString",
    SaslPassword = Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING")
};

// Kubernetes manifest for Azure
/*
apiVersion: v1
kind: Pod
metadata:
  labels:
    azure.workload.identity/use: "true"
spec:
  serviceAccountName: surgewave-client
  containers:
  - name: surgewave-client
    env:
    - name: CLOUD_PROVIDER
      value: "azure"
    - name: EVENTHUB_NAMESPACE
      value: "my-namespace"
    - name: EVENTHUB_CONNECTION_STRING
      valueFrom:
        secretKeyRef:
          name: eventhub-credentials
          key: connection-string
*/
```

## Step 4: Configure for GCP (GKE + Managed Kafka)

```csharp
// GCP configuration
var gcpConfig = new
{
    BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
        ?? "bootstrap.kafka.region.gcp.project.svc:9092",
    SecurityProtocol = "SASL_SSL",
    SaslMechanism = "PLAIN",
    // Use service account credentials
    SaslUsername = Environment.GetEnvironmentVariable("KAFKA_USERNAME"),
    SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD")
};

// Kubernetes manifest for GCP
/*
apiVersion: v1
kind: ServiceAccount
metadata:
  name: surgewave-client
  annotations:
    iam.gke.io/gcp-service-account: surgewave-client@project.iam.gserviceaccount.com
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      serviceAccountName: surgewave-client
      containers:
      - name: surgewave-client
        env:
        - name: CLOUD_PROVIDER
          value: "gcp"
        - name: KAFKA_BOOTSTRAP_SERVERS
          valueFrom:
            secretKeyRef:
              name: kafka-credentials
              key: bootstrap-servers
*/
```

## Step 5: Implement Cloud-Agnostic Configuration

```csharp
// Unified configuration class
record SurgewaveCloudConfig(
    string BootstrapServers,
    string Topic,
    string? SecurityProtocol = null,
    string? SaslMechanism = null,
    string? SaslUsername = null,
    string? SaslPassword = null
);

SurgewaveCloudConfig GetConfiguration(CloudProvider provider)
{
    return provider switch
    {
        CloudProvider.AWS => new SurgewaveCloudConfig(
            BootstrapServers: Environment.GetEnvironmentVariable("MSK_BOOTSTRAP_SERVERS")
                ?? throw new InvalidOperationException("MSK_BOOTSTRAP_SERVERS required"),
            Topic: Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "aws-topic",
            SecurityProtocol: "SASL_SSL",
            SaslMechanism: "AWS_MSK_IAM"
        ),
        CloudProvider.Azure => new SurgewaveCloudConfig(
            BootstrapServers: $"{Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE")}.servicebus.windows.net:9093",
            Topic: Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "azure-topic",
            SecurityProtocol: "SASL_SSL",
            SaslMechanism: "PLAIN",
            SaslUsername: "$ConnectionString",
            SaslPassword: Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING")
        ),
        CloudProvider.GCP => new SurgewaveCloudConfig(
            BootstrapServers: Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
                ?? throw new InvalidOperationException("KAFKA_BOOTSTRAP_SERVERS required"),
            Topic: Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "gcp-topic",
            SecurityProtocol: "SASL_SSL",
            SaslMechanism: "PLAIN",
            SaslUsername: Environment.GetEnvironmentVariable("KAFKA_USERNAME"),
            SaslPassword: Environment.GetEnvironmentVariable("KAFKA_PASSWORD")
        ),
        _ => new SurgewaveCloudConfig(
            BootstrapServers: Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
                ?? "localhost:9092",
            Topic: Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "local-topic"
        )
    };
}
```

## Step 6: Create Producer with Cloud Config

```csharp
var config = GetConfiguration(provider);

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = config.BootstrapServers;
    // Apply security settings if configured
    // options.SecurityProtocol = config.SecurityProtocol;
    // options.SaslMechanism = config.SaslMechanism;
});

Console.WriteLine($"Connected to {provider} cluster at {config.BootstrapServers}");
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- Automatic cloud provider detection
- Cloud-specific configuration
- Unified configuration model
- Cross-cloud compatibility

## Cloud Provider Quick Reference

### AWS MSK Setup
```bash
# Create MSK cluster
aws kafka create-cluster --cluster-name surgewave-cluster ...

# Get bootstrap servers
aws kafka get-bootstrap-brokers --cluster-arn arn:aws:kafka:...

# Create IAM policy for MSK access
aws iam create-policy --policy-name MSKClientPolicy --policy-document ...
```

### Azure Event Hubs Setup
```bash
# Create Event Hubs namespace
az eventhubs namespace create --name surgewave-ns --resource-group rg ...

# Create event hub (topic)
az eventhubs eventhub create --name orders --namespace-name surgewave-ns ...

# Get connection string
az eventhubs namespace authorization-rule keys list ...
```

### GCP Managed Kafka Setup
```bash
# Create Kafka cluster
gcloud kafka clusters create surgewave-cluster --region=us-central1 ...

# Get bootstrap servers
gcloud kafka clusters describe surgewave-cluster --format='value(bootstrapBrokers)'

# Create service account
gcloud iam service-accounts create surgewave-client ...
```

## Knowledge Check

1. How does AWS IRSA provide credentials to pods?
2. What's the difference between Event Hubs and native Kafka?
3. How does GCP Workload Identity work?
4. Why use Private Link/Private Endpoints for Kafka connections?
5. How do you rotate credentials in each cloud provider?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 14.5 - Cloud Providers ===

Detecting cloud environment...
Detected cloud provider: AWS

AWS Configuration:
  Bootstrap Servers: b-1.cluster.xxx.kafka.us-east-1.amazonaws.com:9092
  Topic: orders
  Security Protocol: SASL_SSL
  SASL Mechanism: AWS_MSK_IAM
  Region: us-east-1

Connecting to AWS MSK cluster...
Connected successfully!

Starting message producer...
[AWS/orders] Sent message 1
[AWS/orders] Sent message 2
...

Lesson complete!
```

---

**Congratulations!** You've completed Unit 23: Cloud & Kubernetes Deployment.

**Next Unit:** [Unit 24: Disaster Recovery](../../unit-24/README.md)
