// Surgewave Bootcamp - Unit 12, Lesson 3: Rx Operators
//
// In this lesson, you'll learn:
// - Implement common reactive operators (Where, Select, Take, Buffer)
// - Chain operators in a fluent pipeline
// - Build custom operators for Surgewave messages
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.3 - Rx Operators ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var port = runtime.Port;
Console.WriteLine($"Broker started on port {port}");
Console.WriteLine();

// TODO: Step 1 - Create Subscription and AnonymousObserver helper classes

// TODO: Step 2 - Implement WhereObservable<T>
// - Filter elements based on predicate
// - Forward OnError and OnCompleted unchanged

// TODO: Step 3 - Implement SelectObservable<TSource, TResult>
// - Transform each element using selector function

// TODO: Step 4 - Implement TakeObservable<T>
// - Only emit first N elements
// - Complete after N elements

// TODO: Step 5 - Implement BufferObservable<T>
// - Collect elements into batches
// - Emit batch when count reached
// - Emit remaining on completion

// TODO: Step 6 - Create extension methods for fluent API
// public static class ObservableExtensions
// {
//     public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate)
//     public static IObservable<TResult> Select<TSource, TResult>(...)
//     public static IObservable<T> Take<T>(this IObservable<T> source, int count)
//     public static IObservable<IList<T>> Buffer<T>(this IObservable<T> source, int count)
// }

// Test with simple number sequence
Console.WriteLine("--- Testing Operators with Numbers ---");

// TODO: Create a number sequence and apply operators
// var numbers = Enumerable.Range(1, 20);
// Create an observable from the numbers and test:
// - Where: filter even numbers
// - Select: transform to strings
// - Take: only first 5
// - Buffer: collect in batches of 2

Console.WriteLine();
Console.WriteLine("--- Producing Test Messages ---");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{port}";
});

var products = new[]
{
    ("prod-1", "Laptop|999.99"),
    ("prod-2", "Mouse|29.99"),
    ("prod-3", "Keyboard|79.99"),
    ("prod-4", "Monitor|399.99"),
    ("prod-5", "Headset|149.99"),
    ("prod-6", "Webcam|89.99"),
    ("prod-7", "USB Hub|24.99"),
    ("prod-8", "Mousepad|19.99"),
};

foreach (var (key, value) in products)
{
    await producer.ProduceAsync("products-topic", key, value);
    Console.WriteLine($"  Produced: {key} -> {value}");
}

Console.WriteLine();
Console.WriteLine("--- Applying Operators to Surgewave Messages ---");

// TODO: Step 7 - Create SurgewaveObservable and apply operator pipeline
// Filter products over $50, extract name and price, take first 3

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Operators are composable - each returns IObservable<T>");
Console.WriteLine("2. Where filters, Select transforms, Take limits, Buffer batches");
Console.WriteLine("3. Extension methods enable fluent chaining");
Console.WriteLine("4. Operators propagate errors and completion automatically");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
