# Build Order

Recommended implementation order for the 14 feature specs in `docs/specs/`, based on the
dependencies each spec's domain model declares on another (extended entities, read-only
references to another spec's fields, generated-from relationships).

## Phase 1 — Play Tetris (strictly sequential)

Each spec extends or reads the previous one's domain model, so these must land in order.

1. **[001 — Start a New Game](../specs/001-start-new-game.md)** — the foundation. Defines
   `GameSession`, `Playfield`, `ActivePiece`, `PieceQueue`, the coordinate system and the
   spawn/gravity/lock rules every later spec cites.
2. **[002 — Move, Rotate and Soft-Drop](../specs/002-move-rotate-soft-drop.md)** — extends
   `TetrominoDefinition` with the SRS rotation and wall-kick tables.
3. **[003 — Hard Drop](../specs/003-hard-drop.md)** — small, sharp; depends on locking (001)
   and composes with soft drop's cell-count handoff (002).
4. **[004 — Next Piece Preview](../specs/004-next-piece-preview.md)** — read-only view over
   `PieceQueue` (001); must not mutate it.
5. **[005 — Hold a Piece](../specs/005-hold-piece.md)** — extends `GameSession` with the hold
   slot; interacts with the preview's advance/no-advance rule (004).
6. **[006 — Line Clears and Scoring](../specs/006-line-clears-and-scoring.md)** — owns `score`
   and `linesCleared`; consumes the soft/hard-drop cell counts from 002 and 003.
7. **[007 — Levels and Speed](../specs/007-levels-and-speed.md)** — reads `linesCleared` from
   006 to drive the level and gravity curve.
8. **[008 — Game Over](../specs/008-game-over.md)** — closes the activity; reads final score
   (006), level (007) and piece count (001), and defines the hand-off to Compete.

## Phase 2 — Brand tokens (parallel, start early)

9. **[013 — Brand Styling](../specs/013-brand-styling.md)** — pure design tokens (colour,
   type scale, spacing), no gameplay dependency. Has no blocking dependency on Phase 1, so it
   can be built in parallel with it — doing it early avoids retrofitting styling once 012 and
   014 exist. Its brand-colour values are still placeholders pending Info Support's real
   brand guidelines; build against the token *names*, not the current hex values.
   (implemented — see PR #74)

## Phase 3 — Compete (sequential)

10. **[009 — Submit a Score](../specs/009-submit-score.md)** — foundation of the activity;
    defines `LeaderboardEntry` and the submission contract, and consumes 008's completed-run
    hand-off.
11. **[010 — View the Leaderboard](../specs/010-view-leaderboard.md)** — owns ranking and
    paging over 009's `LeaderboardEntry`.
12. **[011 — Highlight My Run](../specs/011-highlight-my-run.md)** — depends on both: matches
    on the `entryId` from 009 and renders inside 010's ranked table.

## Phase 4 — Thin UI shells (last)

These are wiring layers over everything built above, so they come last.

13. **[012 — Landing Page](../specs/012-landing-page.md)** — consumes 013's tokens; links to
    the game (001) and the leaderboard (010).
14. **[014 — Controls Panel](../specs/014-controls-panel.md)** — consumes 013's tokens, and is
    explicitly *generated from* the binding definitions in 002, 003 and 005 — it cannot start
    until those exist as real code to read from, not just as specs.

## Summary

```
001 → 002 → 003 → 004 → 005 → 006 → 007 → 008 ─┐
                                                 ├─→ 012
013 (parallel, any time) ───────────────────────┤
                                                 └─→ 014
009 → 010 → 011 ────────────────────────────────┘
```
