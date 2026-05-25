# Surgewave Bootcamp

A hands-on, self-paced tutorial to master Surgewave. 36 units, 8 learning paths, 35 guided tours.

## Goal

Learn to build, operate, and scale real-time data systems with Surgewave — from your first message to production-grade streaming pipelines.

## What is Surgewave Bootcamp?

Surgewave Bootcamp is an interactive learning experience where you'll build real applications while learning Surgewave concepts. Each lesson includes:

- **Concepts**: Clear explanations of what you're learning
- **Exercises**: Hands-on coding tasks
- **Starter Code**: Incomplete code to fill in
- **Completed Solutions**: Reference implementations
- **Guided Tours**: 35 Shepherd.js interactive walkthroughs integrated into the DocFX learn site

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- A code editor (VS Code, Visual Studio, Rider)
- Basic C# knowledge
- ~2-4 hours per unit
- For AI units (18, 19, 20, 17): [Surgewave.AI](https://github.com/Kuestenlogik/Surgewave.Ai) NuGet packages

## Getting Started

```bash
# Clone the repository
git clone https://github.com/Kuestenlogik/Surgewave.Bootcamp.git
cd Surgewave.Bootcamp

# Build the solution
dotnet build Kuestenlogik.Surgewave.Bootcamp.slnx

# Start with the introduction
cd units/unit-0/lesson-1
```

### DocFX Learn Site

The Bootcamp includes a full DocFX-powered learn site with Shepherd.js guided tours:

```bash
# Build and serve the learn site on port 8081
.\build-learn.ps1 -Serve -Port 8081
```

## Learning Paths

Choose from 8 structured paths based on your role and goals:

| Path | Target Audience | Duration | Units |
|------|-----------------|----------|-------|
| **Developer Fundamentals** | All developers (start here) | ~30h | 8 units |
| **Stream Processing** | Data engineers, backend devs | ~12h | 2 units |
| **Data Integration** | Integration/platform engineers | ~24h | 6 units |
| **Pipelines & Workflows** | Low-code builders, process automation | ~10h | 2 units |
| **AI & Intelligent Systems** | AI/ML engineers, LLM developers | ~20h | 4 units |
| **Operations & Production** | DevOps, SREs, platform engineers | ~35h | 9 units |
| **Advanced Patterns** | Senior developers, architects | ~28h | 6 units |
| **Performance & Resilience** | Performance engineers, SREs | ~15h | 3 units |

Or complete all 36 units in order for comprehensive coverage.

**[View Detailed Learning Paths →](LEARNING_PATHS.md)**

---

## Curriculum

### Unit 0: Introduction
*Time: ~1 hour*

Understand what Surgewave is and set up your development environment.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [0.1](units/unit-0/lesson-1/README.md) | What is Surgewave? | Message streaming, Surgewave vs alternatives |
| [0.2](units/unit-0/lesson-2/README.md) | Project Setup | Install packages, create projects |
| [0.3](units/unit-0/lesson-3/README.md) | Hello Surgewave | Connect to Surgewave and verify setup |
| [0.4](units/unit-0/lesson-4/README.md) | Contributing to Surgewave | GitHub, bug reports, pull requests |
| [0.5](units/unit-0/lesson-5/README.md) | Getting Support | Community, documentation, support plans |

### Unit 1: Surgewave Fundamentals [DEV]
*Time: ~3 hours*

Learn the core concepts of message streaming with Surgewave.

| Lesson | Topic | What You'll Build |
|--------|-------|-------------------|
| [1.1](units/unit-1/lesson-1/README.md) | Producing Messages | Send your first messages |
| [1.2](units/unit-1/lesson-2/README.md) | Consuming Messages | Receive and process messages |
| [1.3](units/unit-1/lesson-3/README.md) | Topics & Partitions | Understand partitioning |
| [1.4](units/unit-1/lesson-4/README.md) | Consumer Groups | Scale with multiple consumers |
| [1.5](units/unit-1/lesson-5/README.md) | Batching & Message Sizing | Optimize throughput |
| [1.6](units/unit-1/lesson-6/README.md) | Message Best Practices | Events vs commands, immutability |

### Unit 2: Surgewave Control UI Mastery [DEV]
*Time: ~3 hours*

Master the Surgewave Control UI dashboard for cluster management.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [2.1](units/unit-2/lesson-1/README.md) | Dashboard & Navigation | Home dashboard widgets, layout customization, theme toggle |
| [2.2](units/unit-2/lesson-2/README.md) | Topic & Message Management | Topic CRUD, message browser, producer, bulk operations |
| [2.3](units/unit-2/lesson-3/README.md) | Consumer Groups & Lag Monitoring | Group state, member assignment, lag charts, offset reset |
| [2.4](units/unit-2/lesson-4/README.md) | Pipeline Management | Pipeline editor, chat integration, templates, versioning |
| [2.5](units/unit-2/lesson-5/README.md) | Monitoring & Alerting | Metrics dashboards, alert rules, performance advisor |
| [2.6](units/unit-2/lesson-6/README.md) | Security & Governance | ACLs, RBAC, quotas, data catalog, schema registry |

### Unit 3: Testing Surgewave [DEV]
*Time: ~3 hours*

Master testing patterns for Surgewave applications.

| Lesson | Topic | What You'll Build |
|--------|-------|-------------------|
| [3.1](units/unit-3/lesson-1/README.md) | Unit Testing Basics | xUnit tests with embedded broker |
| [3.2](units/unit-3/lesson-2/README.md) | Mocking & Isolation | Mock producers and consumers |
| [3.3](units/unit-3/lesson-3/README.md) | Integration Testing | End-to-end tests with fixtures |
| [3.4](units/unit-3/lesson-4/README.md) | Testcontainers | Docker-based testing patterns |
| [3.5](units/unit-3/lesson-5/README.md) | Benchmarking | Measure performance with BenchmarkDotNet |

### Unit 4: Architecture & Design [ARCH]
*Time: ~3 hours*

Design event-driven architectures with Surgewave.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [4.1](units/unit-4/lesson-1/README.md) | Topic Design | Naming conventions, granularity |
| [4.2](units/unit-4/lesson-2/README.md) | Partitioning Strategies | Key selection, hot partitions |
| [4.3](units/unit-4/lesson-3/README.md) | Event Schema Design | Versioning, compatibility |
| [4.4](units/unit-4/lesson-4/README.md) | Event-Driven Patterns | CQRS, Event Sourcing, Saga |
| [4.5](units/unit-4/lesson-5/README.md) | Capacity Planning | Sizing, retention, throughput |

### Unit 5: Stream Processing [DEV]
*Time: ~4 hours*

Master real-time stream processing with Surgewave Streams.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [5.1](units/unit-5/lesson-1/README.md) | Streams Basics | StreamsBuilder, Stream, topology |
| [5.2](units/unit-5/lesson-2/README.md) | Transformations | map, filter, flatMap, branch, merge |
| [5.3](units/unit-5/lesson-3/README.md) | Aggregations & Windowing | groupBy, count, tumbling/hopping/session windows |
| [5.4](units/unit-5/lesson-4/README.md) | Joins | Stream-stream, stream-table, table-table joins |
| [5.5](units/unit-5/lesson-5/README.md) | State Stores | KeyValueStore, WindowStore, interactive queries |

### Unit 6: Advanced Consumer Patterns [DEV]
*Time: ~3 hours*

Master advanced consumer techniques.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [6.1](units/unit-6/lesson-1/README.md) | Manual Offset Management | Commit strategies, guarantees |
| [6.2](units/unit-6/lesson-2/README.md) | Seek Operations | Offset/timestamp seeking |
| [6.3](units/unit-6/lesson-3/README.md) | Pause & Resume | Flow control, backpressure |
| [6.4](units/unit-6/lesson-4/README.md) | Custom Checkpointing | External stores, exactly-once |
| [6.5](units/unit-6/lesson-5/README.md) | Partition Assignment | Custom assignors, rebalancing |

### Unit 7: Advanced Stream Processing [DEV]
*Time: ~3 hours*

Advanced Streams features for production workloads.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [7.1](units/unit-7/lesson-1/README.md) | Processor Lifecycle Hooks | OnInit, OnClose, graceful shutdown orchestration |
| [7.2](units/unit-7/lesson-2/README.md) | Per-Processor Metrics | OTEL counters, histograms, state store metrics |
| [7.3](units/unit-7/lesson-3/README.md) | Backpressure & Flow Control | Bounded buffers, watermarks, drop strategies |
| [7.4](units/unit-7/lesson-4/README.md) | Foreign Key Table-Table Joins | FK joins, left joins, subscription tracking |
| [7.5](units/unit-7/lesson-5/README.md) | Retry & Backoff Integration | Retry policies, backoff strategies, exhaustion handling |

### Unit 8: Reactive Integration [DEV]
*Time: ~3 hours*

Integrate Surgewave with reactive programming patterns.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [8.1](units/unit-8/lesson-1/README.md) | Reactive Basics | IObservable, IObserver, subscriptions |
| [8.2](units/unit-8/lesson-2/README.md) | Surgewave as Observable | Wrapping consumers as streams |
| [8.3](units/unit-8/lesson-3/README.md) | Operators | Select, Where, Buffer, Window |
| [8.4](units/unit-8/lesson-4/README.md) | Backpressure | Flow control, buffering strategies |
| [8.5](units/unit-8/lesson-5/README.md) | Reactive Pipelines | End-to-end reactive data flows |

### Unit 9: Data Integration [DEV]
*Time: ~3 hours*

Connect external data sources and sinks with Surgewave.

| Lesson | Topic | What You'll Build |
|--------|-------|-------------------|
| [9.1](units/unit-9/lesson-1/README.md) | Connect Concepts | Source/sink connector architecture |
| [9.2](units/unit-9/lesson-2/README.md) | File Connectors | CSV/JSON file ingestion and export |
| [9.3](units/unit-9/lesson-3/README.md) | Database Connectors | SQL database integration |
| [9.4](units/unit-9/lesson-4/README.md) | Change Data Capture | Track database changes in real-time |
| [9.5](units/unit-9/lesson-5/README.md) | Custom Connectors | Build your own connectors |

### Unit 10: Schema Registry [DEV]
*Time: ~3 hours*

Manage message schemas and ensure compatibility.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [10.1](units/unit-10/lesson-1/README.md) | Schema Basics | Why schemas, schema-first design |
| [10.2](units/unit-10/lesson-2/README.md) | JSON Schema | Validation, type safety |
| [10.3](units/unit-10/lesson-3/README.md) | Schema Versioning | Evolution strategies, migrations |
| [10.4](units/unit-10/lesson-4/README.md) | Compatibility Modes | Forward, backward, full compatibility |
| [10.5](units/unit-10/lesson-5/README.md) | Schema Registry Integration | Centralized schema management |

### Unit 11: Kafka Migration
*Time: ~2 hours*

Migrate from Apache Kafka to Surgewave.

| Lesson | Topic | What You'll Build |
|--------|-------|-------------------|
| [11.1](units/unit-11/lesson-1/README.md) | Kafka Compatibility | Run Kafka apps on Surgewave |
| [11.2](units/unit-11/lesson-2/README.md) | API Wrapper | Zero-code migration |
| [11.3](units/unit-11/lesson-3/README.md) | Hybrid Deployment | Run both systems |
| [11.4](units/unit-11/lesson-4/README.md) | Production Cutover | Complete migration |

### Unit 12: Queue Semantics & AMQP
*Time: ~3 hours*

Use Surgewave as a traditional message queue with AMQP support.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [12.1](units/unit-12/lesson-1/README.md) | Log vs Queue — Two Worlds, One System | QueueView concepts, when to use queues |
| [12.2](units/unit-12/lesson-2/README.md) | Configuring QueueView | Per-topic queue semantics, visibility timeout |
| [12.3](units/unit-12/lesson-3/README.md) | AMQP Protocol Adapter | Connect RabbitMQ clients, AMQP 0-9-1 |
| [12.4](units/unit-12/lesson-4/README.md) | Visibility Timeout & Delivery Guarantees | At-least-once, dead letter queue |
| [12.5](units/unit-12/lesson-5/README.md) | Priority Lanes | Priority queues, lane routing |
| [12.6](units/unit-12/lesson-6/README.md) | Migrating from RabbitMQ | Drop-in replacement strategy |

### Unit 13: ASP.NET Core Integration [DEV]
*Time: ~3 hours*

Integrate Surgewave with ASP.NET Core applications.

| Lesson | Topic | What You'll Build |
|--------|-------|-------------------|
| [13.1](units/unit-13/lesson-1/README.md) | Hosted Services | BackgroundService, IHostedService |
| [13.2](units/unit-13/lesson-2/README.md) | Dependency Injection | Service registration, scopes |
| [13.3](units/unit-13/lesson-3/README.md) | Health Checks | Broker connectivity, consumer lag |
| [13.4](units/unit-13/lesson-4/README.md) | Middleware & Endpoints | Minimal API, correlation |
| [13.5](units/unit-13/lesson-5/README.md) | Background Processing | Producer pools, graceful shutdown |

### Unit 14: Wire Protocol [DEV]
*Time: ~4 hours*

Understand the Kafka wire protocol and build custom clients.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [14.1](units/unit-14/lesson-1/README.md) | Protocol Basics | Request/response format, API keys, versioning |
| [14.2](units/unit-14/lesson-2/README.md) | Message Format | Record batches, headers, compression, CRC |
| [14.3](units/unit-14/lesson-3/README.md) | Building a Custom Client | Socket connection, serialization, parsing |
| [14.4](units/unit-14/lesson-4/README.md) | Protocol Debugging | Wireshark, hex dumps, troubleshooting |
| [14.5](units/unit-14/lesson-5/README.md) | Protocol Extensions | Surgewave native vs Kafka, performance differences |

### Unit 15: gRPC/REST Gateway [DEV]
*Time: ~3 hours*

Use Surgewave's HTTP/gRPC gateway for web clients.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [15.1](units/unit-15/lesson-1/README.md) | Gateway Architecture | REST vs gRPC, Confluent v3 API compatibility |
| [15.2](units/unit-15/lesson-2/README.md) | REST Produce/Consume | HTTP endpoints for messages |
| [15.3](units/unit-15/lesson-3/README.md) | WebSocket Streaming | Real-time bidirectional communication |
| [15.4](units/unit-15/lesson-4/README.md) | gRPC Clients | Protobuf definitions, streaming RPCs |
| [15.5](units/unit-15/lesson-5/README.md) | Gateway Security | Authentication, rate limiting, CORS |

### Unit 16: Visual Pipeline UI [DEV]
*Time: ~3 hours*

Build data pipelines visually with Surgewave's Pipeline Designer.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [16.1](units/unit-16/lesson-1/README.md) | Pipeline UI Overview | Visual DAG editor, when to use |
| [16.2](units/unit-16/lesson-2/README.md) | Creating Pipelines | Drag-drop nodes, connections |
| [16.3](units/unit-16/lesson-3/README.md) | Connector Nodes | Source/sink/transform configuration |
| [16.4](units/unit-16/lesson-4/README.md) | DAG Patterns | Fan-out, fan-in, parallel processing |
| [16.5](units/unit-16/lesson-5/README.md) | Pipeline Monitoring | Debugging, data flow visualization |

### Unit 17: Hands-On Pipeline & Workflow Building [DEV]
*Time: ~5 hours*

Build production-ready data processing and AI pipelines from scratch.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [17.1](units/unit-17/lesson-1/README.md) | Pipeline Anatomy | PipelineDefinition model, nodes, connections, status lifecycle |
| [17.2](units/unit-17/lesson-2/README.md) | Node Types & Categories | Source, sink, transform, AI/ML, streams, workflow nodes |
| [17.3](units/unit-17/lesson-3/README.md) | Building Your First Pipeline | Step-by-step: Generator -> SQL Transform -> Stdio |
| [17.4](units/unit-17/lesson-4/README.md) | Building a RAG Pipeline | RAG chatbot from template and from scratch with 7 nodes |
| [17.5](units/unit-17/lesson-5/README.md) | Building an Agent Pipeline | Agent with MCP tool access from template |
| [17.6](units/unit-17/lesson-6/README.md) | Workflow Patterns | Human-in-the-loop, batching, routing, retry, stateful |
| [17.7](units/unit-17/lesson-7/README.md) | Pipeline Chat & Streaming | Chat API, sessions, SSE streaming, ChatDrawer |
| [17.8](units/unit-17/lesson-8/README.md) | Advanced Pipeline Features | Templates, seeder, versioning, hot-deploy, sub-pipelines |
| [17.9](units/unit-17/lesson-9/README.md) | Debugging Pipelines | Live preview, breakpoints, step-through, error inspection |

### Unit 18: AI/LLM Integration [DEV]
*Time: ~4 hours*

Integrate AI and LLM capabilities with Surgewave.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [18.1](units/unit-18/lesson-1/README.md) | OpenAI Embeddings | Generate text embeddings via Surgewave connector |
| [18.2](units/unit-18/lesson-2/README.md) | Ollama Local LLM | Private LLM inference without API costs |
| [18.3](units/unit-18/lesson-3/README.md) | Vector Databases | Store and query embeddings with Qdrant |
| [18.4](units/unit-18/lesson-4/README.md) | RAG Pipeline | Retrieval-Augmented Generation architecture |
| [18.5](units/unit-18/lesson-5/README.md) | Semantic Search | Build semantic search applications |

### Unit 19: MCP Server & AI Agents [DEV]
*Time: ~4 hours*

Build AI agents with Model Context Protocol.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [19.1](units/unit-19/lesson-1/README.md) | MCP Protocol Basics | JSON-RPC, tools, resources, prompts |
| [19.2](units/unit-19/lesson-2/README.md) | Surgewave MCP Server | Configure stdio and HTTP/SSE transports |
| [19.3](units/unit-19/lesson-3/README.md) | MCP Tools | Implement Surgewave tools for AI agents |
| [19.4](units/unit-19/lesson-4/README.md) | Claude Desktop Integration | Connect AI assistants to Surgewave |
| [19.5](units/unit-19/lesson-5/README.md) | Custom AI Agents | Build agents with Surgewave's agent framework |

### Unit 20: AI Guardrails & Agent Memory [DEV]
*Time: ~4 hours*

Secure AI applications with guardrails and persistent agent memory.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [20.1](units/unit-20/README.md) | Content Safety | IGuardrail interface, GuardrailResult, severity levels |
| [20.2](units/unit-20/README.md) | PII Detection | PiiDetector for emails, phones, SSNs, IBANs |
| [20.3](units/unit-20/README.md) | Toxicity Filtering | ToxicityFilter with keyword blocklists |
| [20.4](units/unit-20/README.md) | Prompt Injection | PromptInjectionDetector for LLM security |
| [20.5](units/unit-20/README.md) | Content Policy & Pipeline | GuardrailPipeline, DI registration |
| [20.6](units/unit-20/README.md) | Agent Memory | IAgentMemoryStore, MemoryEntry, AgentMemoryContext |
| [20.7](units/unit-20/README.md) | Tool Result Caching | CachedAgentTool, InMemoryToolResultCache |

### Unit 21: Production Operations [OPS]
*Time: ~3 hours*

Deploy and operate Surgewave in production.

| Lesson | Topic | What You'll Build |
|--------|-------|-------------------|
| [21.1](units/unit-21/lesson-1/README.md) | Clustering | Multi-broker setup |
| [21.2](units/unit-21/lesson-2/README.md) | Replication | Data durability, ISR |
| [21.3](units/unit-21/lesson-3/README.md) | Monitoring | Metrics, Prometheus/Grafana |
| [21.4](units/unit-21/lesson-4/README.md) | Security | TLS, SASL, ACLs |
| [21.5](units/unit-21/lesson-5/README.md) | Performance Tuning | Batching, compression |
| [21.6](units/unit-21/lesson-6/README.md) | Admin API | Programmatic cluster management |

### Unit 22: Observability & Troubleshooting [OPS]
*Time: ~3 hours*

Monitor, debug, and troubleshoot Surgewave applications.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [22.1](units/unit-22/lesson-1/README.md) | Structured Logging | Log correlation, trace IDs, structured formats |
| [22.2](units/unit-22/lesson-2/README.md) | Metrics Collection | Counters, gauges, histograms, Prometheus |
| [22.3](units/unit-22/lesson-3/README.md) | Distributed Tracing | OpenTelemetry, span propagation |
| [22.4](units/unit-22/lesson-4/README.md) | Debugging Techniques | Consumer lag, message inspection |
| [22.5](units/unit-22/lesson-5/README.md) | Performance Profiling | Bottleneck identification, optimization |

### Unit 23: Cloud & Kubernetes Deployment [OPS]
*Time: ~3 hours*

Deploy Surgewave to cloud and container environments.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [23.1](units/unit-23/lesson-1/README.md) | Docker Basics | Dockerfile, multi-stage builds |
| [23.2](units/unit-23/lesson-2/README.md) | Docker Compose | Multi-broker setup, networking |
| [23.3](units/unit-23/lesson-3/README.md) | Kubernetes Fundamentals | Deployments, Services, ConfigMaps |
| [23.4](units/unit-23/lesson-4/README.md) | Helm Charts | Packaging, values, templating |
| [23.5](units/unit-23/lesson-5/README.md) | Cloud Providers | AWS, Azure, GCP considerations |

### Unit 24: Disaster Recovery [OPS]
*Time: ~3 hours*

Implement disaster recovery strategies for Surgewave clusters.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [24.1](units/unit-24/lesson-1/README.md) | Backup Strategies | Topic backup, snapshots, retention |
| [24.2](units/unit-24/lesson-2/README.md) | Cross-DC Replication | MirrorMaker, async replication |
| [24.3](units/unit-24/lesson-3/README.md) | Failover Procedures | Failure detection, manual/auto failover |
| [24.4](units/unit-24/lesson-4/README.md) | Data Recovery | Restore from backup, point-in-time recovery |
| [24.5](units/unit-24/lesson-5/README.md) | DR Testing | Chaos engineering, failover drills |

### Unit 25: Multi-Tenancy [ARCH]
*Time: ~3 hours*

Build multi-tenant Surgewave applications.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [25.1](units/unit-25/lesson-1/README.md) | Tenant Isolation | Isolation levels, shared vs dedicated |
| [25.2](units/unit-25/lesson-2/README.md) | Topic Namespacing | Naming conventions, tenant prefixes |
| [25.3](units/unit-25/lesson-3/README.md) | Quota Management | Rate limiting, fair scheduling |
| [25.4](units/unit-25/lesson-4/README.md) | Access Control | Per-tenant ACLs, audit logging |
| [25.5](units/unit-25/lesson-5/README.md) | Resource Management | Partition allocation, monitoring |

### Unit 26: Tiered Storage [OPS]
*Time: ~3 hours*

Optimize storage costs with hot/warm/cold data tiers.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [26.1](units/unit-26/lesson-1/README.md) | Tiered Storage Concepts | Hot/warm/cold tiers, data lifecycle |
| [26.2](units/unit-26/lesson-2/README.md) | Local File System Tier | File-based cold storage |
| [26.3](units/unit-26/lesson-3/README.md) | S3/MinIO Integration | Cloud object storage tier |
| [26.4](units/unit-26/lesson-4/README.md) | Azure Blob Storage | Azure cloud storage integration |
| [26.5](units/unit-26/lesson-5/README.md) | Retention Policies | Lifecycle management, cost optimization |

### Unit 27: Object Store & Zero-Disk Mode [OPS]
*Time: ~3 hours*

Cloud object stores as primary storage backends.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [27.1](units/unit-27/lesson-1/README.md) | Zero-Disk Architecture | Trade-offs, when to use, architecture overview |
| [27.2](units/unit-27/lesson-2/README.md) | S3-Direct Storage | S3 config, MinIO, multipart upload, consistency |
| [27.3](units/unit-27/lesson-3/README.md) | Azure Blob Storage | Container setup, managed identity, GRS |
| [27.4](units/unit-27/lesson-4/README.md) | GCP Cloud Storage | Bucket setup, storage classes, cross-region replication |
| [27.5](units/unit-27/lesson-5/README.md) | Hybrid Storage Strategies | Tiered policies, cost modeling, migration |
| [27.6](units/unit-27/lesson-6/README.md) | Alternative Storage Engines | RocksDB, SQLite, DuckDB, LMDB, Parquet, NVMe, Arrow |

### Unit 28: Advanced Messaging
*Time: ~3 hours*

Build sophisticated applications with advanced message patterns.

| Lesson | Topic | What You'll Build |
|--------|-------|-------------------|
| [28.1](units/unit-28/lesson-1/README.md) | Custom Serialization | JSON, Protobuf serializers |
| [28.2](units/unit-28/lesson-2/README.md) | Headers & Metadata | Correlation IDs, tracing |
| [28.3](units/unit-28/lesson-3/README.md) | Error Handling | Retries, dead letter queues |
| [28.4](units/unit-28/lesson-4/README.md) | Transactions | Atomic multi-message operations |
| [28.5](units/unit-28/lesson-5/README.md) | Exactly-Once Semantics | Idempotent producers |
| [28.6](units/unit-28/lesson-6/README.md) | Compacted Topics | Log compaction, changelog topics |
| [28.7](units/unit-28/lesson-7/README.md) | Ephemeral Topics | Ring-buffer, real-time distribution |

### Unit 29: Real-World Patterns [ARCH]
*Time: ~4 hours*

Apply Surgewave to real-world application scenarios.

| Lesson | Topic | What You'll Build |
|--------|-------|-------------------|
| [29.1](units/unit-29/lesson-1/README.md) | E-Commerce Pipeline | Order processing, inventory updates |
| [29.2](units/unit-29/lesson-2/README.md) | IoT Data Ingestion | Sensor data, time-series processing |
| [29.3](units/unit-29/lesson-3/README.md) | Log Analytics | Centralized logging, aggregation |
| [29.4](units/unit-29/lesson-4/README.md) | Event Sourcing | Event store, projections, snapshots |
| [29.5](units/unit-29/lesson-5/README.md) | Microservices Communication | Async messaging, choreography |

### Unit 30: Large Message Handling [DEV]
*Time: ~3 hours*

Handle large messages efficiently in Surgewave.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [30.1](units/unit-30/lesson-1/README.md) | Message Size Limits | Configuration, trade-offs |
| [30.2](units/unit-30/lesson-2/README.md) | Compression | Codecs, compression levels |
| [30.3](units/unit-30/lesson-3/README.md) | Chunking | Message splitting, reassembly |
| [30.4](units/unit-30/lesson-4/README.md) | Claim-Check Pattern | External storage, references |
| [30.5](units/unit-30/lesson-5/README.md) | Streaming Large Data | Memory efficiency, backpressure |

### Unit 31: Message Deduplication [DEV]
*Time: ~3 hours*

Implement idempotent message processing.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [31.1](units/unit-31/lesson-1/README.md) | Deduplication Concepts | Why duplicates occur, detection strategies |
| [31.2](units/unit-31/lesson-2/README.md) | In-Memory Deduplication | LRU caches, bloom filters |
| [31.3](units/unit-31/lesson-3/README.md) | Persistent Deduplication | Database-backed stores, TTL |
| [31.4](units/unit-31/lesson-4/README.md) | Idempotent Operations | Upserts, conditional writes |
| [31.5](units/unit-31/lesson-5/README.md) | End-to-End Exactly-Once | Transactional patterns |

### Unit 32: Hybrid Cloud [ARCH]
*Time: ~4 hours*

Deploy Surgewave across multiple clouds and on-premise.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [32.1](units/unit-32/lesson-1/README.md) | Hybrid Cloud Concepts | Architecture patterns, use cases |
| [32.2](units/unit-32/lesson-2/README.md) | Multi-Region Deployment | Cross-region Surgewave clusters |
| [32.3](units/unit-32/lesson-3/README.md) | Cross-Cloud Replication | AWS ↔ Azure ↔ GCP |
| [32.4](units/unit-32/lesson-4/README.md) | Geo-Routing | Latency optimization, locality |
| [32.5](units/unit-32/lesson-5/README.md) | On-Premise + Cloud | Hybrid deployments, data sovereignty |

### Unit 33: Performance & Tuning [DEV/OPS]
*Time: ~4 hours*

Optimize Surgewave for maximum throughput and minimum latency.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [33.1](units/unit-33/lesson-1/README.md) | Performance Fundamentals | Throughput, latency, P50/P90/P99 |
| [33.2](units/unit-33/lesson-2/README.md) | Producer Optimization | Batching, compression, acks |
| [33.3](units/unit-33/lesson-3/README.md) | Consumer Optimization | Fetch size, parallelism |
| [33.4](units/unit-33/lesson-4/README.md) | Memory & GC Tuning | Memory pools, zero-copy, GC modes |
| [33.5](units/unit-33/lesson-5/README.md) | Benchmarking | BenchmarkDotNet, profiling tools |

### Unit 34: Performance Engineering [DEV]
*Time: ~4 hours*

Master zero-allocation patterns, SIMD, pooling, and regression detection.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [34.1](units/unit-34/README.md) | Performance Architecture | Hot paths, metrics, Kafka vs Surgewave comparison |
| [34.2](units/unit-34/README.md) | Zero-Allocation Patterns | Span, Memory, stackalloc, ArrayPool, ValueTask |
| [34.3](units/unit-34/README.md) | SIMD & Hardware Acceleration | CRC32C intrinsics, Vector cosine similarity |
| [34.4](units/unit-34/README.md) | Memory Pooling | BufferPool, RecyclableMemoryStream, object pooling |
| [34.5](units/unit-34/README.md) | Hot-Path Optimizations | LINQ elimination, string interning, compression bypass |
| [34.6](units/unit-34/README.md) | Regression Suite | RegressionDetector, baselines, CI workflow |
| [34.7](units/unit-34/README.md) | BenchmarkDotNet | Surgewave's 7 benchmark projects, writing benchmarks |

### Unit 35: Chaos Testing & Resilience [DEV/OPS]
*Time: ~4 hours*

Build resilient systems with proactive chaos testing.

| Lesson | Topic | What You'll Learn |
|--------|-------|-------------------|
| [35.1](units/unit-35/README.md) | Chaos Engineering Intro | Principles, motivation, ChaosEngine architecture |
| [35.2](units/unit-35/README.md) | The ChaosEngine | Fault activation, deactivation, FaultScope targeting |
| [35.3](units/unit-35/README.md) | Storage Fault Injection | ChaosLogSegment for disk, corruption, partial write faults |
| [35.4](units/unit-35/README.md) | Transport Fault Injection | ChaosRaftTransport for network and election faults |
| [35.5](units/unit-35/README.md) | Scenario Helpers | NetworkPartition, BrokerCrash, DiskFailure, Latency scenarios |
| [35.6](units/unit-35/README.md) | ChaosCluster | Multi-broker integration tests with chaos |
| [35.7](units/unit-35/README.md) | FaultSchedule | Time-delayed fault activation and deactivation |

## How to Use This Bootcamp

### Learning Path

1. **Read the README** - Understand the lesson objectives
2. **Review Concepts** - Learn the theory in INSTRUCTIONS.md
3. **Code the Exercise** - Complete the `start/` code
4. **Check Your Work** - Compare with `completed/`
5. **Experiment** - Try variations and edge cases

### Folder Structure

```
lesson-X/
├── README.md           # Lesson overview and objectives
├── INSTRUCTIONS.md     # Detailed step-by-step guide
├── start/              # Your starting point (incomplete)
│   ├── Program.cs
│   └── *.csproj
└── completed/          # Reference solution
    ├── Program.cs
    └── *.csproj
```

### Running Exercises

```bash
# Navigate to lesson
cd units/unit-1/lesson-1

# Start with the incomplete code
cd start
dotnet run

# Check completed solution
cd ../completed
dotnet run
```

## FAQ

### Do I need prior Kafka experience?
No! This bootcamp teaches streaming concepts from scratch. Kafka experience helps but isn't required.

### Can I skip lessons?
Each unit builds on previous lessons. Within a unit, lessons are sequential. You can skip entire units if you're experienced.

### How do I get help?
- Check the `completed/` solution
- Open a [GitHub Issue](https://github.com/Kuestenlogik/Surgewave/issues)
- Review [Surgewave Documentation](https://github.com/Kuestenlogik/Surgewave/blob/main/docs/)

### Can I contribute lessons?
Yes! See [CONTRIBUTING.md](https://github.com/Kuestenlogik/Surgewave/blob/main/CONTRIBUTING.md) for guidelines.

## Guided Tours

The Bootcamp includes 35 Shepherd.js interactive guided tours -- one per unit. Tours provide step-by-step walkthroughs directly in the DocFX learn site, highlighting key concepts and UI elements.

Tour definitions live in [`units/tours/`](units/tours/) as JSON files following the `tour.schema.json` schema.

To experience the tours, build and serve the learn site:

```bash
.\build-learn.ps1 -Serve -Port 8081
# Open http://localhost:8081 and click the tour button on any unit page
```

## Dependencies

- **Surgewave Core**: All units reference the main [Surgewave](https://github.com/Kuestenlogik/Surgewave) packages
- **Surgewave.AI**: Units 18, 19, 20, and 17 require [Surgewave.AI](https://github.com/Kuestenlogik/Surgewave.Ai) packages (Agents, RAG, Guardrails, Documents, MCP)
- **Surgewave.Connectors**: Unit 9 and 17 reference [Surgewave.Connectors](https://github.com/Kuestenlogik/Surgewave.Connectors) packages

## What's Next?

After completing the bootcamp:

1. **Build a Project** -- Apply your skills to a real problem
2. **Explore Samples** -- Check out [Surgewave.Samples](https://github.com/Kuestenlogik/Surgewave.Samples) for 28 ready-to-run applications
3. **Contribute** -- Help improve Surgewave or this bootcamp
4. **Browse Templates** -- Use [Surgewave.Templates](https://github.com/Kuestenlogik/Surgewave.Templates) for `dotnet new` project scaffolding

---

**Ready to start?** --> [Begin with Unit 0: Introduction](units/unit-0/lesson-1/README.md)
