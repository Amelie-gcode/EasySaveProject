# EasySave log centralization (Docker)

This stack runs a small HTTP service that aggregates logs from every EasySave client into **one daily JSON file** on the Docker host (`yyyy-MM-dd.json`). Each entry includes `machineName` and `userName` so operators can tell which PC and user produced the event.

## Start the centralizer

From the repository root:

```bash
docker compose up -d --build
```

- Health check: `http://localhost:5080/health`
- Log ingest: `POST http://localhost:5080/api/logs`
- Centralized files: Docker volume `easysave-centralized-logs` (path inside container: `/data/logs`)

## Client configuration

In EasySave settings (WPF or `appsettings.json` under `%AppData%\EasySave`):

| Setting | Values |
|--------|--------|
| `LogDestination` | `Local`, `Centralized`, or `Both` |
| `LogCentralizerUrl` | e.g. `http://localhost:5080` (or your server hostname) |

When `Centralized` or `Both` is selected, each backup event is sent in real time to the Docker service without blocking the backup if the network is unavailable.
