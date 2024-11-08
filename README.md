# Semantic Kernel - Document Uploader

7 November 2024

## Overview

Processes documents using Azure Document Intelligence including:

- PDF
- Doc

Breaks document content into chunks, can configure:

- number of words per chunk
- number of words that overlap across two adjacent chunks

Choice of Vector Stores including:

- Azure AI Search
- Azure CosmosDB NoSQL
- Redis
- In-memory vector store

Choice of Embedding Models including:

- Azure OpenAI e.g. text-embedding-ada-002
- Huggingface e.g. sentence-transformers/all-MiniLM-L6-v2
- Ollama e.g. nomic-embed-text
- Bert Onxx e.g. bge-micro-v2

## Configuration

Options in appsettings.json, should be self explanatory.

### Azure

This script may be useful to create Azure OpenAI, Document Intelligence and AI Search: [link](<https://github.com/markharrison/aidemo-create>)

### Redis

Docker command to start Redis container:

```bash
docker run -d --name redis-stack -p 6379:6379 -p 8001:8001 redis/redis-stack:latest
```

### Ollama

Docker command to start Ollama container:

```bash
docker run -d -v ollama:/root/.ollama -p 11434:11434 --name ollama ollama/ollama
```

Need to download models by using Exec on the container  - for example:

```bash
ollama pull nomic-embed-text
```
