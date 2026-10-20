# Feature Specification: Start a New Game — Playfield, Tetrominoes, Spawn and Gravity

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
| Feature ID      | 001                                                                                           |
| Status          | Draft                                                                                         |
| Author          | Daan Harms                                                                                    |
| Created         | 2026-10-20                                                                                    |
| Last updated    | 2026-10-20                                                                                    |
| Epic / Parent   | Play Tetris (story map activity) — `docs/product/story-map.md`                                |
| Arc42 reference | Sections 3, 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders. |

> **Note on this spec.** 001 is the foundational slice. Every later spec in the *Play Tetris* activity
> (002–008) is written against the vocabulary defined in **§5 Domain Model**. Terms, entity names,
> enum members and domain rule IDs defined here are normative and must be reused verbatim rather than
> re-invented. Where this spec deliberately stops short of a decision because it belongs to a later
> story, it says so explicitly and names the owning story.
>
> **Ruleset.** Every detail not otherwise constrained by the story map is resolved against the modern
> **Tetris Guideline**. This is a standing project decision, not an assumption to revisit per-spec.

### 1.1 Problem Statement

An Info Support colleague on a coffee break has nothing to play, and there is currently no running game
at all — the application is an unmodified Razor Pages scaffold. Before any of the game's depth (movement,
scoring, levels, leaderboard) can exist, there must be a playable core: a board, the seven tetromino
shapes, a fair piece sequence, and pieces that fall and settle on their own. Without this foundation
every other story in the map is blocked.

### 1.2 Goal

A colleague opens the page, triggers a single start action with no sign-up, and sees a standard
10×20 Tetris playfield in which tetrominoes spawn one at a time from a fair 7-bag sequence, fall
automatically at one cell per second, and lock into the stack when they can fall no further — after
which the next piece spawns immediately. The game runs hands-free and indefinitely until the stack
reaches the spawn area.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec. Each is either a separate story or a
project-wide exclusion. Do not implement them here, and do not design around a guess at how they
will work — the owning spec defines that.

- **Player input of any kind** — moving, rotating and soft-dropping the falling piece belong to **story 002**.
  This spec defines the *lock delay reset budget* that 002's input will consume, but not the input itself.
- **Hard drop** — **story 003**.
- **Next-piece preview** — **story 004**. This spec defines the `PieceQueue` that 004 will surface, but
  renders nothing from it.
- **Hold / swap** — **story 005**. `GameSession` gains no hold slot here.
- **Line clearing, scoring and multi-line bonuses** — **story 006**. In this spec a completed row simply
  stays on the board; `score` exists and remains 0.
- **Level progression and the gravity speed curve** — **story 007**. This spec fixes the game at level 1
  and uses only the level-1 fall rate.
- **Game over presentation and final score** — **story 008**. This spec defines the *block out* and
  *lock out* conditions that end a session and the terminal `GameOver` status, but not what the player
  is shown when it happens.
- **Leaderboard, name entry, score submission** — stories 009–011.
- **Info Support branding and typography** — story 013. This spec fixes the *type → colour* mapping to the
  Guideline palette; 013 may re-tune the exact hex values (see NFR-003).
- **Controls / how-to-play panel** — story 014.
- **Ghost piece** — out of scope project-wide (story map: "the user wants the challenge").
- **Pause** — out of scope project-wide (story map: "games are short by design").
- **Accounts, registration, login, persisted sessions** — out of scope project-wide.
- **Mobile and touch support** — out of scope project-wide (laptop + keyboard only).
- **Entry delay (ARE) between lock and the next spawn** — explicitly excluded; see DR-20.

## 2. User Stories

### US-001: Start playing immediately

**As a** colleague on a short break,
**I want** to open the page and start a game with one action and no sign-up,
**so that** I spend my five minutes playing instead of filling in forms.

### US-002: A fair, standard set of pieces

**As a** player,
**I want** the seven standard tetrominoes to arrive in a fair, bounded sequence rather than at pure random,
**so that** the game feels like real Tetris and I am never starved of the piece I am setting up for.

### US-003: Pieces fall on their own

**As a** player,
**I want** the current piece to descend steadily without me doing anything,
**so that** the game has the time pressure that makes Tetris a game rather than a puzzle.

### US-004: Pieces settle into a stack

**As a** player,
**I want** a piece that can fall no further to settle into the stack after a brief grace period, and the
next piece to follow straight away,
**so that** the board builds up continuously and I always have a moment to make a final adjustment.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) are defined in §5.4._

