# Feature Specification: Game Over — Topping Out, the Final Summary and Playing Again

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
| Feature ID      | 008                                                                                           |
| Status          | Draft                                                                                         |
| Author          | Daan Harms                                                                                    |
| Created         | 2026-10-20                                                                                    |
| Last updated    | 2026-10-20                                                                                    |
| Epic / Parent   | Play Tetris (story map activity) — `docs/product/story-map.md`                                |
| Arc42 reference | Sections 3, 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders. |

> **Note on this spec.** 008 closes the *Play Tetris* activity and is the hand-off point to *Compete*.
> It has two halves. The **top-out conditions** are already fixed by **spec 001** — this spec consolidates
> them and makes them testable, it does not redefine them. The **game-over experience** — the freeze, the
> final summary, playing again, and the contract handed to story 009 — is owned here.
>
> **Vocabulary.** All entity names, enum members and domain rule IDs from
> [`001-start-new-game.md`](001-start-new-game.md) §5 are normative and are reused verbatim. Where this
> spec cites a rule it cites it by ID and name, e.g. "per **Block Out (DR-21)**". New rules introduced here
> are numbered from **DR-100** so that IDs stay globally unique across concurrently written specs.
>
> **Ruleset.** Every detail not otherwise constrained by the story map is resolved against the modern
> **Tetris Guideline**. This is a standing project decision, not an assumption to revisit per-spec.

### 1.1 Problem Statement

A session can already reach `GameStatus.GameOver` — spec 001 defines **Block Out (DR-21)** and
**Lock Out (DR-22)**, and **Terminal Status (DR-27)** freezes the session when either fires — but nothing
is shown to the player when it happens. The game simply stops: no acknowledgement, no result, no way to
start again short of reloading the page, and no moment at which a run can be offered to the leaderboard.
A five-minute coffee break ends in a dead screen instead of a number worth bragging about.

### 1.2 Goal

The instant the stack tops out, the game freezes and says so. The final board stays on screen exactly as
it stood, so the player can see how they lost. Alongside it they are shown what the run was worth — final
score, lines cleared, level reached and pieces spawned — and they can start a fresh game with one action
without touching the keyboard shortcuts of the game they just lost. The finished run is left available, as
a self-contained immutable record, for story 009 to attach a name to and submit.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec. Each is either a separate story or a
project-wide exclusion. Do not implement them here, and do not design around a guess at how they will
work — the owning spec defines that.

- **Name entry, name validation, score submission and persistence** — **story 009**. This spec creates and
  exposes the `CompletedRun` (§5.1) and stops there. It writes nothing to a server, a database or storage
  of any kind, and it renders no text input.
- **The leaderboard itself** — viewing it (**story 010**) and highlighting the just-finished run on it
  (**story 011**). 008 only guarantees that the run is *identifiable* (DR-107); where the leaderboard lives
  and how it is reached is 010's decision.
- **Any anti-cheat, score signing, server-side replay validation or rate limiting.** See §9.2 — the score
  is computed client-side and is forgeable. That is a stated constraint, not a problem this spec solves.
- **Redefining the top-out conditions.** Block out and lock out are defined by 001 (DR-21, DR-22). This
  spec cites them and gives each a concrete acceptance scenario; it does not restate their mechanics.
- **Additional top-out conditions.** The Guideline's block out and lock out are the only two (DR-100).
  There is no partial-lock-out rule, no "stack reached row N" rule, no time limit and no piece limit.
- **Scoring, line clearing and the level curve.** `score` and `linesCleared` are **story 006**'s;
  `level` is **story 007**'s; `piecesSpawned` is **story 001**'s. 008 reads these values and displays them.
  It never computes, adjusts, rounds or awards an end-of-game bonus (DR-104).
- **Pause and "quit to menu".** Pause is out of scope project-wide (story map: "games are short by
  design"), so there is no paused state to quit from and no menu to quit to. The only ways out of a game
  are topping out and the *New game* action that 001 FR-005 already provides.
- **Retry from a checkpoint, undo, or resuming a lost game.** Playing again means a brand-new session
  (DR-105). Nothing is rewound.
- **Run history.** The player sees the summary for the run they just finished and nothing else. There is
  no "your last 10 games", no personal statistics and no session log. The single leaderboard submission in
  story 009 is the only thing that outlives a run.
- **Accounts, registration, login, persisted identity** — out of scope project-wide. The player is
  identified solely by the name they type at game over, in story 009.
- **Info Support branding, typography and the visual design of the game-over presentation** — **story
  013**. This spec fixes *what information is shown and how it behaves*, not how it looks.
- **The controls / how-to-play panel** — **story 014**, which documents the play-again key named in FR-012.
- **Ghost piece, mobile and touch support** — out of scope project-wide.

## 2. User Stories

### US-001: Know at once that the game is over

**As a** player,
**I want** the game to stop dead and tell me unmistakably that it is over the moment I top out,
**so that** I am not still pressing keys at a board that has stopped responding.

### US-002: See how I lost

**As a** player,
**I want** the final board left on screen exactly as it stood when I topped out,
**so that** I can see the mistake that killed the run instead of guessing at it.

### US-003: See what the run was worth

**As a** player,
**I want** my final score, lines cleared, level and piece count shown together when the game ends,
**so that** I know immediately whether that run was worth putting on the board.

### US-004: Go again straight away

**As a** player,
**I want** to start a fresh game from the game-over screen with a single action,
**so that** "one more go" costs me nothing in the few minutes I have.

### US-005: Hand the run to the leaderboard

**As a** player,
**I want** the run I have just finished to be offered to the leaderboard as it stands, without replaying
or re-entering anything,
**so that** claiming my place is the obvious next step and not a separate chore.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) defined in this spec are
listed in §5.5; rules DR-01 to DR-27 are defined in spec 001 and only cited here._

