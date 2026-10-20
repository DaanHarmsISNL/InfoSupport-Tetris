# Feature Specification: Next Piece Preview

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

| Field           | Value                                                                                   |
| --------------- | ----------------------------------------------------------------------------------------- |
| Feature ID      | 004                                                                                         |
| Status          | Draft                                                                                       |
| Author          | Daan Harms                                                                                  |
| Created         | 2026-10-20                                                                                  |
| Last updated    | 2026-10-20                                                                                  |
| Epic / Parent   | Play Tetris (story map activity) — `docs/product/story-map.md`                              |
| Arc42 reference | Sections 3, 5, 6, 8, 10, 12 — see §9.3. All arc42 files are currently empty placeholders.   |

> **Note on this spec.** 004 is built entirely on the vocabulary of
> [`001-start-new-game.md`](001-start-new-game.md). Entity names, enum members and domain rule IDs
> defined there are normative and are reused verbatim here rather than restated. In particular this
> spec adds **no state** to `GameSession` and **changes no rule** of 001 — it is a pure read of
> `PieceQueue.upcoming`, which 001 already keeps stocked per **Queue Refill Threshold (DR-13)**.
>
> **Ruleset.** Every detail not otherwise constrained by the story map is resolved against the modern
> **Tetris Guideline**, per the standing project decision recorded in 001.
>
> **Rule numbering.** New domain rules in this spec run from **DR-52**. Specs are being written
> concurrently; the DR-52+ block is reserved for 004 so rule IDs stay globally unique.

### 1.1 Problem Statement

Tetris is a game of planning, not reflexes: a player decides where to put the current piece based on
what is coming after it — keeping a well open for an I, or declining to flatten a row because an S is
next. After 001 the `PieceQueue` exists and is guaranteed to hold at least seven upcoming types, but
nothing is drawn from it, so every piece arrives as a surprise and the player can only ever react.
Without a preview the game is materially harder and noticeably less like real Tetris.

### 1.2 Goal

A fixed panel beside the playfield always shows the single tetromino that will spawn next — correct
from before the very first piece falls, updated at the exact instant a piece spawns, drawn in its
spawn shape and its own colour, in an area that never jumps or resizes. The preview is a guarantee,
not a hint: the piece shown is always the piece that arrives. Reading it never disturbs the piece
sequence by so much as one draw.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec.

- **A multi-piece preview queue (3–6 upcoming pieces).** The story asks for *the next piece*, singular,
  so exactly one is specified here (DR-52). This is the cheapest extension in the whole map to add
  later: **Queue Refill Threshold (DR-13)** already guarantees at least seven upcoming types are
  available at all times, so a longer preview needs no change to the randomiser, the queue or this
  spec's timing rules — only to `PreviewGrid`'s count and layout.
- **Hold / swap mechanics** — **story 005**. This spec fixes only how the preview *responds* to a hold
  (DR-62); 005 owns when a hold is legal, what the hold slot contains and how it is rendered.
- **Any change to the piece sequence, the 7-bag randomiser or the queue** — owned by 001 and explicitly
  protected here by **Preview Read-Only (DR-53)**.
- **Game over presentation** — **story 008**. This spec says only that the preview empties (DR-60);
  what else the player sees at game over belongs to 008.
- **Showing the held piece, the score, the level or the line count** in the same panel — stories 005,
  006 and 007 respectively. They share the side panel; see §9.2.
- **Info Support colours and typography** — **story 013**. The preview uses the **Colour Mapping (DR-24)**
  palette; 013 may re-tune the hex values but must preserve the one-to-one mapping and DR-59.
- **Controls / how-to-play panel** — **story 014**.
- **Ghost piece** — out of scope project-wide (story map: "the user wants the challenge").
- **Pause** — out of scope project-wide (story map: "games are short by design").
- **Accounts, registration, login, persisted sessions** — out of scope project-wide.
- **Mobile and touch support** — out of scope project-wide (laptop + keyboard only).

## 2. User Stories

### US-001: Plan the next placement

**As a** player,
**I want** to see which tetromino is coming next,
**so that** I can place the current piece to set that one up instead of guessing.