| ID     | Requirement                                                                                                                                                                                                                | Priority | User Story |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall present a start affordance on page load and begin a `GameSession` when the player activates it, requiring no registration, login or any other data entry.                                                   | Must     | US-001     |
| FR-002 | The system shall initialise a new `GameSession` with a `Playfield` whose every `Cell` is empty, `level` of 1, `score` of 0, no `ActivePiece`, and a freshly seeded `PieceQueue` (DR-23).                                      | Must     | US-001     |
| FR-003 | The system shall display the 20 visible rows × 10 columns of the `Playfield`, drawing each occupied `Cell` in the colour of the `TetrominoType` that locked there (DR-24).                                                    | Must     | US-001     |
| FR-004 | The system shall not render the 20 buffer rows; a piece occupying only buffer rows is invisible to the player (DR-01).                                                                                                       | Must     | US-001     |
| FR-005 | The system shall offer a *New game* action while a session is in progress that discards the current `GameSession` and starts a fresh one per FR-002, without submitting or recording anything from the discarded session.     | Should   | US-001     |
| FR-006 | The system shall support exactly the seven tetromino types I, O, T, S, Z, J and L, each occupying exactly four cells in every orientation, with the shapes given in §5.3 (DR-05).                                             | Must     | US-002     |
| FR-007 | The system shall assign each `TetrominoType` its Tetris Guideline colour, as a stable one-to-one mapping (DR-24).                                                                                                            | Must     | US-002     |
| FR-008 | The system shall generate the piece sequence with a 7-bag randomiser: each bag is a uniformly shuffled permutation of all seven types, and a bag is consumed in full before the next is generated (DR-09, DR-10).             | Must     | US-002     |
| FR-009 | The system shall keep at least seven upcoming types in the `PieceQueue` at all times, so that downstream previews can read ahead without perturbing the sequence (DR-13).                                                     | Must     | US-002     |
| FR-010 | The system shall spawn each piece in `Orientation.Spawn`, horizontally centred with a left bias, with its lowest occupied cell in the bottom-most buffer row, immediately above the visible field (DR-06, DR-07).             | Must     | US-002     |
| FR-011 | The system shall, immediately on spawn and before any gravity interval elapses, step the newly spawned piece down one cell if that cell range is free (DR-08).                                                                | Must     | US-002     |
| FR-012 | The system shall produce a reproducible piece sequence when the randomiser is given a fixed seed, so acceptance scenarios can assert on specific pieces.                                                                      | Should   | US-002     |
| FR-013 | The system shall move the `ActivePiece` down exactly one cell for every elapsed gravity interval, which is 1.000 second at level 1 (DR-14).                                                                                   | Must     | US-003     |
| FR-014 | The system shall advance gravity from elapsed wall-clock time rather than rendered frames, so the fall rate is identical regardless of display refresh rate or frame drops.                                                   | Must     | US-003     |
| FR-015 | The system shall never move, place or lock any part of the `ActivePiece` outside the `Playfield` bounds or into an occupied `Cell` (DR-03, DR-04).                                                                            | Must     | US-003     |
| FR-016 | The system shall stop applying gravity to a *grounded* piece — one that cannot step down one cell — and start a 0.500 second lock delay instead (DR-15, DR-16).                                                               | Must     | US-004     |
| FR-017 | The system shall cancel the lock delay and resume gravity if a grounded piece ceases to be grounded before the delay expires (DR-18).                                                                                        | Must     | US-004     |
| FR-018 | The system shall maintain a per-piece budget of 15 lock delay resets, replenished whenever the piece reaches a row lower than any it has previously occupied; once the budget is exhausted no further reset is granted (DR-17). | Must     | US-004     |
| FR-019 | The system shall lock a piece when its lock delay expires, writing its four cells into the `Playfield` as occupied cells of its type and clearing the `ActivePiece` (DR-19).                                                  | Must     | US-004     |
| FR-020 | The system shall spawn the next piece from the `PieceQueue` immediately after a lock resolves, with no entry delay (DR-20).                                                                                                   | Must     | US-004     |
| FR-021 | The system shall detect *block out* — a piece whose spawn cells are already occupied — and move the session to `GameStatus.GameOver` (DR-21). What the player is then shown is specified by story 008.                        | Must     | US-004     |
| FR-022 | The system shall detect *lock out* — a piece that locks with all four of its cells in buffer rows — and move the session to `GameStatus.GameOver` (DR-22). Presentation again belongs to story 008.                           | Must     | US-004     |
| FR-023 | The system shall hold at most one `ActivePiece` per `GameSession` at any time (DR-25).                                                                                                                                       | Must     | US-003     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash._

### SC-001: Starting a game needs one action and no details (FR-001)

```gherkin
Given a colleague has opened the Tetris page for the first time
  And they have not registered, logged in or entered any information
When they activate the start action
Then a game session begins in status Playing
  And the playfield is shown with all 200 visible cells empty
  And no name, email or other personal detail has been requested at any point
```

### SC-002: A new game starts from a clean slate (FR-002)

```gherkin
Given a game session has just started
When the initial state is inspected
Then every cell of the playfield — visible and buffer — is empty
  And the level is 1
  And the score is 0
  And the piece queue holds at least seven upcoming tetromino types
```

### SC-003: Only the visible field is drawn (FR-003, FR-004)

```gherkin
Given a game session is in progress
  And the active piece occupies only buffer rows
When the player looks at the playfield
Then the playfield shows exactly 20 rows by 10 columns
  And no part of the active piece is visible
```

### SC-004: A locked piece keeps its colour (FR-003, FR-007)

```gherkin
Given a T tetromino has locked into the stack
When the player looks at the playfield
Then the four cells it occupied are drawn in the T colour (purple)
  And they remain that colour for the rest of the session
```

### SC-005: Restarting discards the current game (FR-005)

```gherkin
Given a game session is in progress with a partially built stack
When the player activates the new game action
Then the previous session is discarded
  And a fresh session begins with an empty playfield at level 1 with score 0
  And nothing from the discarded session is recorded or submitted anywhere
```

### SC-006: Every piece is a four-cell tetromino (FR-006)

```gherkin
Given a game session is in progress
When any piece spawns
Then its type is one of I, O, T, S, Z, J or L
  And it occupies exactly four cells
  And its shape matches the spawn-orientation shape defined for that type
```

### SC-007: Each bag contains all seven pieces exactly once (FR-008)

```gherkin
Given a game session has just started
When the first seven pieces have been drawn from the queue
Then the seven pieces are I, O, T, S, Z, J and L in some order
  And no type has appeared twice
```

### SC-008: The sequence is fair across bag boundaries (FR-008)

```gherkin
Given a game session has just started
When 70 consecutive pieces have been drawn
Then each of the seven types has been drawn exactly ten times
  And no type has appeared more than twice consecutively
  And no more than 12 pieces separate two consecutive occurrences of the same type
```

### SC-009: The queue stays stocked for look-ahead (FR-009)

```gherkin
Given a game session is in progress
When a piece is drawn from the queue
Then the queue still holds at least seven upcoming types
```

### SC-010: Pieces spawn centred, upright, just above the visible field (FR-010)