| ID     | Requirement                                                                                                                                                                                                                      | Priority | User Story |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall move a `Playing` session to `GameStatus.GameOver` on **Block Out (DR-21)** or **Lock Out (DR-22)**, and on no other condition whatsoever (DR-100).                                                                   | Must     | US-001     |
| FR-002 | The system shall, on entering `GameOver`, immediately stop gravity, stop all fall and lock timers, and spawn no further piece, per **Terminal Status (DR-27)** and DR-101.                                                            | Must     | US-001     |
| FR-003 | The system shall ignore every gameplay input while the session is `GameOver` — story 002's move, rotate and soft drop, story 003's hard drop and story 005's hold — with no effect on the board, the score or any timer (DR-101).     | Must     | US-001     |
| FR-004 | The system shall present an unmistakable game-over indication within 100 ms of the transition, with no animation gate, countdown or dismissal step in front of it.                                                                   | Must     | US-001     |
| FR-005 | The system shall keep the 20 visible rows of the `Playfield` on screen and legible for as long as the game-over state is presented, showing the stack exactly as it stood at the moment of the transition (DR-102).                   | Must     | US-002     |
| FR-006 | The system shall leave a blocked-out piece unwritten and invisible (DR-21), and shall have written a locked-out piece into the stack before freezing (DR-22), so that the frozen board reflects the actual cause of the top out.     | Must     | US-002     |
| FR-007 | The system shall display, together with the game-over indication, the run's final **score** and **lines cleared** (story 006), final **level** (story 007) and **pieces spawned** (story 001), each exactly as frozen (DR-104).       | Must     | US-003     |
| FR-008 | The system shall record which condition ended the run as `TopOutReason.BlockOut` or `TopOutReason.LockOut`.                                                                                                                          | Should   | US-003     |
| FR-009 | The system shall name the top-out reason in plain language in the summary (e.g. "no room left to spawn" / "locked out above the field").                                                                                             | Could    | US-003     |
| FR-010 | The system shall not count a piece that failed to be placed by block out towards `piecesSpawned` (DR-108).                                                                                                                            | Must     | US-003     |
| FR-011 | The system shall offer a *play again* action in the `GameOver` state that discards the finished session and constructs a fresh one per **Fresh Session State (DR-23)**, carrying nothing over (DR-105).                                | Must     | US-004     |
| FR-012 | The system shall make the *play again* action reachable and activatable from the keyboard alone, without a pointing device.                                                                                                           | Must     | US-004     |
| FR-013 | The system shall not ask the player to confirm *play again*, and shall not block it on the run having been submitted to the leaderboard.                                                                                             | Should   | US-004     |
| FR-014 | The system shall construct exactly one immutable `CompletedRun` at the moment of the transition to `GameOver`, carrying the attributes in §5.1 (DR-103).                                                                               | Must     | US-005     |
| FR-015 | The system shall expose that `CompletedRun` to story 009 for the whole time the game-over state is presented, and shall itself neither persist, transmit nor submit it (DR-106).                                                       | Must     | US-005     |
| FR-016 | The system shall give each `CompletedRun` an identifier equal to the finished `GameSession.id` and unique among all runs played on the page, so story 011 can match a leaderboard entry to the just-finished run (DR-107).             | Must     | US-005     |
| FR-017 | The system shall create no `CompletedRun` for a session that is discarded by the *New game* action mid-play, abandoned, or reloaded away (DR-103).                                                                                     | Must     | US-005     |
| FR-018 | The system shall treat *play again* as the same action as 001's *New game* (001 FR-005), surfaced in the game-over state rather than as a second, competing concept.                                                                  | Should   | US-004     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash.
Board states use the coordinate system fixed in 001 §5.0: row 0–19 buffer, row 20–39 visible, row 39 the
floor, column 0–9 left to right._

