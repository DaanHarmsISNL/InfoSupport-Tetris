# Feature Specification: Hold a Piece — Stash, Swap and the Once-Per-Piece Lock

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
| Feature ID      | 005                                                                                           |
| Status          | Draft                                                                                         |
| Author          | Daan Harms                                                                                    |
| Created         | 2026-10-20                                                                                    |
| Last updated    | 2026-10-20                                                                                    |
| Epic / Parent   | Play Tetris (story map activity) — `docs/product/story-map.md`                                |
| Arc42 reference | Sections 1, 3, 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders. |

> **Note on this spec.** 005 builds directly on **spec 001 — Start a New Game**, which is the normative
> vocabulary for the whole *Play Tetris* activity. Entity names, enum members and domain rule IDs from
> 001 are reused verbatim here and are **not** redefined. 001 §5.1 states that "story 005 adds a hold slot
> to this entity" (`GameSession`); §5.1 below is that extension and the only change this spec makes to the
> 001 domain model. New domain rules introduced here are numbered from **DR-63** so they cannot collide
> with 001's DR-01…DR-27 or with rules added by sibling specs.
>
> **Ruleset.** Every detail not otherwise constrained by the story map is resolved against the modern
> **Tetris Guideline**, per the standing project decision recorded in 001.

### 1.1 Problem Statement

Today a player is forced to place whatever the queue hands them. When an awkward S or Z arrives on top of
a tidy stack, or the I piece needed to clear a four-row well is still three pieces away, the player has no
way to defer the problem — they must ruin their stack or wait and hope. Every real Tetris implementation
solves this with a hold slot, and without one the game feels thin and the next-piece preview (story 004)
is much less useful, because seeing a problem coming does not help if you cannot do anything about it.

### 1.2 Goal

The player can press one key to stash the falling piece for later. The first time they do it the stashed
piece is set aside and the next piece from the queue takes its place; from then on, pressing hold swaps the
falling piece with the stashed one. The held piece is always visible beside the playfield so the player can
plan around it. Hold is allowed once per piece — enough to rescue a bad situation, not enough to shuffle
pieces indefinitely — and the player can always tell at a glance whether it is currently available.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec. Each is either a separate story or a
project-wide exclusion. Do not implement them here, and do not design around a guess at how they
will work — the owning spec defines that.

- **Moving, rotating and soft-dropping the falling piece** — **story 002**. This spec defines one new key
  binding and nothing else about input handling; 002 owns the input model, key repeat policy for movement,
  and the SRS rotation tables.
- **Hard drop** — **story 003**. Hard drop locks a piece, which clears the hold flag per DR-69, but the
  drop itself is 003's concern.
- **The next-piece preview and its rendering** — **story 004**. This spec specifies *whether the queue
  advances* on a hold (DR-65, DR-66) because 004's preview depends on exactly that distinction, but it
  renders no preview itself.
- **Line clearing and scoring** — **story 006**. Using hold neither scores nor penalises, and a hold never
  triggers a line-clear check because nothing locks (DR-70).
- **Level progression and the gravity curve** — **story 007**. A swapped-in piece simply uses the session's
  current `gravityInterval`.
- **Game over presentation** — **story 008**. This spec defines a new *path into* block out (DR-71) but not
  what the player is shown when the session ends.
- **Leaderboard, name entry, score submission** — stories 009–011.
- **Info Support branding, typography, exact colours and panel layout** — **story 013**. This spec fixes the
  hold area's *information content*, *fixed sizing* and *available/unavailable distinction*; 013 owns where
  it sits on the page and what it looks like.
- **The controls / how-to-play panel** — **story 014**. The hold binding is defined here so 014 has
  something to document; the panel itself is 014's.
- **Ghost piece**, **pause**, **accounts/registration/login**, **mobile and touch support** — out of scope
  project-wide per the story map.
- **Holding more than one piece, a hold queue, or a hold that preserves rotation** — not Guideline
  behaviour and explicitly excluded (DR-64, DR-67).

## 2. User Stories

### US-001: Stash an awkward piece

**As a** player,
**I want** to set the current piece aside instead of placing it,
**so that** a piece that would ruin my stack can wait until I have somewhere good to put it.

### US-002: Bring the stashed piece back

**As a** player,
**I want** to swap the falling piece with the one I stashed earlier,
**so that** the I piece I saved for my well arrives exactly when I need it.

### US-003: See what I am holding and whether I can use it

**As a** player,
**I want** the held piece shown beside the board, clearly marked when hold is unavailable,
**so that** I can plan around it without guessing and without pressing a key that does nothing.

### US-004: Hold stays a decision, not a loophole

**As a** player,
**I want** hold limited to once per piece,
**so that** I cannot stall indefinitely by swapping back and forth and the game keeps the challenge I came for.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) are defined in §5.5;
rules DR-01…DR-27 are defined in spec 001 and are referenced, not restated._

