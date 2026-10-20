# Feature Specification: Move, Rotate and Soft-Drop the Falling Piece

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
| Feature ID      | 002                                                                                           |
| Status          | Draft                                                                                         |
| Author          | Daan Harms                                                                                    |
| Created         | 2026-10-20                                                                                    |
| Last updated    | 2026-10-20                                                                                    |
| Epic / Parent   | Play Tetris (story map activity) — `docs/product/story-map.md`                                |
| Arc42 reference | Sections 3, 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders. |

> **Note on this spec.** 002 is written entirely in the vocabulary of **spec 001 §5**, which is normative
> for the whole *Play Tetris* activity. Entity names, enum members and domain rule IDs from 001 are reused
> verbatim and never redefined here. New rules introduced by this spec continue 001's numbering from
> **DR-28**.
>
> **What 001 handed to this spec.** 001 §5.3 explicitly defers two things to 002, and this spec delivers
> both: (a) the `ShapeOffset` tables for `Orientation.Right`, `Inverted` and `Left` for all seven
> `TetrominoType` values — see **§5.3 `OrientationShape`**; and (b) the SRS wall-kick tables — see
> **§5.3 `KickTable`**. 001's `spawnOffsets` are *not* redefined; they remain the authority for
> `Orientation.Spawn`. 001 also left open *what triggers* a lock delay reset under
> **Lock Reset Budget (DR-17)** — **DR-37** below fixes that trigger.
>
> **Ruleset.** Every detail not otherwise constrained by the story map is resolved against the modern
> **Tetris Guideline**, per the standing project decision recorded in 001.

### 1.1 Problem Statement

After story 001 a piece spawns, falls and locks entirely on its own — the player can only watch. A game
in which the stack builds itself is a screensaver, not Tetris. The player needs direct, low-latency
control over the falling piece: slide it to the column they want, turn it to the shape that fits, and
hurry it down when they have already decided. Without this, every later story in the map — hard drop,
preview, hold, line clears, scoring, levels — has nothing to reward.

### 1.2 Goal

While a `GameSession` is `Playing`, the player steers the `ActivePiece` with the keyboard: `←`/`→` slide
it one cell at a time (with Guideline DAS/ARR auto-repeat when held), `↑`/`X` and `Z`/`Ctrl` rotate it
clockwise and counter-clockwise using the Super Rotation System — including wall kicks, floor kicks and
the I tetromino's distinct kick table — and holding `↓` soft-drops it at twenty times normal gravity.
Every action is validated against **No Overlap (DR-04)** and **Piece Containment (DR-03)** and is simply
rejected if it would be illegal. A successful move or rotation of a grounded piece buys the player
another go at the 0.500 s lock delay, bounded by the 15-reset budget of **Lock Reset Budget (DR-17)**.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec.

- **Hard drop (`Space`)** — **story 003**. This spec binds no key to it and defines no instant-drop
  behaviour. 003 will add it to the `KeyBinding` set defined in §5.3.
- **Hold / swap (`C` and `Shift`)** — **story 005**. Those keys are reserved here and bound to nothing.
- **Scoring soft-drop cells** — **story 006**. This spec *counts* the cells a piece descends under soft
  drop and exposes that count as `ActivePiece.softDropCells`; it awards no points. 006 converts the
  count to points (1 point per cell) when the piece locks.
- **180° rotation** — not part of the Tetris Guideline. There is no key for it, no `Spawn ↔ Inverted`
  single-step transition, and no kick table for one. Reaching `Inverted` always takes two rotations.
- **Rebindable keys and a controls panel** — the bindings in **DR-29** are fixed. Showing the player what
  the keys are is **story 014**.
- **Gamepad, touch, swipe or on-screen buttons** — keyboard only (story map: laptop + keyboard).
- **Changing gravity, lock delay duration or the reset budget** — 001 owns **Gravity Step (DR-14)**,
  **Lock Delay (DR-16)** and **Lock Reset Budget (DR-17)**. This spec only supplies the reset *trigger*
  and a faster descent rate while `↓` is held. Story 007 owns the level curve.
- **Spawn offsets and spawn placement** — fixed by **Spawn Placement (DR-06)** and **Spawn Orientation
  (DR-07)**. This spec adds the other three orientations, not the first one.
- **Ghost piece** — out of scope project-wide (story map: "the user wants the challenge"). A rotation or
  move is previewed by nothing; the player judges it from the piece itself.
- **Pause** — out of scope project-wide. No key suspends play.
- **Accounts, registration, login** — out of scope project-wide. No input is attributed to an identity.
- **Mobile and touch support** — out of scope project-wide.
- **Input during `Ready` or `GameOver`** — ignored per **Terminal Status (DR-27)** and **DR-28** below.
  Starting and restarting a game is 001's FR-001/FR-005; presenting game over is story 008.

## 2. User Stories

### US-001: Place the piece where I want it

**As a** player,
**I want** to slide the falling piece one column left or right with the arrow keys,
**so that** I can land it in the gap I am aiming for instead of wherever it happened to spawn.

### US-002: Cross the board without hammering the key

**As a** player,
**I want** to hold an arrow key and have the piece keep sliding at a steady rate after a short delay,
**so that** moving from one edge to the other is one smooth motion rather than nine separate presses.

### US-003: Turn the piece to the shape I need

**As a** player,
**I want** to rotate the piece clockwise or counter-clockwise, one quarter turn per key press,
**so that** I can match it to the hole I am filling.

### US-004: Rotate even when I am pressed against something

**As a** player,
**I want** a rotation that does not quite fit to be nudged into place rather than silently refused,
**so that** I can tuck pieces against the wall, into overhangs and down wells the way real Tetris lets me.

### US-005: Hurry a piece down

**As a** player,
**I want** to hold the down arrow to make the current piece fall much faster,
**so that** I do not wait a full second per row once I have already decided where the piece goes.

### US-006: Make a final adjustment after landing

**As a** player,
**I want** a brief, bounded window to keep moving and rotating a piece that has already touched down,
**so that** I can slide it into a tuck at the last moment — without being able to stall forever.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) are defined in §5.5 of
this spec (DR-28 onward) or in spec 001 §5.5 (DR-01 … DR-27)._