### SC-001: Block out — the next piece has nowhere to spawn (FR-001, FR-006)

```gherkin
Given a session is Playing
  And the stack occupies cells (3,19), (4,19) and (5,19) in the bottom-most buffer row
  And the next type in the piece queue is T, whose spawn cells are (4,18), (3,19), (4,19) and (5,19)
When the piece attempts to spawn
Then the piece is not placed and never becomes the active piece
  And none of its four cells is written to the playfield
  And the session moves to status GameOver
  And the top-out reason recorded is BlockOut
```

### SC-002: Lock out — a piece locks entirely above the visible field (FR-001, FR-006)

```gherkin
Given a session is Playing
  And the stack occupies cells (4,20) and (5,20) in the top visible row
  And the next type in the piece queue is O, whose spawn cells (4,18), (5,18), (4,19) and (5,19) are free
When the piece spawns
Then it is placed, its initial drop is skipped because (4,20) and (5,20) are occupied, and it is grounded
When its lock delay expires and it locks
Then its four cells are written to the playfield as occupied cells of type O
  And because all four lie in buffer rows the session moves to status GameOver
  And the top-out reason recorded is LockOut
```

### SC-003: A hold swap can also block out (FR-001)

```gherkin
Given a session is Playing with story 005's hold available
  And the stack occupies cells (3,19), (4,19) and (5,19)
  And the held piece is a J, whose spawn cells include (3,19)
When the player swaps the held piece in
Then the swapped-in piece is not placed
  And the session moves to status GameOver with reason BlockOut
```

### SC-004: There is no third way to top out (FR-001)

```gherkin
Given a session is Playing
  And the stack reaches the top visible row in several columns
  And no piece has been blocked out and none has locked entirely in buffer rows
When play continues
Then the session remains in status Playing
  And no game-over presentation appears
```

### SC-005: Everything stops at once (FR-002)

```gherkin
Given a session has just moved to status GameOver
When 10 seconds of game time elapse
Then no piece has spawned
  And nothing on the playfield has moved
  And the score, lines cleared, level and piece count are unchanged
```

### SC-006: Gameplay input is dead (FR-003)

```gherkin
Given a session is in status GameOver
When the player presses the move, rotate, soft drop, hard drop and hold controls in any order
Then nothing on the playfield changes
  And no piece spawns
  And the score, lines cleared, level and piece count are unchanged
  And the session is still in status GameOver
```

### SC-007: The game says so immediately (FR-004)

```gherkin
Given a session is Playing
When it tops out
Then within 100 milliseconds a game-over indication is visible
  And it required no key press, click or dismissal to appear
```

### SC-008: The losing board stays on screen (FR-005)

```gherkin
Given a session has topped out with a stack of 47 locked cells
When the player looks at the page
Then all 20 visible rows by 10 columns are still shown
  And every one of those 47 locked cells is still drawn in its tetromino colour and remains identifiable
  And the board does not clear, fade out or scroll away while the game-over state is presented
```

### SC-009: The blocked-out piece is absent from the frozen board (FR-006)

```gherkin
Given a session has ended by block out
When the frozen playfield is inspected
Then it contains exactly the cells of the pieces that locked
  And no cell of the piece that failed to spawn appears anywhere
```

### SC-010: The final summary reports the frozen figures exactly (FR-007, FR-010)

```gherkin
Given a session reached a score of 12,400 with 31 lines cleared at level 4
  And 78 pieces were spawned and placed
When the session tops out by block out on the 79th piece
Then the summary shows a final score of 12,400
  And lines cleared of 31
  And a level of 4
  And 78 pieces spawned, not 79
  And no end-of-game bonus, rounding or adjustment has been applied to any figure
```

### SC-011: A lock that both clears a line and locks out counts the clear (FR-007)

```gherkin
Given a piece is grounded with all four of its cells in buffer rows
  And locking it completes a line
When it locks
Then the line clear resolves and is scored per story 006
  And the final score shown includes that clear
  And the session then moves to status GameOver with reason LockOut
```

### SC-012: Playing again starts from nothing (FR-011, FR-018)

```gherkin
Given a session has topped out at level 7 with a score of 20,100 and a piece held
When the player activates play again
Then the finished session is discarded
  And a fresh session begins with every cell empty, level 1, score 0, lines cleared 0,
      pieces spawned 0, an empty hold slot and a freshly seeded piece queue
  And no part of the previous score, level, stack, hold or piece sequence carries over
```

### SC-013: Playing again needs no mouse (FR-012)

```gherkin
Given a session is in status GameOver
  And the player has not touched the trackpad
When they activate the play-again control from the keyboard
Then a fresh session begins
```

### SC-014: Playing again does not interrogate the player (FR-013)

```gherkin
Given a session is in status GameOver
  And the run has not been submitted to the leaderboard
When the player activates play again
Then a fresh session begins immediately
  And no confirmation prompt, warning or blocking dialog is shown
  And the unsubmitted run is simply discarded
```

