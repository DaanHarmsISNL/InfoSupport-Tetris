# Feature Specification: Levels and the Gravity Speed Curve

<!--
  TEMPLATE INSTRUCTIONS
  =====================
  Fill in each section focusing on WHAT the feature does and WHY — not HOW it should
  be implemented.

  Usage:
  - One spec per feature or functional slice
  - Store in docs/specs/ and version-control alongside your code
  - Use [NEEDS CLARIFICATION: question] markers for unresolved decisions (max 3)
  - Remove optional sections that don't apply — don't leave them as N/A
  - Reference your arc42 architecture docs where relevant rather than duplicating them
-->

## 1. Overview

| Field           | Value                                                                                        |
| --------------- | -------------------------------------------------------------------------------------------- |
| Feature ID      | 007                                                                                            |
| Status          | Draft                                                                                          |
| Author          | Daan Harms                                                                                     |
| Created         | 2026-10-20                                                                                     |
| Last updated    | 2026-10-20                                                                                     |
| Epic / Parent   | Play Tetris (story map activity) — `docs/product/story-map.md`                                 |
| Arc42 reference | Sections 1, 4, 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders. |

> **Note on this spec.** 007 is written entirely in the vocabulary of **spec 001**
> (`docs/specs/001-start-new-game.md`, §5 Domain Model). Entity names, enum members and `DR-nn` rule IDs
> defined there are reused verbatim and are not restated here. 001 explicitly hands two things to this
> spec: "Story 007 owns how [`level`] increases" (§5.1, `GameSession.level`) and, in **Gravity Step
> (DR-14)**, "the Guideline curve `(0.8 - (level - 1) × 0.007) ^ (level - 1)` seconds per row yields
> exactly 1.000 s at level 1; the curve itself is **story 007's** concern." This spec discharges both.
>
> **Ruleset.** Every detail not otherwise constrained by the story map is resolved against the modern
> **Tetris Guideline**, per the standing project decision recorded in 001.
>
> **Borrowed, not redefined.** `totalLinesCleared` is owned by **story 006** and is the single source of
> truth for how many lines the session has cleared. 007 *reads* it and derives `level` from it. 007 does
> not define, increment or constrain `totalLinesCleared`, and does not define scoring.

### 1.1 Problem Statement

A game that falls at one cell per second forever is not Tetris — it is a tidying exercise. Specs 001–006
give a colleague a complete, correct, scoring game, but one whose difficulty never moves: the tenth
minute plays exactly like the first, so a competent player simply does not lose and the all-time
leaderboard (stories 009–011) degenerates into a measure of patience rather than skill. The game needs a
rising difficulty curve tied to the player's own progress, so that every run has a natural, earned end.

### 1.2 Goal

A colleague's skill is answered by escalating pressure. Every ten lines cleared promotes the session one
level, up to a cap of 15, and each level makes gravity measurably faster along the Tetris Guideline
curve — from a leisurely 1.000 second per cell at level 1 to 7 milliseconds per cell at level 15, where
the piece is effectively on the floor the moment it spawns and the 0.500 second lock delay is the only
thinking time left. The current level is on screen throughout, a promotion is immediately noticeable, and
the change never stalls, pauses or interrupts play.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec.

- **Line detection, line clearing, `totalLinesCleared` and scoring** — all **story 006**. This spec reads
  `totalLinesCleared` and hooks its level re-evaluation into 006's line-clear resolution; it defines
  neither. In particular, whether `level` multiplies the score (the Guideline's `base × level` scoring)
  is **006's** decision and is not specified here.
- **A selectable starting level.** Guideline-optional. Every session starts at level 1 per
  **Fresh Session State (DR-23)**. No level-select screen, no URL parameter, no debug override in the
  shipped build.
- **A "lines to next level" goal readout.** Only the current `level` is displayed (FR-017). A separate
  countdown-to-promotion widget is a different, larger HUD story.
- **Variable-goal level systems** (Guideline's optional `levelUp` / variable-goal mode, where the lines
  needed per level grow with the level). This spec uses the fixed-goal system: ten lines per level,
  always.
- **Difficulty settings of any kind** — no easy/hard mode, no curve tuning in the UI, no configurable cap.
  The curve and the cap are fixed product decisions (§9.2).
- **Changing the lock delay, the lock reset budget or the entry delay with level.** **Lock Delay (DR-16)**
  stays 0.500 s, **Lock Reset Budget (DR-17)** stays 15 and **Spawn Immediacy (DR-20)** stays zero at every
  level (DR-99). "20G" / master-mode style tightening is out of scope.
- **Sound effects.** The project has no audio subsystem and this spec does not introduce one. The
  level-up cue is visual only (FR-019).
- **Game over presentation, including showing the level reached** — **story 008**. This spec makes
  `level` available to 008 (FR-020) but renders nothing at game over.
- **Leaderboard use of level** — stories 009–011. The leaderboard is all-time high *score* only
  (story map).
- **Ghost piece, pause, accounts/authentication, mobile and touch support** — out of scope project-wide
  (story map).

## 2. User Stories

### US-001: Difficulty that climbs with me

**As a** player,
**I want** the pieces to fall faster the further I get,
**so that** the game keeps getting harder and every run ends in a real defeat rather than boredom.

### US-002: Levels earned by clearing lines

**As a** player,
**I want** my level to come from the lines I have cleared and nothing else,
**so that** promotion is something I earn by playing well, not something that happens on a timer.

### US-003: Knowing what level I am on

**As a** player,
**I want** to see my current level at all times and to notice the moment it goes up,
**so that** I can feel my progress and brace for the step change in speed.

### US-004: A level-up that never costs me a piece

**As a** player,
**I want** the speed change to land cleanly between pieces,
**so that** a promotion never yanks the piece I am mid-way through placing out from under me and never
freezes the game for an animation.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) are defined in §5.4;
rules numbered DR-01 … DR-27 are 001's and are cited, not restated._