| ID     | Requirement                                                                                                                                                                                                                             | Priority | User Story |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall accept keyboard input only while `GameSession.status` is `GameStatus.Playing` and an `ActivePiece` exists; in `Ready` and `GameOver` every binding in DR-29 is inert (DR-28, DR-27).                                      | Must     | US-001     |
| FR-002 | The system shall bind exactly these keys and no others: `←` move left, `→` move right, `↓` soft drop, `↑` and `X` rotate clockwise, `Z` and `Ctrl` rotate counter-clockwise (DR-29).                                                       | Must     | US-001     |
| FR-003 | The system shall move the `ActivePiece` exactly one column in the pressed direction when `←` or `→` is pressed, provided the result is a valid placement (DR-03, DR-04, DR-30).                                                            | Must     | US-001     |
| FR-004 | The system shall reject a horizontal move whose result would be an invalid placement, leaving the piece's `position` and `orientation` untouched and changing no timer (DR-30, DR-38).                                                     | Must     | US-001     |
| FR-005 | The system shall leave the `ActivePiece.orientation` unchanged on a horizontal move, and shall not move it vertically (DR-30).                                                                                                             | Must     | US-001     |
| FR-006 | The system shall, while `←` or `→` is held, perform the first move immediately, then wait a DAS delay of **167 ms**, then repeat the move every **33 ms** (ARR) until the key is released or the move is rejected (DR-31).                 | Must     | US-002     |
| FR-007 | The system shall treat the most recently pressed of `←` and `→` as the active auto-repeat direction when both are held, and shall restart the DAS delay on that switch (DR-32).                                                           | Must     | US-002     |
| FR-008 | The system shall resume auto-repeat for the still-held opposite key, after a fresh DAS delay, when the more recent of two held horizontal keys is released (DR-32).                                                                        | Should   | US-002     |
| FR-009 | The system shall cancel all auto-repeat state for a direction as soon as its key is released, and shall not emit a queued repeat after release (DR-31).                                                                                   | Must     | US-002     |
| FR-010 | The system shall rotate the `ActivePiece` one quarter turn clockwise on `↑` or `X` and one quarter turn counter-clockwise on `Z` or `Ctrl`, advancing `orientation` through the cycle in DR-33.                                            | Must     | US-003     |
| FR-011 | The system shall perform at most one rotation per key press; holding a rotation key shall not auto-repeat (DR-34).                                                                                                                         | Must     | US-003     |
| FR-012 | The system shall draw the four occupied cells of a rotated piece from the `OrientationShape` offsets for its type and target orientation (§5.3), keeping exactly four cells in every orientation (DR-05).                                  | Must     | US-003     |
| FR-013 | The system shall treat rotation of an O tetromino as a no-op in position: `orientation` advances through the cycle, the four occupied cells do not move, and no kick is attempted (DR-36).                                                 | Must     | US-003     |
| FR-014 | The system shall offer no 180° rotation: there is no binding for it and no single-step `Spawn ↔ Inverted` transition (DR-33).                                                                                                              | Must     | US-003     |
| FR-015 | The system shall, for each rotation, try the five `KickOffset` entries of the matching `KickTable` row in order — test 1 first, test 5 last — and apply the first offset that yields a valid placement (DR-35).                            | Must     | US-004     |
| FR-016 | The system shall reject the rotation outright, leaving `orientation` and `position` unchanged, only when all five tests of the row fail (DR-35, DR-38).                                                                                    | Must     | US-004     |
| FR-017 | The system shall use the dedicated I `KickTable` for the I tetromino and the shared JLSTZ `KickTable` for J, L, S, T and Z (DR-35, §5.3).                                                                                                  | Must     | US-004     |
| FR-018 | The system shall apply kick offsets on both axes, so a rotation may displace the piece horizontally (wall kick) and/or vertically (floor kick), including upward (DR-35).                                                                  | Must     | US-004     |
| FR-019 | The system shall, while `↓` is held, advance the fall timer at **20×** the current `gravityInterval`, i.e. one cell per `gravityInterval / 20`, and shall restore normal gravity the moment `↓` is released (DR-39).                       | Must     | US-005     |
| FR-020 | The system shall never move the `ActivePiece` more than one cell down per update step, regardless of soft drop or elapsed time (DR-39, DR-26).                                                                                             | Must     | US-005     |
| FR-021 | The system shall stop soft drop at the point the piece becomes grounded and shall grant it the full lock delay of **0.500 s** (DR-16); soft drop never locks a piece early (DR-40).                                                        | Must     | US-005     |
| FR-022 | The system shall increment `ActivePiece.softDropCells` by one for every cell the piece descends while `↓` is held, and shall hand that count to story 006 at lock time without awarding any points here (DR-41).                           | Must     | US-005     |
| FR-023 | The system shall not count cells descended under normal gravity, under a floor kick, or under story 003's hard drop toward `softDropCells` (DR-41).                                                                                        | Must     | US-005     |
| FR-024 | The system shall clear soft-drop state on lock, so a piece spawning while `↓` is still held starts with `softDropCells` of 0 and soft-drops from its first update step (DR-41).                                                            | Should   | US-005     |
| FR-025 | The system shall reset the lock delay of a *grounded* piece on every **successful** horizontal move or rotation, consuming one unit of the 15-reset budget (DR-37, DR-17).                                                                 | Must     | US-006     |
| FR-026 | The system shall reset nothing — no lock timer, no budget unit — on a **rejected** move or rotation, or on any input while the piece is not grounded (DR-37, DR-38).                                                                       | Must     | US-006     |
| FR-027 | The system shall cancel the lock timer and resume gravity when a successful move or rotation leaves the piece un-grounded, without refunding the budget (DR-18, DR-37).                                                                    | Must     | US-006     |
| FR-028 | The system shall continue to accept input from a grounded piece whose reset budget is exhausted — the piece still moves and rotates — while the running 0.500 s delay proceeds to expiry (DR-17, DR-37).                                   | Must     | US-006     |
| FR-029 | The system shall replenish the reset budget to 15 when a move or rotation places the piece in a row lower than any it has occupied, updating `lowestRowReached` (DR-17, DR-37).                                                            | Must     | US-006     |
| FR-030 | The system shall suppress the browser's default action for every bound key — notably `↑`, `↓`, `←`, `→` scrolling the page — while a session is `Playing` (DR-42).                                                                         | Must     | US-001     |
| FR-031 | The system shall apply queued inputs in the order they were received, and shall resolve all input for an update step before applying that step's gravity (DR-43).                                                                          | Must     | US-003     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash.
Cell coordinates are `(column, row)` under spec 001 §5.0: row increases downward, row 39 is the floor._

### SC-001: Input is ignored before the game starts (FR-001)

```gherkin
Given a game session in status Ready
When the player presses the left arrow, the up arrow and the down arrow in turn
Then nothing moves on the playfield
  And no piece spawns
  And the session is still in status Ready
```

### SC-002: Input is ignored after game over (FR-001)

```gherkin
Given a game session that has reached status GameOver
When the player presses any of the bound keys
Then the playfield is unchanged
  And the session remains in status GameOver
```

### SC-003: A left press moves the piece one column (FR-003, FR-005)

```gherkin
Given a T tetromino is falling in the Spawn orientation with its cells at (4,22) (3,23) (4,23) (5,23)
When the player presses the left arrow once
Then its cells are (3,22) (2,23) (3,23) (4,23)
  And its orientation is still Spawn
  And its row has not changed
```

### SC-004: A right press moves the piece one column (FR-003)

```gherkin
Given an O tetromino is falling with its cells at (4,25) (5,25) (4,26) (5,26)
When the player presses the right arrow once
Then its cells are (5,25) (6,25) (5,26) (6,26)
```

### SC-005: A move into the wall is refused (FR-004)

```gherkin
Given a J tetromino is falling with its leftmost occupied cell in column 0
When the player presses the left arrow
Then the piece does not move
  And its orientation is unchanged
  And no timer is reset
```

### SC-006: A move into the stack is refused (FR-004)

```gherkin
Given a falling piece with an occupied stack cell directly to its right at the same row
When the player presses the right arrow
Then the piece does not move
  And the stack is unchanged
```

### SC-007: Holding a direction auto-repeats after DAS (FR-006)

```gherkin
Given an I tetromino is falling in open space
When the player presses and holds the left arrow
Then the piece moves one column left immediately
When 167 ms of game time elapses with the key still held
Then the piece moves a second column left
When a further 33 ms elapses
Then the piece moves a third column left
  And it continues at one column per 33 ms while the key is held
```

### SC-008: A tap moves exactly one cell (FR-006)

```gherkin
Given a falling piece in open space
When the player presses and releases the right arrow within 100 ms
Then the piece has moved exactly one column right
  And it does not move again
```

### SC-009: Auto-repeat stops at the wall and does not resume (FR-006, FR-004)

```gherkin
Given a falling piece five columns from the left wall
When the player holds the left arrow for two full seconds
Then the piece slides left until its leftmost cell is in column 0
  And it stops there
  And it is still in column 0 when the key is released
```

### SC-010: The most recent horizontal key wins (FR-007)

```gherkin
Given the player is holding the left arrow and the piece is auto-repeating left
When the player also presses and holds the right arrow
Then the piece moves one column right immediately
  And the auto-repeat delay restarts from 167 ms in the right direction
  And the piece no longer moves left while both keys are held
```

### SC-011: Releasing the newer key hands control back (FR-008)

```gherkin
Given the player is holding both horizontal keys with right as the active direction
When the player releases the right arrow while still holding the left arrow
Then the piece moves one column left immediately
  And auto-repeat resumes leftward after a fresh 167 ms delay
```

### SC-012: Release cancels a pending repeat (FR-009)

```gherkin
Given the player has held the left arrow for 160 ms, so the DAS delay has not yet expired
When the player releases the left arrow
Then the piece does not move again
  And no further movement occurs when the remaining 7 ms elapses
```

### SC-013: Clockwise rotation advances one quarter turn (FR-010)

```gherkin
Given a T tetromino is falling in orientation Spawn
When the player presses the up arrow
Then its orientation is Right
When the player presses X
Then its orientation is Inverted
```

### SC-014: Counter-clockwise rotation advances the other way (FR-010)

```gherkin
Given a T tetromino is falling in orientation Spawn
When the player presses Z
Then its orientation is Left
When the player presses Ctrl
Then its orientation is Inverted
```

### SC-015: Rotation does not auto-repeat (FR-011)