### SC-015: The finished run is offered up complete (FR-014, FR-015, FR-016)

```gherkin
Given a session with id S has topped out with a score of 9,800, 24 lines, level 3 and 61 pieces
When the game-over state is presented
Then exactly one completed run is available
  And its id is S
  And it carries a final score of 9,800, 24 lines cleared, level 3, 61 pieces spawned and a top-out reason
  And it carries no player name and no other identity
  And nothing has been written to a server, a database or browser storage by this feature
```

### SC-016: The completed run cannot be altered (FR-014)

```gherkin
Given a completed run exists for a finished session
When any attempt is made to change its score, lines, level, piece count or reason
Then the attempt is rejected and the recorded values stand
```

### SC-017: Abandoned sessions produce nothing (FR-017)

```gherkin
Given a session is Playing with a score of 5,000
When the player activates New game before topping out
Then no completed run is created for the abandoned session
  And nothing from it is offered to the leaderboard
```

### SC-018: Each run is distinguishable from the last (FR-016)

```gherkin
Given the player has finished three runs in a row on the same page without reloading
When the three completed runs are compared
Then all three ids are different
  And the third run's id identifies the run that has just finished
```

## 5. Domain Model

_This spec adds one entity, one enumeration and two attributes to the model defined in
[001 §5](001-start-new-game.md#5-domain-model). Everything else — `GameSession`, `Playfield`,
`ActivePiece`, `PieceQueue`, `Cell`, `CellPosition`, `TetrominoType`, `Orientation`, `GameStatus` — is
used exactly as 001 defines it and is not repeated here._

### 5.1 Entities

#### CompletedRun

The immutable record of one finished playthrough: everything story 009 needs to put a run on the
leaderboard, and nothing more. It is a snapshot of *copies*, not a view onto the frozen session, so it
cannot drift and cannot be used to reach back into gameplay state.

| Attribute      | Type           | Constraints                                        | Description                                                                                      |
| -------------- | -------------- | -------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| runId          | UUID           | PK; equals the finished `GameSession.id` (DR-107)   | Identifies the run. Story 011 matches a leaderboard entry to the just-finished run by this value.   |
| finalScore     | integer        | ≥ 0; copied at the transition, immutable           | `GameSession.score` as frozen. **Owned by story 006** — read here, never computed here.              |
| linesCleared   | integer        | ≥ 0; copied at the transition, immutable           | `GameSession.linesCleared` as frozen. **Owned by story 006.**                                        |
| finalLevel     | integer        | ≥ 1; copied at the transition, immutable           | `GameSession.level` as frozen. **Owned by story 007.**                                               |
| piecesSpawned  | integer        | ≥ 0; copied at the transition, immutable           | `GameSession.piecesSpawned` as frozen (001), excluding a blocked-out piece (DR-108).                  |
| topOutReason   | TopOutReason   | required, immutable                                | Which of the two conditions ended the run.                                                           |
| startedAt      | datetime       | copied from `GameSession.startedAt`, immutable     | When the run began.                                                                                  |
| endedAt        | datetime       | ≥ `startedAt`, immutable                           | When the session moved to `GameOver`.                                                                |
| duration       | duration       | derived: `endedAt - startedAt`                     | How long the run lasted. Recorded for completeness; displaying it is not required.                   |

`CompletedRun` deliberately carries **no player name and no identity of any kind**. Story 009 pairs a name
with a run at submission time; the name is never an attribute of this entity. Consistent with 001's
NFR-007, this feature is still the point at which no personal data has yet been collected.

#### GameSession (extension to 001)

001 defines this entity in full. 008 adds two attributes and changes nothing else.

| Attribute     | Type           | Constraints                                       | Description                                                                                 |
| ------------- | -------------- | ------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| endedAt       | datetime       | nullable; set once, at the move to `GameOver`     | Null while `Ready` or `Playing`. Immutable once set. Complements 001's `startedAt`.             |
| completedRun  | CompletedRun   | nullable; set once, at the move to `GameOver`     | Null unless and until the session tops out (DR-103). Never set for a discarded session.          |

### 5.2 Relationships

- A **GameSession** produces **at most one CompletedRun**, created exactly at its transition to
  `GameStatus.GameOver` (DR-103). A session that is discarded mid-play, abandoned or never started produces
  none.
- A **CompletedRun** belongs to exactly one **GameSession** and shares its identifier (DR-107).
- A **CompletedRun** holds **copies** of the session's final figures, not references into the session. Once
  created it is independent of the `GameSession`, the `Playfield` and every other object in the model.
- A **CompletedRun** has **no relationship to a player, a name or an account** — none exists in this
  system. Story 009's leaderboard entry is the first thing to associate a name with a `runId`; that entity
  is defined by **story 009**, not here.
- The frozen **Playfield** remains reachable from the finished **GameSession** for as long as the
  game-over state is presented (DR-102), but is deliberately *not* part of the `CompletedRun` — no board
  snapshot is submitted to or stored by the leaderboard.

### 5.3 Value Objects

This spec introduces no new value objects. `CellPosition` and `Cell` (001 §5.3) are used unchanged to
describe the frozen board in §4.

### 5.4 Enumerations

#### TopOutReason

Which of the two Guideline top-out conditions ended a run. Closed set; there is no third member and no
"unknown" member — a `CompletedRun` cannot exist without one of these having fired (DR-100, DR-103).

| Member     | Condition                                                                                                   |
| ---------- | ------------------------------------------------------------------------------------------------------------- |
| `BlockOut` | **Block Out (DR-21)** — a piece could not be placed at its spawn position because a required cell was occupied. |
| `LockOut`  | **Lock Out (DR-22)** — a piece locked with all four of its cells in buffer rows (row ≤ 19).                     |

`GameStatus` is unchanged from 001: `Ready`, `Playing`, `GameOver`. This spec adds no status member — the
game-over presentation is a presentation of the terminal `GameOver` status, not a state of its own.

### 5.5 Domain Rules and Invariants

_New rules introduced by this spec, numbered from DR-100 to keep IDs globally unique. Rules DR-01 to DR-27
are defined in spec 001 and are cited, never restated._

- **DR-100 — Top-Out Completeness**: **Block Out (DR-21)** and **Lock Out (DR-22)** are the *only* ways a
  session reaches `GameStatus.GameOver`. There is no partial-lock-out rule, no stack-height threshold, no
  time limit, no piece limit and no condition based on the visible field filling up. A stack that reaches
  the top visible row is not by itself a top out.
- **DR-101 — Immediate Freeze**: The freeze required by **Terminal Status (DR-27)** takes effect at the
  instant of the transition and extends to input. From that instant: gravity is not applied, fall and lock
  timers do not advance, no piece spawns, and every gameplay input — story 002's move, rotate and soft
  drop, story 003's hard drop, story 005's hold — is discarded with no effect. Input already in flight when
  the transition occurs is discarded, never applied afterwards. Only the game-over actions are accepted:
  *play again* (FR-011) and, from story 009, submitting the run.
- **DR-102 — Final Board Preservation**: The `Playfield` as it stood at the transition is preserved
  unchanged and remains visible for as long as the game-over state is presented. The game-over
  presentation may sit over or beside it but must leave every occupied visible cell identifiable. The board
  is cleared only by starting a new session (DR-105).
- **DR-103 — Completed Run Provenance**: A `CompletedRun` is created exactly once per session, at the moment
  of the transition to `GameOver`, from the frozen session's values, and is immutable thereafter. A session
  that is discarded by *New game* mid-play, abandoned, reloaded away, or never started produces none. The
  **existence** of a `CompletedRun` is therefore the only token this system has that a run was actually
  played to its end; story 009 must treat it as such (see §9.2 on what that token is and is not worth).
- **DR-104 — Summary Fidelity**: Every figure shown at game over and carried on the `CompletedRun` equals
  the frozen session's value exactly. No end-of-game bonus, penalty, rounding, rescaling, recomputation or
  derived "performance rating" is applied. The final score is the score after the last line clear has
  resolved per story 006.
- **DR-105 — Play Again Freshness**: *Play again* constructs a new `GameSession` per **Fresh Session State
  (DR-23)** and discards the finished one. Nothing carries over: not the score, lines cleared, level, piece
  count, stack, hold slot, piece queue, current bag or randomiser seed. A `CompletedRun` already handed to
  story 009 is unaffected by the discard.
- **DR-106 — Run Availability Window**: The `CompletedRun` is available from the transition until the player
  starts a new session or leaves the page, and no longer. This feature never persists it, transmits it or
  writes it to storage of any kind; survival beyond that window is entirely story 009's doing.
- **DR-107 — Run Identity**: `CompletedRun.runId` equals the finished `GameSession.id` and is unique among
  all runs played on the page. Two runs are never conflated, and story 011 can identify the just-finished
  run by this value alone.
- **DR-108 — Spawn Counting at Block Out**: A piece that fails to be placed under **Block Out (DR-21)**
  never spawned and does not increment `piecesSpawned`. This is a clarification of 001's attribute, not a
  change to it: the count is of pieces that actually entered play.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty),