| ID     | Requirement                                                                                                                                                                                                                                   | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall trigger a hold when the player presses `C` or either `Shift` key, on the key-down edge only, ignoring operating-system auto-repeat while the key remains held (DR-63).                                                          | Must     | US-001     |
| FR-002 | The system shall, when a hold is accepted and the hold slot is **empty**, place the `ActivePiece`'s `type` into the hold slot, draw the next `TetrominoType` from the front of `PieceQueue.upcoming`, and spawn it (DR-65).                      | Must     | US-001     |
| FR-003 | The system shall, when a hold is accepted and the hold slot is **occupied**, exchange the `ActivePiece`'s `type` with the held type and spawn the previously held type, **drawing nothing from the `PieceQueue`** (DR-66).                       | Must     | US-002     |
| FR-004 | The system shall leave `PieceQueue.upcoming` completely unchanged — same types, same order — on an occupied-slot hold, so story 004's preview does not advance (DR-66).                                                                          | Must     | US-002     |
| FR-005 | The system shall refill the `PieceQueue` to at least seven upcoming types after an empty-slot hold draws from it, per **Queue Refill Threshold (DR-13)**.                                                                                        | Must     | US-001     |
| FR-006 | The system shall spawn the incoming piece as a genuinely new `ActivePiece` at the standard placement and `Orientation.Spawn`, per **Spawn Placement (DR-06)** and **Spawn Orientation (DR-07)**, regardless of where or how the outgoing piece sat (DR-67). | Must     | US-002     |
| FR-007 | The system shall give the incoming piece `fallTimerElapsed` of 0, no running lock timer, `lockResetsUsed` of 0 and a `lowestRowReached` derived from its own spawn cells — a full fresh 15-reset budget per **Lock Reset Budget (DR-17)** (DR-67). | Must     | US-002     |
| FR-008 | The system shall apply the **Initial Drop (DR-08)** to the incoming piece exactly as it does to a piece spawned after a lock, stepping it down one cell if that placement is valid (DR-67).                                                      | Must     | US-002     |
| FR-009 | The system shall discard the outgoing piece's position, orientation, fall timer, lock timer and reset budget without writing any of its cells to the `Playfield` — the stack is unchanged by a hold (DR-64, DR-70).                              | Must     | US-001     |
| FR-010 | The system shall increment `piecesSpawned` for the incoming piece, as for any other spawn (DR-67).                                                                                                                                               | Should   | US-002     |
| FR-011 | The system shall permit at most one accepted hold per piece, recording that hold has been used for the current piece (DR-68).                                                                                                                    | Must     | US-004     |
| FR-012 | The system shall clear the hold-used record **when a piece locks** per **Locking (DR-19)**, and at no other time — in particular not when a piece spawns (DR-69).                                                                                | Must     | US-004     |
| FR-013 | The system shall ignore a hold press that is not currently permitted, producing no state change, no queue draw, no score or level change and no visible or audible penalty (DR-70).                                                              | Must     | US-004     |
| FR-014 | The system shall resolve an accepted hold before fall and lock timers advance within the same update step, so a hold pressed in the same step as an expiring lock delay takes precedence and the outgoing piece does not lock (DR-72).           | Must     | US-001     |
| FR-015 | The system shall detect **Block Out (DR-21)** when the incoming piece's spawn cells are already occupied by the stack, leave the piece unplaced and move the session to `GameStatus.GameOver` (DR-71).                                           | Must     | US-002     |
| FR-016 | The system shall display the held `TetrominoType` beside the playfield in `Orientation.Spawn`, drawn in that type's **Colour Mapping (DR-24)** colour (DR-73).                                                                                    | Must     | US-003     |
| FR-017 | The system shall reserve a fixed area for the hold display — 4 cells wide by 2 cells tall, the largest spawn-orientation footprint — whose size does not change as the slot fills, empties or swaps (DR-73).                                      | Must     | US-003     |
| FR-018 | The system shall render the hold area in a visually distinct *unavailable* state whenever a hold press would be ignored, so the player can see at a glance that hold is spent for the current piece (DR-73).                                      | Must     | US-003     |
| FR-019 | The system shall start every `GameSession` with an empty hold slot and hold unused, extending **Fresh Session State (DR-23)**, and shall carry no hold state across sessions (DR-74).                                                             | Must     | US-003     |
| FR-020 | The system shall freeze the hold slot and its display when the session reaches `GameStatus.GameOver`, per **Terminal Status (DR-27)** (DR-74).                                                                                                    | Should   | US-003     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash._

### SC-001: The hold key is bound to C and Shift (FR-001)

```gherkin
Given a game session is in progress with a falling piece
  And the hold slot is empty
When the player presses C
Then the piece is held
Given a later piece is falling with hold available again
When the player presses either Shift key
Then that piece is held in the same way
```

### SC-002: Holding the key down does not hold twice (FR-001)

```gherkin
Given a game session is in progress with a falling piece
  And the hold slot is empty
When the player presses and keeps holding the C key for five seconds
  And the piece that came in locks during that time
Then exactly one hold has been performed
  And the piece that spawned after the lock was not also held
```

### SC-003: The first hold stashes the piece and the queue advances (FR-002, FR-005)

```gherkin
Given a game session is in progress with an empty hold slot
  And the falling piece is a Z tetromino
  And the next type in the piece queue is an L tetromino
When the player presses hold
Then the hold slot contains Z
  And an L tetromino is now the falling piece
  And L has been removed from the front of the piece queue
  And the piece queue still holds at least seven upcoming types
```

