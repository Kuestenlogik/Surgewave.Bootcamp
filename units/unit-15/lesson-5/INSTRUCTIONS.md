# Lesson 20.5: Instructions

Follow these step-by-step instructions to understand Gateway security.

## Step 1: Understand Authentication Options

Surgewave Gateway supports multiple authentication methods:

```csharp
// API Key Authentication
// Simple, suitable for server-to-server communication
var apiKeyAuth = new
{
    HeaderName = "X-API-Key",
    Keys = new Dictionary<string, object>
    {
        ["sk_live_abc123"] = new
        {
            ClientId = "web-app",
            Roles = new[] { "producer", "consumer" },
            Topics = new[] { "orders", "inventory" }
        }
    }
};

// JWT/OAuth2 Authentication
// Standard, suitable for user-facing applications
var jwtAuth = new
{
    Authority = "https://auth.example.com",
    Audience = "surgewave-gateway",
    ClaimMappings = new
    {
        ClientId = "client_id",
        Roles = "roles",
        Topics = "topics"
    }
};

// Mutual TLS (mTLS)
// Most secure, requires client certificates
var mtlsAuth = new
{
    RequireClientCertificate = true,
    ValidIssuers = new[] { "CN=Surgewave CA" },
    RevocationMode = "Online"
};
```

## Step 2: Implement API Key Validation

```csharp
// Simulated API key validation
bool ValidateApiKey(string apiKey, string operation, string topic)
{
    // In production, look up key in secure storage
    var keys = new Dictionary<string, (string ClientId, string[] Roles, string[] Topics)>
    {
        ["sk_live_abc123"] = ("web-app", new[] { "producer" }, new[] { "orders" }),
        ["sk_live_xyz789"] = ("backend", new[] { "producer", "consumer", "admin" }, new[] { "*" })
    };

    if (!keys.TryGetValue(apiKey, out var keyInfo))
        return false;

    // Check role permission
    var requiredRole = operation switch
    {
        "produce" => "producer",
        "consume" => "consumer",
        "create_topic" => "admin",
        _ => "unknown"
    };

    if (!keyInfo.Roles.Contains(requiredRole) && !keyInfo.Roles.Contains("admin"))
        return false;

    // Check topic permission
    if (!keyInfo.Topics.Contains("*") && !keyInfo.Topics.Contains(topic))
        return false;

    return true;
}
```

## Step 3: Configure Rate Limiting

```csharp
// Rate limiting configuration
var rateLimitConfig = new
{
    Policies = new Dictionary<string, object>
    {
        ["default"] = new
        {
            RequestsPerSecond = 100,
            BurstSize = 200
        },
        ["premium"] = new
        {
            RequestsPerSecond = 1000,
            BurstSize = 2000
        }
    },
    ClientIdHeader = "X-Client-Id"
};

// Token bucket rate limiter (simplified)
class TokenBucket
{
    private readonly int _capacity;
    private readonly double _refillRate;
    private double _tokens;
    private DateTime _lastRefill;

    public TokenBucket(int requestsPerSecond, int burstSize)
    {
        _capacity = burstSize;
        _refillRate = requestsPerSecond;
        _tokens = burstSize;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume()
    {
        Refill();
        if (_tokens >= 1)
        {
            _tokens -= 1;
            return true;
        }
        return false;
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        _tokens = Math.Min(_capacity, _tokens + elapsed * _refillRate);
        _lastRefill = now;
    }
}
```

## Step 4: Configure CORS

```csharp
// CORS configuration
var corsConfig = new
{
    AllowedOrigins = new[]
    {
        "https://myapp.example.com",
        "https://admin.example.com"
    },
    AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" },
    AllowedHeaders = new[] { "Content-Type", "Authorization", "X-API-Key", "X-Client-Id" },
    ExposeHeaders = new[] { "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset" },
    AllowCredentials = true,
    MaxAge = 86400  // 24 hours
};

// CORS validation
bool ValidateCorsOrigin(string origin, string[] allowedOrigins)
{
    foreach (var allowed in allowedOrigins)
    {
        if (allowed == "*")
            return true;

        if (allowed.StartsWith("https://*."))
        {
            var suffix = allowed[10..];  // Remove "https://*."
            if (origin.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        else if (string.Equals(origin, allowed, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }
    return false;
}
```

## Step 5: Secure WebSocket Connections