| ID     | Requirement                                                                                                                                                                                                                       | Priority | User Story |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall derive the `GameSession.level` from story 006's `totalLinesCleared` as `floor(totalLinesCleared / 10) + 1`, clamped to `levelCap` (DR-90, DR-94).                                                                   | Must     | US-002     |
| FR-002 | The system shall start every session at level 1 with `totalLinesCleared` 0, and shall return to level 1 when a new game begins, per **Fresh Session State (DR-23)**.                                                                 | Must     | US-002     |
| FR-003 | The system shall re-evaluate the level only during story 006's line-clear resolution, and shall leave it unchanged at every other moment of play (DR-91, DR-95).                                                                     | Must     | US-002     |
| FR-004 | The system shall never decrease the level within a session (DR-91).                                                                                                                                                                 | Must     | US-002     |
| FR-005 | The system shall advance the level by at most one per lock, since a single lock clears at most four lines and four is fewer than the ten-line goal (DR-90).                                                                          | Must     | US-002     |
| FR-006 | The system shall continue to accumulate `totalLinesCleared` and the score after `levelCap` is reached, while holding the level and the gravity interval at their level-15 values (DR-94).                                            | Must     | US-002     |
| FR-007 | The system shall derive `GameSession.gravityInterval` from the current level as `(0.8 - ((level - 1) × 0.007)) ^ (level - 1)` seconds, satisfying **Gravity Step (DR-14)** (DR-92).                                                  | Must     | US-001     |
| FR-008 | The system shall use, for every level 1–15, exactly the effective interval given in the normative table in §5.3, which is the curve value rounded to the nearest whole millisecond (DR-93).                                          | Must     | US-001     |
| FR-009 | The system shall produce an interval of exactly 1.000 second at level 1, preserving the level-1 fall rate that 001 fixed in DR-14 and FR-013 (DR-92).                                                                                | Must     | US-001     |
| FR-010 | The system shall produce a strictly decreasing interval across levels 1 → 15, so that every promotion is a measurable speed increase and no level is slower than the one before it (DR-92).                                          | Must     | US-001     |
| FR-011 | The system shall continue to advance fall and lock timers from elapsed wall-clock time rather than frame counts at every level, per **Gravity Advances on Elapsed Time (DR-26)**.                                                    | Must     | US-001     |
| FR-012 | The system shall, when the elapsed time in one update step covers more than one gravity interval, apply the descents as repeated single-cell steps — each one validated against DR-03 and DR-04 and halted at grounding (DR-97).     | Must     | US-001     |
| FR-013 | The system shall derive story 002's soft-drop rate from the current `gravityInterval` as one twentieth of it, so soft drop accelerates with the level; hard drop (story 003) remains instantaneous and level-independent (DR-96).    | Must     | US-001     |
| FR-014 | The system shall keep the 0.500 second lock delay (DR-16), the 15-reset lock budget (DR-17) and the zero entry delay (DR-20) constant at every level (DR-99).                                                                        | Must     | US-001     |
| FR-015 | The system shall apply a changed gravity interval from the instant the level changes — inside 006's line-clear resolution, between the lock (DR-19) and the following spawn (DR-20) — so the next piece to spawn falls at the new speed (DR-95). | Must     | US-004     |
| FR-016 | The system shall never re-time a piece that is already falling: because there is no `ActivePiece` at the moment a level changes (DR-25, DR-95), no in-flight piece is affected mid-fall.                                             | Must     | US-004     |
| FR-017 | The system shall display the current level to the player for the whole time the session is `Playing`, alongside the playfield (DR-98).                                                                                              | Must     | US-003     |
| FR-018 | The system shall update the displayed level in the same line-clear resolution step in which it changes, so the new level is on screen before the next piece spawns (DR-98).                                                          | Must     | US-003     |
| FR-019 | The system shall mark a promotion with a brief visual emphasis on the level readout lasting at most 400 ms, which must not pause, gate, slow or delay gravity, locking or spawning (DR-98).                                          | Should   | US-003     |
| FR-020 | The system shall expose the level reached to story 008 for the game-over summary, without rendering it at game over itself.                                                                                                         | Should   | US-003     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash,
following 001's convention. "Lines cleared" always means story 006's `totalLinesCleared`._

