// Surgewave Bootcamp - Unit 12, Lesson 2: Surgewave as Observable
//
// In this lesson, you'll learn:
// - Convert SurgewaveConsumer to IObservable<T>
// - Handle subscription lifecycle properly
// - Manage cancellation and cleanup
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.2 - Surgewave as Observable ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var port = runtime.Port;
Console.WriteLine($"Broker started on port {port}");
Console.WriteLine();

// TODO: Step 1 - Create Subscription helper class
// public class Subscription : IDisposable { ... }

// TODO: Step 2 - Create CompositeDisposable class
// public class CompositeDisposable : IDisposable { ... }

// TODO: Step 3 - Create SurgewaveObservable<TKey, TValue> class
// - Implement IObservable<ConsumeResult<TKey, TValue>>
// - Constructor takes bootstrapServers, topic, groupId
// - Subscribe method creates consumer and starts consuming loop
// - Return CompositeDisposable for cleanup

// TODO: Step 4 - Create MessageObserver<TKey, TValue> class
// - Implement IObserver<ConsumeResult<TKey, TValue>>
// - Log messages in OnNext
// - Track message count

Console.WriteLine("--- Producing Test Messages ---");
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{port}";
});

for (int i = 1; i <= 10; i++)
{
    await producer.ProduceAsync("reactive-topic", $"key-{i}", $"message-{i}");
    Console.WriteLine($"  Produced: key-{i} -> message-{i}");
}

Console.WriteLine();
Console.WriteLine("--- Subscribing Reactively ---");

// TODO: Step 5 - Create SurgewaveObservable and subscribe
// var observable = new SurgewaveObservable<string, string>(
//     $"localhost:{port}",
//     "reactive-topic",
//     "reactive-group"
// );
// var observer = new MessageObserver<string, string>("Consumer1");
// using var subscription = observable.Subscribe(observer);

// TODO: Step 6 - Wait for messages and then dispose
// await Task.Delay(2000);
// subscription.Dispose();

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. SurgewaveConsumer can be wrapped as IObservable<T>");
Console.WriteLine("2. Each subscription creates its own consumer instance");
Console.WriteLine("3. Disposing subscription cleans up consumer and cancellation");
Console.WriteLine("4. CompositeDisposable manages multiple resources");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
