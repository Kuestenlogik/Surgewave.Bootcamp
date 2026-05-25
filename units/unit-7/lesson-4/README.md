# Lesson 7.4: Foreign Key Table-Table Joins

Standard KTable joins require both tables to be keyed by the same key. Foreign key joins allow you to join two tables where one table references the other by a non-key field.

## Learning Objectives

- Understand the foreign key join problem and how it differs from standard joins
- Use `Join` and `LeftJoin` with a `keyExtractor` function
- Understand `ForeignKeySubscriptionStore` internals
- Handle updates that trigger re-joins across affected records

## Foreign Key Join API

```csharp
var orders = builder.Table<string, Order>("orders");       // keyed by OrderId
var customers = builder.Table<string, Customer>("customers"); // keyed by CustomerId

var enrichedOrders = orders.Join<string, Customer, EnrichedOrder>(
    foreignTable: customers,
    keyExtractor: order => order.CustomerId,
    joiner: (order, customer) => new EnrichedOrder
    {
        OrderId = order.OrderId,
        CustomerName = customer.Name,
        Region = customer.Region,
        Amount = order.Amount
    }
);
```

## Left Join for Optional Matches

Use `LeftJoin` when the foreign key might not have a match in the right table:

```csharp
var enrichedOrders = orders.LeftJoin<string, Customer, EnrichedOrder>(
    foreignTable: customers,
    keyExtractor: order => order.CustomerId,
    joiner: (order, customer) => new EnrichedOrder
    {
        OrderId = order.OrderId,
        CustomerName = customer?.Name ?? "Unknown",
        Region = customer?.Region ?? "N/A",
        Amount = order.Amount
    }
);
```

## ForeignKeySubscriptionStore

The `ForeignKeySubscriptionStore` tracks which primary keys reference each foreign key. When the foreign table updates, all affected primary records are re-joined automatically.

## Exercise

1. Create an `orders` topic keyed by OrderId with a `ProductId` field
2. Create a `products` topic keyed by ProductId with `Name` and `Price` fields
3. Build a foreign key join: `orders.Join(products, o => o.ProductId, ...)`
4. Produce orders and products, verifying joined output
5. Update a product's price and verify all referencing orders re-emit with the new price
6. Test with `LeftJoin` by producing an order referencing a non-existent product

---

[Back to Unit 7](../README.md)