### US-002: A preview I can trust

**As a** player,
**I want** the preview to be exactly right and to update the moment the piece it announced starts
falling,
**so that** I can commit to a plan without being betrayed by a stale or wrong preview.

### US-003: Tell the pieces apart without relying on colour

**As a** player who does not distinguish all seven colours reliably,
**I want** the previewed piece to be recognisable by its shape and nameable in text,
**so that** the preview is usable to me at all.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) at or above DR-52 are
defined in §5.4; lower-numbered rules are defined in 001 §5.5._

| ID     | Requirement                                                                                                                                                                                              | Priority | User Story |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall display a *next piece preview* beside the playfield showing exactly one tetromino: the type at the front of `PieceQueue.upcoming` (DR-52).                                                   | Must     | US-001     |
| FR-002 | The system shall render the previewed tetromino in `Orientation.Spawn`, using the shape given by that type's `TetrominoDefinition.spawnOffsets` (DR-56).                                                      | Must     | US-001     |
| FR-003 | The system shall draw the previewed tetromino in its **Colour Mapping (DR-24)** colour — the same colour it will have as the `ActivePiece` and after it locks into the stack.                                 | Must     | US-001     |
| FR-004 | The system shall reserve a fixed preview area of 4 columns × 2 rows of preview cells whose size and screen position are identical for every type and when the preview is empty (DR-57).                       | Must     | US-001     |
| FR-005 | The system shall centre the previewed shape's occupied-cell bounding box horizontally and vertically within that area, so that the 4-wide I and the 2-wide O are both optically centred (DR-58).              | Must     | US-001     |
| FR-006 | The system shall populate the preview from the moment a `GameSession` is constructed — before the first piece has spawned, while the session is still `Ready` — so the player never sees an empty preview during play. | Must     | US-002     |
| FR-007 | The system shall change the previewed type exactly when a type is drawn from the `PieceQueue` — that is, at the instant a piece spawns — with no animation, transition or delay that leaves a stale value on screen (DR-54). | Must     | US-002     |
| FR-008 | The system shall not draw from, reorder, append to, remove from or otherwise mutate the `PieceQueue`, the `SevenBag` or the randomiser state when producing or refreshing the preview (DR-53).                | Must     | US-002     |
| FR-009 | The system shall guarantee that the type shown immediately before a draw is exactly the type that draw yields, for every draw in a session (DR-55).                                                           | Must     | US-002     |
| FR-010 | The system shall leave the preview unchanged when a hold swap exchanges the active piece with an already-occupied hold slot, and shall advance it when a first hold draws from the queue (DR-62).             | Must     | US-002     |
| FR-011 | The system shall empty the preview once the session reaches `GameStatus.GameOver`, while keeping the area reserved at its full size (DR-60, **Terminal Status (DR-27)**).                                     | Must     | US-002     |
| FR-012 | The system shall show the new session's first upcoming type immediately when the player activates *New game*, carrying nothing over from the discarded session (**Fresh Session State (DR-23)**).             | Must     | US-002     |
| FR-013 | The system shall expose a text alternative naming the previewed `TetrominoType`, so the piece is identifiable without seeing its colour or shape (DR-59).                                                     | Must     | US-003     |
| FR-014 | The system shall label the preview area with a persistent caption identifying it as the next piece, visible whether or not the preview is populated.                                                          | Should   | US-003     |
| FR-015 | The system shall ignore all player input directed at the preview; the preview is display-only and no interaction with it alters the `GameSession` (DR-61).                                                    | Should   | US-002     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash._

### SC-001: The preview shows the piece that is coming (FR-001, FR-002, FR-003)

```gherkin
Given a game session is in progress
  And the front of the piece queue is a T tetromino
When the player looks at the next piece preview
Then exactly one tetromino is shown
  And it is a T
  And it is drawn in its spawn orientation, flat side down
  And it is drawn in the T colour (purple)
```

### SC-002: The preview is populated before the first piece ever spawns (FR-006)

