# Knowledge Assessment: Lesson 8.2 - File Connectors

## Multiple Choice

### 1. What is the correct data flow for a File Sink Connector?
- [ ] A) CSV File -> File Sink Connector -> Surgewave Topic
- [ ] B) Surgewave Topic -> File Sink Connector -> CSV/JSON File
- [ ] C) File Sink Connector -> Surgewave Topic -> CSV File
- [ ] D) JSON File -> Surgewave Topic -> File Sink Connector

<details>
<summary>Answer</summary>
**B)** A sink connector consumes data FROM Surgewave topics and writes TO external systems. So the flow is: Surgewave Topic -> File Sink Connector -> CSV/JSON File.
</details>

### 2. Which file format is specifically designed for streaming one record per line?
- [ ] A) CSV
- [ ] B) JSON
- [ ] C) JSON Lines
- [ ] D) XML

<details>
<summary>Answer</summary>
**C)** JSON Lines (also known as NDJSON) is designed for streaming where each line is a valid JSON object. This makes it ideal for streaming scenarios where records arrive continuously.
</details>

### 3. What configuration option determines how columns are separated in a CSV file?
- [ ] A) has.header
- [ ] B) delimiter
- [ ] C) file.path
- [ ] D) escaping

<details>
<summary>Answer</summary>
**B)** The delimiter configuration specifies the character used to separate columns (commonly comma, tab, or semicolon).
</details>

## True/False

### 4. File source connectors convert batch file data into streaming topic data.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - File connectors bridge the gap between batch-oriented files and streaming topics, effectively converting batch data into a continuous stream of records.
</details>

### 5. CSV and JSON files require the same parsing strategy.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - CSV and JSON require different parsing strategies. CSV is tabular with delimiters and optional headers, while JSON is hierarchical with nested structures and explicit types.
</details>

## Code Challenge

### 6. What configuration is missing from this CSV source connector setup?

```csharp
var csvSource = new CsvSourceConnector();
csvSource.Configure(new Dictionary<string, string>
{
    ["file.path"] = "/data/customers.csv",
    ["has.header"] = "true",
    ["delimiter"] = ","
});
```

<details>
<summary>Answer</summary>
The **topic** configuration is missing. The connector needs to know which Surgewave topic to write the CSV data to.

Fix:
```csharp
csvSource.Configure(new Dictionary<string, string>
{
    ["file.path"] = "/data/customers.csv",
    ["topic"] = "csv-customers",  // Missing!
    ["has.header"] = "true",
    ["delimiter"] = ","
});
```
</details>

### 7. What issue might occur with this JSON data when using a standard JSON parser?

```json
{"id": 1, "name": "Alice"}
{"id": 2, "name": "Bob"}
{"id": 3, "name": "Charlie"}
```

<details>
<summary>Answer</summary>
A standard JSON parser expects a single JSON document (object or array). This format has **multiple JSON objects without array brackets or commas** separating them. This is the **JSON Lines** format, which requires reading and parsing each line separately.

Fix: Either parse line-by-line for JSON Lines:
```csharp
foreach (var line in File.ReadLines(path))
{
    var record = JsonSerializer.Deserialize<Record>(line);
}
```

Or wrap in a JSON array if standard JSON is required:
```json
[
  {"id": 1, "name": "Alice"},
  {"id": 2, "name": "Bob"},
  {"id": 3, "name": "Charlie"}
]
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