### SC-001: A new game starts at level 1 (FR-002)

```gherkin
Given a colleague activates the start action
When the session begins
Then the level is 1
  And the lines cleared is 0
  And the gravity interval is 1.000 second
```

### SC-002: Ten lines promote the player to level 2 (FR-001)

```gherkin
Given a session at level 1 with 6 lines cleared
When a lock clears 4 more lines
Then the lines cleared is 10
  And the level is 2
  And the gravity interval is 793 milliseconds
```

### SC-003: A clear that does not reach the goal leaves the level alone (FR-001, FR-003)

```gherkin
Given a session at level 1 with 3 lines cleared
When a lock clears 1 more line
Then the lines cleared is 4
  And the level is still 1
  And the gravity interval is still 1.000 second
  And no level-up emphasis is shown
```

### SC-004: A Tetris can never skip a level (FR-005)

```gherkin
Given a session at level 1 with 9 lines cleared
When a lock clears 4 lines
Then the lines cleared is 13
  And the level is 2, not 3
```

### SC-005: Crossing a multiple of ten advances exactly one level (FR-001, FR-005)

```gherkin
Given a session at level 4 with 38 lines cleared
When a lock clears 3 lines
Then the lines cleared is 41
  And the level is 5
```

### SC-006: The level only ever changes at a line clear (FR-003)

```gherkin
Given a session at level 3
When 60 seconds of play elapse in which pieces spawn, fall, are moved, are hard dropped and lock
  And no lock clears a line
Then the level is still 3
  And the gravity interval has not changed
```

### SC-007: The level never goes down (FR-004)

```gherkin
Given a session that has reached level 6
When play continues for the rest of the session
Then the level is never observed below 6 at any later moment
```

### SC-008: The whole curve matches the published table (FR-007, FR-008, FR-010)

```gherkin
Given the level is set in turn to each value from 1 to 15
When the gravity interval is read at each level
Then the interval equals the effective value in the §5.3 table for that level
  And each interval is strictly shorter than the interval of the level before it
```

### SC-009: Level 1 still falls at one cell per second (FR-009)

```gherkin
Given a session at level 1 with a piece in free space
When 1.000 second of game time elapses
Then the piece has moved down exactly one cell
```

### SC-010: Level 10 falls fifteen times faster (FR-007, FR-011)

```gherkin
Given a session at level 10 with a piece in a clear column
When 1.280 seconds of game time elapse with no player input
Then the piece has descended 20 cells
```

### SC-011: The new speed applies to the next piece, not the current one (FR-015, FR-016)

```gherkin
Given a session at level 1 with 9 lines cleared
When a piece locks and clears 1 line
Then the level becomes 2 while no piece is active
  And the piece that spawns immediately afterwards descends one cell per 793 milliseconds
  And no piece was re-timed mid-fall, because none was falling at that moment
```

### SC-012: A promotion does not interrupt play (FR-015, FR-019)

```gherkin
Given a session in which the next lock will clear the line that triggers a promotion
When that lock resolves
Then the lines are cleared, the level is raised and the next piece spawns with no perceptible delay
  And no pause, modal, animation gate or entry delay occurs between the lock and the spawn
```

### SC-013: The level readout is visible and current (FR-017, FR-018)

```gherkin
Given a session is in progress at level 4
When the player looks at the screen
Then the current level 4 is displayed alongside the playfield
When a clear promotes the session to level 5
Then the readout shows 5 before the next piece spawns
```

### SC-014: A promotion is noticeable but brief (FR-019)

```gherkin
Given a session is in progress
When the level rises
Then the level readout is briefly emphasised
  And the emphasis ends within 400 milliseconds
  And gravity, locking and spawning continue uninterrupted throughout it
```

### SC-015: Level 15 is the ceiling (FR-006)

```gherkin
Given a session with 139 lines cleared at level 14
When a lock clears 1 line
Then the level is 15
  And the gravity interval is 7 milliseconds
When a further 60 lines are cleared over the rest of the session
Then the level is still 15
  And the gravity interval is still 7 milliseconds
  And the lines cleared is 200
  And the score has continued to increase
```

### SC-016: Soft drop speeds up with the level (FR-013)

```gherkin
Given a session at level 1
When the player holds soft drop on a piece in a clear column
Then the piece descends one cell per 50 milliseconds
Given the same session later at level 9
When the player holds soft drop on a piece in a clear column
Then the piece descends faster than it did at level 1
  And the rate is one twentieth of the level-9 gravity interval
```

### SC-017: Hard drop is unaffected by level (FR-013)

```gherkin
Given a session at level 15
When the player hard drops a piece
Then the piece is placed at its landing row instantly, exactly as it is at level 1
```

### SC-018: Lock delay does not shrink with level (FR-014)

```gherkin
Given a session at level 15
When a piece becomes grounded
Then it locks after 0.500 second of continuous grounded time
  And it still has a budget of 15 lock delay resets
```

### SC-019: Very short intervals still collide correctly (FR-012)