```gherkin
Given a game session has just been constructed and is waiting for the start action
  And no piece has spawned yet
When the player looks at the next piece preview
Then it already shows the first upcoming tetromino type
  And when the player activates the start action, that is the type that spawns
```

### SC-003: The preview advances at spawn, not at lock (FR-007)

```gherkin
Given a game session is in progress
  And the preview shows an S tetromino
  And the active piece is grounded with its lock delay running
When the active piece locks into the stack
Then the preview is read at the instant the lock resolves and the next piece spawns
  And the piece that spawns is the S
  And the preview now shows the type after the S
  And the preview was never blank or stale between the lock and the spawn
```

### SC-004: What was previewed is what spawns, every time (FR-009)

```gherkin
Given a seeded game session is played until 200 pieces have spawned
When each spawn is compared with the type the preview showed immediately before it
Then the two match on all 200 spawns
  And the preview was never one piece behind or one piece ahead
```

### SC-005: Rendering the preview does not disturb the sequence (FR-008)

```gherkin
Given two game sessions started with the same randomiser seed
  And one renders the next piece preview while the other does not
When 200 pieces have spawned in each
Then the two sequences of 200 types are identical
  And the queue of the previewing session still holds at least seven upcoming types
```

### SC-006: The preview area does not resize between an I and an O (FR-004, FR-005)

```gherkin
Given a game session is in progress
When the preview shows an I tetromino, then later an O tetromino
Then the preview area occupies the same size and the same screen position in both cases
  And the playfield does not shift or resize when the previewed type changes
  And the area keeps that same size when the preview is empty
```

### SC-007: Every type is centred in the preview area (FR-005)

```gherkin
Given each of the seven tetromino types is previewed in turn
When the rendered shape is measured against the preview area
Then the occupied cells are centred horizontally within the four columns
  And centred vertically within the two rows
  And the one-row-tall I sits on the vertical midline rather than on the top or bottom row
```

### SC-008: Two identical pieces in a row are previewed honestly (FR-009)

```gherkin
Given a bag boundary places the same type at the end of one bag and the start of the next
When the first of the two spawns
Then the preview shows that same type again
  And this is correct behaviour, not a failure to refresh
```

### SC-009: A first hold advances the preview (FR-010)

```gherkin
Given a game session in which the hold slot is empty
  And the preview shows a J tetromino
When the player holds the active piece
Then the J is drawn from the queue and becomes the active piece
  And the preview now shows the type that was behind the J
```

### SC-010: A hold swap with an occupied slot leaves the preview alone (FR-010)

```gherkin
Given a game session in which the hold slot already contains a tetromino
  And the preview shows a Z tetromino
When the player holds the active piece, swapping it with the held one
Then the previously held piece becomes the active piece
  And nothing is drawn from the piece queue
  And the preview still shows the Z
```

### SC-011: The preview empties at game over (FR-011)

```gherkin
Given a game session reaches game over by block out or by lock out
When the player looks at the next piece preview
Then no tetromino is shown
  And the preview area is still reserved at its full size, so the layout does not collapse
```

### SC-012: New game resets the preview (FR-012)

```gherkin
Given a game session is in progress with a partially built stack and a populated preview
When the player activates the new game action
Then the preview immediately shows the first upcoming type of the fresh session
  And no type from the discarded session remains on screen
```

### SC-013: The previewed piece is identifiable without colour (FR-013, FR-014)

```gherkin
Given each of the seven types is previewed in turn
When the preview is viewed with all colour removed
Then each of the seven renders as a visually distinct shape
  And a text alternative naming the type is available for each
  And the preview area carries its caption identifying it as the next piece
```

### SC-014: The preview is display-only (FR-015)

```gherkin
Given a game session is in progress
When the player clicks, taps, drags or presses a key while the preview is the target
Then the previewed type does not change
  And the active piece does not move, rotate or drop
  And the piece queue is unchanged
```

### SC-015: Hard drop advances the preview exactly once (FR-007, FR-009)

```gherkin
Given a game session is in progress
  And the preview shows an L tetromino
When the player hard drops the active piece
Then the piece locks and the L spawns immediately with no entry delay
  And the preview advances exactly one type
  And it does not skip a type or lag a type behind
```

