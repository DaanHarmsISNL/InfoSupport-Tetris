# Feature Specification: Line Clears and Scoring — Detection, Collapse, and the Scoring Model

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

| Field           | Value                                                                                       |
| --------------- | ------------------------------------------------------------------------------------------- |
| Feature ID      | 006                                                                                           |
| Status          | Draft                                                                                         |
| Author          | Daan Harms                                                                                    |
| Created         | 2026-10-20                                                                                    |
| Last updated    | 2026-10-20                                                                                    |
| Epic / Parent   | Play Tetris (story map activity) — `docs/product/story-map.md`                                |
| Arc42 reference | Sections 1, 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders.  |

> **Note on this spec.** This spec is written entirely in the vocabulary established by
> **[001 — Start a New Game](001-start-new-game.md) §5**. Entity names, enum members and domain rule IDs
> defined there are reused verbatim and are **not** redefined here. Rules introduced by this spec are
> numbered from **DR-75** and are listed in §5.5.
>
> 001 explicitly hands two things to this story: *"Story 006 owns how [`score`] increases"* (001 §5.1,
> `GameSession.score`) and *"Story 006 inserts its line-clear resolution between the lock and the spawn"*
> (001 **Spawn Immediacy (DR-20)**). Both are discharged here.
>
> **Ruleset.** Every detail not otherwise constrained by the story map is resolved against the modern
> **Tetris Guideline**, per the standing project decision recorded in 001.

### 1.1 Problem Statement

After 001, pieces fall and lock but nothing ever leaves the playfield — a completed row simply stays
(001 **EC-15**), the stack grows monotonically, and every session ends in a top-out within a minute or two
with a `score` permanently stuck at 0. There is no reward for stacking well and no reason to prefer one
placement over another, so the game has no skill expression and nothing to compete over. Line clearing is
the mechanic that makes Tetris a game; scoring is what makes it worth playing twice.

### 1.2 Goal

When a piece locks, every row that is now completely filled is removed, the rows above each removed row
slide down to close the gap, and the player is awarded points that scale steeply with how many rows were
removed in one go — 100, 300, 500 and 800 per level for a Single, Double, Triple and Tetris — plus small
points for the distance the player chose to drop the piece. A running score and a running count of lines
cleared are visible throughout play and update the instant a piece locks, so the player can see that a
well-saved four-row clear was worth more than four separate ones.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec.

- **The four optional Guideline bonus mechanics.** The story asks for *"a bonus for multi-line clears"* —
  the 100 / 300 / 500 / 800 curve in **Line Clear Score (DR-80)** already is that bonus. The following are
  Guideline-*optional* and are explicitly excluded:
  - **T-spin and mini-T-spin bonuses** — no spin detection, no last-action tracking, no three-corner test.
  - **Back-to-back bonuses** — consecutive Tetrises score exactly the same as isolated ones.
  - **Combo bonuses** — consecutive clearing locks score exactly the same as isolated ones; no combo counter.
  - **Perfect clear (all-clear) bonuses** — emptying the entire playfield scores as an ordinary clear of
    that size.

  The scoring model in §5 is deliberately structured so that each of these could be added later as an
  additional **`ScoreAward`** with a new **`ScoreSource`** member, without changing **Line Clear Score
  (DR-80)**, **Drop Score (DR-81)** or any of the collapse rules. Adding one is a new story, not a rework.
- **Cascade / sticky gravity after a collapse** — blocks left floating by a clear stay floating. See
  **No Cascade Gravity (DR-78)**.
- **Level progression and the gravity speed curve** — **story 007**. This spec *reads* `level` as a
  multiplier and *writes* `linesCleared` as the counter 007 computes the level from; it never changes
  `level` itself.
- **Game over presentation and the final score screen** — **story 008**. This spec keeps `score` and
  `linesCleared` correct up to and including the terminal lock; what the player is shown afterwards is 008's.
- **Leaderboard, name entry, score submission** — **stories 009–011**. Nothing here is persisted or
  transmitted; `score` is ephemeral session state.
- **The input that produces drop distances** — soft drop is **story 002** and hard drop is **story 003**.
  This spec defines what those distances are worth, not how they are triggered or how the piece moves.
- **Changes to lock behaviour itself** — **Locking (DR-19)**, the lock delay and the reset budget are 001's
  and are untouched. This spec attaches to the moment *after* a lock is written.
- **Scoring presentation beyond a live score and line count** — no score pop-ups at the clear location, no
  "TETRIS!" banner, no clear-type label. Permitted but not required; see FR-018.
- **Ghost piece**, **pause**, **accounts / authentication**, **mobile and touch support** — out of scope
  project-wide (story map).

## 2. User Stories

### US-001: Completed rows disappear and the stack settles

**As a** player,
**I want** a row I have filled completely to vanish and everything above it to drop down one,
**so that** good stacking buys me room and the game does not simply fill up and end.

### US-002: Points that reward saving up for bigger clears

**As a** player,
**I want** clearing four rows at once to be worth far more than clearing four rows one at a time,
**so that** there is a reason to take the risk of building a tall stack instead of clearing greedily.

### US-003: Points for committing to a drop

**As a** player,
**I want** a small reward for dropping a piece further and faster myself rather than waiting for gravity,
**so that** playing quickly is worth something on top of playing well.

### US-004: See where I stand while I play

**As a** player,
**I want** my score and my line count on screen, updating the moment a piece locks,
**so that** I can tell immediately what a placement earned me and how close I am to my best run.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) are defined in §5.5;
rules numbered DR-01 to DR-27 are 001's and are referenced, not restated._

