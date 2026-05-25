# Lesson 4.4: Security

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 4.3

## Overview

Learn how to secure Surgewave with TLS encryption, authentication, and access control.

## Learning Objectives

- Configure TLS for encryption in transit
- Set up SASL authentication
- Implement ACLs for authorization
- Follow security best practices

## Security Layers

| Layer | Purpose | Technology |
|-------|---------|------------|
| **Transport** | Encrypt data in transit | TLS |
| **Authentication** | Verify client identity | SASL |
| **Authorization** | Control access | ACLs |

## Why Security?

Protect your data and infrastructure:

| Risk | Mitigation |
|------|------------|
| Data interception | TLS encryption |
| Unauthorized access | Authentication |
| Privilege escalation | ACLs |

## Key Concepts

### TLS Configuration

```csharp
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithTls(tls =>
    {
        tls.CertificatePath = "server.pfx";
        tls.CertificatePassword = "password";
    })
    .Build()
    .StartAsync();
```

### SASL Authentication

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.SecurityProtocol = SecurityProtocol.SaslSsl;
    opt.SaslMechanism = SaslMechanism.Plain;
    opt.SaslUsername = "user";
    opt.SaslPassword = "password";
});
```

## Exercise

In this exercise, you'll:

1. Generate certificates
2. Configure TLS
3. Set up authentication

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
