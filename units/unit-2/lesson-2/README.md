# Lesson 2.2: Topic & Message Management

The Topics section provides full CRUD operations for Surgewave topics along with a powerful message browser and producer.

## Learning Objectives

- Create, read, update, and delete topics through the Control UI
- Browse messages by partition, offset, and key
- Produce messages with headers and partition selection
- Perform bulk operations on multiple topics

## Topic List

The topic list view shows all topics with sortable columns for name, partition count, replication factor, throughput, and storage size. A search bar and sort controls help navigate large topic sets.

## Topic Detail View

Clicking a topic opens its detail page with three tabs:

- **Partitions**: Partition leader/replica assignments, ISR status, log sizes
- **Configuration**: All topic-level configs (retention, segment size, compression, etc.)
- **Messages**: Integrated message browser

## Message Browser

The message browser lets you consume messages from specific partitions and offsets. You can expand individual messages to see their key, headers, and full value payload (with JSON formatting).

## Message Producer

The built-in producer supports JSON and plain text messages with optional headers and partition selection. Use "Produce & Continue" to send multiple messages without closing the dialog.

## Bulk Operations

Select multiple topics using checkboxes to perform bulk actions:

- **Delete**: Remove multiple topics at once (with confirmation dialog)
- **Config Changes**: Apply configuration changes across selected topics
- **Export**: Export topic configurations as JSON for backup or migration

## SQL Query Page

The Data section of the navigation also includes a **SQL Query** page powered by Surgewave's built-in SQL engine. You can run standard SQL queries (`SELECT`, `WHERE`, `ORDER BY`, `GROUP BY`, aggregates) against topic data with JSON field extraction. Beyond one-off queries, you can create **continuous queries** that run automatically as new messages arrive, producing results into a destination topic for derived views or SQL-driven alerting.

---

[Back to Unit 2](../README.md)