| ID     | Requirement                                                                                                                                                                                                                       | Priority | User Story |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall treat a row as *full* exactly when all 10 of its `Cell`s are occupied, and shall apply this test identically to buffer rows (0–19) and visible rows (20–39) (DR-75).                                               | Must     | US-001     |
| FR-002 | The system shall, after every lock (DR-19), examine all 40 rows of the `Playfield` and identify every full row as a cleared row (DR-76).                                                                                            | Must     | US-001     |
| FR-003 | The system shall remove every cleared row and move each surviving row down by exactly the number of cleared rows below it, leaving the topmost rows empty (DR-77).                                                                   | Must     | US-001     |
| FR-004 | The system shall collapse correctly when the cleared rows are **not adjacent**: rows lying between two cleared rows shift down by one, and rows above both shift down by two (DR-77).                                                | Must     | US-001     |
| FR-005 | The system shall preserve each surviving block's column and each surviving row's contents and relative order during a collapse; no block ever changes column and no block ever moves upward (DR-77).                                 | Must     | US-001     |
| FR-006 | The system shall not re-settle the stack after a collapse — blocks left unsupported remain exactly where the collapse put them (DR-78).                                                                                             | Must     | US-001     |
| FR-007 | The system shall perform detection, collapse, counter update and scoring as one indivisible step between the lock (DR-19) and the next spawn (DR-20), with no gravity, input, rotation, hold or spawn interleaved (DR-85, DR-86).    | Must     | US-001     |
| FR-008 | The system shall evaluate the next spawn — and therefore block out (DR-21) — against the playfield **after** the collapse, so that a clear which frees the spawn area prevents a block out that would otherwise occur (DR-85).       | Must     | US-001     |
| FR-009 | The system shall classify the number of rows cleared by one lock as a `ClearType` of `None`, `Single`, `Double`, `Triple` or `Tetris`, and shall never observe more than four (DR-79, DR-87).                                        | Must     | US-002     |
| FR-010 | The system shall award 100, 300, 500 or 800 points multiplied by the current `level` for a `Single`, `Double`, `Triple` or `Tetris` respectively, and 0 points for a `ClearType` of `None` (DR-80).                                  | Must     | US-002     |
| FR-011 | The system shall use the `level` in effect at the moment the clear resolves as the multiplier, before story 007 applies any level increase caused by the same clear (DR-84).                                                         | Must     | US-002     |
| FR-012 | The system shall award 1 point for every cell the player descends the `ActivePiece` by soft drop, not multiplied by `level`, at the moment of each such descent (DR-81).                                                             | Must     | US-003     |
| FR-013 | The system shall award 2 points for every cell the `ActivePiece` travels during a hard drop, not multiplied by `level`, at the moment the hard drop resolves (DR-81).                                                                | Must     | US-003     |
| FR-014 | The system shall award drop points independently of any line clear — a lock that clears nothing still earns its drop points, and a lock that clears four rows earns both (DR-81).                                                    | Must     | US-003     |
| FR-015 | The system shall not award any points for a cell descended by ordinary gravity (DR-14) rather than by player-initiated soft or hard drop (DR-81).                                                                                    | Must     | US-003     |
| FR-016 | The system shall maintain `GameSession.linesCleared` as a cumulative count of rows cleared in the session, incremented by the number of rows cleared at each lock, as the single source of truth for stories 007 and 008 (DR-82).    | Must     | US-001     |
| FR-017 | The system shall keep `score` a non-negative integer that never decreases within a session; the only way it returns to 0 is a new `GameSession` (DR-23, DR-83).                                                                      | Must     | US-002     |
| FR-018 | The system shall display the current `score` and `linesCleared` throughout play, updated no later than the first render after the lock resolves, with no truncation, ellipsis or rounding of the value shown (DR-88).                | Must     | US-004     |
| FR-019 | The system shall reset `score` and `linesCleared` to 0 when a new `GameSession` starts, including via the mid-session *New game* action (001 FR-005, DR-23).                                                                         | Must     | US-004     |
| FR-020 | The system shall record a `LineClearResult` for each lock resolution — the cleared row indices, the count, the `ClearType`, the multiplier level and the points awarded — and expose it as `GameSession.lastLineClear` for stories 007 and 008 to read (DR-82). | Should   | US-002     |
| FR-021 | The system shall, when a lock both clears rows and triggers lock out (DR-22), award the points and increment `linesCleared` before moving the session to `GameStatus.GameOver`, so the final score includes the last clear (DR-85).   | Should   | US-002     |
| FR-022 | The system may play a visual clear effect of at most 200 ms; if it does, the effect shall not delay, gate or alter the state transition in FR-007 — the playfield, score and line count are already updated when it plays (DR-89).   | Could    | US-004     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash,
matching 001's convention. Row and column indices follow 001 §5.0: row 0 is the topmost buffer row, row 39
is the floor row, and gravity increases `row`._

### SC-001: A completed row clears and the stack above drops one (FR-001, FR-002, FR-003)

```gherkin
Given the playfield has exactly one row with all 10 cells occupied, row 39
  And row 38 holds blocks in columns 0 and 1 only
  And all other rows are empty
When the piece that completed row 39 locks
Then row 39 is no longer the row that was full
  And the blocks that were in row 38 are now in row 39, still in columns 0 and 1
  And row 38 is empty
  And the lines cleared counter has increased by 1
```

### SC-002: A row that is one cell short does not clear (FR-001)

```gherkin
Given the playfield has a row with 9 of its 10 cells occupied
When a piece locks elsewhere on the playfield without filling the tenth cell
Then no row is cleared
  And the row keeps all 9 of its blocks in their original cells
  And the lines cleared counter is unchanged
  And no line clear points are awarded
```

### SC-003: Four rows clear at once (FR-002, FR-003, FR-009, FR-010)

```gherkin
Given rows 36, 37, 38 and 39 each have 9 cells occupied, all missing column 9
  And an I tetromino is falling in column 9 in a vertical orientation
When it locks filling column 9 of rows 36, 37, 38 and 39
Then all four rows are cleared
  And the clear is classified as a Tetris
  And 800 points multiplied by the current level are awarded
  And the lines cleared counter has increased by 4
```

### SC-004: Non-adjacent rows cleared by one piece collapse correctly (FR-004)