### SC-004: A swap takes nothing from the queue (FR-003, FR-004)

```gherkin
Given the hold slot contains an I tetromino
  And the falling piece is an S tetromino
  And hold has not yet been used for the S
  And the next three types in the piece queue are T, O and J
When the player presses hold
Then an I tetromino is now the falling piece
  And the hold slot contains S
  And the next three types in the piece queue are still T, O and J in that order
```

### SC-005: The preview is untouched by a swap (FR-004)

```gherkin
Given the hold slot is occupied
  And the player can see the upcoming pieces
When the player swaps the falling piece with the held piece
Then the upcoming pieces shown are exactly the ones shown before the swap
  And they are in the same order
```

### SC-006: The swapped-in piece starts at the standard spawn placement (FR-006)

```gherkin
Given the hold slot contains a J tetromino that was held from row 35 in the Left orientation
  And the falling piece is a T tetromino resting against the right wall in the Inverted orientation
When the player presses hold
Then the J tetromino appears in the Spawn orientation
  And it occupies the same cells any freshly spawned J would occupy
  And it carries nothing of the position or orientation it had when it was held
```

### SC-007: The swapped-in piece gets fresh timers and a full reset budget (FR-007)

```gherkin
Given the falling piece has been in play for several seconds and has used 12 of its 15 lock delay resets
When the player presses hold
Then the incoming piece has no accumulated fall time
  And it has no running lock delay
  And it has a full budget of 15 lock delay resets
```

### SC-008: The swapped-in piece takes the free initial step (FR-008)

```gherkin
Given a game session with an empty playfield and an occupied hold slot
When the player presses hold
Then the incoming piece steps down one cell immediately, without waiting for a gravity interval
  And at least one of its cells is in the top visible row
  And this behaves identically to a piece spawning after a lock
```

### SC-009: Holding leaves the stack alone (FR-009)

```gherkin
Given a game session with a partially built stack
  And a falling piece resting directly on that stack with its lock delay running
When the player presses hold
Then no cell of the outgoing piece is written to the playfield
  And the stack is exactly as it was before the hold
  And the outgoing piece does not lock
  And no line clear is evaluated
```

### SC-010: Hold cannot be used twice on the same piece (FR-011, FR-013)

```gherkin
Given the falling piece has already been swapped in by a hold
When the player presses hold again
Then nothing happens
  And the falling piece is unchanged
  And the hold slot is unchanged
  And the piece queue is unchanged
  And no penalty is applied to the score or the level
```

### SC-011: Hold becomes available again only after a lock (FR-012)

```gherkin
Given the player has used hold for the current piece
When that piece locks into the stack
  And the next piece spawns
Then hold is available for the newly spawned piece
```

### SC-012: Spawning alone does not restore hold (FR-012)

```gherkin
Given the hold slot is occupied
  And the player presses hold, which spawns the previously held piece
When the player immediately presses hold again on that newly spawned piece
Then nothing happens
  And the player cannot swap again until a piece has locked
```

### SC-013: Hold cannot be used to stall indefinitely (FR-011, US-004)

```gherkin
Given a game session is in progress
When the player presses hold repeatedly and rapidly without letting any piece lock
Then at most one swap occurs
  And gravity continues to act on the falling piece throughout
  And the game cannot be frozen by hold input
```

### SC-014: A hold beats an expiring lock delay in the same step (FR-014)

```gherkin
Given a grounded piece whose 0.500 second lock delay is about to expire
When the player presses hold in the same update step in which the delay would expire
Then the hold is resolved first
  And the outgoing piece is removed without locking
  And the stack does not gain its four cells
```

### SC-015: A hold swap can end the game (FR-015)

```gherkin
Given the stack has grown into the buffer rows
  And cells required by the held type's spawn placement are occupied
When the player presses hold
Then the held piece is not placed
  And there is no active piece
  And the session moves to status GameOver
```

### SC-016: The held piece is shown in spawn orientation and its own colour (FR-016)

```gherkin
Given the hold slot contains an S tetromino
When the player looks at the hold area
Then an S tetromino is drawn in the Spawn orientation
  And it is drawn in the S colour (green)
  And it matches the shape an S shows when it spawns on the playfield
```

### SC-017: The hold area never changes size (FR-017)

```gherkin
Given a game session has just started with an empty hold slot
When the player holds an O tetromino, then later swaps it for an I tetromino
Then the hold area occupies the same space throughout
  And the rest of the page does not shift or reflow at any point
```

### SC-018: The hold area shows when hold is unavailable (FR-018)

```gherkin
Given the hold slot contains a T tetromino
  And hold has already been used for the current piece
When the player looks at the hold area
Then the held T is shown in a visibly dimmed, unavailable state
When the current piece locks
Then the held T is shown in its normal, available state again
```

### SC-019: A new game starts with nothing held (FR-019)

```gherkin
Given a session in which the player has held an L tetromino
When the player starts a new game
Then the hold slot is empty
  And the hold area shows no piece
  And hold is available for the first piece of the new session
```