### SC-016: The preview survives a stalled tab (FR-007, FR-009)

```gherkin
Given a game session is in progress and the browser tab is backgrounded
When the tab is brought back to the foreground
Then the preview shows the type currently at the front of the piece queue
  And it matches the piece that spawns next
```

## 5. Domain Model

_This spec introduces no new persisted state. `NextPiecePreview` is a **derived read model** over
`GameSession`; it has no identity, is never stored and never writes. All other names below are
defined in 001 §5 and reused verbatim._

### 5.1 Entities

#### NextPiecePreview

The player-facing projection of the front of the `PieceQueue`. Recomputed from session state whenever
it is read; never cached across a change to the queue or the session status.

| Attribute       | Type            | Constraints                                                                                   | Description                                                                 |
| --------------- | --------------- | ----------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| previewedType   | TetrominoType?  | derived: `pieceQueue.upcoming[0]` while `status` is `Ready` or `Playing`; null when `GameOver`   | The single type announced to the player (DR-52, DR-60).                        |
| orientation     | Orientation     | constant `Spawn` (DR-56)                                                                        | Never changes, regardless of the `ActivePiece`'s orientation.                   |
| colour          | colour          | derived from `previewedType` via **Colour Mapping (DR-24)**; undefined when `previewedType` is null | Identical to the colour the piece will have when active and when locked.    |
| grid            | PreviewGrid     | required, constant 4 × 2 (DR-57)                                                                | The fixed area the shape is drawn into.                                        |
| caption         | string          | constant for the session, non-empty                                                             | Persistent label identifying the area (FR-014).                                |
| accessibleName  | string          | derived, names `previewedType`; names the empty state when `previewedType` is null              | Non-colour, non-shape identification (DR-59).                                  |

`NextPiecePreview` has no mutable attributes of its own. Every value above is a function of
`GameSession.status` and `GameSession.pieceQueue` — which is precisely what makes DR-53 enforceable.

### 5.2 Relationships

- A **GameSession** has exactly one **NextPiecePreview**, derived rather than stored, existing for the
  session's whole lifetime including `Ready` and `GameOver`.
- A **NextPiecePreview** reads the first element of **PieceQueue**`.upcoming` and nothing else — it never
  reads `upcoming[1]` or beyond, never reads `currentBag`, and never writes to either (DR-52, DR-53).
- A **NextPiecePreview**, when populated, refers to exactly one **TetrominoDefinition** — the one matching
  `previewedType` — from which it takes `spawnOffsets`, `spawnWidth` and `colour`.
- A **NextPiecePreview** is composed of exactly eight **preview cells** arranged by its **PreviewGrid**;
  four of them are occupied whenever `previewedType` is non-null (**Tetromino Cell Count (DR-05)**).
- A **NextPiecePreview** has **no** relationship to the **ActivePiece**. The preview is never the piece
  that is currently falling, and it is unaffected by that piece moving, rotating, dropping or locking.

### 5.3 Value Objects

#### PreviewGrid

The fixed render area. Its dimensions are the tightest box that fits every type in `Orientation.Spawn`:
no spawn shape is wider than four cells (I) or taller than two rows (every type's `spawnOffsets` use
`rowOffset` 0 or 1 only). A 4 × 2 grid therefore holds all seven with nothing left over, which is what
lets DR-57 hold without wasted space.

| Attribute   | Type    | Constraints  |
| ----------- | ------- | ------------ |
| columnCount | integer | constant, 4  |
| rowCount    | integer | constant, 2  |

#### Derived preview geometry

The consequence of applying DR-58 to each type's `spawnOffsets`. Stated here so every implementation
agrees on the exact placement — these values are **derived, not independently specified**. Insets are
in preview cells, measured from the top-left of the `PreviewGrid`.