```gherkin
Given rows 36 and 39 each have 9 cells occupied, both missing column 9
  And rows 37 and 38 have 8 cells occupied, missing columns 8 and 9
  And row 35 holds a single block in column 0
  And an I tetromino is falling in column 9 in a vertical orientation
When it locks filling column 9 of rows 36, 37, 38 and 39
Then rows 36 and 39 are cleared and rows 37 and 38 are not
  And the contents of old row 38 are now in row 39
  And the contents of old row 37 are now in row 38
  And the single block from old row 35 is now in row 37, still in column 0
  And rows 0 and 1 are empty
  And the clear is classified as a Double
```

### SC-005: The general non-adjacent collapse rule (FR-004, FR-005)

```gherkin
Given a playfield state in which rows 25 and 31 are both full
  And every other row between 20 and 39 is partially filled and distinguishable
When the lock resolution clears rows 25 and 31
Then every surviving row from 26 to 30 has moved down by exactly one row
  And every surviving row from 0 to 24 has moved down by exactly two rows
  And every surviving row from 32 to 39 has not moved
  And rows 0 and 1 are empty
  And no block has changed column
```

### SC-006: Blocks left unsupported stay floating (FR-006)

```gherkin
Given row 39 is full
  And row 38 holds a single block in column 4 with row 39 beneath it
  And rows 37 and above are empty apart from that block's neighbours
When row 39 is cleared
Then the block from row 38 is now in row 39
  And no further settling is applied to any other block
  And any block that is now unsupported remains at the row the collapse placed it in
```

### SC-007: A full buffer row clears on the same terms (FR-001, FR-002)

```gherkin
Given the stack has grown into the buffer rows
  And row 19 has all 10 cells occupied
When a piece locks completing row 19
Then row 19 is cleared exactly as a visible row would be
  And the rows above it shift down by one
  And the clear is scored and counted exactly as a visible-row clear
```

### SC-008: The top of the playfield is emptied by a clear (FR-003)

```gherkin
Given three rows are cleared by a single lock
When the collapse completes
Then rows 0, 1 and 2 contain no occupied cells
  And the playfield still has exactly 40 rows and 10 columns
```

### SC-009: Clearing happens between the lock and the next spawn (FR-007)

```gherkin
Given a piece is grounded on a row that it will complete
When its lock delay expires
Then its cells are written into the playfield
  And the completed row is detected and cleared
  And the score and lines cleared counter are updated
  And only then does the next piece spawn
  And no gravity step, input, rotation or hold is processed in between
```

### SC-010: A clear can avert a block out (FR-008)

```gherkin
Given the stack occupies cells in the spawn position of the next piece
  And the lock about to resolve will clear two rows
When the piece locks and the two rows are cleared
Then the collapse frees the spawn cells
  And the next piece spawns normally
  And the session remains in status Playing
```

### SC-011: The just-locked piece has no special treatment during collapse (FR-005)

```gherkin
Given a locked piece has two of its four cells in a cleared row and two in a surviving row
When the collapse completes
Then the two cells in the cleared row are gone
  And the two cells in the surviving row have moved down with that row
  And they are indistinguishable from any other block of the same type
```

### SC-012: A single row clear scores 100 per level (FR-010)

```gherkin
Given the session is at level 1
  And the score is 0
When a lock clears exactly one row
Then 100 points are awarded
  And the score is 100
```

### SC-013: The full line clear scoring table (FR-009, FR-010)

```gherkin
Given the session is at level <level>
  And the score is 0
When a lock clears <rows> rows
Then the clear is classified as <type>
  And <points> points are awarded

Examples:
  | rows | type   | level | points |
  | 0    | None   | 1     | 0      |
  | 1    | Single | 1     | 100    |
  | 2    | Double | 1     | 300    |
  | 3    | Triple | 1     | 500    |
  | 4    | Tetris | 1     | 800    |
  | 1    | Single | 5     | 500    |
  | 2    | Double | 5     | 1500   |
  | 3    | Triple | 5     | 2500   |
  | 4    | Tetris | 5     | 4000   |
  | 4    | Tetris | 12    | 9600   |
```

### SC-014: Four separate Singles are worth less than one Tetris (FR-010)

```gherkin
Given two sessions both at level 1 with a score of 0
When the first session clears one row on each of four separate locks
  And the second session clears four rows on a single lock
Then the first session has scored 400 points
  And the second session has scored 800 points
  And both sessions report 4 lines cleared
```

### SC-015: The multiplier uses the level at the moment of the clear (FR-011)

```gherkin
Given the session is at level 4
  And this clear will take the session over the threshold into level 5
When a lock clears four rows
Then 3200 points are awarded, being 800 multiplied by 4
  And the level increase that story 007 applies afterwards does not change the points awarded
```

### SC-016: Points from the final clear are counted before game over (FR-021)

```gherkin
Given a piece is grounded with all four of its cells in buffer rows
  And locking it will complete one buffer row
When its lock delay expires
Then the row is cleared and 100 points multiplied by the level are awarded
  And the lines cleared counter has increased by 1
  And only then does the session move to status GameOver
  And no further piece spawns
```

### SC-017: Soft drop pays one point per cell and ignores the level (FR-012, FR-015)

```gherkin
Given the session is at level 7
  And the score is 0
When the player soft-drops the active piece 6 cells
Then 6 points are awarded
  And the level has not multiplied the award
```

### SC-018: Hard drop pays two points per cell (FR-013)

```gherkin
Given the active piece would travel 17 cells before becoming grounded
  And the score is 0
When the player hard-drops it
Then 34 points are awarded
  And they are awarded whether or not the resulting lock clears any row
```

### SC-019: Drop points and line clear points combine on one lock (FR-014)

```gherkin
Given the session is at level 2
  And the score is 0
  And the active piece would travel 10 cells before becoming grounded
  And hard-dropping it will complete two rows
When the player hard-drops it
Then 20 points are awarded for the 10 cells travelled
  And 600 points are awarded for the Double
  And the score is 620
```

### SC-020: A drop that travels no distance pays nothing (FR-012, FR-013)