### SC-020: The hold slot freezes at game over (FR-020)

```gherkin
Given the hold slot contains a Z tetromino
When the session reaches game over
Then the hold area still shows the Z
  And pressing hold does nothing
  And no piece spawns
```

## 5. Domain Model

_This spec adds to the normative model in **spec 001 §5**. Everything not restated below is unchanged and
is used with exactly the meaning 001 gives it: `Playfield`, `ActivePiece`, `PieceQueue`, `SevenBag`,
`Cell`, `CellPosition`, `TetrominoDefinition`, `SpawnPlacement`, `TetrominoType`, `Orientation`,
`GameStatus`, and the coordinate system in 001 §5.0._

### 5.1 Entities

#### GameSession *(extended)*

001 §5.1 reserved this extension: "Story 005 adds a hold slot to this entity." Two attributes are added;
no existing attribute changes meaning, type or constraint.

| Attribute          | Type            | Constraints                                      | Description                                                                                                       |
| ------------------ | --------------- | ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| heldType           | TetrominoType?  | nullable, null on a fresh session (DR-74)        | The single stashed tetromino **type**. Never a positioned piece — no position, orientation or timer is stored (DR-64). |
| holdUsedThisPiece  | boolean         | required, `false` on a fresh session (DR-74)     | Whether hold has already been accepted for the current `ActivePiece`. Set on an accepted hold (DR-68); cleared only when a piece locks (DR-69). |

`holdAvailable` is **derived, never stored**: true exactly when `status` is `Playing`, `activePiece` is
non-null, and `holdUsedThisPiece` is false. It is the single condition FR-013 and FR-018 both read, so the
key's behaviour and the display's appearance can never disagree.

**No new entity is introduced.** The hold slot is deliberately two fields on the aggregate root rather than
an object of its own: it holds at most one value, it has no identity and no lifecycle beyond the session's.

#### ActivePiece *(unchanged, note on provenance)*

`ActivePiece` is unchanged. Worth stating explicitly, because it is the subtle part of this feature: a piece
that arrives via a hold swap is **indistinguishable** from a piece that arrives via a post-lock spawn. It
carries no flag recording that it came from the hold slot, and nothing in the model lets the two be told
apart. The once-per-piece limit lives on the *session* (`holdUsedThisPiece`), not on the piece — which is
precisely why it survives the piece being replaced, and why only a **lock** clears it (DR-69).

### 5.2 Relationships

Additions to 001 §5.2:

- A **GameSession** has **at most one held TetrominoType** — zero before the first hold of the session, and
  exactly one from the first accepted hold until game over.
- The held value is a **TetrominoType**, not an **ActivePiece** and not a **TetrominoDefinition** instance.
  The hold display resolves it to shape and colour through the immutable **TetrominoDefinition** reference
  data, the same way the playfield resolves a locked `Cell`'s `occupant`.
- A hold **never** relates the held type to the **Playfield**: no cell is written, read or cleared by a hold.
- An accepted hold relates to the **PieceQueue** in exactly one of two ways, and the distinction is the
  whole point of this feature: an empty-slot hold **draws one type** from it (DR-65); an occupied-slot hold
  **does not touch it at all** (DR-66).

### 5.3 Value Objects

No new value objects. The hold slot reuses `TetrominoType` (an enum member, carried by value) and the hold
display reuses `TetrominoDefinition.spawnOffsets` and `TetrominoDefinition.colour` from 001 §5.3 — the same
reference data the playfield and story 004's preview read. The fixed hold area of FR-017 is a presentation
dimension, not a domain value object.

### 5.4 Enumerations

No new enumerations and no new members on `TetrominoType`, `Orientation` or `GameStatus`. In particular,
**no new `GameStatus` member is added for "hold used"** — availability is derived state (§5.1), not a
lifecycle state, and the session's state machine in 001 §5.1 is unchanged by this spec.

### 5.5 Domain Rules and Invariants

_New rules are numbered from **DR-63**. Rules DR-01…DR-27 belong to spec 001 and are cited here, never
restated. Later specs should cite these by ID and name, e.g. "per **Hold Once Per Piece (DR-68)**"._

- **DR-63 — Hold Binding**: The hold action is bound to the `C` key and to **either** `Shift` key. It
  triggers on the **key-down edge only**: operating-system auto-repeat while the key remains depressed
  produces exactly one hold, never a stream of them. No other key is bound to hold, and hold shares no key
  with story 002's movement, rotation and soft-drop bindings or story 003's hard drop.
- **DR-64 — Hold Slot Contents**: The hold slot holds **at most one `TetrominoType`** and nothing else. It
  never holds an `ActivePiece`, a position, an orientation, a timer, a reset budget or a `lowestRowReached`.
  All of that state belongs to the outgoing piece and is discarded when the piece is held. There is no
  second slot and no hold queue.
- **DR-65 — Hold Into An Empty Slot**: When a hold is accepted and `heldType` is null, the `ActivePiece`'s
  `type` is written to `heldType`, the active piece is discarded without locking, **one type is drawn from
  the front of `PieceQueue.upcoming`** exactly as a post-lock spawn would draw it, and that type is spawned
  per DR-67. The queue then refills per **Queue Refill Threshold (DR-13)**. The queue has advanced by one,
  so story 004's preview advances.