```gherkin
Given a falling piece in orientation Spawn
When the player presses and holds the up arrow for two seconds
Then the piece has rotated exactly once, to orientation Right
When the player releases and presses the up arrow again
Then the piece rotates to orientation Inverted
```

### SC-016: A rotated piece still occupies four cells (FR-012)

```gherkin
Given a falling piece of any type
When it is rotated to each of the four orientations in turn
Then it occupies exactly four cells in every orientation
  And those cells match the OrientationShape offsets defined for its type and orientation
```

### SC-017: The O tetromino does not move when rotated (FR-013)

```gherkin
Given an O tetromino is falling with its cells at (4,30) (5,30) (4,31) (5,31)
When the player rotates it clockwise four times and counter-clockwise four times
Then its cells are still (4,30) (5,30) (4,31) (5,31) at every step
  And no kick offset was ever tried
```

### SC-018: There is no 180° rotation (FR-014)

```gherkin
Given a falling piece in orientation Spawn
When the player presses every bound key in turn
Then no single key press produces orientation Inverted
  And reaching Inverted requires two rotations in the same direction
```

### SC-019: A rotation that fits needs no kick (FR-015)

```gherkin
Given an L tetromino is falling in open space in orientation Spawn
When the player rotates it clockwise
Then the rotation succeeds at kick test 1, the zero offset
  And the bounding box position is unchanged
```

### SC-020: A rotation against the left wall kicks the piece clear (FR-015, FR-018)

```gherkin
Given a J tetromino in orientation Right is flush against the left wall
When the player rotates it clockwise toward orientation Inverted
Then kick test 1 fails because a cell would fall outside the playfield
  And test 2, the offset one column right, succeeds
  And the piece now sits one column further right in orientation Inverted
```

### SC-021: A floor kick lifts a piece that cannot rotate in place (FR-018)

```gherkin
Given a vertical J tetromino in orientation Right is grounded on the stack
  And rotating it to Inverted at the zero offset would overlap occupied cells
  And kick tests 2 and 3 are also blocked
When the player rotates it clockwise
Then kick test 4 of the R to 2 row, the offset two rows upward, is applied
  And the piece ends in orientation Inverted two rows higher than it started
  And it is no longer grounded, so its lock timer is cancelled
```

### SC-022: The I tetromino uses its own kick table (FR-017)

```gherkin
Given an I tetromino and a T tetromino in otherwise identical positions and orientations
When each is rotated clockwise against the right wall
Then the I tetromino is tested against the I kick table, whose first non-zero offset is two columns left
  And the T tetromino is tested against the JLSTZ kick table, whose first non-zero offset is one column left
```

### SC-023: A rotation with no legal placement is refused (FR-016)

```gherkin
Given an S tetromino is wedged so that all five kick tests of its transition are blocked
When the player rotates it clockwise
Then the orientation is unchanged
  And the position is unchanged
  And no lock delay reset is consumed
```

### SC-024: Soft drop falls twenty times faster (FR-019)

```gherkin
Given a game session at level 1, where the gravity interval is 1.000 second
  And a piece is falling in an empty column
When the player presses and holds the down arrow
Then the piece descends one cell every 50 ms
When the player releases the down arrow
Then the piece returns to descending one cell per second
```

### SC-025: Soft drop never skips a cell (FR-020)

```gherkin
Given a piece is soft-dropping down an empty column
When 500 ms of game time elapses in a single update step
Then the piece has moved down at most one cell in that step
  And it has passed through every intervening cell without skipping any
```

### SC-026: A soft-dropped piece still gets its lock delay (FR-021)

```gherkin
Given a piece is soft-dropping toward the floor with the down arrow held
When it reaches the floor row and becomes grounded
Then it does not lock immediately
  And it locks 0.500 second later if nothing un-grounds it
  And the player can still move and rotate it during that half second
```

### SC-027: Soft-dropped cells are counted, not scored (FR-022, FR-023)

```gherkin
Given a piece has descended four cells under normal gravity
When the player holds the down arrow and the piece descends a further nine cells before locking
Then its soft drop cell count is 9
  And the session score is still 0
  And the count of 9 is handed to the line-clear and scoring feature at lock time
```

### SC-028: A successful move resets the lock delay (FR-025)

```gherkin
Given a grounded piece whose 0.500 second lock delay has been running for 0.300 second
  And it has used 0 of its 15 lock delay resets
When the player successfully moves it one column sideways and it is still grounded
Then the lock timer restarts from 0.000 second
  And 1 of the 15 resets has been consumed
```

### SC-029: A successful rotation resets the lock delay (FR-025)

```gherkin
Given a grounded piece with lock delay running
When the player successfully rotates it and it remains grounded
Then the lock timer restarts
  And one reset is consumed
```

### SC-030: A rejected action resets nothing (FR-026)

```gherkin
Given a grounded piece whose lock delay has been running for 0.400 second
  And it is flush against the right wall
When the player presses the right arrow and the move is rejected
Then the lock timer still reads 0.400 second
  And no reset has been consumed
  And the piece locks 0.100 second later
```

### SC-031: Input on an un-grounded piece consumes no budget (FR-026)

```gherkin
Given a piece falling in open space, not grounded
When the player moves it left three times and rotates it twice
Then no lock delay reset has been consumed
  And the lock timer is still null
```

### SC-032: Moving a grounded piece over a gap resumes gravity (FR-027)

```gherkin
Given a grounded piece resting on a ledge with an empty column beside it
When the player moves it sideways over the gap
Then the lock timer is cancelled
  And gravity resumes at one cell per gravity interval
  And the consumed reset is not refunded
```

### SC-033: An exhausted budget still allows movement (FR-028)

```gherkin
Given a grounded piece that has consumed all 15 lock delay resets
  And its lock delay has 0.200 second remaining
When the player moves it one column sideways
Then the move succeeds
  And the lock timer is not reset
  And the piece locks 0.200 second later in its new column
```

### SC-034: Reaching a new lowest row replenishes the budget (FR-029)

```gherkin
Given a grounded piece that has consumed 15 of its 15 lock delay resets on a ledge
When the player moves it sideways over a gap and it falls to a row lower than any it has occupied
Then its reset budget is replenished to 15
  And the next successful move of the grounded piece resets its lock delay again
```

### SC-035: Bound keys do not scroll the page (FR-030)

```gherkin
Given a game session is Playing and the page is taller than the viewport
When the player presses the down arrow and the up arrow repeatedly
Then the page does not scroll
  And the piece soft-drops and rotates as specified
```

### SC-036: Input is applied before gravity in the same step (FR-031)

```gherkin
Given a piece is one cell above becoming grounded
  And the player's move input and a gravity step fall in the same update step
When the update step is processed
Then the move is applied first
  And the gravity descent is then evaluated against the piece's new column
```

## 5. Domain Model

_This section **extends** spec 001 §5. Everything 001 defines — `GameSession`, `Playfield`,
`ActivePiece`, `PieceQueue`, `CellPosition`, `Cell`, `ShapeOffset`, `TetrominoDefinition`, `SevenBag`,
`SpawnPlacement`, and the `TetrominoType`, `Orientation` and `GameStatus` enums — is reused verbatim and
is not restated here. Only new types, new attributes on existing types, and the two tables 001 handed to
this spec appear below._

### 5.0 Sign convention for offsets

Everything in this section uses spec 001 §5.0's coordinate system, repeated here only because the sign
of a kick offset is easy to get wrong:

- `column` runs 0 → 9 left to right; `row` runs 0 → 39 **top to bottom**; gravity **increases** `row`.
- An offset is written `(columnDelta, rowDelta)`. **Positive `columnDelta` is right. Positive `rowDelta`
  is DOWN.** A kick that lifts a piece — a floor kick — therefore has a **negative** `rowDelta`.

> **Warning — published SRS tables are bottom-up.** Every public SRS reference states kick offsets as
> `(x, y)` with **`y` increasing upward**. This project's row axis increases downward. The conversion is
> `columnDelta = x`, **`rowDelta = −y`**. The tables in §5.3 below are already converted and are stated
> in this project's top-down convention — do **not** apply the conversion to them a second time, and do
> not copy a table from an external reference without negating its `y` column first.

### 5.1 Entities

#### ActivePiece *(extended — see spec 001 §5.1 for the base definition)*