```gherkin
Given the active piece is already grounded
When the player hard-drops it
Then the piece travels 0 cells
  And 0 drop points are awarded
When the player instead holds soft drop on an already grounded piece
Then 0 drop points are awarded
```

### SC-021: Ordinary gravity earns nothing (FR-015)

```gherkin
Given the session is at level 1 with a score of 0
When the active piece falls 15 cells under gravity alone, with no player input
Then 0 points are awarded for the descent
```

### SC-022: Score and line count are on screen and update on each lock (FR-018)

```gherkin
Given a game session is in progress
When the player looks at the screen
Then the current score and the current number of lines cleared are both visible
When a piece locks and clears two rows
Then the displayed score has increased by 300 multiplied by the level
  And the displayed line count has increased by 2
  And both updates are visible on the first frame rendered after the lock resolves
```

### SC-023: The line count accumulates across the whole session (FR-016)

```gherkin
Given a game session in which locks have cleared 1, then 4, then 2, then 3 rows
When the line count is read
Then it reads 10
  And it has never decreased at any point in the session
```

### SC-024: A new game resets score and lines (FR-019)

```gherkin
Given a session with a score of 12400 and 47 lines cleared
When the player activates the new game action
Then the new session has a score of 0
  And a lines cleared count of 0
  And nothing from the discarded session is recorded or submitted anywhere
```

### SC-025: A clear effect never delays the game (FR-022)

```gherkin
Given a visual clear effect is configured
When a lock clears three rows
Then the collapse, the score update and the next spawn all happen immediately
  And the effect plays over the already-updated playfield
  And the time from lock to next spawn is the same as for a lock that clears nothing
```

## 5. Domain Model

_This spec adds no new entity. It adds attributes to **`GameSession`** (001 §5.1), two value objects and
two enumerations, all of which extend 001's vocabulary rather than competing with it._

### 5.1 Entities

#### GameSession (extended)

001 defines this entity. This spec takes ownership of `score`'s behaviour and adds two attributes. No other
001 attribute changes meaning.

| Attribute       | Type               | Constraints                                | Description                                                                                                          |
| --------------- | ------------------ | ------------------------------------------ | -------------------------------------------------------------------------------------------------------------------- |
| score           | integer (64-bit)   | ≥ 0, starts at 0, non-decreasing (DR-83)   | **Owned by this spec.** The sum of every `ScoreAward` made in the session. 001 held it fixed at 0.                     |
| linesCleared    | integer            | ≥ 0, starts at 0, non-decreasing (DR-82)   | **New.** Cumulative count of rows cleared this session. The single source of truth for stories 007 and 008.            |
| lastLineClear   | LineClearResult?   | null until the first lock resolves         | **New.** The outcome of the most recent lock resolution, including a `ClearType` of `None`. Read by 007, 008 and FR-022.|
| level           | integer            | ≥ 1 — unchanged, owned by story 007        | Read here only, as the multiplier in DR-80. This spec never writes it.                                                 |

`linesCleared` and `score` are reset by **Fresh Session State (DR-23)** along with everything else; 001's
DR-23 already names `score` and is extended here to cover `linesCleared` and `lastLineClear`.

### 5.2 Relationships

- A **GameSession** has exactly one `score` and one `linesCleared`, both scalar and both monotonically
  non-decreasing for the session's lifetime.
- A **GameSession** holds **at most one** current **LineClearResult** (`lastLineClear`) — the outcome of the
  most recent lock resolution. Results are not accumulated into a history; `score` and `linesCleared` are the
  accumulated state.
- A **LineClearResult** refers to zero to four rows of the **Playfield** by index, captured *before* the
  collapse described by **Row Collapse (DR-77)**. The indices are historical: after the collapse those row
  indices address different contents.
- A **ScoreAward** is produced by exactly one of three occasions, named by its **ScoreSource**: a line clear
  (DR-80), a soft drop (DR-81) or a hard drop (DR-81). `score` is the sum of every award made in the session.
- A **LineClearResult** with a `clearType` of `None` still exists and is still recorded; it simply produces
  no `ScoreAward` of source `LineClear` and does not change `linesCleared`.
- Cleared rows remove **Cell** occupancy only. No **TetrominoType**, **TetrominoDefinition** or
  **PieceQueue** state is touched by a clear.

### 5.3 Value Objects

#### LineClearResult

The outcome of one lock resolution. Immutable once produced.

| Attribute     | Type          | Constraints                                                  | Description                                                                        |
| ------------- | ------------- | ------------------------------------------------------------ | ---------------------------------------------------------------------------------- |
| clearedRows   | integer[]     | 0–4 entries, each 0–39, strictly ascending, no duplicates    | Row indices that were full, as they stood **before** the collapse.                  |
| clearCount    | integer       | 0–4, derived: `clearedRows.length` (DR-87)                   | How many rows this lock cleared.                                                    |
| clearType     | ClearType     | required, derived from `clearCount` (DR-79)                  | `None`, `Single`, `Double`, `Triple` or `Tetris`.                                   |
| levelAtClear  | integer       | ≥ 1                                                          | The `level` used as the multiplier (DR-84). Captured before any 007 level increase. |
| pointsAwarded | integer       | ≥ 0, derived: `basePoints(clearType) × levelAtClear` (DR-80) | 0 when `clearType` is `None`.                                                       |

#### ScoreAward

A single addition to `score`. Modelled explicitly so that the invariant in **Score Monotonicity and Range
(DR-83)** is structural — `score` is the sum of awards and there is no operation that subtracts.

| Attribute  | Type        | Constraints                                   | Description                                                                          |
| ---------- | ----------- | --------------------------------------------- | ------------------------------------------------------------------------------------ |
| points     | integer     | ≥ 0                                           | Never negative. An award of 0 is legal and is simply a no-op on `score`.              |
| source     | ScoreSource | required                                      | Which rule produced it.                                                               |
| cellCount  | integer?    | ≥ 0; present only for `SoftDrop` / `HardDrop` | Cells descended or travelled, for traceability and testing.                           |