```gherkin
Given a game session has just started with an empty playfield
When the first piece spawns
Then it is in the Spawn orientation
  And its lowest occupied cell is in the bottom-most buffer row
  And its leftmost occupied column is floor((10 - its spawn width) / 2)
```

### SC-011: Spawn placement for each of the seven types (FR-010)

```gherkin
Given an empty playfield with columns numbered 0-9
When each type spawns in turn
Then I occupies columns 3, 4, 5 and 6
  And O occupies columns 4 and 5
  And T, S, Z, J and L each occupy columns 3, 4 and 5
```

### SC-012: A spawning piece steps into view straight away (FR-011)

```gherkin
Given an empty playfield
When a piece spawns
Then it immediately steps down one cell without waiting for a gravity interval
  And at least one of its cells is now in the top visible row
```

### SC-013: The initial step is skipped when blocked (FR-011, FR-015)

```gherkin
Given the stack reaches the top visible row directly beneath the spawn position
When a piece spawns above it with its spawn cells still free
Then the piece does not step down
  And it remains in its spawn position
  And it is grounded
```

### SC-014: The same seed gives the same game (FR-012)

```gherkin
Given two game sessions started with the same randomiser seed
When 50 pieces have been drawn in each
Then the two sequences of 50 types are identical
```

### SC-015: Gravity moves a piece one cell per second at level 1 (FR-013, FR-014)

```gherkin
Given a game session at level 1 with a piece in free space
When 1.000 second of game time elapses
Then the piece has moved down exactly one cell
When a further 3.000 seconds elapse
Then the piece has moved down exactly three more cells
```

### SC-016: Gravity is unaffected by frame rate (FR-014)

```gherkin
Given two game sessions at level 1, one rendering at 60 frames per second and one at 30
When 10 seconds of wall-clock time elapse in each with no interference
Then the piece has fallen the same number of cells in both sessions
```

### SC-017: A piece grounded on the floor locks after the delay (FR-016, FR-019)

```gherkin
Given a piece has fallen to the bottom row of the playfield
When it becomes grounded
Then gravity stops acting on it
  And after 0.500 second it locks
  And its four cells become occupied cells of its type
  And it is no longer the active piece
```

### SC-018: A piece grounded on the stack locks after the delay (FR-016, FR-019)

```gherkin
Given the playfield contains a stack of locked cells
  And a falling piece reaches the cell directly above that stack
When 0.500 second passes without the piece becoming un-grounded
Then the piece locks into the stack
  And the stack now includes its four cells
```

### SC-019: A piece that can fall again does not lock (FR-017)

```gherkin
Given a grounded piece whose lock delay is running
When the piece becomes un-grounded before the delay expires
Then the lock delay is cancelled
  And gravity resumes moving the piece down one cell per interval
  And the piece does not lock while it can still fall
```

### SC-020: The reset budget is bounded at 15 (FR-018)

```gherkin
Given a grounded piece whose lock delay has been reset 15 times
When a further reset is requested
Then no reset is granted
  And the piece locks when the running 0.500 second delay expires
```

### SC-021: Falling to a new low row replenishes the budget (FR-018)

```gherkin
Given a grounded piece that has used some of its 15 lock delay resets
When the piece subsequently occupies a row lower than any it has occupied before
Then its reset budget is replenished to 15
```

### SC-022: The next piece follows immediately (FR-020)

```gherkin
Given a piece has just locked into the stack
When the lock resolves
Then the next type is taken from the front of the piece queue
  And it spawns with no perceptible delay
  And the queue has advanced by exactly one type
```

### SC-023: A blocked spawn ends the session (FR-021)

```gherkin
Given the stack has grown so that cells in the spawn position are occupied
When the next piece attempts to spawn
Then the piece is not placed
  And the session moves to status GameOver
```

### SC-024: A piece locking entirely above the visible field ends the session (FR-022)

```gherkin
Given a piece is grounded with all four of its cells in buffer rows
When its lock delay expires and it locks
Then its cells are written to the playfield
  And the session moves to status GameOver
```

### SC-025: Only one piece falls at a time (FR-023)

```gherkin
Given a game session is in progress
When the playfield is inspected at any moment during play
Then there is at most one active piece
  And no second piece spawns while a piece is still active
```

## 5. Domain Model

_This is the normative vocabulary for the whole Play Tetris activity. Specs 002–008 must reuse these
names, enum members and rule IDs verbatim._

### 5.0 Coordinate system

One coordinate convention is fixed here and used by every later spec.

- The origin is the **top-left** cell. Both axes are **zero-based**.
- `column` runs **0 → 9, left to right**.
- `row` runs **0 → 39, top to bottom**. Gravity **increases** `row`.
- Rows **0–19** are the **buffer rows** (hidden). Rows **20–39** are the **visible rows**.
  Row 20 is the top visible row; row 39 is the floor row.
- A piece's `position` is the coordinate of the **top-left cell of its bounding box**, not of an
  occupied cell. The bounding box may extend outside the playfield; only *occupied* cells are
  constrained (DR-03).

> Tetris Guideline documents number rows 1–40 bottom-up, with rows 21–40 hidden. The mapping is
> `guidelineRow = 40 - row`. Only the top-down convention above is used in this project.

### 5.1 Entities

#### GameSession

One playthrough, from the start action to game over. The aggregate root: everything else in this
model is reached through it.

