# Lesson 4.4: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Generate Self-Signed Certificate

For development/testing:

```bash
# Generate self-signed certificate
dotnet dev-certs https --export-path ./server.pfx --password "password"
```

Or programmatically:

```csharp
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

var cert = GenerateSelfSignedCert("localhost");
File.WriteAllBytes("server.pfx", cert.Export(X509ContentType.Pfx, "password"));

X509Certificate2 GenerateSelfSignedCert(string subjectName)
{
    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest(
        $"CN={subjectName}",
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    request.CertificateExtensions.Add(
        new X509BasicConstraintsExtension(false, false, 0, false));

    return request.CreateSelfSigned(
        DateTimeOffset.Now,
        DateTimeOffset.Now.AddYears(1));
}
```

## Step 2: Configure Broker with TLS

```csharp
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithTls(tls =>
    {
        tls.CertificatePath = "server.pfx";
        tls.CertificatePassword = "password";
        tls.RequireClientCertificate = false; // mTLS
    })
    .Build()
    .StartAsync();

Console.WriteLine("Broker started with TLS enabled");
```

## Step 3: Connect Client with TLS

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.SecurityProtocol = SecurityProtocol.Ssl;

    // For self-signed certs in development
    opt.SslCaLocation = "server.pem";
    opt.EnableSslCertificateVerification = false; // Dev only!
});
```

## Step 4: Add SASL Authentication

```csharp
// Broker configuration
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithTls(tls => { /* ... */ })
    .WithSasl(sasl =>
    {
        sasl.Mechanism = SaslMechanism.Plain;
        sasl.AddUser("admin", "admin-secret");
        sasl.AddUser("producer", "producer-secret");
        sasl.AddUser("consumer", "consumer-secret");
    })
    .Build()
    .StartAsync();

// Client configuration
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.SecurityProtocol = SecurityProtocol.SaslSsl;
    opt.SaslMechanism = SaslMechanism.Plain;
    opt.SaslUsername = "producer";
    opt.SaslPassword = "producer-secret";
});
```

## Step 5: Configure ACLs

```csharp
var admin = new SurgewaveAdminClient(/* admin credentials */);

// Allow 'producer' user to write to 'events' topic
await admin.CreateAclAsync(new AclBinding
{
    ResourceType = ResourceType.Topic,
    ResourceName = "events",
    Principal = "User:producer",
    Operation = AclOperation.Write,
    Permission = AclPermission.Allow
});

// Allow 'consumer' user to read from 'events' topic
await admin.CreateAclAsync(new AclBinding
{
    ResourceType = ResourceType.Topic,
    ResourceName = "events",
    Principal = "User:consumer",
    Operation = AclOperation.Read,
    Permission = AclPermission.Allow
});

// Deny all other operations by default
await admin.CreateAclAsync(new AclBinding
{
    ResourceType = ResourceType.Topic,
    ResourceName = "*",
    Principal = "User:*",
    Operation = AclOperation.All,
    Permission = AclPermission.Deny
});
```

## Step 6: Test Security Configuration

```csharp
// This should succeed (producer has write access)
await producer.ProduceAsync("events", "key", "value");

// This should fail (producer doesn't have read access)
try
{
    var consumer = new SurgewaveConsumer<string, string>(producerConfig);
    consumer.Subscribe("events");
    await consumer.ConsumeAsync(timeout);
}
catch (AuthorizationException ex)
{
    Console.WriteLine($"Expected: {ex.Message}");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What's the difference between TLS and SASL?
2. When should you use mTLS?
3. What's the principle of least privilege for ACLs?

---

**Next:** → [Lesson 4.5: Performance Tuning](../lesson-5/README.md)
