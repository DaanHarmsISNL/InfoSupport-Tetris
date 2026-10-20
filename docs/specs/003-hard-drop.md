# Feature Specification: Hard Drop — Slam the Piece Straight Down

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

| Field           | Value                                                                                      |
| --------------- | ------------------------------------------------------------------------------------------ |
| Feature ID      | 003                                                                                          |
| Status          | Draft                                                                                        |
| Author          | Daan Harms                                                                                   |
| Created         | 2026-10-20                                                                                   |
| Last updated    | 2026-10-20                                                                                   |
| Epic / Parent   | Play Tetris (story map activity) — `docs/product/story-map.md`                               |
| Arc42 reference | Sections 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders.   |

> **Note on this spec.** 003 is a small, sharp slice on top of **spec 001** (`docs/specs/001-start-new-game.md`),
> which is the normative vocabulary for the whole *Play Tetris* activity. Every entity, enum member and
> domain rule `DR-01`–`DR-27` referenced below is defined there and is **not** redefined here. This spec
> adds exactly one player action and the rules it needs: `DR-44`–`DR-51`.
>
> **Ruleset.** Every detail not otherwise constrained by the story map is resolved against the modern
> **Tetris Guideline**, per the standing project decision recorded in 001.

### 1.1 Problem Statement

Once a player knows where a piece belongs, gravity (DR-14) makes them wait for it — at level 1 a piece
takes 20 seconds to fall from the top visible row to the floor, and even story 002's soft drop only
accelerates that descent. On a five-minute coffee break that waiting is the whole game's tempo problem:
the player has already made the decision and is just watching. Worse, every extra second in the air is a
second in which the player can fumble a piece they had already placed correctly in their head.

### 1.2 Goal

A player who has lined the piece up presses `Space` once and the piece is instantly where it belongs:
teleported straight down to the lowest position it could legally occupy and locked there in the same
instant, with the next piece already falling. The commitment is deliberate and irreversible — there is
no grace period after a hard drop.

### 1.3 Non-Goals

- **Ghost piece** — out of scope **project-wide** (story map: "explicitly dropped; the user wants the
  challenge"). This is called out here specifically because hard drop is where players most expect one:
  the landing position is computed by DR-44 but is **never rendered or hinted at**, before or during the
  drop. The player must judge the column by eye. Do not add a preview outline, a column highlight, a
  drop shadow, or a trail of faded cells as a substitute.
- **Points for the drop** — **story 006** owns scoring. This spec produces the travelled-cell count and
  hands it over (DR-48); it adds nothing to `score` itself.
- **Movement, rotation and soft drop** — **story 002**. Hard drop neither moves the piece sideways nor
  rotates it, and it does not change soft drop's semantics (soft drop leaves the DR-16 lock delay intact;
  hard drop does not).
- **Line clearing** — **story 006**. A hard drop that completes a row does not clear it here; 006 slots
  its resolution between DR-19 and DR-20 exactly as it does for a gravity lock.
- **Hold / swap** — **story 005**. Hard drop does not interact with a hold slot; 005 owns whatever
  per-turn hold flag it introduces and when it resets.
- **Game over presentation** — **story 008**. A hard drop can end the game by lock out (DR-22); what the
  player is then shown belongs to 008.
- **Key rebinding and the controls panel** — **story 014** documents that `Space` is hard drop. The
  binding itself is fixed here (DR-49) and is not configurable.
- **Input buffering** — a `Space` press that arrives while no piece is active is discarded, never queued
  for the next piece (DR-50). See Open Question 2.
- **Pause, accounts/authentication, mobile and touch support** — out of scope project-wide.

## 2. User Stories

### US-001: Place the piece now, not in twenty seconds

**As a** player who has already lined the piece up,
**I want** one key that slams it straight down and locks it,
**so that** the game moves at the speed of my decisions instead of the speed of gravity.

### US-002: Land exactly where I aimed