Two attributes are added. All of 001's attributes — `type`, `orientation`, `position`,
`fallTimerElapsed`, `lockTimerElapsed`, `lockResetsUsed`, `lowestRowReached` — keep their meaning and
constraints unchanged. This spec is the first to *write* `orientation` after spawn.

| Attribute      | Type    | Constraints                     | Description                                                                                                       |
| -------------- | ------- | ------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| softDropCells  | integer | ≥ 0, starts at 0                | Count of cells this piece has descended while `↓` was held (DR-41). Read by story 006 at lock time; never scored here. |
| lastKickTest   | integer | 1–5, null until first rotation  | Which `KickOffset` of the row was applied by the most recent successful rotation. Diagnostic; story 006 may read it to detect a T-spin. |

> `lastKickTest` exists because the Guideline's T-spin detection depends on *how* the last rotation
> succeeded, and story 006 owns T-spin scoring. This spec only records the value; it attaches no meaning
> to it beyond §4's assertions.

#### InputState

Keyboard state for the current `GameSession`. One per session, created with the session and reset by
the *New game* action (FR-005 of spec 001). It holds only what auto-repeat and soft drop need to know
between update steps; it is not part of the playfield and is never persisted.

| Attribute            | Type                | Constraints                             | Description                                                                               |
| -------------------- | ------------------- | --------------------------------------- | ------------------------------------------------------------------------------------------- |
| heldDirections       | HorizontalDirection[] | ordered by press time, oldest first, length 0–2 | Which of `←`/`→` are currently down. The **last** element is the active direction (DR-32). |
| activeDirection      | HorizontalDirection | nullable, derived from `heldDirections` | The direction auto-repeat is currently driving. Null when neither key is held.              |
| dasTimerElapsed      | duration            | ≥ 0, null when no direction is held     | Time accumulated toward the 167 ms DAS delay (DR-31).                                       |
| autoRepeatArmed      | boolean             | default false                           | True once DAS has elapsed and ARR repeats are being emitted.                                |
| arrTimerElapsed      | duration            | ≥ 0, null until `autoRepeatArmed`       | Time accumulated toward the next 33 ms ARR repeat (DR-31).                                  |
| softDropHeld         | boolean             | default false                           | True while `↓` is down (DR-39).                                                             |

**Lifecycle.** All fields reset to their defaults when a session is constructed, when the *New game*
action fires, and when the session reaches `GameOver`. `heldDirections` and `softDropHeld` are **not**
cleared when a piece locks — a key the player is still physically holding keeps acting on the next piece
(DR-31, FR-024) — but the per-piece `softDropCells` counter is.

### 5.2 Relationships

Added to spec 001 §5.2; none of 001's relationships change.

- A **GameSession** has exactly one **InputState**, for the session's whole lifetime.
- An **InputState** drives the session's single **ActivePiece**, or nothing when there is no active piece.
- A **TetrominoDefinition** has exactly four **OrientationShape** entries, one per **Orientation**. The
  `Spawn` entry is the `spawnOffsets` already defined in spec 001 §5.3 — it is the same data, surfaced
  under a uniform accessor, not a second copy.
- A **TetrominoDefinition** refers to exactly one **KickTable**, selected by `kickTableKind`: all of
  J, L, S, T and Z share the single `JLSTZ` table; I has its own `I` table; O has `None` (DR-36).
- A **KickTable** has exactly eight **KickTableRow** entries, one per **RotationTransition**.
- A **KickTableRow** holds exactly five ordered **KickOffset** values, tests 1 to 5.

### 5.3 Value Objects

#### OrientationShape — *the table spec 001 handed to this spec*

The four occupied cells of a type in one orientation, as `ShapeOffset` values relative to the bounding
box top-left (spec 001 §5.3). Immutable reference data, one entry per (`type`, `orientation`) pair — 28
entries in total.

| Attribute   | Type           | Constraints                   |
| ----------- | -------------- | ----------------------------- |
| type        | TetrominoType  | required                      |
| orientation | Orientation    | required                      |
| offsets     | ShapeOffset[4] | exactly four, distinct (DR-05) |

The box is the square given by `TetrominoDefinition.boundingBoxSize`: 4×4 for I, 2×2 for O, 3×3 for the
other five. Each orientation is the previous one rotated a quarter turn clockwise **inside that box** —
formally `(columnOffset, rowOffset) → (size − 1 − rowOffset, columnOffset)`. The `Spawn` row of each
table below is reproduced from spec 001 §5.3 for readability; **001 remains its authority**.

**I tetromino — 4×4 box**

| Orientation | Shape                                     | offsets                 |
| ----------- | ----------------------------------------- | ----------------------- |
| `Spawn`     | `....` / `XXXX` / `....` / `....`         | (0,1) (1,1) (2,1) (3,1) |
| `Right`     | `..X.` / `..X.` / `..X.` / `..X.`         | (2,0) (2,1) (2,2) (2,3) |
| `Inverted`  | `....` / `....` / `XXXX` / `....`         | (0,2) (1,2) (2,2) (3,2) |
| `Left`      | `.X..` / `.X..` / `.X..` / `.X..`         | (1,0) (1,1) (1,2) (1,3) |

**O tetromino — 2×2 box**

| Orientation | Shape           | offsets                 |
| ----------- | --------------- | ----------------------- |
| `Spawn`     | `XX` / `XX`     | (0,0) (1,0) (0,1) (1,1) |
| `Right`     | `XX` / `XX`     | (0,0) (1,0) (0,1) (1,1) |
| `Inverted`  | `XX` / `XX`     | (0,0) (1,0) (0,1) (1,1) |
| `Left`      | `XX` / `XX`     | (0,0) (1,0) (0,1) (1,1) |

All four are identical — this is why DR-36 makes O's rotation a no-op in position.

**T tetromino — 3×3 box**

| Orientation | Shape                      | offsets                 |
| ----------- | -------------------------- | ----------------------- |
| `Spawn`     | `.X.` / `XXX` / `...`      | (1,0) (0,1) (1,1) (2,1) |
| `Right`     | `.X.` / `.XX` / `.X.`      | (1,0) (1,1) (2,1) (1,2) |
| `Inverted`  | `...` / `XXX` / `.X.`      | (0,1) (1,1) (2,1) (1,2) |
| `Left`      | `.X.` / `XX.` / `.X.`      | (1,0) (0,1) (1,1) (1,2) |

**S tetromino — 3×3 box**

| Orientation | Shape                      | offsets                 |
| ----------- | -------------------------- | ----------------------- |
| `Spawn`     | `.XX` / `XX.` / `...`      | (1,0) (2,0) (0,1) (1,1) |
| `Right`     | `.X.` / `.XX` / `..X`      | (1,0) (1,1) (2,1) (2,2) |
| `Inverted`  | `...` / `.XX` / `XX.`      | (1,1) (2,1) (0,2) (1,2) |
| `Left`      | `X..` / `XX.` / `.X.`      | (0,0) (0,1) (1,1) (1,2) |

**Z tetromino — 3×3 box**

| Orientation | Shape                      | offsets                 |
| ----------- | -------------------------- | ----------------------- |
| `Spawn`     | `XX.` / `.XX` / `...`      | (0,0) (1,0) (1,1) (2,1) |
| `Right`     | `..X` / `.XX` / `.X.`      | (2,0) (1,1) (2,1) (1,2) |
| `Inverted`  | `...` / `XX.` / `.XX`      | (0,1) (1,1) (1,2) (2,2) |
| `Left`      | `.X.` / `XX.` / `X..`      | (1,0) (0,1) (1,1) (0,2) |

**J tetromino — 3×3 box**

| Orientation | Shape                      | offsets                 |
| ----------- | -------------------------- | ----------------------- |
| `Spawn`     | `X..` / `XXX` / `...`      | (0,0) (0,1) (1,1) (2,1) |
| `Right`     | `.XX` / `.X.` / `.X.`      | (1,0) (2,0) (1,1) (1,2) |
| `Inverted`  | `...` / `XXX` / `..X`      | (0,1) (1,1) (2,1) (2,2) |
| `Left`      | `.X.` / `.X.` / `XX.`      | (1,0) (1,1) (0,2) (1,2) |

**L tetromino — 3×3 box**