and the core-loop qualities in 001 §6 continue to apply._

| ID      | Category         | Requirement                                                                                                                                                                                            |
| ------- | ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Responsiveness   | The game-over indication is visible within **100 ms** of the transition, and the full summary within **250 ms**. No animation, countdown or transition delays either.                                       |
| NFR-002 | Correctness      | **Zero** gameplay state changes occur after the transition: across 1,000 simulated key events in the `GameOver` state, the board, score, lines, level and piece count are bit-identical to the frozen state. |
| NFR-003 | Usability        | The summary is readable at arm's length on a mid-range Info Support laptop: all four figures visible at once without scrolling, and labelled, so no figure has to be inferred from position alone.          |
| NFR-004 | Usability        | Where the game-over presentation overlays the playfield, occupied cells behind it retain a contrast ratio of **≥ 3:1** against the empty-cell background, consistent with 001's NFR-003.                     |
| NFR-005 | Accessibility    | The game-over state is announced to assistive technology; the summary is real text, not an image; and both *play again* and story 009's submission affordance are reachable in sequence by keyboard alone.  |
| NFR-006 | Responsiveness   | *Play again* produces a playable fresh session within **500 ms** of activation, with no page reload.                                                                                                        |
| NFR-007 | Testability      | The `CompletedRun` is assertable headlessly: given a seeded session driven to a top out, all nine of its attributes can be read and compared without rendering or real elapsed wall-clock time (001 NFR-006).|
| NFR-008 | Security/Privacy | This feature collects, transmits and persists **no data at all** — personal or otherwise. The first byte to leave the page is story 009's submission, and the first personal datum is the name typed there.  |
| NFR-009 | Resource usage   | Playing ten runs in succession without reloading leaves no retained state from discarded sessions beyond the `CompletedRun`s already handed to story 009; memory does not grow run over run (001 NFR-008).   |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                           | Expected Behavior                                                                                                                                                                        |
| ----- | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | A piece's spawn cells are occupied.                                                                  | Block out (DR-21). The piece is never placed and never drawn; `piecesSpawned` does not increment (DR-108); the session freezes with reason `BlockOut`.                                           |
| EC-2  | A piece locks with all four cells in buffer rows.                                                    | Lock out (DR-22). The cells **are** written first, so the frozen board shows the offending piece, then the session freezes with reason `LockOut`.                                               |
| EC-3  | Story 005's hold swap brings in a piece whose spawn cells are occupied.                              | Block out (DR-21), exactly as for a queued piece. The swapped-in piece is not placed. The hold slot's final contents are irrelevant to the summary.                                             |
| EC-4  | The locking piece both completes a line and locks out.                                               | The lock resolves in full — cells written, line clear resolved and scored per story 006 — and the final score includes that clear. Lock out is judged from the cells at the moment of locking and is not reconsidered after the clear (DR-22, DR-104). |
| EC-5  | The player is holding down a movement key, or has a key repeat in flight, as the game tops out.      | Every such input is discarded (DR-101). The board does not twitch after the freeze.                                                                                                             |
| EC-6  | The player hammers keys at the frozen board.                                                         | Nothing happens (FR-003). No error, no sound-and-fury, no state change. The play-again key still works.                                                                                        |
| EC-7  | A run ends with a score of 0 — e.g. a block out before any line was cleared.                         | A perfectly ordinary `CompletedRun` with `finalScore` 0 is created and shown. Whether a zero score is worth putting on the leaderboard is **story 009's** call, not a reason to suppress the run. |
| EC-8  | The player activates *play again* before submitting the run.                                         | The fresh session starts immediately, no prompt (FR-013), and the unsubmitted run is discarded (DR-105, DR-106). Games are short by design — consistent with 001's EC-8.                           |
| EC-9  | The player reloads the page or closes the tab at game over.                                          | Everything is lost. 008 persists nothing (DR-106). The run is simply not submitted.                                                                                                             |
| EC-10 | The tab is backgrounded at the moment of the top out and restored minutes later.                     | The frozen state is unchanged — there are no running timers to catch up (DR-101) and no multi-step catch-up burst (001 DR-26). The summary is exactly as it would have been.                      |
| EC-11 | The player tops out, plays again, and tops out again on the same page.                               | Two distinct `CompletedRun`s with distinct ids (DR-107). The second summary shows only the second run; nothing from the first bleeds into it (DR-105).                                            |
| EC-12 | Story 009's submission is attempted twice for the same run.                                          | Out of scope here. 008 exposes one immutable run for the whole window (DR-106); whether a second submission is accepted, ignored or treated as an update is **story 009's** decision.             |
| EC-13 | The browser window is resized or zoomed while the game-over state is shown.                          | The frozen playfield and the summary rescale proportionally. No figure changes and the run is not affected.                                                                                     |
| EC-14 | The stack reaches the top visible row but every piece still spawns and still locks with a cell below row 20. | Play continues. This is not a top out (DR-100). The game ends only when DR-21 or DR-22 actually fires.                                                                                   |
| EC-15 | A top out occurs on the very first piece — only possible via a hold swap (EC-3), never on an empty board. | Handled identically: a `CompletedRun` with `piecesSpawned` of 0 or 1, score 0, level 1. No special case and no suppression.                                                                |
| EC-16 | Code elsewhere tries to resume a `GameOver` session, or to set its status back to `Playing`.         | Rejected. `GameOver` is terminal (001 DR-27); the only path onward is a new session (DR-105). There is no resume, no undo and no continue.                                                      |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                       |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| SUC-001 | All 18 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                   |
| SUC-002 | Each of the nine domain rules DR-100 to DR-108 is covered by at least one automated test, and DR-21, DR-22 and DR-27 each gain a test at the behaviour level here.    |
| SUC-003 | A seeded session driven to block out, and one driven to lock out, each produce a `CompletedRun` whose nine attributes match the expected values exactly.            |
| SUC-004 | In a 1,000-event fuzz of gameplay input against a `GameOver` session, zero state changes are observed (NFR-002).                                                    |
| SUC-005 | A player can go from topping out to a piece falling in a fresh game in **one action and under one second**, using the keyboard only.                                |
| SUC-006 | Ten consecutive runs on one page load produce ten distinct `runId`s and ten independent summaries, with no value carried over between them.                         |
| SUC-007 | Story 009 can be built entirely against §5.1's `CompletedRun` without needing any other access to the finished session, and without 008 changing.                   |
| SUC-008 | In informal office play over one week, no player reports being unsure whether the game had ended, or being unable to find how to play again.                        |

