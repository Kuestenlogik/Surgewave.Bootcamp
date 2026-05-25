# Knowledge Assessment: Lesson 14.5 - Cloud Providers

## Multiple Choice

### 1. Which authentication mechanism is recommended for Surgewave applications running on AWS EKS connecting to MSK?
- [ ] A) Username and password
- [ ] B) IAM authentication with IRSA (IAM Roles for Service Accounts)
- [ ] C) API keys
- [ ] D) OAuth tokens

<details>
<summary>Answer</summary>
**B)** IAM authentication with IRSA (IAM Roles for Service Accounts) is the recommended approach on AWS. It allows Kubernetes pods to assume IAM roles without managing credentials, providing secure, automatic credential rotation.
</details>

### 2. Azure Event Hubs provides Kafka API compatibility. What is the typical security protocol used when connecting?
- [ ] A) PLAINTEXT
- [ ] B) SSL only
- [ ] C) SASL_SSL with PLAIN mechanism
- [ ] D) Kerberos

<details>
<summary>Answer</summary>
**C)** Azure Event Hubs with Kafka API uses SASL_SSL with the PLAIN mechanism. The username is typically `$ConnectionString` and the password is the Event Hubs connection string.
</details>

### 3. What is GCP Workload Identity used for in Kubernetes deployments?
- [ ] A) Container image scanning
- [ ] B) Mapping Kubernetes service accounts to GCP service accounts for authentication
- [ ] C) Load balancing
- [ ] D) Log aggregation

<details>
<summary>Answer</summary>
**B)** GCP Workload Identity enables Kubernetes service accounts to act as GCP service accounts. This allows pods to access GCP resources (like Managed Kafka, Secret Manager) using IAM permissions without managing keys.
</details>

## True/False

### 4. All major cloud providers (AWS, Azure, GCP) offer managed Kafka or Kafka-compatible services.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - AWS offers MSK (Managed Streaming for Kafka), Azure offers Event Hubs with Kafka API compatibility, and GCP offers Managed Service for Apache Kafka. All provide Kafka-compatible interfaces.
</details>

### 5. When deploying Surgewave on multiple cloud providers, you should use cloud-specific code for each provider.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Best practice is to use cloud-agnostic application code with configuration-driven cloud-specific settings. The same codebase should work across providers by reading bootstrap servers, authentication settings, and other configuration from environment variables or secrets.
</details>

## Code Challenge

### 6. What issues exist in this cloud-agnostic configuration approach?

```csharp
public class SurgewaveClientFactory
{
    public SurgewaveProducer<string, string> CreateProducer()
    {
        if (Environment.GetEnvironmentVariable("AWS_REGION") != null)
        {
            return CreateAwsProducer();
        }
        else if (Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") != null)
        {
            return CreateAzureProducer();
        }
        else
        {
            return CreateLocalProducer();
        }
    }
}
```

<details>
<summary>Answer</summary>
Issues:

1. **Fragile detection**: Environment variables may exist for other reasons
2. **Hardcoded conditions**: Adding a new cloud requires code changes
3. **Hidden dependencies**: No clear documentation of required configuration
4. **Tight coupling**: Provider-specific logic mixed with factory logic
5. **Not configuration-driven**: Should use explicit configuration

Better approach:
```csharp
public class SurgewaveClientFactory
{
    private readonly SurgewaveConfiguration _config;

    public SurgewaveClientFactory(IOptions<SurgewaveConfiguration> config)
    {
        _config = config.Value;
    }

    public SurgewaveProducer<string, string> CreateProducer()
    {
        return new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = _config.BootstrapServers;

            // Apply security configuration based on config, not detection
            if (!string.IsNullOrEmpty(_config.SecurityProtocol))
            {
                options.SecurityProtocol = _config.SecurityProtocol;
                options.SaslMechanism = _config.SaslMechanism;
                options.SaslUsername = _config.SaslUsername;
                options.SaslPassword = _config.SaslPassword;
            }
        });
    }
}

// Configuration class
public class SurgewaveConfiguration
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string? SecurityProtocol { get; set; }
    public string? SaslMechanism { get; set; }
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
}
```

Configuration per environment:
```yaml
# AWS (via ConfigMap/Secret)
Surgewave__BootstrapServers: "b-1.mycluster.kafka.us-east-1.amazonaws.com:9098"
Surgewave__SecurityProtocol: "SASL_SSL"
Surgewave__SaslMechanism: "AWS_MSK_IAM"

# Azure (via ConfigMap/Secret)
Surgewave__BootstrapServers: "mynamespace.servicebus.windows.net:9093"
Surgewave__SecurityProtocol: "SASL_SSL"
Surgewave__SaslMechanism: "PLAIN"
Surgewave__SaslUsername: "$ConnectionString"
Surgewave__SaslPassword: "Endpoint=sb://..."

# Local (defaults work)
```
</details>