**As a** player,
**I want** the dropped piece to keep the column and rotation I gave it and to stop on the first thing
it meets,
**so that** a hard drop is a commitment I can make confidently, without a ghost piece to check it for me.

### US-003: Not lose my next piece to a held key

**As a** player who presses `Space` a fraction too long,
**I want** the hard drop to fire once per press and no more,
**so that** the piece that spawns next isn't thrown away before I have even seen it.

### US-004: Be rewarded for dropping fast

**As a** player,
**I want** the distance a hard drop travels to count toward my score,
**so that** playing decisively is worth more than letting gravity do the work.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) are defined in §5.5 of
this spec (DR-44–DR-51) and in §5.5 of spec 001 (DR-01–DR-27)._

| ID     | Requirement                                                                                                                                                                                                            | Priority | User Story |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall accept `Space` as the hard drop command, and no other key (DR-49).                                                                                                                                     | Must     | US-001     |
| FR-002 | The system shall translate the `ActivePiece` straight down to its landing position — the lowest placement that is still valid per **Piece Containment (DR-03)** and **No Overlap (DR-04)** — leaving `column` and `orientation` unchanged and never moving the piece upward (DR-44). | Must     | US-001     |
| FR-003 | The system shall stop the piece at the first obstruction in its path, testing every intermediate row, so that a piece never passes through the stack into a cavity beneath it (DR-44).                                   | Must     | US-002     |
| FR-004 | The system shall lock the hard-dropped piece **immediately** on arrival per **Locking (DR-19)**, bypassing the 0.500 s lock delay (DR-16) entirely: a running lock delay is discarded rather than waited out, and the 15-reset budget (DR-17) is neither consulted, consumed nor replenished (DR-45). | Must     | US-001     |
| FR-005 | The system shall resolve the travel and the lock as a single atomic action, with no gravity step, input or rendered frame interleaved, so the piece is never observable at the landing position as an unlocked piece (DR-46). | Must     | US-002     |
| FR-006 | The system shall hard drop a piece that is already *grounded* (DR-15) without moving it: it travels zero cells and locks immediately (DR-44, DR-45).                                                                     | Must     | US-001     |
| FR-007 | The system shall spawn the next piece immediately after the hard drop's lock resolves, per **Spawn Immediacy (DR-20)**, with story 006's line-clear resolution slotted in between and nothing else.                       | Must     | US-001     |
| FR-008 | The system shall apply **Lock Out (DR-22)** to a hard-dropped piece: if it locks with all four cells in buffer rows the session moves to `GameStatus.GameOver`. A hard drop can therefore end the game.                   | Must     | US-001     |
| FR-009 | The system shall fire a hard drop at most once per key press — on the up-to-down transition of `Space` only — ignoring operating-system key-repeat events while the key is held, and requiring a key release before another hard drop can fire (DR-47). | Must     | US-003     |
| FR-010 | The system shall keep the hard drop trigger disarmed across a lock, a spawn, a game over and the start of a new `GameSession`, so that a key held down through any of those boundaries cannot fire a second hard drop (DR-51). | Must     | US-003     |
| FR-011 | The system shall re-arm the hard drop trigger when the game surface loses keyboard focus, so that a key release missed while the window was unfocused cannot leave hard drop permanently disabled (DR-51).                | Should   | US-003     |
| FR-012 | The system shall record the number of cells travelled by each hard drop and publish it as a `HardDropOutcome` at the moment of lock, for story 006 to convert into points (2 per cell). This feature adds nothing to `score` (DR-48). | Must     | US-004     |
| FR-013 | The system shall ignore the hard drop command unless `GameStatus` is `Playing` (DR-27) and an `ActivePiece` exists (DR-25), discarding it rather than queueing it for a later piece (DR-50).                              | Must     | US-003     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash.
Coordinates follow spec 001 §5.0: `(column, row)`, origin top-left, row 39 is the floor._

