# EasySaveProject — Release v3

This repository contains EasySave, a cross-platform backup application implemented in C# (.NET 10 and .NET Framework 4.7.2). This document describes the v3 release, highlights new features, upgrade notes, and basic usage.

## Release v3 — Highlights

- Multithreaded backup engine: jobs and file transfers run concurrently using the .NET thread pool to improve throughput on multi-core machines.
- Priority files: the system supports file priority rules; priority files are processed first across jobs and non-priority files wait cooperatively.
- Improved real-time status: unified state.json contains one current entry per job, updated in real time for monitoring and UI binding.
- File-level streaming copy with progress: large files are streamed and progress is reflected in the state file and WPF UI.
- Controlled concurrency for large files: a semaphore limits simultaneous large-file transfers to avoid saturating I/O.
- External encryption integration: optional post-transfer encryption using an external CryptoSoft executable with robust fallbacks and error handling.
- Centralized logging (optional): logs can be sent to a Docker-hosted centralizer or written locally in JSON/XML formats.
- WPF MVVM improvements: live job view, pause/resume/cancel controls, and per-job progress details.

## Breaking changes / Upgrade notes

- State format: v3 writes current job states as a JSON array (one entry per job) to `%AppData%/EasySave/state.json`. Consumers that expect a historic append-only format must be updated.
- Concurrency: many operations now run asynchronously; if you integrate via code, use the new async APIs (ExecuteJob / ExecuteAll return Task).
- Settings keys: `LogFormat` and `LogDestination` are respected at runtime; ensure your saved appsettings.json contains valid values.

## Quick start

1. Download and execute EasySave.exe.
2. Configure settings (WPF UI or `%AppData%/EasySave/appsettings.json`):

```json
{
  "Language": "EN",
  "LogFormat": "JSON",          // or "XML"
  "LogDestination": "Local",   // Local, Centralized, Both
  "LogCentralizerUrl": "http://localhost:5080",
  "EncryptedExtensions": [ ".enc" ],
  "PriorityExtensions": [ ".db", ".bak" ],
  "MaxParallelSize": 10240       // KB threshold to treat file as "large"
}
```

3. Run the WPF UI or Console. Create jobs, then Start / Execute All.

## State file (real-time status)

- Location: `%AppData%/EasySave/state.json`
- Structure: array with one object per job. Each object contains at minimum:
  - name
  - lastUpdate (ISO local timestamp)
  - status (Active, Paused, Completed, Cancelled, Error, Inactive)
  - totalFiles, totalSize
  - progress (0..100)
  - filesRemaining, sizeRemaining
  - currentSource, currentDestination

Example:

```json
[
  {
	"name": "DailyBackup",
	"lastUpdate": "2026-05-14 15:22:10",
	"status": "Active",
	"totalFiles": 1234,
	"totalSize": 987654321,
	"progress": 42,
	"filesRemaining": 715,
	"sizeRemaining": 572345678,
	"currentSource": "C:\\Data\\foo.db",
	"currentDestination": "\\\\BACKUP01\\Daily\\foo.db"
  }
]
```

## Centralized logs (Docker)

v3 includes an optional lightweight centralizer stack (see `/docker/README.md`) that aggregates logs from multiple clients into one daily JSON file on the host (yyyy-MM-dd.json). Configure `LogDestination` to `Centralized` or `Both` and set `LogCentralizerUrl` to enable.


Health: `http://localhost:5080/health` — ingest endpoint: `POST /api/logs`.

## License & Authors

- Authors: EasySave Team
- License: MIT (see LICENSE file)
