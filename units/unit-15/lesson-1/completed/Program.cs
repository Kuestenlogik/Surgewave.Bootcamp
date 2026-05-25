// Surgewave Bootcamp - Unit 20, Lesson 1: Gateway Architecture
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 20.1 - Gateway Architecture ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
Console.WriteLine();

// Step 1 - Display Gateway access methods
Console.WriteLine("--- Gateway Architecture Overview ---");
Console.WriteLine();
Console.WriteLine("Gateway provides three access methods:");
Console.WriteLine("  1. REST API (JSON over HTTP)");
Console.WriteLine("     - Easy to use from any language");
Console.WriteLine("     - Human-readable for debugging");
Console.WriteLine("     - Best for: Web clients, scripts, simple integrations");
Console.WriteLine();
Console.WriteLine("  2. gRPC API (Protobuf over HTTP/2)");
Console.WriteLine("     - High performance, strongly typed");
Console.WriteLine("     - Bidirectional streaming support");
Console.WriteLine("     - Best for: Microservices, high-throughput clients");
Console.WriteLine();
Console.WriteLine("  3. WebSocket (JSON over WebSocket)");
Console.WriteLine("     - Real-time bidirectional communication");
Console.WriteLine("     - Persistent connections");
Console.WriteLine("     - Best for: Live dashboards, real-time apps");

// Step 2 - Display REST API endpoints (Confluent v3 compatible)
Console.WriteLine();
Console.WriteLine("--- REST API Endpoints (Confluent v3 Compatible) ---");
Console.WriteLine();
Console.WriteLine("Topic Management:");
Console.WriteLine("  GET    /v3/topics               - List topics");
Console.WriteLine("  POST   /v3/topics               - Create topic");
Console.WriteLine("  GET    /v3/topics/{topic}       - Describe topic");
Console.WriteLine("  DELETE /v3/topics/{topic}       - Delete topic");
Console.WriteLine();
Console.WriteLine("Producing:");
Console.WriteLine("  POST   /v3/topics/{topic}/records - Produce message");
Console.WriteLine();
Console.WriteLine("Consuming:");
Console.WriteLine("  POST   /v3/consumers/fetch      - Fetch messages");
Console.WriteLine("  POST   /v3/consumer-groups/{g}/offsets - Commit offsets");
Console.WriteLine("  GET    /v3/consumer-groups/{g}/offsets - Get offsets");

// Step 3 - Display multi-cluster routing examples
Console.WriteLine();
Console.WriteLine("--- Multi-Cluster Support ---");
Console.WriteLine();
Console.WriteLine("Single cluster (default):");
Console.WriteLine("  POST /v3/topics/orders/records");
Console.WriteLine();
Console.WriteLine("Multi-cluster (explicit):");
Console.WriteLine("  POST /v3/clusters/production/topics/orders/records");
Console.WriteLine("  POST /v3/clusters/staging/topics/orders/records");

// Step 4 - Display gRPC services
Console.WriteLine();
Console.WriteLine("--- gRPC Services ---");
Console.WriteLine();
var services = new[]
{
    ("ProducerService", "Produce, ProduceBatch, ProduceStream"),
    ("ConsumerService", "Consume, Fetch, Commit, ListOffsets"),
    ("TopicService", "Create, List, Describe, Delete topics"),
    ("ClusterService", "GetCluster, ListClusters"),
    ("ConsumerGroupService", "Describe, List, Delete groups"),
    ("TransactionService", "InitProducer, EndTransaction")
};

foreach (var (service, operations) in services)
{
    Console.WriteLine($"  {service}");
    Console.WriteLine($"    Operations: {operations}");
}

// Step 5 - Display WebSocket routing
Console.WriteLine();
Console.WriteLine("--- WebSocket Routing ---");
Console.WriteLine();
Console.WriteLine("Default cluster:");
Console.WriteLine("  ws://gateway:8080/ws");
Console.WriteLine();
Console.WriteLine("Specific cluster:");
Console.WriteLine("  ws://gateway:8080/ws/clusters/production");
Console.WriteLine("  ws://gateway:8080/ws/clusters/staging");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Gateway bridges HTTP/gRPC to Surgewave native protocol");
Console.WriteLine("2. REST API is Confluent v3 compatible");
Console.WriteLine("3. gRPC provides streaming and high performance");
Console.WriteLine("4. WebSocket enables real-time applications");
Console.WriteLine("5. Multi-cluster routing from single Gateway");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
