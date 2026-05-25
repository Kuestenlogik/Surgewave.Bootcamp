// Surgewave Bootcamp - Unit 12, Lesson 1: Reactive Basics
//
// In this lesson, you'll learn:
// - Implement IObservable<T> and IObserver<T>
// - Create subscriptions with proper cleanup
// - Use Subject<T> for push-based patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.1 - Reactive Basics ===");
Console.WriteLine();

// TODO: Step 1 - Create a ConsoleObserver class that implements IObserver<T>
// Hint: Implement OnNext, OnError, and OnCompleted methods

// TODO: Step 2 - Create a simple EmptyDisposable class
// public class EmptyDisposable : IDisposable
// {
//     public void Dispose() { }
// }

// TODO: Step 3 - Create a NumberSequence class that implements IObservable<int>
// - Constructor takes start and count
// - Subscribe method emits values synchronously
// - Call OnCompleted when done

// TODO: Step 4 - Create a Subscription class that supports cancellation
// public class Subscription : IDisposable
// {
//     private readonly Action _onDispose;
//     public Subscription(Action onDispose) => _onDispose = onDispose;
//     public void Dispose() => _onDispose?.Invoke();
// }

// TODO: Step 5 - Create an AsyncNumberSequence that emits values with delays
// - Use CancellationTokenSource for cleanup
// - Run emission in Task.Run
// - Handle cancellation gracefully

// TODO: Step 6 - Create a Subject<T> that is both IObservable<T> and IObserver<T>
// - Maintain a list of observers
// - Broadcast OnNext to all observers
// - Handle OnError and OnCompleted

Console.WriteLine("--- Testing Synchronous Observable ---");
// TODO: Create and subscribe to NumberSequence
// var numbers = new NumberSequence(1, 5);
// numbers.Subscribe(new ConsoleObserver<int>("Sync"));

Console.WriteLine();
Console.WriteLine("--- Testing Async Observable with Cancellation ---");
// TODO: Create AsyncNumberSequence and dispose early
// var asyncNumbers = new AsyncNumberSequence(10, 100);
// var subscription = asyncNumbers.Subscribe(new ConsoleObserver<int>("Async"));
// await Task.Delay(350);
// subscription.Dispose();

Console.WriteLine();
Console.WriteLine("--- Testing Subject ---");
// TODO: Create a Subject and push values to multiple subscribers
// var subject = new Subject<string>();
// subject.Subscribe(new ConsoleObserver<string>("Sub1"));
// subject.Subscribe(new ConsoleObserver<string>("Sub2"));
// subject.OnNext("Hello");
// subject.OnNext("World");
// subject.OnCompleted();

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. IObservable<T> is the producer (data source)");
Console.WriteLine("2. IObserver<T> is the consumer (receives data)");
Console.WriteLine("3. Subscribe returns IDisposable for cleanup");
Console.WriteLine("4. Subject<T> bridges imperative and reactive code");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
