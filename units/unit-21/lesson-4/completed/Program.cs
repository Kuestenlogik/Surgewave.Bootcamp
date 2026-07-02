// Surgewave Bootcamp - Unit 4, Lesson 4: Security
// COMPLETED SOLUTION
//
// Note: This demo shows the security configuration patterns.
// In production, use real certificates and secure credential management.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
// using Kuestenlogik.Surgewave.Client.Native.Operations.Admin; // TODO: Admin client not yet implemented

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.4 - Security ===");
        Console.WriteLine();

        // In a real scenario, you would:
        // 1. Generate proper certificates
        // 2. Store credentials securely (e.g., vault)
        // 3. Use environment-specific configurations

        Console.WriteLine("--- Security Configuration Demo ---");
        Console.WriteLine();

        // Start broker (without actual TLS for demo simplicity)
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");
        Console.WriteLine();

        // Demonstrate security configuration patterns
        Console.WriteLine("--- TLS Configuration Pattern ---");
        Console.WriteLine(@"
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithTls(tls =>
    {
        tls.CertificatePath = ""server.pfx"";
        tls.CertificatePassword = Environment.GetEnvironmentVariable(""CERT_PASSWORD"");
        tls.RequireClientCertificate = true; // mTLS
    })
    .Build()
    .StartAsync();
");

        Console.WriteLine("--- SASL Configuration Pattern ---");
        Console.WriteLine(@"
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithSasl(sasl =>
    {
        sasl.Mechanism = SaslMechanism.ScramSha256;
        sasl.AddUser(""admin"", GetSecretFromVault(""admin-password""));
        sasl.AddUser(""app-user"", GetSecretFromVault(""app-password""));
    })
    .Build()
    .StartAsync();
");

        Console.WriteLine("--- Client Security Configuration ---");
        Console.WriteLine(@"
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = ""broker:9092"";
    opt.SecurityProtocol = SecurityProtocol.SaslSsl;
    opt.SaslMechanism = SaslMechanism.ScramSha256;
    opt.SaslUsername = ""app-user"";
    opt.SaslPassword = Environment.GetEnvironmentVariable(""Surgewave_PASSWORD"");
    opt.SslCaLocation = ""/etc/ssl/ca.pem"";
});
");

        Console.WriteLine("--- ACL Configuration Pattern ---");
        Console.WriteLine(@"
// Grant write access to producers
await admin.CreateAclAsync(new AclBinding
{
    ResourceType = ResourceType.Topic,
    ResourceName = ""orders"",
    Principal = ""User:order-service"",
    Operation = AclOperation.Write,
    Permission = AclPermission.Allow
});

// Grant read access to consumers
await admin.CreateAclAsync(new AclBinding
{
    ResourceType = ResourceType.Topic,
    ResourceName = ""orders"",
    Principal = ""User:analytics-service"",
    Operation = AclOperation.Read,
    Permission = AclPermission.Allow
});
");

        // Demo: Send messages (without actual security for simplicity)
        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{runtime.Port}");

        Console.WriteLine("--- Sending Test Messages ---");
        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync("secure-topic", $"key-{i}", $"value-{i}");
            Console.WriteLine($"  Sent message {i}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Security Best Practices ---");
        Console.WriteLine("1. Always use TLS in production");
        Console.WriteLine("2. Rotate credentials regularly");
        Console.WriteLine("3. Use least-privilege ACLs");
        Console.WriteLine("4. Store secrets in vault (not config files)");
        Console.WriteLine("5. Enable audit logging");
        Console.WriteLine("6. Use mTLS for service-to-service communication");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
