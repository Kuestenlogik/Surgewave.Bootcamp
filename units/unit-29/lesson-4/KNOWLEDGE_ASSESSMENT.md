# Knowledge Assessment: Lesson 11.4 - Event Sourcing

## Multiple Choice

### 1. In event sourcing, what is the "source of truth"?
- [ ] A) The current state in the database
- [ ] B) The event stream
- [ ] C) The latest projection
- [ ] D) The command handler

<details>
<summary>Answer</summary>
**B)** In event sourcing, events are the source of truth. Current state is derived by replaying events. Events are immutable facts about what happened, not the current state.
</details>

### 2. What is a projection in event sourcing?
- [ ] A) A prediction of future events
- [ ] B) A transformation of events into a read-optimized view
- [ ] C) A backup of the event store
- [ ] D) A validation of event schemas

<details>
<summary>Answer</summary>
**B)** A projection transforms the event stream into a specific read-optimized view. Different projections can create different views (current balance, transaction history, analytics) from the same event stream.
</details>

### 3. Which event sourcing principle states that events cannot be modified or deleted?
- [ ] A) Temporal Query
- [ ] B) Append-Only
- [ ] C) Immutability
- [ ] D) Event-First Design

<details>
<summary>Answer</summary>
**C)** Immutability means events are never modified or deleted once stored. To "undo" something, you append a compensating event (e.g., MoneyRefunded) rather than deleting the original event.
</details>

## True/False

### 4. Event replay allows you to reconstruct the state of an aggregate at any point in time.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - This is called temporal query. By replaying events up to a specific timestamp, you can determine exactly what the state was at that moment - valuable for debugging, auditing, and compliance.
</details>

### 5. In event sourcing, you should store the current state alongside events to avoid replay overhead.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - While you CAN use snapshots to optimize replay performance, the events remain the source of truth. Current state is always derivable from events. Storing state as the primary source defeats the purpose of event sourcing.
</details>

## Code Challenge

### 6. Given these events, what is the final account balance?

```csharp
var events = new Event[]
{
    new AccountCreated("ACC001", "John Doe", 1000.00m),
    new MoneyDeposited("ACC001", 500.00m, "Salary"),
    new MoneyWithdrawn("ACC001", 200.00m, "ATM"),
    new MoneyDeposited("ACC001", 150.00m, "Refund"),
    new MoneyWithdrawn("ACC001", 75.00m, "Subscription")
};
```

<details>
<summary>Answer</summary>
Replaying the events:

1. **AccountCreated**: Balance = 1000.00
2. **MoneyDeposited** (+500): Balance = 1500.00
3. **MoneyWithdrawn** (-200): Balance = 1300.00
4. **MoneyDeposited** (+150): Balance = 1450.00
5. **MoneyWithdrawn** (-75): Balance = **1375.00**

The final balance is **$1,375.00**.

Code to calculate:
```csharp
decimal balance = 0;
foreach (var evt in events)
{
    balance = evt switch
    {
        AccountCreated e => e.InitialBalance,
        MoneyDeposited e => balance + e.Amount,
        MoneyWithdrawn e => balance - e.Amount,
        _ => balance
    };
}
// balance == 1375.00m
```
</details>

### 7. What's wrong with this event sourcing implementation?

```csharp
public class AccountService
{
    private readonly Dictionary<string, decimal> _balances = new();

    public async Task HandleDeposit(string accountId, decimal amount)
    {
        // Update state directly
        _balances[accountId] += amount;

        // Then emit event
        await _producer.ProduceAsync("events",
            new MoneyDeposited(accountId, amount, "Deposit"));
    }

    public async Task HandleWithdraw(string accountId, decimal amount)
    {
        if (_balances[accountId] < amount)
            throw new InsufficientFundsException();

        // Update state directly
        _balances[accountId] -= amount;

        // Then emit event
        await _producer.ProduceAsync("events",
            new MoneyWithdrawn(accountId, amount, "Withdrawal"));
    }
}
```

<details>
<summary>Answer</summary>
Multiple issues violate event sourcing principles:

1. **State updated BEFORE event** - If the produce fails, state is updated but no event recorded. On replay, the state will be different.

2. **In-memory state as source of truth** - The dictionary is treated as the primary state, not the events.

3. **No event-driven state updates** - State should be derived from events, not modified directly.

Fix:
```csharp
public class AccountService
{
    private readonly Dictionary<string, decimal> _balances = new();

    public async Task HandleDeposit(string accountId, decimal amount)
    {
        // 1. Create the event first
        var evt = new MoneyDeposited(accountId, amount, "Deposit");

        // 2. Persist the event (source of truth)
        await _producer.ProduceAsync("events", evt);

        // 3. Update local projection from the event
        ApplyEvent(evt);
    }

    private void ApplyEvent(object evt)
    {
        // State is derived from events
        switch (evt)
        {
            case AccountCreated e:
                _balances[e.AccountId] = e.InitialBalance;
                break;
            case MoneyDeposited e:
                _balances[e.AccountId] += e.Amount;
                break;
            case MoneyWithdrawn e:
                _balances[e.AccountId] -= e.Amount;
                break;
        }
    }

    // On startup, rebuild state from events
    public async Task RebuildState()
    {
        _balances.Clear();
        await foreach (var evt in _eventStore.ReplayAsync())
        {
            ApplyEvent(evt);
        }
    }
}
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
