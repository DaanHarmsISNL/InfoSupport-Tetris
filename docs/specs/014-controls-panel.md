# Feature Specification: Controls and How-to-Play Panel

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
| Feature ID      | 014                                                                                           |
| Status          | Draft                                                                                         |
| Author          | Daan Harms                                                                                    |
| Created         | 2026-10-20                                                                                    |
| Last updated    | 2026-10-20                                                                                    |
| Epic / Parent   | Look like Info Support (story map activity) — `docs/product/story-map.md`                     |
| Arc42 reference | Sections 3, 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders. |

> **This spec owns no keys.** Every binding it shows is defined elsewhere — `←`, `→`, `↓`, `↑`, `X`, `Z`
> and `Ctrl` by **spec 002 §5.3 / DR-29**, `Space` by **spec 003 / DR-49**, `C` and `Shift` by
> **spec 005 / DR-63**. 014 adds no binding, changes none, and makes none configurable. Its entire job is
> to *show* them, and its entire design problem is doing that **without creating a second copy that can
> drift out of step with the first** (DR-259).
>
> **Tokens.** Every colour, size and space comes from **spec 013 §5** by name. No literal value appears in
> this feature (per **No Literal Values, DR-142**).

### 1.1 Problem Statement

A colleague who has never played this build arrives at a board with pieces already falling and no idea
that `X` rotates, `Space` slams a piece down or `C` holds one for later. The bindings are not guessable —
three of the seven actions have two alternatives, and two of them (`Space`, `C`) have no on-screen hint at
all — so a first run is spent discovering the controls instead of playing. Worse, the bindings live in
three different specs: any on-screen list of them is a fourth copy that will be wrong the first time one
of those specs changes.

### 1.2 Goal

A new player sees, without asking for it and without stopping the game, exactly which keys do what —
all seven actions, both alternatives where there are two — alongside four lines telling them what they
are trying to achieve, how they score, that it gets faster and how it ends. The list is generated from
the same binding definitions the input handler dispatches on, so a binding can only be changed in one
place and the panel can never say something the game does not do. A returning player who already knows
the keys can fold the prose away once and not see it again.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec. Each is either a separate story or a
project-wide exclusion. Do not implement them here, and do not design around a guess at how they will
work — the owning spec defines that.

- **Changing, rebinding or customising any key.** The bindings are fixed by **DR-29**, **DR-49** and
  **DR-63**. This panel is strictly read-only: it has no settings, no key-capture control and no reset.
- **Defining or re-deriving a binding.** If a key is not in the authoritative set (§5.2), it does not
  appear here. 014 never *decides* a binding — a disagreement between this panel and the game is always
  a defect in the panel's source, never a new rule.
- **An interactive tutorial, a guided first game, or a practice mode.** The panel is a reference card
  that is read in ten seconds, not a lesson.
- **Animated or video demonstrations** of a rotation, a kick or a hold. Static text and key caps only;
  see also **Reduced Motion (DR-165)**.
- **A printable or downloadable cheat sheet.** The browser's own print works or it does not; nothing is
  built for it.
- **Localisation and translation.** One language, matching the rest of the application — see §10 row 10.
- **In-game hints, coaching, or advice on where to put the piece.** No "try rotating", no suggested
  placement, no ghost piece (out of scope project-wide).
- **Restating story 006's scoring table, story 007's gravity curve, or story 008's top-out conditions.**
  Those live in their owning specs. The panel summarises each in one line and cites it (DR-271).
- **Landing page layout, copy and hero.** **Story 012** owns the landing page. 014 supplies the legend it
  renders and nothing else.
- **The design tokens themselves.** **Story 013** owns every colour, type size and spacing value; 014
  consumes them by name and introduces none (DR-276).
- **Pause.** Out of scope project-wide (story map: "games are short by design"). This is the constraint
  that shapes the whole feature — see §1.4.
- **Accounts, login, server-side preferences.** Out of scope project-wide. The one preference this
  feature keeps is client-side and disposable (DR-273).
- **Mobile and touch support**, and therefore any documentation of touch gestures. Out of scope
  project-wide (laptop + keyboard only).

### 1.4 The shape of the problem: there is no pause

The obvious design — a *Controls* button that opens a modal — is wrong here, and it is worth saying why
before §3 assumes otherwise.

The story map drops **pause** deliberately. So a modal opened mid-game would sit over the board while the
piece kept falling behind it: the player reads two lines, loses the run, and learns that asking for help
is punished. Blocking the game instead would reintroduce pause through the back door, which the product
owner explicitly rejected.

The resolution is to **have nothing to open**. The panel is a permanent region of the game screen,
alongside the next-piece, hold and score panels — already anticipated by **013 §5.6**, which assigns the
"Controls / how-to-play panel" its surface and text tokens. A player glances at it; there is no state
transition, no overlay, no moment at which the game is or is not interrupted, and therefore nothing to get
wrong. Everything else in this spec follows from that single decision (DR-266), including the focus rule
in DR-267: if the panel is always on screen while the player is playing, it must be impossible for it to
swallow a keystroke meant for the falling piece.

## 2. User Stories

### US-001: Learn the keys without leaving the game

**As a** new player,
**I want** to see which key does what while I am looking at the board,
**so that** my first run is spent playing rather than guessing.

### US-002: Learn *both* ways to do the same thing

**As a** new player,
**I want** to see every key bound to an action, not just one of them,
**so that** I can rotate with `↑` if my hand is on the arrows or with `X` if it is not.

### US-003: Know what I am actually trying to do

**As a** new player who has not played Tetris in fifteen years,
**I want** a few lines telling me the goal, how points work, that it speeds up and how it ends,
**so that** I understand the game I am playing and not just the keys I am pressing.

### US-004: Not be told the same thing every game

**As a** returning player,
**I want** the explanatory prose to stay out of my way once I have read it,
**so that** the screen I look at for twenty minutes is not shouting beginner text at me.

### US-005: Read the panel with a keyboard and a screen reader

**As a** player using assistive technology,
**I want** the controls conveyed as text in a properly named region,
**so that** "the key with the arrow on it" is something I can actually find out.

### US-006: Change a binding in exactly one place

**As a** developer changing a key binding,
**I want** the panel to be generated from the binding definitions the input handler already uses,
**so that** I cannot ship a game whose on-screen help is a lie.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nnn) are defined in §5.6 of
this spec (DR-259 onward); rules DR-01 … DR-258 belong to specs 001–013 and are cited, never redefined._

