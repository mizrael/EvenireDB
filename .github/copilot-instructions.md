# Copilot Instructions for EvenireDB

EvenireDB is a proof-of-concept stream-based database engine for Event Sourcing, written in C# targeting .NET 10. It stores events in append-only streams identified by a `StreamId` (Guid key + `StreamType` string).

## Build, Test, Lint

The solution file is at `src/EvenireDB.sln`. A test-only solution filter is at `src/EvenireDb.Tests.slnf`.

```shell
# Build
cd src && dotnet build

# Run all tests (from repo root)
cd src && dotnet test ./EvenireDb.Tests.slnf --no-build -m:1 -s ../tests.runsettings

# Run a single test project
dotnet test tests/EvenireDB.Tests/EvenireDB.Tests.csproj

# Run a single test by name
dotnet test tests/EvenireDB.Tests/EvenireDB.Tests.csproj --filter "FullyQualifiedName~EventsWriterTests.AppendAsync_should_succeed"
```

Target framework version is centralized in `Versions.props` (`net10.0`). Shared build properties live in `Directory.Build.props`.

## Architecture

### Project Structure

- **EvenireDB** — Core domain library. Contains event storage engine: readers, writers, caching, persistence, validation, background workers. Has `AllowUnsafeBlocks` enabled for low-level binary serialization.
- **EvenireDB.Common** — Shared types used by both server and client: `StreamId`, `StreamType`, `StreamPosition`, `Direction`, `ErrorCodes`.
- **EvenireDB.Grpc** — Generated gRPC stubs from `src/Protos/events.proto`.
- **EvenireDB.Server** — ASP.NET Core host. Configures Kestrel with dual ports (HTTP on configurable port, gRPC on a separate port). Uses Minimal APIs with API versioning.
- **EvenireDB.Client** — NuGet-published client library supporting both HTTP and gRPC transports. Uses Polly for retry policies with decorrelated jitter backoff.
- **EvenireDB.AdminUI** — Blazor-based administration UI.

### Data Flow

1. **Write path**: Client → HTTP route or gRPC service → `EventsWriter` → validates, generates `EventId`, adds to in-memory `StreamsCache` (LRU) → writes to `Channel<IncomingEventsBatch>` → `IncomingEventsPersistenceWorker` (BackgroundService) reads from channel → `EventsProvider` persists to disk via `HeadersRepository` + `DataRepository`.
2. **Read path**: Client → HTTP route or gRPC service → `EventsReader` → `StreamsCache` (loads from disk on cache miss via `EventsProvider`) → returns events as `IAsyncEnumerable<Event>`.

### Storage Format

Events are persisted as two binary files per stream extent:
- `{streamKey}_{extentNumber}_headers.dat` — Fixed-size header records (`RawHeader` struct)
- `{streamKey}_{extentNumber}_data.dat` — Raw event data bytes

Files are organized under `{DataFolder}/{streamType}/`. The `ExtentsProvider` manages file paths and directory structure.

### Configuration

Server settings are bound from the `Evenire` configuration section into `EvenireServerSettings`. Key settings: `MaxPageSize`, `MaxEventDataSize`, `DataFolder`, `MaxInMemoryStreamsCount`, `MaxAllowedAllocatedBytes`, transport ports.

## Key Conventions

### Operation Results

Write operations return `IOperationResult`, which is either `SuccessResult` or `FailureResult` (with an `ErrorCodes` int code and message). These are **not** exceptions — pattern match on the result type:

```csharp
var result = await writer.AppendAsync(streamId, events);
// result is SuccessResult or FailureResult { Code, Message }
```

### Logging

Uses compile-time source-generated logging via `[LoggerMessage]` attributes in `LogMessages.cs`. All log messages should follow this pattern with defined `EventIds`.

### DI Registration

- Server services are registered via `IServiceCollectionExtensions.AddEvenire()` in the core library.
- Client services are registered via `ServiceCollectionExtensions.AddEvenireDB(config)` in the client library.
- Channel-based communication (`Channel<IncomingEventsBatch>`) connects the write path to the background persistence worker.

### Internal Visibility

The core `EvenireDB` project exposes internals to `EvenireDB.Tests` and `EvenireDB.Tools.Benchmark` via `InternalsVisibleTo`. Also exposes to `DynamicProxyGenAssembly2` for NSubstitute mocking of internal types.

### Domain Types

- `StreamId` — `readonly record struct` composed of `Guid Key` + `StreamType Type`. Uses `[SetsRequiredMembers]`.
- `StreamType` — `readonly record struct` wrapping a string with implicit conversions to/from `string`. Implements `IParsable<StreamType>`.
- `Event` — `record` extending `EventData`, adding `EventId`.
- `EventId` — Composite of timestamp + sequence number.

### Testing

- **Framework**: xUnit with `[Fact]` and `[Theory]` attributes.
- **Mocking**: NSubstitute.
- **Integration tests**: `EvenireDB.Server.Tests` uses `WebApplicationFactory<Program>` via `TestServerWebApplicationFactory` (creates temp data directories, randomized ports). Tests use `IClassFixture<ServerFixture>`.
- **Test naming**: `MethodName_should_expected_behavior_when_condition` (e.g., `AppendAsync_should_fail_when_stream_version_mismatch`).
- **Coverage**: Coverlet with `tests.runsettings` configuration.

### HTTP API

Versioned REST API under `/api/v{version}/streams`:
- `GET /api/v1/streams` — List streams
- `GET /api/v1/streams/{streamType}/{streamKey:guid}` — Get stream info
- `DELETE /api/v1/streams/{streamType}/{streamKey:guid}` — Delete stream
- `GET /api/v1/streams/{streamType}/{streamKey:guid}/events?pos=0&dir=Forward` — Read events
- `POST /api/v1/streams/{streamType}/{streamKey:guid}/events?version=N` — Append events

### gRPC API

Defined in `src/Protos/events.proto` with `EventsGrpcService` offering `Read` (server streaming) and `Append` (unary) RPCs.

### Docker

- `Dockerfile.server` / `Dockerfile.adminui` — Separate images for server and admin UI.
- `docker-compose.yml` — Runs both services. Server on port 32154, Admin UI on 32160.
- Build images via `scripts/dockerize.ps1 -version <version> -env local|dev`.

### Benchmarking

Run benchmarks and compare against committed baselines:

```shell
# Compare current branch against baseline
./scripts/benchmark-compare.sh

# Capture a new baseline (run on main before making changes)
./scripts/benchmark-compare.sh --update-baseline

# Run only a specific benchmark
./scripts/benchmark-compare.sh --filter "*EventsProviderRead*"

# Custom regression threshold (default: 10%)
./scripts/benchmark-compare.sh --threshold 15
```

Baseline JSON files are committed in `benchmarks/baselines/`. Update them when merging perf improvements to main.