```gherkin
Given a session at level 15
  And a stack whose surface is 3 cells below the spawning piece
When a single update step delivers 100 milliseconds of elapsed time
Then the piece has descended exactly 3 cells and is grounded
  And it has not passed through or overlapped any occupied cell
```

### SC-020: The level resets on a new game (FR-002)

```gherkin
Given a session that reached level 12
When the player activates the new game action
Then a fresh session begins at level 1 with 0 lines cleared and a gravity interval of 1.000 second
  And nothing from the previous session's level or line count carries over
```

## 5. Domain Model

_This spec adds no new entities. It constrains two attributes that 001 declared and left open, adds one
constant, and adds one piece of reference data. All other vocabulary is 001's §5 and is used verbatim._

### 5.0 What this spec changes

001 declared, on `GameSession`:

- `level` — "integer, ≥ 1, **fixed at 1 in this spec** … Story 007 owns how it increases."
- `gravityInterval` — "duration, **derived from `level`**; 1.000 s at level 1."

007 replaces "fixed at 1" with the derivation in DR-90 and supplies the derivation function for
`gravityInterval` in DR-92. Nothing else in 001's model changes. Story 006's `totalLinesCleared` is the
input; this spec treats it as read-only.

> This section is numbered 5.4 for domain rules, per the template. 001 numbers its rules 5.5 because it
> also defines §5.4 Enumerations; 007 adds no enumerations, so the template numbering applies here.

### 5.1 Entities

#### GameSession (amended)

Only the attributes this spec touches are listed. Every other attribute is exactly as defined in 001 §5.1.

| Attribute         | Type     | Constraints                                                            | Description                                                                                       |
| ----------------- | -------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| level             | integer  | 1 ≤ level ≤ `levelCap`; derived from `totalLinesCleared` (DR-90)       | Current difficulty level. Starts at 1 (DR-23), only ever rises (DR-91).                            |
| levelCap          | integer  | constant, **15**                                                       | Highest attainable level (DR-94). Not configurable.                                                 |
| gravityInterval   | duration | derived from `level` (DR-92, DR-93); 1.000 s at level 1; 7 ms at level 15 | Time per one-cell descent (DR-14). Re-derived whenever `level` changes, and at no other time.      |
| totalLinesCleared | integer  | ≥ 0; **owned by story 006**, read-only here                            | Lines cleared this session. The sole input to DR-90. 007 never writes it.                           |
| score             | integer  | ≥ 0; **owned by story 006**                                            | Keeps rising after `levelCap` is reached (DR-94). 007 neither writes nor caps it.                   |

`softDropInterval` is derived, never stored: `gravityInterval / 20` (DR-96). Story 002 owns soft drop
itself; this spec only fixes where its rate comes from.

### 5.2 Relationships

- A **GameSession** has exactly one `level`, derived solely from its own `totalLinesCleared` (DR-90).
  Nothing else — elapsed time, pieces spawned, score — influences it.
- A **GameSession**'s `gravityInterval` is a pure function of its `level` (DR-92). Two sessions at the
  same level always fall at the same rate.
- A **LevelSpeed** exists once per level 1–15 as immutable reference data, not per session — the same
  relationship **TetrominoDefinition** has to **TetrominoType** in 001 §5.2.
- An **ActivePiece** reads `gravityInterval` from its session; it never holds its own copy that could
  drift out of step with a level change (DR-95).

### 5.3 Value Objects

#### LevelSpeed

Immutable reference data: one per level from 1 to `levelCap`. Not session state.

| Attribute       | Type     | Constraints                                  | Description                                            |
| --------------- | -------- | -------------------------------------------- | ------------------------------------------------------ |
| level           | integer  | 1–15, unique                                 |                                                        |
| linesToReach    | integer  | derived: `(level - 1) × 10`                  | `totalLinesCleared` at which this level is first held. |
| gravityInterval | duration | whole milliseconds, strictly decreasing      | The effective interval for this level (DR-92, DR-93).  |

**Normative speed curve.** `b = 0.8 - ((level - 1) × 0.007)`; the exact curve value is `b ^ (level - 1)`
seconds; the **effective interval** is that value rounded to the nearest whole millisecond (DR-93). This
table is normative: an implementation may compute the curve or look the value up, but it must agree with
the millisecond column exactly.