### SC-001: A hard drop from the spawn area reaches the floor (FR-001, FR-002, FR-004)

```gherkin
Given a game session at level 1 with an empty playfield
  And the active T tetromino has taken its initial drop and occupies (4,19) (3,20) (4,20) (5,20)
When the player presses Space
Then the piece occupies (4,38) (3,39) (4,39) (5,39)
  And it locks in the same instant, without the 0.500 second lock delay elapsing
  And it is no longer the active piece
  And the four cells are occupied cells of type T
```

### SC-002: A hard drop lands on top of the stack (FR-002, FR-004)

```gherkin
Given a playfield whose only occupied cells are (4,30) and (5,30)
  And the active O tetromino occupies (4,19) (5,19) (4,20) (5,20)
When the player presses Space
Then the piece locks occupying (4,28) (5,28) (4,29) (5,29)
  And the cells (4,30) and (5,30) are unchanged
  And 9 cells of travel are reported
```

### SC-003: A hard drop changes nothing but the row (FR-002)

```gherkin
Given an active piece in some orientation over some set of columns
When the player presses Space
Then the piece's orientation is unchanged
  And the set of columns it occupies is unchanged
  And every one of its cells has a strictly greater or equal row than before
```

### SC-004: A hard drop does not pass through an overhang (FR-003)

```gherkin
Given a playfield whose only occupied cell is (5,30)
  And rows 31 to 39 are entirely empty
  And the active I tetromino occupies (3,20) (4,20) (5,20) (6,20)
When the player presses Space
Then the piece locks occupying (3,29) (4,29) (5,29) (6,29)
  And the empty cells beneath the overhang remain empty
```

### SC-005: A hard drop bypasses a running lock delay (FR-004)

```gherkin
Given a grounded piece whose 0.500 second lock delay has been running for 0.100 second
When the player presses Space
Then the piece locks immediately
  And the remaining 0.400 second of lock delay is not waited out
  And the player gets no further opportunity to move or rotate the piece
```

### SC-006: Hard-dropping a grounded piece travels zero cells (FR-006)

```gherkin
Given the active piece is grounded
When the player presses Space
Then the piece locks in the position it already occupied
  And it did not move
  And 0 cells of travel are reported
```

### SC-007: The lock reset budget is irrelevant to a hard drop (FR-004)

```gherkin
Given a grounded piece that has consumed 3 of its 15 lock delay resets
When the player presses Space
Then the piece locks immediately
  And no reset is consumed
  And no reset is replenished
  And the budget ceases to exist along with the piece
```

### SC-008: The next piece follows the hard drop immediately (FR-007)

```gherkin
Given a hard drop has just locked a piece into the stack
When the lock resolves
Then the next type is taken from the front of the piece queue
  And it spawns with no entry delay
  And it spawns in its normal spawn position, falling under gravity
  And it has not itself been dropped
```

### SC-009: A hard drop can end the game by lock out (FR-008)

```gherkin
Given the stack has grown into the buffer rows
  And the active piece is grounded with all four of its cells in buffer rows
When the player presses Space
Then the piece travels 0 cells and locks
  And its cells are written to the playfield
  And the session moves to status GameOver
```

### SC-010: Holding the key does not chain-drop the next piece (FR-009, FR-010)

```gherkin
Given the player presses Space and holds it down
  And the active piece hard drops and locks
  And the next piece spawns
When Space remains held for a further 2 seconds, with the operating system emitting key-repeat events
Then the next piece does not hard drop
  And it continues to fall under gravity at one cell per second
When the player releases Space and presses it again
Then the piece then active hard drops
```

### SC-011: One held key produces exactly one hard drop (FR-009, FR-010)

```gherkin
Given a game session in progress
When the player holds Space down continuously for 10 seconds, spanning at least five pieces
Then exactly one hard drop has occurred — on the piece that was active at the moment of the press
  And every other piece in that span locked by gravity and lock delay
```