| Type | Occupied extent (w × h) | Horizontal inset | Vertical inset | Note                                                           |
| ---- | ----------------------- | ---------------- | -------------- | ---------------------------------------------------------------- |
| I    | 4 × 1                   | 0                | ½              | Full width; the half-row inset is what stops it hugging an edge.  |
| O    | 2 × 2                   | 1                | 0              | Full height; the whole-cell inset on each side centres it.        |
| T    | 3 × 2                   | ½                | 0              |                                                                   |
| S    | 3 × 2                   | ½                | 0              |                                                                   |
| Z    | 3 × 2                   | ½                | 0              |                                                                   |
| J    | 3 × 2                   | ½                | 0              |                                                                   |
| L    | 3 × 2                   | ½                | 0              |                                                                   |

> **I and O are the two that break naive layouts.** An implementation that simply draws each type's
> 4 × 4 or 2 × 2 rotation bounding box, or that sizes the area to the current shape, will make the
> preview visibly grow and shrink as pieces cycle — and will drop the I onto the grid's top or bottom
> edge rather than its midline. DR-57 and DR-58 exist specifically to rule that out. Note also that the
> **playfield** centring in **Spawn Placement (DR-06)** is left-biased (`floor`), whereas preview
> centring is exact: the preview is a label, not a positional rehearsal, so optical centring wins.

### 5.4 Domain Rules and Invariants

New rules introduced by this spec. Later specs should cite these by ID and name, e.g.
"per **Preview Read-Only (DR-53)**".

- **DR-52 — Preview Source**: The next piece preview shows exactly one `TetrominoType`: the element at
  index 0 of `PieceQueue.upcoming`, the type the next draw will yield. No other element of `upcoming`
  is read, shown or relied upon. **Queue Refill Threshold (DR-13)** guarantees this element always
  exists while the session is `Ready` or `Playing`.
- **DR-53 — Preview Read-Only**: Producing, refreshing or rendering the preview never draws from,
  reorders, appends to, removes from or otherwise mutates the `PieceQueue`, the `SevenBag` or the
  randomiser's state. For a given seed, the sequence of types spawned is bit-for-bit identical whether
  the preview is rendered or not. This is the central correctness guarantee of this spec: the preview
  is a window onto the queue, never a consumer of it.
- **DR-54 — Preview Refresh Point**: The previewed type changes exactly when `PieceQueue.upcoming[0]`
  changes — that is, at the instant a type is drawn from the queue. Within a session the draw points
  are the spawn of each piece (**Spawn Immediacy (DR-20)**) and a first hold (DR-62). The preview does
  **not** change at lock time, on a timer, on a frame boundary, or after any animation or transition.
  Between a lock and the spawn that immediately follows it there is no observable intermediate state.
- **DR-55 — Preview Fidelity**: The type shown by the preview immediately before a draw is exactly the
  type that draw yields. The preview is a guarantee, not a hint, an estimate or a probability. Any
  divergence is a defect, not a tolerance.
- **DR-56 — Preview Presentation**: The previewed tetromino is drawn in `Orientation.Spawn` from that
  type's `TetrominoDefinition.spawnOffsets`, in its **Colour Mapping (DR-24)** colour, with the same
  cell styling used for the playfield. It is drawn in `Spawn` regardless of the `ActivePiece`'s current
  orientation, consistently with **Spawn Orientation (DR-07)**.
- **DR-57 — Fixed Preview Area**: The preview occupies a `PreviewGrid` of exactly 4 columns × 2 rows of
  preview cells. Its size and screen position are constant for the whole session and do not vary with
  the previewed type, with the type's bounding box size, or with the preview being empty.
- **DR-58 — Preview Centring**: The previewed shape's *occupied-cell* bounding box — not its rotation
  bounding box — is centred both horizontally and vertically within the `PreviewGrid`. See §5.3 for the
  derived insets.
- **DR-59 — Preview Non-Colour Cue**: The previewed piece is identifiable without relying on colour. Its
  rendered shape distinguishes it from the other six, and a text alternative names its `TetrominoType`.
  Colour is a redundant cue in the preview, never the only one. Story 013 may re-tune the DR-24 palette
  but may not weaken this.
