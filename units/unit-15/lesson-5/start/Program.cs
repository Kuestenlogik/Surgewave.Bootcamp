// Surgewave Bootcamp - Unit 20, Lesson 5: Gateway Security
//
// In this lesson, you'll learn:
// - Configure authentication (API keys, JWT/OAuth2)
// - Implement rate limiting per client
// - Configure CORS for web applications
// - Secure WebSocket connections
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 20.5 - Gateway Security ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
Console.WriteLine();

// TODO: Step 1 - Display authentication methods
// Console.WriteLine("--- Authentication Methods ---");
// Console.WriteLine();
// Console.WriteLine("1. API Key Authentication:");
// Console.WriteLine("   Header: X-API-Key: sk_live_abc123");
// Console.WriteLine("   Use for: Server-to-server, simple scenarios");
// Console.WriteLine();
// Console.WriteLine("2. JWT/OAuth2 Authentication:");
// Console.WriteLine("   Header: Authorization: Bearer eyJhbGci...");
// Console.WriteLine("   Use for: User-facing apps, standard OAuth flows");
// Console.WriteLine();
// Console.WriteLine("3. Mutual TLS (mTLS):");
// Console.WriteLine("   Client certificate required");
// Console.WriteLine("   Use for: High-security, zero-trust environments");

// TODO: Step 2 - Simulate API key validation
// Console.WriteLine();
// Console.WriteLine("--- API Key Validation Demo ---");
// Console.WriteLine();
//
// // Simulated API keys database
// var apiKeys = new Dictionary<string, (string ClientId, string[] Roles, string[] Topics)>
// {
//     ["sk_live_abc123"] = ("web-app", new[] { "producer" }, new[] { "orders" }),
//     ["sk_live_xyz789"] = ("backend", new[] { "producer", "consumer", "admin" }, new[] { "*" })
// };
//
// bool ValidateApiKey(string apiKey, string operation, string topic)
// {
//     if (!apiKeys.TryGetValue(apiKey, out var keyInfo))
//         return false;
//
//     var requiredRole = operation switch
//     {
//         "produce" => "producer",
//         "consume" => "consumer",
//         "create_topic" => "admin",
//         _ => "unknown"
//     };
//
//     if (!keyInfo.Roles.Contains(requiredRole) && !keyInfo.Roles.Contains("admin"))
//         return false;
//
//     if (!keyInfo.Topics.Contains("*") && !keyInfo.Topics.Contains(topic))
//         return false;
//
//     return true;
// }
//
// Console.WriteLine("Validating: sk_live_abc123");
// Console.WriteLine($"  Operation: produce, Topic: orders -> {(ValidateApiKey("sk_live_abc123", "produce", "orders") ? "ALLOWED" : "DENIED")}");
// Console.WriteLine($"  Operation: consume, Topic: orders -> {(ValidateApiKey("sk_live_abc123", "consume", "orders") ? "ALLOWED" : "DENIED (no consumer role)")}");
// Console.WriteLine();
// Console.WriteLine("Validating: sk_live_xyz789");
// Console.WriteLine($"  Operation: produce, Topic: orders -> {(ValidateApiKey("sk_live_xyz789", "produce", "orders") ? "ALLOWED" : "DENIED")}");
// Console.WriteLine($"  Operation: consume, Topic: inventory -> {(ValidateApiKey("sk_live_xyz789", "consume", "inventory") ? "ALLOWED" : "DENIED")}");
// Console.WriteLine($"  Operation: create_topic, Topic: any -> {(ValidateApiKey("sk_live_xyz789", "create_topic", "any") ? "ALLOWED (admin role)" : "DENIED")}");

// TODO: Step 3 - Demonstrate rate limiting
// Console.WriteLine();
// Console.WriteLine("--- Rate Limiting Demo ---");
// Console.WriteLine();
// Console.WriteLine("Policy: default (100 req/s, burst 200)");
// Console.WriteLine("  Request 1: ALLOWED (199 tokens remaining)");
// Console.WriteLine("  Request 2: ALLOWED (198 tokens remaining)");
// Console.WriteLine("  ...");
// Console.WriteLine();
// Console.WriteLine("Policy: premium (1000 req/s, burst 2000)");
// Console.WriteLine("  Higher limits for premium clients");

// TODO: Step 4 - Display CORS configuration
// Console.WriteLine();
// Console.WriteLine("--- CORS Configuration ---");
// Console.WriteLine();
// Console.WriteLine("Allowed Origins:");
// Console.WriteLine("  - https://myapp.example.com");
// Console.WriteLine("  - https://admin.example.com");
// Console.WriteLine("  - https://*.trusted.com (wildcard)");
// Console.WriteLine();
// Console.WriteLine("Preflight Response Headers:");
// Console.WriteLine("  Access-Control-Allow-Origin: https://myapp.example.com");
// Console.WriteLine("  Access-Control-Allow-Methods: GET, POST, PUT, DELETE");
// Console.WriteLine("  Access-Control-Allow-Headers: Content-Type, Authorization");
// Console.WriteLine("  Access-Control-Max-Age: 86400");

// TODO: Step 5 - Display WebSocket security
// Console.WriteLine();
// Console.WriteLine("--- WebSocket Security ---");
// Console.WriteLine();
// Console.WriteLine("Authentication Methods:");
// Console.WriteLine("  1. Query string: ws://gateway/ws?token=eyJ...");
// Console.WriteLine("  2. First message: { \"type\": \"authenticate\", \"payload\": { \"token\": \"...\" } }");
// Console.WriteLine("  3. Subprotocol: Sec-WebSocket-Protocol: bearer.eyJ...");
// Console.WriteLine();
// Console.WriteLine("Session Management:");
// Console.WriteLine("  - Timeout after 30 minutes of inactivity");
// Console.WriteLine("  - Heartbeat required every 30 seconds");
// Console.WriteLine("  - Invalid sessions are terminated");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use HTTPS/TLS in production");
Console.WriteLine("2. Choose authentication based on use case");
Console.WriteLine("3. Rate limiting protects against abuse");
Console.WriteLine("4. CORS enables secure browser access");
Console.WriteLine("5. Audit logging for security monitoring");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
