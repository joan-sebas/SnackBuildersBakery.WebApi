# Snacks Builder Web API

Backend API for bakery order processing and kitchen scheduling.

## Status

Bootstrap in progress. This repository is being built in milestone-based tasks.

## Run

Execution commands and container workflow are completed in milestone M6.

Planned runtime entrypoint:

```bash
docker compose up --build
```

## Repository Structure

- `src/Domain`: core business model and scheduling policy.
- `src/Application`: use cases and orchestration ports.
- `src/Infrastructure`: persistence and external adapters.
- `src/Api`: HTTP surface with minimal APIs.
- `tests`: per-layer test projects.
- `docs`: architecture records, assumptions, and diagrams.

## Documentation

- [Architecture ADR](docs/adr/0001-stack-and-architecture.md)
- [Assumptions](docs/assumptions.md)
- [Layered Architecture Diagram](docs/diagrams/architecture.md)
- [AI Usage](AI_USAGE.md)