| Orientation | Shape                      | offsets                 |
| ----------- | -------------------------- | ----------------------- |
| `Spawn`     | `..X` / `XXX` / `...`      | (2,0) (0,1) (1,1) (2,1) |
| `Right`     | `.X.` / `.X.` / `.XX`      | (1,0) (1,1) (1,2) (2,2) |
| `Inverted`  | `...` / `XXX` / `X..`      | (0,1) (1,1) (2,1) (0,2) |
| `Left`      | `XX.` / `.X.` / `.X.`      | (0,0) (1,0) (1,1) (1,2) |

#### RotationDirection

Which way a rotation key turns the piece. Not an `Orientation` — it is the *verb*.

| Member               | Key(s)        | Effect on `Orientation`                                         |
| -------------------- | ------------- | ----------------------------------------------------------------- |
| `Clockwise`          | `↑`, `X`      | `Spawn → Right → Inverted → Left → Spawn` (DR-33).                |
| `CounterClockwise`   | `Z`, `Ctrl`   | `Spawn → Left → Inverted → Right → Spawn` (DR-33).                |

#### RotationTransition

An ordered pair of orientations naming one of the eight legal quarter-turn transitions. It is the key
into a `KickTable`. Written in the Guideline's shorthand, `0` = `Spawn`, `R` = `Right`, `2` = `Inverted`,
`L` = `Left`.

| Attribute | Type        | Constraints                                          |
| --------- | ----------- | ---------------------------------------------------- |
| from      | Orientation | required                                             |
| to        | Orientation | required, a quarter turn from `from` in either sense  |

The eight members are `0→R`, `R→0`, `R→2`, `2→R`, `2→L`, `L→2`, `L→0`, `0→L`. The pairs `0↔2` and `R↔L`
are **not** transitions — there is no 180° rotation (FR-014, DR-33).

#### KickOffset

One candidate displacement tried during a rotation, in this project's top-down convention (§5.0).

| Attribute    | Type    | Constraints              | Description                                     |
| ------------ | ------- | ------------------------ | ------------------------------------------------- |
| columnDelta  | integer | −2 … +2                  | Positive is right.                               |
| rowDelta     | integer | −2 … +2                  | **Positive is DOWN**; negative lifts the piece.  |

#### KickTableRow

The five ordered tests for one `RotationTransition`. Test 1 is always `(0, 0)`.

| Attribute  | Type               | Constraints                              |
| ---------- | ------------------ | ---------------------------------------- |
| transition | RotationTransition | required, unique within the table        |
| tests      | KickOffset[5]      | exactly five, ordered; `tests[1] = (0,0)` |

#### KickTable — *the table spec 001 handed to this spec*

Three tables exist as immutable reference data, selected by `TetrominoDefinition.kickTableKind`:
`JLSTZ`, `I` and `None`.

**JLSTZ kick table** — used by J, L, S, T and Z. All offsets are `(columnDelta, rowDelta)`, **top-down,
positive `rowDelta` = down**.

| Transition | Test 1 | Test 2 | Test 3  | Test 4  | Test 5  |
| ---------- | ------ | ------ | ------- | ------- | ------- |
| `0→R`      | (0, 0) | (−1, 0) | (−1, −1) | (0, +2) | (−1, +2) |
| `R→0`      | (0, 0) | (+1, 0) | (+1, +1) | (0, −2) | (+1, −2) |
| `R→2`      | (0, 0) | (+1, 0) | (+1, +1) | (0, −2) | (+1, −2) |
| `2→R`      | (0, 0) | (−1, 0) | (−1, −1) | (0, +2) | (−1, +2) |
| `2→L`      | (0, 0) | (+1, 0) | (+1, −1) | (0, +2) | (+1, +2) |
| `L→2`      | (0, 0) | (−1, 0) | (−1, +1) | (0, −2) | (−1, −2) |
| `L→0`      | (0, 0) | (−1, 0) | (−1, +1) | (0, −2) | (−1, −2) |
| `0→L`      | (0, 0) | (+1, 0) | (+1, −1) | (0, +2) | (+1, +2) |

**I kick table** — used by I only. Same convention.

| Transition | Test 1 | Test 2  | Test 3  | Test 4   | Test 5   |
| ---------- | ------ | ------- | ------- | -------- | -------- |
| `0→R`      | (0, 0) | (−2, 0) | (+1, 0) | (−2, +1) | (+1, −2) |
| `R→0`      | (0, 0) | (+2, 0) | (−1, 0) | (+2, −1) | (−1, +2) |
| `R→2`      | (0, 0) | (−1, 0) | (+2, 0) | (−1, −2) | (+2, +1) |
| `2→R`      | (0, 0) | (+1, 0) | (−2, 0) | (+1, +2) | (−2, −1) |
| `2→L`      | (0, 0) | (+2, 0) | (−1, 0) | (+2, −1) | (−1, +2) |
| `L→2`      | (0, 0) | (−2, 0) | (+1, 0) | (−2, +1) | (+1, −2) |
| `L→0`      | (0, 0) | (+1, 0) | (−2, 0) | (+1, +2) | (−2, −1) |
| `0→L`      | (0, 0) | (−1, 0) | (+2, 0) | (−1, −2) | (+2, +1) |

**`None`** — used by O. No row is ever consulted; see DR-36.

> **Cross-check against a published table.** Negating the `rowDelta` column above reproduces the standard
> bottom-up `(x, y)` SRS tables exactly — e.g. JLSTZ `0→R` becomes `(0,0) (−1,0) (−1,+1) (0,−2) (−1,−2)`,
> and I `0→R` becomes `(0,0) (−2,0) (+1,0) (−2,−1) (+1,+2)`. Use that identity as the review check.
> A table whose test 4 and 5 `rowDelta` signs match the published source verbatim has not been converted
> and is **wrong** for this project: pieces would kick into the floor instead of out of it.

#### KeyBinding

Immutable reference data mapping a physical key to a command. Keys not listed here are ignored (DR-29).

| Key        | Command                                      | Auto-repeat        | Owner          |
| ---------- | -------------------------------------------- | ------------------ | -------------- |
| `←`        | Move one cell left                           | DAS + ARR (DR-31)  | this spec      |
| `→`        | Move one cell right                          | DAS + ARR (DR-31)  | this spec      |
| `↓`        | Soft drop while held                         | Continuous (DR-39) | this spec      |
| `↑`        | Rotate clockwise                             | None (DR-34)       | this spec      |
| `X`        | Rotate clockwise                             | None (DR-34)       | this spec      |
| `Z`        | Rotate counter-clockwise                     | None (DR-34)       | this spec      |
| `Ctrl`     | Rotate counter-clockwise                     | None (DR-34)       | this spec      |
| `Space`    | *Reserved — hard drop*                       | —                  | **story 003**  |
| `C`        | *Reserved — hold*                            | —                  | **story 005**  |
| `Shift`    | *Reserved — hold*                            | —                  | **story 005**  |

`X` and `Z` are matched by physical key position, not by the character a non-QWERTY layout produces, so
the two rotation keys stay adjacent on any layout. Either `Ctrl` key rotates counter-clockwise.

#### InputTiming

The Guideline auto-repeat and soft-drop constants, named so later specs and tests can cite them.

| Name              | Value                   | Meaning                                                                      |
| ----------------- | ----------------------- | ------------------------------------------------------------------------------ |
| `dasDelay`        | 167 ms                  | Delay between the first move and the start of auto-repeat (DR-31).            |
| `arrInterval`     | 33 ms                   | Time per auto-repeated cell once DAS has elapsed (DR-31).                     |
| `softDropFactor`  | 20                      | Multiplier on gravity while `↓` is held (DR-39).                              |
| `softDropInterval`| derived: `gravityInterval / softDropFactor`; **50 ms at level 1** | Time per soft-dropped cell (DR-39). |

`softDropInterval` tracks `gravityInterval`, so story 007's level curve speeds soft drop up with the rest
of the game. At the point where `gravityInterval` would fall below `softDropInterval`, normal gravity is
already the faster of the two and soft drop stops having any effect on speed (EC-13).

### 5.4 Enumerations

Added to spec 001 §5.4. `TetrominoType`, `Orientation` and `GameStatus` are unchanged.

#### HorizontalDirection

| Member  | Key | Effect on `position.column` |
| ------- | --- | --------------------------- |
| `Left`  | `←` | −1                          |
| `Right` | `→` | +1                          |

#### KickTableKind

