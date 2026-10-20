# Feature Specification: View the All-Time Leaderboard — Ranking, the Ranked Window and the Board's States

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
| Feature ID      | 010                                                                                                 |
| Status          | Draft                                                                                               |
| Author          | Daan Harms                                                                                          |
| Created         | 2026-10-20                                                                                          |
| Last updated    | 2026-10-20                                                                                          |
| Epic / Parent   | Compete (story map activity) — `docs/product/story-map.md`                                          |
| Arc42 reference | Sections 1, 3, 5, 6, 8, 9, 10, 11, 12 — see §9.3. All arc42 files are currently empty placeholders.  |

> **Note on this spec.** 010 is the **read half of the Compete activity**.
> [`009-submit-score.md`](009-submit-score.md) is foundational: it defines `LeaderboardEntry`, `PlayerName`
> and the rules DR-109 – DR-132 that govern what is stored. **This spec defines nothing about storage and
> changes nothing about it.** It reuses 009's vocabulary verbatim and cites its rules by ID and name, e.g.
> "per **Entry Identity (DR-119)**".
>
> **009 deliberately left rank to this spec.** **Ranking and Presentation Are Deferred (DR-132)** hands 010
> the sort order, the tie-breaking, how many entries are listed, paging, rank numbering, date formatting and
> where the board is reached from. This spec decides all of them, and — as DR-132 requires — decides them
> **without asking 009 for a single new stored attribute**. `LeaderboardEntry` has no `rank` attribute and
> gains none here: rank is derived at read time from the whole board, every time it is read (DR-171).
>
> **Note the actor.** The story says *colleague*, not *player*. Someone may walk past a desk, hear about the
> board and look at it without ever having played — and must be able to, with no prerequisite state, no
> finished run and no idea what a tetromino is (DR-193). Every decision below is made for that reader first
> and for the player who just submitted second. The player who wants to find *their own* run on the board is
> **story 011**; this spec builds the window machinery 011 needs (DR-175) and stops there.
>
> **Vocabulary.** `LeaderboardEntry`, `entryId`, `playerName`, `score`, `linesCleared`, `level`, `runId`,
> `submittedAt` and the `PlayerName` value object are all defined by [`009 §5`](009-submit-score.md#5-domain-model)
> and are used unchanged. `score` and `linesCleared` originate in [`006 §5`](006-line-clears-and-scoring.md#5-domain-model),
> `level` in [`007 §5`](007-levels-and-speed.md#5-domain-model). This spec reads all of them and defines none.
> What it *does* define is a **read model** — `LeaderboardView` and `RankedEntry` (§5.1) — which exists only
> for the duration of one read and is never persisted.
>
> **Rule numbering.** New rules introduced here run from **DR-169 to DR-198**, so that IDs stay globally
> unique across concurrently written specs. DR-01 – DR-138 belong to specs 001 – 009 and DR-139 – DR-168 to
> story 013; all are cited here, never restated.

### 1.1 Problem Statement

009 made a run durable: a colleague can now type a name at game over and the entry survives restarts and
redeployments. But nothing in the product ever shows those entries to anyone. The board exists and is
invisible — the scores go into a drawer nobody can open, so "office bragging rights" amounts to telling
people a number and hoping they believe you. The story map's goal is a *shared* all-time leaderboard, and a
leaderboard that cannot be read is not shared.

### 1.2 Goal

Any colleague — whether or not they have ever played — can open the leaderboard and immediately see who is
at the top: a ranked table of the ten highest scores, each row naming the player and showing what their run
was worth, with a way to look further down the board for those who care. It is reachable from the landing
page and from the game-over screen, it is fresh enough that a player who just submitted sees their own
entry, and it tells the reader the truth in every state — including the two awkward ones, when the board is
genuinely empty and when it could not be loaded at all.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec. Each is either a separate story, a project-wide
exclusion, or an architecture decision. Do not implement them here, and do not design around a guess at how
they will work — the owning document decides.

- **Highlighting the reader's just-finished run, scrolling to it, or saying "you placed 14th"** —
  **story 011**. 010's obligation to 011 is to make the ranking total and deterministic (DR-169, DR-170) and
  to make a window **addressable by the entry it contains** (DR-175), so that 011 can ask for the window
  around an `entryId` without inventing its own ordering. 010 draws no highlight and knows nothing about
  which run the reader just played.
- **Name entry, submission, validation, persistence and the figures themselves** — **story 009**. 010 is a
  reader. It creates nothing, changes nothing and deletes nothing (DR-195). In particular 010 does **not**
  re-specify name validation: 009's **Name Normalisation (DR-110)** and **Name Bounds and Character Policy
  (DR-111)** guarantee the board never receives a name longer than 20 grapheme clusters or carrying control
  or format characters. 010 only decides how the names that *do* arrive are laid out (DR-181 – DR-184).
- **Filtering, searching or free-text lookup of the board.** No "find my name" box, no filter by score
  range, no "only show level 10+". A reader looking for a specific person reads down the list.
- **Sorting by any other column, or reversing the order.** There is exactly one ordering (DR-169) and it is
  not configurable. Lines-cleared and level are *supporting context* for a score, not axes to rank by.
- **Per-name aggregation of any kind** — no "best score per person", no "top 10 players", no deduplication,
  no grouping, no personal best. **A Name Is a Label, Not an Identity (DR-112)** and **One Entry Per
  Submitted Run (DR-114)** make this not merely out of scope but *underivable*: two rows reading "Daan" may
  well be two colleagues, and the product holds nothing that could tell them apart. Faking it would silently
  destroy one stranger's score to flatter another's. Every run is its own row (DR-180).
- **Weekly, monthly, seasonal or per-team views; resets; archives.** Out of scope project-wide (story map:
  "the leaderboard is all-time high score only"), and 009's **One Shared All-Time Leaderboard (DR-122)**
  stores exactly one board with no period attribute to slice by.
- **Editing, deleting, hiding, reporting or moderating an entry.** Entries are immutable once created
  (**Entry Immutability (DR-118)**) and 009's **No Content Moderation (DR-113)** records the accepted
  trade-off. The board has no write path at all (DR-195), so there is nothing here to protect and no
  administrator screen to build.
- **Accounts, authentication, authorisation or "my scores"** — out of scope project-wide (story map). The
  board is public to anyone who can reach the application, because every colleague who can reach it can
  already play and submit.
- **Live updates, push notifications, "3 new scores since you opened this" or a ticker.** The board is
  fetched when it is viewed and refreshed when the reader asks (DR-191). The justification is in §9.2.
- **The visual design — colours, typefaces, tabular figure settings, spacing, the table's chrome** —
  **story 013**, which owns the Info Support brand and the typographic tokens. 010 states the *legibility
  and alignment obligations* its data imposes (NFR-003, DR-177, DR-181) and leaves every token to 013.
- **The landing page itself** — **story 012**. 010 requires that an entry point to the board exists there
  (FR-013) and specifies nothing about how that page looks or what else is on it.
- **The game-over presentation and the submission confirmation that links here** — **stories 008 and 009**.
  010 requires that the board be reachable from the game-over state and does not design that screen.
- **Choosing the storage technology, the query mechanism or the transport.** *That the board can be read as
  a ranked window without shipping the whole board to the browser* is a requirement (DR-176, NFR-002).
  *How* — database ordering, an index, an in-memory sort, a page-rendered table or a fetched payload — is an
  **architecture decision** deferred to an ADR in arc42 §09, exactly as 009 deferred its storage question
  (009 open question 1). Nothing in §3 – §5 changes whichever way it goes.
- **Mobile, touch and small-screen layouts** — out of scope project-wide (laptop + keyboard only).

## 2. User Stories

### US-001: See who is top of the board

**As a** colleague who may never have played,
**I want** to open the leaderboard and immediately see the ten highest scores with the names beside them,
**so that** I know who to tease at the coffee machine without having to play a game first.

### US-002: Look further down than the top ten

**As a** colleague whose score is good but not top-ten,
**I want** a way to see beyond the first ten rows,
**so that** the board is a record of everyone who played and not a wall I am invisible behind.

### US-003: Find the board at all

**As a** colleague who was told about this over lunch,
**I want** to reach the leaderboard from the page I land on, and to be able to send someone a link to it,
**so that** looking at the board is one click and not a scavenger hunt.

### US-004: Understand a board with nothing on it

**As the** first colleague to open a freshly deployed game,
**I want** an empty board to tell me it is empty and invite me to play,
**so that** I do not conclude the thing is broken and close the tab.

### US-005: Be told when it did not load

**As a** colleague on flaky office Wi-Fi,
**I want** a failed load to say so and offer me a retry,
**so that** I am not staring at a spinner forever, and never shown an empty board that is actually a
network error in disguise.

### US-006: Read the board with a screen reader

**As a** colleague who navigates by keyboard and screen reader,
**I want** the board to be a real table whose rank, name and figures are announced per row,
**so that** I can read the standings without inferring anything from visual position.

### US-007: See my own score the moment I have submitted it

**As a** player who has just submitted a run,
**I want** the board to include my entry as soon as I open it,
**so that** the confirmation I was given is not contradicted by the board it refers to.

### US-008: Not have one odd name wreck the table

**As the** colleague who maintains this game,
**I want** a very wide, right-to-left or emoji-heavy name to sit inside its column without shifting the
score column,
**so that** one creative submission does not make the whole board unreadable for everyone else, permanently
and with no way to delete it.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) defined in this spec are
listed in §5.5; rules DR-01 – DR-168 are defined in specs 001 – 009 and story 013, and are only cited here.
"The board" means the set of all `LeaderboardEntry` records (009 DR-122)._

| ID     | Requirement                                                                                                                                                                                                                                                    | Priority | User Story |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall present the board as a list ordered by `score` **descending**, breaking ties by `submittedAt` **ascending** and any remaining tie by `entryId` ascending, yielding a total and deterministic order (DR-169).                                       | Must     | US-001     |
| FR-002 | The system shall number the ordered entries **sequentially from 1** with no shared ranks and no gaps, so that tied scores receive distinct, adjacent ranks (DR-170).                                                                                                | Must     | US-001     |
| FR-003 | The system shall derive every rank at read time from the ordering in FR-001 and shall store no rank on any entry (DR-171, 009 DR-132, DR-118).                                                                                                                      | Must     | US-001     |
| FR-004 | The system shall show, by default, the window of ranks **1 to 10** (DR-173).                                                                                                                                                                                        | Must     | US-001     |
| FR-005 | The system shall show each entry as one row carrying, in this column order, **rank, player name, score, lines cleared, level** (DR-177).                                                                                                                            | Must     | US-001     |
| FR-006 | The system shall show `score`, `linesCleared` and `level` exactly as stored — no recomputation, rounding, abbreviation or unit suffix (DR-178).                                                                                                                      | Must     | US-001     |
| FR-007 | The system shall show every entry in the window as its own row and shall never merge, group, deduplicate or aggregate rows by name (DR-180, 009 DR-112, DR-114).                                                                                                     | Must     | US-001     |
| FR-008 | The system shall state how many entries the board holds in total and which ranks are currently shown (DR-173).                                                                                                                                                      | Should   | US-002     |
| FR-009 | The system shall offer a **show-more** action whenever the board holds more entries than the window shows, extending the window per DR-174 without replacing what is already on screen.                                                                              | Must     | US-002     |
| FR-010 | The system shall keep every already-displayed row at the same rank when the window grows, appending lower ranks beneath (DR-174).                                                                                                                                   | Must     | US-002     |
| FR-011 | The system shall withdraw the show-more action once the whole board is shown, and indicate that the end of the board has been reached (DR-174).                                                                                                                     | Should   | US-002     |
| FR-012 | The system shall be able to produce a window specified **by start rank** and a window specified as **the one containing a given `entryId`**, both ordered and ranked per FR-001 and FR-002 (DR-175).                                                                 | Must     | US-002     |
| FR-013 | The system shall transfer to the browser only the entries in the requested window plus the total entry count, never the whole board (DR-176).                                                                                                                       | Must     | US-002     |
| FR-014 | The system shall make the leaderboard reachable from the landing page (story 012) and from the game-over state (stories 008, 009), with no precondition — no finished run, no submitted score and no prior play (DR-193).                                            | Must     | US-003     |
| FR-015 | The system shall expose the leaderboard at a **stable, shareable, bookmarkable address**; opening that address directly shall present the default window of FR-004 (DR-194).                                                                                        | Should   | US-003     |
| FR-016 | The system shall offer, from the board, a way to start a game and a way back to where the reader came from (DR-193).                                                                                                                                               | Should   | US-003     |
| FR-017 | The system shall never create, modify or delete a `LeaderboardEntry` as a consequence of viewing the board (DR-195, 009 DR-118).                                                                                                                                    | Must     | US-003     |
| FR-018 | The system shall, when the board holds **zero** entries, present an explicit empty state that says the board is empty and invites the reader to play the first game — not a table with a header and no rows (DR-187).                                               | Must     | US-004     |
| FR-019 | The system shall present the **empty** state and the **failed** state with distinct wording and distinct actions, and shall never render one as the other (DR-186).                                                                                                 | Must     | US-004, US-005 |
| FR-020 | The system shall show that the board is loading, and shall never leave the reader on an indefinite loading indication (DR-185, DR-188).                                                                                                                             | Must     | US-005     |
| FR-021 | The system shall abandon a load that has not completed within **10 seconds** and treat it as failed (DR-188).                                                                                                                                                       | Must     | US-005     |
| FR-022 | The system shall report a failed load in plain language naming what went wrong, never a raw error or status code, and shall offer a **retry** action (DR-189).                                                                                                      | Must     | US-005     |
| FR-023 | The system shall, when a **refresh** of an already-displayed board fails, keep the rows already shown, state that they could not be refreshed and may be out of date, and offer a retry — it shall not blank the table (DR-190).                                     | Must     | US-005     |
| FR-024 | The system shall fetch the board fresh each time it is viewed, shall not poll or auto-refresh, and shall offer an explicit **refresh** action (DR-191).                                                                                                            | Must     | US-007     |
| FR-025 | The system shall include an entry whose submission was confirmed before the board was requested, in that board (DR-192).                                                                                                                                           | Must     | US-007     |
| FR-026 | The system shall present the board as a real table with a header row whose column headers are associated with their cells, and with an accessible name identifying it as the all-time leaderboard (DR-197).                                                          | Must     | US-006     |
| FR-027 | The system shall convey each entry's rank as text in its own cell, never by row position alone, and shall not convey any meaning by colour alone (DR-197).                                                                                                          | Must     | US-006     |
| FR-028 | The system shall make show-more, retry, refresh and every other control reachable and operable from the keyboard alone, and shall announce each change of view state to assistive technology (DR-198).                                                              | Must     | US-006     |
| FR-029 | The system shall keep the score, lines and level columns at fixed horizontal positions regardless of the content of any name (DR-181).                                                                                                                             | Must     | US-008     |
| FR-030 | The system shall render a name that exceeds the name column's width as visibly truncated, with the full stored name available to the reader and to assistive technology, and shall never alter the stored value (DR-182, 009 DR-118).                                | Must     | US-008     |
| FR-031 | The system shall render each name in its own directional context, so that a right-to-left name cannot reorder the rank or the figures around it (DR-183).                                                                                                          | Must     | US-008     |
| FR-032 | The system shall render two visually similar or identical names as two ordinary, unannotated rows — no warning, badge, disambiguation or suffix (DR-184, 009 DR-112).                                                                                               | Should   | US-008     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash.
"The board holds …" describes the set of `LeaderboardEntry` records 009 persisted; no scenario here creates
one._

### SC-001: The top ten are shown, ranked (FR-001, FR-002, FR-004, FR-005)

```gherkin
Given the board holds 23 entries with distinct scores
When a colleague opens the leaderboard
Then exactly 10 rows are shown
  And they are the 10 highest scores, highest first
  And they are numbered 1 to 10
  And each row shows the rank, the player name, the score, the lines cleared and the level, in that order
```

### SC-002: A tie is broken by who got there first (FR-001, FR-002)

```gherkin
Given two entries both score 12,400
  And "Jan" submitted at 09:14 and "Ans" submitted at 11:02 on the same day
When the board is shown
Then "Jan" is ranked immediately above "Ans"
  And the two rows carry distinct, consecutive rank numbers
  And neither row shows a shared or repeated rank
```

### SC-003: Ranks are sequential with no gaps (FR-002)

```gherkin
Given the board holds entries scoring 9,000, 9,000, 9,000 and 4,000
When the board is shown
Then the four rows are ranked 1, 2, 3 and 4
  And no rank is skipped and no rank appears twice
```

### SC-004: Rank is not stored (FR-003)

```gherkin
Given an entry is ranked 3rd
When two higher-scoring runs are submitted by other colleagues
  And the board is read again
Then the same entry is ranked 5th
  And nothing about the entry itself changed, per Entry Immutability (DR-118)
```

### SC-005: Figures are shown as stored (FR-006)

```gherkin
Given an entry with a score of 184,000, 140 lines cleared and level 15
When its row is shown
Then the score reads 184,000 and not "184k", "~184,000" or "180,000"
  And the lines cleared reads 140 and the level reads 15
  And no figure is recomputed, bonused or rescaled
```

### SC-006: Two runs by the same name are two rows (FR-007)

```gherkin
Given the board holds an entry named "Daan" scoring 15,000 and another named "Daan" scoring 4,000
When the board is shown
Then both rows appear, at their own ranks
  And neither is hidden behind, merged into or summarised by the other
  And no "best score per name" collapsing has occurred, per DR-112 and DR-114
```

### SC-007: Looking past the top ten (FR-008, FR-009, FR-010)

```gherkin
Given the board holds 412 entries
When a colleague opens the leaderboard
Then ranks 1 to 10 are shown
  And the reader is told the board holds 412 entries in total
When the reader activates show more
Then ranks 1 to 50 are shown
  And the rows that were already on screen still carry ranks 1 to 10, unchanged and in place
  And ranks 11 to 50 have been appended beneath them
```

### SC-008: Reaching the end of the board (FR-011)

```gherkin
Given the board holds 34 entries
When the reader activates show more
Then all 34 rows are shown, ranked 1 to 34
  And the show-more action is no longer offered
  And the reader is told this is the whole board
```

### SC-009: A window can be asked for by rank, and by the entry it contains (FR-012)

```gherkin
Given the board holds 412 entries and the entry with id E is ranked 237th
When a window is requested starting at rank 231
Then the rows returned begin at rank 231, ranked and ordered exactly as in SC-001
When instead the window containing entry E is requested
Then the rows returned include rank 237, carrying entry E
  And the rank reported for E is 237
```

### SC-010: The whole board is never shipped to the browser (FR-013)

```gherkin
Given the board holds 2,000 entries
When a colleague opens the leaderboard
Then the data transferred to the browser covers the 10 displayed entries and the total count
  And it does not contain the other 1,990 entries
```

### SC-011: A colleague who has never played can read the board (FR-014, FR-016)

```gherkin
Given a colleague who has never started a game
When they open the landing page and follow the leaderboard entry point
Then the board is shown
  And nothing asked them to play, name themselves or finish a run first
  And from the board they can start a game, or go back to where they came from
```

### SC-012: The board can be linked to (FR-015)

```gherkin
Given a colleague pastes the leaderboard's address into a chat message
When another colleague opens that address directly in a new browser
Then the leaderboard is shown with the default window of ranks 1 to 10
  And no prior navigation through the application was needed
```

### SC-013: Viewing changes nothing (FR-017)

```gherkin
Given the board holds 12 entries
When the board is viewed, refreshed, expanded with show more and viewed again from another browser
Then all 12 entries are unchanged
  And no entry was created, modified, reordered in storage or deleted
```

### SC-014: A brand-new deployment invites a first game (FR-018)

```gherkin
Given the application has just been deployed and no score has ever been submitted
When a colleague opens the leaderboard
Then they are told the board is empty and that no games have been scored yet
  And they are invited to play the first game, with an action to do so
  And no table header with zero rows is shown
  And nothing suggests an error occurred
```

### SC-015: An empty board and a broken board do not look alike (FR-019)

```gherkin
Given the board holds zero entries
When a colleague opens the leaderboard
Then the empty state is shown, with an invitation to play and no retry action
Given instead the board could not be loaded
When a colleague opens the leaderboard
Then the failed state is shown, saying the board could not be loaded, with a retry action
  And it does not say the board is empty
  And the two states are distinguishable by their wording alone, without colour or iconography
```

### SC-016: Loading is visible and bounded (FR-020, FR-021)

```gherkin
Given the board is slow to load
When a colleague opens the leaderboard
Then they can see that the board is loading
  And after 10 seconds the attempt is abandoned
  And the failed state is shown with a retry action
  And the loading indication is gone
```

### SC-017: A retry after a failure succeeds (FR-022)

```gherkin
Given the board failed to load because the browser was offline
Then the reader is told the leaderboard could not be loaded, in plain language
  And no status code, stack trace or raw error text is shown
When connectivity returns and the reader activates retry
Then the board loads and the top ten are shown
```

### SC-018: A failed refresh does not blank the board (FR-023)

```gherkin
Given the top ten are on screen
When the reader activates refresh and the request fails
Then the ten rows are still on screen, unchanged
  And the reader is told the board could not be refreshed and may be out of date
  And a retry action is offered
  And the reader is not shown an empty board
```

### SC-019: A reader sees their own submission (FR-024, FR-025)

```gherkin
Given a player submitted a run and was shown a success confirmation, per 009 FR-008
When they navigate from the game-over screen to the leaderboard
Then their entry is present on the board at its correct rank
  And no manual refresh was needed to make it appear
```

### SC-020: The board does not change under the reader's eyes (FR-024)

```gherkin
Given the top ten are on screen
  And another colleague submits a higher score a moment later
When the reader does nothing for two minutes
Then the displayed rows are unchanged and no row has moved
When the reader activates refresh
Then the new entry appears at its rank and the rows below it shift down by one
```

### SC-021: A screen reader can read the standings (FR-026, FR-027, FR-028)

```gherkin
Given a colleague using a screen reader opens the leaderboard
When they navigate the board
Then it is announced as a table named for the all-time leaderboard
  And each column is announced by its header — rank, player, score, lines, level
  And each row announces its rank as a number, not merely as its position in the list
When they tab through the page
Then show more, refresh and any retry action are reachable and operable without a pointing device
When the board finishes loading, fails, or grows by show more
Then the change of state is announced
```

### SC-022: A very wide name does not move the scores (FR-029, FR-030)

```gherkin
Given one entry's name is 20 full-width characters and another's is "Jan"
When both rows are shown
Then the score, lines and level columns start at the same horizontal position in both rows
  And the wide name is shown within its column, visibly truncated if it does not fit
  And the full stored name is available to the reader and to assistive technology
  And the stored name is unchanged
```

### SC-023: A right-to-left name stays inside its cell (FR-031)

```gherkin
Given an entry whose name is written in a right-to-left script
When its row is shown
Then the name renders right-to-left within the name column
  And the rank, score, lines and level keep their usual positions and order in the row
  And no neighbouring text is reordered
```

### SC-024: Two identical names are two ordinary rows (FR-032)

```gherkin
Given the board holds two entries both named "Daan", scoring 15,000 and 4,000
When the board is shown
Then both rows show the name "Daan" with no badge, suffix, warning or disambiguation
  And nothing on the board claims or implies they are the same person
```

### SC-025: A zero score is shown like any other (FR-006, FR-007)

```gherkin
Given the board holds 6 entries, one of which scored 0
When the board is shown
Then all 6 rows appear, with the zero-scoring run last
  And its row shows a score of 0, per Zero-Score Runs Are Submittable (DR-130)
  And it is neither suppressed nor marked as unusual
```

## 5. Domain Model

_**This spec adds no stored data.** `LeaderboardEntry` and `PlayerName` are defined by
[009 §5](009-submit-score.md#5-domain-model) and are used here **exactly as 009 defines them**, with no new
attribute, no change of type and no change of constraint. What follows is a **read model**: objects that
exist for the duration of one read of the board and are then discarded. **Ranking and Presentation Are
Deferred (DR-132)** requires precisely this — if 010 needed a stored attribute 009 does not provide, that
would be a change to 009, agreed there, not a field invented here. It does not._

_**Story 011 must reuse the names below verbatim** — `LeaderboardView`, `RankedEntry`, `rank`,
`windowStart`, `windowSize`, `entryCount`, `generatedAt`, and the window modes in §5.4 — rather than
introduce competing ones, the same contract 009 §5 imposes on this spec._

### 5.1 Entities

#### LeaderboardEntry (009 §5.1 — read here, defined there)

The stored record. Restated **only** as a reminder of what a row may draw on, and of what it deliberately
does not carry:

| Attribute      | Used by 010?                                                                                   |
| -------------- | ------------------------------------------------------------------------------------------------ |
| `entryId`      | **Yes** — carried through to `RankedEntry` so that **story 011** can match on it (**Entry Identity (DR-119)**). Never displayed. |
| `playerName`   | **Yes** — the Player column (FR-005). Rendered per DR-181 – DR-184.                               |
| `score`        | **Yes** — the Score column, and the **primary sort key** (DR-169).                                 |
| `linesCleared` | **Yes** — the Lines column (FR-005).                                                               |
| `level`        | **Yes** — the Level column (FR-005).                                                               |
| `submittedAt`  | **Yes, as the tie-break** (DR-169). Carried in the read model but **not rendered as a column** (DR-179). |
| `runId`        | **No.** An idempotency key for 009, never a display or ordering concern. Not carried into the read model. |
| *(no rank)*    | Correct and unchanged. Rank is derived here, per read, and stored nowhere (DR-171, 009 §5.1).      |

#### LeaderboardView

One answer to one read of the board: a contiguous window of ranked rows, plus everything the reader needs
to understand what they are looking at. **Derived, transient and never persisted** — it is recomputed from
the board on every read (DR-191) and two views generated a second apart may legitimately differ.

| Attribute     | Type            | Constraints                                                      | Description                                                                                                       |
| ------------- | --------------- | ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| rows          | RankedEntry[]   | ordered per DR-169; length ≤ `windowSize`; may be empty          | The rows to render, already in display order. Never re-sorted by the reader's side.                                |
| windowStart   | integer         | ≥ 1; equals `rows[0].rank` when `rows` is non-empty              | The rank of the first row in the window. **1** for the default view (DR-173).                                       |
| windowSize    | integer         | ≥ 1; 10 by default; grows per DR-174                             | How many ranks the window asked for. `rows` is shorter when the window runs past the end of the board.              |
| entryCount    | integer         | ≥ 0                                                              | How many entries the **whole board** holds, not how many are shown. Drives FR-008, FR-009 and FR-011. `0` means the board is genuinely empty (DR-186). |
| generatedAt   | datetime (UTC)  | server clock at the moment the view was produced                 | When this view was true. The ranks in it are true **as of** this instant and no later (DR-172).                     |

#### LeaderboardLoad

The reader's in-progress attempt to obtain a `LeaderboardView`. Transient and client-side; it exists in the
model so that the rules about loading, failure, retry and the empty/failed distinction (DR-185 – DR-190)
have something concrete to constrain. The deliberate sibling of 009's `ScoreSubmission`.

| Attribute    | Type                   | Constraints                                           | Description                                                                                   |
| ------------ | ---------------------- | ----------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| request      | WindowRequest          | required (§5.3)                                       | Which window was asked for.                                                                     |
| state        | LeaderboardLoadState   | required, default `Loading`                           | See the state machine below and §5.4.                                                           |
| view         | LeaderboardView?       | null unless `state` is `Ready` or `Empty`             | The last successfully obtained view. **Retained across a failed refresh** (DR-190).             |
| lastFailure  | LoadFailure?           | null unless `state` is `Failed` or `Stale`            | Why the last attempt failed, in terms a reader can act on (§5.3).                               |
| attemptCount | integer                | ≥ 0, starts at 0                                      | Attempts made, including retries. Reading is idempotent, so this is unbounded and harmless.     |

**Lifecycle:**
`Loading` → (a view arrives with `entryCount > 0`) → `Ready`; or → (a view arrives with `entryCount = 0`) →
`Empty`; or → (rejection, network failure or the DR-188 deadline) → `Failed` → (reader retries) →
`Loading`. From `Ready` or `Empty`, a refresh or a show-more re-enters `Loading`; if **that** attempt fails
while rows are already on screen the state becomes `Stale`, **not** `Failed` — the rows stay, flagged as
possibly out of date (DR-190) — and a retry from `Stale` re-enters `Loading`.

No state is terminal: every one of them has a way forward, which is DR-189's whole point.

### 5.2 Relationships

- The **board** is the set of all `LeaderboardEntry` records (009 **DR-122**). There is exactly one, shared
  by every colleague. 010 reads it and never writes it (DR-195).
- A **LeaderboardView** is a **projection of the board**, not a thing the board contains. It holds no
  entries — each of its `RankedEntry` rows holds *copies* of one entry's displayable values, taken at
  `generatedAt`.
- A **RankedEntry** corresponds to **exactly one** `LeaderboardEntry`, by `entryId`. The relationship is
  one-way and read-only: the entry knows nothing of the view.
- A **rank** is a property of the pair *(entry, board at an instant)* — never of an entry alone. The same
  entry has different ranks in two views generated at different times (SC-004, DR-172), and this is correct
  rather than a defect.
- A **LeaderboardLoad** produces **at most one** `LeaderboardView` per attempt, and retains the last
  successful one across failures (DR-190).
- Two **RankedEntry** rows bearing the same `playerName` have **no relationship whatsoever** (009
  **DR-112**). They are not grouped, counted together, or assumed to be one person — not in the model, not
  in the ordering, and not on screen (DR-180, DR-184).
- A **LeaderboardView** has **no relationship to a `GameSession`, a `CompletedRun` or a `ScoreSubmission`**.
  The board is readable with no game in progress and none ever played (DR-193). Connecting a view to the
  reader's own run is **story 011's** job, and it does it with `entryId` (DR-175), not by adding a
  relationship here.

### 5.3 Value Objects

#### RankedEntry

One row of the board: one stored entry plus the rank it holds in the ordering at the moment the view was
produced. Immutable; recomputed per read, never stored.

| Attribute     | Type             | Constraints                                               | Description                                                                             |
| ------------- | ---------------- | --------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| rank          | integer          | ≥ 1, unique within the board's full ordering              | **Derived (DR-171).** Sequential, no gaps, no ties (DR-170). Rendered as text (FR-027).     |
| entryId       | UUID             | from `LeaderboardEntry.entryId` (009 DR-119)              | Carried, **not displayed**. The value **story 011** matches on.                             |
| playerName    | PlayerName       | from the entry, unchanged (009 §5.3)                      | The Player column. Rendered per DR-181 – DR-184. Never re-normalised or re-validated here.  |
| score         | integer (64-bit) | from the entry, unchanged                                 | The Score column and the primary sort key.                                                  |
| linesCleared  | integer          | from the entry, unchanged                                 | The Lines column.                                                                           |
| level         | integer          | from the entry, unchanged, 1–15                           | The Level column.                                                                           |
| submittedAt   | datetime (UTC)   | from the entry, unchanged                                 | The tie-break (DR-169). Carried, **not rendered as a column** (DR-179).                     |

#### WindowRequest

Which slice of the ranked board is being asked for. Immutable.

| Attribute | Type        | Constraints                                               | Description                                                                 |
| --------- | ----------- | --------------------------------------------------------- | ----------------------------------------------------------------------------- |
| mode      | WindowMode  | required (§5.4)                                           | `Top`, `FromRank` or `Containing`.                                            |
| size      | integer     | ≥ 1, ≤ 200 (NFR-002); default 10                          | How many ranks to return.                                                     |
| startRank | integer?    | required when `mode` is `FromRank`; ≥ 1                   | The rank the window begins at.                                                |
| entryId   | UUID?       | required when `mode` is `Containing`                      | The entry the window must include. **Story 011's** entry point (DR-175).      |

#### LoadFailure

Why one load attempt failed, and what the reader can do about it. Immutable; replaced wholesale by the next
attempt's outcome. Deliberately shaped like 009's `SubmissionFailure` so the two read alike.

| Attribute | Type              | Constraints         | Description                                                                                |
| --------- | ----------------- | ------------------- | -------------------------------------------------------------------------------------------- |
| reason    | LoadFailureReason | required            | Which of the categories in §5.4 applies.                                                       |
| message   | string            | required, non-empty | Plain language naming the actual problem, for the reader (DR-189). Never a raw error or code.  |
| retryable | boolean           | required, derived   | Whether loading again unchanged could succeed. Derived from `reason` per the §5.4 table.       |

### 5.4 Enumerations

#### LeaderboardLoadState

Where one `LeaderboardLoad` has got to, and therefore what the reader is looking at. Closed set — there is
no sixth state and, in particular, **no state in which the reader is shown nothing and told nothing**.

| Member    | What the reader sees                                                                                                                   |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `Loading` | The board is being fetched. A progress indication, bounded by DR-188. The initial state of every load.                                   |
| `Ready`   | A view with `entryCount > 0`. The table, the window indicator, and show-more if more ranks exist.                                        |
| `Empty`   | A view with `entryCount = 0`. **Not an error.** The invitation of DR-187, no table, no retry action.                                     |
| `Stale`   | A refresh or show-more failed while rows were already on screen. The previous rows, plus a note that they could not be refreshed, plus a retry (DR-190). |
| `Failed`  | No view could be obtained at all. The message and retry of DR-189. **Never the `Empty` wording** (DR-186).                               |

#### WindowMode

How a `WindowRequest` picks its slice. Closed set.

| Member       | Meaning                                                                                                                           |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------- |
| `Top`        | Ranks 1 to `size`. The default view (DR-173) and what a bare visit to the board's address yields (DR-194).                          |
| `FromRank`   | Ranks `startRank` to `startRank + size - 1`. A `startRank` past the end yields an empty `rows` with the true `entryCount`.           |
| `Containing` | The window that includes the rank of `entryId`. **This is the mode story 011 exists on** (DR-175). An `entryId` not on the board is not an error: the result is the `Top` window and a flag that the entry was not found — 011 decides what to say about that. |

#### LoadFailureReason

Closed set. The `Retryable` column feeds `LoadFailure.retryable`.

| Member               | Meaning                                                                                      | Retryable | Produced by     |
| -------------------- | -------------------------------------------------------------------------------------------- | --------- | --------------- |
| `NetworkUnavailable` | The request could not be sent, or no response arrived.                                        | Yes       | Client only     |
| `Timeout`            | The DR-188 deadline of 10 seconds elapsed with no complete response.                          | Yes       | Client only     |
| `ServerError`        | The server answered, but could not produce a view.                                            | Yes       | Server          |
| `WindowOutOfRange`   | A `FromRank` window began below rank 1. A defect, not something a reader can cause.           | No        | Server          |

> **There is no `Empty` failure reason, and that is the point.** An empty board is a **successful** load of
> a board that happens to hold nothing (`state = Empty`, `entryCount = 0`). It never travels as a failure
> and is never rendered as one, nor the reverse (DR-186). Conflating the two is the single most likely
> mistake in this feature: both end up as "no rows on screen", and a reader who is told "no scores yet —
> be the first!" when in fact the network dropped will go and play a game to fix a problem that is not
> theirs, and conclude the board eats scores when their run does not appear either.

### 5.5 Domain Rules and Invariants

_New rules introduced by this spec, numbered **DR-169 – DR-198** so that IDs stay globally unique across
concurrently written specs. DR-01 – DR-168 belong to specs 001 – 009 and story 013 and are cited, never
restated. **Story 011 should cite these by ID and name**, e.g. "per **Board Ordering (DR-169)**"._

**Ordering and rank**

- **DR-169 — Board Ordering**: The board is ordered by **`score` descending**, then by **`submittedAt`
  ascending**, then by **`entryId` ascending**. The second key means that when two runs scored the same,
  **whoever reached that score first ranks higher** — the fair and intuitive reading of a tie, and the one
  that makes a rank, once earned, only ever lost to someone who genuinely beat you rather than to someone
  who merely arrived later. The third key is a deterministic last resort for the vanishingly unlikely case
  of two identical `submittedAt` values; it carries no meaning and is there solely so the order is
  **total**. A total order is not a nicety: without it two reads of an unchanged board could return
  different rankings, and story 011 could not reliably say where a run landed.
- **DR-170 — Sequential Ranking**: Ranks are assigned by position in the DR-169 ordering: 1, 2, 3, … with
  **no shared ranks and no gaps**. Because the ordering is total, tied scores receive **distinct, adjacent**
  ranks. This is *sequential* (ordinal) ranking, **not** dense ranking and **not** standard competition
  ranking (1, 1, 3) — stated explicitly because all three are defensible in general and mixing them within
  one product is not. Rank *n* always means "the *n*th row of the board", which is what a reader counting
  down the list expects, and it lets 011 say "you placed 14th" without qualification.
- **DR-171 — Rank Is Derived, Never Stored**: Rank is computed at read time from the whole board and is
  **never** written to a `LeaderboardEntry`, cached as an entry attribute, or carried between reads. This
  honours 009's `LeaderboardEntry` having no rank attribute and **Entry Immutability (DR-118)**: an entry
  that never changes cannot hold a number that changes every time someone else plays. 010 adds no stored
  field, which is exactly what **Ranking and Presentation Are Deferred (DR-132)** requires of it.
- **DR-172 — A Rank Is True Only As Of Its View**: Every rank belongs to the `LeaderboardView` that
  produced it and is true as of its `generatedAt`. A row's rank may differ in the next read because other
  colleagues have submitted; nothing is wrong when it does (SC-004). Nothing in this product may treat a
  rank as a durable property, announce a rank change, or compare ranks across views.

**The window**

- **DR-173 — The Ranked Window**: The board is always presented as a **contiguous window** over the DR-169
  ordering, described by `windowStart`, `windowSize` and the board's total `entryCount`. The default window
  is **ranks 1 to 10** — enough to answer "who is winning" at a glance, which is what the story asks for,
  and short enough to read without scrolling on a laptop. The reader is always told both which ranks are
  shown and how many entries exist in total (FR-008), so the top ten never masquerades as the whole board.
- **DR-174 — Show More Extends, Never Replaces**: When `entryCount > windowSize`, a **show-more** action is
  offered. Activating it grows `windowSize` to the next value in the sequence **10 → 50 → 100 → 150 → …**
  (to 50 on the first activation, then by 50 each time), leaving `windowStart` at 1 and **appending** the
  newly revealed ranks beneath the rows already on screen. Already-displayed rows keep their rank numbers
  and their positions; the window never shrinks on its own and the reader never loses their place. When the
  window covers the whole board the action is withdrawn and the end of the board is stated (FR-011). *Why
  these sizes:* 10 answers the common question; 50 covers essentially everyone on an office board in one
  more click; the +50 steps beyond that keep the figures on screen readable ("showing 1–50 of 412") instead
  of arithmetic nobody wants to do.
- **DR-175 — Windows Are Addressable**: A window can be requested three ways (§5.4 `WindowMode`): the
  **top** *n*, **from a given rank**, or **containing a given `entryId`**. All three return rows ordered and
  ranked identically — the mode chooses the slice and nothing else. The `Containing` mode exists **for
  story 011**: a player whose run landed at rank 237 must be able to find where they placed without
  pressing show-more five times, and 011 gets that by asking for the window containing their `entryId`
  (**Entry Identity (DR-119)**) rather than by inventing a second ordering or a second board. An `entryId`
  that is not on the board yields the `Top` window and a not-found indication; it is not an error here, and
  what to say about it is 011's decision.
- **DR-176 — The Browser Receives Only the Window**: Only the entries in the requested window, plus
  `entryCount` and `generatedAt`, ever reach the browser. The full board is never transferred and never
  sorted, ranked or paged on the reader's side. Ordering and ranking are produced once, at the source of
  the data, so that every reader sees the same board and so that render cost is bounded by `windowSize`
  rather than by how many colleagues have ever played (NFR-001, NFR-002).

**The row**

- **DR-177 — Row Content and Column Order**: Every row shows exactly five things, in this order:
  **Rank · Player · Score · Lines · Level**. *Why this order:* rank first because it is the question the
  reader is answering and it anchors the eye down the left edge; the name next because that is what they
  are scanning for; the score next because it is the headline figure and belongs beside the name it
  qualifies; lines and level last because they are supporting context — they explain *how* the score
  happened and are read only once a row has caught the reader's attention. The three figures sit together
  on the right so they can be compared vertically. Nothing else is shown per row — no `entryId`, no
  `runId`, no date, no derived rate, no medal, no avatar.
- **DR-178 — Figures Are Shown As Stored**: `score`, `linesCleared` and `level` are rendered as the exact
  integers 009 stored (**Figures Are Copied, Never Typed (DR-120)**). No rounding, no abbreviation
  ("184k"), no unit suffix, no bucketing, no percentage, no recomputation and no derived statistic.
  Digit-group separators are a **presentation** choice and are permitted — and encouraged, since "184,000"
  is read faster than "184000" — but they never change the value. The figures are the whole point of the
  board; anything that makes them approximate makes the board worthless.
- **DR-179 — No Date Column**: `submittedAt` is carried in the read model, because DR-169 ties are broken by
  it, but it is **not rendered as a column**. *Why:* an all-time board (009 **DR-122**) is not a timeline;
  a sixth column of dates costs the width the names need, adds a date-format and time-zone decision for no
  question anyone is asking, and invites exactly the "recent scores" reading the story map rules out. If a
  later story wants a date it is already stored — no change to 009 is needed, only a decision here.
- **DR-180 — Every Run Is Its Own Row**: The board shows one row per `LeaderboardEntry` within the window.
  Rows are **never** merged, grouped, deduplicated, counted or summarised by name. A colleague who submits
  ten runs occupies ten rows, possibly several of them consecutive. This is not a display convenience to be
  optimised away: **A Name Is a Label, Not an Identity (DR-112)** and **One Entry Per Submitted Run
  (DR-114)** mean a "best score per person" board is not merely out of scope, it is **underivable** — two
  rows reading "Daan" may be two colleagues, and collapsing them would delete a stranger's score from view
  in order to flatter someone they have never met.

**Names on a shared, permanent board**

- **DR-181 — The Name Never Moves the Figures**: The Score, Lines and Level columns occupy fixed horizontal
  positions, identical in every row, **independent of the content of any name**. The Player column has a
  bounded maximum width and cannot grow past it. 009's **Name Bounds and Character Policy (DR-111)** caps a
  name at 20 **grapheme clusters**, which bounds the *count* but not the *width*: 20 full-width CJK
  characters, 20 emoji or 20 wide-script glyphs are roughly twice the width of 20 Latin letters. Counting
  characters therefore cannot protect the layout and width must be bounded directly. One permanent,
  undeletable entry (**Entry Immutability (DR-118)**, **No Content Moderation (DR-113)**) must never be
  able to push the scores out of alignment for every reader, for ever.
- **DR-182 — Names Are Rendered Whole or Visibly Truncated**: A name that fits its column is rendered in
  full. One that does not is **visibly truncated** — the reader can see that there is more — with the
  complete stored name available on demand and to assistive technology (FR-030). The **stored value is
  never altered** (DR-118): truncation is a rendering decision made per view, not a change to the entry,
  and it is the opposite of 009's refusal to truncate on input (DR-111), which was about what gets
  *written*. A name is never silently cut with no indication, and never replaced by a placeholder.
- **DR-183 — Directional Isolation**: Each `playerName` is rendered in its **own directional context**, so
  that a right-to-left or mixed-direction name lays out correctly within the Player column while the rank
  and the three figures keep their usual order and position in the row. 009 already refuses explicit
  bidirectional override characters (DR-111), so this rule covers the ordinary, entirely legitimate case: a
  colleague whose name is written in Hebrew or Arabic. Their name must look right, and must not reorder
  anybody else's row.
- **DR-184 — Lookalike Names Are Not Disambiguated**: Two rows whose names are identical, or visually
  confusable through homoglyphs or lookalike scripts, are shown as **two ordinary rows** — no warning, no
  badge, no numeric suffix, no "(2)", no highlighting and no attempt to tell them apart. This follows
  directly from **DR-112**: the product does not know whether they are one person or two, so any
  disambiguation it offered would be a guess presented as a fact. Readers resolve this the way colleagues
  always have, by asking each other.

**States: loading, empty, stale and failed**

- **DR-185 — The View States Are a Closed Set**: At every moment the board is in exactly one of `Loading`,
  `Ready`, `Empty`, `Stale` or `Failed` (§5.4), each with its own wording and its own available actions.
  There is no sixth state, and in particular no state in which the reader sees nothing and is told nothing.
  Every state offers at least one way forward.
- **DR-186 — Empty Is Not Failure, Failure Is Not Empty**: A board that holds no entries and a board that
  could not be loaded are **different outcomes and must never be presented alike**. `Empty` is a successful
  read of an empty board; `Failed` is the absence of a read. They differ in wording, in what they offer the
  reader (an invitation to play versus a retry), and in what they are announced as. Specifically: a failure
  must **never** fall back to the empty state or to an empty table, and an empty board must **never** be
  reported as an error, a warning or a problem. This is the design trap of this feature — both produce "no
  rows" — and getting it wrong teaches the reader that the board loses scores.
- **DR-187 — The Empty Board Invites a First Game**: When `entryCount` is 0, the board shows a plain
  statement that no scores have been submitted yet and a clear action to go and play — not a table header
  with no rows under it, not a dash, not blank space, and nothing that reads as a malfunction. A brand-new
  deployment is the **first impression** every colleague gets exactly once, and the correct reaction to it
  is "I'll be first", not "this is broken".
- **DR-188 — Load Deadline**: A load that has not completed within **10 seconds** is abandoned, the
  progress indication is removed, and the attempt is reported as `Timeout`. There is no unbounded spinner,
  in any state. Ten seconds matches 009's **Submission Deadline (DR-128)** deliberately: one number for how
  long this product ever makes a colleague wait on the network.
- **DR-189 — No Silent Failure, No Dead End**: Every load reaches a visible outcome. A failure is stated in
  plain language naming what actually went wrong, never as a raw error, status code or stack trace, and is
  always accompanied by a **retry** — reading is idempotent (DR-195), so a retry is always safe and always
  offered. The reader is never left with no way forward: from any state they can retry, go back, or start a
  game.
- **DR-190 — A Failed Refresh Keeps the Rows**: If a refresh or a show-more fails while rows are already on
  screen, those rows **stay**, the state becomes `Stale`, and the reader is told the board could not be
  refreshed and may be out of date, with a retry. The table is never blanked and never replaced by an error
  panel. Slightly old standings are strictly more useful than none, provided the reader is told they are
  old — and blanking a board that was visible a second ago is indistinguishable, to the reader, from the
  scores having vanished.

**Freshness**

- **DR-191 — Fetched On View, Never Polled**: The board is fetched fresh each time it is viewed, and
  otherwise only when the reader asks via the explicit **refresh** action. There is **no polling, no
  auto-refresh, no live stream and no background update**. *Why:* a board that silently reorders itself
  while someone is reading it is actively unpleasant — the row they were looking at moves under their eyes
  — and an office board changes a handful of times a day, so continuous updating would spend network and
  attention to change nothing almost every time. On-demand freshness is both the cheaper and the kinder
  choice (§9.2).
- **DR-192 — Read Your Own Submission**: A read of the board **includes every entry whose submission was
  confirmed before the read was requested**. A player who submits, is told it succeeded (009 FR-008), and
  then opens the board **must** see their own entry there, with no manual refresh and no waiting. Any
  caching anywhere in the path is permitted only if it cannot violate this; a cache that can hide a
  just-confirmed entry is not allowed, however briefly. The confirmation and the board are the same claim
  made twice, and they must not contradict each other — **story 011 depends on this absolutely**, since it
  highlights an entry it has just been told exists.

**Reach and access**

- **DR-193 — Reachable Without Playing**: The board is reachable with **no precondition whatsoever**: no
  finished run, no submitted score, no prior game, no name, no account (story map). There is an entry point
  on the landing page (**story 012**) and one from the game-over state (**stories 008, 009**), and from the
  board a colleague can start a game or go back where they came from. The actor in this story is a
  *colleague*, who may never play at all; gating the board on having played would make the product
  unreadable to exactly the audience the bragging rights are aimed at.
- **DR-194 — The Board Has a Shareable Address**: The leaderboard has a stable address a colleague can
  bookmark or paste into a chat message. Opening it directly presents the default `Top` window (DR-173) and
  requires no prior navigation. An office board that cannot be linked to does not get looked at.
- **DR-195 — Reading Never Writes**: Viewing, refreshing, expanding or linking to the board creates,
  changes and deletes nothing — no entry, no counter, no "views" tally, no analytics event, no cookie and
  no personal datum of any kind. 009's **Minimal Personal Data (DR-131)** states that the typed name is the
  only personal datum this product collects; 010 collects nothing further and adds no second write path to
  a store that has exactly one. Reading is therefore idempotent, which is what makes DR-189's retry
  unconditionally safe, and it is why the board needs no authorisation: there is nothing here to protect
  that every colleague cannot already see, and no way for a reader to damage anything.

**Scale**

- **DR-196 — Design Point and Degradation**: The board is designed to hold up at **2,000 entries** — the
  realistic ceiling for an office game played over coffee breaks, not a public ladder. Within that, DR-176
  keeps render cost bounded by `windowSize` rather than `entryCount`, so the top ten costs the same whether
  the board holds 20 entries or 2,000. **Correctness never degrades with size**: the ordering, the ranking
  and the counts stay exact at any volume, because an approximate leaderboard is not a leaderboard. Beyond
  roughly **10,000 entries** the repeated show-more path becomes impractical for a human to use, and that
  is the signal to revisit paging and ordering cost — recorded as a known limit in arc42 §11, not designed
  around now.

**Accessibility**

- **DR-197 — The Board Is a Real Table**: The leaderboard is rendered as genuine tabular data: a table with
  an accessible name identifying it as the all-time leaderboard, a header row whose five column headers are
  programmatically associated with the cells beneath them, and one row per entry. A screen reader user can
  therefore move cell by cell and hear "Rank 4, Daan, 12,400, 31, 4" rather than five unlabelled numbers.
  **Rank is text in its own cell**, never implied by row position alone, so it survives being read out of
  order, copied, or reached by a direct jump. No meaning anywhere on the board is carried by colour alone
  (story 013 owns the colours; this constrains what they may be asked to do).
- **DR-198 — State Changes Are Announced**: Every transition between the DR-185 states — finishing loading,
  failing, going stale, and the arrival of new rows after show-more — is announced to assistive technology,
  politely rather than interrupting. All controls (show more, refresh, retry, play, back) are reachable and
  operable from the keyboard alone, in a sensible order, consistent with 008's NFR-005 and 009's FR-012. A
  reader who cannot see the table must not have to guess whether anything happened.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty).
009's NFRs for the submission path continue to apply unchanged; these cover the read path._

| ID      | Category         | Requirement                                                                                                                                                                                                                                      |
| ------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance      | **Time to first meaningful render of the top 10 ≤ 1 second at p95**, measured from activating a leaderboard entry point to the ten ranked rows being readable, on the office network with a board of **2,000 entries** (DR-196).                      |
| NFR-002 | Performance      | Producing a `LeaderboardView` — ordering, ranking and windowing — takes **≤ 100 ms at p95** for any `windowSize` up to 200 at 2,000 entries, and its cost grows no worse than logarithmically with `entryCount` for the default `Top` window.          |
| NFR-003 | Performance      | The payload for the default window is **independent of `entryCount`** (DR-176): the same ten rows plus a count, whether the board holds 20 entries or 20,000.                                                                                         |
| NFR-004 | Usability        | The five figures in a row are legible at arm's length on a mid-range Info Support laptop, and the digits of the Score column align on a common vertical axis so two scores can be compared by eye without being read. Typeface, tabular-figure settings and spacing tokens are **story 013's** — this states only what the data requires of them. |
| NFR-005 | Usability        | The default window of ten rows is fully readable **without horizontal scrolling** at a 1366×768 viewport and above, with every column present — no column is dropped, stacked or hidden to make it fit.                                               |
| NFR-006 | Reliability      | **Read-your-writes holds in 100 of 100 attempts**: submit a run, receive the confirmation, open the board, and the entry is present at its correct rank with no manual refresh (DR-192).                                                              |
| NFR-007 | Reliability      | No load ever remains unresolved: every attempt reaches `Ready`, `Empty`, `Stale` or `Failed` within **10 seconds** (DR-188), verified by injecting a never-responding backend.                                                                        |
| NFR-008 | Correctness      | The ordering is **total and reproducible**: ranking the same set of entries twice yields identical ranks, including across process restarts and for entries sharing a score and a `submittedAt` (DR-169).                                             |
| NFR-009 | Accessibility    | The board is navigable and comprehensible with a screen reader and keyboard alone: table semantics verified, every row's rank announced as text, every control reachable in tab order, every state change announced (DR-197, DR-198).                 |
| NFR-010 | Scalability      | At **10,000 entries** — five times the design point — ordering, ranking and the default window remain exactly correct, and NFR-001 degrades by no more than a factor of two. Beyond that is out of scope and recorded as a known limit (DR-196).       |
| NFR-011 | Security/Privacy | Viewing the board collects, derives, transmits and stores **nothing** about the reader — no account, no identifier, no cookie, no analytics event, no view counter (DR-195). The only personal data on screen is the names 009 collected for publication (**Minimal Personal Data (DR-131)**). |
| NFR-012 | Testability      | Ranking is assertable headlessly: given a fixed set of entries, the full ordered ranking and any window over it can be computed and compared without rendering, without a browser and without real elapsed time (consistent with 001 NFR-006).         |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                                   | Expected Behavior                                                                                                                                                                      |
| ----- | ------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| EC-1  | The board holds zero entries.                                                                                | `Empty` state: a statement that no scores have been submitted yet and an action to play the first game (DR-187). No table, no retry, nothing resembling an error (DR-186).                     |
| EC-2  | The board could not be loaded at all.                                                                        | `Failed` state: plain-language message and a retry (DR-189). It does **not** say the board is empty and does **not** render an empty table (DR-186).                                           |
| EC-3  | The board holds fewer than 10 entries — say 3.                                                               | All 3 rows shown, ranked 1–3. No padding rows, no placeholder dashes, no "7 slots remaining". Show-more is not offered; the reader is told this is the whole board.                             |
| EC-4  | The board holds exactly 10 entries.                                                                          | All 10 shown and show-more is **not** offered, because `entryCount` does not exceed `windowSize` (FR-009).                                                                                      |
| EC-5  | Two entries have the same score **and** the same `submittedAt` to the stored precision.                      | Ordered by `entryId` ascending (DR-169) and given consecutive ranks. Arbitrary but **stable** — the same two reads always agree (NFR-008).                                                      |
| EC-6  | Many entries share a score — e.g. thirty runs all scoring 0.                                                 | All thirty are ranked distinctly and consecutively, earliest-submitted first. No grouping, no "30 tied at 0" summary row (DR-170, DR-180).                                                      |
| EC-7  | A new entry is submitted by someone else between the reader's load and their show-more.                      | The extended window is computed from the board **as it is now**; a row may appear to shift by one rank between the two chunks. Acceptable and expected (DR-172). The reader is not warned, and nothing is deduplicated or reconciled. |
| EC-8  | The same entry appears twice in a view because of EC-7's shifting.                                            | Must not happen: each extension is a fresh window over a single consistent read, so a view never contains the same `entryId` twice. If an implementation cannot guarantee this, it re-reads the whole window rather than appending. |
| EC-9  | A `FromRank` window is requested starting past the end of the board.                                         | A successful view with empty `rows` and the true `entryCount`. Not an error, and not the `Empty` state — `Empty` means the **board** is empty (DR-186).                                         |
| EC-10 | A `Containing` window is requested for an `entryId` that is not on the board.                                | The `Top` window plus a not-found indication (§5.4). No error. What **story 011** says about it is 011's decision.                                                                             |
| EC-11 | A name is 20 full-width CJK characters, or 20 emoji.                                                          | Rendered within the bounded Player column, truncated visibly if it does not fit, full value available (DR-182). The Score, Lines and Level columns do not move (DR-181).                        |
| EC-12 | A name is written right-to-left, or mixes scripts.                                                            | Rendered in its own directional context inside the Player column (DR-183). The rank and the three figures keep their positions and reading order.                                              |
| EC-13 | Two entries carry names that are visually indistinguishable.                                                  | Two ordinary rows, no annotation, no warning, no disambiguation (DR-184). The product does not know whether they are one person.                                                               |
| EC-14 | A refresh fails while ten rows are on screen.                                                                 | `Stale`: the ten rows stay, the reader is told they could not be refreshed and may be out of date, a retry is offered (DR-190). The table is never blanked.                                    |
| EC-15 | The load takes longer than 10 seconds.                                                                        | Abandoned, progress indication removed, `Failed` with reason `Timeout`, retry offered (DR-188).                                                                                               |
| EC-16 | The reader hammers show-more, or refresh, repeatedly.                                                          | Harmless — reading is idempotent (DR-195). Requests are not queued into a backlog; the latest window wins and the rank numbering stays consistent. Nothing is created or changed.               |
| EC-17 | The reader opens the board, backgrounds the tab for an hour, and returns.                                      | The rows shown are exactly as they were — there is no polling and no catch-up (DR-191). They are not silently updated, and not falsely presented as current beyond what refresh provides.       |
| EC-18 | A score of 0 is among the top ten, because the board holds only a few entries.                                 | Shown as an ordinary row reading 0 (**Zero-Score Runs Are Submittable (DR-130)**). Never hidden, greyed, mocked or filtered out.                                                                |
| EC-19 | The board holds 2,000 entries and the reader expands repeatedly.                                               | Each expansion stays within NFR-002; only the requested window is transferred (DR-176). Correctness is unaffected at any volume (DR-196).                                                       |
| EC-20 | Someone links directly to the board's address before any game has ever been played.                            | The address resolves and shows the `Empty` state (EC-1). A link that is shared before the first game still works.                                                                             |
| EC-21 | A reader with no pointing device wants to see past the top ten.                                                | Show-more is a real control in the tab order, operable by keyboard, and the arrival of new rows is announced (DR-198).                                                                        |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                                        |
| ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 25 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                                    |
| SUC-002 | Each of the thirty domain rules DR-169 to DR-198 is covered by at least one automated test.                                                                                          |
| SUC-003 | A fixed fixture of 50 entries — including exact score ties, identical `submittedAt` values, a zero score and duplicate names — produces one expected, byte-for-byte ranking, twice in a row and across a restart (NFR-008). |
| SUC-004 | The `Empty` and `Failed` states are distinguishable by their rendered text alone, with colour and iconography removed, confirmed by a test that asserts on text content (DR-186).      |
| SUC-005 | With a backend that never responds, the board reaches `Failed` with a working retry within 10 seconds, in 10 of 10 runs (NFR-007).                                                    |
| SUC-006 | In 100 consecutive submit-then-view sequences, the submitted entry is present on the board every time, at the correct rank, with no manual refresh (NFR-006).                         |
| SUC-007 | Against a seeded board of 2,000 entries, the top ten renders within 1 second at p95, and the payload for that window is identical in size to the payload for a 20-entry board (NFR-001, NFR-003). |
| SUC-008 | A row whose name is 20 full-width characters and a row whose name is "Jan" place their Score, Lines and Level columns at pixel-identical horizontal positions (NFR-004, DR-181).       |
| SUC-009 | A screen reader user completes "tell me who is in the top three and what they scored" and "show me more of the board" using the keyboard alone, without sighted assistance (NFR-009).  |
| SUC-010 | **Story 011 can be built entirely against §5's `LeaderboardView`, `RankedEntry` and the `Containing` window mode, without 010 changing and without 009 gaining a stored attribute.**   |
| SUC-011 | In informal office use over one week, no colleague reports being unable to find the leaderboard, or mistaking an empty board for a broken one.                                        |

## 9. Dependencies and Constraints

### 9.1 Dependencies

**Upstream — 010 cannot be completed before these:**

- **Story 009 (submit a score)** — the whole of it. 009 defines `LeaderboardEntry` and every attribute 010
  displays or orders by, guarantees durability (**Entry Durability (DR-121)**), guarantees one shared board
  (**DR-122**), guarantees a stable `entryId` (**DR-119**), and guarantees — via **DR-110** and **DR-111** —
  that no name reaching this board is over-long or carries control or format characters. Without 009 there
  is nothing to read. 010 adds nothing to 009's model and requires no change to it (DR-132, SUC-010).
- **Stories 006 and 007** — indirectly, as the origin of `score`, `linesCleared` and `level`. 010 reads
  them through 009 and never recomputes them (DR-178).
- **The storage decision deferred by 009** (009 open question 1, arc42 §09) — 010's NFR-001 and NFR-002
  cannot be met without knowing how the board is ordered and windowed at the source (DR-176). The *spec*
  does not depend on the answer; the *implementation* does.

**Downstream — these depend on 010:**

- **Story 011 (highlight the just-finished run)** — depends on this spec for its entire vocabulary: the
  ordering (**DR-169**), sequential ranking (**DR-170**), the `LeaderboardView` / `RankedEntry` read model
  (§5), the `windowStart` / `windowSize` / `entryCount` triple, and above all the **`Containing` window
  mode (DR-175)**, which exists for 011 and nothing else. 011 must reuse these names verbatim and must not
  introduce a second ordering, a second ranking scheme or a stored rank. Read-your-writes (**DR-192**) is a
  hard precondition for 011: it highlights an entry it has just been told exists.
- **Story 012 (landing page)** — hosts one of the two required entry points (FR-014, DR-193). 012 owns
  where it sits and what it looks like; 010 owns only that it exists.
- **Story 013 (Info Support branding)** — styles this table: colours, typeface, tabular figures, row
  rhythm, the empty and failed states' visual treatment. 013 must preserve NFR-004, NFR-005, DR-181 and
  DR-197 — in particular it may not make the Player column elastic, nor carry any meaning by colour alone.
- **Story 008 / 009 (game over and submission)** — host the second entry point. 008's open question 4
  deliberately left "does the game-over state link to the leaderboard?" to this spec; **the answer is yes**
  (FR-014, open question 8 below), and 008 and 009 own where that link sits on their screen.

**Other:**

- **`docs/product/story-map.md`** — source of the story numbering, the *colleague* actor, and the
  project-wide exclusions quoted in §1.3.

### 9.2 Constraints

- **The scores on this board are forgeable, and the board publishes them anyway.** 008 §9.2 records that
  the figures are computed client-side; 009 §9.2 accepted that and added only proportionate sanity
  validation (**DR-123**). 010 inherits the consequence: a row on this board means "someone submitted these
  figures", not "someone achieved these figures". This is an accepted trade-off for an internal office game
  with no stakes beyond bragging rights, stated here because the board is where the consequence becomes
  *visible* and permanent. 010 adds no validation, no verification and no badge of trustworthiness.
- **Entries cannot be removed, and the board has no moderation path** (**DR-118**, **DR-113**). Whatever is
  on the board is on it for ever. This is precisely why DR-181 – DR-183 exist: a layout that one permanent
  entry can break is broken permanently, so the layout must be immune to its content rather than rely on
  cleaning the content up.
- **No accounts, no authentication, no authorisation** (story map). The board is readable by anyone who can
  reach the application. This is deliberate and costless: every colleague who can read it can already play
  and submit, there is no private data on it beyond names given knowingly for publication (**DR-131**), and
  it has no write path at all (**DR-195**).
- **No per-name aggregation is possible, not merely disallowed.** **DR-112** and **DR-114** mean the
  product holds nothing that could group two runs as one person's. Any "top players" view would be a
  fabrication. This constrains every future story, not just this one.
- **On-demand freshness, not live.** The board is a snapshot as of its `generatedAt` (DR-191). A reader may
  be looking at standings a few minutes old; they are never told those standings are current when they are
  not (DR-190), and refresh is always one action away. Live updating was considered and rejected: it would
  move rows under a reader's eyes to reflect a change that happens a few times a day.
- **Platform**: desktop browser on a laptop with a keyboard; fully operable from the keyboard (FR-028). No
  mobile or touch support (story map).
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed by this spec):
  ASP.NET Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`.
  Whether the board is server-rendered or fetched by the browser is an open architecture decision; nothing
  in §3 – §5 changes either way, provided DR-176 and DR-192 hold.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*, matching the approach taken in 001 §9.3, 008 §9.3 and 009 §9.3.

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                              |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 01. Introduction and Goals (`01-introduction-and-goals.md`)       | Should record that the leaderboard is **readable by a colleague who never plays** (DR-193) — the product has two audiences, not one. *Currently empty.*                                       |
| 03. System Scope and Context (`03-system-scope-and-context.md`)   | 010 adds the **read direction** to the boundary 009 opened: the browser now pulls shared, durable data as well as pushing it. Should be drawn as such. *Currently empty.*                     |
| 05. Building Block View (`05-building-block-view.md`)             | Should show the Compete building block with a **read side** producing a `LeaderboardView` and a **write side** producing a `LeaderboardEntry`, sharing one store and nothing else. *Currently empty.* |
| 06. Runtime View (`06-runtime-view.md`)                           | Needs the read scenario: request a window → order and rank at the source → return rows plus `entryCount` → render; plus the refresh and show-more variants and the failure paths. *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)       | Needs two concepts shared with 009 rather than reinvented per feature: the **10-second network deadline** (DR-128, DR-188), and the **load/failure state vocabulary** including the empty-versus-failed distinction (DR-185, DR-186). Also the table and figure-alignment concept 013 will implement. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`)       | 010 warrants ADRs for: **ordering and ranking at the source rather than in the browser** (DR-176); **sequential ranking with a total order** rather than dense or competition ranking (DR-169, DR-170); and **on-demand freshness rather than polling** (DR-191). It also depends on 009's deferred storage ADR. *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)           | Should host the project-wide read-latency and accessibility scenarios that NFR-001 and NFR-009 are instances of, and the read-your-writes consistency scenario (NFR-006). *Currently empty.*  |
| 11. Risks and Technical Debt (`11-risks-and-technical-debt.md`)   | Should carry: the **10,000-entry limit** beyond which repeated show-more is impractical (DR-196); the **forgeable scores now published permanently** risk (§9.2); and the **no removal path** for an entry that turns out to be unwelcome (DR-113, DR-118). *Currently empty.* |
| 12. Glossary (`12-glossary.md`)                                   | Should gain: leaderboard, board ordering, rank, ranked window, window start/size, entry count, show more, stale board, empty board. *Currently empty.*                                        |

