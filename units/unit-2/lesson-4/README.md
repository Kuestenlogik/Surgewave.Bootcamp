# Lesson 2.4: Pipeline Management

The Pipeline section integrates with the Visual Pipeline editor and adds management features including chat integration, templates, and versioning.

## Learning Objectives

- Manage pipelines through the Control UI
- Use chat integration with chat-enabled pipelines
- Create pipelines from pre-built templates
- Understand pipeline versioning and diff view

## Pipeline List

The pipeline list shows all pipelines with columns for name, status, node count, throughput, and last modified time. Pipelines can be started, stopped, and edited from this view.

## Chat Integration

When the broker detects a `ChatEndpoint` on a pipeline, a chat icon appears. Surgewave provides two chat experiences:

- **Chat Drawer**: A slide-out panel accessible from the pipeline list or detail page
- **Dedicated Chat Page**: A full-page chat experience navigable from the Integration menu

## Pipeline Templates

The Control UI ships with pre-built pipeline templates:

| Template | Nodes | Description |
|----------|-------|-------------|
| RAG Chatbot | 6 | Source, Chunker, Embedding, VectorStore, Retriever, Chat |
| Document Indexer | 4 | Source, Parser, Embedding, VectorStore |
| Agent with Tools | 5 | Source, Agent, ToolExecutor, Memory, Chat |

Templates are available from the "Create New" pipeline dialog and can be customized after creation.

## AI Pipeline Seeder

For testing and development, the AI Pipeline Seeder can generate sample pipelines with mock data via the `POST /api/pipelines/seed` endpoint.

## Pipeline Versioning

Every pipeline change creates a new version. The UI provides:

- **Version History**: List of all versions with timestamps and change summaries
- **Diff View**: Side-by-side comparison of any two versions showing added/removed/changed nodes
- **Restore**: Roll back to any previous version with a single click

---

[Back to Unit 2](../README.md)