| Member  | Applies to       | Meaning                                                           |
| ------- | ---------------- | ------------------------------------------------------------------- |
| `JLSTZ` | J, L, S, T, Z    | The shared five-test table above.                                   |
| `I`     | I                | The I tetromino's distinct five-test table above.                   |
| `None`  | O                | Rotation never kicks; it is a no-op in position (DR-36).            |

Added to `TetrominoDefinition` as the attribute `kickTableKind` (required, one per type), alongside the
`orientationShapes` accessor that returns the four `OrientationShape` entries for the type.

#### InputCommand

The command a key press or release resolves to. Introduced so DR-43's ordering rule has something to
order.

| Member                | Trigger                       |
| --------------------- | ----------------------------- |
| `MoveLeft`            | `←` press, and each DAS/ARR repeat while held |
| `MoveRight`           | `→` press, and each DAS/ARR repeat while held |
| `RotateClockwise`     | `↑` or `X` press              |
| `RotateCounterClockwise` | `Z` or `Ctrl` press        |
| `SoftDropStart`       | `↓` press                     |
| `SoftDropEnd`         | `↓` release                   |

### 5.5 Domain Rules and Invariants

_Continuing spec 001's numbering. **DR-01 … DR-27 are defined in spec 001 §5.5 and are not restated
here** — they remain in force. Later specs should cite these by ID and name, e.g. "per **Kick Test Order
(DR-35)**"._

- **DR-28 — Input Gate**: Keyboard input affects the game only while `GameSession.status` is
  `GameStatus.Playing` **and** an `ActivePiece` exists. In `Ready` and `GameOver` every bound key is
  inert, consistent with **Terminal Status (DR-27)**. Key presses arriving in the instant between a lock
  and the next spawn (**Spawn Immediacy, DR-20**) are discarded, not queued; only held-key *state*
  survives, per DR-31.
- **DR-29 — Key Bindings**: The bindings are exactly those in §5.3 `KeyBinding` — `←`, `→`, `↓`, `↑`,
  `X`, `Z`, `Ctrl` — and they are fixed, not configurable. `Space`, `C` and `Shift` are reserved for
  stories 003 and 005 and do nothing in this spec. Every other key is ignored.
- **DR-30 — Horizontal Step**: A move shifts the piece's `position.column` by exactly ±1 and changes
  nothing else — not `row`, not `orientation`. It is applied only if the resulting placement is valid
  under **Piece Containment (DR-03)** and **No Overlap (DR-04)**; otherwise it is rejected per DR-38.
  There is no multi-cell move and no wall wrap.
- **DR-31 — Delayed Auto Shift**: Pressing `←` or `→` performs one move immediately. If the key is still
  held after `dasDelay` (**167 ms**), further moves are emitted every `arrInterval` (**33 ms**) until the
  key is released. Timers advance on measured elapsed time, per **Gravity Advances on Elapsed Time
  (DR-26)**. Releasing a key clears its DAS and ARR timers at once; no repeat may fire after release.
  A rejected repeat (the piece is against a wall or the stack) does not end auto-repeat — the timers keep
  running, so the piece resumes sliding the moment the obstruction clears. Held-key state survives a
  lock, so a player holding `←` keeps sliding the next piece without re-pressing.
- **DR-32 — Opposing Direction Priority**: When both horizontal keys are held, the **most recently
  pressed** wins; its press performs an immediate move and restarts DAS. The older direction is suspended,
  not forgotten: if the newer key is released while the older is still held, the older becomes active
  again with an immediate move and a fresh DAS delay. The two directions never cancel to no movement.
- **DR-33 — Rotation Cycle**: `RotationDirection.Clockwise` advances `Orientation` along
  `Spawn → Right → Inverted → Left → Spawn`; `CounterClockwise` advances along
  `Spawn → Left → Inverted → Right → Spawn`. Only these eight quarter-turn transitions exist. **There is
  no 180° rotation**: `Spawn ↔ Inverted` and `Right ↔ Left` are never single steps, and no key produces
  one.
- **DR-34 — One Rotation Per Press**: Rotation does not auto-repeat. A held rotation key produces exactly
  one rotation; the key must be released and pressed again to rotate again. This holds for all four
  rotation keys, and holding `↑` and `X` simultaneously still yields one rotation per distinct press.
- **DR-35 — Kick Test Order**: A rotation computes the target orientation's `OrientationShape`, then
  tests the five `KickOffset` values of the `KickTableRow` for its `RotationTransition` **in order, 1 to
  5**, stopping at the first offset whose resulting placement is valid under DR-03 and DR-04. That offset
  is applied to `position` and `orientation` is updated; `lastKickTest` records which test succeeded.
  Only if all five fail is the rotation rejected per DR-38. Offsets apply on both axes, so a rotation may
  displace a piece sideways (wall kick) and/or vertically — a negative `rowDelta` lifts the piece out of
  the floor or the stack (floor kick). The table is selected by `kickTableKind`, never by orientation or
  position.
- **DR-36 — O Rotation Is a No-Op in Position**: The O tetromino's four `OrientationShape` entries are
  identical, so rotating an O advances `orientation` through the DR-33 cycle while its four occupied
  cells stay exactly where they were. Its `kickTableKind` is `None` and no kick test is performed. An O
  rotation therefore never fails — which means, per DR-37, it always resets the lock delay of a grounded
  O and always costs one unit of the 15-reset budget.
- **DR-37 — Lock Delay Reset Trigger**: *This is the trigger spec 001 left to this spec under* **Lock
  Reset Budget (DR-17)**. A **successful** horizontal move (DR-30) or rotation (DR-35) of a piece that is
  **grounded** (**Grounded, DR-15**) at the moment the action is applied resets its `lockTimerElapsed` to
  zero and increments `lockResetsUsed` by one — provided `lockResetsUsed < 15`. Gravity steps, soft drop
  steps, spawns and story 003's hard drop are **not** reset triggers. When the budget is exhausted the
  action still succeeds and the piece still moves; only the reset is withheld, and the running delay
  proceeds to expiry. If the successful action leaves the piece un-grounded, **Lock Timer Cancellation
  (DR-18)** applies instead: the timer is cancelled rather than reset, no budget unit is consumed, and
  gravity resumes. If the action places the piece in a row lower than `lowestRowReached`, DR-17's
  replenishment to 15 applies and takes precedence over the increment.
- **DR-38 — A Rejected Action Is Inert**: A move or rotation that produces no valid placement changes
  **nothing**: not `position`, not `orientation`, not `fallTimerElapsed`, not `lockTimerElapsed`, not
  `lockResetsUsed`, not `softDropCells`, not `lowestRowReached`, and not the playfield. In particular it
  does not reset the lock delay — a player cannot stall a piece by pushing it into a wall. The attempt is
  not an error and produces no message; the piece simply does not move.
- **DR-39 — Soft Drop Rate**: While `↓` is held, the active piece's fall timer advances toward
  `softDropInterval` = `gravityInterval / 20` instead of `gravityInterval`. At level 1 this is **50 ms**
  per cell. Releasing `↓` restores the normal interval immediately, carrying the accrued fall time over
  rather than discarding it. Soft drop never moves a piece more than one cell per update step, per
  **Gravity Advances on Elapsed Time (DR-26)**, so every intervening cell is collision-checked and no
  cell is skipped. Soft drop is pure acceleration of gravity: it respects DR-03, DR-04 and DR-15 exactly
  as normal gravity does.
- **DR-40 — Soft Drop Preserves Lock Delay**: A soft-dropped piece that becomes grounded gets the full
  **0.500 s** of **Lock Delay (DR-16)**, identical to a piece that arrived under normal gravity. Holding
  `↓` neither shortens the delay nor forces a lock, and the piece remains fully movable and rotatable for
  that half second. An instant lock on contact is story 003's hard drop, not this.
- **DR-41 — Soft Drop Cell Count**: `ActivePiece.softDropCells` increments by one for each cell the piece
  descends while `↓` is held. Cells descended under normal gravity, by a floor kick (DR-35), or by story
  003's hard drop do not count. The counter is per piece: it starts at 0 on spawn — even if `↓` is still
  held from the previous piece — and its final value is handed to **story 006** when the piece locks.
  **Story 006 owns the award (1 point per cell); this spec awards nothing and leaves `score` at 0.**
- **DR-42 — Input Capture**: While a session is `Playing`, the browser's default action for every bound
  key is suppressed — most importantly the arrow keys' page scrolling and `Space`'s page-down, reserved
  for story 003. Suppression is scoped to the game: ordinary keyboard use elsewhere on the page, and all
  keys while the session is `Ready` or `GameOver`, behave normally.