| ID     | Requirement                                                                                                                                                                                                                      | Priority | User Story |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall render the control legend from the **same binding definitions the input handler dispatches on**, holding no second list of keys anywhere in the application (DR-259).                                             | Must     | US-006     |
| FR-002 | The system shall show exactly one legend entry per bound player action — move left, move right, soft drop, hard drop, rotate clockwise, rotate counter-clockwise, hold — and no entry for any key that is not bound (DR-260).       | Must     | US-006     |
| FR-003 | The system shall collapse the paired start/stop commands of a held key into the single player-facing action the player experiences, so `SoftDropStart` and `SoftDropEnd` appear as one row, *Soft drop* (DR-261).                   | Must     | US-001     |
| FR-004 | The system shall show **every** key bound to an action: rotate clockwise as `↑` and `X`, rotate counter-clockwise as `Z` and `Ctrl`, hold as `C` and `Shift` — never one alternative in place of the other (DR-263).                | Must     | US-002     |
| FR-005 | The system shall separate the alternative keys of one action with the word **"or"**, and shall never use a separator that could read as a simultaneous chord (DR-263).                                                             | Must     | US-002     |
| FR-006 | The system shall present legend entries in the fixed order given in §5.2 — move, drop, rotate, hold — so the panel's shape is stable between visits and between surfaces (DR-262).                                                 | Should   | US-001     |
| FR-007 | The system shall render each key as a key cap whose **text name** is available to the reader, with any glyph (`←`, `→`, `↑`, `↓`) carried as presentation alongside that name, never as its only form (DR-264).                     | Must     | US-005     |
| FR-008 | The system shall attach the short qualifying note to the two actions that need one — *Soft drop* "while held", *Hold piece* "once per piece" — and to no others (DR-270).                                                          | Should   | US-001     |
| FR-009 | The system shall render the panel as a **permanent region of the game screen**, fully visible throughout `GameStatus.Playing`, requiring no action to reveal and offering no action to dismiss (DR-266).                           | Must     | US-001     |
| FR-010 | The system shall leave the session untouched by anything the player does to the panel: no gravity step, lock delay, reset budget, DAS/ARR timer or level is altered by reading, expanding or collapsing it (DR-269).               | Must     | US-001     |
| FR-011 | The system shall route every key bound by DR-29, DR-49 and DR-63 to the game while `status` is `Playing`, **wherever focus sits on the page**, and shall not let such a key activate a focused control or enter text (DR-267).     | Must     | US-001     |
| FR-012 | The system shall render the panel as static, non-focusable content while `status` is `Playing`, so the panel contains nothing that can hold focus while a piece is falling (DR-268).                                               | Must     | US-001     |
| FR-013 | The system shall include a how-to-play summary of **at most four lines**, covering in order: the goal, scoring, the rising speed, and how the game ends (DR-270).                                                                  | Must     | US-003     |
| FR-014 | The system shall derive each how-to-play line from exactly one owning spec — goal and scoring from **006**, speed from **007**, ending from **008** — restating none of their tables, formulas or numbers (DR-271).                | Must     | US-003     |
| FR-015 | The system shall show the how-to-play summary **expanded** to any visitor with no stored preference (DR-272).                                                                                                                     | Must     | US-003     |
| FR-016 | The system shall offer a control to collapse and re-expand the how-to-play summary whenever `status` is **not** `Playing`, and shall record the chosen state client-side (DR-272, DR-273).                                        | Should   | US-004     |
| FR-017 | The system shall store that preference as a single boolean under one named client-side key, store nothing else, and send nothing to any server (DR-273).                                                                          | Must     | US-004     |
| FR-018 | The system shall treat a missing, unreadable, unwritable or unrecognised preference as **expanded** and shall continue to work normally — losing the preference degrades the experience, never the feature (DR-273).               | Must     | US-004     |
| FR-019 | The system shall keep the **key legend** visible at all times regardless of the stored preference; only the how-to-play prose collapses (DR-274).                                                                                 | Must     | US-001     |
| FR-020 | The system shall expose the panel as a region with the accessible name *Controls*, placed in reading order with the rest of the game chrome and reachable without a pointer (DR-275).                                              | Must     | US-005     |
| FR-021 | The system shall announce each legend entry as its action followed by its key names joined by "or" — e.g. "Rotate clockwise, Up arrow or X" (DR-264, DR-275).                                                                     | Must     | US-005     |
| FR-022 | The system shall show a `--focus-ring` indicator on the collapse control per **Focus Visibility (DR-150)**, at `--focus-ring-width` and `--focus-ring-offset`.                                                                    | Must     | US-005     |
| FR-023 | The system shall draw the panel entirely from spec 013's semantic tokens (§5.5 of this spec), introducing no new colour, type size, spacing or radius value (DR-276, 013 FR-030).                                                  | Must     | US-005     |
| FR-024 | The system shall render the same legend, from the same source, on the landing page (story 012) as on the game screen (DR-259).                                                                                                    | Must     | US-001     |
| FR-025 | The system shall provide no means to change, clear or capture a binding anywhere in the panel (DR-265).                                                                                                                           | Must     | US-006     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash._

### SC-001: Every bound action appears exactly once (FR-002)

```gherkin
Given the controls panel is on screen
When the legend is read
Then it contains exactly seven entries
  And they are: move left, move right, soft drop, hard drop, rotate clockwise,
      rotate counter-clockwise, hold piece
  And no entry appears twice
```

### SC-002: A changed binding reaches the panel with no separate edit (FR-001)

```gherkin
Given rotate clockwise is bound to the Up arrow and X
When spec 002's binding definition for rotate clockwise is changed to the Up arrow and V
  And no file belonging to the controls panel is edited
Then the panel shows "Up arrow or V" for rotate clockwise
  And the panel no longer mentions X anywhere
```

### SC-003: A newly bound action appears without touching the panel (FR-001, FR-002)

```gherkin
Given the authoritative binding set contains seven actions
When an eighth action with a key is added to that set
  And no file belonging to the controls panel is edited
Then the panel shows eight entries
  And the eighth entry carries the new action's label and key names
```

### SC-004: A key the game does not handle never appears (FR-002)

```gherkin
Given a key is listed in the panel
When that key is pressed during play
Then the game performs the action the panel names for it
  And there is no key in the panel that the game ignores
  And there is no key the game acts on that the panel omits
```

### SC-005: Both alternatives are shown for every action that has two (FR-004)

```gherkin
Given the controls panel is on screen
When the rotate clockwise, rotate counter-clockwise and hold entries are read
Then rotate clockwise shows both the Up arrow and X
  And rotate counter-clockwise shows both Z and Ctrl
  And hold piece shows both C and Shift
  And the four single-key actions show exactly one key each
```

### SC-006: Alternatives do not read as a chord (FR-005)

```gherkin
Given an action with two bound keys
When its entry is read
Then the two key caps are joined by the word "or"
  And they are not joined by "+", by "/" or by adjacency alone
  And a reader cannot conclude that both keys must be pressed together
```

### SC-007: The panel is already there — there is nothing to open (FR-009)

```gherkin
Given a game is in progress with a piece falling
When the player looks for the controls
Then the panel is already fully visible beside the board
  And there is no button, link or key that opens or closes it
  And no part of the board, next-piece panel or score panel is covered by it
```

### SC-008: Reading the panel does not stop the piece (FR-010)

```gherkin
Given a game is in progress at level 1 with a piece in free fall
When the player spends five seconds reading the controls panel and presses nothing
Then the piece has descended five cells
  And gravity, the lock delay and the level are exactly as they would have been
  And at no point was the session paused, slowed or suspended
```