## 10. Open Questions

| #   | Question                                                                                                | Owner      | Status   | Resolution                                                                                                                                                                                                                                                                      |
| --- | --------------------------------------------------------------------------------------------------------- | ---------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Dense ranking (1, 1, 2), standard competition ranking (1, 1, 3), or sequential (1, 2, 3)?                 | Daan Harms | Resolved | **Sequential** (DR-170). Because DR-169's order is total, no genuine tie survives to display, so shared ranks would be a fiction. Rank *n* then always means "the *n*th row", which is what a reader counting down expects and what lets story 011 say "you placed 14th" flatly.   |
| 2   | How is a tie on score broken?                                                                             | Daan Harms | Resolved | **`submittedAt` ascending** — whoever reached that score **first** ranks higher (DR-169). It is the intuitive reading of a tie and the only one where a rank, once earned, is lost only to someone who actually beat you. `entryId` ascending is a deterministic last resort, never a meaningful one. |
| 3   | How many entries by default, and what does "see more" do?                                                 | Daan Harms | Resolved | **Top 10 by default**, show-more growing the window 10 → 50 → 100 → +50 (DR-173, DR-174). 10 answers "who is winning" without scrolling; 50 covers an office board in one more click. Crucially, the window is also addressable **by the entry it contains** (DR-175) — that is how **story 011** finds a run at rank 237 without 47 clicks. |
| 4   | Should the board show when each score was submitted?                                                      | Daan Harms | Resolved | **No date column** (DR-179). `submittedAt` is carried and used as the tie-break, but an all-time board is not a timeline; a sixth column costs width the names need and invites a "recent scores" reading the story map excludes. The data is stored, so a later story can surface it with no change to 009. |
| 5   | Should the board poll, cache or refetch?                                                                  | Daan Harms | Resolved | **Fetched fresh on each view, refreshed only on request** (DR-191), with caching permitted only where it cannot hide a just-confirmed entry (DR-192). An office board changes a few times a day; polling would spend network and attention to change nothing, and would move rows under a reader's eyes. |
| 6   | Should the leaderboard require any authentication?                                                        | Daan Harms | Resolved | **No** (DR-195, story map). There is no write path, no private data beyond names given knowingly for publication (**DR-131**), and every colleague who can reach the page can already play and submit. Authentication would add a barrier that protects nothing.                      |
| 7   | Truncate, wrap, shrink or scroll a name too wide for its column?                                          | Daan Harms | Resolved | **Truncate visibly, with the full name available** (DR-182), inside a Player column of bounded width (DR-181). Wrapping would make rows different heights and break the vertical scan down the Score column; shrinking the text would make exactly the wide-script names hardest to read. |
| 8   | Does the game-over state link to the leaderboard? (008 open question 4 deferred this here.)               | Daan Harms | Resolved | **Yes** (FR-014, DR-193) — reaching the board from game over is required, alongside an entry point on the landing page. **Where** it sits on that screen is 008's and 009's layout decision; 010 requires only that it exists and that following it needs no submitted score.          |
| 9   | Should an entry's own rank be stored so it does not have to be recomputed?                                | Daan Harms | Resolved | **No** (DR-171). A stored rank would be wrong the moment anyone else submits, and **Entry Immutability (DR-118)** forbids updating one. **Ranking and Presentation Are Deferred (DR-132)** explicitly requires 010 to need no new stored attribute, and it needs none (SUC-010).       |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-006, SC-025; US-002 -> SC-007..SC-010;
        US-003 -> SC-011..SC-013; US-004 -> SC-014, SC-015; US-005 -> SC-015..SC-018;
        US-006 -> SC-021; US-007 -> SC-019, SC-020; US-008 -> SC-022..SC-024
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-007 -> US-001; FR-008..FR-013 -> US-002; FR-014..FR-017 -> US-003;
         FR-018, FR-019 -> US-004; FR-019..FR-023 -> US-005; FR-026..FR-028 -> US-006;
         FR-024, FR-025 -> US-007; FR-029..FR-032 -> US-008)
  - [x] Domain model covers all entities mentioned in the requirements
        New (read model only, nothing persisted): LeaderboardView, LeaderboardLoad,
        RankedEntry, WindowRequest, LoadFailure, LeaderboardLoadState, WindowMode,
        LoadFailureReason. Reused verbatim from 009: LeaderboardEntry, PlayerName.
        No new stored attribute is required of 009 (DR-132, SUC-010).
  - [x] Domain rules and invariants are listed (DR-169 .. DR-198; DR-110..DR-132 cited from 009)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-21, including the
        empty-versus-failed trap at EC-1/EC-2 and the failed-refresh case at EC-14)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-012)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (all nine resolved)
-->
