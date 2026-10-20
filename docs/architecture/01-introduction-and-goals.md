# 1. Introduction and Goals

## Requirements Overview

Tetris, playable entirely in a desktop browser, built as an ASP.NET Core 10 Razor Pages application
(`infosupport-tetris`). A colleague on a coffee break opens a single page, starts a game with one
action — no sign-up, no accounts, no personal data collected — and plays a standard 10×20 Guideline
Tetris: a fair 7-bag piece sequence, gravity, movement/rotation with SRS kicks, hard drop, hold, line
clears and scoring, level progression with a gravity speed curve, and a game-over summary (final
score, lines, level, pieces) with an immediate "play again" restart. Game state lives server-side in
an in-memory store keyed by the anonymous ASP.NET Core session cookie (`Services/GameSessionStore.cs`,
`Domain/GameSession`); the page is server-rendered with no SPA/JS framework, driving the game loop via
a meta-refresh cycle and small POST handlers for input. Spec 009 (submit score) has a backend-only
JSON endpoint and domain model (`OnPostSubmitScore`, `Domain/LeaderboardEntry.cs`, `Services/ScoreSubmissionService.cs`)
but no UI page yet; specs 010–011 (view leaderboard, highlight my run) are planned, not-yet-implemented scope.

The dominant driving force is that this is a **training/learning project**: it is being built
incrementally, spec by spec (`docs/specs/001`–`014`), from an unmodified Razor Pages scaffold, with
each feature spec written and implemented before the next begins, and with heavy traceability
(FR-/DR-/EC- comments in code linking back to the specs). The architecture should stay simple enough
to build this way without over-engineering for scale or longevity it does not need.

## Quality Goals

<!-- Top 3-5 quality goals for the architecture, ordered by priority -->

| Priority | Quality Goal | Motivation |
| -------- | ------------ | ---------- |
| 1 | Playability / responsiveness | The core loop (gravity, input, lock delay) must feel like real Tetris despite running server-side with no client JS framework — specs 001 and 002 set explicit timing NFRs (e.g. gravity drift under one cell over 10 minutes, ≥30 fps rendering, descent within ±50 ms of the nominal interval). A game that feels laggy or jittery fails its one purpose. |
| 2 | Simplicity / learnability | Built incrementally as a spec-driven training exercise (one user story at a time, from a bare scaffold); the design must stay easy to read, extend and reason about one spec at a time rather than optimise for scale this project will never need. |
| 3 | Testability | Domain logic (`Domain/`) is kept pure and framework-free so it can be unit tested headlessly (xUnit, `tests/infosupport-tetris.Tests/`) with seeded randomness and injectable game time — every spec's acceptance scenarios and domain rules (DR-xx) are expected to map to automated tests. |
| 4 | Privacy by default / no persisted identity | No accounts, no login, no personal data at any point in normal play (NFR-007 in spec 001); sessions are anonymous, in-memory and tied only to a session cookie. Only the future leaderboard feature (specs 009-011) introduces a player name, and only at explicit submission time. |
| 5 | Traceability / requirements discipline | Code is annotated throughout with references back to functional requirements, domain rules and edge cases (FR-xxx, DR-xxx, EC-xxx) defined in `docs/specs/*.md`, so the implementation's intent and rationale stay auditable as the project grows spec by spec. |

## Stakeholders

<!-- Key stakeholders and their expectations -->

| Role/Name | Expectations |
| --------- | ------------ |
| Player (Info Support colleague) | Can start playing within seconds, with no sign-up or data entry, on a desktop browser with a keyboard; expects a faithful, responsive Guideline Tetris experience and (once implemented) a way to see their score on a leaderboard. |
| Developer / maintainer (Daan Harms, `DaanHarmsISNL`) | Uses this repo to learn and demonstrate a spec-driven, incremental build process; expects the codebase to stay clean, well-tested and traceable to its specs as each new feature is added one at a time. |
| Training / workshop reviewers | May inspect the repo, specs and architecture docs as an example of spec-driven development and arc42 documentation practice; expect the docs to genuinely reflect the current state of the code, not aspirational or stale content. |