### SC-009: A bound key never activates a focused control (FR-011)

```gherkin
Given a game is in progress
  And keyboard focus is on a focusable control elsewhere on the game screen
When the player presses Space
Then the active piece hard drops
  And the focused control is not activated
  And the page does not scroll
```

### SC-010: The player cannot type into the panel while a piece is falling (FR-012)

```gherkin
Given a game is in progress
When the player presses Tab repeatedly through the whole game screen
Then focus never lands inside the controls panel
  And every subsequent key press reaches the falling piece
```

### SC-011: A first-time visitor gets the full explanation (FR-015)

```gherkin
Given a visitor whose browser holds no stored preference for this application
When the controls panel is first shown
Then the how-to-play summary is expanded
  And all four lines are readable without any interaction
```

### SC-012: A collapsed summary stays collapsed (FR-016, FR-017)

```gherkin
Given the player collapses the how-to-play summary before starting a game
When the player returns to the application in the same browser the next day
Then the how-to-play summary is collapsed
  And the key legend is fully visible
```

### SC-013: A lost preference falls back to the beginner's view (FR-018)

```gherkin
Given the player previously collapsed the how-to-play summary
When the browser's stored data is cleared, or storage is unavailable or refuses to be written
Then the panel is shown with the summary expanded
  And no error is surfaced to the player
  And the game is fully playable
```

### SC-014: Collapsing never hides the keys (FR-019)

```gherkin
Given the how-to-play summary is collapsed
When a different colleague sits down at the same laptop and starts a game
Then all seven key legend entries are visible without any interaction
  And only the four lines of prose are hidden
```

### SC-015: A screen reader hears the action and the key names (FR-007, FR-021)

```gherkin
Given a player using a screen reader
When they read the rotate clockwise entry
Then they hear "Rotate clockwise, Up arrow or X"
  And they hear no entry announced merely as an unlabelled graphic or symbol
```

### SC-016: The arrow glyph is never the only carrier (FR-007)

```gherkin
Given the move left entry
When images and icon fonts are disabled, or the entry's text is extracted
Then the key is still identifiable as the Left arrow key
  And the same holds for the right, up and down arrow entries
```

### SC-017: Focus on the collapse control is visible (FR-022)

```gherkin
Given no game is in progress
When the player moves keyboard focus onto the how-to-play collapse control
Then a focus indicator in --focus-ring is drawn at --focus-ring-width with --focus-ring-offset
  And it meets the 3:1 floor of DR-148 against both the control and the surface behind it
```

### SC-018: The panel introduces no new design values (FR-023)

```gherkin
Given the implemented controls panel
When its styles are inspected
Then every colour, font size, spacing and radius resolves to a token published in spec 013 §5
  And no literal colour, size or spacing value appears
```

### SC-019: Landing page and game screen agree (FR-024)

```gherkin
Given the legend shown on the landing page
  And the legend shown beside the board during play
When the two are compared
Then they list the same actions, in the same order, with the same keys and the same labels
  And they were produced from the same binding definitions
```

### SC-020: Scoring is summarised, not restated (FR-014)

```gherkin
Given the how-to-play summary
When the scoring line is read
Then it conveys that clearing several rows at once is worth disproportionately more
  And it contains no points table, no per-line values and no multiplier formula
  And spec 006 remains the only place those numbers appear
```

## 5. Domain Model

_Vocabulary from specs 001–013 is reused verbatim: `GameSession`, `GameStatus`, `ActivePiece`,
`KeyBinding`, `InputCommand` and the `--*` tokens all mean exactly what their owning spec says they mean._

### 5.0 How to read this section

The model here is **derived data**, not stored data. There is one durable record in the whole feature —
a single boolean preference (§5.1) — and everything else is a projection of binding definitions that
already exist in specs 002, 003 and 005.

That distinction is the whole point. A `ControlBinding` is not a row somebody types into a template; it
is what you get when you take an entry from the authoritative binding set and ask it *"what should a
human be told about you?"*. If the authoritative set changes, every projection changes with it, because
there is nowhere else for the panel to get its content from (DR-259).

### 5.1 Entities

#### ControlLegend

The ordered, complete projection of the authoritative binding set into something a player can read.
It exists **once** and is rendered by both surfaces that need it — the landing page (story 012) and the
game screen — so the two can never disagree (FR-024).

| Attribute  | Type                   | Constraints                                                      | Description                                                            |
| ---------- | ---------------------- | ---------------------------------------------------------------- | ------------------------------------------------------------------------ |
| entries    | ordered list of `ControlBinding` | required, non-empty, one per `PlayerAction`, no duplicates | The rows of the legend, in `displayOrder` (DR-260, DR-262).             |
| source     | reference              | required, the authoritative binding set                          | The same definitions the input handler dispatches on. Not a copy (DR-259). |

`ControlLegend` has **no** editable state, no per-player variation and no construction parameters.
Two renders of it on the same build are identical by construction.

#### ControlsPanelPreference

The one piece of player state in this feature: whether the how-to-play prose is folded away. Held
client-side in the player's own browser, never transmitted, never associated with a person (there are no
accounts), and entirely disposable (DR-273).

| Attribute          | Type    | Constraints                           | Description                                                                 |
| ------------------ | ------- | ------------------------------------- | ----------------------------------------------------------------------------- |
| howToPlayCollapsed | boolean | required when present, default `false` | `true` once the player has folded the prose away. Absent until they do.      |

- Stored under **one** named key. Nothing else about the player, the panel or the session is stored.
- Absent, unreadable, unwritable, or holding any value that is not recognisably boolean ⇒ treated as
  `false`, i.e. **expanded** (FR-018, DR-273).
- It governs the **prose only**. The seven key entries are never affected by it (DR-274).

### 5.2 Value Objects

#### ControlBinding

One row of the legend: an action, the key or keys that perform it, a short human label, and an optional
qualifying note. Immutable, and derived — never authored.

| Attribute    | Type                      | Constraints                                        | Description                                                                 |
| ------------ | ------------------------- | --------------------------------------------------- | ----------------------------------------------------------------------------- |
| action       | `PlayerAction`            | required, unique within the legend                  | What the player is doing. One per legend entry (DR-260).                     |
| keys         | ordered list of `KeyCap`  | required, 1–2 entries, no duplicates                | Every key bound to the action — **all** of them (DR-263).                    |
| label        | string                    | required, max 32 chars, sentence case               | The short human name, e.g. "Rotate clockwise".                               |
| note         | string                    | optional, max 20 chars                              | A qualifier the player needs to not be surprised, e.g. "while held".         |
| displayOrder | integer                   | required, unique, stable                            | Position in the legend (DR-262).                                             |

**The legend content.** This table *shows* the derived result for the current binding set; it is not a
second definition of it. The `keys` column is reproduced from **002 §5.3 `KeyBinding`**, **003 DR-49** and
**005 DR-63** and is normative there, not here (DR-259).

