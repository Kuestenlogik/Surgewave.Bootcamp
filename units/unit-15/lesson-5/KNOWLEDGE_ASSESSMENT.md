# Knowledge Assessment: Lesson 20.5 - Gateway Security

## Multiple Choice

### 1. In what order are security layers typically applied to incoming Gateway requests?
- [ ] A) Auth -> Rate Limit -> CORS -> TLS
- [ ] B) TLS -> CORS -> Auth -> Rate Limit -> ACL
- [ ] C) CORS -> Auth -> TLS -> Rate Limit
- [ ] D) Rate Limit -> Auth -> CORS -> TLS

<details>
<summary>Answer</summary>
**B)** Security layers are applied in order: TLS (transport encryption) -> CORS (cross-origin control) -> Auth (authentication) -> Rate Limit (throttling) -> ACL (authorization).
</details>

### 2. What HTTP status code is returned when rate limiting is triggered?
- [ ] A) 401 Unauthorized
- [ ] B) 403 Forbidden
- [ ] C) 429 Too Many Requests
- [ ] D) 503 Service Unavailable

<details>
<summary>Answer</summary>
**C)** HTTP 429 Too Many Requests is returned when a client exceeds their rate limit. The response includes headers indicating the limit, remaining requests, and reset time.
</details>

### 3. Which HTTP header is used for API key authentication by default in the Gateway?
- [ ] A) Authorization
- [ ] B) X-API-Key
- [ ] C) Api-Token
- [ ] D) X-Auth-Token

<details>
<summary>Answer</summary>
**B)** X-API-Key is the default header name for API key authentication. This can be configured via the HeaderName setting in the ApiKey configuration.
</details>

### 4. What is the purpose of the Access-Control-Max-Age header in CORS?
- [ ] A) To specify how long the connection can remain open
- [ ] B) To specify how long preflight results can be cached
- [ ] C) To set the authentication token expiry
- [ ] D) To limit the maximum request size

<details>
<summary>Answer</summary>
**B)** Access-Control-Max-Age specifies how long (in seconds) browsers can cache the preflight response, reducing the number of preflight OPTIONS requests.
</details>

### 5. What is the token bucket algorithm used for in rate limiting?
- [ ] A) Encrypting authentication tokens
- [ ] B) Controlling request throughput with burst allowance
- [ ] C) Managing WebSocket connections
- [ ] D) Compressing request payloads

<details>
<summary>Answer</summary>
**B)** The token bucket algorithm controls request throughput while allowing short bursts. Tokens are added at a steady rate (RequestsPerSecond), and the bucket can hold up to BurstSize tokens for handling traffic spikes.
</details>

### 6. Which WebSocket authentication method passes the token as a URL parameter?
- [ ] A) Token in first message
- [ ] B) Token in subprotocol
- [ ] C) Token in query string
- [ ] D) Token in WebSocket header

<details>
<summary>Answer</summary>
**C)** Token in query string passes the authentication token as a URL parameter: ws://gateway/ws?token=eyJhbGci... This is one of three supported methods for WebSocket authentication.
</details>

## True/False

### 7. JWT tokens validated by the Gateway must always be issued by a single, preconfigured authority.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The Gateway configuration specifies a single Authority URL for JWT validation. All tokens must be issued by this authority when ValidateIssuer is enabled.
</details>

## Code Challenge

### 8. What's wrong with this CORS configuration?

```json
{
  "Cors": {
    "Enabled": true,
    "AllowedOrigins": ["*"],
    "AllowedMethods": ["GET", "POST"],
    "AllowCredentials": true,
    "MaxAge": 86400
  }
}
```

<details>
<summary>Answer</summary>
The configuration has a security conflict: AllowedOrigins is set to "*" (wildcard) while AllowCredentials is true. This combination is not allowed by the CORS specification because it would allow any website to make credentialed requests:

```json
{
  "Cors": {
    "Enabled": true,
    "AllowedOrigins": [
      "https://myapp.example.com",
      "https://admin.example.com"
    ],  // Must specify explicit origins when using credentials
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],  // Also consider adding all needed methods
    "AllowedHeaders": ["Content-Type", "Authorization", "X-API-Key"],  // Missing! Should specify allowed headers
    "AllowCredentials": true,
    "MaxAge": 86400
  }
}
```

When AllowCredentials is true, you must specify explicit origins rather than using wildcards. Also, AllowedHeaders should be specified to allow Authorization and custom headers.
</details>

## Score
- 8/8: Excellent!
- 6-7/8: Good understanding
- 4-5/8: Review the lesson
- <4/8: Re-do the lesson
