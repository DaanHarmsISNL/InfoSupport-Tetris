# Feature Specification: Highlight My Just-Finished Run on the Leaderboard

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
| Feature ID      | 011                                                                                                 |
| Status          | Draft                                                                                               |
| Author          | Daan Harms                                                                                          |
| Created         | 2026-10-20                                                                                          |
| Last updated    | 2026-10-20                                                                                          |
| Epic / Parent   | Compete (story map activity) — `docs/product/story-map.md`                                          |
| Arc42 reference | Sections 3, 5, 6, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders.         |

> **Note on this spec.** 011 closes the *Compete* activity and is deliberately the smallest of its three
> specs. It adds **one mark and one sentence** to a board that story 010 has already drawn: *that row is
> yours, and this is where you landed.* It defines no entity that outlives a page load, writes nothing, and
> changes neither the storage model nor the ranking.
>
> **Vocabulary.** `LeaderboardEntry`, `ScoreSubmission`, `PlayerName`, `SubmissionStatus` and
> `SubmissionFailureReason` are defined by [`009 §5`](009-submit-score.md#5-domain-model) and are reused
> verbatim, never restated. `CompletedRun` and `runId` come from [`008 §5`](008-game-over.md#5-domain-model);
> `score` and `linesCleared` from [`006 §5`](006-line-clears-and-scoring.md#5-domain-model); `level` from
> [`007 §5`](007-levels-and-speed.md#5-domain-model). **Ranking, paging, the table itself and every row of it
> belong to story 010** and are cited, never redefined (DR-210). Cited rules are named by ID, e.g. "per
> **Entry Identity (DR-119)**".
>
> **Rule numbering.** New rules introduced here run from **DR-199 to DR-218**, inside this spec's reserved
> block DR-199 – DR-228, so that IDs stay globally unique across concurrently written specs. DR-01 – DR-138
> belong to specs 001 – 009, DR-139 – DR-168 to story 013 and DR-169 – DR-198 to story 010; all are cited
> only.
>
> **Dependency on story 010.** At the time of writing, `docs/specs/010-view-leaderboard.md` does not yet
> exist. This spec is written against the three facts 010 has been given to establish: entries are ranked by
> **`score` descending, ties broken by `submittedAt` ascending**; the **top 10** is the default view; and
> there is an affordance to see more, designed so that a run outside the top 10 can still be located. This
> spec names those as 010's and nothing here redefines them. If 010 lands with different numbers — a default
> view of 20, say — only the *illustrations* in this spec change, not a single requirement (DR-210).

### 1.1 Problem Statement

Story 009 lets a player stamp their name on the board and story 010 draws the board, but between the two
sits the question the player actually asked when they typed their name: *where did I land?* On a shared
all-time board that will hold hundreds of entries, a casual player's first run will usually not make the top
10 — so the board 010 shows them by default is a list of other people, with no answer in it. Scanning a
long table for your own name does not work either: two colleagues may share a first name (009 DR-112), the
same person may have ten entries (009 DR-114), and a near-identical score sits a row away.

### 1.2 Goal

The moment a run is successfully submitted, the player sees the board with **their** row unmistakably marked
as theirs and is told in plain words where they placed — "You placed 47th of 183" — whether or not that row
made the default top 10. If it did not, the row is reachable without hunting: the board shows it in context
with its immediate neighbours. The mark follows the one entry the server vouched for, so it is right even
when the row above carries the same name and a near-identical score. It lasts exactly as long as it is true
— through scrolling and expanding the board — and is gone the moment the player starts a new game or the
next colleague walks up to the laptop.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec. Each is either a separate story, a project-wide
exclusion, or an architecture decision.

- **Drawing the leaderboard** — **story 010**. Ordering, tie-breaking, paging, how many rows are shown by
  default, the columns, date formatting and where the board is reached from are all 010's (009 DR-132).
  011 marks one of 010's rows and adds one sentence beside the board (DR-210).
- **Submitting the score, name entry, validation, retry and idempotency** — **story 009**. 011 consumes
  exactly two things 009 produces: `ScoreSubmission.status` and `ScoreSubmission.entryId` (DR-203).
- **A persistent "my scores" page, a run history, or anything that remembers the player between visits.**
  There are no accounts and no sessions (story map). The highlight is transient client-side state and is
  gone on reload (DR-207). Nothing in this spec is written anywhere (DR-218).
- **Comparing this run against the player's previous runs** — "your personal best", "+2,400 since last
  time", "you beat your record". All of these require linking two runs to one person, which this product
  cannot do and will never do (009 DR-112). Two entries sharing a name are not evidence of a person.
- **Notifying the player when someone later overtakes them.** The board is a moving target by design
  (DR-209); it is read when it is read. There is no push, no poll, no "you've been knocked off the top 10".
- **Animating rank changes, counting the rank up, or any motion that moves the board under the player.**
  The row does not slide, rise, pulse or scroll into place (DR-214).
- **Sharing a placement** — no share link, no copyable URL, no image, no "I came 3rd!" button. A URL that
  labels an entry as somebody's run is actively wrong the moment it is pasted into a chat (DR-205).
- **Reordering, pinning or promoting the player's row.** It stays exactly where the ranking puts it
  (DR-201). A board that moves your row to the top is no longer a leaderboard.
- **Editing, deleting or re-submitting the highlighted entry.** Entries are immutable (009 DR-118). The
  highlight is a mark on a row, not a handle on it.
- **Highlighting anything other than the just-finished run** — not the top score, not a friend, not entries
  matching a name. There is exactly one highlight and it is the most recent successful submission (DR-217).
- **Accounts, authentication, sessions, weekly resets, seasons, challenges, streaks** — out of scope
  project-wide (story map).
- **The visual design of the highlight** — colours, weight, borders and the brand tokens — are **story 013**.
  This spec fixes what the highlight must *convey* and what it must never rely on (DR-215), not how it looks.
- **Mobile, touch and on-screen keyboards** — out of scope project-wide (laptop + keyboard only).

## 2. User Stories

### US-001: Find my row at a glance

**As a** player who has just submitted a run,
**I want** my own row on the leaderboard marked unmistakably as mine,
**so that** I can see where I landed without reading the whole table.

### US-002: Find out where I landed even though I didn't make the top 10

**As a** casual player whose first run scored 3,200,
**I want** to be told my placement and shown my row in context, even though it is nowhere near the top 10,
**so that** the answer to "how did I do?" is on the screen instead of thirty rows down a list I have to hunt
through.

### US-003: Be sure the marked row is actually mine

**As a** player who shares a first name with two colleagues and scored within a hundred points of one of them,
**I want** exactly one row marked, and for it to be the run I just finished,
**so that** I am not reading somebody else's score and thinking it is mine.

### US-004: Know what the number means

**As a** player told I placed 12th,
**I want** to understand that this is where I stood when the board was read, not a position I now hold,
**so that** I am not confused or cheated when a refresh a minute later says 13th.

### US-005: See the mark without relying on colour

**As a** colleague who is colour-blind or uses a screen reader,
**I want** the marked row to say in words that it is mine, and to hear my placement,
**so that** the feature works for me at all rather than being a tint I cannot perceive.

### US-006: Not inherit somebody else's highlight

**As the** next colleague to walk up to the shared laptop,
**I want** the board to show no highlight at all unless I have just submitted a run myself,
**so that** the game never tells me that a stranger's score is mine.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) defined in this spec are
listed in §5.5; rules DR-01 – DR-198 are defined in specs 001 – 010 and 013 and are only cited here._

| ID     | Requirement                                                                                                                                                                                                                                  | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall identify the player's row on the leaderboard **solely** by matching `LeaderboardEntry.entryId` against the `entryId` returned by the successful submission, per **Entry Identity (DR-119)** (DR-199).                           | Must     | US-001     |
| FR-002 | The system shall never identify the player's row by `playerName`, by `score`, by any combination of displayed values, or by position in the list (DR-199, 009 DR-112).                                                                           | Must     | US-003     |
| FR-003 | The system shall mark **at most one** row as the player's run in any view of the board, and shall mark exactly one whenever the matching entry is present in what is displayed (DR-200).                                                        | Must     | US-003     |
| FR-004 | The system shall leave the row in the position story 010's ranking gives it, never moving, pinning, promoting, duplicating or re-sorting it because it is highlighted (DR-201, DR-210).                                                         | Must     | US-001     |
| FR-005 | The system shall mark only a row the board already contains, never adding, injecting or synthesising a row for the player's entry (DR-202).                                                                                                    | Must     | US-001     |
| FR-006 | The system shall tell the player their placement in words, stating their **rank** and the **total number of entries** on the board — for example "You placed 47th of 183" (DR-208).                                                              | Must     | US-002     |
| FR-007 | The system shall derive that rank from story 010's ordering — `score` descending, ties broken by `submittedAt` ascending — and shall not define, vary or re-implement an ordering of its own (DR-210).                                           | Must     | US-002     |
| FR-008 | The system shall, when the player's row falls **inside** the board's default view, highlight it in place and change nothing else about the view: no scroll, no jump, no expansion, no extra rows (DR-211, DR-214).                               | Must     | US-001     |
| FR-009 | The system shall, when the player's row falls **outside** the default view, additionally show that row together with its **two nearest-ranked neighbours above and below**, appended below the default view behind a labelled break (DR-212).    | Must     | US-002     |
| FR-010 | The system shall drop the appended context once the player expands the board to a view that already contains their row, so that the row is never shown twice (DR-213).                                                                          | Must     | US-002     |
| FR-011 | The system shall omit the break and simply continue the rows when the player's context is contiguous with the default view, and shall omit any neighbour that does not exist because the row is at the top or bottom of the board (DR-213).      | Should   | US-002     |
| FR-012 | The system shall never move the viewport on the player's behalf — no auto-scroll, no scroll-into-view, no focus jump and no animation of the row into position (DR-214).                                                                        | Must     | US-001     |
| FR-013 | The system shall keep the highlight and the placement statement in place while the player scrolls the page, expands the board, pages through it or re-reads it, for as long as the highlight is live (DR-206).                                  | Must     | US-001     |
| FR-014 | The system shall show a highlight only when a `ScoreSubmission` for the current browser view has reached `Submitted` and carries an `entryId` (DR-203, 009 §5.1).                                                                               | Must     | US-006     |
| FR-015 | The system shall render the board entirely normally — correctly ranked, with no highlight, no placement statement, no empty marker and no error — when the submission status is `Unsubmitted`, `Submitting`, `Failed` or `Declined` (DR-203).    | Must     | US-006     |
| FR-016 | The system shall end the highlight when the player starts a new game, reloads the page, navigates away or closes the tab, and shall not restore it afterwards (DR-207).                                                                         | Must     | US-006     |
| FR-017 | The system shall not place the highlighted `entryId` anywhere that can be bookmarked, copied, shared or reopened — in particular not in the page address — so that no link exists that labels an entry as another person's run (DR-205).         | Must     | US-006     |
| FR-018 | The system shall highlight only the **most recent** successful submission; a run submitted earlier in the same visit is not highlighted and no history of highlights is kept (DR-217).                                                          | Must     | US-006     |
| FR-019 | The system shall convey the highlight by a **textual marker** such as "your run" on the row itself, in addition to any visual treatment, and shall never rely on colour alone (DR-215, story 013; 004 makes the same point).                    | Must     | US-005     |
| FR-020 | The system shall make the placement statement available to assistive technology as text, and shall describe the break above an appended context in words rather than by a visual separator alone (DR-216).                                       | Must     | US-005     |
| FR-021 | The system shall present the rank as the standing at the moment the board was read, and shall not claim, imply or cache it as a position the player continues to hold (DR-209).                                                                 | Should   | US-004     |
| FR-022 | The system shall recompute the rank and the highlight from whatever the board returns on each read, so that a refresh showing a different rank is a correct outcome rather than an inconsistency (DR-209).                                      | Must     | US-004     |
| FR-023 | The system shall write nothing as a result of highlighting — no attribute on `LeaderboardEntry`, no server-side state, no cookie, no stored identifier (DR-218, 009 DR-118, DR-131).                                                            | Must     | US-006     |
| FR-024 | The system shall show the board with no highlight and no error when the submitted entry is not present in what the board returns, suppressing the placement statement rather than inventing a rank (DR-202).                                     | Should   | US-003     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash.
"A just-submitted run" means a `ScoreSubmission` has reached `Submitted` and carries the `entryId` the server
returned (009 FR-008, **Entry Identity (DR-119)**). "The default view" is whatever story 010 shows before the
player expands the board — the top 10. Illustrative board sizes and ranks are examples, not constants._

### SC-001: A top-10 run is highlighted where it already sits (FR-001, FR-003, FR-008)

```gherkin
Given the board holds 183 entries
  And the player has just submitted a run that ranks 4th, with entry id E
When the leaderboard is shown
Then the row whose entryId is E is marked as the player's run
  And it is the only marked row
  And it is still in 4th position, between the 3rd and 5th rows
  And the view has not scrolled, expanded or added any row
  And the player is told "You placed 4th of 183"
```

### SC-002: A run outside the top 10 is still findable (FR-006, FR-009)

```gherkin
Given the board holds 183 entries
  And the player has just submitted a run that ranks 47th, with entry id E
When the leaderboard is shown
Then the player is told "You placed 47th of 183"
  And the default view still shows the top 10, unchanged and in order
  And below it, behind a labelled break, rows 45 to 49 are shown
  And the row whose entryId is E — rank 47 — is marked as the player's run
  And no row has scrolled or moved on its own
```

### SC-003: Expanding the board absorbs the appended context (FR-010, FR-013)

```gherkin
Given the player's run ranks 47th and is shown in an appended context below the top 10
When the player expands the board to see more entries, far enough to include rank 47
Then rank 47 is marked as the player's run in the main list, in place
  And the appended context is gone
  And the row appears exactly once in the view
  And the placement statement still reads "You placed 47th of 183"
```

### SC-004: A run just below the default view needs no break (FR-011)

```gherkin
Given the board holds 60 entries
  And the player's run ranks 11th
When the leaderboard is shown
Then rows 11 to 13 follow row 10 directly, with no break and no gap marker
  And row 11 is marked as the player's run
  And the player is told "You placed 11th of 60"
```

### SC-005: A run at the very bottom has no neighbours below it (FR-011)

```gherkin
Given the board holds 183 entries
  And the player's run ranks 183rd — last
When the leaderboard is shown
Then the appended context shows rows 181 to 183 and nothing after
  And row 183 is marked as the player's run
  And no empty, placeholder or "no more entries" rows are added to fill the space
  And the player is told "You placed 183rd of 183"
```

### SC-006: Topping the board needs no context at all (FR-006, FR-008)

```gherkin
Given the board holds 183 entries
  And the player has just submitted the highest score on it
When the leaderboard is shown
Then the first row is marked as the player's run
  And no context is appended
  And the player is told "You placed 1st of 183"
```

### SC-007: A colleague with the same name and a near-identical score is not marked (FR-002, FR-003)

```gherkin
Given the board holds an entry named "Daan" with a score of 12,450, entry id X
  And the player has just submitted a run named "Daan" with a score of 12,400, entry id E
  And the two rows are adjacent on the board
When the leaderboard is shown
Then only the row whose entryId is E is marked as the player's run
  And the row whose entryId is X is not marked, highlighted or annotated in any way
  And exactly one row on the board is marked
```

### SC-008: A tied score marks the right one of the two rows (FR-001, FR-003, FR-007)

```gherkin
Given two entries share a score of 9,000 — entry X submitted first and entry E submitted second
  And story 010's tie-break puts X above E
  And the player has just submitted the run whose entry id is E
When the leaderboard is shown
Then the lower of the two rows, whose entryId is E, is marked as the player's run
  And X is not marked
  And the stated rank is E's rank, one below X's
```

### SC-009: An earlier run from the same visit is not marked (FR-018)

```gherkin
Given the player submitted a run scoring 4,000 earlier in this visit
  And they then played again and have just submitted a second run scoring 11,000, entry id E
When the leaderboard is shown
Then only the row whose entryId is E is marked as the player's run
  And the 4,000 entry appears on the board as an ordinary row
  And the placement statement refers to the 11,000 run
```

### SC-010: Skipping the submission leaves the board plain (FR-015)

```gherkin
Given a run finished and the player skipped submission, so the submission is Declined
When the leaderboard is shown
Then the board renders normally, correctly ranked
  And no row is marked
  And no placement statement is shown
  And no error, warning or "your run is not on the board" message appears
```

### SC-011: A failed submission leaves the board plain (FR-015)

```gherkin
Given a submission failed and its status is Failed
When the leaderboard is shown
Then no row is marked and no placement statement is shown
  And the board shows no error of its own
  And story 009's failure message and retry remain available on the game-over screen
When the player retries and the submission succeeds
Then the board shows the highlight and the placement statement
```

### SC-012: A reload clears the highlight (FR-016)

```gherkin
Given the player's just-submitted run is highlighted at rank 47
When the player reloads the page
Then the board renders normally with no row marked
  And no placement statement is shown
  And no error is shown
  And the entry itself is still on the board at rank 47, as an ordinary row
```

### SC-013: Starting a new game clears the highlight (FR-016)

```gherkin
Given the player's just-submitted run is highlighted
When the player starts a new game
Then the highlight ends immediately
  And returning to the board shows no marked row and no placement statement
  And the highlight does not reappear when the new game ends
  And it reappears only if the new run is itself submitted successfully
```

### SC-014: The next colleague sees nobody's highlight (FR-014, FR-016)

```gherkin
Given a colleague submitted a run on this laptop and saw it highlighted
When they walk away and a different colleague opens the page
Then the board shows no marked row
  And no placement statement is shown
  And nothing on the screen suggests that any entry belongs to the person at the keyboard
```

### SC-015: The board is a moving target (FR-021, FR-022)

```gherkin
Given the player was told "You placed 12th of 183"
  And another colleague then submits a higher score
When the player refreshes the board
Then their row is still marked as their run
  And the placement statement now reads "You placed 13th of 184"
  And this is reported as the current standing, not as an error or a correction
```

### SC-016: The highlight never moves the row or the page (FR-004, FR-012)

```gherkin
Given the player's run ranks 47th
When the leaderboard is shown
Then the top 10 is exactly what it would be with no highlight at all
  And the player's row is not pinned, promoted or duplicated into the top 10
  And the page does not scroll, animate or move focus on its own
  And the player's current scroll position is unchanged
```

### SC-017: The highlight survives moving around the board (FR-013)

```gherkin
Given the player's run is highlighted and the placement statement is shown
When the player scrolls the page, expands the board and pages through further entries
Then the row remains marked wherever it is visible
  And the placement statement remains readable throughout
  And neither is lost, reset or duplicated by any of those actions
```

### SC-018: The mark does not depend on sight of colour (FR-019, FR-020)

```gherkin
Given the player's run ranks 47th and is marked
When the board is rendered in greyscale, or with colour overridden by the operating system
Then the row still carries the textual marker "your run"
When a screen reader reads the board
Then it announces the placement "You placed 47th of 183"
  And it announces the marked row as the player's run
  And the break above the appended context is read as words, not skipped as decoration
```

### SC-019: An entry the board does not return is not invented (FR-005, FR-024)

```gherkin
Given a submission succeeded and returned entry id E
  And the board, when read, does not contain an entry with id E
When the leaderboard is shown
Then no row is marked
  And no placement statement is shown
  And no row is fabricated, appended or synthesised for E
  And the board renders normally with no error of its own
```

### SC-020: There is no link that hands the highlight to someone else (FR-017)

```gherkin
Given the player's run is highlighted at rank 47
When the player copies the page address and sends it to a colleague
  And the colleague opens it
Then the colleague sees the board with no marked row and no placement statement
  And the address contains no entry identifier
When the player bookmarks the page and reopens it tomorrow
Then no highlight is restored
```

## 5. Domain Model

_This spec introduces **no stored entity and no new attribute on any existing one**. `LeaderboardEntry`,
`ScoreSubmission`, `PlayerName`, `SubmissionStatus` and `SubmissionFailureReason` are
[009 §5](009-submit-score.md#5-domain-model)'s, used verbatim and left untouched (DR-218). The ranked list,
its ordering, its paging and its rows are **story 010's** and are referred to here as "the board" without
being redefined (DR-210). What follows is one piece of transient, client-side state and the two value
objects it needs._

### 5.1 Entities

#### RunHighlight

The client's answer to "which row is mine, and where did I land". **Transient and client-side**: it comes
into existence when a `ScoreSubmission` reaches `Submitted`, lives as long as the player is still looking at
the result of that run, and is never persisted, transmitted or written anywhere (DR-218). It is the only
thing this spec adds to the model.

| Attribute      | Type                     | Constraints                                                               | Description                                                                                                        |
| -------------- | ------------------------ | ------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| entryId        | UUID                     | required, immutable, equals `ScoreSubmission.entryId`                     | **The only thing a row is matched on** (DR-199). Server-generated per **Entry Identity (DR-119)**.                     |
| runId          | UUID                     | required, immutable, equals `CompletedRun.runId`                          | Carried for diagnostics and for the DR-217 recency check. **Never** used to match a row (DR-199).                      |
| placement      | Placement?               | null until the board has been read; replaced whole on each read            | Where the entry stood in the board as it was last read (§5.3). Null is normal, not an error (DR-202).                  |
| isLive         | boolean                  | derived; true until an event in DR-207 occurs                             | False means the highlight has ended; a `RunHighlight` that is not live is discarded, not kept in a dormant state.      |

A `RunHighlight` has **no rank attribute of its own** — rank lives on `Placement`, because it is a property
of a board reading and not of the run (DR-209). It also has no name, score, lines or level: everything
displayable about the entry comes from the row story 010 already renders. Duplicating any of it here would
create a second source of truth for figures 009 made immutable.

#### What is deliberately not here

- **No stored highlight.** No attribute is added to `LeaderboardEntry` — no `isMine`, no `highlighted`, no
  `lastViewedBy`. The entry is immutable (009 DR-118) and shared by everyone; "mine" is a fact about one
  browser at one moment, not about the entry (DR-218).
- **No history.** No list of previously highlighted runs, no "your runs this session". At most one
  `RunHighlight` exists at a time and a new one replaces the old wholesale (DR-217).
- **No player or identity reference.** None exists in this product (009 DR-112).

### 5.2 Relationships

- A **ScoreSubmission** (009 §5.1) in status `Submitted` yields **exactly one RunHighlight**; a submission in
  any other status yields **none** (DR-203). The two share `entryId` and `runId` by value, not by reference.
- A **RunHighlight** points at **exactly one LeaderboardEntry**, by `entryId`, and the entry knows nothing
  about it. The relationship is one-way, read-only and non-exclusive: the same entry is an ordinary row for
  every other viewer of the board at the same moment.
- A **RunHighlight** may point at an entry that is **not present** in the board as read. That is a legal
  state, not an error (DR-202), and is what `placement` being null expresses.
- **At most one RunHighlight exists in a browser at a time** (DR-217). Starting a new game, reloading or
  navigating away destroys it; a later successful submission creates a new one.
- A **RunHighlight** has **no relationship to any other RunHighlight**, to a `GameSession`, or to any entry
  other than its own — including other entries bearing the same `playerName` (009 DR-112).
- A **Placement** belongs to exactly one `RunHighlight` and is derived entirely from **story 010's** ranked
  board at one instant. It is replaced, never updated (DR-209).

### 5.3 Value Objects

#### Placement

Where one entry stood in the board **as it was last read**. Immutable; a later read produces a new
`Placement`, it does not mutate this one. This object is the whole of the spec's answer to "where did I
land", and the whole of its honesty about that answer being a snapshot (DR-209).

| Attribute     | Type           | Constraints                                        | Description                                                                              |
| ------------- | -------------- | -------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| rank          | integer        | ≥ 1, ≤ `totalEntries`                              | 1-based position in story 010's ordering (DR-210). 1 is the best score on the board.           |
| totalEntries  | integer        | ≥ 1                                                | How many entries the board held at this reading. Gives the rank its meaning (DR-208).          |
| readAt        | datetime (UTC) | required, immutable                                | When the board was read. The instant the rank is true of (DR-209).                             |
| case          | HighlightPlacementCase | required, derived from `rank` and 010's default view size | Which of the three presentations in §5.4 applies.                                     |

`rank` is **derived, never stored** — 009 deliberately keeps no rank on `LeaderboardEntry` (009 §5.1,
DR-132), and this spec does not reintroduce one. Two `Placement`s for the same `entryId` with different
`rank` values are both correct if their `readAt` differ (DR-209).

#### PlacementContext

The small slice of the board shown below the default view so that an out-of-view row is reachable without
hunting. Present only when `Placement.case` is `OutsideDefaultView`; absent otherwise (DR-212).

| Attribute      | Type      | Constraints                                                                        | Description                                                                               |
| -------------- | --------- | ----------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| neighboursAbove| integer   | 0–2; fewer than 2 only when the board has fewer ranks above                         | Rows of higher rank shown above the player's row — the scores to beat.                          |
| neighboursBelow| integer   | 0–2; fewer than 2 only when the player's row is at or near the bottom                | Rows of lower rank shown below it.                                                              |
| showsBreak     | boolean   | derived; false when the slice is contiguous with the default view                   | Whether a labelled break separates the slice from the rows above it (DR-213, FR-011).           |

**Two above and two below** is a deliberate choice, not a tunable: one neighbour each side reads as an
accident of layout, five turns the context into a second leaderboard competing with the real one, and two
is enough to answer the only question the player actually has — *what do I have to beat next time?*

### 5.4 Enumerations

#### HighlightPlacementCase

Which of the three presentations applies to a highlight. Closed set, derived from `Placement.rank` and
story 010's default view, never chosen by a user.

| Member               | Meaning                                                                                                                                       |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| `WithinDefaultView`  | The row is already on screen. Highlight it in place; add nothing, move nothing (DR-211). The common case for a good run.                           |
| `OutsideDefaultView` | The row is further down than the default view reaches. Highlight it and append a `PlacementContext` behind a labelled break (DR-212). The common case for a casual player's first run — and the reason this story exists. |
| `Unavailable`        | No row could be matched: the board has not been read yet, the read failed, or it returned nothing with this `entryId`. No highlight, no placement statement, no error of this feature's own (DR-202, DR-204). |

`OutsideDefaultView` collapses to `WithinDefaultView` the moment the player expands the board far enough to
include the row — which is exactly what makes FR-010 a consequence of the model rather than a special case.

### 5.5 Domain Rules and Invariants

_New rules introduced by this spec, numbered **DR-199 – DR-219** inside this spec's reserved block
DR-199 – DR-228, so that IDs stay globally unique across concurrently written specs. DR-01 – DR-198 belong to
specs 001 – 010 and 013 and are cited, never restated._

**Which row is mine**

- **DR-199 — Highlight Identity**: The player's row is identified by **one value and one value only**: the
  `LeaderboardEntry.entryId` the server generated and returned on successful submission, per **Entry
  Identity (DR-119)**, carried to the client in `ScoreSubmission.entryId` (009 §5.1). A row is the player's
  run **if and only if** its `entryId` equals that value. It is **never** matched by `playerName` — a name
  is a label, not an identity, and two colleagues may share one (**009 DR-112**) — never by `score`, never
  by any combination of displayed values, never by position in the list, and never by "the most recent
  entry". `runId` may *corroborate* a match in a diagnostic or a test, but it is generated in the browser
  (008 DR-107), is no more trustworthy than the score beside it (009 §9.2), and **is never the authority**.
  This is the single most important correctness rule in this spec: every other rule here is cosmetic if the
  wrong row is marked, and a name-based or score-based implementation looks correct right up to the day two
  colleagues called Daan both score well.
- **DR-200 — Exactly One Highlighted Row**: In any view of the board, **at most one** row is marked as the
  player's run, and **exactly one** is marked whenever a live `RunHighlight` exists and its entry appears in
  what is displayed. Never two — not when a neighbouring row shows the same name, not when it shows the same
  score, not when the player has several entries on the board (009 DR-114). Never zero when the row is on
  screen. A view that shows the same entry twice (DR-213) still marks it once per appearance of that
  entry — and DR-213 exists precisely so that situation does not arise.
- **DR-201 — The Highlight Never Reorders**: Highlighting marks a row; it does not move one. The player's
  entry stays exactly where story 010's ordering puts it. It is never pinned to the top, promoted into the
  default view, sorted differently, duplicated into the top 10, or removed from its natural position. The
  rows above and below it are the rows that genuinely out-scored and under-scored it. *Why:* a board that
  quietly moves your row to the top stops being a leaderboard and starts being a mirror, and the one thing
  the player wants to know — who is above me — is destroyed by the very feature meant to answer it.
- **DR-202 — No Phantom Row**: The highlight marks a row the board already contains. It never creates,
  injects, appends or synthesises a row for the player's entry, and never reconstructs one from the client's
  own copy of the figures. If the board as read contains no entry with the matching `entryId`, the correct
  outcome is **no highlight and no placement statement** — not a fabricated row at the bottom, not a "you:
  12,400" footer. A rank that cannot be derived from the board is not a rank, and showing one would be the
  feature lying at exactly the moment it is least able to tell.

**When there is a highlight at all**

- **DR-203 — Highlight Provenance**: A highlight exists only when a `ScoreSubmission` has reached
  `SubmissionStatus.Submitted` and carries a non-null `entryId` (009 §5.1, DR-119). Those two values are the
  *entire* input to this feature. In every other status — `Unsubmitted`, `Submitting`, `Failed` (009
  DR-127) and `Declined` (009 DR-116, DR-117) — there is **no entry**, and therefore nothing to highlight:
  the board renders completely normally, with no mark, no placement statement, no greyed-out placeholder and
  no message about the run that was not submitted. A player who chose not to submit chose not to be on the
  board, and the board must not nag them about it.
- **DR-204 — The Absence of a Highlight Is Not an Error**: Every "no highlight" case — not submitted,
  submission failed, highlight expired, entry not found in the board, board not yet read — is a **normal,
  silent outcome**. This feature raises no error, shows no warning, logs nothing the player can see and
  degrades to exactly the board story 010 would have drawn on its own. Any error that genuinely belongs to
  reading the board is **story 010's** to report, once, in its own words; 011 never adds a second message
  about the same failure.

**How long it lasts**

- **DR-205 — No Shareable, Bookmarkable or Transferable Highlight**: The highlighted `entryId` is held as
  **transient client-side state only**. It is never placed in the page address, a query parameter, a
  bookmarkable fragment, a cookie, or anything else that can be copied, shared, bookmarked or reopened.
  *Why:* "your run" is a statement about the person sitting at this keyboard right now. A URL carrying it is
  wrong the instant it is pasted into a team chat, where it would tell twenty colleagues that one of them
  owns a stranger's score. This rule is the reason the obvious implementation — a `?highlight=<entryId>`
  parameter — is **ruled out**, and it is what makes DR-207's reload behaviour a consequence rather than an
  accident. It also keeps 009's privacy position intact: nothing identifying is written anywhere (009
  DR-131, DR-218).
- **DR-206 — Highlight Lifetime**: The highlight **begins** at the moment the submission is confirmed —
  `SubmissionStatus.Submitted` with an `entryId` (DR-203) — and is applied to the board from the first time
  the player sees it afterwards. While live it **survives**: scrolling the page, expanding the board, paging
  through it, collapsing it again, re-reading the board, a changed rank (DR-209), and any number of views of
  the board within the same visit. None of these is an ending condition, and none may reset, duplicate or
  silently drop it. The placement statement lives exactly as long as the highlight does.
- **DR-207 — Highlight Expiry**: The highlight **ends**, immediately and irreversibly, on any of: the player
  **starting a new game**; a **page reload**; **navigating away** from the application; or **closing the
  tab**. It is not restored afterwards by any means. Starting a new game ends it because the highlight is a
  statement about the run the player just finished, and they no longer have; the other three end it because
  the state is transient by construction (DR-205) and there is no session to resume (story map: no accounts,
  no sessions). **On reload, specifically: the highlight is *not* remembered.** The board renders normally
  and the entry is still there, correctly ranked, as an ordinary row. The cost is real but small — the
  player was told their placement at the moment they asked — and every way of avoiding it is worse: a URL
  parameter is shareable (DR-205), browser storage outlives the colleague who created it and would greet the
  next person at a shared office laptop with somebody else's run marked as theirs (SC-014), and a server-side
  memory of "who" requires an identity this product deliberately does not have (009 DR-112).

**Where did I land**

- **DR-208 — Placement Statement**: On every view of the board with a live highlight, the player is told
  their standing in **plain language**, giving both the **rank** and the **total number of entries** — for
  example "You placed 47th of 183". Both halves are required: a rank without a total is unreadable (is 47th
  good?), and a total without a rank does not answer the question. The statement is **text**, not a tooltip,
  not a transient toast and not an image, so it survives being read slowly, zoomed, copied or announced
  (DR-216). It is shown for every placement, including 1st and including last — there is no threshold below
  which the player is spared the number, consistent with zero-score runs being first-class (009 DR-130).
- **DR-209 — Rank Is a Reading, Not a Fact**: Rank is derived from the board **as it was read at
  `Placement.readAt`**, and is true of that instant only. The leaderboard is shared and all-time (009
  DR-122), so a colleague may submit a better score between this player's submission and their next look at
  the board, moving them from 12th to 13th. That is **correct behaviour, not an inconsistency**: the
  placement statement describes where they stood, and a later read legitimately shows a different number
  with no error, no explanation and no apology. The rank is therefore recomputed from each reading and never
  cached as a position the player "holds", and the product never promises the number will stay (§1.3: no
  overtaking notifications).
- **DR-210 — Ranking and Presentation Belong to Story 010**: This spec defines **no ordering, no
  tie-breaking, no paging, no row layout and no column**. The board is ranked by `score` descending with
  ties broken by `submittedAt` ascending, the default view is the top 10, and expanding it is 010's
  affordance — all of which are **story 010's** decisions, cited here and never re-specified (009 DR-132).
  011's rank is simply the position the entry occupies in 010's order. If 010 changes its default view size
  or its tie-break, this spec follows without amendment; the only things that change are the illustrative
  numbers in §4.

**Reaching the row**

- **DR-211 — Inside the Default View, Highlight in Place**: When the player's row already appears in the
  board's default view (`HighlightPlacementCase.WithinDefaultView`), it is marked where it sits and
  **nothing else about the view changes**: no scroll, no jump, no expansion, no appended context, no extra
  rows, no change to which entries are shown or their order. The row was already findable; the mark is the
  entire feature in this case.
- **DR-212 — Outside the Default View, Show It in Context**: When the player's row falls below the default
  view (`OutsideDefaultView`), the board **additionally** shows that row together with its **two
  nearest-ranked neighbours above and below**, appended below the default view behind a labelled break, with
  the player's row marked. The default view itself is untouched — the top 10 remains the top 10 (DR-201).
  *Why this and not an automatic scroll:* appending a small context window answers the question without
  moving the board under the player (DR-214), works whether or not the board is scrollable, needs no
  viewport arithmetic, leaves 010's default view intact for the colleague reading over their shoulder, and —
  unlike a bare rank number — shows the scores immediately above, which is the next thing the player wants
  to know. An automatic scroll or auto-expansion would instead override 010's considered default, hijack the
  player's position on the page, and fight the paging affordance that 010 already provides for exactly this
  purpose.
- **DR-213 — The Context Composes With Paging, It Does Not Compete With It**: The appended context is a
  convenience on top of story 010's board, never a second board. Three consequences, all required:
  1. **Absorbed on expansion.** As soon as the player expands or pages the board to a view that already
     contains their row, the appended context is **dropped** and the row is marked in place in the main
     list. The entry is never shown twice in one view (DR-200).
  2. **No break when contiguous.** If the context begins immediately after the last row of the default
     view — the player ranked 11th or 12th, say — the rows simply continue and **no break is drawn**. A
     separator implying omitted rows where none are omitted is a lie about the board.
  3. **Truncated at the edges.** A neighbour that does not exist is not shown and not replaced by a
     placeholder: a player ranked last has no rows below them, and one ranked just outside the default view
     has fewer than two above them that are not already displayed.
- **DR-214 — No Viewport Hijack**: This feature never moves the page on the player's behalf. No auto-scroll,
  no scroll-into-view, no focus shift, no animated travel of the row into position, no counting the rank up,
  no flashing. The player's scroll position and keyboard focus after the board appears are exactly what they
  were before. *Why:* moving content under someone who is reading is disorienting for everyone and actively
  hostile to a screen-magnifier or screen-reader user, and this feature's whole job can be done without it
  (DR-212).

**Being perceivable**

- **DR-215 — Never Colour Alone**: The highlight is **never conveyed by colour alone**. The marked row
  carries a **textual marker** — "your run" or equivalent — rendered as real text on the row itself, in
  addition to whatever visual treatment **story 013** specifies. The feature must remain fully usable in
  greyscale, with colours overridden by the operating system, and by a player who cannot distinguish the
  highlight tint from the row background. The actual colours, weights and brand tokens are **story 013's**
  and are not restated here; 004 makes the same point about the preview and the two must not diverge.
- **DR-216 — The Placement Is Announced**: A screen-reader user is told where they landed and which row is
  theirs, without having to read the table to find out. The placement statement (DR-208) is text in the
  document, not a decorative overlay; the marked row's status is exposed as part of the row rather than as a
  purely visual treatment; and the labelled break above an appended context (DR-212) is **described in
  words** — "your placement" or equivalent — rather than rendered as an ellipsis or a rule that assistive
  technology skips as decoration. The placement statement appearing is the kind of change that should be
  announced when it happens rather than discovered later.

**One highlight, no footprint, one deferred decision**

- **DR-217 — Only the Just-Finished Run**: At most one `RunHighlight` exists in a browser at a time, and it
  is always the **most recent** successful submission. A run submitted earlier in the same visit is not
  highlighted, even though its entry is on the board and was submitted from this very keyboard — starting a
  new game ended that highlight (DR-207), and a new submission replaces rather than accumulates. There is no
  "my entries", no multi-row highlight and no history. *Why:* the story is "my **just-finished** run"; the
  moment the feature marks two rows it has started claiming an identity across runs, which this product does
  not have (009 DR-112).
- **DR-218 — The Highlight Writes Nothing**: Highlighting is a **read-only** act. It adds no attribute to
  `LeaderboardEntry` (009 DR-118 — entries are immutable), creates no server-side state, writes no cookie,
  stores no identifier, and sends nothing anywhere. Nothing about who viewed what is recorded. The only
  personal datum this product collects remains the name the player typed at game over (009 DR-131), and this
  spec does not add to it. Any proposal to "remember" a highlight — in storage, in a URL or on the server —
  is a change to this rule, to DR-205 and to DR-207 together, not an implementation detail.
- **DR-219 — Highlight Handover**: The `entryId` travels from the game-over screen (where 009 receives it)
  to the board view (where 011 uses it) as transient client-side state. *How* it travels — the board being
  part of the same view, a client-side transition, or a one-shot handover consumed on arrival — is an
  **architecture decision** deferred to arc42 §09 (§9.3), exactly as 009 deferred its storage question.
  Whichever is chosen is constrained by two rules and no others: it must not leave the identifier anywhere
  shareable or reopenable (DR-205), and it must not survive a reload (DR-207). If the chosen architecture
  makes the board a separate page reached by a full navigation, satisfying both is a design requirement of
  that decision, not a licence to relax them.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty).
Story 010 owns the board's own read performance; the figures below are 011's **additional** cost._

| ID      | Category         | Requirement                                                                                                                                                                                             |
| ------- | ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance      | Matching the row and deriving the `Placement` adds **no additional server round trip** beyond the board read story 010 already performs, and **< 50 ms at p95** to rendering the board.                        |
| NFR-002 | Performance      | The highlight and the placement statement appear in the **same render** as the board — never as a second pass that reflows the page, shifts the rows or flashes the mark in a moment later (DR-214).          |
| NFR-003 | Correctness      | Rank and total remain correct for a board of at least **10,000 entries**, including ordinal formatting for every rank in that range (11th, 12th, 13th, 21st, 101st, 111th, 1,002nd).                           |
| NFR-004 | Accessibility    | The marked row is identifiable with **colour removed entirely** from the rendering, and the textual marker meets a contrast ratio of **≥ 4.5:1** against the highlighted row background (tokens: story 013).   |
| NFR-005 | Accessibility    | The placement statement is exposed to assistive technology as text and is announced when it appears; a screen-reader user can determine their rank without traversing the table (DR-216).                      |
| NFR-006 | Security/Privacy | This feature transmits and persists **zero bytes**. It adds no field to any request, no attribute to any stored record, no cookie and no client-side storage entry (DR-205, DR-218, 009 DR-131).               |
| NFR-007 | Reliability      | Any failure to match a row or derive a placement degrades to **no highlight and an intact board** — never a broken, blank, duplicated or misordered board (DR-204).                                            |
| NFR-008 | Testability      | The highlight's inputs (`entryId`, the board reading and its `readAt`) are injectable, so all §4 scenarios — including the moving-target and tie cases — run headlessly with no real submission and no clock.   |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                                   | Expected Behavior                                                                                                                                                      |
| ----- | ---------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | The board as read contains no entry with the highlighted `entryId` (e.g. a row was removed by hand, §9.2).   | `HighlightPlacementCase.Unavailable`: no mark, no placement statement, no fabricated row, no error (DR-202, DR-204). The board renders normally.                         |
| EC-2  | Reading the board fails outright.                                                                            | Story 010 reports it, once, in its own words. 011 adds nothing and shows no second message (DR-204).                                                                     |
| EC-3  | The player ranks 11th — immediately below the default view.                                                  | The context follows the top 10 directly with **no break** (DR-213.2). Rows 11–13 are shown; only one row above is available that is not already displayed.               |
| EC-4  | The player ranks last on the board.                                                                          | Two neighbours above, none below, no placeholder rows (DR-213.3). "You placed 183rd of 183."                                                                             |
| EC-5  | The player's is the only entry on the board.                                                                 | "You placed 1st of 1." The row is in the default view, marked in place, no context appended (DR-211).                                                                    |
| EC-6  | Another colleague submits a better score between this player's submission and their view of the board.       | The board is read once; the rank already reflects the newcomer. Nothing is reconciled, corrected or re-announced — the rank is a reading (DR-209).                        |
| EC-7  | The player refreshes the board a minute later and the rank has changed.                                      | The new rank is shown as the current standing. Not an error, not flagged as a change, not animated (DR-209, DR-214, SC-015).                                             |
| EC-8  | The player submits, views the board, plays again, and tops out without submitting.                           | The highlight ended when the new game started (DR-207) and the new run has no entry (DR-203). The board shows no mark at all — not the earlier run's row (DR-217).        |
| EC-9  | The player opens the leaderboard in a second browser tab.                                                    | No highlight there. The state is transient and belongs to the view that made the submission (DR-205, DR-219). The entry itself is on the board in both tabs.             |
| EC-10 | The submission succeeded through 009's idempotent return after a lost response (009 DR-115, SC-014).         | Indistinguishable here: `entryId` is the existing entry's and the highlight behaves identically. If the name was edited before the retry, the row shows the **stored** name (009 DR-126), which is what is on the board. |
| EC-11 | Two adjacent rows carry the same `playerName` **and** the same `score`, one of them the player's.            | Exactly one row is marked — the one whose `entryId` matches (DR-199, DR-200). This is the case that exposes any name- or score-based implementation (SC-007, SC-008).    |
| EC-12 | The player expands the board, then collapses it back to the default view.                                    | The appended context reappears and the highlight is intact. Neither is lost, reset or duplicated by the round trip (DR-206, DR-213.1).                                   |
| EC-13 | The board read returns the same entry twice (a board-side defect).                                           | At most one row is marked — the first occurrence (DR-200). 011 does not mark both and does not attempt to repair the board.                                              |
| EC-14 | The run scored 0 and sits at the bottom of the board.                                                        | Highlighted and stated on exactly the same terms as any other run: "You placed 183rd of 183" (009 DR-130). No suppression, no softening, no "better luck next time".      |
| EC-15 | The player scrolls far past their marked row while reading the board.                                        | The mark stays on the row and the placement statement stays in the document and reachable. Nothing floats, follows the scroll or re-announces itself (DR-214).           |
| EC-16 | The default view size changes because story 010 revises it (e.g. to 20 rows).                                | 011 follows with no amendment: `HighlightPlacementCase` is derived from 010's view, not from a constant of this spec (DR-210).                                           |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                      |
| ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 20 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                   |
| SUC-002 | All 21 domain rules in §5.5 are each covered by at least one automated test, with DR-199 covered by the same-name, same-score and tie cases specifically.           |
| SUC-003 | A player whose run ranks outside the top 10 can state their rank **without scrolling, clicking or searching** — the number is on screen when the board appears.     |
| SUC-004 | In a board seeded with ten entries sharing one name and ten sharing one score, every highlight test marks the correct single row; no test passes by coincidence.    |
| SUC-005 | Removing all colour from the rendering leaves every §4 scenario still passing, and a screen-reader walkthrough yields the rank without the table being read.        |
| SUC-006 | Grepping the implementation finds **no** comparison of `playerName` or `score` used to select the highlighted row, and no `entryId` in any URL, cookie or storage.  |
| SUC-007 | A reload, a new game and a second colleague opening the page each leave the board with no highlight, verified as three separate tests (SC-012, SC-013, SC-014).     |
| SUC-008 | Story 010's board renders byte-for-byte identically with the highlight absent, confirming 011 adds a mark and a sentence and changes nothing else (DR-201, DR-210). |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Story 009 (`009-submit-score.md`)** — **hard dependency.** 011 cannot exist without **Entry Identity
  (DR-119)**: a server-generated `entryId` returned on every successful submission and handed to the client
  in `ScoreSubmission.entryId`. If 009 did not return it, this feature would have nothing to match on but a
  name, and 009 DR-112 forbids that. 011 also depends on `SubmissionStatus` to know whether a highlight
  exists at all (DR-203).
- **Story 010 (view the leaderboard)** — **hard dependency.** 010 owns the ranked board, the ordering
  (`score` descending, `submittedAt` ascending), the default top-10 view, the paging affordance and every
  row of the table. 011 marks one of its rows and derives a rank from its order (DR-210). **At the time of
  writing 010's spec does not yet exist**; this spec is written against the three facts recorded in §1 and
  requires no amendment if 010's numbers differ.
- **Story 013 (Info Support colours and typography)** — owns the visual tokens for the highlight. 011 fixes
  the non-visual contract (textual marker, never colour alone, DR-215) and defers the appearance entirely.
  013's accessibility position and 004's "never colour alone" point must not diverge from DR-215.
- **Story 008 (`008-game-over.md`)** — the game-over state that hosts the submission and the transition to
  the board. **Run Availability Window (DR-106)** bounds the `ScoreSubmission` this feature reads from, and
  starting a new game (008 FR-011) is one of DR-207's expiry events.
- **No downstream dependents.** 011 closes the Compete activity. Nothing in specs 012 – 014 reads anything
  defined here.
- **`docs/product/story-map.md`** — the source of the story numbering and of the project-wide exclusions
  (no accounts, no sessions, no weekly resets, no mobile) that DR-205 and DR-207 lean on directly.

### 9.2 Constraints

- **No accounts, no sessions, no server-side identity** (story map). This is the constraint that shapes the
  entire spec: "mine" can only ever be a fact about one browser at one moment, which is why the highlight is
  transient (DR-205), dies on reload (DR-207) and can never become a "my scores" page (§1.3).
- **Entries are immutable and shared** (009 DR-118, DR-122). Nothing about a viewer can be stored on an
  entry, and the same row is an ordinary row for every other colleague looking at the board right now.
- **The board is a moving target** (009 DR-122). It is shared and all-time, so rank is inherently unstable
  and the spec says so rather than pretending otherwise (DR-209).
- **A row may be removed by a human** — 009 §9.2 records deletion-by-hand as the only moderation mitigation.
  011 must survive its entry vanishing from the board between submission and view (EC-1).
- **Platform**: desktop browser on a laptop with a keyboard. No mobile or touch support (story map).
- **Technology context** (from `CLAUDE.md`, stated for the implementer, not prescribed by this spec):
  ASP.NET Core 10 Razor Pages, root namespace `infosupport_tetris`, static client assets under `wwwroot/`.
  Note that a Razor Pages default of full-page navigation interacts directly with DR-219 — reaching the
  board by a page load must still satisfy DR-205 and DR-207.
- **No test project exists yet**; SUC-001 and SUC-002 require one.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below state what *this feature needs
> those sections to say*, per `CLAUDE.md` ("fill them in as architectural decisions for the Tetris
> implementation are made").

| Arc42 Section                                               | Relevance to This Feature                                                                                                                                                      |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 03. System Scope and Context (`03-system-scope-and-context.md`) | 011 adds **no** external interface and **no** data crossing the boundary — it is the only Compete story that does not. Should record that the highlight is client-local. *Currently empty.* |
| 05. Building Block View (`05-building-block-view.md`)       | 011 adds presentation-layer behaviour to story 010's leaderboard building block and introduces no domain or persistence component. *Currently empty.*                              |
| 06. Runtime View (`06-runtime-view.md`)                     | Needs the runtime scenario **submit → entryId returned → board read → match, rank, render**, including the handover across whatever view transition the architecture chooses (DR-219). *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`) | Needs the project-wide accessibility position — never colour alone, text alternatives, no viewport hijack — which 004, 011 (DR-215, DR-216) and 013 all depend on and none should own alone. *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`) | 011 warrants one ADR: **how the `entryId` reaches the board view** (DR-219), constrained by DR-205 and DR-207. The "put it in the query string" option must be recorded as considered and **rejected**, with the reason, so it is not reintroduced later. *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)     | Should host the project-wide accessibility and responsiveness scenarios that §6 only adds to. *Currently empty.*                                                                   |
| 12. Glossary (`12-glossary.md`)                             | Should gain: highlight, placement, rank, default view, placement context, and the distinction between `entryId` (identity) and `runId` (idempotency key). *Currently empty.*        |

## 10. Open Questions

| #   | Question                                                                                                              | Owner      | Status   | Resolution                                                                                                                                                                                                                                                                       |
| --- | --------------------------------------------------------------------------------------------------------------------- | ---------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | For a run outside the top 10, should the board **auto-scroll/auto-expand** to the row, or **append it in context**?      | Daan Harms | Resolved | **Append it in context** (DR-212): the row plus two neighbours either side, below the default view behind a labelled break. Auto-scrolling overrides 010's considered default, hijacks the player's position on the page (DR-214), needs viewport arithmetic, and fights 010's paging affordance instead of composing with it. Appending also shows the scores immediately above — the thing the player actually wants to know. |
| 2   | Should the highlight be remembered across a **page reload**?                                                            | Daan Harms | Resolved | **No** (DR-207). Every mechanism that would remember it is worse than losing it: a URL parameter is shareable and would label a stranger's row as someone's run (DR-205); browser storage outlives the colleague who created it and would greet the next person at a shared laptop with a false highlight (SC-014); a server-side memory needs an identity this product does not have (009 DR-112). The player is told their placement when they ask, which is when it matters. |
| 3   | Should the `entryId` travel in the page address so the board can be linked to?                                          | Daan Harms | Resolved | **No** (DR-205). "Your run" is a claim about the person at the keyboard, and a copyable link makes it false for everyone it is sent to. Explicitly recorded as a **rejected** option in the §9.3 ADR so it is not reintroduced as a convenience later.                            |
| 4   | How many neighbouring rows should the appended context show?                                                            | Daan Harms | Resolved | **Two above and two below** (DR-212). One reads as a layout accident; five becomes a second leaderboard competing with the real one. Two answers "what do I have to beat next time?" and stays visibly subordinate to the top 10.                                                  |
| 5   | Should the player's row be pinned into the top 10 so it is always on screen?                                            | Daan Harms | Resolved | **No** (DR-201). A board that moves your row to the top is a mirror, not a leaderboard, and it destroys the information — who is immediately above you — that the feature exists to deliver. The row stays where the ranking puts it; the context window makes it reachable.        |
| 6   | Should the rank update live while the player watches, as colleagues submit?                                             | Daan Harms | Resolved | **No** (DR-209, §1.3). No polling, no push, no "you've been overtaken". The rank is the standing at `readAt` and a refresh is the way to get a newer one. Live updates would mean a number changing under a player who is reading it, for a board nobody is watching in real time.  |
| 7   | Should the board tell a player who skipped submission that their run **could** have been on it?                         | Daan Harms | Resolved | **No** (DR-203, DR-204). Declining is a first-class choice (009 DR-116, DR-117) and the run is already discarded. A "you could have been 12th" message would nag the player about a decision they made deliberately, and would require keeping a run 009 says is gone.             |
| 8   | Does 011 need a stored `rank` on `LeaderboardEntry` to avoid deriving it on every read?                                 | Daan Harms | Resolved | **No** (DR-209, DR-218, 009 §5.1). Rank is a property of the whole board at an instant, not of an entry; storing it would be wrong the moment the next entry arrives and would violate 009's immutability (DR-118). 009 deliberately left `rank` off the entity and this spec does not reintroduce it. |
| 9   | Is the textual marker on the row enough, or should the highlight also be announced as it appears?                       | Daan Harms | Resolved | **Both** (DR-215, DR-216). The marker makes the row self-describing for anyone reading the table; announcing the placement statement when it appears means a screen-reader user gets the answer without traversing the table at all. Neither alone satisfies US-005.                |

_No `[NEEDS CLARIFICATION]` markers remain in this spec._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001, SC-006, SC-016, SC-017; US-002 -> SC-002..SC-005;
        US-003 -> SC-007, SC-008, SC-019; US-004 -> SC-015; US-005 -> SC-018;
        US-006 -> SC-009..SC-014, SC-020
  - [x] Each functional requirement traces to a user story
        (FR-001, FR-004, FR-005, FR-008, FR-012, FR-013 -> US-001;
         FR-006, FR-007, FR-009, FR-010, FR-011 -> US-002;
         FR-002, FR-003, FR-024 -> US-003; FR-021, FR-022 -> US-004;
         FR-019, FR-020 -> US-005; FR-014..FR-018, FR-023 -> US-006)
  - [x] Domain model covers all entities mentioned in the requirements
        RunHighlight (the only new one); value objects Placement, PlacementContext;
        enum HighlightPlacementCase. LeaderboardEntry, ScoreSubmission, PlayerName,
        SubmissionStatus are story 009's and are reused unchanged, not restated.
  - [x] Domain rules and invariants are listed (DR-199 .. DR-219, inside this spec's
        reserved block DR-199 .. DR-228)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-16)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-008)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain)
  - [x] Open questions are assigned and have a resolution path (9 questions, all Resolved)
-->