> **Extension point.** The four excluded bonuses in §1.3 would each be a further `ScoreSource` member
> producing a further `ScoreAward` at the same point in the lock resolution. Neither `LineClearResult` nor
> any collapse rule would need to change.

### 5.4 Enumerations

#### ClearType

Classification of a single lock resolution by how many rows it cleared (DR-79).

| Member   | Rows cleared | Base points (before the `level` multiplier) |
| -------- | ------------ | ------------------------------------------- |
| `None`   | 0            | 0                                            |
| `Single` | 1            | 100                                          |
| `Double` | 2            | 300                                          |
| `Triple` | 3            | 500                                          |
| `Tetris` | 4            | 800                                          |

Closed set. No value above `Tetris` is reachable — a tetromino occupies exactly four cells (**Tetromino Cell
Count (DR-05)**) spanning at most four rows, so at most four rows can newly become full (DR-87).

#### ScoreSource

Why points were awarded.

| Member      | Rule   | Multiplied by `level`? |
| ----------- | ------ | ---------------------- |
| `LineClear` | DR-80  | **Yes**                |
| `SoftDrop`  | DR-81  | No                     |
| `HardDrop`  | DR-81  | No                     |

### 5.5 Domain Rules and Invariants

_New rules are numbered from **DR-75**. 001's **DR-01 – DR-27** remain in force unchanged; where a rule here
refines one of them it says so explicitly. Later specs should cite these by ID and name, e.g. "per **Row
Collapse (DR-77)**"._

- **DR-75 — Full Row**: A row is *full* exactly when all 10 of its `Cell`s are occupied, i.e. every
  `Cell.occupant` in that row is non-null. The test is identical for buffer rows (0–19) and visible rows
  (20–39) — a full buffer row clears on exactly the same terms as a full visible row, even though the player
  cannot see it (**Playfield Geometry (DR-01)**).
- **DR-76 — Clear Detection Scope**: Detection runs after every lock (**Locking (DR-19)**) and examines all
  40 rows of the `Playfield`. Every full row found is a cleared row. Only rows touched by the just-locked
  piece can *newly* become full — a row cannot fill itself — but the check is specified over the whole
  playfield so that correctness never depends on tracking which rows a piece occupied. Detection reads the
  playfield *after* the piece's four cells have been written, and never considers the `ActivePiece`, which no
  longer exists at this point (DR-19).
- **DR-77 — Row Collapse**: Let `C` be the set of cleared row indices. The collapse is defined as a single
  simultaneous remapping:
  - every surviving row `r` (that is, `r ∉ C`) moves to row index `r + |{ c ∈ C : c > r }|`, i.e. it shifts
    down by the number of cleared rows **below** it;
  - the topmost `|C|` rows — indices `0` through `|C| - 1` — become entirely empty;
  - every block keeps its `column`; no block ever changes column and no block ever moves upward;
  - the relative vertical order of surviving rows is preserved exactly.

  The mapping is order-preserving and onto rows `|C| … 39`, so no row is lost, duplicated or overwritten.
  Worked example for `C = {25, 31}`: old row 32 stays at 32; old row 30 moves to 31; old row 26 moves to 27;
  old row 24 moves to 26; old row 0 moves to 2; rows 0 and 1 end up empty. This non-adjacent case is the one
  that naive "shift everything above the topmost cleared row down by the total count" implementations get
  wrong, and FR-004 and SC-004/SC-005 exist specifically to pin it.
- **DR-78 — No Cascade Gravity**: After a collapse the stack is **not** re-settled. Blocks left unsupported —
  floating above a gap — stay exactly where DR-77 put them. There is no cascade, sticky-gravity or chain-clear
  behaviour, and therefore no possibility of a second detection pass arising from the first. Each lock
  resolves at most one round of detection and collapse.
- **DR-79 — Clear Type Classification**: The number of rows cleared by one lock maps one-to-one to a
  `ClearType`: 0 → `None`, 1 → `Single`, 2 → `Double`, 3 → `Triple`, 4 → `Tetris`. The classification is
  derived, never stored independently of `clearCount`.
- **DR-80 — Line Clear Score**: A lock resolution awards `basePoints(clearType) × level` points, where
  `basePoints` is 0 / 100 / 300 / 500 / 800 for `None` / `Single` / `Double` / `Triple` / `Tetris`. Because
  `level ≥ 1` (001 §5.1), the multiplier never zeroes or reduces an award. A `ClearType` of `None` awards
  nothing; no award is recorded for it.
- **DR-81 — Drop Score**: Player-initiated descent is worth **1 point per cell** for a soft drop and
  **2 points per cell** for a hard drop. Neither is multiplied by `level`. Soft-drop points accrue as each
  cell is descended (story 002 supplies the count as `ActivePiece.softDropCells`); hard-drop points are
  awarded when the hard drop resolves, immediately before the lock is written (story 003 supplies the
  distance travelled as its `cellsTravelled`). Cells descended by
  ordinary gravity (**Gravity Step (DR-14)**) are worth nothing. Drop points are entirely independent of line
  clears: a drop that clears nothing still pays, and a drop that clears four rows pays both.
- **DR-82 — Lines Cleared Counter**: `GameSession.linesCleared` is a cumulative count of rows cleared in the
  session. It increases by `clearCount` at each lock resolution, never decreases, and is the **single source
  of truth** for the line total — story 007 computes the level from it and story 008 displays it. Neither may
  maintain its own count. It is reset to 0 only by **Fresh Session State (DR-23)**.
- **DR-83 — Score Monotonicity and Range**: `score` is a non-negative integer equal to the sum of every
  `ScoreAward` made in the session. It is non-decreasing for the session's lifetime: no rule in this or any
  other spec subtracts from it, and the only way it returns to 0 is a new `GameSession` (DR-23). The highest
  value a single lock can produce is `800 × level + 2 × 39`; even a pathological ten-hour session at a high
  level stays below 600 million, comfortably inside a 32-bit signed integer. `score` is nevertheless held as a
  **64-bit signed integer** so that overflow cannot arise under any sequence of play, and it must be rendered
  in full — no overflow, truncation, ellipsis, rounding or scientific notation at any value (see DR-88 and
  NFR-004).