- **DR-60 — Empty Preview at Game Over**: Once `GameStatus` is `GameOver`, `previewedType` is null and no
  tetromino is drawn, while the area keeps its reserved size per DR-57. Consistent with **Terminal
  Status (DR-27)**: no piece will ever spawn again, so a populated preview would assert something false
  and violate DR-55.
- **DR-61 — Preview Is Non-Interactive**: The preview accepts no player input. No click, tap, drag,
  keypress or focus directed at the preview changes the previewed type, the `ActivePiece`, the
  `PieceQueue` or any other part of the `GameSession`.
- **DR-62 — Preview Follows Draws, Not Swaps**: A hold swap that exchanges the `ActivePiece` with an
  **already-occupied** hold slot draws nothing from the `PieceQueue` and therefore leaves the preview
  unchanged. A **first** hold, with an empty hold slot, draws the next type from the queue to supply the
  new active piece and therefore advances the preview by exactly one. **Story 005 owns the hold
  mechanics**, including when a hold is legal and how often; this rule fixes only the preview's response
  to each case.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently
empty). 001's NFR-003 (colour contrast ≥ 3:1) applies to the preview's cells unchanged and is not
restated here._

| ID      | Category        | Requirement                                                                                                                                                                                                      |
| ------- | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| NFR-001 | Performance     | The preview reflects a change to `PieceQueue.upcoming[0]` within **one animation frame (≤ 16 ms)** of the spawn that caused it, so it is never visibly stale even during a burst of hard drops.                      |
| NFR-002 | Usability       | Preview cells are rendered at **no less than 60%** of the playfield cell edge length, so the shape is readable at a glance without the player shifting focus away from the stack.                                    |
| NFR-003 | Usability       | Changing the previewed type causes **zero layout shift**: the measured bounding box of the preview area and of the playfield is pixel-identical across all seven types and the empty state.                           |
| NFR-004 | Accessibility   | All seven previewed types are mutually distinguishable **with colour removed**, verified against a greyscale rendering of the seven.                                                                                  |
| NFR-005 | Accessibility   | The preview's text alternative is exposed as an accessible name, **not** as a live region. At one piece every few seconds an announcement per change would make the game unusable with a screen reader; the name is available on demand instead. |
| NFR-006 | Reliability     | For any fixed seed, a 500-piece session produces an identical spawn sequence with the preview rendered and with it disabled (DR-53).                                                                                 |
| NFR-007 | Testability     | `previewedType` is observable as a derived value, so all §4 scenarios can assert on the preview headlessly without rasterising or inspecting pixels.                                                                  |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                   | Expected Behavior                                                                                                                                                   |
| ----- | -------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | The preview is inspected while the session is still `Ready` and the player has not started.   | Populated with `upcoming[0]` (FR-006). The preview is correct before the first spawn, not merely after it.                                                                 |
| EC-2  | An I is previewed, then an O.                                                                 | The area keeps its 4 × 2 extent in both cases (DR-57); the I is centred on the vertical midline, the O inset one cell on each side (DR-58). Neither widens nor narrows it. |
| EC-3  | A bag boundary yields the same type twice in a row (001 EC-7, **Repeat Bound (DR-12)**).      | The preview shows that type again after the first of the pair spawns. Correct and expected — not a stuck render. Do not add a re-roll or a "changed" guard.                |
| EC-4  | The player hard drops repeatedly, spawning several pieces per second (story 003).             | The preview advances exactly once per spawn and always shows the current `upcoming[0]`. Updates are never batched, coalesced or left a piece behind.                        |
| EC-5  | The player holds for the first time, with an empty hold slot (story 005).                     | The queue is drawn from, so the preview advances by exactly one (DR-62).                                                                                                    |
| EC-6  | The player holds again, swapping with an occupied hold slot (story 005).                      | Nothing is drawn from the queue, so the preview is unchanged (DR-62). A preview that moved here would be a defect.                                                          |
| EC-7  | **Block out (DR-21)**: the spawn cells are occupied and the piece is not placed.              | The session moves to `GameOver`, so the preview empties (DR-60). Whether the type had already been drawn before placement failed is therefore not observable in the preview. |
| EC-8  | **Lock out (DR-22)**: a piece locks entirely in buffer rows.                                  | The session moves to `GameOver`, so the preview empties (DR-60), while the area stays reserved so story 008's presentation does not have to re-flow the layout.             |
| EC-9  | The player activates *New game* mid-session.                                                  | The preview immediately shows the fresh session's first upcoming type (FR-012, **Fresh Session State (DR-23)**). Nothing from the discarded session is shown even briefly.   |
| EC-10 | The browser window is resized or zoomed during play (001 EC-9).                               | The preview rescales proportionally with the playfield; the previewed type, the 4 × 2 grid and the centring are unaffected.                                                 |
| EC-11 | The tab is backgrounded and a large time delta arrives on resume (001 EC-2).                  | The preview is derived, never cached across the stall, so on resume it matches the current `upcoming[0]`. No catch-up sequence of preview changes is replayed.              |
| EC-12 | The queue is somehow empty when the preview is read (a defect elsewhere; DR-13 forbids it).   | The preview renders empty rather than throwing or showing a stale type. The game loop's own handling of an empty queue is 001's concern (001 EC-14).                        |
| EC-13 | The player is colour-blind, or the page is rendered in a forced high-contrast or print mode.  | Shape and the text alternative identify the piece (DR-59). Losing colour degrades the preview but never disables it.                                                        |
| EC-14 | The preview receives keyboard focus via tab navigation.                                       | No game state changes (DR-61). Keys directed at the preview do not move, rotate or drop the active piece; story 002 owns where game input is read.                          |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                   |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 16 acceptance scenarios in §4 pass as automated tests in CI.                                                                                               |
| SUC-002 | Over a seeded 500-piece session, the type spawned equals the type previewed immediately beforehand on **500 of 500** spawns (DR-55).                            |
| SUC-003 | For the same seed, the full 500-type spawn sequence is identical with the preview rendered and with it disabled (DR-53).                                       |
| SUC-004 | The measured bounding box of the preview area is pixel-identical across all seven types and the empty state, and the playfield's box is unchanged throughout.  |
| SUC-005 | A greyscale rendering of all seven previews yields seven visually distinct shapes, each with a correct text alternative naming its type.                        |
| SUC-006 | Each of DR-52 … DR-62 is covered by at least one automated test.                                                                                               |
| SUC-007 | After one week of informal office play, no player reports the preview showing a piece that did not arrive, lagging a piece behind, or making the layout jump.   |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **`docs/specs/001-start-new-game.md`** — a hard dependency and the only one. 004 cannot be built
  before 001's `PieceQueue`, `TetrominoDefinition`, `GameStatus` and the rules **DR-13**, **DR-20**,
  **DR-23**, **DR-24** and **DR-27** exist. 001 §5.1 already states that story 004 "reads `upcoming` to
  render the preview and must not mutate it"; **DR-53** is the formal statement of that obligation.