### SC-012: Hard drop is ignored outside play (FR-013)

```gherkin
Given a session in status Ready, with no piece on the board
When the player presses Space
Then no game starts
  And nothing moves
  And the status is still Ready
Given a session in status GameOver
When the player presses Space
Then nothing moves, the playfield stays frozen, and the status is still GameOver
```

### SC-013: A press with no active piece is discarded, not buffered (FR-013)

```gherkin
Given a hard drop has just locked a piece
  And story 006's line-clear resolution is in progress, so there is no active piece
When the player releases Space and presses it again during that moment
Then no hard drop occurs
  And the press is not applied to the piece that spawns next
```

### SC-014: The travelled distance is handed to scoring (FR-012)

```gherkin
Given an active piece whose lowest occupied cell is in row 25
When the player presses Space and the piece lands with its lowest occupied cell in row 39
Then the hard drop reports 14 cells travelled
  And the score is unchanged by this feature
  And story 006 is the component that turns 14 cells into 28 points
```

### SC-015: Losing focus with the key down re-arms the trigger (FR-011)

```gherkin
Given the player has hard dropped and is still holding Space down
When the game surface loses keyboard focus and later regains it, with no key release ever observed
Then the hard drop trigger is armed again
  And the next Space press hard drops the active piece
```

### SC-016: A hard drop never produces an invalid placement (FR-002, FR-003)

```gherkin
Given any stack and any active piece
When the player presses Space
Then every occupied cell of the locked piece lies within the playfield
  And none of them coincides with a cell that was already occupied
  And no cell of the piece passed through an occupied cell on the way down
```

## 5. Domain Model

_Spec 001 §5 is the normative model. This section records **only what 003 adds**. `GameSession`,
`Playfield`, `ActivePiece`, `PieceQueue`, `CellPosition`, `Cell`, `TetrominoType`, `Orientation` and
`GameStatus` are used exactly as 001 defines them and are not restated._

### 5.1 Entities

#### HardDropTrigger

The edge detector for the hard drop key. It exists **once per input surface** (the page the player is
typing into), **not** once per `GameSession` — which is precisely what makes DR-51 expressible, and what
prevents a held key from following a piece into the next one or into a new game.

| Attribute | Type    | Constraints                     | Description                                                                                             |
| --------- | ------- | ------------------------------- | --------------------------------------------------------------------------------------------------------- |
| isArmed   | boolean | required, `true` initially      | `true` when a key-down on `Space` would fire a hard drop. Cleared by key-down, set only by key-up or blur. |
| binding   | key     | constant, `Space` (DR-49)       | Fixed. Not configurable; story 014 documents it.                                                          |

#### ActivePiece (extension)

No new stored attributes. One derived value is named here so later specs and tests can refer to it:

| Derived value   | Type         | Description                                                                                                                                    |
| --------------- | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| landingPosition | CellPosition | The `position` the piece would have after a hard drop, per DR-44. Recomputed on demand; never stored, and — per §1.3 — **never rendered**. |

### 5.2 Relationships

- A **HardDropTrigger** relates to the input surface, not to a **GameSession**. It outlives any one
  session and is unaffected by session lifecycle transitions (DR-51).
- A hard drop acts on the **GameSession**'s single **ActivePiece** (DR-25) and, through **Locking (DR-19)**,
  writes into the **Playfield**.
- Each accepted hard drop produces exactly one **HardDropOutcome**. A rejected command (DR-50) produces none.

### 5.3 Value Objects

#### HardDropOutcome

Published at the moment the hard-dropped piece locks. Consumed by **story 006** for scoring; nothing in
003 reads it back.