- **DR-66 — Hold Swap With An Occupied Slot**: When a hold is accepted and `heldType` is non-null, the
  `ActivePiece`'s `type` and `heldType` are **exchanged**: the outgoing type becomes `heldType`, and the
  previously held type is spawned per DR-67. **Nothing is drawn from the `PieceQueue`**, no bag is consumed
  and no refill is triggered; `PieceQueue.upcoming` is byte-for-byte identical before and after. Story 004's
  preview therefore does **not** change. This asymmetry between DR-65 and DR-66 is normative and is the most
  commonly mis-implemented part of hold.
- **DR-67 — The Incoming Piece Is Freshly Spawned**: In both DR-65 and DR-66 the incoming type produces a
  **genuinely new `ActivePiece`**, not the old one relocated or the held one restored. It enters at the
  standard **Spawn Placement (DR-06)** for its type — the cells listed in 001 §5.3's `SpawnPlacement`
  table — in **`Orientation.Spawn` (DR-07)**, with `fallTimerElapsed` 0, `lockTimerElapsed` null,
  `lockResetsUsed` 0 and `lowestRowReached` set from its own spawn cells, giving it the full 15-reset budget
  of **Lock Reset Budget (DR-17)**. The **Initial Drop (DR-08)** applies: the piece attempts exactly one free
  one-cell descent and takes it if the result is a valid placement. This is deliberate — a hold-spawned piece
  is identical in every respect to a lock-spawned piece, so there is exactly one spawn behaviour in the
  system. `piecesSpawned` increments. A piece that is swapped *out* and later swapped back *in* keeps nothing
  from its earlier life: it is a new piece that happens to share a type.
- **DR-68 — Hold Once Per Piece**: Hold may be accepted at most once per piece. Accepting a hold sets
  `holdUsedThisPiece` to true. A hold is accepted only when `status` is `Playing`, `activePiece` is non-null
  and `holdUsedThisPiece` is false.
- **DR-69 — Hold Availability Is Cleared By A Lock, Not A Spawn**: `holdUsedThisPiece` is set back to false
  when a piece **locks** per **Locking (DR-19)** — including a lock reached by story 003's hard drop or by
  story 002's input — and at no other moment. It is explicitly **not** cleared when a piece spawns. This is
  the whole mechanism preventing infinite hold-swapping: the piece that arrives from a swap inherits a
  `holdUsedThisPiece` that is already true, so it cannot be held in turn until it has committed to the stack.
  A piece that leaves play via a hold never locks, so it never clears the flag.
- **DR-70 — Ignored Hold Input**: A hold press that is not accepted under DR-68 is **silently ignored**. No
  state changes at all: no queue draw, no bag consumption, no change to `heldType` or the active piece, no
  change to the playfield, score, level or any timer, no lock-delay reset consumed from the DR-17 budget, and
  no penalty, error, sound or message. The press is simply not an action. Likewise, a hold never writes cells
  to the playfield, so it never triggers story 006's line-clear evaluation.
- **DR-71 — Hold Block Out**: If any cell required by the incoming piece's spawn placement (DR-06) is already
  occupied by the stack, **Block Out (DR-21)** applies in full: the piece is not placed, `activePiece` becomes
  null and the session moves to `GameStatus.GameOver`. The hold swap itself is **already committed** when this
  happens — the outgoing type is in `heldType`, and in the DR-65 case the queue has already advanced — so the
  frozen session story 008 presents shows the outgoing type in the hold slot. Note that only the **stack** can
  cause this: the outgoing piece's cells were never written, so a hold can never block itself out. It can still
  block out because spawn footprints differ by type (001 §5.3), so an incoming piece may need a cell the
  outgoing piece did not.
- **DR-72 — Hold Resolves Before Gravity In An Update Step**: Within a single update step, a pending hold is
  resolved **before** fall and lock timers advance. A hold pressed in the same step in which the outgoing
  piece's lock delay would expire therefore takes precedence, and that piece does not lock. The hold itself
  consumes no game time: it neither advances nor resets the session clock, and it is subject to the same
  elapsed-time model as everything else (**DR-26**).
- **DR-73 — Hold Display**: The held type is rendered beside the playfield, in `Orientation.Spawn` using the
  `spawnOffsets` of its `TetrominoDefinition`, filled with that type's **Colour Mapping (DR-24)** colour — the
  same colour it will have when it locks. The area is a **fixed 4 cells wide by 2 cells tall**, the largest
  spawn-orientation footprint (the I piece), so it never resizes between empty, occupied and swapped states and
  never reflows the page. An empty slot renders the empty area, not a collapsed one. Whenever `holdAvailable`
  is false the area is rendered in a clearly distinguishable **unavailable** state — dimmed to roughly 40%
  opacity plus a non-colour cue per NFR-003. The area carries a short `HOLD` label; the key hint belongs to
  story 014's controls panel and is not duplicated here. Exact placement, framing and the final visual
  treatment are **story 013's**.