| Attribute          | Type            | Constraints                                     | Description                                                                                  |
| ------------------ | --------------- | ----------------------------------------------- | -------------------------------------------------------------------------------------------- |
| id                 | UUID            | PK, generated                                   | Identifies the session. Not persisted in this spec; story 009 persists a result, not a session. |
| status             | GameStatus      | required, default `Ready`                       | Lifecycle state. See the state machine below.                                                  |
| playfield          | Playfield       | required, exactly one                           | The grid and the stack.                                                                       |
| activePiece        | ActivePiece     | nullable                                        | The falling piece. Null before the first spawn, between lock and spawn, and after game over.   |
| pieceQueue         | PieceQueue      | required, exactly one                           | Source of upcoming types.                                                                     |
| level              | integer         | ≥ 1, fixed at 1 in this spec                    | Drives `gravityInterval`. Story 007 owns how it increases.                                     |
| score              | integer         | ≥ 0, fixed at 0 in this spec                    | Present for continuity. Story 006 owns how it increases.                                      |
| gravityInterval    | duration        | derived from `level`; 1.000 s at level 1        | Time per one-cell descent (DR-14).                                                             |
| startedAt          | datetime        | generated, immutable                            | When the session moved to `Playing`.                                                           |
| piecesSpawned      | integer         | ≥ 0, starts at 0                                | Count of pieces spawned this session. Useful for assertions and for story 008's summary.       |

**Lifecycle:** `Ready` → (player activates start) → `Playing` → (block out per DR-21 **or** lock out per
DR-22) → `GameOver`. `GameOver` is terminal: no further spawn, gravity or lock occurs. The *New game*
action (FR-005) does not transition a session; it discards it and constructs a new one in `Ready`.
Story 005 adds a hold slot to this entity; story 004 reads `pieceQueue`; story 008 owns the `GameOver`
experience.

#### Playfield

The grid of cells, holding both the empty space and the stack of locked blocks.

| Attribute       | Type             | Constraints                     | Description                                                       |
| --------------- | ---------------- | ------------------------------- | ----------------------------------------------------------------- |
| columnCount     | integer          | constant, 10                    | Width.                                                             |
| visibleRowCount | integer          | constant, 20                    | Rows shown to the player (rows 20–39).                             |
| bufferRowCount  | integer          | constant, 20                    | Hidden rows above the visible field (rows 0–19), where pieces spawn.|
| totalRowCount   | integer          | derived, 40                     | `visibleRowCount + bufferRowCount`.                                |
| cells           | Cell[40][10]     | required, fully allocated       | Indexed `[row][column]`. Every position always holds a `Cell`.     |

The **stack** is the set of occupied cells in the playfield — the blocks left behind by locked pieces.
It is a derived notion, not a stored attribute, and the term is used throughout specs 002–008.

#### ActivePiece

The single tetromino currently falling under gravity. Created on spawn, destroyed on lock.

| Attribute         | Type           | Constraints                                   | Description                                                                                 |
| ----------------- | -------------- | --------------------------------------------- | ------------------------------------------------------------------------------------------- |
| type              | TetrominoType  | required, immutable for the piece's lifetime  | Which of the seven shapes this is.                                                            |
| orientation       | Orientation    | required, `Spawn` at spawn time               | Current rotation state. Story 002 owns changing it.                                           |
| position          | CellPosition   | required                                      | Top-left of the bounding box (see §5.0).                                                      |
| fallTimerElapsed  | duration       | ≥ 0, < `gravityInterval`, starts at 0         | Time accumulated toward the next one-cell descent.                                            |
| lockTimerElapsed  | duration       | ≥ 0, ≤ 0.500 s, null while not grounded       | Time accumulated toward locking (DR-16).                                                      |
| lockResetsUsed    | integer        | 0–15                                          | Resets consumed from the budget of 15 (DR-17).                                                |
| lowestRowReached  | integer        | 0–39                                          | Lowest (largest) row index any of this piece's cells has occupied. Drives budget replenishment.|

`occupiedCells` is derived, never stored: the four `CellPosition`s obtained by applying the
`TetrominoDefinition` offsets for `type` and `orientation` to `position`.

#### PieceQueue

The ordered stream of upcoming tetromino types, fed by the 7-bag randomiser.

| Attribute      | Type              | Constraints                        | Description                                                                  |
| -------------- | ----------------- | ---------------------------------- | ---------------------------------------------------------------------------- |
| upcoming       | TetrominoType[]   | ordered, length ≥ 7 (DR-13)        | Front of the list is the next piece to spawn.                                 |
| currentBag     | SevenBag          | required                           | The bag currently being drained into `upcoming`.                              |
| seed           | integer           | optional, immutable                | When supplied, makes the whole sequence reproducible (FR-012).                |

Drawing takes the first element of `upcoming` and removes it; the queue then refills from bags until
it again holds at least seven. Story 004 reads `upcoming` to render the preview and must not mutate it.

### 5.2 Relationships

- A **GameSession** has exactly one **Playfield**, which exists for the session's whole lifetime.
- A **GameSession** has exactly one **PieceQueue**.
- A **GameSession** has **at most one ActivePiece** at any moment (DR-25) — zero before the first spawn,
  between a lock and the following spawn, and after game over.
- A **Playfield** is composed of exactly 400 **Cell** value objects, addressed by **CellPosition**.
- A **Cell** holds either nothing or the **TetrominoType** of the piece that locked there — never a
  reference to an `ActivePiece`. Once locked, a block has no identity beyond its type.
- An **ActivePiece** refers to exactly one **TetrominoType** and one **Orientation**; together these
  select one set of four offsets from the matching **TetrominoDefinition**.
- A **PieceQueue** draws from a succession of **SevenBag** value objects, each fully consumed before the
  next is created (DR-10).
- A **TetrominoDefinition** exists once per **TetrominoType** as immutable reference data, not per session.

### 5.3 Value Objects

#### CellPosition

| Attribute | Type    | Constraints |
| --------- | ------- | ----------- |
| column    | integer | 0–9         |
| row       | integer | 0–39        |

Used both for cells in the playfield and for the top-left corner of a piece's bounding box. A bounding
box corner may sit at a position whose box extends past the edges; only occupied cells are bound by
DR-03.

#### Cell

| Attribute | Type             | Constraints                            |
| --------- | ---------------- | -------------------------------------- |
| occupant  | TetrominoType?   | null when empty                        |

`isOccupied` is derived: `occupant != null`. The render colour is derived from `occupant` via DR-24.

