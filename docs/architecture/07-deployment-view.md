# 7. Deployment View

## Infrastructure Level 1

There is currently no production deployment. The project runs only as a local
development process, started with `dotnet run` (or `dotnet build` followed by
running the produced binary) from `Properties/launchSettings.json`. A single
ASP.NET Core 10.0 Kestrel process hosts the whole application — Razor Pages,
game services, and the in-memory game session store all run in-process, with
no external dependencies (no database, no cache server, no message broker).

Two local endpoints are exposed, per the `http` and `https` profiles in
`launchSettings.json`:

- `http://localhost:5207`
- `https://localhost:7257` (falls back to `http://localhost:5207` as a secondary binding)

`Program.cs` always calls `UseHttpsRedirection()`. `UseExceptionHandler("/Error")`
and `UseHsts()` are only registered when `ASPNETCORE_ENVIRONMENT` is **not**
`Development` — in practice, during local `dotnet run` (which sets
`ASPNETCORE_ENVIRONMENT=Development` in both launch profiles), neither of those
two middlewares is active. `app.UseSession()` backs the anonymous,
cookie-keyed session id (see NFR-007 in the requirements) used to look up each
browser tab's game in the in-memory store.

```
                        developer machine
   +-----------------------------------------------------------+
   |                                                             |
   |   Browser  <---HTTP :5207---->  +---------------------+    |
   |   (client)  <--HTTPS :7257-->   |  Kestrel process     |    |
   |                                 |  (dotnet run)         |    |
   |                                 |                       |    |
   |                                 |  - Razor Pages        |    |
   |                                 |  - Game services      |    |
   |                                 |  - IGameSessionStore  |    |
   |                                 |    (ConcurrentDictionary, |  |
   |                                 |     in-process memory)|    |
   |                                 +---------------------+    |
   |                                                             |
   +-----------------------------------------------------------+

   Single node, single process, no external dependencies.
```

| Node | Description |
| ---- | ----------- |
| Developer machine (Kestrel process) | The only deployment node that exists today. Runs the ASP.NET Core app via `dotnet run`/`dotnet build`, serving both the HTTP (`5207`) and HTTPS (`7257`) endpoints defined in `Properties/launchSettings.json`, with `ASPNETCORE_ENVIRONMENT=Development`. All application state, including in-progress Tetris games (`Services/GameSessionStore.cs`, a `ConcurrentDictionary<string, GameSession>`), lives in this single process's memory and is lost whenever the process restarts. |

## Infrastructure Level 2

Not yet applicable. No production hosting environment has been established:
there is no Dockerfile, no `docker-compose` file, and no CI/CD pipeline
(e.g. no `.github/workflows/*`) in the repository as of this writing. There is
no cloud provider, reverse proxy, container orchestrator, or load balancer
configured or decided upon. This is presently a training/learning project run
exclusively on a developer's local machine; a Level 2 breakdown of deployment
nodes will be added once (and if) a real hosting target is chosen.

Because game state lives only in a single process's memory
(`Services/GameSessionStore.cs`), any future move beyond a single node would
need to address horizontal scaling and process-restart behavior — e.g. via
sticky sessions/session affinity or an externalized, shared session store.
Today none of that exists, so scaling out or redeploying without data loss is
not currently possible. This is tracked as an open risk; see also
`08-concepts.md` / the risks-and-technical-debt doc if one exists, since it
affects more than just deployment topology.

## Mapping: Building Blocks to Infrastructure

Everything (Razor Pages, application services, and the domain/game logic)
runs in the single ASP.NET Core process described above — there is nothing to
meaningfully split across nodes yet.

| Building Block | Infrastructure Node |
| -------------- | -------------------- |
| Entire application (Razor Pages, `Services/*` incl. `GameSessionStore`, domain/game logic) | Developer machine (Kestrel process via `dotnet run`) |