| Attribute       | Type         | Constraints                        | Description                                                            |
| --------------- | ------------ | ---------------------------------- | ------------------------------------------------------------------------ |
| cellsTravelled  | integer      | 0–39                               | The `d` of DR-44. Zero for an already-grounded piece.                    |
| landingPosition | CellPosition | required                           | Where the piece locked. Same convention as `ActivePiece.position`.       |
| type            | TetrominoType| required                           | Which piece was dropped. Carried for 006's and 008's reporting.          |

### 5.4 Enumerations

**None new.** `GameStatus`, `TetrominoType` and `Orientation` are used unchanged from spec 001 §5.4.
In particular, hard drop does not introduce a rotation state and does not alter `Orientation`.

### 5.5 Domain Rules and Invariants

_Continuing 001's numbering. 003 owns `DR-44`–`DR-51`; cite them by ID and name as elsewhere, e.g.
"per **Hard Drop Immediate Lock (DR-45)**"._

- **DR-44 — Hard Drop Landing**: The *landing position* is the active piece's `position` translated down
  by `d` cells, where `d` is the largest integer ≥ 0 for which **every** translation by `0, 1, … d` is a
  valid placement under **Piece Containment (DR-03)** and **No Overlap (DR-04)**. `column` and
  `orientation` are unchanged, and `d` is never negative — a hard drop never moves a piece up or sideways.
  Because every intermediate row is tested, a piece can never tunnel through the stack into a cavity
  beneath it. For a piece that is already *grounded* (DR-15), `d` is 0.
- **DR-45 — Hard Drop Immediate Lock**: A hard drop locks the piece at its landing position per
  **Locking (DR-19)** in the same update step in which the command is accepted. The **Lock Delay (DR-16)**
  is not started; a lock delay already running is discarded rather than waited out; and the
  **Lock Reset Budget (DR-17)** is neither consulted, consumed nor replenished. This is the defining
  difference between hard drop and both gravity and soft drop: the 0.500 s grace period the player relies
  on for a last adjustment is deliberately forfeited, which is exactly what makes the key feel decisive.
- **DR-46 — Hard Drop Atomicity**: Travel and lock are one atomic action. Between accepting the command
  and completing the lock, no gravity step is applied, no further input is processed and no frame is
  rendered — the piece is never observable at the landing position as an unlocked active piece. Either
  the piece travels and locks, or (when the command is rejected under DR-50) nothing happens at all.
- **DR-47 — Hard Drop Edge Trigger**: A hard drop fires on the transition of `Space` from up to down, and
  at most once per such transition. Key-down events produced by operating-system autorepeat while the key
  is already held are ignored, whatever the repeat rate. A key release is required before another hard
  drop can fire. There is no DAS/ARR behaviour for hard drop — that mechanism belongs to story 002's
  horizontal movement and must not be applied here.
- **DR-48 — Hard Drop Distance Handoff**: Every accepted hard drop records `cellsTravelled` — the `d` of
  DR-44 — and publishes it as part of a `HardDropOutcome` at the moment of lock. **Story 006 owns the
  award** of 2 points per cell travelled. This spec adds nothing to `score`, including when `d` is 0.
- **DR-49 — Hard Drop Binding**: Hard drop is bound to `Space` and to no other key, and no other action
  binds `Space`. The binding is fixed; rebinding is out of scope and story 014 documents the key.
- **DR-50 — Hard Drop Input Gating**: A hard drop command is accepted only when `GameStatus` is `Playing`
  (**Terminal Status, DR-27**) and the session has an `ActivePiece` (**Single Active Piece, DR-25**). In
  every other state — `Ready`, `GameOver`, and the instant between a lock and the next spawn while story
  006 resolves line clears — the command is **discarded**, never queued or buffered for a later piece. A
  discarded command still clears the trigger's armed state per DR-47, so a key held from a rejected press
  cannot fire once play resumes.