#### ShapeOffset

A single cell of a tetromino, relative to its bounding box top-left.

| Attribute    | Type    | Constraints |
| ------------ | ------- | ----------- |
| columnOffset | integer | 0–3         |
| rowOffset    | integer | 0–3         |

#### TetrominoDefinition

Immutable reference data: one per `TetrominoType`. Not session state.

| Attribute        | Type              | Constraints                               | Description                                                     |
| ---------------- | ----------------- | ----------------------------------------- | ----------------------------------------------------------------- |
| type             | TetrominoType     | required, unique                          |                                                                   |
| boundingBoxSize  | integer           | 4 for I, 2 for O, 3 for T/S/Z/J/L         | Side length of the square rotation box.                           |
| colour           | colour            | required, unique across the seven (DR-24) |                                                                   |
| spawnOffsets     | ShapeOffset[4]    | exactly four, see table below             | Occupied cells in `Orientation.Spawn`.                            |
| spawnWidth       | integer           | derived: 4 for I, 2 for O, 3 for the rest | Width of the occupied cells in `Spawn`; feeds DR-06.              |

**Spawn-orientation shapes** (`.` empty, `X` occupied; offsets are `(columnOffset, rowOffset)`):

| Type | Box | Shape                    | spawnOffsets                   | spawnWidth |
| ---- | --- | ------------------------ | ------------------------------ | ---------- |
| I    | 4×4 | `....` / `XXXX` / `....` | (0,1) (1,1) (2,1) (3,1)        | 4          |
| O    | 2×2 | `XX` / `XX`              | (0,0) (1,0) (0,1) (1,1)        | 2          |
| T    | 3×3 | `.X.` / `XXX` / `...`    | (1,0) (0,1) (1,1) (2,1)        | 3          |
| S    | 3×3 | `.XX` / `XX.` / `...`    | (1,0) (2,0) (0,1) (1,1)        | 3          |
| Z    | 3×3 | `XX.` / `.XX` / `...`    | (0,0) (1,0) (1,1) (2,1)        | 3          |
| J    | 3×3 | `X..` / `XXX` / `...`    | (0,0) (0,1) (1,1) (2,1)        | 3          |
| L    | 3×3 | `..X` / `XXX` / `...`    | (2,0) (0,1) (1,1) (2,1)        | 3          |

J, L and T spawn flat-side-down, per the Guideline.

> **Handed to story 002.** The offset tables for `Orientation.Right`, `Inverted` and `Left`, together
> with the SRS wall-kick / offset tables that govern rotation against walls and the stack, are **owned by
> story 002** and are deliberately not defined here. 002 extends `TetrominoDefinition` with them; it must
> not redefine the spawn offsets above.

#### SevenBag

One shuffled bag in the 7-bag randomiser.

| Attribute | Type              | Constraints                                                |
| --------- | ----------------- | ---------------------------------------------------------- |
| remaining | TetrominoType[]   | ordered; on creation a permutation of all seven types       |

A bag is created full, drained from the front, and discarded when empty (DR-09, DR-10).

#### SpawnPlacement

The derived result of applying DR-06 and DR-07. Stated here as a table so every spec agrees on the
exact starting cells. Positions are the bounding box top-left; occupied cells are `(column, row)`.

| Type | Spawn position (col, row) | Occupied cells at spawn                   | After the DR-08 initial step (clear board) |
| ---- | ------------------------- | ----------------------------------------- | ------------------------------------------- |
| I    | (3, 18)                   | (3,19) (4,19) (5,19) (6,19)               | (3,20) (4,20) (5,20) (6,20)                 |
| O    | (4, 18)                   | (4,18) (5,18) (4,19) (5,19)               | (4,19) (5,19) (4,20) (5,20)                 |
| T    | (3, 18)                   | (4,18) (3,19) (4,19) (5,19)               | (4,19) (3,20) (4,20) (5,20)                 |
| S    | (3, 18)                   | (4,18) (5,18) (3,19) (4,19)               | (4,19) (5,19) (3,20) (4,20)                 |
| Z    | (3, 18)                   | (3,18) (4,18) (4,19) (5,19)               | (3,19) (4,19) (4,20) (5,20)                 |
| J    | (3, 18)                   | (3,18) (3,19) (4,19) (5,19)               | (3,19) (3,20) (4,20) (5,20)                 |
| L    | (3, 18)                   | (5,18) (3,19) (4,19) (5,19)               | (5,19) (3,20) (4,20) (5,20)                 |

### 5.4 Enumerations

#### TetrominoType

`I`, `O`, `T`, `S`, `Z`, `J`, `L` — the seven one-sided tetrominoes. Closed set; no other value is valid.

#### Orientation

The four SRS rotation states. All four are defined here so later specs share the vocabulary, even though
story 002 owns the transitions between them.

| Member     | Guideline symbol | Meaning                                              |
| ---------- | ---------------- | ---------------------------------------------------- |
| `Spawn`    | `0`              | The orientation a piece spawns in (shapes in §5.3).  |
| `Right`    | `R`              | 90° clockwise from `Spawn`.                          |
| `Inverted` | `2`              | 180° from `Spawn`.                                   |
| `Left`     | `L`              | 90° counter-clockwise from `Spawn`.                  |

The O tetromino's four orientations are visually identical. The enum still has all four members for O so
that rotation state is uniform across types.

#### GameStatus

| Member     | Meaning                                                                                      |
| ---------- | -------------------------------------------------------------------------------------------- |
| `Ready`    | Session constructed, playfield empty, waiting for the player's start action. No gravity runs. |
| `Playing`  | Pieces spawn, fall and lock.                                                                  |
| `GameOver` | Terminal. Reached via block out (DR-21) or lock out (DR-22). Story 008 owns what is shown.    |

### 5.5 Domain Rules and Invariants

Later specs should cite these by ID and name, e.g. "per **Spawn Placement (DR-06)**".