- **DR-43 — Input Ordering**: Within one update step, pending `InputCommand` values are applied in the
  order they were received, and all of them are applied **before** that step's gravity or soft drop
  descent. This makes a frame's outcome deterministic and independent of frame rate, so the §4 scenarios
  are reproducible headlessly (NFR-006 of spec 001).

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Spec 001's NFR-001 … NFR-008 still apply to the core loop;
project-wide qualities belong in arc42 §10 (currently empty)._

| ID      | Category      | Requirement                                                                                                                                                                                                       |
| ------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Responsiveness| The interval from a key press being delivered by the browser to the moved or rotated piece appearing on screen is **≤ 50 ms at p95 and ≤ 100 ms at p99**, measured over 500 presses on a mid-range Info Support laptop at 60 Hz. |
| NFR-002 | Responsiveness| No input is dropped at up to **20 key events per second** sustained — the rate a fast player reaches tapping and rotating — and every delivered press produces exactly one move or rotation attempt (DR-43).         |
| NFR-003 | Responsiveness| Auto-repeat timing holds to **±10 ms** of the 167 ms DAS delay and **±5 ms** of the 33 ms ARR interval under normal load, so a held slide feels even rather than stuttering.                                        |
| NFR-004 | Correctness   | A full traversal of all **56 kick-table rows** (7 types × 8 transitions) against a reference SRS implementation produces identical resulting positions in **100%** of cases, including every floor-kick row.         |
| NFR-005 | Correctness   | Soft drop at level 1 covers **20 rows in 1.00 s ± 50 ms**, and a piece released from soft drop resumes the level-1 rate within one gravity interval.                                                                |
| NFR-006 | Testability   | Every input is injectable as an `InputCommand` with a supplied timestamp, so all §4 scenarios — including the 167 ms and 33 ms timings — run headlessly without a real keyboard or real elapsed time.                |
| NFR-007 | Robustness    | Input handling never produces a placement violating DR-03 or DR-04, under any sequence or timing of key events, including simultaneous and rapidly alternating presses (verified by property-based testing, SUC-005). |
| NFR-008 | Usability     | The bindings work on QWERTY, AZERTY and QWERTZ laptop keyboards without the player changing layout: `X` and `Z` are matched by physical key position, and either `Ctrl` key rotates counter-clockwise (§5.3).        |
| NFR-009 | Accessibility | Every action is reachable with one hand on the arrow keys alone (`←` `→` `↓` `↑`), so no player is required to use the two-handed `X`/`Z`/`Ctrl` alternatives.                                                      |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                       | Expected Behavior                                                                                                                                                      |
| ----- | ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| EC-1  | A move or rotation would push a cell past column 0 or 9, or past row 39.                       | Rejected per DR-30/DR-35; for rotation the next kick test is tried. Nothing changes if all tests fail (DR-38). No wrap-around, no clamping.                               |
| EC-2  | All five kick tests of a transition fail — the piece is wedged.                                | The rotation is refused silently (DR-35, DR-38). No lock reset is consumed. The piece stays rotatable; a later move may free it.                                          |
| EC-3  | The player holds `←` and `→` at the same time.                                                 | The most recently pressed direction wins and restarts DAS (DR-32). Releasing it hands control back to the other after a fresh DAS. The piece never freezes.               |
| EC-4  | The player holds `←` against the left wall for several seconds.                                | Each repeat is rejected and inert (DR-38). Auto-repeat timers keep running, so the piece slides immediately if the wall is later cleared by a rotation. No lock stalling.  |
| EC-5  | A key is held when a piece locks and the next piece spawns.                                    | Held-key state survives the lock (DR-31): the new piece begins auto-repeating or soft-dropping without a re-press. Its `softDropCells` still starts at 0 (DR-41).          |
| EC-6  | A key is held when the session reaches `GameOver` (block out, DR-21, or lock out, DR-22).      | All input becomes inert immediately (DR-28). `InputState` resets; a key still held when a new game starts must be released and pressed again.                             |
| EC-7  | The browser tab loses focus while a key is held (alt-tab, click elsewhere).                    | All held keys are treated as released: auto-repeat and soft drop stop, and the piece reverts to normal gravity. A runaway slide while the player is elsewhere is a bug.    |
| EC-8  | A key-up event is never delivered (focus loss, OS quirk, modal dialog).                        | Same as EC-7 — loss of focus releases everything. No piece may auto-repeat indefinitely on a key the player is no longer pressing.                                        |
| EC-9  | A single update step carries a large elapsed delta after a stall (spec 001 EC-2).              | The step is capped at 100 ms (DR-26) and the piece moves at most one cell down (DR-39). At most one ARR repeat is emitted for that step; repeats are not batched.          |
| EC-10 | A rotation succeeds with a floor kick, lifting a grounded piece clear of the stack.            | The piece is no longer grounded: the lock timer is cancelled and gravity resumes (DR-18, DR-37). No budget unit is consumed, and none is refunded. The lifted cells do not count toward `softDropCells` (DR-41). |
| EC-11 | A kick displaces the piece downward into a row lower than any it has occupied.                 | `lowestRowReached` updates and the 15-reset budget is replenished (DR-17, DR-37). This is the intended Guideline behaviour, not an exploit — the piece made real progress. |
| EC-12 | A rotation or move would place cells into buffer rows (row ≤ 19).                              | Legal — buffer rows are ordinary playfield cells (DR-01, DR-03). The affected cells are simply not rendered (FR-004 of spec 001). Lock out still applies at lock time (DR-22). |
| EC-13 | Story 007 raises the level until `gravityInterval` ≤ `softDropInterval`.                       | Soft drop becomes a no-op on speed: the piece already falls at least that fast. `↓` is still accepted and still counts cells for DR-41. No negative or zero interval arises. |
| EC-14 | The player presses `↓` on a piece that is already grounded.                                    | Nothing descends, because the piece cannot move down (DR-15). No lock reset — soft drop is not a reset trigger (DR-37). `softDropCells` does not increment (DR-41).         |
| EC-15 | The player rotates an O tetromino while it is grounded.                                        | The rotation always succeeds (DR-36), so it always resets the lock delay and always costs one of the 15 resets. After 15 such rotations the piece locks on schedule (DR-17). |
| EC-16 | The player presses `Space`, `C` or `Shift`.                                                    | Nothing happens in this spec — the keys are reserved for stories 003 and 005 (DR-29). The browser default is still suppressed for `Space` so the page does not scroll (DR-42). |
| EC-17 | The player holds `Ctrl` and presses another key (e.g. `Ctrl`+`R`, `Ctrl`+`T`).                 | The `Ctrl` press rotates counter-clockwise once; the browser shortcut is left alone. Browser-reserved combinations are never swallowed — reloading the page must keep working. |
| EC-18 | Key events arrive out of order or with timestamps in the past.                                 | Applied in arrival order (DR-43); a non-positive delta is treated as zero (spec 001 EC-10). Timers never run backwards and the piece never moves up except by a kick.       |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                      |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 36 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                  |
| SUC-002 | All 16 domain rules in §5.5 (DR-28 … DR-43) are each covered by at least one automated test, and spec 001's DR-03, DR-04, DR-15, DR-17 and DR-18 gain tests exercising them through this spec's input paths. |
| SUC-003 | All 28 `OrientationShape` entries are asserted cell-by-cell, and rotating any type four times in either direction returns it to its exact starting cells.           |
| SUC-004 | All 56 kick-table rows are asserted offset-by-offset against the published SRS tables after the `rowDelta = −y` conversion (§5.3), including every floor-kick row.  |
| SUC-005 | A property-based run of 100,000 random key-event sequences against a random stack produces no invalid placement, no stuck piece, no piece outside bounds, and no lock reset beyond the 15 permitted per piece. |
| SUC-006 | A player can slide a piece from column 0 to column 9 by holding one arrow key in **under 500 ms** (167 ms DAS + 8 × 33 ms ARR ≈ 431 ms).                           |
| SUC-007 | A T-spin triple is achievable: a T piece can be rotated into a covered three-row well that it cannot reach by movement alone, confirming the kick tables are correct. |
| SUC-008 | Measured input-to-render latency meets NFR-001 over a 500-press sample, and three colleagues playing informally report the controls as responsive rather than laggy. |
| SUC-009 | A 10-minute played session ends in block out or lock out with no exception, no dropped input and no key stuck in auto-repeat.                                       |
| SUC-010 | Specs 003 and 005 can bind `Space`, `C` and `Shift` and reuse `InputState`, `KeyBinding` and `InputCommand` without renaming or restructuring anything defined here. |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 001 (`docs/specs/001-start-new-game.md`)** — a hard prerequisite. This spec adds nothing that
  can be built or tested without 001's `GameSession`, `Playfield`, `ActivePiece`, the gravity loop and
  DR-01 … DR-27. In particular it consumes **Piece Containment (DR-03)**, **No Overlap (DR-04)**,
  **Tetromino Cell Count (DR-05)**, **Spawn Orientation (DR-07)**, **Gravity Step (DR-14)**, **Grounded
  (DR-15)**, **Lock Delay (DR-16)**, **Lock Reset Budget (DR-17)**, **Lock Timer Cancellation (DR-18)**,
  **Gravity Advances on Elapsed Time (DR-26)** and **Terminal Status (DR-27)**, and it supplies the
  `Right`/`Inverted`/`Left` offsets, the SRS kick tables and DR-17's missing reset trigger.
