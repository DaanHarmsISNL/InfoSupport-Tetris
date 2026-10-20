# Feature Specification: Submit a Score — Name Entry and the Leaderboard Entry

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

| Field           | Value                                                                                           |
| --------------- | ------------------------------------------------------------------------------------------------- |
| Feature ID      | 009                                                                                                 |
| Status          | Draft                                                                                               |
| Author          | Daan Harms                                                                                          |
| Created         | 2026-10-20                                                                                          |
| Last updated    | 2026-10-20                                                                                          |
| Epic / Parent   | Compete (story map activity) — `docs/product/story-map.md`                                          |
| Arc42 reference | Sections 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 — see §9.3. All arc42 files are currently empty placeholders. |

> **Note on this spec.** 009 opens the *Compete* activity. It is the **foundational spec for Compete** in
> exactly the way [`001-start-new-game.md`](001-start-new-game.md) is foundational for *Play Tetris*: the
> `LeaderboardEntry` entity, the `PlayerName` value object and the submission contract defined in **§5** are
> normative for **story 010** (view the leaderboard) and **story 011** (highlight the just-finished run).
> Those specs must reuse these names and rule IDs verbatim rather than re-invent them.
>
> **Vocabulary.** Everything from [`001 §5`](001-start-new-game.md#5-domain-model) and
> [`008 §5`](008-game-over.md#5-domain-model) is reused verbatim and never restated — in particular
> `GameSession`, `GameStatus`, `CompletedRun` and `TopOutReason`. `score` and `linesCleared` are defined by
> [`006 §5`](006-line-clears-and-scoring.md#5-domain-model); `level` by
> [`007 §5`](007-levels-and-speed.md#5-domain-model). This spec reads all four and defines none of them.
> Cited rules are named by ID, e.g. "per **Fresh Session State (DR-23)**".
>
> **Rule numbering.** New rules introduced here run from **DR-109** to keep IDs globally unique across
> concurrently written specs. DR-01 – DR-108 belong to specs 001 – 008 and are only ever cited.
>
> **This spec is the system's first boundary crossing.** Up to and including 008, the whole product lives on
> one page and writes nothing anywhere (008 NFR-008). 009 is where a byte first leaves the browser, where the
> first personal datum is collected, and where the first thing outlives a page load. §6, §7 and §9.2 are
> therefore weightier here than in any earlier spec, and deserve to be.

### 1.1 Problem Statement

A run now ends properly — 008 freezes the board, shows the final figures and hands over an immutable
`CompletedRun` — but the moment the player closes the tab, the best game they have ever played evaporates.
There is nothing to brag about on Monday, because there is nowhere to put the number. The story map's whole
premise ("turning a five-minute coffee break into office bragging rights") depends on a run being claimable
by name and surviving beyond the page that produced it, and today nothing is.

### 1.2 Goal

At game over the player can type a display name and submit the run they have just finished, in one action,
with no account and nothing else to fill in. The submission produces exactly one durable `LeaderboardEntry`
holding that name and the run's figures — one entry per submitted run, never two, no matter how many times
the button is pressed or how badly the network behaves. The entry survives a page reload, an application
restart and a redeploy, and comes back with a stable identifier so story 011 can point at it and say *that
one is yours*. A player who does not want to be on the board simply does not submit, and loses nothing but
the run.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec. Each is either a separate story, a project-wide
exclusion, or an architecture decision. Do not implement them here, and do not design around a guess at how
they will work — the owning document decides.

- **Rendering the leaderboard** — **story 010**. 009 writes entries and confirms the write. It draws no
  table, no ranking, no rank number and no "you are 4th" message. Ordering, paging, how many entries are
  shown and where the board is reached from are all 010's (DR-132).
- **Highlighting the just-finished run on the board** — **story 011**. 009's obligation to 011 is to return
  a stable `LeaderboardEntry.entryId` on success (DR-119) and nothing more.
- **Accounts, registration, login, persisted identity, "my profile", "my history"** — out of scope
  project-wide (story map). A name typed at game over is a **display label, not an identity** (DR-112).
  Nothing links two runs submitted by the same person, and nothing ever will within this product.
- **Editing or deleting an entry, renaming yourself, or an admin moderation screen.** Entries are immutable
  once created (DR-118) and this feature removes nothing (§9.2).
- **Profanity, slur or content filtering of names.** A deliberate, stated trade-off (DR-113), not an
  oversight — see §9.2.
- **The classic three-initials arcade convention.** The story says "type my name" and 010 shows "each name";
  a free-text display name is the product decision (open question 2).
- **Anti-cheat of any kind** — no score signing, no replayed input stream, no server-side simulation, no
  rate limiting, no proof of work, no CAPTCHA. 008 §9.2 records that the score is computed client-side and is
  trivially forgeable; 009 **accepts** that constraint (§9.2) and does only the one cheap, proportionate
  thing: sanity validation of the submitted figures (DR-123). Anything beyond that is explicitly excluded.
- **Choosing the storage technology.** *That entries are durable* is a requirement of this spec (FR-021,
  DR-121). *Where and how they are stored* — relational database, file, table storage, anything else — is an
  **architecture decision**, deferred to an ADR in arc42 §09 exactly as 001 deferred its browser-versus-server
  question (001 open question 1). Nothing in §3 – §5 changes whichever way it goes.
- **Weekly resets, seasons, challenges, streaks, per-level or per-team boards** — out of scope project-wide
  (story map: "the leaderboard is all-time high score only"). There is exactly one board (DR-122).
- **Recomputing, adjusting, bonusing or rounding any figure.** The figures are copied from the
  `CompletedRun` as 008 froze them (**Summary Fidelity (DR-104)**, DR-120). 009 is a courier, not an auditor.
- **Re-entering gameplay figures by hand.** The player types a name. Nothing else about the run is typed,
  typeable or editable (DR-120).
- **Submitting a run that is still in progress, or an abandoned one.** A submission requires a
  `CompletedRun`, which 008 creates only at a genuine top out (**Completed Run Provenance (DR-103)**).
- **The game-over presentation, the final summary and *play again*** — **story 008**. 009 adds a name field
  and a submit action to a screen 008 already owns, and must not break 008's FR-011 – FR-013.
- **Info Support branding and the visual design of the name field** — **story 013**.
- **The controls / how-to-play panel** — **story 014**, which documents the keys named in FR-012.
- **Mobile, touch and on-screen keyboards** — out of scope project-wide (laptop + keyboard only).

## 2. User Stories

### US-001: Put my name on the board

**As a** player who has just finished a good run,
**I want** to type my name at game over and submit the score in one action,
**so that** the run becomes something my colleagues can see instead of a number that disappears with the tab.

### US-002: Walk away without submitting

**As a** player whose run was embarrassing,
**I want** to go straight to *play again* without entering anything,
**so that** a bad game costs me nothing and nobody has to see it.

### US-003: Not lose my score to a flaky network

**As a** player on office Wi-Fi,
**I want** to be told clearly if the submission fails and be able to try again without replaying,
**so that** my best run is not thrown away silently by a dropped request.

### US-004: Not end up on the board twice

**As a** player with an impatient trigger finger,
**I want** one finished run to produce exactly one entry however many times I press submit,
**so that** I am not sitting at positions 3, 4 and 5 with the same score and looking like a cheat.

### US-005: Find my score still there tomorrow

**As a** colleague,
**I want** the leaderboard to be shared by everyone and to survive restarts and deployments,
**so that** "all-time high score" means something and Monday's bragging rights are still there on Friday.

### US-006: Keep obvious nonsense off the board

**As the** colleague who maintains this game,
**I want** submissions whose figures are impossible to be refused,
**so that** a fat-fingered or casually tampered request cannot park `999999999` at the top of a board nobody
can clean up.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) defined in this spec are
listed in §5.5; rules DR-01 – DR-108 are defined in specs 001 – 008 and are only cited here._

| ID     | Requirement                                                                                                                                                                                                                                                 | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall offer a name field and a submit action whenever a `CompletedRun` is available (008 FR-015), and at no other time.                                                                                                                              | Must     | US-001     |
| FR-002 | The system shall require a `PlayerName` of **1 to 20 characters** after normalisation, and shall refuse a submission whose name is empty or consists only of whitespace (DR-109, DR-111).                                                                        | Must     | US-001     |
| FR-003 | The system shall normalise the typed name before validating or storing it: Unicode NFC, leading and trailing whitespace removed, and every internal run of whitespace collapsed to one space (DR-110).                                                           | Must     | US-001     |
| FR-004 | The system shall refuse a name that still contains a control or format character after normalisation — including zero-width and bidirectional-override characters (DR-111).                                                                                      | Must     | US-001     |
| FR-005 | The system shall prevent a 21st character from being typed into the name field, and shall **refuse** rather than truncate an over-long name that reaches the server anyway (DR-111).                                                                             | Must     | US-001     |
| FR-006 | The system shall take the score, lines cleared and level from the `CompletedRun` (008 §5.1) and offer the player no way to type, edit or override any of them (DR-120).                                                                                          | Must     | US-001     |
| FR-007 | The system shall, on an accepted submission, create exactly one `LeaderboardEntry` carrying the attributes in §5.1 (DR-114).                                                                                                                                    | Must     | US-001     |
| FR-008 | The system shall confirm a successful submission to the player and return the created entry's `entryId` and stored `playerName` to the client (DR-119).                                                                                                         | Must     | US-001     |
| FR-009 | The system shall accept a name identical to one already on the board, creating a separate entry, and shall never merge, replace or deduplicate entries by name (DR-112, DR-114).                                                                                 | Must     | US-001     |
| FR-010 | The system shall accept a submission whose score is 0 on exactly the same terms as any other (DR-130).                                                                                                                                                         | Should   | US-001     |
| FR-011 | The system shall report a rejected name with a message naming the actual problem, keep the typed text in the field, and leave the run submittable (DR-127).                                                                                                      | Must     | US-001     |
| FR-012 | The system shall make the name field, the submit action and the skip action reachable and operable from the keyboard alone, with submit bound to `Enter` while the name field has focus, and shall **not** auto-focus the name field, so that 008's `R` play-again key is not swallowed as typed text (open question 3).                              | Must     | US-001     |
| FR-013 | The system shall let the player leave the game-over state without submitting, by an explicit skip action or simply by activating *play again* (DR-116, DR-117).                                                                                                  | Must     | US-002     |
| FR-014 | The system shall never block, delay, gate or confirm *play again* on the submission state of the finished run (DR-129, 008 FR-013).                                                                                                                             | Must     | US-002     |
| FR-015 | The system shall discard a declined run silently — no prompt, no warning, no "are you sure", and nothing written anywhere (DR-117).                                                                                                                             | Must     | US-002     |
| FR-016 | The system shall show that a submission is in progress from the moment it is started until it succeeds, fails or reaches its deadline, and shall never leave the player on an indefinite spinner (DR-128).                                                       | Must     | US-003     |
| FR-017 | The system shall abandon a submission that has not completed within **10 seconds** and treat it as failed (DR-128).                                                                                                                                             | Must     | US-003     |
| FR-018 | The system shall report a failed submission explicitly, keep the typed name and the `CompletedRun` intact, and offer a retry — a failure never silently discards the run (DR-127).                                                                               | Must     | US-003     |
| FR-019 | The system shall keep *play again* operable throughout a submission, including while one is in flight and after one has failed (DR-129).                                                                                                                       | Must     | US-003     |
| FR-020 | The system shall ignore a submit activation while a submission for the same run is already in flight (DR-125).                                                                                                                                                  | Must     | US-004     |
| FR-021 | The system shall create at most one `LeaderboardEntry` per `CompletedRun.runId`; a repeat submission of a run that already has an entry shall create nothing and shall return the existing entry as a success (DR-115, DR-126).                                   | Must     | US-004     |
| FR-022 | The system shall refuse to change a submitted run's entry, including when the player edits the name after a successful submission (DR-115, DR-118).                                                                                                             | Must     | US-004     |
| FR-023 | The system shall persist each `LeaderboardEntry` so that it survives a page reload, an application restart and a redeployment (DR-121).                                                                                                                         | Must     | US-005     |
| FR-024 | The system shall keep exactly one leaderboard, shared by every player of the application, holding every entry ever submitted with no reset, expiry, partition or cap (DR-122).                                                                                   | Must     | US-005     |
| FR-025 | The system shall treat a `LeaderboardEntry` as immutable once created — no attribute is ever updated by this feature or any later one (DR-118).                                                                                                                 | Must     | US-005     |
| FR-026 | The system shall validate the submitted figures **on the server** before creating an entry, rejecting values that are out of range or mutually inconsistent per DR-123, and shall create nothing when it rejects (DR-123).                                        | Must     | US-006     |
| FR-027 | The system shall record `submittedAt` from the server's own clock in UTC, ignoring any time supplied by the client (DR-124).                                                                                                                                    | Must     | US-006     |
| FR-028 | The system shall do no validation beyond DR-123 — no scoring simulation, no replay check, no rate limiting, no filtering of name content (DR-113, §9.2).                                                                                                        | Must     | US-006     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash.
"A finished run" means a `CompletedRun` exists and the game-over state is presented, per 008 FR-014 and
**Run Availability Window (DR-106)**._

### SC-001: Typing a name and submitting puts the run on the board (FR-001, FR-002, FR-007, FR-008)

```gherkin
Given a finished run with id R, a score of 12,400, 31 lines cleared and level 4
  And the name field is shown alongside the final summary
When the player types "Daan" and submits
Then exactly one leaderboard entry is created
  And it holds the name "Daan", a score of 12,400, 31 lines cleared and level 4
  And it holds a submittedAt timestamp and a stable entryId
  And the entryId is returned to the client
  And the player is shown that the submission succeeded
```

### SC-002: The figures come from the run, not from the player (FR-006)

```gherkin
Given a finished run with a score of 9,800
When the player inspects the submission form
Then the only editable field is the name
  And there is no field, control or affordance for the score, lines cleared or level
When the submission is accepted
Then the stored score is exactly 9,800, with no bonus, rounding or adjustment applied
```

### SC-003: A blank name is refused (FR-002, FR-011)

```gherkin
Given a finished run
When the player submits with the name field empty
Then no leaderboard entry is created
  And the player is told a name is required
  And the run is still available to submit
```

### SC-004: A whitespace-only name is refused (FR-002, FR-003, FR-011)

```gherkin
Given a finished run
When the player submits the name "      "
Then the name normalises to the empty string
  And no leaderboard entry is created
  And the player is told a name is required
```

### SC-005: Surrounding and internal whitespace is normalised (FR-003)

```gherkin
Given a finished run
When the player submits the name "   Jan    Willem   "
Then the stored name is exactly "Jan Willem"
  And the submission is accepted
```

### SC-006: Twenty characters are allowed and twenty-one are not (FR-002, FR-005)

```gherkin
Given a finished run
When the player types a 20-character name
Then it is accepted and stored in full
When the player attempts to type a 21st character into the field
Then the character is not accepted and the field still holds 20 characters
When a 21-character name is submitted directly to the server
Then it is refused outright and is not truncated to 20
  And no leaderboard entry is created
```

### SC-007: Invisible and control characters are refused (FR-004)

```gherkin
Given a finished run
When the player submits a name containing a zero-width space, a bidirectional override
     or any other control or format character
Then the submission is refused
  And no leaderboard entry is created
  And the player is told which kind of character is not allowed
```

### SC-008: Two colleagues with the same name both get an entry (FR-009)

```gherkin
Given the board already holds an entry named "Daan" with a score of 15,000
When a different colleague finishes a run scoring 8,000 and submits the name "Daan"
Then a second, separate entry named "Daan" with a score of 8,000 is created
  And the existing 15,000 entry is unchanged
  And neither entry is merged into, replaced by or hidden behind the other
```

### SC-009: The same person's two runs are two entries (FR-009)

```gherkin
Given a player has already submitted a run scoring 4,000 as "Daan"
When they play again, score 11,000 and submit as "Daan"
Then the board holds two entries named "Daan", one at 4,000 and one at 11,000
  And no "best score per name" collapsing has occurred
```

### SC-010: A zero score is submittable (FR-010)

```gherkin
Given a finished run with a score of 0
When the player submits the name "Nul"
Then an entry with a score of 0 is created and confirmed
  And the submission is treated no differently from any other
```

### SC-011: Skipping submits nothing (FR-013, FR-015)

```gherkin
Given a finished run with a score of 7,200
When the player activates the skip action
Then no leaderboard entry is created
  And nothing is sent to the server
  And no confirmation prompt or warning is shown
  And the player can start a fresh game
```

### SC-012: Playing again without submitting discards the run (FR-013, FR-014, FR-015)

```gherkin
Given a finished run that has not been submitted
When the player activates play again
Then a fresh session begins immediately, per Fresh Session State (DR-23)
  And no prompt, warning or blocking dialog is shown
  And the run is discarded and is no longer submittable
  And no leaderboard entry exists for it
```

### SC-013: A double-click produces one entry (FR-020, FR-021)

```gherkin
Given a finished run with id R and a valid name typed
When the player activates submit twice within 100 milliseconds
Then exactly one submission is sent
  And exactly one leaderboard entry exists for run R
  And the player sees one success confirmation, not two
```

### SC-014: A retry after a lost response produces one entry (FR-021)

```gherkin
Given a finished run with id R was submitted and the server created an entry with id E
  And the response was lost before reaching the browser, so the client reported a failure
When the player retries the submission for run R
Then no second entry is created
  And the submission is reported as a success
  And the entry returned is E, with the name as originally stored
```

### SC-015: Two simultaneous submissions of the same run collide safely (FR-021)

```gherkin
Given two submissions for the same run id R arrive at the server at the same moment
When both are processed
Then exactly one leaderboard entry exists for run R
  And both callers are told the submission succeeded
  And both are given the same entryId
  And neither caller is shown a conflict or duplicate error
```

### SC-016: A submitted run cannot be renamed (FR-022)

```gherkin
Given a finished run has been submitted successfully as "Daan"
When the player edits the name field to "Daniel" and submits again
Then the stored entry is still named "Daan"
  And no second entry is created
  And the player is told the run has already been submitted
```

### SC-017: A submission in flight is visible and bounded (FR-016, FR-017)

```gherkin
Given a finished run and a valid name
When the player submits and the server does not respond
Then the player can see that the submission is in progress
  And after 10 seconds the attempt is abandoned and reported as failed
  And the progress indication is gone
```

### SC-018: A failed submission loses nothing (FR-018)

```gherkin
Given a submission has failed because the browser is offline
When the player looks at the game-over screen
Then they are told the submission failed and that nothing has been saved
  And the typed name is still in the field
  And the final summary still shows the run's figures
  And a retry action is available
When connectivity returns and the player retries
Then the submission succeeds and one entry is created
```

### SC-019: The player is never trapped (FR-014, FR-019)

```gherkin
Given a submission for a finished run is in flight
When the player activates play again
Then a fresh session begins immediately
  And no prompt is shown
  And the outcome of the in-flight submission does not affect the new session
```

### SC-020: Entries survive a restart (FR-023)

```gherkin
Given three runs have been submitted
When the application is restarted and the page is reloaded
Then all three entries are still present and unchanged
When the application is redeployed to a new version
Then all three entries are still present and unchanged
```

### SC-021: There is one board for everyone (FR-024)

```gherkin
Given colleague A submits a run from one browser
  And colleague B submits a run from another browser on another machine
When either of them looks at the leaderboard
Then both entries are present on the same board
  And no entry has expired, been reset or been evicted to make room
```

### SC-022: An entry never changes after it is created (FR-025)

```gherkin
Given a leaderboard entry exists
When any attempt is made to change its name, score, lines cleared, level, runId or submittedAt
Then the attempt is rejected and the stored values stand
```

### SC-023: Impossible figures are refused (FR-026)

```gherkin
Given a submission claiming a score of 999,999,999 with 12 lines cleared at level 2
When the server validates it
Then the submission is refused because the score exceeds what 12 cleared lines can produce
  And no leaderboard entry is created
  And the player is told the submission was refused
```

### SC-024: An inconsistent level is refused (FR-026)

```gherkin
Given a submission claiming 31 lines cleared at level 9
When the server validates it
Then the submission is refused, because Level Derivation (DR-90) fixes level 4 for 31 lines
  And no leaderboard entry is created
```

### SC-025: Negative figures are refused (FR-026)

```gherkin
Given a submission claiming a score of -500, or -1 lines cleared, or level 0
When the server validates it
Then the submission is refused
  And no leaderboard entry is created
```

### SC-026: A legitimate high score is not refused (FR-026, FR-028)

```gherkin
Given a genuinely excellent run scoring 184,000 with 140 lines cleared at level 15
When the player submits it
Then the submission is accepted and the entry is created
  And no rate limit, challenge or additional check is applied
```

### SC-027: The server timestamps the entry (FR-027)

```gherkin
Given a submission arrives carrying a client-supplied timestamp of 1999-01-01T00:00:00Z
When the entry is created
Then submittedAt is the server's own UTC clock reading at the moment of creation
  And the client-supplied value is ignored entirely
```

### SC-028: Everything works from the keyboard (FR-012)

```gherkin
Given a finished run and no pointing device in use
When the player tabs to the name field, types a name and presses Enter
Then the submission is sent
When instead the player tabs to the skip action and activates it
Then the run is discarded and a fresh game can be started
```

## 5. Domain Model

_This is the normative vocabulary for the whole **Compete** activity. **Stories 010 and 011 must reuse these
names, attributes, enum members and rule IDs verbatim** rather than introduce competing ones — the same
contract 001 §5 imposes on 002 – 008._

_Everything from 001 §5 and 008 §5 is used unchanged and is not repeated here: `GameSession`, `GameStatus`,
`Playfield`, `CompletedRun`, `TopOutReason`. `score` and `linesCleared` are owned by
[006 §5.1](006-line-clears-and-scoring.md#51-entities); `level` by story 007 (**Level Derivation (DR-90)**,
**Level Cap (DR-94)**). This spec reads all three and defines none of them._

### 5.1 Entities

#### LeaderboardEntry

**The central entity of the Compete activity.** One durable, immutable record of one submitted run: a
display name and what that run was worth. It is the *only* thing in this product that outlives a page load,
and the only thing stories 010 and 011 read.

| Attribute     | Type             | Constraints                                                            | Description                                                                                                        |
| ------------- | ---------------- | ---------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| entryId       | UUID             | PK, server-generated, immutable, unique                                | **Stable identity of this entry.** Returned to the client on success (DR-119) and the value **story 011** matches on.  |
| playerName    | PlayerName       | required, normalised, 1–20 characters (§5.3, DR-109 – DR-111)          | The display label the player typed. **Not an identity** (DR-112). **Story 010** renders this.                          |
| score         | integer (64-bit) | ≥ 0, ≤ the DR-123 ceiling, immutable                                   | `CompletedRun.finalScore` as submitted. **Owned by story 006** — copied here, never computed (DR-120).                 |
| linesCleared  | integer          | ≥ 0, ≤ 9,999 (DR-123), immutable                                       | `CompletedRun.linesCleared` as submitted. **Owned by story 006.**                                                      |
| level         | integer          | 1–15, consistent with `linesCleared` per DR-123, immutable             | `CompletedRun.finalLevel` as submitted. **Owned by story 007.**                                                        |
| runId         | UUID             | required, **unique across all entries**, immutable                     | `CompletedRun.runId` (008 DR-107). The idempotency key that makes DR-115 enforceable. Not an identity (see note below). |
| submittedAt   | datetime (UTC)   | server-generated at creation, immutable                                | When the entry was created, by the server's clock (DR-124). The time of *submission*, not of the run.                   |

Deliberately **absent**, and not to be added by 010 or 011:

- **No rank.** Rank is a derived, moving property of the whole board at the moment it is read, not an
  attribute of an entry. **Story 010** derives it (DR-132).
- **No player, account or user reference.** None exists in this product. See DR-112.
- **No `piecesSpawned`, `duration`, `topOutReason`, `startedAt` or `endedAt`**, although `CompletedRun`
  carries all five (008 §5.1). A leaderboard answers "who, and how well" — none of these five help a reader
  rank or recognise a run, and every stored field is one more thing to keep forever (open question 5).
- **No board snapshot, replay or input log.** 008 §5.2 already excludes the `Playfield` from `CompletedRun`;
  nothing of the kind reaches storage here.
- **No IP address, user agent, browser fingerprint or session cookie.** See DR-131 and NFR-007.

> **`runId` is an idempotency key, not an identity.** It is generated in the browser (008 DR-107) and is
> therefore no more trustworthy than the score beside it (§9.2). It is stored, and constrained unique, so
> that DR-115 can be enforced structurally — not so that anything can be *inferred* from it. Story 011 must
> match on `entryId`, which the server generates and returns (DR-119); `runId` may corroborate but is never
> the authority.

#### ScoreSubmission

The player's in-progress attempt to submit one finished run. Transient and client-side: it lives exactly as
long as the game-over state does (**Run Availability Window (DR-106)**) and is never persisted. It exists in
the model so that the correctness rules around double-submission and retry (DR-115, DR-125, DR-126) have
something concrete to constrain.

| Attribute     | Type                | Constraints                                                      | Description                                                                                 |
| ------------- | ------------------- | ---------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| runId         | UUID                | required, immutable, equals `CompletedRun.runId`                 | Which run this attempt is for. Identical across every attempt and retry (DR-126).                 |
| draftName     | string              | 0–20 characters, raw as typed, hard-capped at 20 (DR-111)        | What is currently in the name field, **before** normalisation. Never stored (DR-110).             |
| status        | SubmissionStatus    | required, default `Unsubmitted`                                  | See the state machine below.                                                                      |
| attemptCount  | integer             | ≥ 0, starts at 0                                                 | Attempts sent. Retries increment it; the run still yields at most one entry (DR-115).             |
| entryId       | UUID?               | null unless `status` is `Submitted`; set once                    | The `LeaderboardEntry.entryId` the server returned (DR-119). Handed on to story 011.              |
| storedName    | PlayerName?         | null unless `status` is `Submitted`; set once                    | The name as the server stored it — **authoritative**, and what is confirmed to the player (DR-126).|
| lastFailure   | SubmissionFailure?  | null unless `status` is `Failed`                                 | Why the last attempt failed, and whether retrying is worth it (§5.3).                             |

**Lifecycle:**
`Unsubmitted` → (player submits) → `Submitting` → (server accepts, or returns the existing entry per DR-115)
→ `Submitted` **(terminal)**; or `Submitting` → (rejection, network failure or the DR-128 deadline) →
`Failed` → (player retries) → `Submitting`; or `Unsubmitted` / `Failed` → (player skips or plays again) →
`Declined` **(terminal)**.

`Submitted` and `Declined` are terminal: no transition leaves either (DR-115, DR-117). There is no path from
`Submitted` back to `Submitting`, which is what FR-022 and SC-016 assert. A submit activation while `status`
is `Submitting` is ignored outright (DR-125) — it is not queued, debounced or replayed afterwards.

### 5.2 Relationships

- A **CompletedRun** (008 §5.1) yields **at most one ScoreSubmission**, created when the game-over state is
  presented and discarded with it. A run the player never submits yields a `ScoreSubmission` that ends
  `Declined`, or none at all.
- A **ScoreSubmission** results in **at most one LeaderboardEntry** (DR-114), however many attempts it makes
  (DR-115, DR-126). `attemptCount` and the number of entries created are entirely independent.
- A **LeaderboardEntry** corresponds to **exactly one** `CompletedRun`, by `runId`, and **no two entries
  share a `runId`** (DR-115). The relationship is one-way: the entry holds a copy of the run's figures and
  the run holds no reference to the entry.
- A **LeaderboardEntry** holds **copies** of the run's figures, taken at submission. It is independent of the
  `CompletedRun`, the `GameSession` and everything else in 001's model from the instant it is created, and
  survives all of them (DR-121).
- A **LeaderboardEntry** has **no relationship to a player, an account or any other entry** — none of the
  three exists. Two entries sharing a `playerName` have **no** relationship whatsoever; they may well be two
  different colleagues (DR-112).
- The **leaderboard** is the set of all `LeaderboardEntry` records. There is exactly one such set, global to
  the application and shared by every player (DR-122). It is not an entity with attributes of its own — no
  owner, no period, no reset marker, no cap.
- A **LeaderboardEntry** holds exactly one **PlayerName** value object, by value. Changing one would require
  replacing the entry, which is not permitted (DR-118).

### 5.3 Value Objects

#### PlayerName

The normalised display label on an entry. Immutable, and meaningful only as text to be shown — never as a
key, a login, a lookup or a grouping (DR-112).

| Attribute | Type   | Constraints                                                                                                              |
| --------- | ------ | -------------------------------------------------------------------------------------------------------------------------- |
| value     | string | Unicode NFC; no leading or trailing whitespace; no internal whitespace run longer than one space; no control or format characters; **1–20** extended grapheme clusters (DR-110, DR-111) |

A `PlayerName` cannot be constructed from text that fails any of those constraints — that is the single
place FR-002 – FR-005 are enforced, on the client for immediate feedback and again on the server as the
authority. Equality is ordinal on `value`: two `PlayerName`s are equal when their normalised text matches
exactly. **That equality carries no meaning about people** (DR-112) and must not be used to group, match,
deduplicate or rank entries in story 010 or 011.

#### SubmissionFailure

Why one attempt failed, and what the player can do about it. Immutable; replaced wholesale by the next
attempt's outcome.

| Attribute | Type                     | Constraints              | Description                                                                               |
| --------- | ------------------------ | ------------------------ | --------------------------------------------------------------------------------------------- |
| reason    | SubmissionFailureReason  | required                 | Which of the categories in §5.4 applies.                                                        |
| message   | string                   | required, non-empty      | Plain language naming the *actual* problem, for the player to read (DR-127). Never a raw error. |
| retryable | boolean                  | required, derived        | Whether submitting again unchanged could succeed. Derived from `reason` per the §5.4 table.     |

### 5.4 Enumerations

#### SubmissionStatus

Where one `ScoreSubmission` has got to. Closed set.

| Member        | Meaning                                                                                                   |
| ------------- | ------------------------------------------------------------------------------------------------------------ |
| `Unsubmitted` | A finished run is available and nothing has been sent. The initial state of every `ScoreSubmission`.           |
| `Submitting`  | An attempt is in flight. Submit activations are ignored while in this state (DR-125). Bounded by DR-128.       |
| `Submitted`   | **Terminal.** An entry exists for this run; `entryId` and `storedName` are set. Reached by a fresh create *or* by DR-115's idempotent return of an entry that already existed. The two are indistinguishable to the player, by design. |
| `Failed`      | The last attempt did not produce an entry — as far as the client can tell. `lastFailure` says why. Retryable.   |
| `Declined`    | **Terminal.** The player skipped, or played again, without submitting. Nothing was sent and nothing was stored. |

#### SubmissionFailureReason

Closed set. The `Retryable` column feeds `SubmissionFailure.retryable`; the `Produced by` column matters,
because the client must not invent server reasons or vice versa.

| Member                       | Meaning                                                                                       | Retryable                    | Produced by     |
| ---------------------------- | --------------------------------------------------------------------------------------------- | ---------------------------- | --------------- |
| `NameMissing`                | Empty or whitespace-only after normalisation (DR-109).                                         | After correcting the name    | Client + server |
| `NameTooLong`                | More than 20 grapheme clusters after normalisation (DR-111).                                   | After correcting the name    | Client + server |
| `NameHasForbiddenCharacters` | A control or format character survived normalisation (DR-111).                                 | After correcting the name    | Client + server |
| `FiguresRejected`            | The figures failed DR-123's sanity validation. Nothing the player can do about it.             | No                           | Server only     |
| `RunUnavailable`             | No `CompletedRun` is available — the window closed (DR-106). Nothing to submit.                | No                           | Client only     |
| `AlreadySubmitted`           | This run is already `Submitted`; the client refuses to send again (FR-022, SC-016).            | No                           | **Client only** |
| `NetworkUnavailable`         | The request could not be sent or no response arrived.                                          | Yes                          | Client only     |
| `Timeout`                    | The DR-128 deadline of 10 seconds elapsed with no response.                                    | Yes                          | Client only     |
| `ServerError`                | The server answered, but with a failure it does not attribute to the submission.               | Yes                          | Server          |

> **`AlreadySubmitted` is a client-side guard only.** The *server* never reports it: a repeat submission of
> an already-submitted `runId` is answered as a **success** carrying the existing entry (DR-115), because the
> server cannot distinguish an impatient player from a retry after a lost response (SC-014) — and treating
> either as an error would be wrong. This asymmetry is deliberate; implementing it as a server-side conflict
> error would break SC-014 and SC-015.

### 5.5 Domain Rules and Invariants

_New rules introduced by this spec, numbered **DR-109 – DR-132** so that IDs stay globally unique across
concurrently written specs. DR-01 – DR-108 belong to specs 001 – 008 and are cited, never restated. Stories
010 and 011 should cite these by ID and name, e.g. "per **Entry Identity (DR-119)**"._

**The name**

- **DR-109 — Name Required**: A submission carries a `PlayerName`. A name that is absent, empty, or
  whitespace-only after **Name Normalisation (DR-110)** is refused with reason `NameMissing`. There is no
  anonymous entry, no default name, no "Player 1" and no auto-generated placeholder — if the player will not
  name themselves, the correct outcome is not to submit (DR-116).
- **DR-110 — Name Normalisation**: The typed text is normalised before it is validated, compared or stored,
  in this order: (1) Unicode **NFC** normalisation; (2) **trim** all leading and trailing whitespace,
  including non-ASCII whitespace; (3) **collapse** every internal run of whitespace to a single `U+0020`
  space. The normalised text is what is validated and what is stored — the raw `draftName` is never stored.
  Normalisation is applied identically on the client and on the server, and the server's result is
  authoritative. *Why collapse internal whitespace:* without it a name of `"A"` plus eighteen spaces plus
  `"B"` would stretch a row of story 010's board and shove the scores out of alignment, for no legitimate
  reason anyone has.
- **DR-111 — Name Bounds and Character Policy**: After DR-110 a `PlayerName` must be **1 to 20 characters**,
  counted in **Unicode extended grapheme clusters** — what the player perceives as characters, so that an
  emoji or a combining accent costs one, not two or four. It must contain **no control characters**
  (`U+0000`–`U+001F`, `U+007F`–`U+009F`) and **no Unicode format characters** (category `Cf`, which covers
  zero-width spaces, zero-width joiners and the bidirectional overrides `U+202A`–`U+202E` and
  `U+2066`–`U+2069`). Every other printable character is allowed, including letters of any script, digits,
  punctuation, emoji and internal spaces. Two separate obligations follow:
  - the name field **hard-caps input at 20** — a 21st character cannot be typed, so the limit is discovered
    while typing rather than at submission;
  - an over-long or forbidden-character name that reaches the server anyway is **refused, never truncated or
    silently cleaned**. Truncation would put a name on a shared, permanent board that the player never chose
    and cannot correct (DR-118). A refusal they can fix is strictly better than a mutilation they cannot.
- **DR-112 — A Name Is a Label, Not an Identity**: This product has no accounts and no identity (story map).
  `playerName` is text to display and nothing else. Specifically: two entries bearing the same name may be
  two different colleagues and **must never be assumed to be the same person**; nothing links two runs
  submitted by one person; names are never compared to authenticate, authorise, group, deduplicate or
  aggregate; and no "best score per name", "your personal best" or per-name collapsing may be introduced by
  this spec or by stories 010 and 011. A name is also not reserved — typing one does not claim it.
- **DR-113 — No Content Moderation**: Names are **not** filtered, screened, blocklisted, flagged or reviewed.
  This is an **accepted trade-off**, stated here so it is a decision on the record and not an oversight: the
  audience is a single office of colleagues on an internal game, social pressure is a sufficient control at
  that scale, and every automated filter brings false positives that reject legitimate names (not least
  colleagues' own surnames) along with a maintenance burden nobody has signed up for. The mitigation, if it
  is ever needed, is a human deleting a row — out of scope here (§9.2).

**One entry per run**

- **DR-114 — One Entry Per Submitted Run**: Every accepted submission creates exactly one
  `LeaderboardEntry` representing **one run**. Entries are **never** collapsed, merged or replaced on the
  basis of the name they carry. A player who submits ten runs appears ten times. *Why:* without accounts a
  name is not an identity (DR-112), so a "best score per name" board would silently merge strangers who
  happen to share a first name and destroy one of their scores. Showing every run is both the honest model
  and the simpler one. How many of them story 010 chooses to *display* is 010's business (DR-132); what is
  *stored* is every one.
- **DR-115 — Submit Once (Idempotent Submission)**: A given `CompletedRun.runId` yields **at most one**
  `LeaderboardEntry`, enforced structurally by a **uniqueness constraint on `LeaderboardEntry.runId`** and
  not by a client-side guard alone. A submission for a `runId` that already has an entry **creates nothing
  and is answered as a success**, returning the existing entry's `entryId` and `playerName`. It is never
  answered as a conflict or an error, because the server cannot tell an impatient second click from a retry
  after a lost response, and both deserve the same answer. Two concurrent submissions of the same `runId`
  therefore resolve to one entry and two identical success responses (SC-015) — the attempt that loses the
  race must read the winner's entry back and return it, never surface the constraint violation.
- **DR-116 — Submission Is Optional**: Submitting is never required and never implicit. A run reaches the
  board only by an explicit submit action on a valid name. Closing the tab, pressing *play again*, skipping,
  or doing nothing at all all leave the board untouched.
- **DR-117 — Declining Discards the Run Silently**: Skipping, or starting a new game without submitting,
  discards the `CompletedRun` with **no prompt, no warning, no confirmation and no "you will lose your
  score"**, and sends nothing anywhere. This follows 008 FR-013 and 001 EC-8: games are short by design, and
  a dialog on the hottest path in the product is worse than occasionally losing a run the player chose to
  walk away from.

**The entry**

- **DR-118 — Entry Immutability**: A `LeaderboardEntry` is immutable from creation. No attribute — name,
  score, lines, level, `runId` or `submittedAt` — is ever updated, by this feature or any later one. There is
  no edit, no rename and no correction path. A submitted name stands.
- **DR-119 — Entry Identity**: `entryId` is a server-generated, globally unique, stable identifier assigned
  at creation and **returned to the client on every successful submission**, including DR-115's idempotent
  return of an existing entry. It is the value **story 011** matches on to highlight the just-finished run,
  and the only identifier in this model the server vouches for. It is never reused, never reassigned and
  never derived from the name, the score or the `runId`.
- **DR-120 — Figures Are Copied, Never Typed**: `score`, `linesCleared` and `level` are copied from the
  `CompletedRun` as 008 froze them (**Summary Fidelity (DR-104)**). The player has no way to enter, edit or
  influence them, and this feature applies no bonus, penalty, rounding, rescaling or recomputation. The only
  thing the player contributes is the name.
- **DR-121 — Entry Durability**: Once a submission is confirmed, the entry survives a page reload, a browser
  restart, an application restart and a **redeployment of the application**. "All-time" in the story map's
  goal is taken literally. Confirming a submission that is held only in memory, in a browser, or in anything
  a deployment replaces, does not satisfy this rule. *Where* the entries live — relational database, file,
  table storage or anything else — is an **architecture decision deferred to an ADR in arc42 §09** (§9.3,
  open question 1), exactly as 001 deferred its browser-versus-server question. Nothing in §3 – §5 changes
  whichever way that decision goes.
- **DR-122 — One Shared All-Time Leaderboard**: There is exactly **one** leaderboard, global to the
  application and shared by every colleague who plays. It is never reset, cleared, rotated, archived or
  partitioned — no weekly boards, no seasons, no per-team or per-level boards (story map). It has no capacity
  cap: an entry is never evicted to make room for a better one, because eviction would make the stored data
  depend on display decisions that belong to story 010 (DR-132).

**Integrity, proportionately**

- **DR-123 — Proportionate Server-Side Validation**: Before creating an entry the **server** checks the
  submitted figures against what the rules of the game make reachable, and refuses the submission with
  `FiguresRejected` if any check fails. The checks are exactly these, and no others:
  1. `score`, `linesCleared` and `level` are integers; `score ≥ 0`, `linesCleared ≥ 0`, `level ≥ 1`.
  2. `linesCleared ≤ 9,999` and `level ≤ 15` (**Level Cap (DR-94)**).
  3. **Internal consistency**: `level = min(floor(linesCleared / 10) + 1, 15)`, exactly as
     **Level Derivation (DR-90)** defines it. This is deterministic, so a mismatch is always wrong.
  4. **Score ceiling**: `score ≤ 3,200 × linesCleared + 10,000`. Derived from the rules already specified:
     a cleared row is worth at most `200 × level ≤ 3,000` points (**Line Clear Score (DR-80)**, DR-94); a
     piece earns at most `2 × 39 = 78` drop points (**Drop Score (DR-81)**); and since each piece adds four
     cells, each cleared row removes ten, and the playfield holds 400 (**Playfield Geometry (DR-01)**), a run
     clearing `L` rows places at most `100 + 2.5L` pieces. Total: `3,195 × L + 7,800`, rounded up to the
     stated ceiling to leave headroom.

  These are **sanity checks, not anti-cheat**. They reject the *impossible*, never the merely impressive: a
  genuinely excellent run always passes (SC-026). A rejection is a defect report or a clumsy forgery, not a
  judgement on the player — which is why the message says the submission was refused and nothing more.
- **DR-124 — Submission Time Authority**: `submittedAt` is read from the **server's** clock in **UTC** at the
  moment the entry is created. Any timestamp supplied by the client is ignored entirely. Client clocks are
  wrong, time-zoned and trivially settable, and `submittedAt` is the one field a later ordering or display
  decision might lean on.
**Failure, retry and not being trapped**

- **DR-125 — Single In-Flight Submission**: While a `ScoreSubmission` is `Submitting`, further submit
  activations are **ignored** — not queued, not debounced into a later send, not replayed when the first
  completes. At most one request per run is ever in flight. This is the client-side half of DR-115; the
  uniqueness constraint is the half that actually guarantees correctness, because a client-side guard cannot
  survive a reload, a second tab, or a user with developer tools open.
- **DR-126 — Retry Safety**: A retry re-sends the **same `runId`**. Combined with DR-115 this makes retrying
  always safe: either no entry existed and one is created, or one existed and it is returned. `attemptCount`
  may grow without bound; the number of entries cannot exceed one. One consequence is knowingly accepted: if
  an attempt in fact succeeded but its response was lost, and the player edits the name before retrying, the
  retry returns the entry **as originally stored**, with the original name — the edit is silently not
  applied. The client therefore confirms using the server's `storedName`, not the field contents, so the
  player sees what is actually on the board. This is rare, harmless, and strictly better than the
  alternative, which is a second entry (open question 4).
- **DR-127 — No Silent Failure, No Dead End**: Every submission reaches a visible outcome. A failure or
  rejection is reported in plain language naming the actual problem, the typed name stays in the field, the
  `CompletedRun` and its summary stay intact, and — when the reason is retryable — a retry is offered. A
  failed submission never discards the run, never clears the form, never shows a raw error or status code,
  and never leaves the player with no way forward. At every moment, in every state, the player has at least
  two available actions: deal with the submission, or play again (DR-129).
- **DR-128 — Submission Deadline**: A submission that has not completed within **10 seconds** of being sent
  is abandoned, the progress indication is removed, and the attempt is reported as `Timeout`. The player is
  never left on an unbounded spinner. Abandoning is a *client-side* decision only: the server may well have
  committed the entry, which is exactly why the retry is idempotent (DR-115, DR-126).
- **DR-129 — Play Again Is Never Blocked**: *Play again* (008 FR-011) remains available and immediate in
  every submission state — `Unsubmitted`, `Submitting`, `Submitted`, `Failed` and `Declined` alike. It is
  never disabled, delayed, gated behind a submission, or guarded by a confirmation. If a submission is in
  flight when the player plays again, the new session starts at once and the submission's outcome has no
  effect on it whatsoever. This preserves 008 FR-013 and is the single most important thing this feature must
  not break: the product's core loop is "play → see the score → go again", and the leaderboard is a side
  path off it, never a toll gate across it.
- **DR-130 — Zero-Score Runs Are Submittable**: A run scoring 0 is submitted, validated, stored and
  displayed on exactly the same terms as any other (008 EC-7). There is no minimum score, no "not worth
  submitting" threshold and no suppression. Whether a zero ever appears on screen is story 010's display
  decision (DR-132), not a storage one.

**Personal data**

- **DR-131 — Minimal Personal Data**: The **only** personal datum this product collects is the name the
  player types, given knowingly and for publication on a board their colleagues will read. Nothing else about
  the player is collected, derived, stored or transmitted — no account, no email, no IP address, no user
  agent, no cookie, no fingerprint, no analytics identifier. The submission payload carries exactly the
  `PlayerName`, the three figures and the `runId`, and nothing more (NFR-007). 001's NFR-007 and 008's
  NFR-008 state that nothing had been collected up to this point; DR-131 states precisely what changes.

**What is not decided here**

- **DR-132 — Ranking and Presentation Are Deferred**: This spec defines what is **stored** and nothing about
  how it is **shown**. Sort order, tie-breaking, how many entries are listed, paging, rank numbering, date
  formatting, where the board is reached from and whether the game-over screen links to it are all **story
  010's**; highlighting the just-finished run is **story 011's**. Neither may change the storage model to suit
  a display choice — in particular, neither may introduce per-name collapsing (DR-112, DR-114) or eviction
  (DR-122). If 010 needs a stored attribute this spec does not provide, that is a change to this spec, agreed
  here, not a field invented there.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty);
001 §6 and 008 §6 continue to apply to the game itself._

| ID      | Category         | Requirement                                                                                                                                                                                                                                             |
| ------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance      | A submission is validated, stored and confirmed within **500 ms at p95** and **2 s at p99**, measured at the server, with **20 colleagues submitting concurrently** — a plausible peak for one office at coffee time.                                      |
| NFR-002 | Responsiveness   | The in-progress indication appears within **100 ms** of the submit activation, and the outcome is shown within **100 ms** of it being known. No submission is ever indicated for longer than the DR-128 deadline of 10 s.                                  |
| NFR-003 | Durability       | **Zero confirmed entries are lost.** A submission is confirmed only once the entry is durably stored; in a test that confirms 100 submissions and then restarts and redeploys the application, all 100 are present and unchanged afterwards (DR-121).       |
| NFR-004 | Correctness      | **Exactly one entry under any concurrency.** With 100 simultaneous submissions of one `runId`, exactly one `LeaderboardEntry` exists afterwards, all 100 callers receive a success carrying the same `entryId`, and no caller sees a conflict (DR-115).     |
| NFR-005 | Accessibility    | The name field has a programmatic label; validation messages are associated with it and announced by assistive technology; the success confirmation is announced; and the whole flow — focus the field, type, submit, or skip — works by keyboard alone. Focus is never trapped in the field, and the field is never auto-focused (FR-012). |
| NFR-006 | Usability        | The 20-character limit is discoverable **before** submitting, not only on rejection: the field shows the limit or the characters remaining. Every refusal message names the actual problem in plain language and never shows a status code or a raw error (DR-127). |
| NFR-007 | Security/Privacy | The submission payload carries exactly `runId`, `playerName`, `score`, `linesCleared` and `level` — nothing else (DR-131). It is sent over HTTPS. No IP address, user agent, cookie or fingerprint is stored with the entry, and names are not written to application logs. |
| NFR-008 | Security         | A `playerName` is stored **verbatim** and treated as untrusted text wherever it is shown. It is contextually escaped at render time and never interpolated into markup, a script, a URL, a header or a query. A name containing `<script>` appears on the board as those literal characters and does nothing. This binds story 010 too. |
| NFR-009 | Scalability      | Correctness and NFR-001 hold at **100,000 entries** — roughly a century of office play at the expected rate of tens of submissions a day. Nothing in the model degrades as the board grows, and nothing is ever evicted to keep it small (DR-122).           |
| NFR-010 | Testability      | The whole submission path is assertable headlessly, with an injectable server clock (DR-124) and a substitutable transport, so that double submission, concurrent submission, rejection, timeout and retry-after-lost-response are all testable without a real network or real elapsed time (001 NFR-006, 008 NFR-007). |

## 7. Edge Cases and Error Scenarios

_The double-submission and failure families below are the main correctness risk in this feature and are
enumerated deliberately — a plausible implementation gets the happy path right and every one of EC-3 to
EC-10 wrong._

| ID    | Scenario                                                                                              | Expected Behavior                                                                                                                                                                                       |
| ----- | ------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | The player double-clicks, or hammers `Enter`, on submit.                                                | The first activation sends; every activation while `Submitting` is ignored (DR-125). One request, one entry, one confirmation.                                                                                 |
| EC-2  | Submit is activated again after the first attempt **failed**.                                           | A new attempt is sent with the same `runId` (DR-126). `attemptCount` increments; the entry count cannot exceed one (DR-115).                                                                                   |
| EC-3  | The server created the entry but the response was lost, so the client reported a failure.               | The retry finds the existing entry by `runId`, creates nothing, and is answered as a **success** carrying the original `entryId` and `storedName` (DR-115). The player sees one entry and one confirmation.     |
| EC-4  | The player edits the name, then retries after an apparent failure that had in fact succeeded.           | The entry stands **as originally stored**; the edited name is not applied (DR-118, DR-126). The confirmation shows the server's `storedName`, so the player sees what is actually on the board rather than what they just typed. Knowingly accepted — see open question 4. |
| EC-5  | Two requests for the same `runId` arrive at the server simultaneously.                                  | The `runId` uniqueness constraint lets exactly one win. The loser reads the winner's entry back and returns it as a success with the same `entryId` (DR-115). Neither caller sees a constraint violation.       |
| EC-6  | The DR-128 deadline expires; the response then arrives at 12 seconds.                                   | The client has already reported `Timeout` and removed the progress indication. The late response is ignored. If the server did commit, the retry is idempotent (EC-3) — which is precisely why abandoning is safe. |
| EC-7  | The browser is offline when submit is activated.                                                        | Fail fast with `NetworkUnavailable`. The name stays in the field, the summary stays on screen, a retry is offered, and nothing is lost (DR-127).                                                               |
| EC-8  | The server answers with an unattributed failure (e.g. a 5xx).                                           | Reported as `ServerError`, retryable. The player is told the submission did not save and can retry; no status code or stack trace is shown (DR-127, NFR-006).                                                   |
| EC-9  | The storage the entries live in is unreachable at submit time.                                          | The submission is **not** confirmed — a confirmation is only ever given for a durably stored entry (NFR-003). It is reported as a retryable failure. Never confirm optimistically and reconcile later.          |
| EC-10 | The player activates *play again* while a submission is in flight.                                      | A fresh session starts immediately, with no prompt (DR-129). The in-flight submission's outcome does not touch the new session. If it succeeded, the entry stands; if it failed, the run is simply gone.         |
| EC-11 | The player reloads the page or closes the tab mid-submission.                                           | Whatever the server already committed stands; anything it had not is lost. The `CompletedRun` is gone (**Run Availability Window (DR-106)**) and the run cannot be retried.                                      |
| EC-12 | Submit is attempted when no `CompletedRun` is available — e.g. after a reload.                          | Refused client-side with `RunUnavailable`. There is nothing to submit and nothing is sent.                                                                                                                     |
| EC-13 | The player submits successfully, then edits the name and submits again.                                 | The client refuses with `AlreadySubmitted` and sends nothing (FR-022). The entry is immutable (DR-118). Had the request been sent, the server would have returned the original entry unchanged (DR-115).         |
| EC-14 | The name is `"   "` — only whitespace.                                                                  | Normalises to empty; refused with `NameMissing` (DR-109, DR-110). Nothing is created.                                                                                                                          |
| EC-15 | The name is `"   Jan    Willem   "`.                                                                    | Normalised to `"Jan Willem"` and accepted (DR-110). The stored name has no surrounding whitespace and no internal run longer than one space.                                                                    |
| EC-16 | A 60-character name is pasted into the field, or posted straight to the server.                         | The field accepts only the first 20 characters; a direct post of 60 is **refused** with `NameTooLong`, never truncated (DR-111). The player fixes it themselves rather than discovering a mutilated name on a permanent board. |
| EC-17 | The name is made of emoji, or of letters with combining accents.                                        | Accepted. Length is counted in extended grapheme clusters (DR-111), so a four-emoji name is four characters, not sixteen. Only control and format characters are refused.                                        |
| EC-18 | The name contains a zero-width space or a bidirectional override, pasted from elsewhere.                | Refused with `NameHasForbiddenCharacters` (DR-111). These are invisible, can reorder the rest of a leaderboard row, and no legitimate name needs them.                                                           |
| EC-19 | The name contains `<script>` or other markup.                                                           | Stored verbatim as those literal characters and escaped at render (NFR-008). It appears on the board as text and does nothing. It is **not** stripped, rewritten or rejected — that would be content filtering (DR-113), and escaping is the correct fix. |
| EC-20 | A name identical to one already on the board is submitted by a different colleague.                     | Accepted as a separate entry (DR-112, DR-114). Nothing is merged and neither entry is hidden. Telling two Daans apart is a human problem, and the honest one, given there are no accounts.                        |
| EC-21 | The figures are tampered with in the browser to an impossible value — `score` 999,999,999 on 12 lines.  | Refused by DR-123's ceiling. Nothing is created, and the player is told the submission was refused (SC-023).                                                                                                     |
| EC-22 | The figures are tampered with to a **plausible** value — a real-looking 40,000 on 130 lines at level 14. | **Accepted.** No check can distinguish it from a genuinely good run without a server-side replay, which §9.2 excludes as disproportionate. Stated here so it is a known, accepted consequence rather than a surprise. |
| EC-23 | A tampered client submits another run's `runId`.                                                        | The server returns the existing entry (DR-115), so the griefer's own run never reaches the board and nobody else's entry is altered. The damage is confined to the attacker's own submission — an accepted cost of a client-generated idempotency key. |
| EC-24 | A marathon run clears more than 9,999 lines.                                                            | Refused by DR-123's bound. That is many hours of continuous play at level 15; the bound exists to keep the arithmetic honest, not to police skill (open question 6).                                              |
| EC-25 | The server clock jumps backwards, so a later entry carries an earlier `submittedAt`.                    | Both entries stand. `submittedAt` is a record, not an ordering or uniqueness key; nothing here depends on it being monotonic, and how story 010 orders the board is 010's decision (DR-132).                      |
| EC-26 | A submission succeeds while story 010 does not exist yet.                                               | Entirely fine. The entry is stored durably and confirmed; there is simply nowhere to look at it yet. 009 is independently shippable and independently testable (SUC-009).                                         |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                                       |
| ------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 28 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                                   |
| SUC-002 | Each of the 24 domain rules DR-109 – DR-132 is covered by at least one automated test.                                                                                              |
| SUC-003 | **One entry per run, proven under stress**: in a test firing 100 concurrent submissions of a single `runId`, exactly one entry exists afterwards and all 100 callers receive the same `entryId` (NFR-004). |
| SUC-004 | A submission whose response is lost, followed by a retry, produces exactly one entry, and the player is shown a success (EC-3).                                                     |
| SUC-005 | 100 confirmed submissions survive an application restart **and** a redeployment with every attribute unchanged (NFR-003, DR-121).                                                   |
| SUC-006 | A name-validation table test covering empty, whitespace-only, 1, 20 and 21 characters, emoji, combining marks, internal double spaces, control characters and format characters produces the expected accept-or-refuse outcome for every row. |
| SUC-007 | A fuzz of 1,000 tampered submissions finds no case in which an entry is created with a negative figure, a `level` inconsistent with `linesCleared`, or a score above the DR-123 ceiling — and no case in which a legitimate run is refused. |
| SUC-008 | A player can go from the game-over screen to a confirmed entry in **one name and one action**, keyboard only, in under 3 seconds on the office network.                              |
| SUC-009 | 009 ships and is fully testable with stories 010 and 011 unbuilt, and 010 and 011 can then be built entirely against §5.1 without 009 changing — the contract 008 SUC-007 set for 009, carried forward. |
| SUC-010 | In informal office play over one week: no duplicate-entry report, no report of a score lost without explanation, and no player stuck on a spinner or unable to start a new game.      |

## 9. Dependencies and Constraints

### 9.1 Dependencies

**Upstream — 009 cannot be completed before these:**

- **Story 008 (game over)** — the hand-off contract. 008 creates the immutable `CompletedRun` (008 §5.1,
  **Completed Run Provenance (DR-103)**), guarantees it is available for the whole game-over state
  (**Run Availability Window (DR-106)**), and guarantees its `runId` is unique among runs played on the page
  (**Run Identity (DR-107)**). 009 consumes exactly that and needs no other access to the finished session —
  the boundary 008 SUC-007 asked for. 009 also adds its field and actions to a screen 008 owns, and must not
  break 008 FR-011 – FR-013 (*play again* unblocked, unconfirmed and immediate).
- **Story 006 (line clears and scoring)** — owns `score` and `linesCleared` (006 §5.1). 009 submits both and
  computes neither. DR-123's score ceiling is derived from 006's **Line Clear Score (DR-80)** and
  **Drop Score (DR-81)**.
- **Story 007 (levels and speed)** — owns `level`. DR-123's consistency check *is* 007's
  **Level Derivation (DR-90)** and **Level Cap (DR-94)** applied as a validation rule; if 007's curve or cap
  changes, DR-123 changes with it. This coupling is deliberate and is noted in §9.2.
- **Story 001 (start a new game)** — the vocabulary everything else is written in, plus
  **Fresh Session State (DR-23)** which *play again* invokes.
- **A durable store, and the architecture decision that chooses it** — DR-121 cannot be satisfied by
  anything the browser owns. This is the first feature in the project with a real infrastructure dependency
  (open question 1, arc42 §07 and §09).

**Downstream — these depend on 009:**

- **Story 010 (view the leaderboard)** — reads `LeaderboardEntry` (§5.1) and owns every display decision:
  ordering, tie-breaking, how many rows, paging, rank numbering, formatting and where the board is reached
  from (DR-132). It must reuse §5's names verbatim, must honour NFR-008 when rendering a `playerName`, and
  must not introduce per-name collapsing (DR-112, DR-114) or eviction (DR-122).
- **Story 011 (highlight the just-finished run)** — matches on the `entryId` that 009 returns on success
  (**Entry Identity (DR-119)**), carried on the `ScoreSubmission` (§5.1). `runId` may corroborate but is
  never the authority, because the client generates it.
- **Story 013 (branding)** — styles the name field, the confirmation and the failure message. It must
  preserve NFR-005 and NFR-006.
- **Story 014 (controls panel)** — documents `Enter` to submit and 008's `R` to play again, and the
  no-autofocus interaction between them (open question 3).

**Other:**

- **`docs/product/story-map.md`** — source of the story numbering, the *Compete* activity, the all-time
  shared leaderboard, and the project-wide exclusions quoted in §1.3.

### 9.2 Constraints

- **The submitted score is computed client-side and is therefore forgeable — accepted.** 008 §9.2 states the
  constraint and hands the decision to accept or mitigate to this spec. **009 accepts it.** This is an
  internal office game with no accounts, no stakes beyond bragging rights and a user base of colleagues; the
  only real mitigation is server-side validation of a replayed input stream, whose cost is wildly out of
  proportion to someone typing a bigger number to amuse themselves. The one cheap, proportionate thing
  *is* done: DR-123's sanity validation, which rejects the impossible (EC-21) and nothing else. A plausible
  forgery is accepted knowingly (EC-22). **No anti-cheat is designed here** — see §1.3.
- **A client-generated `runId` is used as the idempotency key.** It is the only value both sides can agree on
  before the entry exists, and it is what makes retry safe (DR-115, DR-126). It is not trustworthy, and
  nothing is inferred from it; the blast radius of abusing it is confined to the abuser's own submission
  (EC-23).
- **No accounts and no identity** (story map). A name is a label (DR-112). Two entries with the same name may
  be two people; one person's two runs are two unrelated entries. Every later feature must live with that.
- **No content moderation** (DR-113) — accepted, with a human deletion as the remedy of last resort. There is
  no deletion path in this feature, so exercising that remedy means operating on the store directly. Recorded
  as a known gap in arc42 §11, not papered over.
- **No deletion and no correction path.** Entries are immutable and permanent (DR-118, DR-122). A colleague
  who asks for their name to be taken off the board cannot be served by this feature. The name is personal
  data, given knowingly for publication to colleagues, and it is the only personal datum held (DR-131) — but
  "we can't remove it" is a real limitation and belongs on the record (arc42 §11, open question 8).
- **Durability forces part of 001's open question 1.** Wherever the game loop ends up running, the
  leaderboard **must** be server-side: nothing the browser owns survives a redeploy on someone else's
  machine. 009 therefore settles the persistence half of that question — there is a server component — while
  leaving the game-loop half exactly as open as 001 left it.
- **DR-123 is coupled to stories 006 and 007.** Its ceiling and its consistency check are derived from DR-80,
  DR-81, DR-90 and DR-94. Changing the scoring table, the level curve or the level cap invalidates DR-123 and
  must update it in the same change. This is the price of validating anything at all server-side, and it is
  cheap at this size.
- **Platform**: desktop browser on a laptop with a keyboard. The whole flow is keyboard-operable (FR-012).
  No mobile, no touch, no on-screen keyboard.
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed by this spec):
  ASP.NET Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`.
  No test project exists yet; SUC-001 requires one.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*, matching the approach taken in 001 §9.3 and 008 §9.3. 009 is the
> first story that makes several of them genuinely necessary rather than merely desirable.

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                                              |
| ----------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 02. Architecture Constraints (`02-architecture-constraints.md`)   | Should record "no accounts, ever" and the accepted forgeable-score constraint as product-wide architectural constraints, since DR-112 and §9.2 follow from them rather than from this feature. *Currently empty.* |
| 03. System Scope and Context (`03-system-scope-and-context.md`)   | **009 moves the system boundary.** Up to 008 the product was one page with no external systems; here it gains a server-side API and a durable store. The context diagram needs both. *Currently empty.*         |
| 04. Solution Strategy (`04-solution-strategy.md`)                 | 001 left the game-loop placement open; 009 settles that persistence is server-side regardless (§9.2). Should record that split. *Currently empty.*                                                             |
| 05. Building Block View (`05-building-block-view.md`)             | Introduces the Compete building block — submission endpoint, validation, entry store — sitting behind the seam 008 drew with `CompletedRun`. Story 010 reads from the same store. *Currently empty.*            |
| 06. Runtime View (`06-runtime-view.md`)                           | Needs the submission scenario end to end, including the three that matter: double submit, retry after a lost response, and concurrent submission of one `runId` (EC-3, EC-5). *Currently empty.*                |
| 07. Deployment View (`07-deployment-view.md`)                     | **First section this project actually needs.** DR-121 requires entries to survive a redeploy, which constrains where the store lives and how it is provisioned, backed up and migrated. *Currently empty.*       |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)       | Needs the project's concepts for input validation (client plus authoritative server), error presentation (DR-127), idempotency (DR-115) and output escaping (NFR-008) — all cross-cutting, all first needed here. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`)       | **009 warrants four ADRs**: (1) where leaderboard entries are stored, deferred from DR-121 and open question 1; (2) idempotency keyed on a client-generated `runId` with a uniqueness constraint, rather than a server-issued submission token; (3) one entry per run rather than best-score-per-name (DR-114); (4) accepting client-computed scores with sanity validation only (§9.2), which 008 §9.3 already flagged. *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)           | Should host the durability and "exactly once" quality scenarios that NFR-003 and NFR-004 are instances of, since story 010 inherits both. *Currently empty.*                                                   |
| 11. Risks and Technical Debt (`11-risks-and-technical-debt.md`)   | Should carry three accepted risks with named owners: forgeable scores (§9.2), unmoderated names (DR-113), and no deletion or correction path for a published name (§9.2). *Currently empty.*                     |
| 12. Glossary (`12-glossary.md`)                                   | Should gain: leaderboard, leaderboard entry, player name, submission, idempotent submission, display label (as distinct from identity). *Currently empty.*                                                       |