| displayOrder | action                     | label                     | keys              | note            | Defined by      |
| ------------ | -------------------------- | ------------------------- | ----------------- | --------------- | --------------- |
| 1            | `MoveLeft`                 | Move left                 | `←`               | —               | 002, DR-29      |
| 2            | `MoveRight`                | Move right                | `→`               | —               | 002, DR-29      |
| 3            | `SoftDrop`                 | Soft drop                 | `↓`               | while held      | 002, DR-29      |
| 4            | `HardDrop`                 | Hard drop                 | `Space`           | —               | 003, DR-49      |
| 5            | `RotateClockwise`          | Rotate clockwise          | `↑` **or** `X`    | —               | 002, DR-29      |
| 6            | `RotateCounterClockwise`   | Rotate counter-clockwise  | `Z` **or** `Ctrl` | —               | 002, DR-29      |
| 7            | `Hold`                     | Hold piece                | `C` **or** `Shift`| once per piece  | 005, DR-63      |

The order is movement, then the two drops, then the two rotations, then hold: easiest to hardest, and the
two rotations adjacent so the clockwise/counter-clockwise pairing is visible at a glance (DR-262).

Two notes and no more. *"While held"* matters because `↓` behaves unlike every other key — it is a state,
not an event (**DR-39**) — and *"once per piece"* matters because a player who presses `C` twice and sees
nothing will otherwise think hold is broken (**DR-68**). The other five actions do exactly what their
label says, and a note on each would turn a reference card into a manual.

#### KeyCap

A single physical key as the player must be able to perceive it. The separation of `name` from `glyph` is
what makes DR-264 enforceable.

| Attribute | Type   | Constraints                                  | Description                                                                      |
| --------- | ------ | -------------------------------------------- | ---------------------------------------------------------------------------------- |
| name      | string | required, non-empty, max 16 chars            | The key's name **as text** — the accessible, readable form. Never omitted.        |
| glyph     | string | optional                                     | A symbol shown in place of the name where one is conventional. Presentation only. |

| action                   | name(s)                  | glyph(s)   |
| ------------------------ | ------------------------ | ---------- |
| `MoveLeft`               | "Left arrow"             | `←`        |
| `MoveRight`              | "Right arrow"            | `→`        |
| `SoftDrop`               | "Down arrow"             | `↓`        |
| `HardDrop`               | "Space"                  | —          |
| `RotateClockwise`        | "Up arrow", "X"          | `↑`, —     |
| `RotateCounterClockwise` | "Z", "Ctrl"              | —, —       |
| `Hold`                   | "C", "Shift"             | —, —       |

A `KeyCap` with a `glyph` renders the glyph visually and carries `name` as its text alternative, so the
same cap reads as "←" to a sighted player and "Left arrow" to a screen reader. A `KeyCap` with no glyph
renders `name` directly. **There is no third case**: a cap that renders a glyph and exposes no name is a
defect (DR-264).

#### HowToPlayNote

One line of the summary. Four of them, each answering a different question and each owned by exactly one
other spec.

| Attribute  | Type    | Constraints                       | Description                                                   |
| ---------- | ------- | --------------------------------- | --------------------------------------------------------------- |
| order      | integer | required, 1–4, unique             | Reading order.                                                 |
| text       | string  | required, max 160 chars           | The line as the player reads it.                               |
| owningSpec | string  | required, one of `006`, `007`, `008` | The spec whose behaviour this line summarises (DR-271).     |

**Proposed copy.** This is the testable part of *"so I know which keys do what"*'s companion half, so it
is written here rather than left to the implementer:

| order | owningSpec | text                                                                                                 |
| ----- | ---------- | ------------------------------------------------------------------------------------------------------ |
| 1     | 006        | Fill a row all the way across and it clears, dropping everything above it down.                        |
| 2     | 006        | Every cleared row scores — and clearing several at once is worth far more than clearing them one by one. |
| 3     | 007        | The more lines you clear, the higher your level and the faster the pieces fall.                        |
| 4     | 008        | The game ends when there is no room left for the next piece. You will be shown your final score.       |

Four lines, 480 characters, no numbers. Line 2 deliberately says "far more" rather than the Single /
Double / Triple / Tetris values: those are **006**'s and restating them here creates exactly the drift
problem this spec exists to avoid, one category up from keys (DR-271).

### 5.3 Enumerations

#### PlayerAction

What the player is doing, as the player understands it. This is the **display projection** of spec 002's
`InputCommand`, and it is deliberately not the same enum.

| Member                   | Corresponding `InputCommand` / owning rule     | Why it differs                                                     |
| ------------------------ | ------------------------------------------------ | -------------------------------------------------------------------- |
| `MoveLeft`               | `MoveLeft`                                       | 1:1.                                                                |
| `MoveRight`              | `MoveRight`                                      | 1:1.                                                                |
| `SoftDrop`               | `SoftDropStart` **and** `SoftDropEnd`            | Two commands, one action. A player holds a key; they do not "start" and "end" a soft drop (DR-261). |
| `HardDrop`               | Hard drop (003, DR-49)                           | 1:1. 003 models it as an edge trigger, not an `InputCommand`.        |
| `RotateClockwise`        | `RotateClockwise`                                | 1:1.                                                                |
| `RotateCounterClockwise` | `RotateCounterClockwise`                         | 1:1.                                                                |
| `Hold`                   | Hold (005, DR-63)                                | 1:1.                                                                |

The mapping is **total in both directions**: every dispatchable command belongs to exactly one
`PlayerAction`, and every `PlayerAction` is dispatchable. That totality is what SC-004 tests, and it is
the property that breaks loudly — rather than silently — if someone adds a binding and forgets the panel
(DR-260).

### 5.4 Relationships

- A **ControlLegend** has exactly one **ControlBinding** per **PlayerAction** (one-to-one, total).
- A **ControlBinding** has one or two **KeyCaps**, ordered; a `KeyCap` belongs to exactly one binding.
- A **ControlBinding** is *derived from* one entry of the authoritative binding set owned by spec 002,
  003 or 005. It never outlives, contradicts or predates that entry (DR-259).
- The **controls panel** renders one **ControlLegend** and the four **HowToPlayNotes**. Both the landing
  page and the game screen render the same `ControlLegend` instance (FR-024).
- A **ControlsPanelPreference** governs the visibility of the **HowToPlayNotes** only. It has no
  relationship to the **ControlLegend** at all — which is the structural reason DR-274 cannot be violated
  by accident.
- The panel has **no** relationship to `GameSession`. It reads no session state and writes none, which is
  why FR-010 holds trivially.

### 5.5 Token consumption

Per **013 §5.6**, which already allocates this surface. Consumed by name; nothing new is introduced
(DR-276). Cited as *per **DR-nnn — Name*** where a rule governs the choice.