| Level | Lines to reach | `b`   | Exact `b ^ (level-1)` (s) | **Effective interval** | Cells / second | 20-row descent |
| ----- | -------------- | ----- | ------------------------- | ---------------------- | -------------- | -------------- |
| 1     | 0              | 0.800 | 1.000000000               | **1000 ms**            | 1.00           | 20.000 s       |
| 2     | 10             | 0.793 | 0.793000000               | **793 ms**             | 1.26           | 15.860 s       |
| 3     | 20             | 0.786 | 0.617796000               | **618 ms**             | 1.62           | 12.360 s       |
| 4     | 30             | 0.779 | 0.472729139               | **473 ms**             | 2.11           | 9.460 s        |
| 5     | 40             | 0.772 | 0.355196928               | **355 ms**             | 2.82           | 7.100 s        |
| 6     | 50             | 0.765 | 0.262003550               | **262 ms**             | 3.82           | 5.240 s        |
| 7     | 60             | 0.758 | 0.189677245               | **190 ms**             | 5.26           | 3.800 s        |
| 8     | 70             | 0.751 | 0.134734731               | **135 ms**             | 7.41           | 2.700 s        |
| 9     | 80             | 0.744 | 0.093882249               | **94 ms**              | 10.64          | 1.880 s        |
| 10    | 90             | 0.737 | 0.064151585               | **64 ms**              | 15.63          | 1.280 s        |
| 11    | 100            | 0.730 | 0.042976258               | **43 ms**              | 23.26          | 0.860 s        |
| 12    | 110            | 0.723 | 0.028217678               | **28 ms**              | 35.71          | 0.560 s        |
| 13    | 120            | 0.716 | 0.018153329               | **18 ms**              | 55.56          | 0.360 s        |
| 14    | 130            | 0.709 | 0.011439342               | **11 ms**              | 90.91          | 0.220 s        |
| 15    | 140            | 0.702 | 0.007058616               | **7 ms**               | 142.86         | 0.140 s        |

Notes on the table:

- No exact value falls within 0.05 ms of a half-millisecond boundary, so the rounding mode is never
  actually exercised as a tie. DR-93 still fixes it (round half away from zero) for completeness.
- The 20-row descent column is the time a piece takes to cross the whole visible field unaided, and is
  the most useful quantity for SC-010 and SUC-004.
- Soft drop is one twentieth of the interval (DR-96): **50 ms per cell at level 1**, 0.35 ms per cell at
  level 15 — i.e. visually instantaneous by the top of the curve, which is the intended end state.
- **Why the cap is 15.** The curve continues past 15 — level 16 is 4.264 ms, level 17 is 2.520 ms, level
  20 is 0.455 ms — but below a few milliseconds the interval stops being a meaningful difficulty dial.
  It is already shorter than a 60 Hz display frame at level 13, and by level 16 gravity is simply
  instantaneous: the piece is on the stack the moment it appears. Past that point the curve changes
  nothing a player can perceive, while **Lock Delay (DR-16)** becomes the sole source of thinking time.
  Capping at 15 keeps the top of the curve honest and bounded, and keeps the arithmetic above a
  millisecond so timing stays testable.
- **The game stays hard, not impossible.** At level 15 a piece crosses the field in 0.140 s and then sits
  grounded for 0.500 s before locking, and the 15-reset budget (DR-17) lets a player extend that while
  manoeuvring. The pressure is real — roughly half a second of decision time per piece — but the piece
  is still placeable.

### 5.4 Domain Rules and Invariants

Later specs should cite these by ID and name, e.g. "per **Level Derivation (DR-90)**". Rules DR-01 … DR-27
belong to 001 and are unchanged by this spec.

- **DR-90 — Level Derivation**: `level = min(floor(totalLinesCleared / 10) + 1, levelCap)`. The level is a
  pure function of story 006's `totalLinesCleared` and of nothing else — not elapsed time, not pieces
  spawned, not score. Because a single lock clears at most four lines and four is fewer than the ten-line
  goal, one lock can advance the level by at most one; a Tetris can never skip a level, and a clear that
  crosses a multiple of ten advances exactly one level.
- **DR-91 — Level Monotonicity**: Within a session the level never decreases and never changes except as
  a consequence of DR-90 at a line clear. It returns to 1 only when a new session is constructed
  (DR-23) — which is not a decrease but a new session.
- **DR-92 — Gravity Speed Curve**: `gravityInterval` is derived from `level` as
  `(0.8 - ((level - 1) × 0.007)) ^ (level - 1)` seconds — the Tetris Guideline curve. At level 1 this is
  `0.8 ^ 0 = 1.000` second exactly, which is precisely the value **Gravity Step (DR-14)** fixes, so 007
  extends DR-14 without contradicting it. The curve is strictly decreasing over levels 1–15. The interval
  is a pure function of the level: no other state may perturb it.
- **DR-93 — Interval Precision**: The curve is evaluated in IEEE-754 double precision and the result
  rounded to the nearest whole millisecond, halves away from zero. The resulting **effective interval** is
  what the game uses, and the table in §5.3 is the normative list of all fifteen values. Fixing the
  precision this way makes the curve reproducible across platforms and exactly assertable in tests,
  rather than subject to floating-point drift.
