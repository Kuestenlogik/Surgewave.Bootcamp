# Knowledge Assessment: Lesson 12.1 - Reactive Basics

## Multiple Choice

### 1. What is the primary difference between imperative (pull) and reactive (push) programming?
- [ ] A) Imperative uses less memory than reactive
- [ ] B) In reactive programming, the producer notifies consumers when data is available
- [ ] C) Reactive programming always blocks the thread
- [ ] D) Imperative programming uses callbacks exclusively

<details>
<summary>Answer</summary>
**B)** In reactive programming, the producer notifies consumers when data is available. In imperative programming, the consumer requests data when needed (pull model), while reactive programming pushes data to subscribers as it becomes available (push model).
</details>

### 2. Which method in IObserver<T> is called when the observable stream finishes normally?
- [ ] A) OnNext()
- [ ] B) OnError()
- [ ] C) OnCompleted()
- [ ] D) OnDispose()

<details>
<summary>Answer</summary>
**C)** OnCompleted() is called when the observable stream finishes normally. OnNext() delivers values, OnError() delivers exceptions, and there is no OnDispose() method in IObserver<T>.
</details>

### 3. What does the Subscribe() method on IObservable<T> return?
- [ ] A) void
- [ ] B) Task
- [ ] C) IObserver<T>
- [ ] D) IDisposable

<details>
<summary>Answer</summary>
**D)** Subscribe() returns an IDisposable that can be used to unsubscribe from the observable and clean up resources.
</details>

## True/False

### 4. A Subject<T> can act as both an IObservable<T> and an IObserver<T>.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - A Subject<T> implements both interfaces, making it useful for bridging imperative and reactive code. You can push values to it using OnNext() and others can subscribe to it.
</details>

### 5. In reactive programming, error handling is done using traditional try/catch blocks.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - In reactive programming, errors are propagated through the OnError callback method in IObserver<T>, not through try/catch blocks. This provides a unified error propagation mechanism through the pipeline.
</details>

## Code Challenge

### 6. What is the issue with this observer implementation?

```csharp
public class MyObserver<T> : IObserver<T>
{
    public void OnNext(T value)
    {
        Console.WriteLine($"Received: {value}");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"Error: {error.Message}");
        // Continue processing
    }
}
```

<details>
<summary>Answer</summary>
The issue is that the class is missing the OnCompleted() method required by the IObserver<T> interface. Additionally, after OnError is called, the observer should not expect any more notifications - the observable contract states that after OnError or OnCompleted, no more OnNext calls should occur.

Fix:
```csharp
public class MyObserver<T> : IObserver<T>
{
    public void OnNext(T value)
    {
        Console.WriteLine($"Received: {value}");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"Error: {error.Message}");
        // Stream has terminated with an error
    }

    public void OnCompleted()
    {
        Console.WriteLine("Stream completed");
    }
}
```
</details>

### 7. What happens when you call Dispose() on the IDisposable returned from Subscribe()?

<details>
<summary>Answer</summary>
When you call Dispose() on the subscription:
1. The observer stops receiving notifications (OnNext, OnError, OnCompleted)
2. The subscription is cancelled
3. Any resources associated with the subscription are cleaned up
4. The observable may stop producing values if there are no other subscribers

This is the proper way to unsubscribe from an observable sequence and prevent memory leaks.
</details>

## Score
- 7/7: Excellent! You have a solid understanding of reactive programming basics
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