## 9. Dependencies and Constraints

### 9.1 Dependencies

**Upstream — 008 cannot be completed before these:**

- **Story 001 (start a new game)** — defines the two top-out conditions (DR-21, DR-22), the terminal status
  (DR-27), `GameStatus`, `GameSession`, `Playfield`, `piecesSpawned` and `DR-23`'s fresh-session rule, and
  001 FR-005's *New game* action that FR-018 reuses. This spec is meaningless without it.
- **Story 006 (line clearing and scoring)** — owns `score` and `linesCleared`. 008 reads both. Until 006
  exists, the summary truthfully shows 0 for each.
- **Story 007 (levels)** — owns `level`. 008 reads it. Until 007 exists, the summary shows level 1.
- **Stories 002, 003, 005 (input, hard drop, hold)** — 008 must ignore each of their inputs at game over
  (FR-003). Each is also a way to *reach* a top out: 003's hard drop can lock a piece out, and 005's hold
  swap can block out (SC-003). 008 does not depend on them existing, but its input-freeze tests do.

**Downstream — these depend on 008:**

- **Story 009 (name entry and score submission)** — consumes the `CompletedRun` and owns everything that
  happens to it next: the name input, its validation, the submission, any server call and all persistence.
  The boundary is deliberate and total: 008 hands over a record and does not know whether it is submitted.
  See the hand-off contract in §5.1, DR-103 and DR-106, and the integrity caveat in §9.2.