- **DR-84 — Multiplier Level**: The `level` used by DR-80 is the value in effect at the instant the clear
  resolves — that is, **before** story 007 applies any level increase triggered by the lines this very clear
  contributed. A clear that pushes the player over a level threshold is paid at the old level.
- **DR-85 — Lock Resolution Sequence**: This rule discharges 001's **Spawn Immediacy (DR-20)** hand-off
  (*"Story 006 inserts its line-clear resolution between the lock and the spawn"*). A lock resolves in exactly
  this order, as one indivisible transition:
  1. If the lock was reached by a hard drop, its drop points are awarded (DR-81).
  2. The piece's four cells are written into the `Playfield` and the `ActivePiece` is cleared (DR-19).
  3. The **lock out** condition (**Lock Out (DR-22)**) is evaluated against the cells as just written — that
     is, against their *pre-collapse* positions — and the outcome is held.
  4. **Line clear resolution**: detect full rows (DR-75, DR-76); collapse (DR-77); increase `linesCleared`
     (DR-82); award line clear points (DR-80); record `lastLineClear`.
  5. If step 3 detected a lock out, the session moves to `GameStatus.GameOver` (DR-22) and no spawn occurs
     (**Terminal Status (DR-27)**). Otherwise the next piece spawns immediately (DR-20), and **Block Out
     (DR-21)** is evaluated against the **post-collapse** playfield.

  Two consequences are deliberate and load-bearing. Step 4 before step 5 means the player is paid for a clear
  made by the piece that ends the game (FR-021). Step 5's post-collapse evaluation means a clear that frees
  the spawn area prevents a block out that would otherwise have occurred (FR-008).
- **DR-86 — Line Clear Atomicity**: Steps 1–5 of DR-85 are atomic with respect to everything else in the
  game. No gravity step, keyboard input, rotation, soft or hard drop, hold (story 005), preview refresh
  (story 004), level change (story 007) or spawn is processed part-way through. An observer of the
  `GameSession` sees either the pre-lock state or the fully resolved post-collapse state, never an
  intermediate one in which rows have been detected but not collapsed, or collapsed but not scored.
- **DR-87 — Clear Count Bound**: A single lock clears between 0 and 4 rows inclusive. A tetromino occupies
  exactly four cells (**Tetromino Cell Count (DR-05)**) and therefore spans at most four rows, and a row can
  only become full by having a cell written into it, so no more than four rows can newly become full. A count
  outside 0–4 is a defect, not a game state.
- **DR-88 — Live Score Visibility**: `score` and `linesCleared` are visible to the player for the whole of
  `GameStatus.Playing` and are updated no later than the first frame rendered after a lock resolves. The
  displayed values always equal the session values — there is no animated count-up that lags behind the true
  score, and no value is abbreviated, truncated or rounded.
- **DR-89 — Clear Feedback Non-Interference**: Any visual feedback for a clear is presentation only. It may
  not delay, gate, extend or alter the DR-85 sequence, and 001's **Spawn Immediacy (DR-20)** continues to
  hold: there is no animation gate before the next spawn. Feedback plays over an already-updated playfield,
  lasts at most 200 ms, and is safe to skip entirely. Omitting it changes no game state and no acceptance
  scenario outcome.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty).
001's NFR-001 (frame rate), NFR-006 (determinism and injectable time) and NFR-007 (no personal data) continue
to apply and are not restated._

