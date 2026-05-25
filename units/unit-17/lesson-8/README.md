# Lesson 17.8: Advanced Pipeline Features

### Pipeline Templates

Surgewave ships with 16 built-in templates across multiple categories:

| Category | Templates |
|----------|-----------|
| Data Integration | File to Kafka, Database Change Capture, PostgreSQL CDC to BigQuery |
| Event Processing | HTTP Webhook Processor |
| IoT | IoT MQTT to Kafka |
| Cloud | S3 Data Lake Ingestion, Azure Event Hub to Blob |
| Search | Elasticsearch Indexer |
| AI/ML | RAG Embedding Pipeline, RAG Chatbot, Document Indexer, Agent with Tools |
| Architecture | Event Sourcing Pattern, Saga Orchestration |
| Analytics | Real-time Analytics Dashboard |
| Observability | Log Analytics Pipeline |
| Notifications | Alert to Slack |

**Create a pipeline from a template via API:**

```bash
curl -X POST http://localhost:5050/api/pipelines/from-template/rag-chatbot \
  -H "Content-Type: application/json"
```

You can also save any existing pipeline as a custom template for reuse.

### AI Pipeline Seeder

On first startup, the `AiPipelineSeeder` creates three example AI pipelines in `Draft` status:

| Pipeline ID | Name | Description |
|-------------|------|-------------|
| `example-ai-chatbot` | Example: Simple AI Chatbot | 4 nodes: ChatInput -> PromptBuilder -> LLM -> ChatResponse |
| `example-ai-document-qa` | Example: Document Q&A with RAG | 8 nodes: ChatInput -> Router -> DocumentParser/Embedder + Retriever -> PromptBuilder -> LLM -> ChatResponse |
| `example-ai-agent` | Example: AI Agent with Tool Access | 3 nodes: ChatInput -> Agent -> ChatResponse |

These pipelines require API key configuration before starting. They serve as reference implementations you can study and modify.

### Pipeline Versioning

- Every save creates a new version in the pipeline's history
- Use the **Version History** panel to see all previous versions
- **Diff View** highlights changes between versions (added/removed nodes, config changes)
- **Restore** any previous version with a single click

### Hot-Deploy

Update running pipelines without downtime:

1. Make changes in the editor while the pipeline is running
2. Click **Deploy** (not Stop + Start)
3. Surgewave performs a rolling update: new node configurations are applied, connections are re-wired
4. In-flight messages are preserved

### Sub-Pipelines (Composite Nodes)

Encapsulate reusable pipeline fragments as composite nodes:

1. Select multiple nodes in the editor
2. Right-click > **Create Sub-Pipeline**
3. Define input and output ports via `PortMappingsJson`
4. The group collapses into a single composite node
5. Reuse across multiple pipelines by referencing the `SubPipelineId`

### Data Provenance & Lineage

- **Record Lineage Tracking**: Each record carries provenance headers showing which nodes processed it
- **Cross-Pipeline Lineage**: When pipelines publish to topics consumed by other pipelines, Surgewave discovers the inter-pipeline data flow
- **Lineage View**: Visual graph showing end-to-end data lineage across all connected pipelines

### Remote Worker Deployment

Pipelines can distribute connector execution across remote worker nodes for resource isolation and horizontal scaling:

1. Workers register with the broker and advertise their available connector types
2. In the Pipeline Editor, connectors that support remote execution show a **worker availability badge**
3. Click a node and open the **Worker Selector** dropdown in the configuration panel to choose a target worker
4. When the pipeline starts, the broker schedules the node on the selected worker instead of running it locally

This is particularly useful for resource-intensive connectors (e.g., LLM inference, large file parsing) that benefit from dedicated compute, or for geo-distributed deployments where connectors need to run close to the data source.

### Node Groups & Alignment

- **Node Groups**: Select multiple nodes, press `Ctrl+G` to group them into a named container
- **Collapse/Expand**: Click the group header to collapse or expand
- **Alignment Guides**: When moving nodes, snap-to-grid guides appear for clean layouts

---

[Back to Unit 17](../README.md)