- **DR-51 — Hard Drop Arming Scope**: `HardDropTrigger.isArmed` tracks the **physical key**, not the
  piece, the session or the game state. It is cleared by a key-down and set only by a key-up — or by the
  game surface losing keyboard focus, which is treated as a key-up so that a release missed while
  unfocused cannot disable hard drop permanently. It is **not** re-armed by a piece locking, a piece
  spawning, the session reaching `GameOver`, or a new `GameSession` starting (DR-23). A key held down
  across any of those boundaries therefore cannot fire a second hard drop.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty);
001's NFR-001–NFR-008 continue to apply to the loop this feature sits in._

| ID      | Category        | Requirement                                                                                                                                                                                       |
| ------- | --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Responsiveness  | A hard drop resolves within the update step that accepts the key event, and the locked result is on screen within **one rendered frame** of the key press — ≤ 33 ms at the 30 fps floor of 001 NFR-001, targeting ≤ 16 ms. |
| NFR-002 | Input integrity | `Space` held continuously for **10 seconds** across at least five pieces produces **exactly one** hard drop, independent of the operating system's autorepeat delay and rate (typically 25–33 Hz).   |
| NFR-003 | Determinism     | The landing position is a pure function of the active piece's `type`, `orientation` and `column` together with the stack. Identical inputs give identical landings regardless of frame rate, elapsed time, or how long the piece has been falling. |
| NFR-004 | Performance     | Resolving a hard drop costs at most one playfield-height of placement checks (≤ 40) and adds **< 1 ms** to the frame in which it occurs, so a burst of rapid drops never causes a visible stall.     |
| NFR-005 | Testability     | A hard drop is invokable headlessly — without synthesising key events and without advancing game time — so all §4 scenarios run deterministically. Extends 001 NFR-006.                             |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                       | Expected Behavior                                                                                                                                                              |
| ----- | ------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | `Space` is held down through the lock and into the next piece. *(The classic bug in this feature.)* | The next piece does **not** drop (DR-47, DR-51). Only a key release followed by a fresh press fires again. This case is non-negotiable and must have a dedicated regression test. |
| EC-2  | The operating system emits key-repeat key-downs while `Space` is held.                           | All but the first are ignored (DR-47). The repeat rate is irrelevant.                                                                                                             |
| EC-3  | The active piece is already grounded when `Space` is pressed.                                    | Legal. It travels 0 cells and locks immediately (DR-44, DR-45). `cellsTravelled` of 0 is handed to 006, which awards 0 points.                                                     |
| EC-4  | `Space` is pressed while a 0.500 s lock delay is already running.                                | The delay is discarded, not waited out; the piece locks at once (DR-45). No reset is consumed or replenished.                                                                     |
| EC-5  | The lock delay expires in the same update step as the `Space` press.                             | The piece locks exactly once, in the same position either way. If the delay resolved first there is no active piece, so the press is discarded (DR-50) and — being already disarmed — cannot affect the next piece. |
| EC-6  | A hard drop lands with all four cells still in buffer rows.                                      | Lock out (DR-22): the cells are written, then the session moves to `GameOver`. A hard drop can end the game instantly.                                                             |
| EC-7  | The next spawn after a hard drop finds its spawn cells occupied.                                 | Block out (DR-21): the piece is not placed and the session moves to `GameOver`. Unchanged from 001; hard drop just makes it arrive sooner.                                         |
| EC-8  | A hard drop completes one or more rows.                                                          | 003 clears nothing. Story 006 resolves the clear between **Locking (DR-19)** and **Spawn Immediacy (DR-20)**, exactly as for a gravity lock.                                       |
| EC-9  | `Space` is pressed while the status is `Ready` or `GameOver`.                                    | Discarded (DR-50). It must not start a game, restart one, or act as a confirmation. The trigger still disarms, so a key held from such a press cannot fire when play resumes.      |
| EC-10 | `Space` is pressed while keyboard focus is inside a text field (story 009's name entry).         | No hard drop — the status is `GameOver` there, so DR-50 covers it. The game must not swallow the keystroke: the space character still reaches the field.                           |
| EC-11 | Focus is lost with `Space` held, so the key-up is never observed.                                | Blur is treated as a key-up and re-arms the trigger (DR-51). Hard drop is never permanently disabled by a missed release.                                                         |
| EC-12 | The piece sits above a cavity with an overhang between.                                          | It lands on the highest obstruction in its path and does not tunnel into the cavity (DR-44). The cavity stays empty — that is the hole the player has to live with.                |
| EC-13 | `Space` is pressed on the very first piece, right after its initial drop (DR-08).                | A normal hard drop. Travel is measured from the piece's position *after* the DR-08 initial step, not from its spawn position.                                                     |
| EC-14 | Genuine rapid play: press–release–press across two consecutive pieces within a few hundred ms.   | Two hard drops, one per piece. There is no rate limit and no cooldown — only the release requirement of DR-47.                                                                     |
| EC-15 | A new game is started (FR-005 of 001) while `Space` is still held from the previous session.     | The trigger stays disarmed across the session boundary (DR-51). The first piece of the new game is not dropped.                                                                   |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                             |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 16 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                        |
| SUC-002 | Over 1,000 randomly generated (stack, piece, orientation, column) cases, the hard drop landing is identical to the position reached by repeated single-cell descents, and `cellsTravelled` equals the row delta in every case. |
| SUC-003 | A 10-second continuous `Space` hold spanning at least five pieces yields exactly one hard-drop lock; the remaining pieces lock via gravity and lock delay.                |
| SUC-004 | On an empty board at level 1, a hard drop from the top visible row locks the piece in under 50 ms, against the ~20 s the same descent takes under gravity alone.          |
| SUC-005 | Each of DR-44 through DR-51 is covered by at least one automated test, with EC-1 covered by a named regression test.                                                     |
| SUC-006 | A 10-minute session using hard drop as the primary placement method reaches block out or lock out with no invalid placement, no piece lost to an unintended drop, and no missed spawn. |
| SUC-007 | After one week of informal office play, zero reports of "it dropped my next piece too" and zero reports of the key becoming unresponsive.                                 |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 001 (`docs/specs/001-start-new-game.md`)** — hard, blocking. 003 is written entirely in 001's
  vocabulary and depends on `ActivePiece`, `Playfield`, placement validity (DR-03, DR-04), groundedness
  (DR-15), the lock delay it bypasses (DR-16, DR-17), locking (DR-19), spawn immediacy (DR-20), lock out
  (DR-22) and terminal status (DR-27).
- **Story 002 (movement, rotation, soft drop)** — co-dependent on the keyboard input surface rather than
  blocking. 002 owns the general input handling and the remaining key bindings; `Space` is reserved for
  003 by DR-49 and 002 must not bind it. Whichever of the two lands first establishes the input surface;
  the other extends it. 003 can be implemented and tested against 001 alone, by invoking the hard drop
  command directly (NFR-005).
- **Story 006 (line clearing and scoring)** — downstream. It consumes `HardDropOutcome.cellsTravelled`
  (DR-48) at 2 points per cell, and inserts its clear resolution between DR-19 and DR-20.
- **Story 008 (game over)** — downstream. It presents the `GameOver` that a hard-drop lock out (EC-6) or
  the following block out (EC-7) can trigger.
- **Story 014 (controls panel)** — downstream. It documents `Space` = hard drop.
- **No dependency on stories 004, 005 or 007.** Preview, hold and the level curve neither constrain nor
  are constrained by this feature.

### 9.2 Constraints

- **No ghost piece** (story map, project-wide). The landing position is computed and immediately used;
  it is never shown. This materially raises the difficulty of hard drop and is the intended design.
- **Binding is fixed to `Space`** (DR-49), not configurable.
- **Platform**: desktop browser on a laptop with a keyboard. No mobile or touch support, so there is no
  gesture equivalent of hard drop to design.
- **No input buffering** anywhere in this feature (DR-50). See Open Question 2.
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed here): ASP.NET
  Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`. Where
  the key event is observed and where the drop is resolved follows from the client/server split that
  001 §9.3 flags as an outstanding ADR; this spec's behaviour is identical either way.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below state what *this feature needs
> those sections to say* once 001's implementation triggers filling them in.

| Arc42 Section                                               | Relevance to This Feature                                                                                                                                            |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 05. Building Block View (`05-building-block-view.md`)       | Hard drop belongs to the core game-domain block introduced by 001, with an input-handling block (shared with story 002) that owns the `HardDropTrigger` edge detector. *Currently empty.* |
| 06. Runtime View (`06-runtime-view.md`)                     | Adds a runtime scenario that short-circuits 001's loop: key press → landing search → travel → immediate lock → (006 clear) → spawn, with no lock-delay state in between. *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`) | Needs the project-wide **keyboard input model**: edge-triggered versus level-triggered (repeating) actions, autorepeat suppression, and focus/blur handling. Hard drop is the canonical edge-triggered action; story 002's horizontal movement is the canonical repeating one. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`) | Warrants an ADR on edge-triggered input with no command buffering (DR-47, DR-50, DR-51) — it is a project-wide input policy, not a hard-drop detail. *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)     | Should host the input-latency quality scenario that NFR-001 instantiates; §6 here only adds what is specific to hard drop. *Currently empty.*                           |
| 12. Glossary (`12-glossary.md`)                             | Add: hard drop, landing position, cells travelled, edge trigger / armed, autorepeat. *Currently empty.*                                                                 |