| ID      | Category        | Requirement                                                                                                                                                                                             |
| ------- | --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance     | A full lock resolution — detection across all 40 rows, collapse, counter update and scoring — completes in **under 2 ms** on a mid-range Info Support laptop, and never exceeds a single 16 ms frame budget, so a clear causes no observable stutter or dropped frame. |
| NFR-002 | Performance     | The cost of a lock resolution is **independent of how many rows clear**: a Tetris takes no more than 1.5× the time of a lock that clears nothing, so the game does not hitch precisely at the most rewarding moment. |
| NFR-003 | Testability     | Line clear resolution is a deterministic function of the playfield state, the `level` and the drop distances — given identical inputs it produces an identical playfield, `LineClearResult` and score delta, with no dependence on wall-clock time, frame timing or randomness. All §4 scenarios run headlessly. |
| NFR-004 | Usability       | The score and line count are legible at a glance without taking the eyes off the playfield: text contrast **≥ 4.5:1** against its background, and the fields widen rather than truncate, displaying at least **10 digits** without clipping or reflowing the playfield. |
| NFR-005 | Accessibility   | Any clear effect flashes **no more than three times per second** and conveys the clear by motion or shape as well as by colour, so it is safe and perceivable for photosensitive and colour-blind players (WCAG 2.3.1 and 1.4.1). |
| NFR-006 | Reliability     | Over a 10,000-lock soak, the playfield contains no block outside rows 0–39 or columns 0–9, no full row survives a lock resolution, and `score` and `linesCleared` are each exactly the sum of their per-lock deltas — no drift, no double-counting, no lost award. |
| NFR-007 | Correctness     | `score` never overflows, saturates or wraps for any reachable value, and `linesCleared` is exact — never estimated, sampled or recomputed from the score (DR-82, DR-83).                                 |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                          | Expected Behavior                                                                                                                                                                      |
| ----- | ------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | A piece locks and completes no row.                                                                | `ClearType` is `None`, 0 line clear points, `linesCleared` unchanged (DR-79, DR-80). Any drop points for that piece are still awarded (DR-81). `lastLineClear` is still recorded.          |
| EC-2  | Two cleared rows are non-adjacent, e.g. rows 36 and 39 from a vertical I.                          | Rows between them shift down one; rows above both shift down two (DR-77). This is the classic implementation bug — SC-004 and SC-005 are the regression tests.                             |
| EC-3  | A collapse leaves blocks floating over a gap.                                                      | They stay floating (DR-78). No cascade, no chain clear, no second detection pass. This is correct Guideline behaviour, not a bug to fix.                                                   |
| EC-4  | A row in the hidden buffer rows becomes full.                                                      | It clears on exactly the same terms as a visible row, scores the same and counts the same (DR-75). The player sees only the resulting shift at the top of the visible field.               |
| EC-5  | The lock that clears rows also frees the next piece's spawn cells.                                 | Block out (DR-21) is evaluated post-collapse, so no block out occurs and play continues (DR-85 step 5). A good clear genuinely saves the run.                                              |
| EC-6  | The lock that clears rows also triggers lock out (DR-22).                                          | Points are awarded and `linesCleared` incremented first, then the session moves to `GameOver` (DR-85, FR-021). Story 008's final score includes the last clear.                            |
| EC-7  | A clear empties the playfield entirely (a "perfect clear").                                        | It scores exactly as an ordinary clear of that size. No perfect clear bonus — explicit non-goal (§1.3). The empty playfield is a normal state; the next piece spawns as usual.             |
| EC-8  | Consecutive locks each clear rows (a combo), or consecutive Tetrises (back-to-back).               | Each is scored independently by DR-80. No combo counter, no back-to-back multiplier — explicit non-goals (§1.3).                                                                           |
| EC-9  | A hard drop travels 0 cells because the piece is already grounded.                                 | 0 drop points (DR-81). The lock still resolves normally and any resulting clear still scores.                                                                                             |
| EC-10 | Soft drop is held while the piece is grounded and cannot descend.                                  | No cell is descended, so no points accrue (DR-81). Holding the key does not farm points.                                                                                                  |
| EC-11 | A piece soft-drops several cells, becomes un-grounded again via story 002's input, and drops more. | Every soft-dropped cell pays once, as it is descended (DR-81). Points already awarded are never revoked (DR-83), even though the piece's final resting row may be higher than a prior one. |
| EC-12 | Story 007 is not yet implemented, so `level` is fixed at 1 (001 FR-002).                           | Every line clear award is multiplied by 1 — 100 / 300 / 500 / 800. The model is unchanged; only the multiplier's value is.                                                                 |
| EC-13 | A clear takes the player past a level threshold.                                                   | The clear is paid at the **old** level (DR-84). Story 007 raises the level afterwards; the next clear is paid at the new one.                                                             |
| EC-14 | The player activates *New game* mid-session with a high score on screen (001 FR-005, EC-8).        | `score` and `linesCleared` reset to 0 (DR-23, FR-019). Nothing is recorded or submitted — story 009 is the only path to the leaderboard.                                                   |
| EC-15 | The browser tab is throttled and a large time delta arrives during a clear.                        | Irrelevant to this spec: the resolution is atomic and time-independent (DR-86, NFR-003). 001's **DR-26** delta cap governs the gravity that follows.                                       |
| EC-16 | A full row is somehow present that the just-locked piece did not touch.                            | It is cleared anyway — detection is defined over all 40 rows (DR-76), so correctness never depends on the touched-row optimisation being right.                                           |
| EC-17 | All four of a vertical I's rows are full, giving a `clearCount` of 4.                              | A Tetris: 800 × level, `linesCleared` += 4 (DR-87 is at its bound). No higher `ClearType` exists or is reachable.                                                                         |
| EC-18 | A clear effect is still playing when the next piece becomes grounded.                              | The effect is purely visual and never gates state (DR-89). It is cut short or overlapped; the game does not wait for it.                                                                   |
| EC-19 | `score` is read by story 008 or 009 at the moment of game over.                                    | It is final and complete, including the terminal lock's drop and clear points (DR-85). No award is pending or in flight.                                                                   |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4, matching 001's convention._

| ID      | Criterion                                                                                                                                                                   |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 25 acceptance scenarios in §4 pass as automated tests in CI, including every row of SC-013's examples table.                                                               |
| SUC-002 | A property test over 10,000 randomly generated playfield states confirms the DR-77 collapse: no surviving block changes column, no block moves up, relative row order is preserved, exactly `|C|` top rows end empty, and no full row survives. |
| SUC-003 | The non-adjacent collapse cases (SC-004, SC-005) and the 2-, 3- and 4-row adjacent cases each have a dedicated regression test that fails against a naive "shift by total count" collapse. |
| SUC-004 | A seeded 500-piece session replays to a byte-identical final playfield, `score` and `linesCleared` on every run (NFR-003).                                                      |
| SUC-005 | For a scripted session, the final `score` equals an independently hand-computed total from the per-lock clear counts, levels and drop distances — to the point, with no rounding. |
| SUC-006 | All 15 domain rules introduced in §5.5 (DR-75 – DR-89) are each covered by at least one automated test.                                                                        |
| SUC-007 | A player clearing four rows at once scores more than the same player clearing those four rows across four locks, demonstrably on screen (SC-014) — the multi-line bonus is perceptible without explanation. |
| SUC-008 | A 10,000-lock soak completes with no invalid playfield state, no score drift and no frame exceeding 16 ms during a lock resolution (NFR-001, NFR-006).                          |
| SUC-009 | Stories 007 and 008 can be built reading only `GameSession.score`, `GameSession.linesCleared` and `GameSession.lastLineClear`, without maintaining a parallel line or score count. |
| SUC-010 | Adding any one of the four excluded bonuses (§1.3) later requires only a new `ScoreSource` member and a new `ScoreAward` at DR-85 step 4 — no change to DR-75 through DR-81.    |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[001 — Start a New Game](001-start-new-game.md)** — hard dependency. This spec is written entirely in
  001's §5 vocabulary and attaches to its lock/spawn sequence. In particular it consumes **Locking (DR-19)**,
  **Spawn Immediacy (DR-20)**, **Block Out (DR-21)**, **Lock Out (DR-22)**, **Fresh Session State (DR-23)**,
  **Terminal Status (DR-27)**, the `Playfield` geometry of **DR-01**, the coordinate system of §5.0, and
  `GameSession.level` and `GameSession.score`.