- **Story 011 (highlight the just-finished run)** — depends on `runId` (DR-107) to pick its run out of the
  leaderboard.
- **Story 010 (view the leaderboard)** — owns where the leaderboard is reached from, including whether the
  game-over state links to it. 008 reserves no space for it and makes no assumption.
- **Story 013 (branding)** — styles the game-over presentation. It must preserve NFR-003 and NFR-004.
- **Story 014 (controls panel)** — documents the play-again key chosen per FR-012 and open question 2.

**Other:**

- **`docs/product/story-map.md`** — source of the story numbering, the primary user, and the project-wide
  exclusions quoted in §1.3.
- **The modern Tetris Guideline** — the normative ruleset; here it is what fixes block out and lock out as
  the only two top-out conditions (DR-100).

### 9.2 Constraints

- **The score is computed client-side and is therefore forgeable.** Everything in the `CompletedRun` —
  score, lines, level, piece count — is produced by code running in the player's browser and can be
  altered by anyone willing to open the developer tools. `CompletedRun`'s existence (DR-103) is evidence of
  nothing stronger than "this page said a game ended". This is stated plainly rather than papered over.
  It is an **accepted trade-off**: this is an internal office game with no accounts, no stakes beyond
  bragging rights, and a user base of colleagues. Hardening it would mean server-side validation of a
  replayed input stream — cost far out of proportion to the risk. **Whether to accept or mitigate this is
  story 009's decision**, since 009 is what publishes a run to a shared board. 008 designs no anti-cheat,
  signs nothing and validates nothing.
- **No persistence of any kind in this feature** (DR-106). Nothing is written to a server, a database,
  `localStorage` or a cookie. A refresh at game over loses the run.
- **No accounts and no identity** (story map). A run is anonymous until a name is typed in story 009.
- **No pause** (story map), so there is no paused state and no "quit to menu" — the only exits from a game
  are topping out and *New game*.
