# Knowledge Assessment: Lesson 12.3 - Rx Operators

## Multiple Choice

### 1. What is the correct description of how reactive operators work?
- [ ] A) They modify the source observable in place
- [ ] B) They take an IObservable as input and return a new IObservable as output
- [ ] C) They directly call methods on the observer
- [ ] D) They store all values in memory before processing

<details>
<summary>Answer</summary>
**B)** Reactive operators take an IObservable as input and return a new IObservable as output. They subscribe to the source internally and transform notifications before forwarding them. This enables fluent chaining.
</details>

### 2. Which operator would you use to transform each message value from JSON to a deserialized object?
- [ ] A) Where
- [ ] B) Buffer
- [ ] C) Select
- [ ] D) Distinct

<details>
<summary>Answer</summary>
**C)** Select (also known as Map) is used to transform each element in the stream. For example: `.Select(msg => JsonSerializer.Deserialize<Order>(msg.Value))`
</details>

### 3. What does the Buffer operator do?
- [ ] A) Filters elements based on a predicate
- [ ] B) Transforms each element to a new type
- [ ] C) Collects elements into batches
- [ ] D) Removes duplicate elements

<details>
<summary>Answer</summary>
**C)** The Buffer operator collects elements into batches (groups). For example, `Buffer(3)` collects every 3 elements and emits them as a list/array, which is useful for batch processing.
</details>

## True/False

### 4. Operators can only be applied one at a time and cannot be chained together.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - One of the main benefits of reactive operators is that they can be chained together fluently: `source.Where(x => x > 0).Select(x => x * 2).Buffer(3).Subscribe(...)`. This creates a pipeline of transformations.
</details>

### 5. The Where operator in reactive programming is equivalent to LINQ's Where method conceptually.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The Where operator filters elements based on a predicate, conceptually the same as LINQ's Where. Elements that match the predicate are passed through; others are discarded.
</details>

## Code Challenge

### 6. What is wrong with this operator implementation?

```csharp
public static IObservable<T> Where<T>(
    this IObservable<T> source,
    Func<T, bool> predicate)
{
    var results = new List<T>();
    source.Subscribe(x =>
    {
        if (predicate(x))
            results.Add(x);
    });
    return new ListObservable<T>(results);
}
```

<details>
<summary>Answer</summary>
Multiple issues:

1. **Eager evaluation**: This subscribes immediately and collects all results into a list, defeating the purpose of reactive streaming
2. **Memory consumption**: All matching values are stored in memory
3. **Timing**: The method subscribes and waits before returning, instead of lazily subscribing when the returned observable is subscribed to
4. **No disposal handling**: The subscription is never disposed
5. **Blocking**: Not suitable for infinite streams

Fix using proper lazy evaluation:
```csharp
public static IObservable<T> Where<T>(
    this IObservable<T> source,
    Func<T, bool> predicate)
{
    return Observable.Create<T>(observer =>
    {
        return source.Subscribe(
            x => { if (predicate(x)) observer.OnNext(x); },
            observer.OnError,
            observer.OnCompleted
        );
    });
}
```
Or using a custom WhereObservable class that subscribes lazily.
</details>

### 7. Write a reactive pipeline that: filters orders with Total > 100, transforms them to extract just the OrderId, and batches them into groups of 5.

```csharp
surgewaveObservable
    // Add your operators here
```

<details>
<summary>Answer</summary>
```csharp
surgewaveObservable
    .Where(msg => msg.Value != null)
    .Select(msg => JsonSerializer.Deserialize<Order>(msg.Value))
    .Where(order => order.Total > 100)
    .Select(order => order.OrderId)
    .Buffer(5)
    .Subscribe(batch => ProcessOrderIdBatch(batch));
```

This pipeline:
1. Filters out null messages
2. Deserializes the message value to Order objects
3. Filters to only orders with Total > 100
4. Transforms to extract just the OrderId
5. Batches into groups of 5
6. Processes each batch
</details>

## Score
- 7/7: Excellent! You have mastered reactive operators
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