- **Story 002 (move, rotate, soft drop)** — supplies the number of cells descended by soft drop, which DR-81
  converts to points. 006 can be built and tested without 002 by treating that count as 0.
- **Story 003 (hard drop)** — supplies the distance travelled by a hard drop, which DR-81 converts to points.
  Same fallback: without 003 the count is 0 and only gravity and soft drop reach the lock.
- **The modern Tetris Guideline** — the normative ruleset for the 100 / 300 / 500 / 800 curve, the 1- and
  2-point drop values, and the no-cascade collapse.
- **Downstream dependents**: **story 007** reads `linesCleared` to compute the level and writes the `level`
  that DR-80 multiplies by; **story 008** reads `score` and `linesCleared` for the game over screen; **story
  009** submits the final `score` to the leaderboard. None of the three may maintain its own line or score
  count (DR-82, SUC-009).

### 9.2 Constraints

- **The lock→spawn window is exclusive.** 001's DR-20 permits line clear resolution and *nothing else*
  between a lock and the next spawn. Stories 004, 005 and 007 must not add work there; anything they need
  belongs before the lock or after the spawn.
- **`level` is read-only here.** This spec must not write `level` under any circumstance, even though it is
  the reason `level` changes. Story 007 owns that write (DR-84).
- **`linesCleared` is the only line count.** No derived or duplicate counter anywhere in the system.
- **No persistence.** `score` and `linesCleared` live only in the session. Nothing here writes to storage or
  the network; story 009 is the first point at which a score leaves the page (001 NFR-007).
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed by this spec):
  ASP.NET Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`.
  No test project exists yet; SUC-001 through SUC-008 require one.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*, exactly as 001 §9.3 does.

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                                 |
| --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01. Introduction and Goals (`01-introduction-and-goals.md`)      | Should record scoring as the mechanism behind the story map's "office bragging rights" goal — the score defined here is the number that ends up on the leaderboard. *Currently empty.*          |
| 05. Building Block View (`05-building-block-view.md`)            | 006 adds a line clear resolution and a scoring concern to the core game-domain building block introduced by 001, and a score/line readout to the presentation layer. *Currently empty.*         |
| 06. Runtime View (`06-runtime-view.md`)                          | 006 refines 001's central runtime scenario by expanding the lock→spawn step into the five-step DR-85 sequence. This is the single most important runtime detail in the game. *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)      | Needs the project-wide rule that score and line count have exactly one owner (DR-82, DR-83) and are read, never recomputed, by stories 007, 008 and 009. *Currently empty.*                     |
| 09. Architecture Decisions (`09-architecture-decisions.md`)      | 006 warrants ADRs for: the DR-77 shift-by-cleared-rows-below collapse over the naive variant, no cascade gravity (DR-78), the Guideline scoring curve over the NES/original curve, and scoring the clear before the game over transition (DR-85). *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)          | Should host the project-wide frame budget that NFR-001 and NFR-002 draw on, and the accessibility baseline behind NFR-005. *Currently empty.*                                                   |
| 12. Glossary (`12-glossary.md`)                                  | Should gain: full row, cleared row, collapse, cascade gravity, Single / Double / Triple / Tetris, line clear resolution, drop score, lines cleared. *Currently empty.*                          |

## 10. Open Questions

| #   | Question                                                                                                 | Owner      | Status   | Resolution                                                                                                                                                                                                                                                                           |
| --- | ---------------------------------------------------------------------------------------------------------- | ---------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Does a clear that coincides with lock out (DR-22) still score?                                             | Daan Harms | Resolved | **Yes.** DR-85 awards the points and increments `linesCleared` before the `GameOver` transition. The alternative — silently voiding the last clear — would be indistinguishable from a bug to the player and would make story 008's final score wrong.                                   |
| 2   | Is block out (DR-21) evaluated before or after the collapse?                                               | Daan Harms | Resolved | **After** (DR-85 step 5). A clear genuinely rescues the spawn area, which is both the Guideline behaviour and the better game. Evaluating before the collapse would end runs that the player had just saved.                                                                            |
| 3   | Should the level multiplier use the level before or after any level-up this clear causes?                  | Daan Harms | Resolved | **Before** (DR-84). The Guideline behaviour, and the only ordering that keeps the award a pure function of state already settled when the clear resolves — otherwise 006 and 007 would have a circular dependency at every threshold.                                                   |
| 4   | Should the clear use an animation, and if so may it pause the game?                                        | Daan Harms | Resolved | An effect is **optional and never blocking** (DR-89, FR-022). 001's DR-20 forbids an animation gate before the spawn, and the story map's "zero friction, games are short" framing argues against anything that slows the loop. Capped at 200 ms, purely decorative.                    |
| 5   | Should blocks left floating by a collapse fall (cascade gravity)?                                          | Daan Harms | Resolved | **No** (DR-78). Cascade gravity is a different game — it changes stacking strategy fundamentally and would interact with scoring via chain clears, which are an explicit non-goal. Classic and Guideline Tetris both leave floating blocks floating.                                    |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-011, SC-023; US-002 -> SC-012..SC-016; US-003 -> SC-017..SC-021;
        US-004 -> SC-022, SC-024, SC-025
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-008, FR-016 -> US-001; FR-009..FR-011, FR-017, FR-020, FR-021 -> US-002;
         FR-012..FR-015 -> US-003; FR-018, FR-019, FR-022 -> US-004)
  - [x] Domain model covers all entities mentioned in the requirements
        GameSession extended with score (ownership), linesCleared, lastLineClear;
        value objects LineClearResult, ScoreAward; enums ClearType, ScoreSource.
        Playfield, Cell, ActivePiece, PieceQueue, TetrominoType are 001's and are referenced, not redefined.
  - [x] Domain rules and invariants are listed (DR-75 .. DR-89; 001's DR-01 .. DR-27 remain in force)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-19)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-007)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all five resolved)
-->