## 10. Open Questions

| #   | Question                                                                                                | Owner      | Status   | Resolution                                                                                                                                                                                                                                                                                              |
| --- | --------------------------------------------------------------------------------------------------------- | ---------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Where do leaderboard entries actually live — database, file, table storage?                               | Daan Harms | Resolved | **Deferred — this is an architecture decision, not a behavioural one**, handled exactly as 001 handled its browser-versus-server question (001 open question 1). What this spec requires is *durability* across reload, restart and redeploy (DR-121) and correctness at 100,000 entries (NFR-009). Recorded in §9.3 as an ADR arc42 §09 must capture before implementation. Nothing in §3 – §5 changes whichever way it goes. |
| 2   | Three initials, arcade style, or a free-text name?                                                        | Daan Harms | Resolved | **Free-text display name, 1–20 characters** (DR-111). The story says "type my name" and story 010 shows "each name" — initials are a constraint of 1983 cabinet hardware, not a product requirement here, and `DH` next to `DH` is unreadable in an office of colleagues. Three initials is a **Non-Goal** (§1.3). 20 characters fits a full name comfortably and still fits a leaderboard row. |
| 3   | Should the name field be auto-focused when the game-over screen appears?                                  | Daan Harms | Resolved | **No.** 008 open question 2 binds `R` to *play again*; if the field held focus, `R` would type a letter instead, and the product's hottest path would silently break. The player focuses the field deliberately (Tab or click), `Enter` submits while it has focus, and `R` keeps working everywhere else (FR-012). Story 014 documents both. |
| 4   | A retry succeeds idempotently, but the player had edited the name in between. Which name wins?            | Daan Harms | Resolved | **The originally stored name stands** (DR-118, DR-126). The alternative — letting a retry update the entry — would mean an entry is mutable, and mutability is exactly what makes "exactly one entry" hard to reason about. The client confirms using the server's `storedName` so the player is never shown a name that is not on the board (EC-4). Rare, harmless, and far better than a second entry. |
| 5   | Should the entry also store `piecesSpawned`, `duration` and `topOutReason`? `CompletedRun` has them.      | Daan Harms | Resolved | **No** (§5.1). A leaderboard answers "who, and how well"; none of the three helps a reader rank or recognise a run, and everything stored here is stored forever and immutably (DR-118, DR-122). Score, lines and level are what story 010 will show. They remain on `CompletedRun`, so adding one later is a schema change to 009, agreed here — not a field 010 invents (DR-132). |
| 6   | Is the 9,999-line ceiling in DR-123 arbitrary?                                                            | Daan Harms | Resolved | **It is a deliberate sanity bound, not a skill judgement.** 9,999 lines is many hours of continuous play at level 15 — well past "a five-minute coffee break" and past any session the product is designed for. Its real job is to keep DR-123's check 4 honest: without some bound on `linesCleared`, the derived score ceiling is unbounded too and validates nothing (EC-24). |
| 7   | One entry per run, or one row per name showing that name's best score?                                    | Daan Harms | Resolved | **One entry per submitted run** (DR-114). Without accounts a name is not an identity (DR-112), so "best score per name" would merge two different colleagues who both go by "Daan" and silently destroy one of their scores — a data-loss bug dressed as a feature. Every run submitted is stored. How many are *displayed* is story 010's decision (DR-132). |
| 8   | Can a colleague have their name removed from the board?                                                   | Daan Harms | Resolved | **Not by this feature** (DR-118, §9.2). Entries are immutable and permanent, and no moderation or deletion path is in scope. The remedy of last resort is a human operating on the store directly. This is a genuine limitation rather than a decision to be proud of, and it is recorded as an accepted risk in arc42 §11 rather than left unsaid. |
| 9   | Should a successful submission tell the player what rank they achieved?                                   | Daan Harms | Resolved | **No — out of scope here** (DR-132). Rank is a property of the whole board at the moment it is read, and is **story 010's** to derive and **story 011's** to point at. 009 confirms that the entry was saved and returns its `entryId`; that is exactly what 011 needs and nothing more. |
| 10  | Should the confirmation lead straight to the leaderboard?                                                 | Daan Harms | Resolved | **Not decided here**, consistent with 008 open question 4. Story 010 owns where the board is reached from. 009 reserves no space for a link and makes no assumption, so 010 is unconstrained. |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-010, SC-028; US-002 -> SC-011, SC-012; US-003 -> SC-017..SC-019;
        US-004 -> SC-013..SC-016; US-005 -> SC-020..SC-022; US-006 -> SC-023..SC-027
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-012 -> US-001; FR-013..FR-015 -> US-002; FR-016..FR-019 -> US-003;
         FR-020..FR-022 -> US-004; FR-023..FR-025 -> US-005; FR-026..FR-028 -> US-006)
  - [x] Domain model covers all entities mentioned in the requirements
        New: LeaderboardEntry and ScoreSubmission entities; PlayerName and SubmissionFailure value
        objects; SubmissionStatus and SubmissionFailureReason enumerations.
        Reused verbatim: CompletedRun, GameSession, GameStatus (001, 008); score and linesCleared (006);
        level (007)
  - [x] Domain rules and invariants are listed (DR-109 .. DR-132; DR-23, DR-80, DR-81, DR-90, DR-94,
        DR-103, DR-104, DR-106, DR-107 cited from 001, 006, 007 and 008)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-26, with EC-1..EC-13 covering
        the double-submission and network-failure families in depth)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-010)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all ten resolved)
-->

