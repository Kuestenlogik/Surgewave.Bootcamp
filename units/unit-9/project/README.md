# Unit 8 Project: Data Pipeline

Build a data pipeline that ingests CSV data into Surgewave and stores it in an in-memory database.

## Scenario

You're building an ETL pipeline for a retail analytics system. Product inventory data arrives as CSV files and needs to be:
1. Parsed and validated
2. Streamed through Surgewave for processing
3. Stored in an in-memory "database" for querying
4. Aggregated for reporting

## Requirements

### Functional Requirements

- [ ] Read product inventory from CSV format
- [ ] Produce inventory events to Surgewave topic
- [ ] Consume events and store in in-memory dictionary
- [ ] Calculate category aggregations (total stock, average price)
- [ ] Query the "database" and display results

### Technical Requirements

- [ ] Use proper CSV parsing (handle edge cases)
- [ ] Implement data validation (positive stock, valid price)
- [ ] Use partitioning by category for related data locality
- [ ] Implement idempotent inserts (same product ID updates)

## Data Schema

```csharp
public record ProductInventory(
    string ProductId,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice,
    DateTime LastUpdated
);

public record CategoryStats(
    string Category,
    int TotalProducts,
    int TotalStock,
    decimal AveragePrice
);
```

## Sample CSV Data

```csv
ProductId,Name,Category,StockQuantity,UnitPrice
P001,Laptop Pro,Electronics,50,1299.99
P002,Wireless Mouse,Electronics,200,29.99
P003,Office Chair,Furniture,75,249.99
P004,Standing Desk,Furniture,30,599.99
P005,USB-C Cable,Electronics,500,12.99
P006,Monitor Arm,Furniture,100,89.99
```

## Expected Output

```
=== Data Pipeline: CSV -> Surgewave -> In-Memory DB ===

[CSV Reader] Loading product inventory...
[CSV Reader] Parsed 6 products from CSV

[Producer] Streaming to Surgewave...
[Producer] P001 (Electronics) -> Partition 0
[Producer] P002 (Electronics) -> Partition 0
[Producer] P003 (Furniture) -> Partition 1
[Producer] P004 (Furniture) -> Partition 1
[Producer] P005 (Electronics) -> Partition 0
[Producer] P006 (Furniture) -> Partition 1
[Producer] Published 6 inventory events

[Consumer] Processing inventory stream...
[Database] Stored: P001 - Laptop Pro (50 units)
[Database] Stored: P002 - Wireless Mouse (200 units)
[Database] Stored: P003 - Office Chair (75 units)
[Database] Stored: P004 - Standing Desk (30 units)
[Database] Stored: P005 - USB-C Cable (500 units)
[Database] Stored: P006 - Monitor Arm (100 units)

[Query] In-Memory Database Contents:
  Electronics: 3 products, 750 total stock, avg price $447.66
  Furniture: 3 products, 205 total stock, avg price $313.32

=== Data Pipeline Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-8/project/start
dotnet run
```

## Hints

1. Use `string.Split(',')` for simple CSV parsing
2. Use `Category` as the message key for partitioning
3. Use `ConcurrentDictionary<string, ProductInventory>` for the database
4. LINQ's `GroupBy` works well for category aggregations

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| CSV parsing works correctly | 20 |
| Products streamed through Surgewave | 25 |
| In-memory storage is correct | 25 |
| Category aggregations accurate | 20 |
| Code is clean and readable | 10 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
