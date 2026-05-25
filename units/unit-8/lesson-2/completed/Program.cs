// Surgewave Bootcamp - Unit 12, Lesson 2: Surgewave as Observable
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.2 - Surgewave as Observable ===");
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
Console.WriteLine("--- Subscribing Reactively (Single Observer) ---");

var observable = new SurgewaveObservable<string, string>(
    $"localhost:{port}",
    "reactive-topic",
    "reactive-group-1"
);

var observer1 = new MessageObserver<string, string>("Observer1");
using (var subscription = observable.Subscribe(observer1))
{
    await Task.Delay(2000); // Wait for messages
    Console.WriteLine($"  Observer1 received {observer1.Count} messages");
}
Console.WriteLine("  Subscription disposed");

Console.WriteLine();
Console.WriteLine("--- Subscribing with Multiple Observers ---");

// Produce more messages
for (int i = 11; i <= 15; i++)
{
    await producer.ProduceAsync("reactive-topic-2", $"key-{i}", $"message-{i}");
}

var observable2 = new SurgewaveObservable<string, string>(
    $"localhost:{port}",
    "reactive-topic-2",
    "reactive-group-2"
);

var observer2a = new MessageObserver<string, string>("Observer2A");
var observer2b = new MessageObserver<string, string>("Observer2B");

// Each observer gets its own consumer (different consumer groups would share)
using var sub2a = observable2.Subscribe(observer2a);
await Task.Delay(1500);

Console.WriteLine();
Console.WriteLine("--- Using Extension Method ---");

var observable3 = "reactive-topic".ToObservable<string, string>(
    $"localhost:{port}",
    "reactive-group-3"
);
var observer3 = new MessageObserver<string, string>("ExtMethod");
using (var subscription3 = observable3.Subscribe(observer3))
{
    await Task.Delay(1500);
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. SurgewaveConsumer can be wrapped as IObservable<T>");
Console.WriteLine("2. Each subscription creates its own consumer instance");
Console.WriteLine("3. Disposing subscription cleans up consumer and cancellation");
Console.WriteLine("4. CompositeDisposable manages multiple resources");
Console.WriteLine("5. Extension methods provide fluent API");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Type Definitions ---

// Step 1 - Subscription helper class
public class Subscription : IDisposable
{
    private readonly Action _onDispose;
    private bool _disposed;

    public Subscription(Action onDispose) => _onDispose = onDispose;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _onDispose?.Invoke();
        }
    }
}

// Step 2 - CompositeDisposable for multiple resources (supports both sync and async)
public class CompositeDisposable : IDisposable
{
    private readonly object?[] _disposables;
    private bool _disposed;

    public CompositeDisposable(params object?[] disposables)
        => _disposables = disposables;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            foreach (var d in _disposables)
            {
                try
                {
                    if (d is IAsyncDisposable asyncDisposable)
                    {
                        asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    }
                    else if (d is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
        }
    }
}

// Step 3 - Surgewave Observable wrapper
public class SurgewaveObservable<TKey, TValue> : IObservable<ConsumeResult<TKey, TValue>>
{
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;

    public SurgewaveObservable(string bootstrapServers, string topic, string groupId)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
        _groupId = groupId;
    }

    public IDisposable Subscribe(IObserver<ConsumeResult<TKey, TValue>> observer)
    {
        var cts = new CancellationTokenSource();

        var consumer = new SurgewaveConsumer<TKey, TValue>(options =>
        {
            options.BootstrapServers = _bootstrapServers;
            options.GroupId = _groupId;
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe(_topic);

        Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
                    if (result != null && !cts.Token.IsCancellationRequested)
                    {
                        observer.OnNext(result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during disposal - do nothing
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }, cts.Token);

        return new CompositeDisposable(
            new Subscription(() => cts.Cancel()),
            consumer
        );
    }
}

// Step 4 - Message observer for testing
public class MessageObserver<TKey, TValue> : IObserver<ConsumeResult<TKey, TValue>>
{
    private readonly string _name;
    private int _count;

    public MessageObserver(string name) => _name = name;
    public int Count => _count;

    public void OnNext(ConsumeResult<TKey, TValue> result)
    {
        _count++;
        Console.WriteLine($"  [{_name}] Message {_count}: Key={result.Key}, Value={result.Value}");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"  [{_name}] Error: {error.Message}");
    }

    public void OnCompleted()
    {
        Console.WriteLine($"  [{_name}] Completed after {_count} messages");
    }
}

// Extension method for fluent API
public static class SurgewaveReactiveExtensions
{
    public static IObservable<ConsumeResult<TKey, TValue>> ToObservable<TKey, TValue>(
        this string topic,
        string bootstrapServers,
        string groupId)
    {
        return new SurgewaveObservable<TKey, TValue>(bootstrapServers, topic, groupId);
    }
}