| Element                    | Tokens                                                                      | Governing rule                          |
| -------------------------- | ----------------------------------------------------------------------------- | ----------------------------------------- |
| Panel background           | `--surface-raised`, `--border-subtle` edge, `--radius-md`                    | 013 §5.6; **Elevation** (§5.4 of 013)    |
| Panel heading ("Controls") | `--font-size-h3`, `--text-primary`                                           | **Type Roles (DR-159)**                  |
| Action labels              | `--font-size-body`, `--text-secondary`                                       | **Body Text Contrast (DR-146)** — 9.08:1 |
| Notes ("while held")       | `--font-size-label`, `--text-secondary`                                      | **Minimum Text Size (DR-164)** — 14px floor |
| Key caps                   | `--text-primary` on `--surface-raised`, `--border-strong` edge, `--radius-sm` | 013 §5.6; **Non-Text Indicator Contrast (DR-148)** — 3.55:1 |
| The word "or"              | `--font-size-body`, `--text-secondary`                                       | **Body Text Contrast (DR-146)**          |
| How-to-play prose          | `--font-size-body`, `--text-secondary`                                       | **Body Text Contrast (DR-146)**          |
| Collapse control focus     | `--focus-ring`, `--focus-ring-width`, `--focus-ring-offset`                  | **Focus Visibility (DR-150)**            |
| All spacing and gaps       | `--space-1` … `--space-4`                                                    | **Spacing Scale Exclusivity (DR-144)**   |
| Collapse transition        | `--duration-fast`, `--easing-standard`                                       | **Reduced Motion (DR-165)**              |

Key caps sit on `--surface-raised` with a `--border-strong` edge rather than a filled background: a fill
would need a new colour token, and 013 publishes none for this. The border carries the "this is a key"
affordance at 3.55:1, clears DR-148, and survives forced-colours mode intact for the same reason
**Block Edge (DR-156)** does (DR-168).

No `--brand-primary` anywhere in this panel. It is reference material, not a call to action, and brand
colour spent on a static list is brand colour not available to the landing page's primary action.

### 5.6 Domain Rules and Invariants

_This spec owns the block **DR-259 – DR-288**. Rules DR-01 – DR-258 belong to specs 001–013; they are
cited here, never redefined. Later work should cite these by ID and name, e.g. "per **Single Binding
Source (DR-259)**"._

#### The single source of truth

- **DR-259 — Single Binding Source**: The panel is **generated from the same binding definitions the
  input handler dispatches on**. There is no second list of keys — not in a view, not in a resource file,
  not in a constant, not in prose. A binding can therefore be changed in exactly one place, and any change
  to **DR-29**, **DR-49** or **DR-63** reaches the panel with no edit to the panel. This is the rule the
  whole feature exists to make true; a hard-coded key name anywhere in the presentation layer is a defect
  even if it is currently correct.
- **DR-260 — Legend Completeness**: The legend's set of actions and the game's set of dispatchable
  actions are **equal** — every bound action is shown exactly once, and nothing is shown that the game
  does not act on. Neither direction is allowed to drift: an unshown binding leaves the player guessing,
  and a shown non-binding is a documented lie. The equality is a set comparison, which makes it testable
  without rendering anything.
- **DR-261 — Action Granularity**: The legend's unit is the **player action**, not the dispatched
  command. Where one key produces a start/end command pair because it is held — soft drop's
  `SoftDropStart` / `SoftDropEnd` under **DR-39** — the two collapse into one legend entry carrying the
  note "while held". The mapping from `InputCommand` to `PlayerAction` is total and many-to-one, so
  DR-260's equality still holds.
- **DR-262 — Stable Display Order**: Legend entries render in `displayOrder` (§5.2) on every surface and
  every visit. The order never depends on the player, the session, storage, or the order the bindings
  happen to be defined in. A player who has used the panel once knows where to look the next time.
- **DR-265 — Read-Only Panel**: The panel displays bindings and nothing more. It offers no control that
  changes, clears, captures or restores a binding, and it writes nothing to the authoritative binding set.
  Rebinding is out of scope project-wide (**DR-29**: the bindings "are fixed, not configurable").

#### Showing a key

- **DR-263 — Alternatives Shown Together**: An action with more than one bound key shows **all** of them
  in one entry, joined by the word **"or"** — `↑` or `X`, `Z` or `Ctrl`, `C` or `Shift`. Showing one and
  omitting the other is a violation, as is choosing between them on the player's behalf. The separator is
  the literal word: `+` means a chord, `/` and bare adjacency are ambiguous, and a player who concludes
  they must hold `Z` *and* `Ctrl` has been actively misled.
- **DR-264 — Key Name Is Text**: Every key is conveyed as its **text name**. A glyph (`←`, `→`, `↑`, `↓`)
  may be shown in addition, as presentation, and never instead. A key rendered only as an arrow symbol, an
  icon font or an image is unreadable to a screen reader and unguessable to anyone who cannot see it, and
  is a defect regardless of how it looks. Key caps carry keyboard semantics, so assistive technology can
  announce "key" rather than leaving a bare letter floating in the text.

#### Living alongside a game that cannot be paused

- **DR-266 — Always Present, Never Opened**: The panel is a **permanent region of the game screen**,
  fully visible throughout `GameStatus.Playing`. It is not a modal, a drawer, an overlay or a tooltip, and
  no key or control opens or closes it. This follows directly from the project-wide absence of **pause**:
  anything the player must *open* mid-game either covers a board whose piece is still falling or stops a
  game the product owner decided never stops. Having nothing to open removes the choice.
- **DR-267 — Game Keys Outrank Page Focus**: While `status` is `Playing`, every key bound by **DR-29**,
  **DR-49** and **DR-63** is handled by the game wherever keyboard focus sits on the page, and its default
  action is suppressed per **DR-42**. Such a key never activates a focused control, never toggles a
  disclosure and never enters a character into a field. `Space` on a focused button hard drops the piece
  and does not press the button. The player can never be "typing into the panel while their piece drops",
  because while a piece is dropping those keys belong to the piece. Keys the game does not bind are
  unaffected and behave normally — `Tab`, `Enter` and browser shortcuts all still work.
- **DR-268 — No Focusable Controls During Play**: While `status` is `Playing` the panel renders as static,
  non-focusable content. Its only interactive affordance — the how-to-play collapse control — is present
  only when no game is in progress. DR-267 already makes a stray keystroke harmless; this rule makes it
  impossible, and keeps the Tab order during play down to the controls a player actually needs.
- **DR-269 — The Panel Never Touches the Session**: Nothing the player does to the panel alters
  `GameSession` in any way — not gravity (**DR-14**), not the lock delay (**DR-16**), not the reset budget
  (**DR-17**), not DAS/ARR timers (**DR-31**), not the level (**007**). Reading, expanding and collapsing
  are presentation events with no game consequence. There is no pause, no slow-motion and no grace period,
  by design and not by omission.

#### Content and the returning player

- **DR-270 — Reference Card Scope**: The panel's content is bounded: the legend, at most four
  `HowToPlayNote` lines, and qualifying notes on only those actions that would otherwise surprise the
  player. No strategy, no history, no glossary, no worked example. If it cannot be read in ten seconds it
  is no longer the thing the story asked for.