- **DR-94 — Level Cap**: `levelCap` is **15**. `level` is clamped to it. Clearing lines beyond 140
  continues to raise `totalLinesCleared` and the score (both story 006's) while `level` and
  `gravityInterval` stay at their level-15 values. The cap is a ceiling on speed only, never on progress.
- **DR-95 — Speed Takes Effect at the Level Change**: A changed `gravityInterval` applies from the instant
  `level` changes. By DR-91 that instant is always inside story 006's line-clear resolution, which **Spawn
  Immediacy (DR-20)** places between the lock (DR-19) and the next spawn — a point at which `activePiece`
  is null by **Single Active Piece (DR-25)**. There is therefore never a falling piece to re-time: the
  first piece to fall at the new speed is the one that spawns immediately afterwards, starting from a
  `fallTimerElapsed` of 0. A level change must never pause, gate or delay that spawn.
- **DR-96 — Soft Drop Derivation**: Story 002's soft drop descends at twenty times gravity, i.e. one cell
  per `gravityInterval / 20`. It is derived from the current interval, so it accelerates with the level
  automatically. Story 003's hard drop is instantaneous and therefore level-independent.
- **DR-97 — Multi-Cell Gravity Steps**: When the elapsed time available to one update step covers more
  than one `gravityInterval` — unavoidable above level 12, where the interval is shorter than a 60 Hz
  frame — gravity is applied as repeated **single-cell** descents, never as one multi-cell jump. Each
  step is independently checked against **Piece Containment (DR-03)** and **No Overlap (DR-04)**, and the
  sequence stops the moment the piece is grounded (DR-15), with the remaining time discarded. A piece can
  never pass through the stack or the floor. This is the high-level reading of DR-26's "never moves a
  piece past the point at which it becomes grounded"; DR-26's 100 ms cap on a single update step still
  applies, bounding one step to at most 14 descents at level 15.
- **DR-98 — Level Visibility**: The current `level` is displayed throughout `GameStatus.Playing` and
  updates within the same line-clear resolution step in which it changes, before the next piece spawns. A
  promotion is marked by a brief visual emphasis on the readout of at most 400 ms, which runs alongside
  play and must not pause, gate, slow or delay gravity, locking or spawning. In `Ready` the readout shows
  1.
- **DR-99 — Level-Independent Timings**: The level changes the gravity interval and nothing else. The
  0.500 s lock delay (DR-16), the 15-reset lock budget (DR-17), the replenishment condition (DR-17), the
  zero entry delay (DR-20), the spawn placement (DR-06) and the 7-bag sequence (DR-09, DR-10) are
  identical at every level.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. 001's NFR-001 … NFR-008 still apply; where this feature
makes one of them harder to satisfy, the stricter statement is given here._

| ID      | Category         | Requirement                                                                                                                                                                                                     |
| ------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Correctness      | The effective gravity interval matches the §5.3 table **exactly** (0 ms deviation) at every level 1–15, on every platform the game runs on.                                                                        |
| NFR-002 | Performance      | At level 15 (7 ms per cell) the game loop sustains 001's NFR-001 frame rate (≥ 30 fps, targeting 60) and drops no descents: over a 20-row unaided fall the piece arrives in 140 ms ± 20 ms.                         |
| NFR-003 | Performance      | Timing tolerance supersedes 001's NFR-002 at high levels: per-cell deviation is within **±50 ms or ±25 % of the nominal interval, whichever is larger**, measured over a 20-cell descent.                            |
| NFR-004 | Responsiveness   | The displayed level reflects a change within one render frame (**≤ 33 ms**) of the line-clear resolution that caused it, and always before the next piece is visible.                                                |
| NFR-005 | Usability        | A level-up is noticeable without being read: a player watching the playfield rather than the readout registers the promotion. The emphasis lasts **≤ 400 ms** and costs **0 ms** of game time.                       |
| NFR-006 | Accessibility    | The level-up emphasis is confined to the level readout — not a full-screen flash — and flashes no more than **3 times per second**, so it cannot trigger photosensitive reactions (WCAG 2.3.1).                      |
| NFR-007 | Testability      | `level` and `gravityInterval` are pure functions of `totalLinesCleared`, with no hidden state and no dependence on wall-clock time, so every §4 scenario runs headlessly with injected time per 001's NFR-006.       |
| NFR-008 | Reliability      | A session played continuously to level 15 and held there for 10 minutes accumulates less than one cell of gravity timing drift, matching 001's NFR-004 at the fastest level rather than only at level 1.            |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                          | Expected Behavior                                                                                                                                                                       |
| ----- | ------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | A Tetris clears 4 lines taking `totalLinesCleared` from 8 to 12, crossing the goal.                | Level advances from 1 to 2 — exactly one level (DR-90). The two "extra" lines count toward the next promotion; nothing is lost or carried differently.                                      |
| EC-2  | A clear lands exactly on a multiple of ten (6 → 10).                                               | Level advances to 2. `floor(10/10) + 1 = 2`; the boundary belongs to the new level (DR-90).                                                                                                 |
| EC-3  | A clear lands exactly on 140 lines.                                                                 | Level becomes 15, the cap. Interval 7 ms (DR-94).                                                                                                                                           |
| EC-4  | Lines are cleared after the cap (148 → 152).                                                        | `totalLinesCleared` and the score rise; `level` stays 15 and `gravityInterval` stays 7 ms (DR-94). No overflow, no wrap, no error.                                                           |
| EC-5  | A lock clears lines **and** triggers lock out (DR-22) — a full row entirely in buffer rows.         | The lock is written, 006's resolution clears the lines and DR-90 re-derives the level, then the session moves to `GameOver` (DR-22). No further spawn occurs (DR-27). The level reached is the post-clear value. |
| EC-6  | A lock clears lines and the following spawn is blocked (block out, DR-21).                          | The level has already been raised by the time the spawn is attempted, since DR-95 places the change before DR-20's spawn. The session moves to `GameOver` at the correct final level.        |
| EC-7  | The tab is backgrounded at level 15 and a multi-second delta arrives.                               | DR-26 caps the step at 100 ms, which DR-97 applies as at most 14 single-cell descents, each collision-checked, halting at grounding. The remainder is discarded. No tunnelling, no burst past the stack. |
| EC-8  | The display runs at 60 Hz while the interval is 18 ms (level 13) or 7 ms (level 15).                | Correctness is defined by the model, not the render: 1–3 single-cell descents resolve per frame (DR-97) and the render shows the resulting position. Intermediate positions need not be drawn. |
| EC-9  | Soft drop is being held across a level-up.                                                           | The promotion occurs between pieces (DR-95). The next piece soft-drops at the new, faster derived rate (DR-96); the held key is not re-read or reset.                                        |
| EC-10 | A hard drop is performed at level 15.                                                                | Unchanged from level 1: the piece is placed at its landing row instantly (DR-96). Hard drop has no interval to speed up.                                                                     |
| EC-11 | A piece is grounded the instant it spawns at level 15 (the stack is at the top).                     | It still gets the full 0.500 s lock delay and the full 15-reset budget (DR-99). High levels never shorten the player's last chance.                                                         |
| EC-12 | A lock clears lines but does not change the level (3 → 4).                                           | `gravityInterval` is not re-derived and the readout is not emphasised (DR-91, DR-98). Only the line and score readouts move.                                                                 |
| EC-13 | A platform computes `0.786 ^ 2` as `0.6177960000000001`.                                             | Irrelevant to behaviour: DR-93 rounds to 618 ms and the §5.3 table is normative. Tests assert milliseconds, never raw doubles.                                                               |
| EC-14 | The player starts a new game while at level 15.                                                      | The fresh session is at level 1 with 0 lines and a 1.000 s interval (DR-23, FR-002). No level, line count or speed carries over (EC-8 of 001 applies to the discard).                        |
| EC-15 | The session is in `Ready` and has never been started.                                                | The level readout shows 1 and no gravity runs (DR-98, 001's EC-13).                                                                                                                         |
| EC-16 | A level-up emphasis is still running when the *next* level-up occurs (two promotions close together). | The running emphasis is replaced by the new one; emphases never queue up, never extend one another past 400 ms, and never gate play (DR-98).                                                |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4, per 001's convention._

| ID      | Criterion                                                                                                                                                              |
| ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 20 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                        |
| SUC-002 | A table-driven test asserts all fifteen effective intervals from §5.3 exactly, in milliseconds, and asserts that the sequence is strictly decreasing.                     |
| SUC-003 | Driving `totalLinesCleared` from 0 to 250 one line at a time produces the level sequence 1,1,…,2,2,…,15,15,… with promotions at exactly 10, 20, …, 140 and none after.    |
| SUC-004 | At every level 1–15, a piece released into an empty column crosses the 20 visible rows in the §5.3 "20-row descent" time, within NFR-003's tolerance.                     |
| SUC-005 | All ten domain rules DR-90 … DR-99 are each covered by at least one automated test.                                                                                      |
| SUC-006 | A colleague watching recorded play at levels 1, 7 and 15 with the readout hidden can rank the three clips by speed without hesitation.                                   |
| SUC-007 | Across a full session played to level 15, no frame exists in which play is paused, input is ignored or gravity is stalled because of a level change or its emphasis.      |
| SUC-008 | A 10-minute session pinned at level 15 completes without an exception, a stuck piece, an overlapping placement or a piece outside bounds — 001's SUC-004 at full speed.   |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 001** (`docs/specs/001-start-new-game.md`) — the normative domain model. 007 builds directly on
  DR-14 (gravity step), DR-15/DR-16/DR-17 (grounding and lock delay), DR-19/DR-20 (lock and spawn
  immediacy), DR-23 (fresh session state), DR-25 (single active piece), DR-26 (elapsed-time timing) and
  DR-27 (terminal status).
- **Story 006 (line clearing and scoring)** — a hard upstream dependency. 006 owns `totalLinesCleared`,
  the sole input to DR-90, and owns the line-clear resolution step into which DR-95 hooks the level
  change. 007 cannot be implemented or meaningfully tested before 006 exists.
- **Story 002 (movement and soft drop)** — downstream: its soft-drop rate is derived from
  `gravityInterval` (DR-96), so it changes behaviour the moment 007 lands. 002 needs no code change if it
  already reads the interval rather than hard-coding 50 ms.
- **Story 003 (hard drop)** — unaffected by design (DR-96). Listed so the independence is deliberate and
  testable, not accidental.
- **Story 008 (game over)** — downstream: reads the level reached for its summary (FR-020).
- **Story 013 (branding)** — downstream: styles the level readout and its emphasis (FR-017, FR-019)
  within the Info Support palette, subject to NFR-006.
- **The modern Tetris Guideline** — the source of both the ten-lines-per-level fixed goal and the
  `(0.8 - 0.007(n-1))^(n-1)` curve.

### 9.2 Constraints

- **The curve is fixed.** `(0.8 - ((level - 1) × 0.007)) ^ (level - 1)` is the Guideline curve and is not
  tuned, approximated or replaced. Its level-1 value of exactly 1.000 s is what lets 007 extend DR-14
  rather than contradict it.
- **The cap is fixed at 15** and is not configurable, per the reasoning in §5.3. Raising it later is a
  product decision, not a setting.
- **Ten lines per level is fixed** — the Guideline's fixed-goal system, not the variable-goal variant
  (§1.3).
- **`totalLinesCleared` is single-source.** 007 must not maintain a parallel line counter; a second
  counter that could disagree with 006's is the obvious failure mode here and is forbidden.
- **No level-based timing changes other than gravity** (DR-99).
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed here): ASP.NET
  Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`. The
  level readout is part of the same presentation surface as the playfield.
- **Platform**: desktop browser on a laptop with a keyboard (story map). At level 15 the interval is
  shorter than a display frame on every common refresh rate, which is why DR-97 exists.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*, exactly as 001 §9.3 does.

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                                       |
| --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01. Introduction and Goals (`01-introduction-and-goals.md`)     | Should record rising difficulty as a quality driver in its own right: a run must be able to end in defeat, which is what makes the leaderboard (009–011) mean anything. *Currently empty.*         |
| 04. Solution Strategy (`04-solution-strategy.md`)               | The browser-vs-server game-loop decision 001 flagged bites hardest here: at 7 ms per cell the loop cannot tolerate a network round trip per descent. *Currently empty.*                            |
| 05. Building Block View (`05-building-block-view.md`)           | 007 adds a small, pure difficulty component (level derivation + speed curve) between the scoring block (006) and the game loop. It holds no mutable state of its own. *Currently empty.*           |
| 06. Runtime View (`06-runtime-view.md`)                         | Extends 001's central scenario at the lock → line-clear → spawn seam: lock (DR-19) → 006 resolves lines → DR-90 re-derives level → DR-92 re-derives interval → spawn (DR-20). *Currently empty.*  |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)     | The project-wide timing model (DR-26) needs extending with DR-97's sub-frame, multi-step gravity, which is cross-cutting rather than 007-specific. *Currently empty.*                              |
| 09. Architecture Decisions (`09-architecture-decisions.md`)     | 007 warrants ADRs for: millisecond-rounded interval values as the normative contract (DR-93), the level cap of 15 (DR-94), and single-cell-stepping gravity under short intervals (DR-97). *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)         | Should host the high-level performance scenario — the loop must keep up at 7 ms per cell — as a project-wide quality scenario rather than only an NFR here (NFR-002). *Currently empty.*           |
| 12. Glossary (`12-glossary.md`)                                 | Add: level, level cap, gravity speed curve, effective interval, promotion, fixed-goal level system. *Currently empty.*                                                                             |

## 10. Open Questions

| #   | Question                                                                                      | Owner      | Status   | Resolution                                                                                                                                                                                                                      |
| --- | ----------------------------------------------------------------------------------------------- | ---------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Where should the level cap sit — 15, 20, or uncapped?                                           | Daan Harms | Resolved | **15** (DR-94). Past 15 the interval drops below a few milliseconds, where the curve no longer changes anything a player can perceive and lock delay is the only real constraint. 15 also keeps every value above a millisecond, so the table stays exactly testable. |
| 2   | Should the game use the exact floating-point curve value or a fixed table?                      | Daan Harms | Resolved | **Both, made identical** (DR-93): compute or look up, but the millisecond-rounded value in §5.3 is normative. This removes cross-platform floating-point drift and makes tests assert exact integers.                             |
| 3   | Should the lock delay shorten at high levels, as some implementations do?                       | Daan Harms | Resolved | **No** (DR-99). The Guideline keeps lock delay constant, and it is the only thing that keeps level 15 playable rather than merely fast. Shortening it would turn the cap into a hard wall.                                        |
| 4   | Should the player be able to pick a starting level, as the Guideline optionally allows?         | Daan Harms | Resolved | **No** — an explicit Non-Goal (§1.3). Every run starts at level 1 (DR-23), which keeps leaderboard scores comparable and keeps the start action to the single click FR-001 of 001 promises.                                        |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-008..SC-010, SC-016..SC-019; US-002 -> SC-001..SC-007, SC-015, SC-020;
        US-003 -> SC-013, SC-014; US-004 -> SC-011, SC-012
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-006 -> US-002; FR-007..FR-014 -> US-001; FR-015, FR-016 -> US-004;
         FR-017..FR-020 -> US-003)
  - [x] Domain model covers all entities mentioned in the requirements
        No new entities. GameSession amended (level, levelCap, gravityInterval, read-only
        totalLinesCleared and score); new reference value object LevelSpeed with the normative
        15-level interval table. All other vocabulary is spec 001's §5, reused verbatim.
  - [x] Domain rules and invariants are listed (DR-90 .. DR-99; 001's DR-01 .. DR-27 cited, not restated)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-16)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-008)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all four resolved)
-->