- **`docs/product/story-map.md`** — the source of the story numbering and the project-wide exclusions
  quoted in §1.3.
- **Story 005 (hold)** — a two-way dependency on **DR-62** only. 004 can ship before 005; when 005 lands,
  its hold implementation must satisfy DR-62 and 005 should cite it. 004 needs no change at that point.
- **Story 003 (hard drop)** — no new preview behaviour; hard drop is just a faster path to the spawn that
  DR-54 already covers (EC-4, SC-015).
- **Story 013 (branding)** — may re-tune the DR-24 palette used by the preview; constrained by DR-59 and
  by 001's NFR-003.
- **Story 014 (controls panel)** — shares the side panel; see §9.2.
- **No downstream feature depends on 004.** Removing the preview would make the game harder but would
  break nothing else — which is the practical test of DR-53.

### 9.2 Constraints

- **Read-only by construction.** The preview must not become a second consumer of the queue. Any design
  in which the preview pops a type and pushes it back, or holds its own copy that can drift, violates
  DR-53 even if the rendered output happens to look right.
- **Side panel real estate.** The preview shares the area beside the playfield with story 005's hold
  box, story 006's score, story 007's level and line count, and story 014's controls panel. DR-57's
  fixed area is what makes that panel stable; each later story must place itself around the preview
  rather than reflow it.