- **DR-74 — Hold Slot Lifecycle**: A new `GameSession` starts with `heldType` null and `holdUsedThisPiece`
  false, extending **Fresh Session State (DR-23)**. Nothing carries over between sessions, and the *New game*
  action (001 FR-005) discards the hold slot with the rest of the session. Once the session is `GameOver`, the
  hold slot is frozen along with everything else per **Terminal Status (DR-27)**: no hold is accepted and the
  display keeps showing whatever was held.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty),
and 001's NFR-001…NFR-008 continue to apply to the core loop._

| ID      | Category     | Requirement                                                                                                                                                                                             |
| ------- | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance  | A hold is reflected on screen — both the new falling piece and the updated hold area — within **one rendered frame and no more than 50 ms** of the key press, so the swap feels instantaneous.              |
| NFR-002 | Performance  | A hold performs no allocation or work proportional to the playfield: it touches a constant number of fields and at most one queue draw, so it cannot cause a frame drop under NFR-001 of spec 001.          |
| NFR-003 | Usability    | The unavailable state of the hold area is conveyed by **at least two independent cues** (for example reduced opacity *and* a desaturated or outlined rendering), so it is perceivable without relying on colour or opacity alone, and remains distinguishable at the rendered cell size. |
| NFR-004 | Usability    | The held piece is legible as its tetromino type at the rendered size, with the same **≥ 3:1** contrast against its background that 001 NFR-003 requires of playfield cells.                                   |
| NFR-005 | Consistency  | The hold area and story 004's next-piece preview render tetrominoes with the same cell size, orientation convention and colours, so the player reads them the same way.                                      |
| NFR-006 | Testability  | Hold is a deterministic, pure state transition over `GameSession`: given a session state and a hold press, the resulting state is fully determined, so every §4 scenario runs headlessly with injected time and a seeded randomiser (001 NFR-006). |
| NFR-007 | Reliability  | No sequence of hold presses — including rapid repeats, presses during `Ready` or `GameOver`, and presses interleaved with story 002 and 003 input — can produce an invalid placement (DR-03, DR-04), a duplicated or lost queue entry, or more than one `ActivePiece` (DR-25). |
| NFR-008 | Resource use | Hold adds exactly two fields per session and no per-piece allocation that accumulates; a 30-minute session using hold on every piece shows no growth in retained objects beyond 001 NFR-008.                 |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                                        | Expected Behavior                                                                                                                                                                            |
| ----- | ----------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | Hold is pressed on the very first piece of a session, with an empty slot.                                         | Normal empty-slot hold (DR-65): the first piece's type goes to the slot, the second type is drawn and spawned, the preview advances by one.                                                      |
| EC-2  | Hold is pressed a second time on the piece that a hold just brought in.                                           | Ignored (DR-68, DR-70). `holdUsedThisPiece` is still true because only a lock clears it (DR-69). Nothing changes.                                                                                 |
| EC-3  | Hold is pressed while the piece is grounded with its lock delay running.                                          | Accepted if available. The outgoing piece is removed **without locking**; its cells are not written and no line clear is evaluated (DR-70). The incoming piece spawns at the top (DR-67).         |
| EC-4  | Hold is pressed in the same update step in which the lock delay expires.                                          | The hold wins (DR-72): resolved before timers advance, so the outgoing piece does not lock.                                                                                                      |
| EC-5  | Hold is pressed while the piece has used 14 of its 15 lock resets.                                                | The budget is discarded with the outgoing piece. The incoming piece gets a fresh 15 (DR-67, DR-17). Hold is not a reset-budget exploit, because it may be used only once per piece (DR-68).       |
| EC-6  | The incoming piece's spawn cells are occupied by the stack.                                                       | Hold block out (DR-71 → DR-21): piece unplaced, session `GameOver`. The swap is already committed, so the hold slot shows the outgoing type when story 008 presents the frozen session.            |
| EC-7  | The incoming and outgoing types have different spawn footprints (e.g. swapping a T for an O or an I).             | Entirely normal, and the reason EC-6 is reachable at all: O needs (4,18),(5,18) and I needs columns 3–6 of row 19, which a T does not. An overhang in the buffer rows can block one but not the other. |
| EC-8  | Hold is pressed while `status` is `Ready` — the player has not started yet.                                       | Ignored (DR-68, DR-70). No session state changes; no piece spawns.                                                                                                                               |
| EC-9  | Hold is pressed after game over.                                                                                  | Ignored (DR-70, DR-74). The frozen playfield and hold slot are untouched.                                                                                                                        |
| EC-10 | Hold is pressed in the instant between a lock and the next spawn, when `activePiece` is null.                     | Ignored (DR-68) — there is no piece to hold. **Spawn Immediacy (DR-20)** makes this window effectively zero-length, so it should not be reachable in practice; the guard exists so that it is safe if it is. |
| EC-11 | The player keeps `C` or `Shift` depressed across a lock and the next spawn.                                       | Exactly one hold (DR-63). Auto-repeat produces no further holds; a second hold requires releasing and pressing the key again.                                                                     |
| EC-12 | The player presses `C` and `Shift` together, or both within the same update step.                                 | One hold. The second press finds `holdUsedThisPiece` already true and is ignored (DR-68, DR-70).                                                                                                  |
| EC-13 | `Shift` is pressed as a modifier for a browser or OS shortcut (e.g. `Shift`+a movement key).                      | The `Shift` key-down still triggers a hold if one is available — the binding is on the key itself, not on a modifier-free chord. Story 002 must therefore not bind any `Shift`-modified combination. |
| EC-14 | An empty-slot hold leaves the queue below seven upcoming types.                                                   | A new `SevenBag` is generated and appended before the next read (DR-13). A draw never finds an empty queue — this is 001's EC-14 reached by a new route.                                           |
| EC-15 | The player uses hold on every single piece for a whole session.                                                   | Legal and supported. Each piece is held once, the stack still grows because every swap brings in a piece that must eventually be placed, and the game still ends by block out or lock out.         |
| EC-16 | Two sessions are run with the same randomiser seed and the same hold presses.                                     | Identical play. Hold draws from the queue only in the DR-65 case, and deterministically, so seeded reproducibility (001 FR-012, NFR-006) is preserved.                                             |
| EC-17 | The browser tab is backgrounded with a hold press queued.                                                         | The press is resolved in the next update step under the DR-26 clamp, before timers advance (DR-72). No burst of holds, and at most one hold per piece regardless (DR-68).                          |
| EC-18 | The window is resized or zoomed while a piece is held.                                                            | The hold area rescales proportionally and keeps its fixed 4×2 cell footprint (DR-73). The held type, availability and all session state are unaffected.                                            |
| EC-19 | The hold slot is empty and hold is unavailable (only reachable in `Ready` or `GameOver`).                         | The empty hold area is rendered in the unavailable state — the frame and label dim, with no piece drawn (DR-73).                                                                                   |
| EC-20 | Story 003's hard drop is used on a piece that was swapped in by a hold.                                           | The piece locks normally, which clears `holdUsedThisPiece` (DR-69), making hold available for the next piece. Hold and hard drop do not otherwise interact.                                        |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                              |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| SUC-001 | All 20 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                          |
| SUC-002 | All 12 domain rules in §5.5 (DR-63 … DR-74) are each covered by at least one automated test.                                                                              |
| SUC-003 | A property test over a seeded 1,000-piece session with hold pressed at random moments shows: the multiset of types that have entered play equals the queue's drawn sequence plus at most one type resident in the hold slot — no type is ever duplicated or lost. |
| SUC-004 | An automated test asserts that an occupied-slot hold leaves `PieceQueue.upcoming` identical, and that an empty-slot hold advances it by exactly one — the DR-65/DR-66 distinction story 004 depends on. |
| SUC-005 | An automated test asserts that no sequence of hold presses produces two holds between consecutive locks, for at least 1,000 pieces.                                       |
| SUC-006 | A hold is visible on screen within 50 ms of the key press in manual observation, and the hold area never changes size or reflows the page during a full session.          |
| SUC-007 | A player unfamiliar with the game can tell, without being told, whether hold is currently available, in an informal test with five colleagues.                            |
| SUC-008 | Zero critical or high severity defects attributable to hold after one week of informal office play — specifically no lost pieces, no duplicated pieces and no stuck board. |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 001 — Start a New Game** (hard dependency). 005 extends `GameSession` as 001 §5.1 anticipated, and
  reuses DR-06, DR-07, DR-08, DR-13, DR-17, DR-19, DR-20, DR-21, DR-23, DR-24, DR-25, DR-26 and DR-27 without
  restating them. 005 cannot be implemented or tested before 001's domain model exists.
