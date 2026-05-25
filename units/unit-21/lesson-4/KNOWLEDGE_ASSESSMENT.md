# Knowledge Assessment: Lesson 4.4 - Security

## Multiple Choice

### 1. What are the three security layers in Surgewave?
- [ ] A) Firewall, VPN, Encryption
- [ ] B) Transport (TLS), Authentication (SASL), Authorization (ACLs)
- [ ] C) Network, Application, Database
- [ ] D) User, Group, Admin

<details>
<summary>Answer</summary>
**B)** Surgewave uses three security layers: Transport (TLS for encrypting data in transit), Authentication (SASL for verifying client identity), and Authorization (ACLs for controlling access to resources).
</details>

### 2. What does TLS protect against?
- [ ] A) Unauthorized access to topics
- [ ] B) Data interception in transit
- [ ] C) Privilege escalation
- [ ] D) Denial of service attacks

<details>
<summary>Answer</summary>
**B)** TLS (Transport Layer Security) encrypts data in transit, protecting against data interception. Without TLS, data sent between clients and brokers could be read by network sniffers.
</details>

### 3. Which security protocol should be used when combining SASL authentication with TLS encryption?
- [ ] A) `SecurityProtocol.Plaintext`
- [ ] B) `SecurityProtocol.Ssl`
- [ ] C) `SecurityProtocol.SaslPlaintext`
- [ ] D) `SecurityProtocol.SaslSsl`

<details>
<summary>Answer</summary>
**D)** `SecurityProtocol.SaslSsl` combines SASL authentication with TLS encryption. This provides both client identity verification (SASL) and encrypted communication (SSL/TLS).
</details>

## True/False

### 4. ACLs (Access Control Lists) are used to verify client identity.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - ACLs are used for authorization (controlling access to resources), not authentication. SASL is used to verify client identity (authentication).
</details>

### 5. Without TLS, data sent between clients and Surgewave brokers can be intercepted.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Without TLS encryption, data is sent in plaintext over the network and can be intercepted by anyone with network access.
</details>

## Code Challenge

### 6. What security features are missing from this producer configuration?

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
});
```

<details>
<summary>Answer</summary>
This configuration has no security features enabled. For a secure setup, you should add:

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";

    // Transport security (TLS)
    opt.SecurityProtocol = SecurityProtocol.SaslSsl;

    // Authentication (SASL)
    opt.SaslMechanism = SaslMechanism.Plain;
    opt.SaslUsername = "user";
    opt.SaslPassword = "password";
});
```

Missing features:
1. **TLS encryption**: Data is sent in plaintext (interceptable)
2. **SASL authentication**: No client identity verification
3. **Without authentication**, ACLs cannot be enforced
</details>

### 7. Match each security risk with its mitigation:

| Risk | Mitigation |
|------|------------|
| Data interception | ??? |
| Unauthorized access | ??? |
| Privilege escalation | ??? |

<details>
<summary>Answer</summary>

| Risk | Mitigation |
|------|------------|
| Data interception | **TLS encryption** - Encrypts data in transit so intercepted packets cannot be read |
| Unauthorized access | **SASL authentication** - Verifies client identity before allowing connections |
| Privilege escalation | **ACLs (Authorization)** - Controls what operations each authenticated user can perform on specific resources |

Security layers work together:
1. TLS encrypts the connection
2. SASL verifies who the client is
3. ACLs determine what that client can do
</details>

## Score
- 7/7: Excellent! You understand security fundamentals
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