- **Platform**: desktop browser on a laptop with a keyboard. No mobile or touch support (story map).
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed by this spec):
  ASP.NET Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under
  `wwwroot/`. No test project exists yet; SUC-001 requires one.
- **No persistence, no personal data.** The preview adds no stored state and no identity, consistent
  with 001's NFR-007.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*, per `CLAUDE.md` ("fill them in as architectural decisions for
> the Tetris implementation are made").

| Arc42 Section                                               | Relevance to This Feature                                                                                                                                        |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 03. System Scope and Context (`03-system-scope-and-context.md`) | The preview adds nothing to the system boundary: no external system, no persistence, no new input. It is strictly additional output within the existing page. *Currently empty.* |
| 05. Building Block View (`05-building-block-view.md`)         | 004 introduces a read-model / projection over the game domain, consumed by the presentation layer. It must be recorded as a *reader* of the piece queue block, never a collaborator that can mutate it. *Currently empty.* |
| 06. Runtime View (`06-runtime-view.md`)                       | 001's central runtime scenario (spawn → gravity → lock → spawn) gains one observable step: the preview refreshes at the draw point (DR-54). The runtime view should show the refresh hanging off the spawn, not off the lock. *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)   | Needs two cross-cutting positions this feature is the first to require: (a) derived read models are recomputed, never cached (DR-53, EC-11); (b) the accessibility baseline — shape and text as redundant cues, accessible names rather than live regions (DR-59, NFR-004, NFR-005). *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)       | Should host the project-wide accessibility and layout-stability scenarios that §6 here only instantiates for the preview. *Currently empty.*                        |
| 12. Glossary (`12-glossary.md`)                               | Should gain: next piece preview, preview grid, preview cell, draw point. *Currently empty.*                                                                         |

Arc42 sections 01, 02, 04, 07, 09 and 11 are unaffected by this feature — 004 introduces no new goal,
constraint, solution strategy, deployment concern, architecture decision or technical debt beyond what
001 already records.

## 10. Open Questions

| #   | Question                                                                            | Owner      | Status   | Resolution                                                                                                                                                                                                                                 |
| --- | ------------------------------------------------------------------------------------- | ---------- | -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | One previewed piece, or the Guideline-optional 3–6 piece preview queue?                | Daan Harms | Resolved | **One** (DR-52). The story says "the next piece", singular, and one piece is what a casual coffee-break player can actually use. Recorded as a Non-Goal in §1.3 with the note that **Queue Refill Threshold (DR-13)** already makes a longer preview a trivial later change. |
| 2   | Should the preview be populated while the session is still `Ready`, before the start action? | Daan Harms | Resolved | **Yes** (FR-006, EC-1). The rule "the preview shows the type the next draw will yield" is then true for the session's whole lifetime with no special case, and the player can plan their opening placement. There is nothing to spoil — the first piece is random either way. |
| 3   | At game over, should the preview blank or freeze on its last value?                    | Daan Harms | Resolved | **Blank** (DR-60). Under **Terminal Status (DR-27)** no piece will ever spawn again, so a populated preview would be announcing a piece that is never coming — a direct contradiction of **Preview Fidelity (DR-55)**. The area stays reserved so story 008's presentation does not have to re-flow the panel. |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001, SC-006, SC-007; US-002 -> SC-002..SC-005, SC-008..SC-012, SC-014..SC-016;
        US-003 -> SC-013
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-005 -> US-001; FR-006..FR-012, FR-015 -> US-002; FR-013, FR-014 -> US-003)
  - [x] Domain model covers all entities mentioned in the requirements
        NextPiecePreview (derived read model); value object PreviewGrid; all other names reused
        verbatim from 001 §5 (GameSession, PieceQueue, ActivePiece, TetrominoDefinition,
        TetrominoType, Orientation, GameStatus)
  - [x] Domain rules and invariants are listed (DR-52 .. DR-62; DR-01..DR-27 inherited from 001)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-14)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-007)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders,
        and naming the sections this feature does NOT touch
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all three resolved)
-->