```csharp
// WebSocket authentication options
var wsSecurityConfig = new
{
    RequireAuthentication = true,
    AuthenticationMethods = new[]
    {
        "QueryString",   // ws://gateway/ws?token=...
        "FirstMessage",  // { "type": "authenticate", "payload": { "token": "..." } }
        "Subprotocol"    // Sec-WebSocket-Protocol: bearer.eyJ...
    },
    SessionTimeout = TimeSpan.FromMinutes(30),
    RequireHeartbeat = true,
    HeartbeatInterval = TimeSpan.FromSeconds(30)
};

// Validate WebSocket connection
async Task<bool> AuthenticateWebSocket(string token)
{
    // Validate JWT token
    // In production, use proper JWT validation library
    try
    {
        // Decode and validate token
        // Check expiration, signature, claims
        return true;
    }
    catch
    {
        return false;
    }
}
```

## Step 6: Security Best Practices

```csharp
// Security checklist configuration
var securityConfig = new
{
    // Always use HTTPS in production
    RequireHttps = true,

    // Enable security headers
    SecurityHeaders = new
    {
        StrictTransportSecurity = "max-age=31536000; includeSubDomains",
        ContentSecurityPolicy = "default-src 'self'",
        XContentTypeOptions = "nosniff",
        XFrameOptions = "DENY"
    },

    // Audit logging
    AuditLogging = new
    {
        Enabled = true,
        LogSuccessfulRequests = false,  // Only log failures by default
        LogAuthenticationFailures = true,
        LogAuthorizationFailures = true,
        LogRateLimitExceeded = true
    },

    // IP restrictions (optional)
    IpRestrictions = new
    {
        Enabled = false,
        AllowedIps = new[] { "10.0.0.0/8", "192.168.0.0/16" },
        DeniedIps = Array.Empty<string>()
    }
};
```

## Complete Solution

See `completed/Program.cs` for demonstration of security concepts.

## Sample Output

```
=== Surgewave Bootcamp: Lesson 20.5 - Gateway Security ===

Surgewave broker started on port 51234

--- Authentication Methods ---

1. API Key Authentication:
   Header: X-API-Key: sk_live_abc123
   Use for: Server-to-server, simple scenarios

2. JWT/OAuth2 Authentication:
   Header: Authorization: Bearer eyJhbGci...
   Use for: User-facing apps, standard OAuth flows

3. Mutual TLS (mTLS):
   Client certificate required
   Use for: High-security, zero-trust environments

--- API Key Validation Demo ---

Validating: sk_live_abc123
  Operation: produce, Topic: orders -> ALLOWED
  Operation: consume, Topic: orders -> DENIED (no consumer role)

Validating: sk_live_xyz789
  Operation: produce, Topic: orders -> ALLOWED
  Operation: consume, Topic: inventory -> ALLOWED
  Operation: create_topic, Topic: any -> ALLOWED (admin role)

--- Rate Limiting Demo ---

Policy: default (100 req/s, burst 200)
  Request 1: ALLOWED (199 tokens remaining)
  Request 2: ALLOWED (198 tokens remaining)
  ...

Policy: premium (1000 req/s, burst 2000)
  Higher limits for premium clients

--- CORS Configuration ---

Allowed Origins:
  - https://myapp.example.com
  - https://admin.example.com
  - https://*.trusted.com (wildcard)

Preflight Response Headers:
  Access-Control-Allow-Origin: https://myapp.example.com
  Access-Control-Allow-Methods: GET, POST, PUT, DELETE
  Access-Control-Allow-Headers: Content-Type, Authorization
  Access-Control-Max-Age: 86400

--- WebSocket Security ---

Authentication Methods:
  1. Query string: ws://gateway/ws?token=eyJ...
  2. First message: { "type": "authenticate", "payload": { "token": "..." } }
  3. Subprotocol: Sec-WebSocket-Protocol: bearer.eyJ...

Session Management:
  - Timeout after 30 minutes of inactivity
  - Heartbeat required every 30 seconds
  - Invalid sessions are terminated

--- Key Takeaways ---
1. Use HTTPS/TLS in production
2. Choose authentication based on use case
3. Rate limiting protects against abuse
4. CORS enables secure browser access
5. Audit logging for security monitoring

Lesson complete!
```

## Knowledge Check

1. When would you use API keys vs JWT tokens?
2. How does token bucket rate limiting work?
3. Why is CORS important for browser-based clients?
4. What are the WebSocket authentication options?
5. What security headers should be enabled?

---

**Unit 15 Complete!** You've learned how to use Surgewave Gateway's REST, WebSocket, and gRPC APIs with proper security configuration.
