# Feature Specification: The Landing Page — What This Is, and One Key to Start

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
| Feature ID      | 012                                                                                                 |
| Status          | Draft                                                                                               |
| Author          | Daan Harms                                                                                          |
| Created         | 2026-10-20                                                                                          |
| Last updated    | 2026-10-20                                                                                          |
| Epic / Parent   | Look like Info Support (story map activity) — `docs/product/story-map.md`                           |
| Arc42 reference | Sections 1, 2, 3, 5, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders.      |

> **Note on this spec.** 012 is the **front door**. It is the only screen in the product that someone who
> has never heard of it will see first, and it is the screen that replaces `Pages/Index.cshtml` — still the
> unmodified `dotnet new webapp` welcome text today.
>
> **Note the actor.** The story says *visitor*, not *player*. A visitor may have been sent a link, may never
> play, and owes the page nothing. The page has about three seconds to answer *what is this, who made it,
> and what do I do* — and then to get out of the way (DR-232). Every decision below is made for that visitor
> first and for the colleague who plays every day second.
>
> **This spec owns no colours and no type sizes.** [`013-brand-styling.md`](013-brand-styling.md) is the
> foundational slice of the *Look like Info Support* activity and owns the design token system. 012 consumes
> those tokens **by name** and introduces no literal value of any kind, per **No Literal Values (DR-142)**
> and **Token Name Stability (DR-141)**. Where 013 labels a `--brand-*` value PLACEHOLDER pending Info
> Support's brand guidelines, 012 builds against the **token name**, which is stable — 013's Open Question 1
> is 013's, and is not restated here.
>
> **Vocabulary.** `GameSession`, `GameStatus` and the start action come from
> [`001 §5`](001-start-new-game.md#5-domain-model); `LeaderboardEntry` and the board from
> [`009 §5`](009-submit-score.md#5-domain-model) and [`010 §5`](010-view-leaderboard.md#5-domain-model).
> 012 defines none of them and reads none of them. What it defines is a **page composition**: regions,
> actions, a message order and the copy that carries it (§5).
>
> **Rule numbering.** New rules introduced here run from **DR-229 to DR-258**, so that IDs stay globally
> unique across concurrently written specs. DR-01 – DR-138 belong to specs 001 – 009, DR-139 – DR-168 to
> story 013, and DR-169 – DR-228 to stories 010 and 011; all are cited here, never restated.

### 1.1 Problem Statement

Everything else in the product is built and nothing points at it. A colleague who follows a link in a chat
message arrives at the ASP.NET Core scaffold's welcome text — "Learn about building Web apps with
ASP.NET Core" — which tells them nothing about Tetris, nothing about Info Support, and offers them no way
to start playing or to look at the board their colleagues are on. The game, the leaderboard and the brand
all exist behind a door with no sign on it.

### 1.2 Goal

A visitor opens the application and, within about three seconds and without scrolling, knows that this is
Tetris, that it is an Info Support thing, exactly which single action starts a game, and that there is an
all-time leaderboard they can look at. One keystroke starts a real game — no sign-up, no name, no options.
A colleague who has played forty times gets to the same keystroke by exactly the same route, because the
page never explains itself at anyone twice.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec. Each is either a separate story, a project-wide
exclusion, or an architecture decision. Do not implement them here, and do not design around a guess at how
they will work — the owning document decides.

- **A marketing site.** No second page, no navigation bar, no about, contact, team, privacy, terms, imprint
  or careers page, no footer link farm. The product is one game, one board and one front door (DR-246).
- **Any sign-up, login, account, profile or "continue as guest" choice** — out of scope project-wide (story
  map). 001's **FR-001** promises one action and no details, and this page is where that promise is either
  kept or broken (DR-229).
- **Asking for the player's name here.** The name is asked for exactly once, at game over, by **story 009**.
  A name box on the landing page would be a second place to type a name and would break the one-action
  promise outright (DR-231).
- **Difficulty, starting level, speed, board size or any other option.** Level starts at 1 and climbs by
  **story 007**'s curve; there is nothing to choose (DR-230).
- **How the leaderboard renders** — **story 010**, which owns the ranking, the ranked window, the table,
  the empty state and the failure states. 012 provides one entry point to it (FR-011, 010's **FR-014**) and
  specifies nothing about what is on the other side.
- **The controls / how-to-play panel itself** — **story 014**. 012 provides an entry point to it and does
  not design it, does not decide whether it is a panel, a page or an overlay, and does not list a single key
  (DR-244).
- **The game screen** — **story 001** and onwards. 012 ends at the moment the game begins.
- **Colours, typefaces, type sizes, spacing, radii, motion durations and the font-loading strategy** —
  **story 013**, consumed by name (DR-241).
- **Screenshots, a trailer, a demo reel, an animated board preview or a playable background.** The hero is
  type and colour only (DR-258).
- **A cookie banner or consent dialog.** Nothing is tracked, so there is nothing to consent to. 009's
  **Minimal Personal Data (DR-131)** makes the typed name the only personal datum the product collects, and
  010's **Reading Never Writes (DR-195)** adds nothing; this page adds nothing either (DR-253).
- **Analytics, a visit counter, a "players today" figure or any other telemetry** (DR-253).
- **Localisation and translation.** The page is in English, as is every other screen and the entire domain
  vocabulary of 001–013 (DR-237). No language picker, no resource files, no `Accept-Language` negotiation.
- **Mobile, touch and small-screen layouts** — out of scope project-wide (laptop + keyboard only). The page
  honours 013's **Minimum Viewport (DR-166)** of 1024 × 640 CSS pixels and is not designed below it.
- **Ghost piece, pause** — out of scope project-wide (story map).
- **Deciding how the browser gets from this page to the game** — the *requirement* is that the game has its
  own address and that this one always shows the landing page (DR-245); whether that is a navigation, a
  server-rendered page or a client transition is an **architecture decision** deferred to an ADR in arc42
  §09. Nothing in §3 – §5 changes whichever way it goes.

## 2. User Stories

### US-001: Understand what this is, fast

**As a** visitor who followed a link and knows nothing about this,
**I want** the page to tell me in one glance that it is Tetris, that Info Support made it, and what to do
next,
**so that** I can decide whether to play in about three seconds instead of reading a page.

### US-002: Start with one action and no decisions

**As a** colleague with five minutes before a meeting,
**I want** one obvious action that drops me straight into a real game,
**so that** I spend my break playing rather than choosing a difficulty and typing my name.

### US-003: Start without touching the mouse

**As a** player on a laptop whose hands are already on the keyboard,
**I want** to start the game with a keystroke, and to reach everything else by tabbing,
**so that** the first thing the game asks of me is not to find the trackpad.

### US-004: Look at the board without playing

**As a** colleague who was told over lunch that someone scored 40,000,
**I want** to get to the leaderboard straight from the page I land on,
**so that** I can check the claim without having to play a game first.

### US-005: Come back tomorrow and start just as fast

**As a** colleague who has played this forty times,
**I want** the page never to put an explainer, a splash or a "new here?" prompt between me and the game,
**so that** my fortieth start costs exactly what my first one did.

### US-006: Use the page with assistive technology

**As a** colleague using a screen reader and the keyboard,
**I want** a page with a meaningful title, one heading, named regions, honestly-labelled controls and a
focus indicator I can see,
**so that** I can start a game and find the board on the same terms as everyone else.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) are defined in §5.6 and
occupy the block **DR-229 – DR-258**. Rules DR-01 – DR-228 belong to specs 001–011 and 013 and are cited,
never redefined._

| ID     | Requirement                                                                                                                                                                                                                 | Priority | User Story |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall present, as the application's root screen, a landing page that replaces the ASP.NET Core scaffold's default welcome content entirely.                                                                        | Must     | US-001     |
| FR-002 | The system shall communicate four things, in this order of prominence: (1) this is Tetris, (2) it is an Info Support product, (3) this single action starts a game, (4) there is an all-time leaderboard and this reaches it (DR-232, DR-233). | Must     | US-001     |
| FR-003 | The system shall display the Info Support mark as supplied, in the hero region, with the clear space reserved by 013 §9.2, carrying the text alternative `Info Support` (DR-238, DR-239).                                      | Must     | US-001     |
| FR-004 | The system shall render the hero region on `--brand-deep` with heading text in `--text-primary` at the display role, and the primary action on `--brand-primary` with `--text-on-brand`, per 013 §5.6 and **Text-on-Brand Pairing (DR-149)** (DR-240). | Must     | US-001     |
| FR-005 | The system shall introduce no colour, type size, spacing, radius, border width or duration value of its own, drawing every one from 013's semantic tokens (DR-241; 013 **DR-141**, **DR-142**).                               | Must     | US-001     |
| FR-006 | The system shall fit everything required by FR-002 — mark, headline, lead line, primary action and leaderboard entry point — within a **1024 × 640 CSS pixel** viewport with no scrolling of any kind (DR-235; 013 **DR-166**). | Must     | US-001     |
| FR-007 | The system shall present exactly one primary action that **starts a game immediately**, requesting no name, no level, no difficulty, no mode and no other detail (DR-229, DR-230; 001 **FR-001**).                            | Must     | US-002     |
| FR-008 | The system shall ask for the player's name at no point on this page; name entry happens once, at game over, per **story 009** (DR-231).                                                                                        | Must     | US-002     |
| FR-009 | The system shall, on activation of the primary action, place the visitor in a `GameSession` in status `Playing` as specified by 001, at the game's own address, leaving the landing page address always presenting the landing page (DR-245). | Must     | US-002     |
| FR-010 | The system shall give the primary action keyboard focus when the page loads, without scrolling the page or obscuring any part of the hero, so that `Enter` starts a game with no prior navigation (DR-247).                    | Must     | US-003     |
| FR-011 | The system shall place exactly three controls in the focus order — the primary action, the leaderboard entry point, the how-to-play entry point — in that order, matching their visual and reading order (DR-242, DR-248, DR-234). | Must     | US-003     |
| FR-012 | The system shall bind no global "press any key" handler; the keyboard affordance is focus on a real control plus a visible key hint (DR-249).                                                                                  | Must     | US-003     |
| FR-013 | The system shall not deliver the keystroke that activated the primary action into the new session as a game input command (DR-250).                                                                                            | Must     | US-003     |
| FR-014 | The system shall show a visible focus indicator at every focus stop, using `--focus-ring` at `--focus-ring-width` and `--focus-ring-offset` per 013's **Focus Visibility (DR-150)** (DR-251).                                  | Must     | US-003     |
| FR-015 | The system shall offer an entry point to the all-time leaderboard with **no precondition** — no finished run, no submitted score, no prior play — resolving to 010's stable shareable address (DR-243; 010 **DR-193**, **DR-194**, **FR-014**). | Must     | US-004     |
| FR-016 | The system shall subordinate the leaderboard entry point to the primary action visually, while keeping it visible without scrolling at the minimum viewport (DR-233, DR-235).                                                  | Must     | US-004     |
| FR-017 | The system shall offer an entry point to story 014's controls / how-to-play material, ranked below the leaderboard entry point and never interposed before play (DR-244).                                                      | Should   | US-004     |
| FR-018 | The system shall present no splash screen, modal, tour, carousel, onboarding step, consent dialog or "new here?" interstitial, at any visit (DR-252).                                                                           | Must     | US-005     |
| FR-019 | The system shall store nothing about the visitor and read nothing back — no cookie, no local storage, no server-side session, no counter, no analytics event — so that every visit is identical and there is nothing to remember (DR-253; 009 **DR-131**, 010 **DR-195**). | Must     | US-005     |
| FR-020 | The system shall keep the explanatory copy to a single lead line, so that it is read once by a newcomer and skipped by everyone else without being dismissed (DR-232, DR-236).                                                 | Must     | US-005     |
| FR-021 | The system shall give the page a document title that identifies the product out of context, and exactly one `h1` (DR-254).                                                                                                     | Must     | US-006     |
| FR-022 | The system shall structure the page as landmark regions with the hero as the main region, so that a screen reader user can reach the primary action directly (DR-255).                                                          | Must     | US-006     |
| FR-023 | The system shall give every control a native, keyboard-operable role and an accessible name that is meaningful read on its own, with the key hint excluded from that name (DR-251).                                            | Must     | US-006     |
| FR-024 | The system shall remain fully readable, correctly laid out and fully operable before any web font, image or non-critical asset has finished loading (DR-256; 013 **DR-161**, **DR-162**).                                      | Must     | US-001     |
| FR-025 | The system shall make the primary action operable as soon as the page's markup has been parsed, with no dependency on script having run (DR-257).                                                                              | Must     | US-002     |
| FR-026 | The system shall display no screenshot, trailer, demo reel, animated board or decorative hero image (DR-258).                                                                                                                 | Must     | US-001     |
| FR-027 | The system shall present the page in English only, with no language selection and no translated variants (DR-237).                                                                                                             | Must     | US-001     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash._

### SC-001: The scaffold is gone (FR-001)

```gherkin
Given the application is running
When a visitor opens its root address
Then the page is the Info Support Tetris landing page
  And no text from the ASP.NET Core default template appears anywhere on it
```

### SC-002: The page answers the three questions at a glance (FR-002, FR-003, FR-006)

```gherkin
Given a visitor who has never seen this product opens the landing page
When they look at it without scrolling and without reading past the first two lines
Then they can say that it is Tetris
  And they can say that Info Support made it
  And they can point at the single action that starts a game
  And they can point at the way to see the leaderboard
```

### SC-003: Everything essential fits the minimum viewport (FR-006, FR-016)

```gherkin
Given a browser viewport of 1024 by 640 CSS pixels
When the landing page is rendered
Then the mark, the headline, the lead line, the primary action and the leaderboard entry point are all visible
  And neither a horizontal nor a vertical scrollbar is needed to see any of them
  And nothing is clipped or overlapping
```

### SC-004: Starting a game takes one action and no details (FR-007, FR-008, FR-009)

```gherkin
Given a visitor has opened the landing page for the first time
When they activate the primary action
Then a game session begins in status Playing with an empty 10 by 20 playfield at level 1
  And no name, level, difficulty, mode or other detail was requested at any point
  And no form, dialog or confirmation appeared between the action and the game
```

### SC-005: There is nowhere on this page to type a name (FR-008)

```gherkin
Given the landing page is displayed
When every control on it is enumerated
Then none of them accepts text input
  And the only place the product ever asks for a name is the game-over screen of story 009
```

### SC-006: Enter starts a game with no prior navigation (FR-010)

```gherkin
Given a visitor loads the landing page and does not touch the mouse
When they press Enter
Then a game session begins
  And no Tab, arrow key or pointer interaction was needed first
```

### SC-007: Focus lands on the primary action without moving the page (FR-010)

```gherkin
Given the landing page finishes loading
When the initial focus position is inspected
Then the primary action holds focus
  And a visible focus indicator surrounds it
  And the page has not scrolled and no part of the hero is obscured
```

### SC-008: Three focus stops, in the stated order (FR-011)

```gherkin
Given focus is on the primary action
When the visitor presses Tab three times
Then focus moves to the leaderboard entry point, then to the how-to-play entry point, then out of the page
  And each stop shows a visible focus indicator
  And that order is the same as the order the controls appear in visually and in the reading order
```

### SC-009: The page claims no keys of its own (FR-012)

```gherkin
Given the landing page is displayed
When the visitor presses a key that is not Tab, Shift+Tab, Enter or Space
Then nothing on the page reacts
  And the browser's own behaviour for that key is unchanged
```

### SC-010: The starting keystroke does not fall into the game (FR-013)

```gherkin
Given focus is on the primary action
When the visitor activates it with Space and holds the key down
Then a game session begins
  And the first piece has not been hard dropped
  And no game input command was recorded for that keystroke
```

### SC-011: The leaderboard is reachable without having played (FR-015)

```gherkin
Given a colleague who has never played opens the landing page
When they follow the leaderboard entry point
Then the all-time leaderboard is presented
  And no game was started, no score was submitted and no name was requested
  And the address they arrived at is one they can bookmark or paste into a chat message
```

### SC-012: The leaderboard entry point is secondary, not hidden (FR-016)

```gherkin
Given the landing page at the minimum viewport
When the primary action and the leaderboard entry point are compared
Then the primary action is the more prominent of the two
  And the leaderboard entry point is still visible without scrolling and operable by keyboard
```

### SC-013: How-to-play is offered, never imposed (FR-017, FR-018)

```gherkin
Given a visitor opens the landing page for the very first time
When the page is displayed
Then an entry point to the controls and how-to-play material is present
  And it has not opened by itself
  And nothing stands between the visitor and the primary action
```

### SC-014: The fortieth visit costs what the first did (FR-018, FR-019)

```gherkin
Given a colleague has played and returns to the landing page later
When the page loads
Then it is identical to the page a first-time visitor sees
  And no splash, tour, modal or consent dialog appears
  And pressing Enter starts a game, exactly as it did the first time
```

### SC-015: The page remembers nothing (FR-019)

```gherkin
Given a visitor opens the landing page, looks at the leaderboard and returns
When the browser's cookies, local storage and session storage are inspected
Then the application has written nothing to any of them
  And no analytics, telemetry or counter request was made
  And no request was made to any third-party origin
```

### SC-016: The page identifies itself out of context (FR-021, FR-022)

```gherkin
Given a visitor has the landing page open in one of twenty browser tabs
When they read the tab's title, and when a screen reader announces the page on load
Then the title identifies the product without needing the page to be visible
  And exactly one h1 is present
  And the primary action is reachable directly through the page's landmark regions
```

### SC-017: Every control says what it does (FR-023)

```gherkin
Given a screen reader user is listing the page's controls
When each control's accessible name is announced
Then each name is meaningful read entirely on its own
  And no name is "click here", "more", "here" or the bare word "link"
  And the key hint shown beside the primary action is not part of its accessible name
```

### SC-018: The mark survives a failed image load (FR-003)

```gherkin
Given the Info Support mark's image file cannot be fetched
When the landing page is rendered
Then the words "Info Support" appear in its place
  And the visitor can still tell whose product this is
```

### SC-019: The page is usable before the web font resolves (FR-024)

```gherkin
Given a visitor loads the landing page on a cold cache over a throttled connection
When the page first paints
Then the headline, the lead line and all three controls are readable in the fallback stack
  And the primary action can already be activated
  And nothing reflows, resizes or repositions when the web font later resolves or fails
```

### SC-020: The primary action works without script (FR-025)

```gherkin
Given a visitor with script disabled or blocked opens the landing page
When they activate the primary action
Then they arrive at the game's address
  And the landing page itself needed no script to render, lay out or be operated
```

### SC-021: The hero carries no imagery (FR-026, FR-004)

```gherkin
Given the landing page is rendered
When its hero region is inspected
Then it contains the mark, type and token-derived colour and nothing else
  And no screenshot, trailer, demo reel, animated board or decorative background image is present
  And its background resolves to --brand-deep and its primary action to --brand-primary
```

### SC-022: The page introduces no new design values (FR-005)

```gherkin
Given the landing page's styles
When every colour, font size, spacing, radius, border width and duration is collected
Then each one resolves to a semantic token published in 013 section 5
  And no literal value appears
  And no --is- primitive is referenced directly
```

### SC-023: The landing address always shows the landing page (FR-009)

```gherkin
Given a visitor started a game from the landing page
When they go back to the landing page address
Then the landing page is presented
  And no game is in progress on it
  And the abandoned session recorded and submitted nothing
```

## 5. Domain Model — The Page's Composition

_This spec persists nothing and reads nothing. It has no entities in the storage sense: there is no
`LandingPage` record, no visit, no preference and no state (DR-253). Its domain is a **composition** — the
regions the page is built from, the actions it offers, the order in which it says four things, and the copy
that carries them. The structures below are a read-only description of a static page, in the same way that
013's "entities" are design tokens._

### 5.0 How to read this section

**012 owns composition; 013 owns appearance.** Every colour, size and spacing reference below is a token
**name** published by [`013 §5`](013-brand-styling.md#5-domain-model-the-token-system). This spec never
states a hex value, a pixel size or a ratio that 013 already owns. Where 013 marks a `--brand-*` value as
PLACEHOLDER, that affects nothing here: the name is the contract (**Token Name Stability (DR-141)**).

**Copy is proposed, not frozen.** §5.3 gives real, usable English sentences rather than `[headline here]`,
because "tells me what this is" is the testable part of this story and cannot be tested against a bracket.
The **wording is open to change**; what is **not** open is the message order (DR-232), the one-action
promise it makes (DR-229) and the tone (DR-236). Rewriting the headline is an editorial change; adding a
second primary action to it is a breaking one.

### 5.1 Page regions

The page is four regions. There is no navigation bar, no sidebar and no footer (DR-246).

| Region       | Landmark role | Contains                                                              | Background          | Notes                                                                 |
| ------------ | ------------- | --------------------------------------------------------------------- | ------------------- | ---------------------------------------------------------------------- |
| **Brand**    | banner        | The Info Support mark, as supplied, with its clear space (DR-238).    | `--brand-deep`      | Visually part of the hero band; a separate landmark so it is skippable. |
| **Hero**     | main          | The `h1`, the lead line, the primary action.                          | `--brand-deep`      | Carries messages 1 – 3 (DR-232). Radius `--radius-lg` per 013 §5.4.     |
| **Ways in**  | *(within main)* | The leaderboard entry point and the how-to-play entry point.        | `--surface`         | Carries message 4. Separated from the hero by `--border-subtle`.        |
| **Colophon** | contentinfo   | One quiet line of attribution. No links.                              | `--surface`         | Optional; see DR-246. It exists so the page does not end abruptly, not to host navigation. |

The brand region and the hero share `--brand-deep` so they read as one lockup; the landmark split exists
for assistive technology, not for the eye. `--text-primary` on `--brand-deep` is verified by 013 at
**14.46:1**, so the hero's heading and lead both clear the body-text floor of **Body Text Contrast
(DR-146)** without the hero needing a text colour of its own.

### 5.2 Actions

A **LandingAction** is the page's only interactive concept. It is a value object — it has no identity, no
state and nothing to persist.

| Attribute        | Type    | Constraints                                        | Description                                                          |
| ---------------- | ------- | -------------------------------------------------- | --------------------------------------------------------------------- |
| `rank`           | enum    | `Primary` \| `Secondary` \| `Tertiary`; exactly one `Primary` | Its place in the visual hierarchy (DR-233).               |
| `label`          | string  | required; the visible text                         | What the visitor reads.                                               |
| `accessibleName` | string  | required; meaningful read in isolation (DR-251)    | Equals `label` for all three actions here.                            |
| `destination`    | enum    | `Game` \| `Leaderboard` \| `HowToPlay`             | Where activating it leads. All three are addresses, not behaviours.   |
| `focusOrder`     | integer | 1 – 3, unique, equal to visual order (DR-248)      | Position in the tab sequence.                                         |
| `keyHint`        | string? | decorative only; excluded from `accessibleName`    | Present on the primary action, absent on the others.                  |

The three actions, in full:

| `rank`      | `label`                | `destination` | `focusOrder` | `keyHint` | Tokens                                                                 |
| ----------- | ---------------------- | ------------- | ------------ | --------- | ------------------------------------------------------------------------ |
| `Primary`   | `Play`                 | `Game`        | 1            | `Enter`   | `--brand-primary` fill, `--text-on-brand` label, `--radius-md`; hover `--brand-primary-hover`, pressed `--brand-primary-active`. |
| `Secondary` | `View the leaderboard` | `Leaderboard` | 2            | —         | `--surface-raised` with `--border-strong`, `--text-primary` label, `--radius-md`. |
| `Tertiary`  | `How to play`          | `HowToPlay`   | 3            | —         | Text-only on `--surface`, `--text-secondary` label.                      |

**Three actions and no more (DR-242).** A fourth would start competing with the first. The restart action
of 001's **FR-005** is not here — it belongs to the game screen, where there is a game to restart.

**The key hint is decorative.** `Enter` beside `Play` is a visual affordance for sighted keyboard users. A
screen reader already announces `Play` as a button and already knows that `Enter` activates a button, so
repeating it in the accessible name would be noise. It is therefore excluded from the name (DR-251,
SC-017) — this is deliberate, not an omission.

### 5.3 Proposed copy

**Proposed, and open to wording changes** (§5.0). English throughout (DR-237).

| Slot                       | Proposed text                                                                                       |
| -------------------------- | ----------------------------------------------------------------------------------------------------- |
| Document title             | `Info Support Tetris`                                                                                 |
| Mark text alternative      | `Info Support`                                                                                        |
| `h1`                       | `Tetris`                                                                                              |
| Lead line                  | `Ten columns, five minutes, one all-time leaderboard. No sign-up, no settings — one key and you're playing.` |
| Primary action label       | `Play`                                                                                                |
| Primary action key hint    | `Enter` _(decorative)_                                                                                |
| Secondary action label     | `View the leaderboard`                                                                                |
| Tertiary action label      | `How to play`                                                                                         |
| Colophon                   | `An Info Support product.`                                                                            |

**Why the title and the `h1` differ.** The `h1` is `Tetris` because the mark sits directly above it and the
two read as one lockup — the biggest word on the page should be the thing the page is. The document title
is `Info Support Tetris` because a title is consumed **out of context**: in a tab strip, a bookmark list, a
browser history entry and a link preview pasted into a chat, where the bare word `Tetris` identifies
nothing and names a product this is not (DR-254).

**Why the mark carries a text alternative rather than being decorative.** The `h1` does not contain the
words "Info Support", so the mark is the only thing in the page's structure that does. Giving it the
alternative `Info Support` means message 2 survives a blocked image, a slow asset and a screen reader alike
(DR-239, SC-018). This is the one place where the mark is load-bearing rather than ornamental.

**On tone (DR-236).** Short, concrete, confident. Nouns the visitor can picture — *ten columns, five
minutes* — and a promise that is immediately testable: *one key and you're playing*. No "welcome to", no
"we are excited to", no "unleash", no exclamation marks, no feature list. The lead line does four jobs in
seventeen words: says what the game is, hints at the leaderboard, rules out sign-up, and tells you the
start is one keystroke.

### 5.4 The message order

**Four messages, in this order of prominence (DR-232).** This is the spine of the layout, and the reason
the hierarchy is what it is.

| # | Message                                    | Carried by                                              | Weight                                                    |
| - | ------------------------------------------- | -------------------------------------------------------- | ----------------------------------------------------------- |
| 1 | **This is Tetris.**                        | The `h1`.                                                | `--font-size-display`, the largest type on the page.        |
| 2 | **Info Support made it.**                  | The mark, plus the `--brand-deep` hero band.             | Directly above the `h1`; colour does the rest.              |
| 3 | **Press here to play.**                    | The primary action.                                      | The only `--brand-primary` fill on the page.                |
| 4 | **There is a leaderboard; this sees it.**  | The lead line's closing clause, then the secondary action. | `--font-size-body-lg` lead, then a bordered, unfilled control. |

**Visual hierarchy follows message order (DR-233)**, and **reading order follows visual order (DR-234)**.
The second is not a restatement of the first: a layout that places the leaderboard control visually beside
the play button but earlier in the markup would pass a screenshot review and fail a screen reader. The
rule is that the sequence a screen reader hears, the sequence `Tab` visits and the sequence the eye travels
are the same sequence — mark, `h1`, lead, `Play`, `View the leaderboard`, `How to play`.

**Why there is exactly one filled control.** `--brand-primary` as a fill appears once on the page. The
moment a second control carries it, the visitor has to choose, and the three-second budget goes on the
choice rather than on the game. The leaderboard route is a bordered control and the how-to-play route is
plain text, so the hierarchy is legible before a single word is read.

**Why the lead line is one line.** A newcomer reads it once; a returning colleague's eye skips a single
line without effort and without needing to dismiss anything. An explainer that must be *dismissed* implies
somewhere to record the dismissal, and this page records nothing (DR-253). One line is the only length that
serves both visitors at once (DR-252).

### 5.5 Tokens consumed

The complete set of 013 tokens this page uses. **Nothing outside this list, and nothing new** (DR-241;
013's **DR-141**, **DR-142**). Listing them exhaustively is what makes SC-022 and SUC-004 checkable, and
is 012's half of 013's **SC-022 / SUC-007**.

| Purpose                     | Tokens                                                                                                       |
| --------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Hero and brand band         | `--brand-deep`, `--radius-lg`                                                                                  |
| Primary action              | `--brand-primary`, `--brand-primary-hover`, `--brand-primary-active`, `--text-on-brand`, `--radius-md`, `--border-width-standard` |
| Page body                   | `--surface`, `--surface-raised`, `--border-subtle`, `--border-strong`                                          |
| Text                        | `--text-primary`, `--text-secondary`                                                                           |
| Type                        | `--font-display`, `--font-body`, `--font-size-display`, `--font-size-body-lg`, `--font-size-body`, `--font-size-label`, `--font-weight-regular`, `--font-weight-semibold`, `--font-weight-bold` |
| Focus                       | `--focus-ring`, `--focus-ring-width`, `--focus-ring-offset`                                                    |
| Spacing                     | `--space-2`, `--space-3`, `--space-4`, `--space-6`, `--space-8`, `--space-10`, `--space-12`                    |
| Motion                      | `--duration-fast`, `--easing-standard` (hover and focus transitions only)                                      |
| Layout bound                | `--viewport-min-width`, `--viewport-min-height`                                                                |

**No board tokens and no piece colours.** `--piece-*`, `--board-*` and `--cell-*` do not appear on this
page, which is the landing-page half of 013's **Board / Chrome Colour Quarantine (DR-157)** — there is no
playfield here, so there is nothing for a brand colour to collide with.

**No `--font-numeric` and no numeric readouts.** The page shows no score, no rank and no count. A "players
today" or "games played" figure would be telemetry, which DR-253 forbids.

### 5.6 Domain Rules and Invariants

_New rules introduced by this spec, numbered **DR-229 – DR-258** so that IDs stay globally unique across
concurrently written specs. Later specs should cite these by ID and name, e.g. "per **One Action, No
Details (DR-229)**"._

#### The one-action promise

- **DR-229 — One Action, No Details**: The landing page's primary action starts a `GameSession`
  **immediately**. Between the visitor's activation and the first piece falling there is no form, no
  dialog, no confirmation, no interstitial and no choice of any kind. This is 001's **FR-001** — *one
  action and no details* — and this page is where that promise is either kept or broken. **It is the spine
  of the page**: every other rule here exists to protect it or to stay out of its way. A change that adds
  anything between the action and the game is a change to this rule, agreed here, not a layout tweak.
- **DR-230 — No Options Before Play**: There is no difficulty picker, no starting-level select, no speed
  setting, no board-size option, no mode choice and no "advanced" disclosure. There is nothing to choose
  because nothing is configurable: the level starts at 1 and climbs by **story 007**'s curve, the board is
  10 × 20 by **DR-01**, and the keys are fixed and non-configurable by **002's DR-29**.
- **DR-231 — The Name Is Asked Once, at Game Over**: The landing page never requests a name, and contains
  no text input of any kind. **Story 009** asks for the name exactly once, after the run, when there is
  something worth putting a name on. Asking first would cost the visitor a decision before they knew
  whether they cared, and would create a second place a name can be typed — contradicting 009's **A Name
  Is a Label, Not an Identity (DR-112)**, which depends on the name being attached to a run rather than to
  a person.

#### What the page says

- **DR-232 — The Four Messages**: The page communicates exactly four things, in this order of prominence:
  (1) this is Tetris, (2) Info Support made it, (3) this action starts a game, (4) there is an all-time
  leaderboard and this reaches it. A fifth message competes with the fourth and is not added without
  removing one. The budget is roughly three seconds of a stranger's attention, and four messages is what
  fits in it.
- **DR-233 — Hierarchy Follows Message Order**: Size, colour weight and position rank the four messages in
  DR-232's order. Exactly **one** control carries a `--brand-primary` fill — the primary action — so that
  the thing to do is identifiable before any word is read. A second filled control turns a glance into a
  choice.
- **DR-234 — Reading Order Equals Visual Order**: The order a screen reader announces the page's content,
  the order `Tab` visits its controls and the order the eye travels it are the **same order**: mark, `h1`,
  lead line, `Play`, `View the leaderboard`, `How to play`. Styling never reorders content relative to the
  markup. This is distinct from DR-233 and is not implied by it — a layout can rank things correctly by
  eye and still hand a screen reader the wrong sequence.
- **DR-235 — Everything Essential Is Above the Fold**: At the **1024 × 640 CSS pixel** minimum viewport of
  013's **Minimum Viewport (DR-166)**, the mark, the `h1`, the lead line, the primary action and the
  leaderboard entry point are all visible with **no scrolling in either axis**. A landing page whose call
  to action is below the fold on the smallest supported laptop has not landed anybody. The figure is 013's
  and is not re-chosen here.
- **DR-236 — Plain, Confident Copy**: The copy is short, concrete and written in the product's own voice.
  No "welcome to", no "we are excited to", no superlatives, no exclamation marks, no feature list, no
  corporate boilerplate. The lead line is **one line** at the minimum viewport. Wording is editorial and
  may change freely; the message order (DR-232) and the promise (DR-229) may not.
- **DR-237 — English Only**: Every string on the page is English, matching every other screen in 001–014,
  the domain vocabulary of all thirteen specs and the codebase itself. There is no language picker, no
  resource-file indirection and no `Accept-Language` negotiation. Info Support works in Dutch and English;
  a half-translated interface is worse than a consistently English one, and a coffee-break game does not
  earn a localisation pipeline. Revisiting this means translating all of 008–014, not just this page.

#### Branding

- **DR-238 — Mark Placement and Clear Space**: The Info Support mark is used **as supplied** — not redrawn,
  recoloured, stretched or reconstructed (013 §9.2) — and sits in the brand region directly above the `h1`,
  with the clear space 013 §9.2 reserves kept free of text and controls on all four sides.
- **DR-239 — The Mark Carries Its Name**: The mark has the text alternative `Info Support`. It is the only
  element in the page's structure that carries message 2, because the `h1` is `Tetris`, so it is
  load-bearing rather than decorative: a blocked image, a slow asset or a screen reader must each still
  deliver "Info Support". If the `h1` is ever changed to contain the words, this rule flips to "decorative"
  in the same change — duplicate announcement is the failure mode on the other side.
- **DR-240 — Hero Treatment**: The hero band is `--brand-deep` with heading and lead in `--text-primary`,
  per 013 §5.6. The primary action is `--brand-primary` with `--text-on-brand` and **no other text colour**
  on it, per **Text-on-Brand Pairing (DR-149)**. When the placeholder brand values are replaced with the
  authoritative Info Support ones, that pairing is re-verified at ≥ 4.5:1 and the brand value adjusted if
  it fails — the text token is never weakened (DR-149).
- **DR-241 — Tokens Only**: This page introduces **no** colour, type size, spacing, radius, border width or
  duration value of its own. Every value is a semantic token from 013 §5, listed exhaustively in §5.5. No
  `--is-*` primitive is referenced directly. This is 013's **No Literal Values (DR-142)** and **Token Name
  Stability (DR-141)** applied here, and 012's half of 013's **SUC-007**.

#### Ways in and ways on

- **DR-242 — Three Ways In, and No More**: The page offers exactly three controls — play, leaderboard,
  how-to-play — in that rank. No fourth. 001's *new game* action (**FR-005**) belongs to the game screen,
  where there is a game to discard; it is not duplicated here.
- **DR-243 — The Leaderboard Entry Point**: The page offers a route to the all-time leaderboard with **no
  precondition whatsoever** — no finished run, no submitted score, no prior play, no name — satisfying
  010's **Reachable Without Playing (DR-193)** and its **FR-014**. The route resolves to the stable,
  shareable address of 010's **The Board Has a Shareable Address (DR-194)**. What is on the other side —
  ranking, the ranked window, the table, the empty state, the failure states — is entirely 010's, and 012
  makes no assumption about it, including about whether it loads successfully.
- **DR-244 — How-to-Play Is Offered, Never Imposed**: An entry point to **story 014**'s controls /
  how-to-play material is present, ranked third, and is **never** auto-opened, never a modal on first
  visit, and never a step the visitor passes through on the way to playing. 012 designs none of it, lists
  no key and makes no claim about whether 014 renders as a panel, a page or an overlay. Tetris is a game
  most people already know; the controls are there for the person who wants them and invisible to everyone
  else.
- **DR-245 — The Game Has Its Own Address**: Activating the primary action takes the visitor to the game at
  **its own address**, distinct from the landing page's. The landing page address always presents the
  landing page and never a game in progress, so that it stays shareable and bookmarkable (the same
  reasoning as 010's DR-194) and so that going back from a game returns here rather than leaving the
  product. An abandoned session records and submits nothing, per 001's **FR-005** and 009. *How* the
  browser makes that transition is an architecture decision deferred to arc42 §09.
- **DR-246 — No Other Navigation**: There is no navigation bar, no menu, no breadcrumb, no sidebar and no
  footer link list. The product is one game, one board and one front door; a nav bar listing three
  destinations is chrome around a page whose entire job is to point at one of them. The colophon is a
  single unlinked line of attribution and may be omitted without affecting any requirement.

#### Keyboard and focus

- **DR-247 — Play Holds Initial Focus**: On load, keyboard focus is placed on the primary action, so that
  `Enter` starts a game with no prior navigation. Placing that focus must **not** scroll the page, must not
  obscure any part of the hero, and must leave the document title and `h1` to announce normally. This is a
  deliberate exception to the general rule against moving focus on load: the target is the page's single
  purpose, it is inside the main landmark, and the whole page is three controls deep, so nothing is
  skipped past.
- **DR-248 — Focus Order**: The tab sequence is exactly: primary action, leaderboard entry point,
  how-to-play entry point. Three stops, matching the visual and reading order (DR-234). No skip link — a
  skip link exists to bypass repeated navigation, and there is none to bypass (DR-246). No focus trap, no
  roving tabindex, no positive `tabindex` values.
- **DR-249 — No "Press Any Key"**: The page binds **no** global key handler and claims no key for itself.
  The keyboard affordance is focus on a real control (DR-247) plus a visible, decorative key hint. "Press
  any key to start" is rejected: it swallows the keys assistive technology and the browser need, it gives
  a visitor no idea *which* key or that anything is listening, and it makes the leaderboard and
  how-to-play routes unreachable for anyone who starts typing. A real button announces itself, works with
  both `Enter` and `Space` natively, and costs the same one keystroke.
- **DR-250 — The Starting Keystroke Stays Out of the Game**: The key press that activates the primary
  action is consumed by the landing page and is **never** delivered to the newly started session as an
  input command. Without this, activating with `Space` — which every native button accepts — would hard
  drop the first piece before the visitor had seen it (**003**), and a held `Enter` would arrive as
  repeats. The game's first input is the first key pressed *after* the game exists.
- **DR-251 — Real Controls, Honest Names**: Every control has a native, keyboard-operable role, is reachable
  by `Tab`, shows the `--focus-ring` indicator of 013's **Focus Visibility (DR-150)** at
  `--focus-ring-width` and `--focus-ring-offset`, and has an accessible name that is meaningful **read
  entirely on its own** — never "click here", "more" or "here". The primary action's key hint is
  **excluded** from its accessible name: a screen reader already announces the control as a button and
  already knows `Enter` activates buttons, so including it is noise, not help.

#### Returning visitors

- **DR-252 — Never an Interstitial**: The page shows no splash screen, no modal, no tour, no carousel, no
  onboarding step, no "new here?" prompt, no newsletter box and no consent dialog — on the first visit or
  any other. The explainer is **one line of text that is simply there**, which a newcomer reads and a
  returning colleague's eye crosses without effort. Anything that must be *dismissed* implies somewhere to
  record the dismissal, and this page records nothing (DR-253); anything undismissable taxes the fortieth
  visit to serve the first.
- **DR-253 — The Page Remembers Nothing**: The landing page writes and reads **no** cookie, no local or
  session storage, no server-side session, no visit counter, no analytics event and no telemetry of any
  kind, and makes no request to any third-party origin. There is consequently nothing to consent to and no
  cookie banner. This continues 009's **Minimal Personal Data (DR-131)** — the typed name is the only
  personal datum the product collects — and 010's **Reading Never Writes (DR-195)**. It is also what makes
  DR-252 cheap: with nothing remembered, every visit is identical by construction rather than by effort.

#### Accessibility and performance

- **DR-254 — Title and Heading**: The document has a title that identifies the product **out of context**
  — in a tab strip, a bookmark, a history entry or a pasted link — and exactly **one** `h1`. The two differ
  deliberately (§5.3): the title names the product, the `h1` names the page's subject.
- **DR-255 — Landmark Regions**: The page is structured as landmark regions (§5.1), with the hero as the
  **main** region, so that a screen reader user can jump straight to the primary action rather than listen
  to the brand band first.
- **DR-256 — Usable Before Fonts and Non-Critical Assets**: The page is fully readable, correctly laid out
  and fully operable **before** any web font, image or non-critical asset has resolved. 013 owns the
  font-loading strategy — self-hosted, preloaded, `font-display: optional`, metric-matched fallback
  (**Font Loading Without Shift (DR-161)**, **Fallback Sufficiency (DR-162)**, **First-Party Fonts Only
  (DR-163)**) — and this page states only its consequence here: nothing on it waits for a font, and
  nothing on it moves when one arrives or fails.
- **DR-257 — The Play Action Is Live at First Paint**: The primary action is operable as soon as the
  markup has been parsed, with **no dependency on script having run**. All three actions are addresses,
  not behaviours, so the page needs no script at all to render, lay out or be operated. A landing page
  whose only button waits on a bundle has a slower first keystroke than the game it leads to.
- **DR-258 — No Hero Imagery**: The hero carries the mark, type and token-derived colour, and nothing else
  — no screenshot, no trailer, no demo reel, no animated board, no decorative background image. Three
  reasons, in order: the largest contentful paint stays a text node and so NFR-001's budget is met without
  optimisation work; a screenshot of a Tetris board tells a visitor nothing they did not already know from
  the word *Tetris*; and an animated preview would need a reduced-motion variant (013's **Reduced Motion
  (DR-165)**) for no informational gain.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty),
and every contrast ratio, type scale and font budget belongs to 013 §6 and is cited rather than restated._

**Measurement baseline.** Unless stated otherwise, figures are measured on an **office laptop** — a
1366 × 768 machine of the class 013's **DR-166** sizes the layout for — on the **office LAN**, with a
**cold browser cache**, at the **95th percentile** over at least 20 loads. This is the real first
impression, not a lab number, and it is the condition under which a colleague actually opens the link.

| ID      | Category        | Requirement                                                                                                                                                                                                 |
| ------- | --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance     | **Largest contentful paint ≤ 1.0 s at p95** on the baseline above. The LCP element is a text node — the `h1` — by construction (DR-258), so the budget is met by not shipping anything heavy rather than by optimising something heavy. |
| NFR-002 | Performance     | **The primary action is operable ≤ 1.0 s at p95** from navigation start, and at first paint in every case, with no dependency on script, font or image (DR-257, DR-256). Measured as time to the control being focusable and activatable, not to a framework signalling readiness. |
| NFR-003 | Performance     | **Total transferred weight of the landing page ≤ 100 KB compressed**, excluding web fonts (013's NFR-010 budgets those separately at ≤ 120 KB) and excluding anything fetched after the page is interactive. |
| NFR-004 | Performance     | **From activating the primary action to the first piece falling: ≤ 500 ms at p95** on the baseline. The one-action promise of DR-229 is about effort; this is about the wait that would undo it.               |
| NFR-005 | Stability       | **Cumulative layout shift ≤ 0.0** for the landing page. Font loading contributes zero by 013's **DR-161**; the page has no other late-arriving content, because it has no imagery (DR-258) and no script-inserted content (DR-257). |
| NFR-006 | Accessibility   | An automated accessibility audit of the page reports **zero** violations at WCAG 2.1 AA, and the page is verified manually with a screen reader and keyboard only. Contrast is 013's (**DR-146** – **DR-150**) and is not re-measured here. |
| NFR-007 | Accessibility   | **The page is fully operable with the keyboard alone**, in exactly three focus stops (DR-248), with the `--focus-ring` indicator visible at every one (013 **DR-150**).                                       |
| NFR-008 | Usability       | **At least 4 of 5 colleagues** who have never seen the product, shown the page for **three seconds** and then asked, can name the game, name Info Support as its owner, point at the action that starts a game and say that there is a leaderboard (DR-232). This is the testable form of "tells me what this is". |
| NFR-009 | Layout          | At **1024 × 640 CSS pixels** the page renders with no scrolling in either axis and nothing clipped or overlapping (DR-235; 013 **DR-166**). At 200% browser zoom the page may scroll, but the primary action remains reachable and first in the reading order (013 EC-8). |
| NFR-010 | Privacy         | **Zero** cookies, zero storage writes, zero analytics or telemetry requests, and zero requests to any third-party origin, on any visit (DR-253). Verified by inspecting the network log and the storage inspector after a full visit. |
| NFR-011 | Maintainability | **Zero** literal colour, type-size, spacing, radius, border-width or duration values in the page's styles, and **zero** direct references to an `--is-*` primitive (DR-241). Shares the automated check behind 013's **SUC-004**. |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                       | Expected Behavior                                                                                                                                                       |
| ----- | ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | The visitor activates the primary action with `Space` rather than `Enter`, and holds it down.   | A game starts, and the keystroke is **not** forwarded into the session (DR-250) — the first piece is not hard dropped (003) and no repeats arrive. The game's first input is the first key pressed after it exists. |
| EC-2  | The visitor presses `Enter` repeatedly, or double-clicks the primary action.                    | Exactly one `GameSession` starts. Subsequent activations arrive after the landing page is no longer displayed and do nothing; they never start a second session or stack two games. |
| EC-3  | The Info Support mark's image file is missing, blocked or slow.                                 | The text alternative `Info Support` appears in its place (DR-239). Message 2 of DR-232 survives intact, the layout does not shift (NFR-005), and no error is shown — this is not something the visitor needs to know about. |
| EC-4  | The web font is blocked by a corporate proxy or an extension, or arrives after first paint.     | The page renders and is fully operable in the metric-matched fallback stack at identical sizes and positions (013 **DR-162**, EC-1/EC-2). `font-display: optional` declines a post-paint swap, so nothing moves (013 **DR-161**). |
| EC-5  | Script is disabled, blocked or fails to load.                                                   | The page renders, lays out and operates normally, and the primary action reaches the game (DR-257, SC-020). Whether the *game* then runs without script is 001's concern, not this page's. |
| EC-6  | The leaderboard is empty, failing to load, or unreachable.                                      | The landing page is unaffected and still offers the entry point. 010 owns every board state including the empty one and the failed one (010 **DR-185** – **DR-190**); 012 makes no claim about what is on the other side (DR-243). |
| EC-7  | A visitor bookmarks the game's address and opens it directly, skipping the landing page.        | A game starts, per 001's **FR-002**. The landing page is a front door, not a gate — skipping it is a feature of DR-245, not a bypass of anything. |
| EC-8  | A visitor starts a game, abandons it and presses the browser's Back button.                     | The landing page is presented, with no game in progress on it (DR-245). The abandoned session is discarded and recorded or submitted nowhere (001 **FR-005**, 009). |
| EC-9  | A visitor returns to the landing page address while a game is in progress in another tab.       | The landing page is presented normally. It holds no state (DR-253) and knows nothing about any session, so there is nothing to reconcile; the other tab's game is untouched. |
| EC-10 | The viewport is narrower or shorter than 1024 × 640 — a phone, a split-screen window, a 200% zoom. | Unsupported below the minimum (013 **DR-166**). The page degrades to scrolling rather than clipping or overlapping, and the primary action stays reachable and first in the reading order. No mobile layout is provided (§1.3). |
| EC-11 | A visitor has requested reduced motion.                                                         | Nothing on the page is affected in substance: it has no entrance animation and no decorative motion, only token-governed hover and focus transitions, which resolve to `--duration-instant` (013 **DR-165**). No information is carried by motion, so none is lost. |
| EC-12 | A visitor runs the browser in forced-colours or high-contrast mode.                             | The page remains fully operable: text and controls adopt the user's colours and the focus indicator remains visible (013 **DR-168**). The page carries no meaning by colour alone — the hierarchy of DR-233 is also carried by size, position and order. |
| EC-13 | The brand placeholder values are swapped for Info Support's authoritative ones (013 OQ 1).      | Nothing on this page changes. 012 consumes token **names** only (DR-241), and 013's **Single Brand Source (DR-140)** makes the swap a single-file change. The `--text-on-brand` pairing is re-verified at that point, not here (DR-240, 013 **DR-149**). |
| EC-14 | A visitor uses a screen reader and lands on the page with focus already moved to `Play`.        | The document title and the `h1` still announce, the focus move does not scroll the page, and the visitor can read upward through the main landmark or activate immediately (DR-247, DR-255). Three focus stops means nothing meaningful sits behind the focused control. |
| EC-15 | Someone proposes adding an "About us", "Contact" or "Privacy" page, or a nav bar to reach them. | Out of scope by §1.3 and DR-246. There is no personal data to write a privacy page about beyond 009's **DR-131**, and a nav bar listing three destinations is chrome around a page whose job is to point at one of them. |
| EC-16 | A contributor adds a second `--brand-primary`-filled control to the page.                       | A defect against DR-233, caught in review and by SUC-003. Exactly one filled control exists so that the thing to do is identifiable before a word is read. |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                             |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| SUC-001 | All 23 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                       |
| SUC-002 | Each of the thirty domain rules DR-229 – DR-258 is covered by at least one automated test or, where a rule is editorial (DR-236), by a documented review step.          |
| SUC-003 | An automated check confirms the page has exactly one `h1`, exactly three focus stops in the DR-248 order, exactly one `--brand-primary`-filled control, and no text input. |
| SUC-004 | An automated check reports zero literal design values and zero direct `--is-*` primitive references in the page's styles (NFR-011) — 012's half of 013's **SUC-007**.    |
| SUC-005 | An automated accessibility audit reports zero WCAG 2.1 AA violations, and a manual screen-reader-and-keyboard pass starts a game and reaches the leaderboard without a pointer (NFR-006, NFR-007). |
| SUC-006 | The three-second comprehension test of NFR-008 passes with at least 4 of 5 colleagues who were not involved in building the product. This is the acceptance test for "tells me what this is". |
| SUC-007 | On the measurement baseline of §6, LCP is ≤ 1.0 s at p95, the primary action is operable at first paint, and cumulative layout shift is 0 (NFR-001, NFR-002, NFR-005).   |
| SUC-008 | The landing page renders, lays out and is fully operable with the web font blocked, all images blocked and script disabled, in three independent runs (EC-3, EC-4, EC-5). |
| SUC-009 | A full visit — load, open the leaderboard, return, start a game — writes zero cookies and zero storage entries and makes zero third-party requests (NFR-010).            |
| SUC-010 | At 1024 × 640 the page needs no scrolling in either axis, with every element of DR-232 visible (NFR-009, SC-003).                                                        |
| SUC-011 | No text from the ASP.NET Core default template remains anywhere in `Pages/Index.cshtml` or its code-behind (SC-001).                                                     |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **`docs/product/story-map.md`** — the source of the story numbering, the *Look like Info Support*
  activity, the Goal paragraph this page has to make true in three seconds, and the Primary User the copy
  is written for.
- **Spec 013 (`013-brand-styling.md`)** — **the blocking dependency**. 013 owns every token this page
  consumes (§5.5). Its `--brand-*` values are placeholders pending Info Support's brand guidelines (013
  Open Question 1), which blocks **release**, not implementation: the token **names** are stable under
  **Token Name Stability (DR-141)** and the swap is a single-file change under **Single Brand Source
  (DR-140)**. 012 is buildable today and is not blocked on that question — and does not restate it.
- **Spec 001 (`001-start-new-game.md`)** — **FR-001**'s *one action, no details* is the promise DR-229
  keeps, and **FR-002** defines the session the primary action starts. 012 adds no requirement to 001 and
  changes none of its rules.
- **Spec 010 (`010-view-leaderboard.md`)** — **FR-014** requires an entry point here; **Reachable Without
  Playing (DR-193)** fixes that it has no precondition; **The Board Has a Shareable Address (DR-194)**
  gives it something stable to point at. 012 supplies the entry point and nothing else.
- **Spec 009 (`009-submit-score.md`)** — **Minimal Personal Data (DR-131)** is why there is no cookie
  banner, and the name-entry step it owns is why there is no name box here (DR-231).
- **Spec 014 (controls / how-to-play)** — downstream. 012 provides its entry point (DR-244) and requires
  only that 014 be reachable from an address; 014 owns everything behind it.
- **The Info Support mark, as supplied** — needed as an asset file. Until it arrives, the text alternative
  of DR-239 renders in its place, which is also the EC-3 fallback, so the page is complete without it.
- **Downstream dependents**: none. 012 is a leaf — no other spec consumes anything it defines. That is
  deliberate: a front door that other screens depended on would be a front door that could not be
  rewritten.

### 9.2 Constraints

- **The one-action promise is a product constraint, not a design preference.** It comes from the story
  map's *"zero friction to start"* and 001's FR-001. Any future request to add a field, a choice or a step
  to this page is a change to DR-229 and to 001, agreed there.
- **Accessibility target**: WCAG 2.1 **AA**, per 013 §9.2. AAA is not a target.
- **Platform**: desktop browser on a laptop with a keyboard; minimum viewport 1024 × 640 CSS pixels (013
  **DR-166**). No mobile, no touch (story map).
- **Trademark.** The Info Support logo and wordmark are trademarks of Info Support B.V., used in-house
  under the company's own brand guidelines and **as supplied** — not redrawn, recoloured, stretched or used
  to imply endorsement outside the company (013 §9.2). 012 places the mark; it does not author it.
- **This page is the product's first impression and its only unprompted one.** Everything else is reached
  from here or from a link someone sent. That is why its performance budget (NFR-001 – NFR-005) is tighter
  than any other screen's and why its content budget (DR-232) is four messages.
- **Technology context** (from `CLAUDE.md`): ASP.NET Core 10 Razor Pages, root namespace
  `infosupport_tetris`. This page replaces `Pages/Index.cshtml`, which is still the unmodified
  `dotnet new webapp` welcome text. Client libraries are vendored in `wwwroot/lib/` and are **not**
  npm-managed, so no build step may be assumed; Bootstrap is re-skinned through 013's bridge, never edited
  in place (013 **DR-167**).
- **No behavioural change to any earlier spec.** 012 adds a screen in front of the product and alters no
  rule in 001–011 or 013.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*. Building 012 is a natural trigger for filling them in, per
> `CLAUDE.md` ("fill them in as architectural decisions for the Tetris implementation are made").

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                             |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01. Introduction and Goals (`01-introduction-and-goals.md`)      | The story map's Goal paragraph — zero friction, a proper game, office bragging rights — is what this page has three seconds to convey. The top quality goals ranked there should include the first-impression budget of §6. *Currently empty.* |
| 02. Architecture Constraints (`02-architecture-constraints.md`)  | Should record the constraints in §9.2 that outlive this feature: the one-action-to-start promise, WCAG 2.1 AA, the 1024 × 640 minimum viewport, English-only, and no client-side storage or third-party requests. *Currently empty.* |
| 03. System Scope and Context (`03-system-scope-and-context.md`)  | The landing page is the system's only unprompted entry point and the boundary a visitor crosses. The addresses it names — landing, game, leaderboard, how-to-play — are the product's external surface. *Currently empty.* |
| 05. Building Block View (`05-building-block-view.md`)            | 012 replaces the scaffold's `Pages/Index.cshtml` with the landing page building block, which depends on 013's presentation tokens and on nothing in the game domain — it starts a session but reads no game state. *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)      | The natural home for three positions this spec instantiates product-wide: no client-side storage or telemetry (DR-253, extending 009's DR-131 and 010's DR-195), keyboard-first operation with a visible focus indicator, and progressive enhancement — markup first, script optional (DR-257). *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`)      | 012 warrants ADRs for: the game having its own address rather than swapping in place (DR-245), focusing the primary action on load (DR-247), rejecting "press any key" in favour of a real control (DR-249), and no client-side storage of any kind (DR-253). *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)          | Should host the first-impression quality scenarios §6 instantiates — the LCP and interactivity budgets, zero layout shift, and the three-second comprehension test — alongside 013's accessibility and layout-stability scenarios. *Currently empty.* |
| 12. Glossary (`12-glossary.md`)                                  | Should be populated from §5 — landing page, visitor, hero, lead line, primary/secondary/tertiary action, key hint, landmark region, accessible name, above the fold. *Currently empty.* |

## 10. Open Questions

_Every decision this spec faced was resolved. No `[NEEDS CLARIFICATION]` marker remains: the one genuinely
open brand question belongs to **013 Open Question 1** and is tracked there, not duplicated here._

| #   | Question                                                                                                  | Owner      | Status   | Resolution                                                                                                                                                                                                                                                  |
| --- | ------------------------------------------------------------------------------------------------------------ | ---------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Should the landing page ask for the player's name before the game, so the leaderboard already knows them?    | Daan Harms | Resolved | **No.** It would break 001's FR-001 outright and create a second place a name can be typed, contradicting 009's **A Name Is a Label, Not an Identity (DR-112)**, which depends on the name being attached to a *run*. The name is asked once, after the run, when there is something worth naming (DR-231). |
| 2   | Should the page offer a difficulty or starting-level choice?                                                 | Daan Harms | Resolved | **No.** Nothing is configurable: level starts at 1 and climbs by 007's curve, the board is 10 × 20 (**DR-01**), the keys are fixed (**002 DR-29**). A picker would be a choice with no options behind it, spending the visitor's attention for nothing (DR-230). |
| 3   | "Press any key to start", or focus a real button?                                                            | Daan Harms | Resolved | **Focus a real button** (DR-247, DR-249). "Press any key" swallows the keys assistive technology and the browser need, tells the visitor neither which key nor that anything is listening, and makes the other two routes unreachable for anyone who starts typing. A focused button costs the same single keystroke and announces itself. |
| 4   | Is moving focus on page load acceptable, given it is usually discouraged?                                    | Daan Harms | Resolved | **Yes, here.** The target is the page's single purpose, sits inside the main landmark after the `h1` and lead in reading order, and the whole page is three focus stops deep — nothing meaningful is skipped. The title and `h1` still announce, and the move must not scroll the page (DR-247). The rule exists to prevent disorientation; on a three-control page there is nothing to be disoriented by. |
| 5   | Should the `h1` be "Tetris" or "Info Support Tetris"?                                                        | Daan Harms | Resolved | **`Tetris`**, with the mark directly above it and the document title `Info Support Tetris`. Message order (DR-232) puts *this is Tetris* first and biggest; the mark delivers *Info Support* visually and, through its text alternative, structurally (DR-239). The title differs deliberately because a title is read out of context, where the bare word `Tetris` identifies nothing (DR-254). |
| 6   | Should the mark be decorative or carry a text alternative?                                                   | Daan Harms | Resolved | **Carry the alternative `Info Support`.** Because the `h1` does not contain the words, the mark is the only element carrying message 2, so it is load-bearing: a blocked image or a screen reader must still deliver it (DR-239, EC-3). Had the `h1` contained the words, decorative would be correct — the two decisions move together. |
| 7   | Does the game replace the landing page in place, or live at its own address?                                 | Daan Harms | Resolved | **Its own address** (DR-245). The landing address stays shareable and bookmarkable — the same reasoning as 010's **DR-194** — Back from a game returns here rather than leaving the product, and the landing page never has to represent a game in progress. *How* the transition happens is deferred to an ADR in arc42 §09. |
| 8   | Should the controls / how-to-play panel be shown inline on first visit?                                      | Daan Harms | Resolved | **No.** It is a ranked-third entry point, never auto-opened (DR-244). Tetris is a game most colleagues already know; an explainer in the way taxes the fortieth visit to serve the first, and "shown on *first* visit" would require remembering the visit, which DR-253 forbids. 014 owns the content. |
| 9   | How does the page avoid forcing returning colleagues past an explainer?                                      | Daan Harms | Resolved | **By making the explainer one line of static text that never needs dismissing** (DR-252). No modal, no tour, no "new here?". The eye crosses one line without effort; nothing has to be clicked away, so nothing has to be remembered (DR-253). Identical page, identical keystroke, first visit and fortieth. |
| 10  | Should there be a cookie banner, or any analytics?                                                           | Daan Harms | Resolved | **Neither.** Nothing is stored and nothing is tracked (DR-253), continuing 009's **Minimal Personal Data (DR-131)** and 010's **Reading Never Writes (DR-195)**, so there is nothing to consent to. A banner would be the page's first interaction and would contradict the one-action promise before the visitor reached it. |
| 11  | Which language — Dutch, English, or both?                                                                    | Daan Harms | Resolved | **English only** (DR-237). Every other screen, every spec and the entire domain vocabulary of 001–014 is English; Info Support works in both, and a half-translated interface is worse than a consistently English one. Revisiting means translating 008–014, not just this page. |
| 12  | Should the page show a screenshot, an animated board, or a "games played" figure?                            | Daan Harms | Resolved | **None of them.** A screenshot tells a visitor nothing the word *Tetris* did not (DR-258); an animated board needs a reduced-motion variant for no informational gain; a counter is telemetry, which DR-253 forbids. Keeping the hero to type and colour also keeps the LCP element a text node, which is how NFR-001 is met without optimisation work. |
| 13  | Should the page have a navigation bar, footer links, or an about/privacy page?                               | Daan Harms | Resolved | **No** (DR-246, §1.3). The product is one game, one board and one front door. A nav bar listing three destinations is chrome around a page whose entire job is to point at one of them, and there is no personal data to write a privacy page about beyond 009's DR-131. |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-003, SC-018, SC-019, SC-021, SC-022;
        US-002 -> SC-004, SC-005, SC-020, SC-023;
        US-003 -> SC-006..SC-010;
        US-004 -> SC-011..SC-013;
        US-005 -> SC-013..SC-015;
        US-006 -> SC-016, SC-017
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-006, FR-024, FR-026, FR-027 -> US-001; FR-007..FR-009, FR-025 -> US-002;
         FR-010..FR-014 -> US-003; FR-015..FR-017 -> US-004; FR-018..FR-020 -> US-005;
         FR-021..FR-023 -> US-006)
  - [x] Domain model covers all entities mentioned in the requirements
        The domain is the page's composition: regions (§5.1), the LandingAction value object and the
        three actions (§5.2), the proposed copy (§5.3), the message order and hierarchy (§5.4) and the
        exhaustive list of 013 tokens consumed (§5.5). The page persists and reads nothing (DR-253).
  - [x] Domain rules and invariants are listed (DR-229 .. DR-258 — this spec's allocated block;
        DR-01..DR-228 belong to specs 001-011 and 013 and are cited, never redefined)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-16)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-011 — each is a time
        budget, a byte budget, a count, a layout-shift figure or a stated pass rate, all against the
        measurement baseline declared at the head of §6)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero remain; the one open brand question
        is 013's Open Question 1 and is tracked there, not duplicated here)
  - [x] Open questions are assigned and have a resolution path (13 recorded, all 13 resolved)
  - [x] No colour, type size, spacing or other design value is stated here — every one is a 013 token
        name, listed exhaustively in §5.5 (DR-241)
  - [x] Proposed copy is marked as proposed and open to wording changes (§5.0, §5.3)
-->