- **DR-271 — Every Fact Cites Its Owner**: Each `HowToPlayNote` summarises exactly one other spec (006,
  007, 008) and reproduces none of its tables, formulas or numbers. A line may say that clearing several
  rows at once is worth far more; it may not say *how much* more, because then 006's table would exist in
  two places and DR-259's problem would reappear one category up. When an owning spec changes, its line is
  reviewed — a four-row list of owners is what makes that review possible.
- **DR-272 — Expanded By Default**: A visitor with no stored preference sees the how-to-play summary
  **expanded**. The story's actor is a new player, and a new player who has to find and press something to
  discover what the game is has already been failed. Collapsing is an opt-out a returning player chooses
  once, never a default anyone inherits.
- **DR-273 — The Preference Is Disposable**: The collapsed state is a single boolean held in client-side
  browser storage under one named key. Nothing else is stored; nothing is transmitted; no account, session
  or server record exists to hold it (story map). Absent, unreadable, unwritable, corrupt or unrecognised
  ⇒ **expanded**, silently, with no error shown and no loss of function. Private browsing, a cleared cache
  and a different machine all simply return the player to the beginner's view, which is the safe direction
  to fail in.
- **DR-274 — Bindings Are Never Hidden**: `ControlsPanelPreference` governs the four prose lines and
  nothing else. The seven key entries are visible in every state, to every player, with no interaction.
  This is what makes the stored preference safe on a shared office laptop: the worst a stale preference
  can do to the next colleague is withhold four lines they can also read on the landing page — it can
  never leave a new player without the keys (cf. **011 DR-207**, which rejected browser storage outright
  because *its* stale value would have shown a stranger's row as someone's own run; the asymmetry in
  consequence is the reason the decisions differ).

#### Presentation

- **DR-275 — Named, Reachable Region**: The panel is exposed as a region with the accessible name
  *Controls*, placed in the document's reading order beside the rest of the game chrome and reachable
  without a pointer. Each entry is announced as its label followed by its key names joined by "or"
  ("Rotate clockwise, Up arrow or X"). The legend is structured as rows of action-and-keys, so it can be
  navigated as a list rather than read as one run-on paragraph.
- **DR-276 — Tokens Only**: Every colour, font size, weight, spacing, radius, border width and duration
  in this panel is a semantic token published by **spec 013 §5**, consumed by name, per **No Literal
  Values (DR-142)** and **Token Tiering (DR-139)**. The panel introduces no new token and reads no
  `--is-*` primitive. If this feature appears to need a value 013 does not publish, that is a change to
  013, not a literal written here.

## 6. Non-Functional Requirements

_Only what is specific to this feature. Project-wide quality requirements belong in arc42 §10, and the
contrast, type and viewport floors belong to **spec 013 §5.7** — both are cited here, not duplicated._

| ID      | Category        | Requirement                                                                                                                                                                                                                 |
| ------- | --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance     | The panel contributes **zero work per frame**. Its content is fixed for the lifetime of the page, so it is produced once and never re-rendered while `status` is `Playing`. A profile of a 60-second session shows no panel work in the game loop.                 |
| NFR-002 | Usability       | A player who has never seen the game can name the key for every one of the seven actions **within 10 seconds** of first seeing the panel, measured on five colleagues who have not read this spec.                            |
| NFR-003 | Maintainability | Changing a binding — adding, removing or swapping a key — requires editing **exactly one** definition and **zero** files belonging to the panel, on either surface. This is verified by the SC-002 test, not by inspection.   |
| NFR-004 | Accessibility   | The panel meets **WCAG 2.1 AA** under 013's floors: body text ≥ 4.5:1 (**DR-146**), key-cap borders ≥ 3:1 (**DR-148**), focus visible (**DR-150**), nothing below 14px (**DR-164**). Every key is announced by name (DR-264). |
| NFR-005 | Accessibility   | The panel is fully usable with a screen reader and with the keyboard alone, and remains readable with CSS disabled, with images blocked, and under forced-colours mode (**DR-168**).                                          |
| NFR-006 | Layout          | At the minimum viewport of **1024 × 640** (**DR-166**) the panel fits beside the board and the other chrome panels without horizontal scrolling and **without forcing `--cell-size` below 24px** — the board does not shrink to make room for help text.            |
| NFR-007 | Privacy         | The feature transmits **zero bytes**. It adds no field to any request, no cookie, and exactly one client-side boolean (DR-273). No analytics record that the panel was read or collapsed.                                     |
| NFR-008 | Reliability     | Every storage operation is failure-tolerant: a throw, a quota rejection, a disabled-storage browser or a corrupt value resolves to "expanded" and never propagates an error to the player or the game loop (DR-273).          |
| NFR-009 | Content         | The whole panel — heading, seven entries, two notes and four prose lines — stays under **700 characters**, so it remains a reference card and cannot quietly grow into a manual (DR-270).                                     |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                              | Expected Behavior                                                                                                                                                                    |
| ----- | ------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | A binding is changed in spec 002, 003 or 005 and no panel file is touched.                             | The panel shows the new key. This is the feature working, not a lucky outcome (DR-259). It has a dedicated regression test — SC-002.                                                   |
| EC-2  | A new action with a key is added to the authoritative set.                                             | An eighth legend entry appears, carrying the new label and keys (DR-260). If the new action has no `label`, the build fails rather than rendering a blank row — `label` is required.    |
| EC-3  | An action is **unbound** — its key is removed entirely.                                                | Its entry disappears from the legend. A `ControlBinding` with zero `keys` is invalid (§5.2) and must not render as an action with no way to perform it.                                 |
| EC-4  | Browser storage is unavailable (private mode, disabled, quota exhausted, throws on write).             | The panel renders expanded and the collapse control still works for the current page view; the choice simply is not remembered (DR-273, NFR-008). Nothing is surfaced to the player.    |
| EC-5  | The stored preference holds a value that is not a boolean — `"yes"`, `null`, a stale object, garbage.  | Treated as absent ⇒ expanded (DR-273). The bad value is overwritten on the next deliberate collapse; it is never partially parsed.                                                     |
| EC-6  | A colleague collapsed the summary; a different, new colleague then sits at the same laptop.            | They see the collapsed summary but the **full key legend** (DR-274). They are never left without the bindings. This is the bounded failure that made the storage decision acceptable.   |
| EC-7  | The player presses `Space` while keyboard focus is on a button on the game screen, mid-game.           | The piece hard drops; the button is **not** activated; the page does not scroll (DR-267, **DR-42**). The classic way a help panel eats a keystroke, and it must have a test.            |
| EC-8  | The player presses `Tab` repeatedly during play.                                                       | Focus moves through the game screen's genuinely interactive controls and never enters the panel (DR-268). Game keys keep reaching the piece throughout (DR-267).                        |
| EC-9  | The player holds `Shift` while tabbing (reverse tab order) during play.                                | Focus moves backwards normally **and** a hold fires if one is available, per **005 EC-13** — the hold binding is on the key itself. Expected; the panel changes nothing about it.      |
| EC-10 | The player collapses or expands the summary between games, then starts a game.                         | The session starts normally. No timer, gravity value or level is affected (DR-269). The panel's state is irrelevant to the game.                                                       |
| EC-11 | The player's browser blocks the web font (**DR-162**).                                                 | Key caps and labels render in the fallback stack and stay legible and correctly sized. Key caps are bordered boxes, not glyphs from a font, so nothing becomes unidentifiable.          |
| EC-12 | Forced-colours mode or a high-contrast OS theme is active.                                             | Key caps remain distinguishable because their affordance is a **border**, which forced-colours preserves, not a fill, which it discards (**DR-168**, same reasoning as **DR-156**).     |
| EC-13 | Reduced motion is requested.                                                                           | The collapse happens instantly with no transition (**DR-165**). No information is lost — the end state already carries it. Gameplay timing is untouched.                                |
| EC-14 | The viewport is at the 1024 × 640 minimum with the game, score, next and hold panels all present.      | The controls panel fits without scrolling and without pushing `--cell-size` below 24px (NFR-006, **DR-166**). If it cannot, the panel's layout yields — the board never does.           |
| EC-15 | Story 012's landing page has not been built yet.                                                       | The game screen's panel is complete and correct on its own. The landing page consumes the same `ControlLegend` when it arrives; 014 requires no change at that point (§10 row 9).      |
| EC-16 | The page is printed.                                                                                   | Whatever the browser does is acceptable. No print stylesheet, no cheat-sheet layout — explicitly a Non-Goal (§1.3).                                                                     |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                             |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| SUC-001 | All 20 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                        |
| SUC-002 | A test asserts **set equality** between the legend's actions and the game's dispatchable actions, and fails if either side gains or loses one (DR-260).                   |
| SUC-003 | Changing a binding in its owning spec's definition and running the suite produces a panel showing the new key with **zero** edits to any panel file (NFR-003, SC-002).   |
| SUC-004 | A search of the presentation layer finds **no** hard-coded key name, glyph or key list outside the authoritative binding definitions (DR-259).                            |
| SUC-005 | Five colleagues who have never played this build each name the key for all seven actions within 10 seconds of first seeing the panel (NFR-002).                          |
| SUC-006 | An automated accessibility pass over the game screen and the landing page reports zero violations attributable to the panel, and every key cap exposes a text name.       |
| SUC-007 | A 60-second recorded session shows the panel doing no work in the game loop and the piece descending exactly as it would with the panel absent (NFR-001, SC-008).         |
| SUC-008 | With storage disabled, cleared, and populated with each of five malformed values, the panel renders expanded and the game is fully playable in all seven cases (NFR-008). |
| SUC-009 | All 18 domain rules in §5.6 are each covered by at least one automated test.                                                                                            |
| SUC-010 | The panel is built entirely from spec 013's published tokens, adding no colour, type size or spacing value — satisfying **013 SC-022 / SUC-007** for this story.          |