- **DR-01 — Playfield Geometry**: The playfield is always exactly 10 columns by 40 rows. Rows 20–39 are
  visible; rows 0–19 are buffer rows and are never rendered. These dimensions never change during a session.
- **DR-02 — Cell Occupancy Exclusivity**: A cell holds at most one block. Writing a block into an already
  occupied cell is never a legal operation.
- **DR-03 — Piece Containment**: Every occupied cell of the active piece must satisfy
  `0 ≤ column ≤ 9` and `0 ≤ row ≤ 39`. The bounding box itself may extend beyond these bounds.
- **DR-04 — No Overlap**: No occupied cell of the active piece may coincide with an occupied cell of the
  stack. DR-03 and DR-04 together define a *valid placement*; every spawn, descent, move (002), rotation
  (002) and drop (003) must result in a valid placement or be rejected.
- **DR-05 — Tetromino Cell Count**: Every tetromino occupies exactly four cells in every orientation.
- **DR-06 — Spawn Placement**: A piece spawns with its leftmost occupied column at
  `floor((10 - spawnWidth) / 2)` and its lowest occupied cell in row 19, the bottom-most buffer row,
  immediately above the top visible row. This yields columns 3–6 for I, 4–5 for O, and 3–5 for T, S, Z,
  J and L — the Guideline's centred, left-biased placement.
- **DR-07 — Spawn Orientation**: Every piece spawns in `Orientation.Spawn`, regardless of type and
  regardless of the orientation of the previous piece.
- **DR-08 — Initial Drop**: Immediately after spawning, and before any gravity time accrues, the piece
  attempts exactly one one-cell descent. It is applied if the result is a valid placement and skipped
  otherwise. This step is free: it does not consume the gravity interval.
- **DR-09 — Bag Completeness**: Every `SevenBag` is created as a permutation of all seven
  `TetrominoType` values, each appearing exactly once. The shuffle must be unbiased — all 5040
  permutations equally likely.
- **DR-10 — Bag Order Consumption**: Types are drawn strictly in bag order. A bag is fully consumed
  before a new one is generated. No type is drawn out of order, skipped, re-inserted or discarded.
- **DR-11 — Drought Bound**: As a consequence of DR-09 and DR-10, at most 12 other pieces may fall
  between two consecutive occurrences of the same type. This bound is a testable property of the
  sequence, not a separate mechanism.
- **DR-12 — Repeat Bound**: As a consequence of DR-09 and DR-10, a type never appears more than twice
  consecutively — and only ever across a bag boundary.
- **DR-13 — Queue Refill Threshold**: The `PieceQueue` always exposes at least seven upcoming types.
  When a draw would leave fewer than seven, a new bag is generated and appended. Refilling must not
  reorder or alter types already in the queue.
- **DR-14 — Gravity Step**: While a piece is `Playing` and not grounded, it descends exactly one cell per
  `gravityInterval`. At level 1 the interval is **1.000 second**. The Guideline curve
  `(0.8 - (level - 1) × 0.007) ^ (level - 1)` seconds per row yields exactly 1.000 s at level 1; the
  curve itself is **story 007's** concern.
- **DR-15 — Grounded**: A piece is *grounded* exactly when moving it down one cell would produce an
  invalid placement under DR-03 or DR-04. Groundedness is recomputed after every change to the piece or
  the stack, never cached across changes.
- **DR-16 — Lock Delay**: A grounded piece locks after **0.500 second** of continuous grounded time.
  Gravity does not act on a grounded piece.
- **DR-17 — Lock Reset Budget**: A piece may have its lock delay reset at most **15** times. The budget
  is replenished to 15 whenever the piece occupies a row lower than any it has previously occupied
  (i.e. `lowestRowReached` increases). Once exhausted, further reset requests are ignored and the running
  delay continues to expiry. **What triggers a reset — a successful move or rotation — is story 002's
  concern**; this rule fixes the budget and the replenishment condition.
- **DR-18 — Lock Timer Cancellation**: If a grounded piece ceases to be grounded, its lock timer is
  cancelled and gravity resumes. The reset budget is *not* refunded by this alone — only DR-17's
  new-lowest-row condition replenishes it.
- **DR-19 — Locking**: Locking writes the piece's four occupied cells into the playfield as occupied
  cells of its type and clears the active piece. Locking is atomic: all four cells are written or none.
  A locked block is permanent for the rest of the session unless removed by line clearing (**story 006**).
- **DR-20 — Spawn Immediacy**: The next piece spawns immediately after a lock resolves. There is no entry
  delay (ARE) and no animation gate. Story 006 inserts its line-clear resolution between the lock and the
  spawn; nothing else may.
- **DR-21 — Block Out**: If any cell required by a piece's spawn placement (DR-06) is already occupied,
  the piece is not placed and the session moves to `GameStatus.GameOver`.
- **DR-22 — Lock Out**: If a piece locks with all four of its cells in buffer rows (row ≤ 19), the
  session moves to `GameStatus.GameOver` after the lock is written.
- **DR-23 — Fresh Session State**: A new `GameSession` starts with every cell empty, `level` 1, `score` 0,
  `piecesSpawned` 0, no active piece, a `PieceQueue` filled from a fresh bag, and status `Ready`. No state
  carries over from any previous session.
- **DR-24 — Colour Mapping**: Each `TetrominoType` maps to exactly one colour and each colour to exactly
  one type. The Guideline palette is: **I cyan** `#00FFFF`, **O yellow** `#FFFF00`, **T purple** `#800080`,
  **S green** `#00FF00`, **Z red** `#FF0000`, **J blue** `#0000FF`, **L orange** `#FF7F00`. Story 013 may
  re-tune the exact hex values for the Info Support palette and for NFR-003, but must preserve the
  one-to-one mapping and each colour's recognisable hue.
- **DR-25 — Single Active Piece**: A session has at most one active piece. A spawn is only legal when
  there is no active piece.