## 10. Open Questions

| #   | Question                                                                                                   | Owner      | Status   | Resolution                                                                                                                                                                                                                              |
| --- | ------------------------------------------------------------------------------------------------------------ | ---------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Should the landing position be shown before the drop — i.e. a ghost piece?                                   | Daan Harms | Resolved | **No.** Project-wide exclusion from the story map ("the user wants the challenge"). Hard drop is where players most expect one, so §1.3 names it explicitly and forbids the near-equivalents (outline, column highlight, trail).          |
| 2   | Should a `Space` press that arrives with no active piece be buffered and applied to the piece that spawns?   | Daan Harms | Resolved | **No** (DR-50). Buffering would produce exactly the chain-drop players fear (EC-1), and the window is effectively zero except during story 006's clear resolution — where buffering is precisely the wrong behaviour. Drop the press.     |
| 3   | Should a hard drop grant a short post-landing window in which the piece can still be nudged before locking?  | Daan Harms | Resolved | **No** (DR-45). Immediate locking *is* the feature: the player trades the 0.500 s lock delay for speed and certainty. A post-drop slide window would make hard drop a fast soft drop and reintroduce the fumble it exists to eliminate.   |
| 4   | Should `Space` be rebindable, or should a second key (e.g. `Enter`) also hard drop?                          | Daan Harms | Resolved | **No** to both (DR-49). `Space` is the Guideline binding and the one every player already knows. A single fixed binding keeps story 014's controls panel honest and avoids a settings surface the product does not otherwise have.        |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001, SC-002, SC-005..SC-009; US-002 -> SC-003, SC-004, SC-016;
        US-003 -> SC-010..SC-013, SC-015; US-004 -> SC-014
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-002, FR-004, FR-006..FR-008 -> US-001; FR-003, FR-005 -> US-002;
         FR-009..FR-011, FR-013 -> US-003; FR-012 -> US-004)
  - [x] Domain model covers all entities mentioned in the requirements
        New: HardDropTrigger (entity), HardDropOutcome (value object), ActivePiece.landingPosition
        (derived). All other entities reused verbatim from spec 001 §5 and not redefined here.
  - [x] Domain rules and invariants are listed (DR-44 .. DR-51; DR-01..DR-27 inherited from spec 001)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-15)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-005)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all four resolved)
-->