## 9. Dependencies and Constraints

### 9.1 Dependencies

014 is almost entirely downstream. It defines no behaviour of its own beyond presentation, and it cannot
be correct unless the specs below are.

- **Spec 002 (move, rotate, soft drop)** — the authoritative source for `←`, `→`, `↓`, `↑`, `X`, `Z` and
  `Ctrl` (§5.3 `KeyBinding`, **DR-29**), for `InputCommand`, and for **DR-42**'s default-action
  suppression that DR-267 builds on. 002 §1.3 already assigns this documentation duty to 014: *"the
  bindings in DR-29 are fixed. Showing the player what they are is story 014."*
- **Spec 003 (hard drop)** — the authoritative source for `Space` (**DR-49**, which states "the binding is
  fixed; rebinding is out of scope and story 014 documents the key").
- **Spec 005 (hold)** — the authoritative source for `C` and either `Shift` (**DR-63**). 005 §9.1 names
  014 as the owner of documenting that binding to the player.
- **Spec 013 (brand styling)** — publishes every token this panel consumes, and **§5.6 already allocates
  this surface**: `--surface-raised` background, `--text-secondary` body, `--text-primary` key caps,
  `--border-strong` on key caps. 014 adds nothing to that allocation.
- **Specs 006, 007, 008** — the owners of the three facts the how-to-play lines summarise (DR-271). If any
  of them changes its player-visible behaviour, the corresponding line is reviewed.
- **Spec 012 (landing page)** — renders the same `ControlLegend` on the landing page (FR-024). 012 owns
  that page's layout, copy and language; 014 owns only the legend it embeds. **012 did not exist when
  this spec was written** — see §10 row 9 for exactly what is assumed and what is not.
- **Spec 001** — `GameSession`, `GameStatus` and the game screen this panel is a region of. 001 §1.3 and
  §10 row 2 both point forward to this panel.
- **No downstream dependents.** 014 is the last story in the map. Nothing is waiting on it.

### 9.2 Constraints

- **No pause** (story map). The single hardest constraint in this feature and the reason for DR-266: the
  panel cannot be something the player opens, because opening it either covers a live board or stops a
  game that is not allowed to stop.
- **No accounts, no sessions, no server-side player state** (story map). The one remembered preference is
  therefore client-side, anonymous and disposable (DR-273). There is nowhere else it could live.
- **Fixed bindings** (**DR-29**, **DR-49**, **DR-63**). Rebinding is not a deferred feature; it is not a
  feature. The panel is read-only by rule, not by omission.
- **Keyboard and laptop only** (story map). No touch gestures exist, so none are documented. The panel
  describes a keyboard because there is nothing else to describe.
- **Minimum viewport 1024 × 640** (**DR-166**). The panel competes for width with the board, the
  next-piece panel, the hold panel and the score panel. NFR-006 settles the contest in the board's favour.
- **Single language.** The application ships in one language, matching story 012. Localisation is a
  Non-Goal — see §10 row 10.
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed here): ASP.NET
  Core 10 Razor Pages, shared layout at `Pages/Shared/_Layout.cshtml`, static assets under `wwwroot/`.
  Whether the legend is produced server-side or client-side is an architecture decision (§9.3), and DR-259
  holds either way.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*, consistent with how specs 001–013 treat them.

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                                 |
| ----------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 03. System Scope and Context (`03-system-scope-and-context.md`)  | The panel adds no external interface and no new actor. It does introduce the one piece of **client-side persisted state** in the whole product, which belongs on the context boundary. *Currently empty.* |
| 05. Building Block View (`05-building-block-view.md`)            | Should show the authoritative binding set as a building block **shared** by the input handler and the presentation layer, with the dependency pointing one way only. That shape is what makes DR-259 structural rather than a convention. *Currently empty.* |
| 06. Runtime View (`06-runtime-view.md`)                          | The panel has no runtime scenario during play — it is static (NFR-001). The only scenario worth recording is read-preference → render → write-preference, and its failure path (DR-273). *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)      | Two concerns here are cross-cutting, not 014-specific: **keyboard input ownership while playing** (DR-267 — bound keys outrank focused controls anywhere on the page) and **failure-tolerant client-side storage** (DR-273). Both should be stated once, project-wide. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`)      | Warrants ADRs for: a persistent panel over a modal, given no pause (DR-266); generating player-facing documentation from the executable binding set (DR-259); and using browser storage here after 011 rejected it (DR-274, §10 row 4). *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)          | Should host the project-wide **WCAG 2.1 AA** commitment that NFR-004 and NFR-005 instantiate; 013 §6 already assumes it. *Currently empty.*                                                |
| 12. Glossary (`12-glossary.md`)                                  | Should gain: control legend, key cap, player action (as distinct from input command), how-to-play note. *Currently empty.*                                                                 |

## 10. Open Questions

_Every question raised while writing this spec was resolved. None is left open._

| #   | Question                                                                                                       | Owner      | Status   | Resolution                                                                                                                                                                                                                                                                                      |
| --- | ---------------------------------------------------------------------------------------------------------------- | ---------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | A modal *Controls* dialog, or a permanent panel?                                                               | Daan Harms | Resolved | **Permanent panel** (DR-266). With no pause, a modal opened mid-game covers a board whose piece is still falling — the player is punished for asking for help — and blocking the game instead reintroduces pause through the back door. Having nothing to open removes the choice entirely. 013 §5.6 already allocates a panel surface for exactly this. |
| 2   | Show one key per action, or every alternative?                                                                 | Daan Harms | Resolved | **Every alternative** (DR-263). Three of seven actions have two bindings. Picking one would make the panel incomplete *and* arbitrary: `↑` suits a hand on the arrows, `X` suits a hand on `Z`/`X`, and neither is the right default for everyone.                                             |
| 3   | How do two alternatives render so the pairing is unambiguous?                                                  | Daan Harms | Resolved | **Joined by the literal word "or"**, both caps on one row (DR-263). `+` reads as a chord, `/` and bare adjacency are ambiguous, and a player who thinks they must hold `Z` *and* `Ctrl` has been misled by the panel that was supposed to help them. "or" also reads correctly aloud.           |
| 4   | Is the collapsed state remembered, given 011 rejected browser storage outright?                                | Daan Harms | Resolved | **Yes, and the two decisions are consistent.** 011's **DR-207** rejected storage because a stale value there would label a *stranger's* row as someone's own run — a wrong statement about another person. Here a stale value withholds four lines of prose that are also on the landing page, while the keys stay visible unconditionally (DR-274). Same mechanism, incomparable consequences. |
| 5   | Default state for a visitor with no stored preference?                                                         | Daan Harms | Resolved | **Expanded** (DR-272). The story's actor is a *new* player; a new player who must find and press something before learning what the game is has already been failed. Collapsing is an opt-out a returning player chooses once.                                                                 |
| 6   | Can the key legend be collapsed too?                                                                           | Daan Harms | Resolved | **No** (DR-274). The keys are the story — "so I know which keys do what". Letting them be hidden means a colleague at a shared laptop can inherit a screen with no bindings on it, which is precisely the failure this feature exists to prevent. Only the prose folds.                        |
| 7   | Is the collapse control available during play?                                                                 | Daan Harms | Resolved | **No** (DR-268). A focusable control on screen while a piece falls is a keystroke waiting to be swallowed. DR-267 already makes that harmless; removing the control makes it impossible and keeps the in-play Tab order to the controls a player actually needs. The choice is made between games, which is when a player makes it anyway. |
| 8   | What happens to keystrokes when something on the game screen has focus?                                        | Daan Harms | Resolved | **The game takes them** (DR-267). While `Playing`, every key bound by DR-29/DR-49/DR-63 is handled by the game wherever focus sits, with its default action suppressed per **DR-42** — `Space` hard drops and does not press a focused button. This is stated as a general rule because 001's *New game* button already creates the collision, panel or no panel. |
| 9   | Story 012 (landing page) did not exist when this spec was written. What does 014 assume about it?              | Daan Harms | Resolved | **Only that it exists and embeds the legend** (FR-024). 014 asserts nothing about 012's layout, copy, hero or vocabulary beyond reusing `ControlLegend` from the same source. The game-screen panel is complete and correct without 012 (EC-15). If 012 contradicts anything here, 012 wins on landing-page presentation and 014 wins on binding content. |
| 10  | Should the panel be localisable?                                                                               | Daan Harms | Resolved | **No** (§1.3). One language, matching 012 and the rest of the application. The audience is one office; a translation layer for ~700 characters is cost without a reader. Note that **key names** are content, not chrome: localising would mean translating "Left arrow", which is exactly the kind of second copy DR-259 forbids. |
| 11  | Do the arrow glyphs appear at all, given the accessibility requirement?                                        | Daan Harms | Resolved | **Yes, alongside the text name — never instead of it** (DR-264). `←` is instantly recognisable to a sighted player and worthless to a screen reader, so the `KeyCap` carries both: glyph for presentation, `name` for meaning. A cap that renders a glyph and exposes no name is a defect.      |
| 12  | Which actions get a qualifying note, and does the note belong here or in 002/005?                              | Daan Harms | Resolved | **Two notes, defined here as display data** (DR-270). *"While held"* on soft drop, because `↓` is a state not an event (**DR-39**); *"once per piece"* on hold, because a player pressing `C` twice to no effect will think it is broken (**DR-68**). The *behaviours* stay owned by 002 and 005; only their one-word summaries are display data. A note on all seven would make this a manual. |
| 13  | Does the panel need a token 013 does not publish — a key-cap fill, for instance?                               | Daan Harms | Resolved | **No** (DR-276). Key caps use `--text-primary` on `--surface-raised` with a `--border-strong` edge at 3.55:1, which clears **DR-148** and survives forced-colours mode because it is a border rather than a fill (**DR-168**, mirroring **DR-156**). A fill would have required a new token and a new contrast verification for no gain. |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001, SC-007..SC-010, SC-014, SC-019; US-002 -> SC-005, SC-006;
        US-003 -> SC-011, SC-020; US-004 -> SC-012, SC-013;
        US-005 -> SC-015..SC-018; US-006 -> SC-002, SC-003, SC-004
  - [x] Each functional requirement traces to a user story
        (FR-001, FR-002, FR-025 -> US-006; FR-003, FR-006, FR-008..FR-012, FR-019, FR-024 -> US-001;
         FR-004, FR-005 -> US-002; FR-013..FR-015 -> US-003; FR-016..FR-018 -> US-004;
         FR-007, FR-020..FR-023 -> US-005)
  - [x] Domain model covers all entities mentioned in the requirements
        Entities ControlLegend, ControlsPanelPreference; value objects ControlBinding, KeyCap,
        HowToPlayNote; enum PlayerAction. No key, label or token is defined here that is not
        derived from spec 002, 003, 005 or 013.
  - [x] Domain rules and invariants are listed (DR-259 .. DR-276, inside this spec's DR-259–DR-288 block)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-16 — binding drift, storage
        failure, focus theft, shared laptop, blocked fonts, forced colours, minimum viewport)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-009)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all 13 resolved)
-->
