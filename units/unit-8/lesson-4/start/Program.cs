// Surgewave Bootcamp - Unit 12, Lesson 4: Backpressure
//
// In this lesson, you'll learn:
// - Handle flow control when producers are faster than consumers
// - Implement buffering strategies with overflow policies
// - Create sampling and throttling operators
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.4 - Backpressure ===");
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

// TODO: Step 1 - Define OverflowPolicy enum
// public enum OverflowPolicy
// {
//     DropOldest, DropNewest, Block, ThrowException
// }

// TODO: Step 2 - Create BoundedBuffer<T> class
// - Queue with capacity limit
// - TryAdd with overflow policy handling
// - TryTake for consumption

// TODO: Step 3 - Create BufferedObservable<T>
// - Wraps source with bounded buffer
// - Consumer task drains buffer
// - Handles overflow according to policy

// TODO: Step 4 - Create SampleObservable<T>
// - Emits only latest value at intervals
// - Drops intermediate values

// TODO: Step 5 - Create ThrottleObservable<T>
// - Passes first value, blocks subsequent for interval
// - Rate limits the stream

// TODO: Step 6 - Add extension methods
// public static class BackpressureExtensions
// {
//     public static IObservable<T> WithBuffer<T>(...);
//     public static IObservable<T> Sample<T>(...);
//     public static IObservable<T> Throttle<T>(...);
// }

Console.WriteLine("--- Demonstrating Backpressure Problem ---");

// TODO: Create a fast producer observable that emits rapidly
// Show what happens when consumer is slow

Console.WriteLine();
Console.WriteLine("--- Testing Sample Operator ---");

// TODO: Test Sample operator
// Only emit latest value every 200ms

Console.WriteLine();
Console.WriteLine("--- Testing Throttle Operator ---");

// TODO: Test Throttle operator
// Rate limit to at most 1 value per 200ms

Console.WriteLine();
Console.WriteLine("--- Testing Bounded Buffer ---");

// TODO: Test bounded buffer with different overflow policies

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Backpressure occurs when producer > consumer speed");
Console.WriteLine("2. Sample takes latest value at intervals (lossy)");
Console.WriteLine("3. Throttle rate limits, passing first value per window");
Console.WriteLine("4. Bounded buffer with policies controls memory usage");
Console.WriteLine("5. Choose strategy based on data loss tolerance");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
