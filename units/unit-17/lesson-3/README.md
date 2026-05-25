# Lesson 17.3: Building Your First Pipeline

This lesson walks you through building a simple data pipeline from scratch using the Surgewave Control UI at `http://localhost:5050`.

### Step 1: Open the Pipeline Editor

1. Navigate to **Pipelines** in the left sidebar
2. Click **+ New Pipeline**
3. The visual editor canvas opens with an empty workspace

### Step 2: Add a Generator Source Node

1. In the Node Palette (left panel), expand **Sources**
2. Drag **Generator** onto the canvas
3. The node appears with a default label

### Step 3: Add a Transform Node

1. In the Node Palette, expand **Streams**
2. Drag **SQL Transform** onto the canvas to the right of the Generator

### Step 4: Add a Stdio Sink Node

1. Expand **Sinks** in the palette
2. Drag **Stdio** onto the canvas to the right of the transform

### Step 5: Connect the Nodes

1. Hover over the Generator node's **output port** (right side)
2. Click and drag to the SQL Transform node's **input port** (left side)
3. A connection line appears
4. Repeat: drag from SQL Transform output to Stdio input

Your pipeline should now look like:

```mermaid
graph LR
    Generator --> SQLTransform["SQL Transform"] --> Stdio
```

### Step 6: Configure Each Node

Click on each node to open the configuration panel on the right:

**Generator node:**

| Setting | Value |
|---------|-------|
| `interval.ms` | `1000` |
| `format` | `json` |

**SQL Transform node:**

| Setting | Value |
|---------|-------|
| `query` | `SELECT * WHERE value > 50` |

**Stdio node:**

No configuration needed (defaults to standard output).

### Step 7: Set Pipeline Metadata

1. Click the **Settings** icon (gear) in the toolbar
2. Set **Name**: `My First Pipeline`
3. Set **Description**: `Generator producing JSON, filtered by SQL, output to console`
4. Click **Save**

### Step 8: Start the Pipeline

1. Click the **Play** button in the toolbar
2. The pipeline status changes from `Draft` to `Running`
3. Each node shows a green indicator when active

### Step 9: Monitor Execution

1. The status bar shows messages processed per second
2. Click any connection line to see sample messages flowing through
3. Check the Stdio output in the broker's console window

### Step 10: Stop the Pipeline

1. Click the **Stop** button
2. Status returns to `Stopped`
3. Internal topics are preserved -- you can restart without data loss

---

[Back to Unit 17](../README.md)