- **Spec 002 — Move, rotate and soft-drop** (input model). 005 adds one binding to whatever input handling 002
  establishes. 002 must not bind `C` or any `Shift`-modified combination (EC-13). 005 is otherwise independent
  of 002 and can be specified and built in parallel.
- **Spec 004 — Next-piece preview** (consumer). 004 renders `PieceQueue.upcoming` and must reflect the DR-65 /
  DR-66 asymmetry: the preview advances on an empty-slot hold and does not move on a swap. 004 also sets the
  rendering conventions the hold area matches under NFR-005.
- **Spec 003 — Hard drop** (interaction only). A hard-dropped piece locks, which clears `holdUsedThisPiece`
  per DR-69. No other coupling.
- **Spec 006 — Line clearing** (negative dependency). A hold must *not* trigger the line-clear step, because
  nothing locks (DR-70). 006 inserts itself between DR-19 and DR-20 only.
- **Spec 008 — Game over** (consumer). 008 presents the frozen session reached by a hold block out (DR-71),
  including the hold slot contents.
- **Spec 013 — Branding** owns the hold area's placement and final visual treatment; **spec 014 — Controls
  panel** owns documenting the `C` / `Shift` binding to the player. Both depend on DR-63 and DR-73 being settled.

### 9.2 Constraints

- **Platform**: desktop browser on a laptop with a keyboard (story map). The `C` and `Shift` bindings assume a
  physical keyboard; no touch or gamepad equivalent is specified.
- **Keyboard layout**: the binding is on the physical `C` position and the `Shift` keys. On layouts where `C`
  sits elsewhere, the key that produces `c` is used; rebinding is not offered.
