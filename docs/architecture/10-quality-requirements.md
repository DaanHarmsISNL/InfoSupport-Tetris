# 10. Quality Requirements

## Quality Tree

<!-- Hierarchical breakdown of quality goals into concrete scenarios -->

```
Quality
├── Responsiveness / timing accuracy
│   ├── QS-01 Key press to on-screen move/rotation (NFR-001, spec 002)
│   ├── QS-02 Gravity interval held within ±50 ms per step (NFR-002, spec 001)
│   └── QS-03 Hold resolves before the same-step gravity tick (DR-72, spec 005)
├── Testability
│   ├── QS-04 Domain scenarios run headlessly with injectable time/seed (NFR-006, spec 001; NFR-006, spec 002)
│   └── QS-05 Every spec's acceptance scenarios map to an xUnit test
├── Maintainability / traceability
│   └── QS-06 FR-/DR-/EC- IDs in a spec resolve to code via comments
└── Privacy by default / simplicity
    └── QS-07 Starting and playing a game persists no personal data (NFR-007)
```

## Quality Scenarios

<!-- Concrete, measurable quality scenarios -->

| ID    | Quality Goal                        | Scenario                                                                                                                                 | Expected Response                                                                                                                                                                                             | Priority |
| ----- | ------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- |
| QS-01 | Responsiveness                       | Player presses a move/rotate key while a piece is actively falling.                                                                      | The POST handler applies the command and returns 204 (no full page render) within the latency bound set by NFR-001 of spec 002 (≤50 ms p95 / ≤100 ms p99 over 500 presses); the new position is visible on the next meta-refresh. | High     |
| QS-02 | Timing accuracy                      | A piece descends under gravity over a multi-minute session with no player input.                                                         | Per-step descent stays within ±50 ms of the nominal `gravityInterval` (NFR-002, spec 001), and accumulated drift over a 10-minute session stays under one cell (NFR-004, spec 001).                           | High     |
| QS-03 | Timing accuracy / ordering           | Player holds the active piece in the same tick that the lock-delay timer or gravity would otherwise fire (DR-72, spec 005).               | `GameSession` resolves the hold swap before advancing gravity/lock in that tick, so the hold always takes effect instead of being pre-empted by an expiring lock — asserted in `HoldPieceTests.cs`.            | High     |
| QS-04 | Testability                          | A new scoring, gravity-curve, or kick-table rule needs to be verified against a spec's acceptance scenarios.                              | The rule lives in framework-free `Domain/` code with injectable game time and a seeded piece sequence, so every scenario in `docs/specs/*.md` §4 is expressible as an xUnit test with no ASP.NET Core host, browser, or real wall-clock delay required. | High     |
| QS-05 | Testability                          | A spec (e.g. `docs/specs/007-levels-gravity-curve.md`) is implemented.                                                                   | A corresponding test file exists under `tests/infosupport-tetris.Tests/` covering its scenarios, matching the project's one-test-file-per-spec pattern.                                                       | Medium   |
| QS-06 | Maintainability / traceability       | A requirement (FR-, DR-, or EC- ID) in a spec changes or is questioned during review.                                                     | Grepping that ID across `Domain/`, `Services/`, and `Pages/*.cshtml.cs` locates the implementing code via an inline comment referencing the same ID (e.g. DR-72 in `Index.cshtml.cs` and `GameSession.cs`).    | Medium   |
| QS-07 | Privacy by default / simplicity      | A player opens the page and plays a full game without ever signing up or entering a name.                                                 | No personal data is collected, transmitted, or persisted at any point (NFR-007, spec 001); the only session identifier is the anonymous ASP.NET Core session cookie, and state lives only in-memory (`GameSessionStore.cs`). | High     |