### 7. Write a configuration class and factory that supports all three major cloud providers (AWS, Azure, GCP) as well as local development:

<details>
<summary>Answer</summary>
```csharp
// Configuration classes
public class SurgewaveCloudConfiguration
{
    public string Provider { get; set; } = "local"; // local, aws, azure, gcp
    public string BootstrapServers { get; set; } = "localhost:9092";
    public SecurityConfiguration Security { get; set; } = new();
}

public class SecurityConfiguration
{
    public string? Protocol { get; set; }         // SASL_SSL, SSL, PLAINTEXT
    public string? Mechanism { get; set; }        // PLAIN, AWS_MSK_IAM, SCRAM-SHA-256
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? CertificatePath { get; set; }
}

// Factory
public class CloudAgnosticSurgewaveFactory
{
    private readonly SurgewaveCloudConfiguration _config;
    private readonly ILogger<CloudAgnosticSurgewaveFactory> _logger;

    public CloudAgnosticSurgewaveFactory(
        IOptions<SurgewaveCloudConfiguration> config,
        ILogger<CloudAgnosticSurgewaveFactory> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public SurgewaveProducer<TKey, TValue> CreateProducer<TKey, TValue>()
    {
        _logger.LogInformation(
            "Creating producer for {Provider} at {Servers}",
            _config.Provider,
            _config.BootstrapServers);

        return new SurgewaveProducer<TKey, TValue>(ConfigureOptions);
    }

    public SurgewaveConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(string groupId)
    {
        return new SurgewaveConsumer<TKey, TValue>(options =>
        {
            ConfigureOptions(options);
            options.GroupId = groupId;
        });
    }

    private void ConfigureOptions(SurgewaveClientOptions options)
    {
        options.BootstrapServers = _config.BootstrapServers;

        if (_config.Security.Protocol != null)
        {
            options.SecurityProtocol = _config.Security.Protocol;
        }

        switch (_config.Provider.ToLower())
        {
            case "aws":
                ConfigureAws(options);
                break;
            case "azure":
                ConfigureAzure(options);
                break;
            case "gcp":
                ConfigureGcp(options);
                break;
            case "local":
            default:
                // No special configuration needed
                break;
        }
    }

    private void ConfigureAws(SurgewaveClientOptions options)
    {
        // AWS MSK with IAM auth
        options.SecurityProtocol = "SASL_SSL";
        options.SaslMechanism = "AWS_MSK_IAM";
        // IAM credentials are automatically picked up via IRSA
    }

    private void ConfigureAzure(SurgewaveClientOptions options)
    {
        // Azure Event Hubs
        options.SecurityProtocol = "SASL_SSL";
        options.SaslMechanism = "PLAIN";
        options.SaslUsername = _config.Security.Username ?? "$ConnectionString";
        options.SaslPassword = _config.Security.Password;
    }

    private void ConfigureGcp(SurgewaveClientOptions options)
    {
        // GCP Managed Kafka
        options.SecurityProtocol = _config.Security.Protocol ?? "SASL_SSL";
        options.SaslMechanism = _config.Security.Mechanism ?? "PLAIN";
        options.SaslUsername = _config.Security.Username;
        options.SaslPassword = _config.Security.Password;
    }
}

// Registration
services.Configure<SurgewaveCloudConfiguration>(
    configuration.GetSection("Surgewave"));
services.AddSingleton<CloudAgnosticSurgewaveFactory>();
```

**appsettings.json for different environments**:

```json
// Local development
{
  "Surgewave": {
    "Provider": "local",
    "BootstrapServers": "localhost:9092"
  }
}

// AWS (from ConfigMap/Secret)
{
  "Surgewave": {
    "Provider": "aws",
    "BootstrapServers": "b-1.cluster.kafka.us-east-1.amazonaws.com:9098"
  }
}

// Azure (from ConfigMap/Secret)
{
  "Surgewave": {
    "Provider": "azure",
    "BootstrapServers": "mynamespace.servicebus.windows.net:9093",
    "Security": {
      "Password": "Endpoint=sb://mynamespace.servicebus.windows.net/..."
    }
  }
}
```
</details>

## Score
- 7/7: Excellent! You understand cloud provider deployment patterns
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