- **Platform**: desktop browser on a laptop with a keyboard. The game-over state must be fully operable
  from the keyboard (FR-012). No mobile or touch support.
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed by this spec):
  ASP.NET Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`.
  Whether the game loop runs browser-side or server-side is still an open architecture decision (001 open
  question 1); nothing in §3–§5 changes either way.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*, matching the approach taken in 001 §9.3.

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                      |
| ----------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01. Introduction and Goals (`01-introduction-and-goals.md`)       | Should record that the loop "play → top out → see the score → go again" is the product's core, and that the frictionless restart (SUC-005) is a quality driver, not a nicety. *Currently empty.* |
| 02. Architecture Constraints (`02-architecture-constraints.md`)   | Should record the client-side-score constraint from §9.2 as an acknowledged, accepted architectural limitation of the whole product. *Currently empty.*                               |
| 03. System Scope and Context (`03-system-scope-and-context.md`)   | 008 is the last point at which the system boundary is still a single page with no external systems. The boundary crossing begins in story 009; this spec defines what sits on the inside of it. *Currently empty.* |
| 05. Building Block View (`05-building-block-view.md`)             | 008 introduces the `CompletedRun` as the explicit seam between the game-domain building block and the Compete building block. Should be drawn as such. *Currently empty.*              |
| 06. Runtime View (`06-runtime-view.md`)                           | Extends 001's central runtime scenario with the two exits — spawn-blocked and lock-out — and the subsequent freeze → summarise → hand off → restart sequence. *Currently empty.*       |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)       | Needs the input-handling concept to state that input is gated on `GameStatus` (DR-101), since every input story (002, 003, 005) must honour it rather than each inventing its own guard. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`)       | 008 warrants ADRs for: trusting client-computed scores (§9.2), and an immutable snapshot `CompletedRun` as the activity boundary rather than letting 009 read live session state. *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)           | Should host the project-wide responsiveness and accessibility scenarios that NFR-001, NFR-005 and NFR-006 are instances of. *Currently empty.*                                        |
| 11. Risks and Technical Debt (`11-risks-and-technical-debt.md`)   | Should carry the forgeable-score risk from §9.2 as an accepted risk with a named owner, so it is a decision on the record rather than an oversight. *Currently empty.*                 |
| 12. Glossary (`12-glossary.md`)                                   | Should gain: top out, block out, lock out, completed run, final summary, play again. *Currently empty.*                                                                               |

## 10. Open Questions

| #   | Question                                                                                                   | Owner      | Status   | Resolution                                                                                                                                                                                                                                                       |
| --- | ------------------------------------------------------------------------------------------------------------ | ---------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | Should *play again* warn the player that an unsubmitted run will be lost?                                    | Daan Harms | Resolved | **No prompt** (FR-013). It would contradict 001's EC-8, and a confirmation dialog on the hottest path in the game is worse than occasionally losing a run the player chose to walk away from. Story 009 may make its submission affordance the visually primary one. |
| 2   | Which key triggers *play again*?                                                                             | Daan Harms | Resolved | **`R`** (for retry), reachable by keyboard alone per FR-012. `Enter` is deliberately left free for story 009's name submission so the two never fight. Story 014 documents it. The exact binding is coordinated with 009 before implementation.                      |
| 3   | Should the game-over summary show the duration of the run, or pieces-per-minute?                             | Daan Harms | Resolved | **Duration is recorded, not shown.** The brief is score, lines, level and pieces. More numbers dilute the one the player came for. `duration` is on `CompletedRun` so a later story can surface it without a schema change.                                         |
| 4   | Should the game-over state link straight to the leaderboard?                                                 | Daan Harms | Resolved | **Not decided here.** Story 010 owns where the leaderboard is reached from and story 009 owns the submission flow that naturally leads there. 008 reserves no space and makes no assumption, so neither is constrained.                                              |
| 5   | Can the score reaching the leaderboard be trusted?                                                           | Daan Harms | Resolved | **No**, and that is accepted — see §9.2. It is computed client-side and is trivially forgeable. 008 states it as a constraint and hands the decision to accept or mitigate to **story 009**. No anti-cheat is designed here.                                        |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-007; US-002 -> SC-008, SC-009; US-003 -> SC-010, SC-011;
        US-004 -> SC-012..SC-014; US-005 -> SC-015..SC-018
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-004 -> US-001; FR-005, FR-006 -> US-002; FR-007..FR-010 -> US-003;
         FR-011..FR-013, FR-018 -> US-004; FR-014..FR-017 -> US-005)
  - [x] Domain model covers all entities mentioned in the requirements
        New: CompletedRun entity, TopOutReason enum, GameSession.endedAt and .completedRun.
        Reused verbatim from 001: GameSession, Playfield, ActivePiece, PieceQueue, Cell,
        CellPosition, TetrominoType, Orientation, GameStatus
  - [x] Domain rules and invariants are listed (DR-100 .. DR-108; DR-21, DR-22, DR-23, DR-27 cited from 001)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-16)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-009)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all five resolved)
-->