- **No pause** (story map) — hold is not a stalling mechanism and must not become one; DR-68 is what enforces
  that, and it is the reason this spec treats the once-per-piece limit as a Must rather than a tuning knob.
- **Guideline fidelity**: hold behaviour — the binding, the empty/occupied asymmetry, the fresh spawn, the
  once-per-piece lock, and clearing on lock rather than on spawn — is standard modern Tetris and must not be
  "improved". Players arrive with muscle memory from other implementations.
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed here): ASP.NET Core 10
  Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`. No test project
  exists yet; SUC-001 and SUC-002 require one.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*, exactly as spec 001 §9.3 does. Implementing 001 remains the natural
> trigger for filling them in, per `CLAUDE.md`.

| Arc42 Section                                                  | Relevance to This Feature                                                                                                                                                             |
| ---------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01. Introduction and Goals (`01-introduction-and-goals.md`)      | Should record "a proper game of Tetris" as a quality driver; hold is one of the features players expect and notice the absence of. *Currently empty.*                                       |
| 03. System Scope and Context (`03-system-scope-and-context.md`)  | Hold adds a key binding to the browser-page boundary and no external interface, persistence or network call. *Currently empty.*                                                             |
| 05. Building Block View (`05-building-block-view.md`)            | Hold is state and behaviour on the existing game-domain building block introduced by 001, plus one presentation element beside the playfield. It introduces no new component. *Currently empty.* |
| 06. Runtime View (`06-runtime-view.md`)                          | Adds two runtime paths to 001's central scenario: *hold with empty slot* (stash → draw → spawn) and *hold with occupied slot* (swap → spawn, no draw), plus the hold block-out exit. *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)      | Needs the input-handling concept shared with story 002 — key-down edge versus auto-repeat (DR-63) — and the ordering rule that input resolves before timers within an update step (DR-72). Both are cross-cutting, not hold-specific. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`)      | Warrants an ADR for the decision that a hold-spawned piece reuses the single spawn path including DR-08's initial drop, rather than a special-cased "restore from hold" path. *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)          | Should host the project-wide input-responsiveness scenario that NFR-001 here is an instance of. *Currently empty.*                                                                           |
| 12. Glossary (`12-glossary.md`)                                  | Should gain *hold*, *hold slot*, *swap*, *hold availability* and *hold block out* alongside 001's terms. *Currently empty.*                                                                  |

## 10. Open Questions

| #   | Question                                                                                                  | Owner      | Status   | Resolution                                                                                                                                                                                                                      |
| --- | ----------------------------------------------------------------------------------------------------------- | ---------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Should the hold-used flag be cleared when a piece **locks** or when a piece **spawns**?                      | Daan Harms | Resolved | **On lock (DR-69).** Clearing on spawn would let the player hold the swapped-in piece immediately, swap back, and repeat forever — an infinite stall with no piece ever committing to the stack. Clearing on lock is the Guideline behaviour and is what makes hold a single decision per piece. |
| 2   | Does the swapped-in piece take the DR-08 initial drop, or does it start at the un-dropped spawn position?    | Daan Harms | Resolved | **It takes the initial drop (DR-67).** There is one spawn behaviour in the system and a hold-spawned piece is in no way special. Making it an exception would give the player a subtly different piece depending on how it arrived, and would double the number of spawn paths to test. |
| 3   | Should the held piece retain the orientation it had when it was held?                                        | Daan Harms | Resolved | **No (DR-64, DR-67).** The slot holds a `TetrominoType`, not a piece. Retaining orientation is not Guideline behaviour, would surprise players with muscle memory from other implementations, and would require storing piece state the slot deliberately does not have. |
| 4   | Should using hold cost anything — a score penalty, a lost reset, a brief delay?                              | Daan Harms | Resolved | **No (DR-70).** The once-per-piece limit (DR-68) is the entire cost. A penalty would be invisible to a new player, would couple this spec to story 006's scoring, and would punish exactly the planning the feature exists to enable. |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-003, SC-009, SC-014; US-002 -> SC-004..SC-008, SC-015;
        US-003 -> SC-016..SC-020; US-004 -> SC-010..SC-013
  - [x] Each functional requirement traces to a user story
        (FR-001, FR-002, FR-005, FR-009, FR-014 -> US-001;
         FR-003, FR-004, FR-006..FR-008, FR-010, FR-015 -> US-002;
         FR-016..FR-020 -> US-003; FR-011..FR-013 -> US-004)
  - [x] Domain model covers all entities mentioned in the requirements
        GameSession extended with heldType and holdUsedThisPiece; everything else reused verbatim
        from spec 001 §5 (Playfield, ActivePiece, PieceQueue, Cell, CellPosition, TetrominoDefinition,
        SpawnPlacement, SevenBag, TetrominoType, Orientation, GameStatus). No new entities, value
        objects or enum members introduced.
  - [x] Domain rules and invariants are listed (DR-63 .. DR-74; DR-01..DR-27 cited from spec 001)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-20, including hold block out,
        hold during Ready/GameOver, auto-repeat, and the lock-delay race)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-008)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all four resolved)
-->