- **The modern Tetris Guideline** — the normative source for the bindings, DAS/ARR values, the 20× soft
  drop factor, the SRS kick tables, and the absence of 180° rotation.
- **`docs/product/story-map.md`** — the source of story 002's scope and of the project-wide exclusions
  quoted in §1.3.
- **Downstream dependents**: **003** (hard drop — binds `Space`, reuses `InputState` and `KeyBinding`,
  and must not count its cells toward `softDropCells` per DR-41); **005** (hold — binds `C`/`Shift` and
  must decide whether a swap resets lock delay, which DR-37 deliberately does not cover); **006** (line
  clearing and scoring — consumes `softDropCells` for the 1-point-per-cell award, and may read
  `lastKickTest` for T-spin recognition); **007** (levels — changes `gravityInterval` and therefore
  `softDropInterval`, see EC-13); **014** (controls panel — renders the `KeyBinding` table from §5.3 and
  must stay in sync with DR-29).

### 9.2 Constraints

- **Platform**: desktop browser on a laptop with a physical keyboard. No gamepad, touch or on-screen
  controls (story map).
- **Fixed bindings**: the DR-29 bindings are not configurable in this project. Story 014 documents them;
  nothing rebinds them.
- **Browser key handling**: the implementation must not swallow browser-reserved combinations (EC-17),
  and must handle key events through the standard DOM keyboard model — which means it cannot rely on
  OS-level key repeat, since its rate is user-configurable and is not 167/33 ms. DAS and ARR must be
  timed by the game's own clock (DR-31, DR-26).
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed here): ASP.NET
  Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`. Where
  the game loop runs — browser or server — is still the open architecture decision recorded by spec 001
  (§10 question 1); it bears on this spec more than on 001, because NFR-001's 50 ms p95 budget is hard to
  meet with a network round trip per key press. §10 question 2 below records that.
- **No pause, no ghost piece** (story map) — the player gets no preview of where a move or rotation lands.
- **Determinism**: input must be injectable with supplied timestamps (NFR-006) so the DAS/ARR scenarios
  in §4 are testable without a real keyboard.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in, exactly as spec 001 found them. The references
> below state what *this feature needs those sections to say*.

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                      |
| ----------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01. Introduction and Goals (`01-introduction-and-goals.md`)       | Should list **responsiveness** as a top quality driver alongside 001's zero-friction goal: a faithful Tetris is unplayable if input lags. NFR-001 is its quality scenario. *Currently empty.* |
| 02. Architecture Constraints (`02-architecture-constraints.md`)   | Should record the §9.2 constraints: keyboard-only, fixed bindings, and the inability to rely on OS key repeat for DAS/ARR. *Currently empty.*                                   |
| 03. System Scope and Context (`03-system-scope-and-context.md`)   | The keyboard is the system's only inbound interface in the Play Tetris activity. This spec defines that interface completely. *Currently empty.*                               |
| 04. Solution Strategy (`04-solution-strategy.md`)                 | The browser-versus-server game loop decision (spec 001 §10 q1) is forced by NFR-001 — a per-keystroke round trip cannot meet 50 ms p95. See §10 q2. *Currently empty.*          |
| 05. Building Block View (`05-building-block-view.md`)             | 002 introduces an input-handling building block (key capture, DAS/ARR timing, command queue) sitting between the browser and 001's game-domain block. *Currently empty.*        |
| 06. Runtime View (`06-runtime-view.md`)                           | 002 defines the per-update-step sequence: drain input queue → apply commands in order → apply gravity or soft drop → recompute grounded → advance lock timer (DR-43). *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)       | Needs the project-wide input model — event capture, default-action suppression (DR-42), focus-loss handling (EC-7) — since stories 003, 005 and 009 all take keyboard input. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`)       | 002 warrants ADRs for: adopting SRS over a simpler rotation system, the top-down kick-table conversion (§5.0), game-timed DAS/ARR over OS key repeat, and the client/server split forced by NFR-001. *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)           | Should host the input-latency quality scenario generalising NFR-001 to all interactive stories. §6 here only states what is specific to piece control. *Currently empty.*       |
| 12. Glossary (`12-glossary.md`)                                   | Should gain: SRS, wall kick, floor kick, kick test, DAS, ARR, soft drop, T-spin, orientation cycle — alongside the terms spec 001 contributes. *Currently empty.*               |

## 10. Open Questions

| #   | Question                                                                                                      | Owner      | Status   | Resolution                                                                                                                                                                                                                                      |
| --- | --------------------------------------------------------------------------------------------------------------- | ---------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Should 180° rotation be offered, as many modern clients do?                                                     | Daan Harms | Resolved | **No.** It is not in the Tetris Guideline, and the standing project decision resolves every unspecified detail to the Guideline. It is an explicit Non-Goal (§1.3, DR-33). Adding it later would need its own kick table and a free key.          |
| 2   | Does NFR-001's 50 ms p95 latency budget force the game loop into the browser?                                   | Daan Harms | Resolved | **In practice yes, but it stays an architecture decision, not a behavioural one.** Nothing in §3–§5 changes either way. Recorded in §9.3 (arc42 §04 and §09) as an input to the ADR spec 001 §10 q1 already raised; this spec adds the latency evidence. |
| 3   | Should the lock delay reset trigger include soft drop, so a player can tap `↓` to buy time?                     | Daan Harms | Resolved | **No.** DR-37 limits the trigger to a successful move or rotation, per the Guideline. Soft drop only accelerates gravity; letting it reset the delay would let a grounded piece be held indefinitely within the 15-reset budget for no skill.     |
| 4   | Should `←` and `→` held together cancel out, as some older implementations do?                                  | Daan Harms | Resolved | **No.** Last-pressed wins (DR-32), the Guideline behaviour. Cancelling to no movement is a well-known source of "my piece froze" complaints during fast play.                                                                                     |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-006, SC-035; US-002 -> SC-007..SC-012;
        US-003 -> SC-013..SC-018, SC-036; US-004 -> SC-019..SC-023;
        US-005 -> SC-024..SC-027; US-006 -> SC-028..SC-034
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-005, FR-030 -> US-001; FR-006..FR-009 -> US-002;
         FR-010..FR-014, FR-031 -> US-003; FR-015..FR-018 -> US-004;
         FR-019..FR-024 -> US-005; FR-025..FR-029 -> US-006)
  - [x] Domain model covers all entities mentioned in the requirements
        Extends spec 001 §5: ActivePiece gains softDropCells and lastKickTest; new entity InputState;
        new value objects OrientationShape, RotationDirection, RotationTransition, KickOffset,
        KickTableRow, KickTable, KeyBinding, InputTiming; new enums HorizontalDirection,
        KickTableKind, InputCommand. Spec 001's types are referenced, never redefined.
  - [x] Domain rules and invariants are listed (DR-28 .. DR-43, continuing spec 001's DR-01 .. DR-27)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-18)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-009)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all four resolved)
  - [x] Both tables spec 001 deferred to this spec are delivered in full:
        28 OrientationShape entries (§5.3) and 56 kick-table rows across the JLSTZ and I tables (§5.3),
        both stated in spec 001 §5.0's top-down row convention with the sign conversion made explicit.
-->