- **DR-26 — Gravity Advances on Elapsed Time**: Fall and lock timers advance from measured elapsed
  wall-clock time, never from a frame or tick count, so behaviour is identical across refresh rates.
  A single update step consumes at most 100 ms of elapsed time (see EC-2) and never moves a piece past
  the point at which it becomes grounded.
- **DR-27 — Terminal Status**: Once a session is `GameOver`, no piece spawns, no gravity is applied and
  no lock occurs. The playfield is frozen for story 008 to present.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty)._

| ID      | Category        | Requirement                                                                                                                                                                                  |
| ------- | --------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance     | The playfield renders the falling piece and stack at a sustained **≥ 30 frames per second**, targeting 60, on a mid-range Info Support laptop in a current desktop browser.                     |
| NFR-002 | Performance     | A piece's descent deviates by **no more than ±50 ms per cell step** from the nominal `gravityInterval` under normal load, so the fall feels even.                                               |
| NFR-003 | Usability       | Each tetromino colour has a contrast ratio of **≥ 3:1** against the empty-cell background and is visually distinguishable from the other six at the rendered cell size.                         |
| NFR-004 | Reliability     | Over a continuous 10-minute session, accumulated gravity timing drift is **less than one cell**, i.e. the piece count and fall distance match elapsed time to within one gravity interval.      |
| NFR-005 | Reliability     | The game loop recovers from a stalled or throttled browser tab without a burst of catch-up descents (see EC-2) and without entering an invalid placement.                                       |
| NFR-006 | Testability     | The piece sequence is deterministic given a seed, and game time is injectable, so all §4 scenarios run headlessly without relying on real elapsed wall-clock time.                              |
| NFR-007 | Security/Privacy| Starting a game collects, transmits and persists **no personal data whatsoever**. There is no identity associated with a session; story 009 is the first point at which the player gives a name. |
| NFR-008 | Resource usage  | A session running continuously for 30 minutes shows no unbounded growth in retained objects — locked blocks are the only state that accumulates, and it is bounded by the 400-cell playfield.    |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                              | Expected Behavior                                                                                                                                             |
| ----- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | A piece's spawn cells are already occupied by the stack.                              | Block out (DR-21): the piece is not placed, the session moves to `GameOver`. Story 008 presents it.                                                            |
| EC-2  | The browser tab is backgrounded or a frame takes seconds; a huge time delta arrives.   | A single update consumes at most 100 ms of elapsed time (DR-26); the remainder is dropped. No multi-cell catch-up burst, no skipped collision check.            |
| EC-3  | A grounded piece becomes un-grounded (e.g. moved over a gap by story 002's input).     | Lock timer cancelled, gravity resumes (DR-18). The reset budget is *not* refunded unless the piece reaches a new lowest row (DR-17).                            |
| EC-4  | The 15-reset budget is exhausted while the piece is still grounded.                    | Further reset requests are ignored; the piece locks when the running 0.500 s delay expires (DR-17). The piece remains movable until it locks.                   |
| EC-5  | A piece reaches the floor row (row 39).                                                | It is grounded (DR-15) and locks after the delay like any other grounded piece. No special case.                                                                |
| EC-6  | A piece locks with all four cells still in buffer rows.                                | Lock out (DR-22): cells are written, then the session moves to `GameOver`.                                                                                      |
| EC-7  | A bag boundary places the same type at the end of one bag and the start of the next.    | Expected and correct (DR-12). Two identical pieces in a row is the designed worst case, not a randomiser bug — do not add a re-roll.                            |
| EC-8  | The player activates *New game* mid-session.                                           | The current session is discarded with no score recorded or submitted; a fresh session starts per DR-23. No confirmation prompt — games are short by design.     |
| EC-9  | The browser window is resized or zoomed during play.                                   | The playfield rescales proportionally; cell count, piece position, gravity and timers are unaffected.                                                           |
| EC-10 | The time source produces a zero or negative delta (clock adjustment, monotonic hiccup). | The delta is treated as zero. Timers never run backwards and the piece never moves up.                                                                          |
| EC-11 | An I piece spawns on an empty 10-wide board.                                           | It occupies columns 3–6, fitting with three empty columns to its left and three to its right. Its 4×4 bounding box extends below the occupied row — this is legal (DR-03). |
| EC-12 | The start or *New game* action is activated repeatedly in quick succession.            | Exactly one session is active at any time; each activation replaces the previous session wholesale. No orphaned game loop keeps running.                        |
| EC-13 | The page is left in `Ready` status without the player starting.                        | No gravity runs, no piece spawns, nothing is timed. The session waits indefinitely.                                                                             |
| EC-14 | The piece queue is drawn from faster than expected (future stories, e.g. hard drop).    | Refill to ≥ 7 upcoming types happens before the next read (DR-13); a draw never finds an empty queue.                                                            |
| EC-15 | A completed row forms while this spec is the only one implemented.                     | Nothing happens — the row stays. Line clearing is story 006. Play continues until block out or lock out.                                                         |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                             |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| SUC-001 | All 25 acceptance scenarios in §4 pass as automated tests in CI.                                                                                        |
| SUC-002 | A colleague can go from opening the page to watching a piece fall in **one action and under two seconds**, with no data entry.                          |
| SUC-003 | Over a seeded 1,000-piece sequence, every type appears 142–143 times, no type appears more than twice consecutively, and no gap exceeds 12 pieces.      |
| SUC-004 | A 10-minute unattended session runs to block out or lock out without an exception, a stuck piece, an overlapping placement, or a piece outside bounds.   |
| SUC-005 | At level 1, a piece dropped into an empty column takes 20 ± 0.5 seconds to travel from the top visible row to the floor row.                             |
| SUC-006 | All 27 domain rules in §5.5 are each covered by at least one automated test.                                                                            |
| SUC-007 | Specs 002–008 can be written entirely in the vocabulary of §5 without introducing a competing name for any concept defined here.                         |
| SUC-008 | Zero critical or high severity defects in the core loop after one week of informal office play.                                                         |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **`docs/product/story-map.md`** — the source of the story numbering, the primary user, and the
  project-wide exclusions quoted in §1.3.
- **The modern Tetris Guideline** — the normative ruleset for every detail not fixed by the story map.
- **No upstream feature dependencies.** 001 is the root of the Play Tetris activity and can be built
  against an empty repository.
- **Downstream dependents** (these cannot start until 001's domain model is agreed): 002 (input and
  movement — consumes DR-17's reset budget and DR-15's groundedness, and owns the `Right`/`Inverted`/`Left`
  offset tables and SRS kicks), 003 (hard drop), 004 (preview — reads `PieceQueue.upcoming`),
  005 (hold — adds a hold slot to `GameSession`), 006 (line clearing — removes locked cells and inserts
  itself between DR-19 and DR-20), 007 (levels — owns `gravityInterval`'s curve), 008 (game over — owns the
  `GameOver` experience triggered by DR-21 and DR-22).
- **Story 013 (branding)** may re-tune the DR-24 palette; it depends on DR-24's one-to-one constraint.

### 9.2 Constraints

- **Platform**: desktop browser on a laptop with a keyboard. No mobile or touch support (story map).
- **No accounts, no authentication, no server-side session identity** (story map). A session is anonymous
  and entirely ephemeral until story 009.
- **No pause and no ghost piece** (story map) — both were explicitly dropped by the product owner.
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed by this spec):
  ASP.NET Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`
  (Bootstrap/jQuery vendored, not npm-managed). No test project exists yet; SUC-001 requires one.
- **Fixed geometry**: 10 × 20 visible with a 20-row buffer is not configurable. Any "wide board" or
  "tall board" variant is a different product.
- **Determinism**: the randomiser must be seedable to satisfy NFR-006 and SUC-003.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*. Implementing 001 is the natural trigger for filling them in,
> per `CLAUDE.md` ("fill them in as architectural decisions for the Tetris implementation are made").

| Arc42 Section                                            | Relevance to This Feature                                                                                                                                       |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01. Introduction and Goals (`01-introduction-and-goals.md`) | Should record the story map goal — zero-friction, faithful Tetris on a coffee break — as the top quality driver that FR-001 and SUC-002 serve. *Currently empty.*  |
| 02. Architecture Constraints (`02-architecture-constraints.md`) | Should record the §9.2 constraints: desktop-only, no accounts, ASP.NET Core 10 Razor Pages. *Currently empty.*                                                   |
| 03. System Scope and Context (`03-system-scope-and-context.md`) | Defines the system boundary. For 001 the boundary is a single browser page with no external systems and no persistence. *Currently empty.*                       |
| 04. Solution Strategy (`04-solution-strategy.md`)        | Needs the decision on where the game loop and domain model live — browser-side versus server-side. This materially shapes 001 and is listed below as a constraint to settle. *Currently empty.* |
| 05. Building Block View (`05-building-block-view.md`)    | 001 introduces the core game-domain building block (playfield, tetrominoes, randomiser, game loop) and its separation from the Razor Pages presentation layer. *Currently empty.* |
| 06. Runtime View (`06-runtime-view.md`)                  | 001 defines the central runtime scenario: start → spawn → initial drop → gravity → ground → lock delay → lock → spawn, and the two exits to game over. *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`) | Needs the project-wide timing model (elapsed-time game loop, DR-26) and the randomiser seeding approach — both are cross-cutting, not 001-specific. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`) | 001 warrants ADRs for: the top-left zero-based coordinate system (§5.0), elapsed-time gravity (DR-26), 7-bag over pure random (DR-09), and the client/server split. *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)  | Should host project-wide performance and responsiveness scenarios; §6 here only adds what is specific to the core loop. *Currently empty.*                        |
| 12. Glossary (`12-glossary.md`)                          | Should be populated from §5 — playfield, buffer row, stack, tetromino, orientation, spawn, grounded, lock delay, 7-bag, block out, lock out. *Currently empty.*     |

## 10. Open Questions

| #   | Question                                                                                                                                                      | Owner      | Status   | Resolution                                                                                                                                                                                 |
| --- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Does the game loop and domain model run in the browser or on the server?                                                                                        | Daan Harms | Resolved | Out of scope for this spec — it is an **architecture** decision, not a behavioural one. None of §3–§5 changes either way. Recorded in §9.3 as the ADR arc42 §09 must capture before implementation. |
| 2   | Should the game auto-start on page load instead of requiring one action?                                                                                        | Daan Harms | Resolved | **No.** One explicit action (FR-001). Auto-start would drop pieces while the player is still reading story 014's controls panel, and would make the game run in a backgrounded tab. One action still meets "zero friction". |
| 3   | How many buffer rows — the Guideline's 20, or the 2–4 that implementations typically need?                                                                      | Daan Harms | Resolved | **20** (DR-01), the Guideline value. The extra rows cost nothing, keep the arithmetic in §5.0 exact, and leave headroom for story 003's hard drop and future piece sets.                        |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-005; US-002 -> SC-006..SC-014; US-003 -> SC-015, SC-016, SC-025;
        US-004 -> SC-017..SC-024
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-005 -> US-001; FR-006..FR-012 -> US-002; FR-013..FR-015, FR-023 -> US-003;
         FR-016..FR-022 -> US-004)
  - [x] Domain model covers all entities mentioned in the requirements
        GameSession, Playfield, ActivePiece, PieceQueue; value objects CellPosition, Cell, ShapeOffset,
        TetrominoDefinition, SevenBag, SpawnPlacement; enums TetrominoType, Orientation, GameStatus
  - [x] Domain rules and invariants are listed (DR-01 .. DR-27)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-15)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-008)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all three resolved)
-->
