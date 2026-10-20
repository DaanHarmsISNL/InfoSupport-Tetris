# Feature Specification: Info Support Brand Styling — Design Tokens, Colour, Typography and Motion

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
| Feature ID      | 013                                                                                           |
| Status          | Draft                                                                                         |
| Author          | Daan Harms                                                                                    |
| Created         | 2026-10-20                                                                                    |
| Last updated    | 2026-10-20                                                                                    |
| Epic / Parent   | Look like Info Support (story map activity) — `docs/product/story-map.md`                     |
| Arc42 reference | Sections 2, 5, 8, 9, 10, 12 — see §9.3. All arc42 files are currently empty placeholders.    |

> **Note on this spec.** 013 is the foundational slice of the *Look like Info Support* activity, in the
> same way that 001 is foundational for *Play Tetris*. It owns the **design token system** — the names,
> roles and values that every screen draws from. Stories **012** (branded landing page) and **014**
> (controls / how-to-play panel) consume these tokens verbatim and must not introduce competing names
> or literal values. 013 owns the tokens; individual screens own their layout.
>
> **Relationship to 001.** **Colour Mapping (DR-24)** permits this spec to re-tune the tetromino hex
> values while preserving the one-to-one type↔colour mapping and each colour's recognisable hue.
> §5.2 exercises exactly that permission and no more: the mapping is unchanged, the seven hues are
> unchanged, only the hex values move. Every other rule in 001–009 stands untouched.
>
> **On brand values.** Info Support is a real company with a real brand, and this spec does **not**
> invent its colours or typefaces. Values in §5 are labelled either **Confirmed** (derived here and
> verifiable from this document) or **PLACEHOLDER** (standing in for an authoritative value that must
> come from Info Support's brand guidelines). The two are never blurred. See §5.1 and Open Question 1.

### 1.1 Problem Statement

The application is still wearing the default ASP.NET Core scaffold: stock Bootstrap chrome, a 14px root
font, an unbranded favicon and a hard-coded `#258cfb` focus glow in `wwwroot/css/site.css`. Nothing
about it says Info Support, and the game itself — specified across 001–009 — has no agreed surface,
text or chrome colours to render against, only the raw Tetris Guideline palette. Without an owned set
of design tokens, stories 012 and 014 would each invent their own colours and type sizes, and the board
would keep a palette that was never checked for contrast or colour-vision deficiency.

### 1.2 Goal

The whole application — landing page, game board, side panels, leaderboard and game-over overlay —
renders from a single named set of design tokens that carry Info Support's colours and typography.
A colleague opening the page recognises it as an Info Support product before reading a word. Text is
legible, the seven tetrominoes are tellable apart by anyone including players with any of the three
dichromacies, numbers do not jitter as the score climbs, a slow or blocked web font never breaks the
game, and nothing animates that a player who has asked for reduced motion did not ask for.

### 1.3 Non-Goals

The following are deliberately **not** part of this spec.

- **Component-library rewrite** — Bootstrap stays vendored in `wwwroot/lib/` and is re-skinned through
  its own custom properties, not replaced (DR-167). No migration to Tailwind, no bespoke component kit.
- **End-user theming or skinning** — there is no theme picker, no saved preference, no per-player
  palette. One theme, decided here (DR-145).
- **Logo redesign** — the Info Support logo and wordmark are used **as supplied**. This spec reserves a
  slot and a clear-space rule for them; it does not redraw, recolour or reconstruct them (§9.2).
- **A light theme** — deliberately deferred, not forgotten. See DR-145 for the reasoning and the
  structural provision that keeps a light theme an additive change.
- **Page layout and copy** — where the logo sits on the landing page, what the hero says and how the
  controls panel is arranged belong to **story 012** and **story 014**. 013 gives them tokens, not
  compositions.
- **Any change to game behaviour** — no rule in 001–009 is altered. Gravity, scoring, the piece
  sequence, the leaderboard and every timing value are untouched. This spec is presentation only.
- **The tetromino type↔colour *mapping*** — that is 001's **Colour Mapping (DR-24)** and is binding
  here. Only the hex values are re-tuned (DR-151).
- **Per-type identification on the board without colour** — the preview (004) and hold slot (005)
  already carry shape plus a text alternative per **Preview Non-Colour Cue (DR-59)**. On the board the
  requirement is different and is addressed by DR-156; see the reasoning there.
- **Mobile, touch and responsive phone layouts** — out of scope project-wide (laptop + keyboard only).
  This spec does fix a **minimum viewport** the layout must not break below (DR-166).
- **Ghost piece, pause, accounts and authentication** — out of scope project-wide (story map).
- **Print stylesheets, email templates, social preview cards** — no such surfaces exist.

## 2. User Stories

### US-001: The app looks like Info Support

**As a** colleague opening the page,
**I want** the whole application to carry Info Support's colours and typography,
**so that** it reads as something my company made rather than a default web template.

### US-002: Everything stays readable

**As a** player glancing at the score while a piece is falling,
**I want** text, numbers and interface edges to be clearly legible against their backgrounds,
**so that** I can read the game state at a glance without leaning into the screen.

### US-003: Play without relying on colour perception

**As a** player who does not distinguish all seven colours reliably,
**I want** to tell the blocks on the board apart and see where one block ends and the next begins,
**so that** I can read the stack accurately and play on equal terms.

### US-004: A steady, calm interface

**As a** player mid-game,
**I want** the layout to hold still — no font swap reflow, no jittering digits, no motion I did not
ask for,
**so that** nothing pulls my attention away from the falling piece.

### US-005: One vocabulary for every screen

**As a** developer building the landing page (012) or the controls panel (014),
**I want** a published, stable set of named tokens to style against,
**so that** every screen agrees on colour, type and spacing without my re-deciding it per page.

## 3. Functional Requirements

_All requirements are stated at the behaviour level. Named domain rules (DR-nn) are defined in §5.6
and occupy the block **DR-139 – DR-168**. Rules DR-01 – DR-138 belong to specs 001–009 and are cited,
never redefined._

| ID     | Requirement                                                                                                                                                                                                 | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ---------- |
| FR-001 | The system shall express every colour, type, spacing, radius, border and motion value as a named **design token**, organised in the two tiers defined in §5.1 (DR-139).                                        | Must     | US-001     |
| FR-002 | The system shall define all brand primitive values in exactly one file, so that replacing placeholder values with authoritative Info Support values is a single-file change (DR-140).                          | Must     | US-001     |
| FR-003 | The system shall style every surface of the application — landing page, game board, side panels, leaderboard and game-over overlay — from the semantic tokens in §5.1, with no surface left on scaffold defaults. | Must     | US-001     |
| FR-004 | The system shall use no literal colour, font-size, spacing or radius value anywhere outside the primitive tier; all consuming styles reference semantic tokens (DR-142).                                       | Must     | US-001     |
| FR-005 | The system shall present a single dark theme, declaring its colour scheme so that browser-rendered controls, scrollbars and form widgets match it (DR-145).                                                    | Must     | US-001     |
| FR-006 | The system shall re-skin the vendored Bootstrap by mapping its own custom properties onto the semantic tokens, without editing any file under `wwwroot/lib/` (DR-167).                                         | Must     | US-001     |
| FR-007 | The system shall render all body-sized text at a contrast ratio of at least **4.5:1** against the surface behind it (DR-146).                                                                                  | Must     | US-002     |
| FR-008 | The system shall render all large text — at least 24px, or at least 18.66px when bold — at a contrast ratio of at least **3:1** against the surface behind it (DR-147).                                        | Must     | US-002     |
| FR-009 | The system shall render every non-text indicator that carries meaning — cell outlines, panel borders, the board frame, focus rings, icons — at a contrast ratio of at least **3:1** against its neighbour (DR-148). | Must     | US-002     |
| FR-010 | The system shall use `--text-on-brand` as the only text colour placed on `--brand-primary`, and shall verify that pairing at **≥ 4.5:1** whenever either value changes (DR-149).                               | Must     | US-002     |
| FR-011 | The system shall show a visible keyboard focus indicator on every interactive element, at **≥ 3:1** against both the element and the surface behind it, and shall never remove focus styling without replacing it (DR-150). | Must     | US-002     |
| FR-012 | The system shall render no interface text below **14px**, the smallest size in the type scale (DR-164).                                                                                                       | Must     | US-002     |
| FR-013 | The system shall fix the root font size at **16px** regardless of viewport width, replacing the scaffold's 14px-below-768px rule, so that every rem-based token has one meaning (DR-143).                      | Must     | US-002     |
| FR-014 | The system shall assign each `TetrominoType` the re-tuned colour given in §5.2, preserving the one-to-one mapping and the named hue required by **Colour Mapping (DR-24)** (DR-151, DR-152).                   | Must     | US-003     |
| FR-015 | The system shall render each tetromino fill at a contrast ratio of at least **3:1** against `--cell-empty` (DR-153). This satisfies 001's NFR-003 and supersedes nothing else in it.                           | Must     | US-003     |
| FR-016 | The system shall keep all 21 pairs of tetromino fills separated by at least **ΔE00 10** under simulated deuteranopia, protanopia and tritanopia as well as under normal vision (DR-154).                       | Must     | US-003     |
| FR-017 | The system shall spread the seven tetromino fills in relative luminance so that every pair differs by a contrast ratio of at least **1.20:1**, giving a greyscale difference independent of hue (DR-155).      | Must     | US-003     |
| FR-018 | The system shall draw every occupied cell — on the board, in the preview and in the hold slot — with the **block edge** treatment of DR-156, so that cell boundaries are visible without relying on fill colour. | Must     | US-003     |
| FR-019 | The system shall use no brand colour inside the playfield and no tetromino colour in the surrounding chrome, separating the two regions with the board frame (DR-157).                                         | Must     | US-003     |
| FR-020 | The system shall remain fully operable under forced-colours mode and shall honour a request for increased contrast, with cell occupancy still conveyed by the DR-156 block edge (DR-168).                      | Should   | US-003     |
| FR-021 | The system shall size and weight all text from the five type roles and the fixed type scale in §5.3, with no ad-hoc sizes (DR-158, DR-159).                                                                    | Must     | US-004     |
| FR-022 | The system shall render every in-game numeric readout — score, level, lines, countdowns, leaderboard scores and ranks — with **tabular figures**, so a digit change never alters the width of the number (DR-160). | Must     | US-004     |
| FR-023 | The system shall load its web fonts without causing any layout shift after first paint, by self-hosting, preloading, and declaring a metric-matched fallback (DR-161).                                         | Must     | US-004     |
| FR-024 | The system shall remain completely usable, correctly laid out and on-scale when the web font fails to load, is blocked, or arrives late (DR-162).                                                              | Must     | US-004     |
| FR-025 | The system shall serve all fonts from its own origin and shall request no font from a third-party host (DR-163).                                                                                               | Must     | US-004     |
| FR-026 | The system shall honour a reduced-motion preference by suppressing decorative motion and by applying the final state of any information-bearing animation immediately, while never suppressing the falling piece itself (DR-165). | Must     | US-004     |
| FR-027 | The system shall lay out the game and all other screens without clipping or horizontal scrolling at or above a viewport of **1024 × 640 CSS pixels**, scaling the board through `--cell-size` alone (DR-166).  | Must     | US-004     |
| FR-028 | The system shall take every margin, padding and gap from the 4px spacing scale in §5.4 (DR-144).                                                                                                               | Should   | US-004     |
| FR-029 | The system shall treat the token names in §5 as a stable published contract; renaming or removing a token is a breaking change that requires updating stories 012 and 014 in the same change (DR-141).          | Must     | US-005     |
| FR-030 | The system shall expose the token set such that stories 012 and 014 can style their screens entirely from semantic tokens, without reading primitives and without adding new colour or type values.             | Must     | US-005     |

## 4. Acceptance Scenarios

_IDs in this section are `SC-nnn` per the template. Success criteria in §8 use `SUC-nnn` to avoid a clash._

### SC-001: Every surface draws from the token set (FR-003, FR-004)

```gherkin
Given the application is running
When the landing page, the game board, the side panels, the leaderboard and the game-over overlay are inspected
Then every background, text, border and accent colour resolves to a semantic token from §5.1
  And no literal colour value appears outside the brand primitive file
```

### SC-002: Swapping the brand values touches one file (FR-002)

```gherkin
Given the brand primitive values are replaced with a different set
When only the brand primitive file has been edited
Then every screen picks up the new brand colours
  And no other stylesheet, page or component required a change
```

### SC-003: The scaffold's hard-coded styling is gone (FR-004, FR-011, FR-013)

```gherkin
Given the application is running
When the stylesheets are inspected
Then the scaffold's 14px root font rule has been replaced by a constant 16px root
  And the scaffold's hard-coded focus glow has been replaced by the focus ring token
  And no vendored file under the client library folder has been modified
```

### SC-004: Body text meets the contrast floor (FR-007)

```gherkin
Given any screen of the application
When each piece of body-sized text is measured against the surface behind it
Then every measurement is at least 4.5:1
```

### SC-005: Interface edges meet the non-text floor (FR-009)

```gherkin
Given any screen of the application
When each meaningful border, outline, icon and indicator is measured against its neighbouring colour
Then every measurement is at least 3:1
  And decorative-only dividers that fall below 3:1 carry no information on their own
```

### SC-006: Keyboard focus is always visible (FR-011)

```gherkin
Given a player is navigating with the keyboard only
When focus moves to any interactive element on any screen
Then a focus indicator is visible around that element
  And it measures at least 3:1 against both the element and the surface behind it
```

### SC-007: The tetromino mapping is unchanged (FR-014)

```gherkin
Given the re-tuned palette of §5.2
When each of the seven types is rendered
Then each type has exactly one colour and each colour belongs to exactly one type
  And I reads as cyan, O as yellow, T as purple, S as green, Z as red, J as blue and L as orange
  And nothing in specs 001 to 009 other than the hex values of DR-24 has changed
```

### SC-008: Every piece stands out from an empty cell (FR-015)

```gherkin
Given the playfield is rendered
When each of the seven fills is measured against the empty cell colour
Then every measurement is at least 3:1
```

### SC-009: The seven pieces stay distinct under colour-vision deficiency (FR-016)

```gherkin
Given the seven tetromino fills
When all 21 pairs are compared under simulated deuteranopia, protanopia and tritanopia
Then every pair differs by at least ΔE00 10 in each of the three simulations
  And every pair differs by at least ΔE00 10 under normal vision
```

### SC-010: The pieces separate in greyscale too (FR-017)

```gherkin
Given the seven tetromino fills
When all 21 pairs are compared by relative luminance alone
Then every pair differs by a contrast ratio of at least 1.20:1
```

### SC-011: Block boundaries are visible without colour (FR-018)

```gherkin
Given a stack in which two vertically adjacent blocks are of different types
When the board is viewed with all colour removed
Then the boundary between the two blocks is still visible
  And the boundary between an occupied cell and an empty cell is still visible
```

### SC-012: Brand colour and piece colour never touch (FR-019)

```gherkin
Given a game is in progress with pieces locked into the stack
When the playfield and the surrounding chrome are inspected
Then no brand colour appears inside the playfield
  And no tetromino colour appears in the chrome
  And the board frame separates the two regions at a contrast of at least 3:1
```

### SC-013: Type comes only from the scale (FR-021, FR-012)

```gherkin
Given any screen of the application
When every text element's computed font size is collected
Then each size is one of the sizes in the type scale of §5.3
  And no size is below 14px
```

### SC-014: Digits do not jitter (FR-022)

```gherkin
Given the score readout is showing 1111
When the score changes to 8888
Then the readout occupies exactly the same width as before
  And no element beside or below it has moved
```

### SC-015: A climbing score does not reflow the panel (FR-022)

```gherkin
Given the score readout has a fixed field width
When the score climbs from 0 through 999 to 10000
Then the panel's other contents stay in place
  And only the digits themselves change
```

### SC-016: The web font arrives without shifting anything (FR-023)

```gherkin
Given a player loads the page on a cold cache
When the web font finishes loading after first paint
Then no text reflows, resizes or repositions
  And the page's cumulative layout shift from font loading is zero
```

### SC-017: A blocked font does not break the game (FR-024, FR-025)

```gherkin
Given the web font files cannot be fetched
When the player loads the page and starts a game
Then every screen renders in the fallback stack at the same sizes and in the same positions
  And the game is fully playable
  And no request was made to any third-party host
```

### SC-018: Reduced motion keeps the information (FR-026)

```gherkin
Given a player whose system requests reduced motion
When a line is cleared, the level increases and the game ends
Then each of those events is still clearly conveyed
  And the conveying state is applied immediately rather than animated
  And the falling piece still descends normally under gravity
```

### SC-019: Reduced motion does not stop the game (FR-026)

```gherkin
Given a player whose system requests reduced motion
When a game session is in progress
Then pieces spawn, fall, move and lock exactly as specified in 001
  And no gameplay timing is altered
```

### SC-020: The layout holds at the minimum viewport (FR-027)

```gherkin
Given a browser viewport of 1024 by 640 CSS pixels
When the player opens the landing page, plays a game and views the leaderboard
Then no content is clipped
  And the game area needs no horizontal scrolling
  And the whole 20-row playfield is visible without vertical scrolling
```

### SC-021: The board scales from one token (FR-027)

```gherkin
Given the cell size token is changed within its supported range
When the board is re-rendered
Then the playfield, the preview and the hold slot all rescale proportionally
  And no other token or style needed changing
```

### SC-022: Stories 012 and 014 need no new values (FR-029, FR-030)

```gherkin
Given the token set published in §5
When the branded landing page and the controls panel are built
Then both are styled entirely from semantic tokens
  And neither introduces a new colour, type size or spacing value
  And neither reads a primitive token directly
```

## 5. Domain Model — The Token System

_This spec's domain is presentation. Its "entities" are **design tokens**: named, typed, constrained
values that every screen resolves its appearance from. These names are the normative vocabulary for
stories **012** and **014**, exactly as 001's §5 is normative for 002–008. Reuse them verbatim._

### 5.0 How to read these tables

**Two tiers (DR-139).** Tokens come in two layers and only one of them is public:

| Tier          | Naming             | Who may use it                                      | What it is                                          |
| ------------- | ------------------ | --------------------------------------------------- | ---------------------------------------------------- |
| **Primitive** | `--is-*`           | The token definitions only. **Never** a feature.    | A raw value with no meaning attached.                |
| **Semantic**  | unprefixed, by role | Every screen, component and feature spec.           | A role — what the value is *for*, not what it *is*.  |

The `--is-` prefix marks primitives as Info Support-internal raw material and makes an accidental
direct use obvious in review. Semantic tokens are deliberately unprefixed because they are the public
API that 012 and 014 write against, and because Bootstrap occupies the separate `--bs-` namespace, so
there is no collision (DR-167).

**Provenance.** Every value carries one of two labels, and they are never mixed:

| Label             | Meaning                                                                                                              |
| ----------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Confirmed**     | Decided by this spec and verifiable from it. Every contrast ratio, ΔE00 and luminance figure below was computed against the stated formulas and is reproducible. |
| **PLACEHOLDER**   | A stand-in for a value that must come from Info Support's brand guidelines. Structurally correct, contrast-checked and safe to build against — but **not** Info Support's actual brand value and must not be presented as such. |

> **The brand values below are PLACEHOLDER.** Searches of `infosupport.com` and `infosupport.nl` did
> not surface a published brand guideline, press kit or design-system page carrying authoritative hex
> values or typeface names, and the page's stylesheets were not retrievable. Rather than invent values
> and present them as Info Support's, this spec fixes the **token structure** — which is the part 012
> and 014 actually depend on — and leaves the brand primitives as clearly-labelled placeholders behind
> the single-file boundary of DR-140. Swapping them in later is a one-file change that alters no token
> name, no rule and no screen.
>
> [NEEDS CLARIFICATION: What are Info Support's authoritative brand colours (primary, hover/active,
> deep and accent) and brand typefaces, per the official brand guidelines? Needed before release, not
> before implementation — see Open Question 1.]

**Measurement conventions.** Contrast ratios are WCAG 2.1 relative-luminance ratios. Colour difference
is **ΔE00** (CIEDE2000) computed in CIELAB from sRGB. Colour-vision deficiency is simulated with the
Machado et al. (2009) severity-1.0 matrices for deuteranopia, protanopia and tritanopia, applied in
linear RGB. All three are deterministic and automatable, which is what makes §6's NFRs testable.

### 5.1 Colour tokens

#### Primitives — brand (PLACEHOLDER)

| Primitive          | Value     | Provenance  | Notes                                                        |
| ------------------ | --------- | ----------- | ------------------------------------------------------------ |
| `--is-brand-400`   | `#3A6FE8` | PLACEHOLDER | Lighter step, for hover.                                     |
| `--is-brand-500`   | `#2E5FD9` | PLACEHOLDER | The principal brand colour.                                  |
| `--is-brand-600`   | `#2450BC` | PLACEHOLDER | Darker step, for pressed state.                              |
| `--is-brand-900`   | `#16213D` | PLACEHOLDER | Deep brand tint, for the landing page hero band (story 012). |
| `--is-accent-400`  | `#E8A33D` | PLACEHOLDER | Sparing emphasis — e.g. story 011's highlighted row.         |

#### Primitives — neutrals, signals and board (Confirmed)

These are this spec's own values, not brand values. They are chosen to satisfy §5.6's contrast rules
and are not awaiting anything.

| Primitive           | Value     | Provenance | Notes                                                 |
| ------------------- | --------- | ---------- | ------------------------------------------------------ |
| `--is-neutral-1000` | `#0B0D12` | Confirmed  | Deepest well. Relative luminance 0.0040.               |
| `--is-neutral-950`  | `#15181F` | Confirmed  | Page background.                                       |
| `--is-neutral-900`  | `#1C2029` | Confirmed  | Raised panel.                                          |
| `--is-neutral-800`  | `#2B313C` | Confirmed  | Hairline divider. 1.36:1 on `--surface` — decorative only. |
| `--is-neutral-500`  | `#6B7689` | Confirmed  | Strong border. 3.87:1 on `--surface`, 3.55:1 on `--surface-raised`. |
| `--is-neutral-200`  | `#B9C2D0` | Confirmed  | Secondary text. 9.89:1 on `--surface`.                 |
| `--is-neutral-50`   | `#F2F4F8` | Confirmed  | Primary text. 16.13:1 on `--surface`.                  |
| `--is-white`        | `#FFFFFF` | Confirmed  | Focus ring and text on brand.                          |
| `--is-red-400`      | `#FF7A7A` | Confirmed  | 7.03:1 on `--surface`.                                 |
| `--is-green-400`    | `#5BD88A` | Confirmed  | 9.84:1 on `--surface`.                                 |
| `--is-amber-400`    | `#F2B23C` | Confirmed  | 9.48:1 on `--surface`.                                 |
| `--is-edge-dark`    | `#05070B` | Confirmed  | Block outer border (DR-156).                           |
| `--is-edge-light`   | `#E8ECF4` | Confirmed  | Block inner bevel (DR-156). 17.03:1 against `--is-edge-dark`. |
| `--is-grid-line`    | `#262C36` | Confirmed  | Empty-cell grid. 1.38:1 on `--cell-empty` — decorative only. |

#### Semantic colour tokens

**This table is the contract.** Stories 012 and 014 use these names and only these names.

| Token                    | Resolves to         | Provenance  | Role                                                      | Verified                                      |
| ------------------------ | ------------------- | ----------- | ---------------------------------------------------------- | ---------------------------------------------- |
| `--surface`              | `--is-neutral-950`  | Confirmed   | Default page background.                                   | —                                              |
| `--surface-raised`       | `--is-neutral-900`  | Confirmed   | Panels: score, next-piece, hold, controls, leaderboard.    | 1.09:1 on `--surface` — a shape cue, not a contrast cue; panels also carry `--border-subtle`. |
| `--surface-sunken`       | `--is-neutral-1000` | Confirmed   | The board well and any inset field.                        | —                                              |
| `--surface-overlay`      | `#05070B` at 82%    | Confirmed   | Game-over scrim (story 008).                               | `--text-primary` on the composited result: 17.98:1. |
| `--border-subtle`        | `--is-neutral-800`  | Confirmed   | Decorative dividers and panel edges. **Never the sole carrier of meaning.** | 1.36:1 — intentionally below 3:1, hence the restriction. |
| `--border-strong`        | `--is-neutral-500`  | Confirmed   | Meaningful boundaries: board frame, input outlines, separators that carry structure. | 3.87:1 on `--surface`, 3.55:1 on `--surface-raised`, 4.24:1 on `--cell-empty`. |
| `--text-primary`         | `--is-neutral-50`   | Confirmed   | Body copy, headings, score digits.                         | 16.13:1 on `--surface`, 14.81:1 on `--surface-raised`. |
| `--text-secondary`       | `--is-neutral-200`  | Confirmed   | De-emphasised labels — still full body contrast.           | 9.89:1 on `--surface`, 9.08:1 on `--surface-raised`. |
| `--text-on-brand`        | `--is-white`        | Confirmed   | The **only** text colour permitted on `--brand-primary` (DR-149). | 5.59:1 on the placeholder `--brand-primary`.  |
| `--brand-primary`        | `--is-brand-500`    | PLACEHOLDER | Primary actions, key accents, hero.                        | 3.18:1 on `--surface` — adequate as a large block, not as text. |
| `--brand-primary-hover`  | `--is-brand-400`    | PLACEHOLDER | Hover state.                                               | `--text-on-brand` at 4.55:1.                   |
| `--brand-primary-active` | `--is-brand-600`    | PLACEHOLDER | Pressed state.                                             | `--text-on-brand` at 7.12:1.                   |
| `--brand-deep`           | `--is-brand-900`    | PLACEHOLDER | Landing page hero band (story 012).                        | `--text-primary` at 14.46:1.                   |
| `--brand-accent`         | `--is-accent-400`   | PLACEHOLDER | Sparing emphasis; story 011's highlighted row.             | 8.24:1 on `--surface`.                         |
| `--status-danger`        | `--is-red-400`      | Confirmed   | Validation errors (story 009), top-out warning.            | 7.03:1 on `--surface`.                         |
| `--status-success`       | `--is-green-400`    | Confirmed   | Score submitted (story 009).                               | 9.84:1 on `--surface`.                         |
| `--status-warning`       | `--is-amber-400`    | Confirmed   | Cautions.                                                  | 9.48:1 on `--surface`.                         |
| `--focus-ring`           | `--is-white`        | Confirmed   | Keyboard focus indicator (DR-150).                         | 17.76:1 on `--surface`, 16.30:1 on `--surface-raised`. |

> **No `--text-muted`.** Deliberately absent. A third, dimmer text colour is the single most common way
> a design system drifts below 4.5:1, and nothing in 001–009 needs one. `--text-secondary` is the
> dimmest text token and it clears the body-text floor with margin.

#### Semantic colour tokens — the board

| Token             | Resolves to         | Provenance | Role                                                                 |
| ----------------- | ------------------- | ---------- | --------------------------------------------------------------------- |
| `--board-well`    | `--surface-sunken`  | Confirmed  | The playfield background behind the cells.                            |
| `--board-frame`   | `--border-strong`   | Confirmed  | The frame separating the playfield from the chrome (DR-157).          |
| `--cell-empty`    | `--is-neutral-1000` | Confirmed  | An unoccupied cell. The reference background for DR-153.              |
| `--cell-grid-line`| `--is-grid-line`    | Confirmed  | Optional grid on empty cells. Decorative; carries no information.     |
| `--cell-edge-dark`| `--is-edge-dark`    | Confirmed  | Outer border of every occupied cell (DR-156).                         |
| `--cell-edge-light`| `--is-edge-light`  | Confirmed  | Inner bevel of every occupied cell (DR-156).                          |
| `--piece-i`       | `#1FF0E6`           | Confirmed  | See §5.2.                                                             |
| `--piece-o`       | `#F7F32B`           | Confirmed  | See §5.2.                                                             |
| `--piece-t`       | `#B470E8`           | Confirmed  | See §5.2.                                                             |
| `--piece-s`       | `#4FC57A`           | Confirmed  | See §5.2.                                                             |
| `--piece-z`       | `#DD313C`           | Confirmed  | See §5.2.                                                             |
| `--piece-j`       | `#4B5ADD`           | Confirmed  | See §5.2.                                                             |
| `--piece-l`       | `#F97C12`           | Confirmed  | See §5.2.                                                             |

### 5.2 The re-tuned tetromino palette

**Colour Mapping (DR-24)** permits a re-tune provided the one-to-one mapping and each colour's
recognisable hue survive. Both do. The Guideline's values are pure saturated sRGB primaries chosen for
CRT-era hardware, not for a dark panel or for colour-vision deficiency: `J #0000FF` sits at relative
luminance 0.072 and `T #800080` at 0.061, so both nearly vanish against a dark board, and `S #00FF00`
against `Z #FF0000` is the textbook red/green collapse. The values below keep every hue and fix that.

| Type | Token       | Guideline (001) | **013 value** | Relative luminance | vs `--cell-empty` | Hue reads as |
| ---- | ----------- | --------------- | ------------- | ------------------ | ----------------- | ------------- |
| I    | `--piece-i` | `#00FFFF`       | **`#1FF0E6`** | 0.6832             | 13.57:1           | cyan          |
| O    | `--piece-o` | `#FFFF00`       | **`#F7F32B`** | 0.8405             | 16.48:1           | yellow        |
| T    | `--piece-t` | `#800080`       | **`#B470E8`** | 0.2712             | 5.94:1            | purple        |
| S    | `--piece-s` | `#00FF00`       | **`#4FC57A`** | 0.4300             | 8.88:1            | green         |
| Z    | `--piece-z` | `#FF0000`       | **`#DD313C`** | 0.1790             | 4.24:1            | red           |
| J    | `--piece-j` | `#0000FF`       | **`#4B5ADD`** | 0.1403             | 3.52:1            | blue          |
| L    | `--piece-l` | `#FF7F00`       | **`#F97C12`** | 0.3460             | 7.33:1            | orange        |

**Verified properties** (all Confirmed, all reproducible, all enforced as NFRs in §6):

| Property                                       | Requirement | Measured | Binding pair          |
| ---------------------------------------------- | ----------- | -------- | ---------------------- |
| Fill vs `--cell-empty` contrast                | ≥ 3:1       | **3.52:1** | J                     |
| Pairwise luminance ratio (greyscale separation) | ≥ 1.20:1   | **1.20:1** | Z / J                 |
| Pairwise ΔE00, normal vision                   | ≥ 10        | **19.8**  | T / J                 |
| Pairwise ΔE00, deuteranopia                    | ≥ 10        | **14.9**  | S / L                 |
| Pairwise ΔE00, protanopia                      | ≥ 10        | **10.0**  | T / J                 |
| Pairwise ΔE00, tritanopia                      | ≥ 10        | **11.7**  | Z / L                 |

> **Why ΔE00 10 and not higher.** Seven hues that must each stay recognisably cyan, yellow, purple,
> green, red, blue and orange cannot be pushed much further apart without one of them ceasing to read
> as its name — which DR-24 forbids. The binding constraint is **T purple against J blue under
> protanopia**, where purple loses its red component and converges on blue; the only remaining
> separator is lightness, and the two are already a 1.69:1 luminance ratio apart. A palette reaching
> ΔE00 11.1 was evaluated and rejected because it turned J into a pale sky blue. **This is precisely
> why DR-156 exists**: colour is never the sole cue on the board.

#### The non-colour cue (DR-156)

Three options were weighed: a per-type pattern or glyph, a border, and an inner bevel.

- **Per-type pattern — rejected.** Seven distinguishable textures inside a ~28px square is visual
  noise at exactly the moment the player needs to read the stack quickly, and small-scale patterns are
  themselves hard to tell apart. It also solves the wrong problem: once a piece has locked, its *type*
  stops mattering. Nothing in 001–009 asks the player to identify a locked block's type.
- **What the board actually needs** is structural: *is this cell occupied*, and *where does one block
  end and the next begin*. That is a boundary problem, not an identification problem. Per-type
  identification is already covered where it is genuinely needed — the preview and the hold slot carry
  shape plus a text alternative under **Preview Non-Colour Cue (DR-59)**.
- **A fill-derived bevel — evaluated and rejected.** Tinting each block's edge from its own fill was
  measured and fails: a dark edge derived from one fill can land at **1.01:1** against a neighbouring
  fill, so the boundary between two different-type blocks disappears. Derived edges are not safe.
- **Chosen: a fixed two-tone edge.** Every occupied cell gets a 1px outer border in
  `--cell-edge-dark` and a 1px inner bevel in `--cell-edge-light`. Because both tones are **fixed and
  fill-independent**, the edge contrasts with itself at a constant **17.03:1** no matter which two
  types are adjacent, and every block carries a visible edge whatever its fill:

| Fill | vs `--cell-edge-dark` | vs `--cell-edge-light` | Best |
| ---- | --------------------- | ---------------------- | ---- |
| I    | 14.08:1               | 1.21:1                 | 14.08:1 |
| O    | 17.10:1               | 1.00:1                 | 17.10:1 |
| T    | 6.17:1                | 2.76:1                 | 6.17:1  |
| S    | 9.22:1                | 1.85:1                 | 9.22:1  |
| Z    | 4.40:1                | 3.87:1                 | 4.40:1  |
| J    | 3.65:1                | 4.66:1                 | 4.66:1  |
| L    | 7.60:1                | 2.24:1                 | 7.60:1  |

  Every fill clears 3:1 against at least one tone, and `--cell-edge-light` is 16.41:1 against
  `--cell-empty`, so an isolated block reads against the empty board. The cost is that a 1px near-white
  bevel very slightly lightens each block's perceived colour; at the rendered cell size this is
  negligible and it is a deliberate trade for a guarantee that holds under every colour-vision
  condition, in greyscale, and in forced-colours mode.

#### Keeping the board and the brand apart (DR-157)

The brand palette is not yet known, so this spec cannot assert that it will not collide with a piece
colour. It makes the collision harmless instead, structurally: **no brand colour is used inside the
playfield, and no tetromino colour is used in the chrome.** The `--board-frame` separates the two
regions at 3.87:1 against `--surface` and 4.24:1 against `--cell-empty`, so a brand colour and a piece
colour never share an edge and are never compared side by side.

The risk is real and worth demonstrating rather than asserting away. The placeholder `--brand-primary`
`#2E5FD9` sits at **ΔE00 4.2** from `--piece-j` `#4B5ADD` — well inside the range where two colours
read as the same blue. Under the quarantine this does not matter, because the two never meet. Where a
brand colour and a piece colour *would* appear within the same field of view — the next-piece panel
sits beside a branded header — DR-157 additionally requires a separation of **ΔE00 ≥ 20**, met by
nudging the brand token's lightness within its own hue rather than by moving the piece colour, since
the piece palette is already at its separation limit.

### 5.3 Typography tokens

#### Families

| Token                    | Value                                                                                   | Provenance  |
| ------------------------ | --------------------------------------------------------------------------------------- | ----------- |
| `--font-brand`           | The Info Support brand typeface name                                                     | PLACEHOLDER |
| `--font-fallback-sans`   | `system-ui, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`                     | Confirmed   |
| `--font-fallback-mono`   | `ui-monospace, "Cascadia Mono", "Segoe UI Mono", Consolas, "Liberation Mono", monospace` | Confirmed   |
| `--font-display`         | `var(--font-brand), var(--font-fallback-sans)`                                           | Confirmed (structure) |
| `--font-body`            | `var(--font-brand), var(--font-fallback-sans)`                                           | Confirmed (structure) |
| `--font-numeric`         | `var(--font-brand), var(--font-fallback-mono)`                                           | Confirmed (structure) |

`--font-numeric` falls back to **mono**, not sans: if the brand face turns out to lack tabular figures,
the monospace stack guarantees DR-160 unconditionally. This is the one place where the fallback is
deliberately a different classification from the primary.

#### Scale and roles

A 1.25 modular scale on a fixed 16px root (DR-143). Nine sizes, five roles, three weights — small
enough to hold in your head and to keep the font payload bounded.

| Token                   | Size            | Role     | Weight | Line height | Letter spacing | Used for                                        |
| ----------------------- | --------------- | -------- | ------ | ----------- | -------------- | ------------------------------------------------ |
| `--font-size-display`   | `3rem` (48px)   | display  | 700    | 1.05        | `-0.02em`      | Landing page hero (012).                         |
| `--font-size-h1`        | `2.5rem` (40px) | heading  | 700    | 1.15        | `-0.01em`      | Page titles, game-over headline (008).           |
| `--font-size-h2`        | `2rem` (32px)   | heading  | 600    | 1.2         | normal         | Section headings, leaderboard title (010).       |
| `--font-size-h3`        | `1.5rem` (24px) | heading  | 600    | 1.25        | normal         | Panel headings: Next, Hold, Controls.            |
| `--font-size-body-lg`   | `1.25rem` (20px)| body     | 400    | 1.5         | normal         | Lead paragraph (012).                            |
| `--font-size-body`      | `1rem` (16px)   | body     | 400    | 1.5         | normal         | Default copy, leaderboard names, key legends.    |
| `--font-size-label`     | `0.875rem`(14px)| label    | 600    | 1.4         | `0.04em`       | Panel labels. **Smallest permitted size** (DR-164). |
| `--font-size-numeric`   | `1.5rem` (24px) | numeric  | 600    | 1           | normal         | Level, lines, leaderboard scores.                |
| `--font-size-numeric-lg`| `2rem` (32px)   | numeric  | 600    | 1           | normal         | Live score, final score (008).                   |

| Token                     | Value |
| ------------------------- | ----- |
| `--font-weight-regular`   | 400   |
| `--font-weight-semibold`  | 600   |
| `--font-weight-bold`      | 700   |

Three upright weights, no italics, Latin subset only. `--font-size-h3` (24px) and both numeric sizes
qualify as **large text** and so fall under the 3:1 floor of DR-147; everything at or below 20px, and
anything at 18.66px that is not bold, falls under the 4.5:1 floor of DR-146.

#### Numerics (DR-160)

Every numeric readout — live score, level, lines, final score, leaderboard scores and ranks — renders
with **tabular figures** (`font-variant-numeric: tabular-nums`, with `font-feature-settings: "tnum"`
as the fallback switch). The testable property is width equality, not a declaration: the rendered
width of `0000000000` equals that of `1111111111` equals that of `8888888888`, so a score climbing
from 999 to 1000 moves nothing beside it. Score fields additionally reserve a fixed character width so
that gaining a digit does not reflow the panel.

#### Font loading (DR-161, DR-162, DR-163)

| Decision                       | Choice                                                                 | Why                                                                                                      |
| ------------------------------ | ---------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Hosting                        | Self-hosted WOFF2 under `wwwroot/fonts/`, same origin                  | No third-party request, no IP disclosure to a font CDN. Continues 001's NFR-007 posture; also the safer position for a Dutch employer under GDPR. |
| Discovery                      | `<link rel="preload" as="font" type="font/woff2" crossorigin>`         | Starts the fetch in the first round trip instead of after CSS parse.                                       |
| Swap behaviour                 | `font-display: optional`                                               | **The decisive choice.** `swap` and `fallback` both permit a re-render after first paint, which reflows text *during play*. `optional` gives the font a short block period and then declines to swap for that page load — a guaranteed zero post-paint shift. |
| Fallback metrics               | `size-adjust`, `ascent-override`, `descent-override`, `line-gap-override` on a local fallback `@font-face` | Matches the fallback's metrics to the brand face so the two occupy identical space. Values are **PLACEHOLDER** until the real face is known — they are computed from it, not guessed. |
| Payload                        | 3 weights, upright, Latin subset                                       | Keeps the block period short enough that `optional` usually succeeds.                                      |

The consequence of `optional` is accepted deliberately: on a cold, slow connection the first page load
may render entirely in the fallback stack, and the brand face appears on the next visit. A game that
reflows under the player's eyes is worse than a game that is briefly off-face for one load.

### 5.4 Spacing, geometry and elevation tokens

#### Spacing (DR-144)

A 4px base. Every margin, padding and gap comes from this scale — no arbitrary values.

| Token | Value | px | | Token | Value | px |
| ----- | ----- | -- |-| ----- | ----- | -- |
| `--space-0` | `0` | 0 | | `--space-5` | `1.25rem` | 20 |
| `--space-1` | `0.25rem` | 4 | | `--space-6` | `1.5rem` | 24 |
| `--space-2` | `0.5rem` | 8 | | `--space-8` | `2rem` | 32 |
| `--space-3` | `0.75rem` | 12 | | `--space-10` | `2.5rem` | 40 |
| `--space-4` | `1rem` | 16 | | `--space-12` | `3rem` | 48 |

#### Radius and borders

| Token                      | Value   | Used for                                              |
| -------------------------- | ------- | ------------------------------------------------------ |
| `--radius-sm`              | `2px`   | Playfield cells and preview cells.                     |
| `--radius-md`              | `6px`   | Panels, buttons, inputs.                               |
| `--radius-lg`              | `12px`  | Hero band, game-over dialog.                           |
| `--radius-pill`            | `999px` | Badges, the story-011 "your run" marker.               |
| `--border-width-hairline`  | `1px`   | Dividers, cell edges.                                  |
| `--border-width-standard`  | `2px`   | Inputs, buttons, focus ring.                           |
| `--border-width-frame`     | `3px`   | The board frame (DR-157).                              |
| `--focus-ring-width`       | `2px`   | DR-150.                                                |
| `--focus-ring-offset`      | `2px`   | Keeps the ring clear of the element's own border.      |

#### Elevation

| Token              | Value                                | Note                                                                  |
| ------------------ | ------------------------------------ | ---------------------------------------------------------------------- |
| `--shadow-raised`  | `0 1px 2px rgba(0,0,0,0.4)`          | Subtle; see below.                                                     |
| `--shadow-overlay` | `0 16px 48px rgba(0,0,0,0.6)`        | Game-over dialog.                                                      |

On a dark surface a shadow is nearly invisible — `--surface-raised` is only 1.09:1 above `--surface`.
Elevation is therefore conveyed by the **surface step plus `--border-subtle`**, with the shadow as a
refinement only. No panel may rely on its shadow to be perceivable as a distinct region.

#### Board geometry

| Token                     | Value                                  | Constraints                                           |
| ------------------------- | -------------------------------------- | ------------------------------------------------------ |
| `--cell-size`             | `28px`                                 | Supported range **20px – 32px**. The single knob that scales the board (DR-166). |
| `--board-width`           | `calc(var(--cell-size) * 10)`          | 280px at the default. Column count is **DR-01**'s, not redefined here. |
| `--board-height`          | `calc(var(--cell-size) * 20)`          | 560px at the default. Visible rows only, per **DR-01**. |
| `--viewport-min-width`    | `1024px`                               | DR-166.                                                |
| `--viewport-min-height`   | `640px`                                | DR-166. At 28px cells the 560px board plus chrome fits; 24px cells leave comfortable room. |

"No mobile" means laptops, not any screen. 1024 × 640 CSS pixels is the viewport left by a 1366 × 768
laptop after browser chrome — the smallest machine a colleague is plausibly playing on.

### 5.5 Motion tokens

| Token                 | Value                          | Used for                                              |
| --------------------- | ------------------------------ | ------------------------------------------------------ |
| `--duration-instant`  | `0ms`                          | The reduced-motion value of every duration below.      |
| `--duration-fast`     | `120ms`                        | Hover, focus, button press.                            |
| `--duration-base`     | `200ms`                        | Panel and overlay transitions.                         |
| `--duration-slow`     | `320ms`                        | Game-over overlay entrance.                            |
| `--easing-standard`   | `cubic-bezier(0.2, 0, 0, 1)`   | Entrances and general movement.                        |
| `--easing-exit`       | `cubic-bezier(0.4, 0, 1, 1)`   | Exits.                                                 |

> **These tokens do not govern gameplay.** Piece descent, lock delay and line-clear timing are fixed by
> **Gravity Step (DR-14)**, **Lock Delay (DR-16)** and story 006, and are game rules, not presentation.
> No reduced-motion preference and no token value may alter them (DR-165).

### 5.6 Surface mapping

Which tokens each surface of the application draws from. Layout is the owning story's business; these
are the colours and type roles it must use.

| Surface                            | Owning story | Background         | Text                               | Accents and edges                                      |
| ---------------------------------- | ------------ | ------------------ | ---------------------------------- | ------------------------------------------------------- |
| Landing page hero                  | 012          | `--brand-deep`     | `--text-primary`, display role     | `--brand-primary` call-to-action with `--text-on-brand`.|
| Landing page body                  | 012          | `--surface`        | `--text-primary` / `--text-secondary` | `--border-subtle`.                                   |
| Playfield well and cells           | 001          | `--board-well`, `--cell-empty` | —                      | `--board-frame`, `--cell-edge-dark`, `--cell-edge-light`, the seven `--piece-*`. |
| Next-piece panel, hold panel       | 004, 005     | `--surface-raised` | `--text-secondary` label role      | `--border-subtle`; piece colours inside the panel only. |
| Score / level / lines panel        | 006, 007     | `--surface-raised` | `--text-primary`, numeric roles    | `--border-subtle`.                                      |
| Controls / how-to-play panel       | 014          | `--surface-raised` | `--text-secondary` body, `--text-primary` for key caps | `--border-strong` on key caps. |
| Game-over overlay                  | 008          | `--surface-overlay` scrim over a `--surface-raised` dialog | `--text-primary`, h1 and numeric-lg | `--brand-primary` primary action, `--radius-lg`. |
| Name entry and submission          | 009          | `--surface-raised` | `--text-primary`                   | `--border-strong` input, `--status-danger` errors, `--status-success` confirmation. |
| Leaderboard table                  | 010          | `--surface`        | `--text-primary` names, numeric role for scores | `--border-subtle` row rules.               |
| Leaderboard — the player's own run | 011          | `--surface-raised` | `--text-primary`                   | `--brand-accent` marker **plus** a non-colour cue, per the same reasoning as DR-156. |
| Keyboard focus, everywhere         | 013          | —                  | —                                  | `--focus-ring` at `--focus-ring-width` / `--focus-ring-offset`. |

### 5.7 Domain Rules and Invariants

_This spec owns the block **DR-139 – DR-168**. Rules DR-01 – DR-138 belong to specs 001–009; they are
cited here, never redefined. Later specs should cite these by ID and name, e.g. "per **Block Edge
(DR-156)**"._

#### Token system

- **DR-139 — Token Tiering**: Every presentation value exists as a token in one of exactly two tiers —
  `--is-*` **primitives** (raw values, no meaning) and unprefixed **semantic** tokens (roles). Features,
  pages and components consume semantic tokens only. A primitive referenced outside the token
  definitions is a defect.
- **DR-140 — Single Brand Source**: All brand primitives are defined in exactly one file. Replacing
  placeholder brand values with authoritative Info Support values must require editing that file and
  nothing else — no token name changes, no rule changes, no screen changes.
- **DR-141 — Token Name Stability**: The semantic token names in §5.1, §5.3, §5.4 and §5.5 are a
  published contract consumed verbatim by stories 012 and 014. Adding a token is additive and safe;
  renaming or removing one is a breaking change that must update every consumer in the same change.
- **DR-142 — No Literal Values**: No literal colour, font size, spacing, radius, border width or
  duration appears outside the primitive tier. This includes the scaffold's leftovers — the 14px root
  rule and the hard-coded `#258cfb` focus glow in `wwwroot/css/site.css` are removed, not overridden.
- **DR-143 — Constant Root Font Size**: The root font size is **16px** at every viewport width. The
  scaffold's "14px below 768px, 16px above" rule is deleted. Every `rem`-based token therefore has
  exactly one pixel meaning, which is what makes DR-158 and DR-164 checkable.
- **DR-144 — Spacing Scale Exclusivity**: Every margin, padding and gap is one of the `--space-*`
  values. Optical adjustments that genuinely need an off-scale value must be justified in review and
  added to the scale rather than written inline.
- **DR-145 — Single Dark Theme**: The application ships **one** theme, dark, and declares its colour
  scheme so browser-rendered controls, scrollbars and form widgets match. One theme because the piece
  palette's contrast, ΔE00 and luminance guarantees (DR-153, DR-154, DR-155) are all measured against a
  single background, and a second theme doubles that verification matrix for a coffee-break game.
  Because every consumer uses semantic *roles* rather than raw values, adding a light theme later is an
  additive change to the token definitions alone.

#### Contrast and focus

- **DR-146 — Body Text Contrast**: All text below the large-text threshold renders at **≥ 4.5:1**
  against the surface behind it, satisfying WCAG 2.1 AA (1.4.3).
- **DR-147 — Large Text Contrast**: Text at **≥ 24px**, or **≥ 18.66px when bold**, renders at
  **≥ 3:1** against the surface behind it. `--font-size-h3`, `--font-size-numeric` and
  `--font-size-numeric-lg` qualify.
- **DR-148 — Non-Text Indicator Contrast**: Every non-text element that carries meaning — cell edges,
  the board frame, input outlines, icons, focus rings, status indicators — renders at **≥ 3:1**
  against its adjacent colour, satisfying WCAG 2.1 AA (1.4.11). `--border-subtle` is below this floor
  by design and may therefore only ever be decorative.
- **DR-149 — Text-on-Brand Pairing**: `--text-on-brand` is the only text colour placed on
  `--brand-primary`, `--brand-primary-hover`, `--brand-primary-active` or `--brand-deep`. Whenever any
  brand value changes — including the swap of Open Question 1 — the pairing is re-verified at
  **≥ 4.5:1** and the brand value adjusted if it fails. The text token is not weakened to fit the brand.
- **DR-150 — Focus Visibility**: Every interactive element shows a visible `:focus-visible` indicator
  in `--focus-ring` at `--focus-ring-width` with `--focus-ring-offset`, at **≥ 3:1** against both the
  element and the surface behind it. Focus styling is never removed without an equivalent replacement.

#### The board palette

- **DR-151 — Brand-Tuned Tetromino Palette**: The `--piece-*` values in §5.2 supersede the hex values
  given in **Colour Mapping (DR-24)**, under the permission DR-24 grants. Everything else in DR-24
  stands: each `TetrominoType` maps to exactly one colour and each colour to exactly one type.
- **DR-152 — Hue Identity**: Each tetromino colour must remain recognisably its named hue — I cyan,
  O yellow, T purple, S green, Z red, J blue, L orange. A re-tune that makes a colour read as a
  different hue violates DR-24 regardless of how well it scores on DR-154.
- **DR-153 — Cell Contrast**: Every `--piece-*` fill renders at **≥ 3:1** against `--cell-empty`.
  Measured minimum **3.52:1** (J). This is 001's NFR-003 restated against the new values.
- **DR-154 — CVD Separation**: All 21 pairs of `--piece-*` fills differ by **≥ ΔE00 10** under normal
  vision and under simulated deuteranopia, protanopia and tritanopia. Measured minimum **10.0**
  (T / J under protanopia).
- **DR-155 — Luminance Spread**: All 21 pairs of `--piece-*` fills differ by a relative-luminance
  contrast ratio of **≥ 1.20:1**, so the seven remain separable in pure greyscale independently of hue.
  Measured minimum **1.20:1** (Z / J).
- **DR-156 — Block Edge**: Every occupied cell — on the board, in the preview and in the hold slot —
  is drawn with a `--border-width-hairline` outer border in `--cell-edge-dark` and an inner bevel in
  `--cell-edge-light`. Both tones are **fixed and never derived from the fill**, so the edge contrasts
  with itself at a constant **17.03:1** regardless of which two types are adjacent. This is the board's
  non-colour cue: colour is never the only thing distinguishing one block from another or from an
  empty cell. A fill-derived edge is explicitly forbidden — it was measured at 1.01:1 in the worst case.
- **DR-157 — Board / Chrome Colour Quarantine**: No brand colour appears inside the playfield and no
  tetromino colour appears in the surrounding chrome. `--board-frame` separates the two regions at
  **≥ 3:1**. Where a brand colour and a piece colour appear in the same field of view, they must differ
  by **≥ ΔE00 20**; the brand token's lightness is nudged within its own hue to achieve this, never the
  piece colour, because the piece palette is already at its separation limit under DR-154.

#### Typography

- **DR-158 — Type Scale Exclusivity**: Every rendered font size is one of the nine `--font-size-*`
  values. No ad-hoc sizes, no inline overrides.
- **DR-159 — Type Roles**: Every text element is assigned one of the five roles — display, heading,
  body, label, numeric — and takes that role's size, weight, line height and letter spacing together.
  Roles are not mixed and matched per element.
- **DR-160 — Tabular Figures**: Every numeric readout — live score, level, lines, final score,
  leaderboard scores and ranks — renders with tabular figures. The testable property is that
  `0000000000`, `1111111111` and `8888888888` render at identical widths. Score fields additionally
  reserve a fixed character width so that gaining a digit reflows nothing.
- **DR-161 — Font Loading Without Shift**: Web fonts are self-hosted WOFF2, preloaded, declared
  `font-display: optional`, and paired with a metric-overridden fallback `@font-face`. Cumulative
  layout shift attributable to font loading is **zero**. No font may swap in after first paint.
- **DR-162 — Fallback Sufficiency**: With every web font blocked, missing or slow, the application
  remains fully laid out, fully legible and fully playable at the same sizes and positions. The web
  font is an enhancement; nothing depends on it.
- **DR-163 — First-Party Fonts Only**: All font files are served from the application's own origin. No
  request is made to any third-party font host. This continues the no-personal-data posture of 001's
  NFR-007 and avoids disclosing players' IP addresses to a third party.
- **DR-164 — Minimum Text Size**: No interface text renders below **14px** (`--font-size-label`).

#### Motion, viewport and integration

- **DR-165 — Reduced Motion**: When reduced motion is requested, all `--duration-*` tokens resolve to
  `--duration-instant` and decorative motion is suppressed. Any animation that *conveys information* —
  a line clear, a level change, the game-over overlay, story 011's run highlight — must have a static
  end state that already carries the information, and that end state is applied immediately rather than
  animated. **Gameplay is never suppressed**: piece descent, lock delay and every timing rule in
  001–009 are unaffected, because they are game rules rather than presentation.
- **DR-166 — Minimum Viewport**: Every screen lays out without clipping or horizontal scrolling of the
  game area at or above **1024 × 640 CSS pixels**, with all 20 visible rows on screen without vertical
  scrolling. The board scales through `--cell-size` alone, within its supported 20–32px range. Below
  the minimum the layout is not supported and is not designed around.
- **DR-167 — Bootstrap Bridging**: The vendored Bootstrap in `wwwroot/lib/` is **re-skinned, not
  replaced**, by mapping its `--bs-*` custom properties onto this spec's semantic tokens in a separate
  bridge stylesheet. No file under `wwwroot/lib/` is edited — those files are replaceable artefacts.
  Bootstrap is retained because story 009's name-entry form and the vendored
  jquery-validation-unobtrusive already depend on its classes, and removing it is a rewrite this spec
  lists as a Non-Goal. No Bootstrap component is used inside the playfield.
- **DR-168 — Forced Colours and Increased Contrast**: Under forced-colours mode the application remains
  operable: text, controls and focus indicators adopt the user's colours, and cell occupancy stays
  readable because **Block Edge (DR-156)** is a border treatment, which forced-colours modes preserve,
  rather than a fill difference, which they discard. A request for increased contrast is honoured by
  raising `--border-subtle` to `--border-strong` and must not reduce any ratio below its floor.

## 6. Non-Functional Requirements

_Only requirements specific to this feature. Project-wide qualities belong in arc42 §10 (currently empty).
Every ratio, ΔE00 and width check below is deterministic and automatable — that is the point._

| ID      | Category        | Requirement                                                                                                                                                                                             |
| ------- | --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Accessibility   | Every body-sized text/background pairing in the application measures **≥ 4.5:1**; every large-text pairing measures **≥ 3:1** (WCAG 2.1 AA 1.4.3). Checked automatically over the token pairings in §5.6, not sampled by eye. |
| NFR-002 | Accessibility   | Every meaningful non-text indicator measures **≥ 3:1** against its adjacent colour (WCAG 2.1 AA 1.4.11).                                                                                                 |
| NFR-003 | Accessibility   | All 21 pairs of tetromino fills measure **≥ ΔE00 10** under normal vision and under simulated deuteranopia, protanopia and tritanopia — 84 checks in total, all automated.                               |
| NFR-004 | Accessibility   | Every tetromino fill measures **≥ 3:1** against `--cell-empty`, and all 21 pairs differ by **≥ 1.20:1** in relative luminance.                                                                           |
| NFR-005 | Accessibility   | Occupied and empty cells, and the boundary between two adjacent occupied cells, remain distinguishable in a fully desaturated rendering of the board and under forced-colours mode.                      |
| NFR-006 | Accessibility   | The application is fully operable by keyboard alone, with a focus indicator visible at **≥ 3:1** at every focus stop on every screen.                                                                     |
| NFR-007 | Stability       | Cumulative layout shift attributable to font loading is **0** on a cold cache, measured at the 95th percentile over repeated loads with network throttling.                                              |
| NFR-008 | Stability       | A numeric readout's rendered width is invariant under digit changes: `0000000000`, `1111111111` and `8888888888` differ in width by **0 pixels**.                                                         |
| NFR-009 | Performance     | The styling adds no measurable cost to the game loop: with tokens applied, the board still sustains **≥ 30 fps** per 001's NFR-001, and total blocking time contributed by stylesheets and fonts is **< 50 ms**. |
| NFR-010 | Performance     | Total font payload is **≤ 120 KB** compressed across all weights, so that `font-display: optional`'s block period is usually met on an office connection.                                                |
| NFR-011 | Privacy         | Zero network requests to any third-party origin for fonts, icons or stylesheets. Continues 001's NFR-007.                                                                                                |
| NFR-012 | Maintainability | Replacing every placeholder brand value with authoritative Info Support values requires changing **one file** and **zero token names**, and leaves all §4 scenarios passing.                              |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                       | Expected Behavior                                                                                                                                                       |
| ----- | ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | The web font fails to download, or is blocked by an extension or corporate proxy.               | Every screen renders in the metric-matched fallback stack at identical sizes and positions (DR-162). The game is fully playable. No error is shown — this is not a failure the player needs to know about. |
| EC-2  | The web font arrives after first paint.                                                          | `font-display: optional` declines the swap for that page load (DR-161). Nothing reflows. The brand face appears on the next visit from cache.                            |
| EC-3  | The brand face turns out to lack tabular figures.                                                | `--font-numeric` falls through to `--font-fallback-mono`, which guarantees equal digit widths (DR-160). No readout jitters.                                              |
| EC-4  | The authoritative brand primary turns out to be very close to a tetromino colour.                | Harmless by construction: DR-157's quarantine means the two never share an edge. Where both are in one field of view, the brand token's lightness is nudged within its hue to reach ΔE00 20 — the piece colour is never moved. |
| EC-5  | The authoritative brand primary fails the 4.5:1 pairing with `--text-on-brand`.                  | The brand value is adjusted, or a darker brand step is used behind text (DR-149). The text token is never weakened to accommodate the brand.                            |
| EC-6  | A player's system requests reduced motion mid-session.                                           | Decorative transitions stop immediately; information-bearing animations collapse to their end state (DR-165). Gravity, lock delay and every 001–009 timing rule are unaffected. |
| EC-7  | A player runs the browser in forced-colours or high-contrast mode.                               | Text and controls adopt the user's colours; cell occupancy and block boundaries stay readable because the DR-156 edge is a border, which forced-colours preserves (DR-168). |
| EC-8  | The player zooms the browser to 200%.                                                            | Layout reflows from rem-based tokens without clipping. Below the effective minimum viewport the layout is unsupported (DR-166) but must not corrupt game state — 001's EC-9 governs the playfield itself. |
| EC-9  | The viewport is narrower or shorter than 1024 × 640.                                             | Unsupported (DR-166). The page degrades to scrolling rather than clipping or overlapping; no game state is affected.                                                     |
| EC-10 | A future screen needs a colour that is not in the token set.                                     | A semantic token is added to §5.1 — additive, so non-breaking (DR-141). A literal value is never introduced (DR-142).                                                    |
| EC-11 | Bootstrap is upgraded and its custom property names change.                                      | Only the bridge stylesheet needs updating; the semantic tokens and every screen are untouched (DR-167). Vendored files were never edited, so the upgrade is a clean replace. |
| EC-12 | A contributor writes a literal hex value in a page or component.                                 | Caught in review and by the automated check behind SUC-004. Treated as a defect, not a style preference.                                                                 |
| EC-13 | `--cell-size` is set outside its 20–32px supported range.                                        | Unsupported. Below 20px the DR-156 hairline edge stops being legible and the board's accessibility guarantees no longer hold; above 32px the board exceeds the minimum viewport. |
| EC-14 | The game-over overlay appears over a board full of bright pieces.                                | The `--surface-overlay` scrim composites to 17.98:1 against `--text-primary` regardless of what is beneath it, so the final score is always legible (story 008 owns the content). |
| EC-15 | A player prints the page or views it in a reader mode.                                           | Out of scope as a designed surface, but nothing breaks: text falls back to the user agent's styles and remains readable. No print stylesheet is provided (§1.3).         |

## 8. Success Criteria

_Using `SUC-nnn` to avoid colliding with the acceptance scenario IDs in §4._

| ID      | Criterion                                                                                                                                                      |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 22 acceptance scenarios in §4 pass as automated tests in CI.                                                                                                 |
| SUC-002 | All 30 domain rules in §5.7 are each covered by at least one automated test.                                                                                     |
| SUC-003 | An automated accessibility check over every screen reports **zero** contrast failures against NFR-001 and NFR-002.                                               |
| SUC-004 | An automated check reports **zero** literal colour, font-size, spacing or duration values outside the primitive tier, and **zero** direct uses of an `--is-*` primitive by a page or component. |
| SUC-005 | The 84 colour-vision checks of NFR-003 and the 21 luminance checks of NFR-004 all pass, and are re-run automatically whenever a `--piece-*` value changes.        |
| SUC-006 | A colleague shown the landing page and the game identifies it as an Info Support product without being told — informally verified with five colleagues who were not involved in building it. |
| SUC-007 | Stories 012 and 014 are built using only the semantic tokens published in §5, introducing no new colour, type size or spacing value (this is also SC-022).        |
| SUC-008 | Replacing the placeholder brand values with the authoritative Info Support values is a single-file change, after which SUC-001 and SUC-003 still pass unchanged.  |
| SUC-009 | On a cold cache over a throttled connection, font loading contributes a cumulative layout shift of 0, and the game is interactive before the font resolves.       |
| SUC-010 | A player using only the keyboard can reach and operate every control on every screen, with the focus indicator visible at each stop.                             |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **`docs/product/story-map.md`** — the source of the story numbering, the *Look like Info Support*
  activity, and the project-wide exclusions quoted in §1.3.
- **Spec 001 (`001-start-new-game.md`)** — **Colour Mapping (DR-24)** grants this spec its permission
  to re-tune the palette and binds it to the one-to-one mapping and recognisable hues. 001's NFR-003
  (≥ 3:1 per colour) is restated as DR-153 against the new values.
- **Spec 004 (`004-next-piece-preview.md`)** — **Preview Non-Colour Cue (DR-59)** establishes that
  colour is never the only cue. DR-156 extends that principle from the preview to the board, using a
  different mechanism for a different reason (see §5.2).
- **An authoritative Info Support brand reference** — the one genuinely blocking dependency, and the
  only thing this spec could not resolve itself. Needed before release, not before implementation; see
  Open Question 1. Everything in §5 is buildable today against the placeholders.
- **Vendored Bootstrap in `wwwroot/lib/`** — bridged, not replaced (DR-167). Story 009's form and the
  vendored jquery-validation-unobtrusive depend on its classes.
- **Downstream dependents**: **story 012** (branded landing page) and **story 014** (controls panel)
  consume these tokens verbatim and cannot be built coherently until the names in §5 are agreed.
  Stories **008**, **009**, **010** and **011** consume the overlay, form, table and accent tokens from
  §5.6. Specs 001–009 are otherwise unaffected — no behavioural rule changes.

### 9.2 Constraints

- **Trademark.** The Info Support logo and wordmark are trademarks of Info Support B.V. This is an
  internal project using them in-house under the company's own brand guidelines. They are used **as
  supplied**: not redrawn, not recoloured, not reconstructed, not stretched, and not used to imply
  endorsement outside the company. This spec reserves a slot and a clear-space rule for the mark; it
  does not author it (§1.3).
- **Brand values are not this spec's to invent.** Where authoritative values were unavailable, the
  structure is specified and the values are labelled PLACEHOLDER. No placeholder may be presented as,
  or shipped as, an Info Support brand value (DR-140, Open Question 1).
- **Accessibility target**: WCAG 2.1 **AA**. AAA is not a target; where a value clears AAA it is noted
  as headroom, not as a commitment.
- **Platform**: desktop browser on a laptop with a keyboard; minimum viewport 1024 × 640 CSS pixels
  (DR-166). No mobile, no touch (story map).
- **Technology context** (from `CLAUDE.md`): ASP.NET Core 10 Razor Pages, root namespace
  `infosupport_tetris`, static assets under `wwwroot/`, client libraries vendored in `wwwroot/lib/` and
  **not npm-managed** — so no build step may be assumed for token processing. Tokens are therefore
  plain CSS custom properties, which also makes DR-140's single-file swap trivial.
- **No behavioural change**: nothing in 001–009 may be altered by this spec. Presentation only.

### 9.3 Architecture References

> **Status check:** all files in `docs/architecture/` are **empty arc42 templates** — headings and
> placeholder tables only, with no content filled in. The references below therefore state what *this
> feature needs those sections to say*. Implementing 013 is a natural trigger for filling them in, per
> `CLAUDE.md` ("fill them in as architectural decisions for the Tetris implementation are made").

| Arc42 Section                                                   | Relevance to This Feature                                                                                                                                                             |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 02. Architecture Constraints (`02-architecture-constraints.md`)  | Should record the constraints in §9.2 that outlive this feature: WCAG 2.1 AA as the accessibility target, the 1024 × 640 minimum viewport, first-party assets only, and the trademark position on the logo. *Currently empty.* |
| 05. Building Block View (`05-building-block-view.md`)            | 013 introduces the presentation-token building block — brand primitives, semantic tokens, the Bootstrap bridge — and its boundary with the game domain, which must remain free of any presentation concern. *Currently empty.* |
| 08. Cross-cutting Concepts (`08-cross-cutting-concepts.md`)      | The natural home for the three genuinely cross-cutting positions this spec establishes: the two-tier token model (DR-139), the accessibility baseline extended from 004's DR-59 to colour, contrast, focus and motion, and the asset/font loading strategy (DR-161, DR-163). *Currently empty.* |
| 09. Architecture Decisions (`09-architecture-decisions.md`)      | 013 warrants ADRs for: single dark theme (DR-145), bridging rather than replacing Bootstrap (DR-167), `font-display: optional` over `swap` (DR-161), self-hosted fonts (DR-163), and the fixed two-tone block edge over a per-type pattern (DR-156). *Currently empty.* |
| 10. Quality Requirements (`10-quality-requirements.md`)          | Should host the project-wide accessibility, layout-stability and performance scenarios that §6 here instantiates for styling — particularly the WCAG AA floors and the zero-layout-shift target, which apply to every future screen. *Currently empty.* |
| 12. Glossary (`12-glossary.md`)                                  | Should be populated from §5 — design token, primitive, semantic token, type scale, type role, tabular figures, block edge, colour-vision deficiency, ΔE00, contrast ratio, forced colours. *Currently empty.* |

## 10. Open Questions

| #   | Question                                                                                                  | Owner      | Status   | Resolution                                                                                                                                                                                                                                                  |
| --- | ------------------------------------------------------------------------------------------------------------ | ---------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | What are Info Support's authoritative brand colours and typefaces, per the official brand guidelines?        | Daan Harms | **Open** | **Unresolved and deliberately not guessed.** No published brand guideline, press kit or design-system page carrying hex values or typeface names was found on `infosupport.com` or `infosupport.nl`, and the site's stylesheets were not retrievable. Obtain the brand guidelines from Info Support marketing/communications. Blocks **release**, not implementation: the token structure is fixed, all placeholders are contrast-checked, and the swap is a single-file change (DR-140, NFR-012, SUC-008). |
| 2   | Dark theme, light theme, or both?                                                                            | Daan Harms | Resolved | **Dark only.** The board reads best on a dark surface, and every piece-palette guarantee (DR-153, DR-154, DR-155) is measured against one background — a second theme doubles that verification matrix for a coffee-break game. Because consumers use roles rather than raw values, a light theme stays an additive change (DR-145). |
| 3   | Override the vendored Bootstrap, or replace it?                                                              | Daan Harms | Resolved | **Override.** Story 009's name-entry form and the vendored jquery-validation-unobtrusive already depend on Bootstrap's classes; removing it is a rewrite and an explicit Non-Goal. Tokens drive Bootstrap's own custom properties from a bridge stylesheet, and no vendored file is edited (DR-167). |
| 4   | Which non-colour cue on the board — per-type pattern, border, or inner bevel?                                | Daan Harms | Resolved | **A fixed two-tone edge: dark outer border plus light inner bevel, never derived from the fill** (DR-156). Per-type patterns were rejected as visual noise that solves the wrong problem — a locked block's *type* is not something 001–009 ever asks the player to read; its *boundary* is. A fill-derived edge was measured and rejected at 1.01:1 worst case. The fixed pair contrasts with itself at a constant 17.03:1. Per-type identification stays where it belongs, in the preview and hold slot (DR-59). |
| 5   | Re-tune the tetromino hex values at all, or keep the Guideline palette?                                      | Daan Harms | Resolved | **Re-tune**, under the permission DR-24 grants. The Guideline values are CRT-era saturated primaries: `J #0000FF` and `T #800080` nearly vanish on a dark board, and `S #00FF00` against `Z #FF0000` is the textbook red/green collapse. §5.2 keeps every hue and every mapping while fixing contrast and CVD separation. |
| 6   | What ΔE00 floor should the palette be held to?                                                               | Daan Harms | Resolved | **10**, which the palette meets exactly at its binding pair (T / J under protanopia). Seven colours that must each stay recognisably their named hue cannot be pushed much further apart without violating DR-152 — a ΔE00 11.1 alternative was evaluated and rejected for turning J into a pale sky blue. The floor is set where it is *because* DR-156 means colour is not carrying the board alone. |
| 7   | Font loading: `font-display: swap`, `fallback`, or `optional`?                                               | Daan Harms | Resolved | **`optional`.** Both `swap` and `fallback` permit a re-render after first paint, which means text reflowing while a piece is falling. `optional` trades an occasional off-face first load for a guaranteed zero post-paint shift (DR-161). For a game, a stable layout beats a perfect first impression. |
| 8   | Self-host the fonts, or use a CDN?                                                                           | Daan Harms | Resolved | **Self-host.** A font CDN discloses every player's IP address to a third party, which contradicts 001's NFR-007 no-personal-data posture and is the wrong default for a Dutch employer. Self-hosting also removes a third-party failure mode (DR-163). |
| 9   | What minimum viewport must the layout hold at, given "no mobile"?                                            | Daan Harms | Resolved | **1024 × 640 CSS pixels** (DR-166) — the viewport left by a 1366 × 768 laptop after browser chrome, which is the smallest machine a colleague plausibly plays on. "No mobile" means the layout is not designed for phones, not that it may break on a small laptop. |
| 10  | Should 013 own any page layout, or only tokens?                                                              | Daan Harms | Resolved | **Tokens only.** 013 owns the vocabulary; 012 and 014 own their compositions. §5.6 maps tokens to surfaces so the owning stories inherit colour and type decisions without inheriting layout ones. Mirrors how 001 owns the domain model for 002–008. |
| 11  | Should the palette include a dimmer `--text-muted` for de-emphasised copy?                                   | Daan Harms | Resolved | **No.** A third, dimmer text colour is the most common way a token set drifts below 4.5:1, and nothing in 001–009 needs one. `--text-secondary` is the dimmest text token and clears the body floor at 9.89:1. Omitting it removes the temptation entirely. |

_Exactly one `[NEEDS CLARIFICATION]` marker remains in this spec (§5.0), tracked as Open Question 1._

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
        US-001 -> SC-001..SC-003; US-002 -> SC-004..SC-006, SC-013;
        US-003 -> SC-007..SC-012; US-004 -> SC-014..SC-021; US-005 -> SC-022
  - [x] Each functional requirement traces to a user story
        (FR-001..FR-006 -> US-001; FR-007..FR-013 -> US-002; FR-014..FR-020 -> US-003;
         FR-021..FR-028 -> US-004; FR-029..FR-030 -> US-005)
  - [x] Domain model covers all entities mentioned in the requirements
        The domain is the token system: colour primitives and semantic roles (§5.1), the tetromino
        palette (§5.2), typography (§5.3), spacing/geometry/elevation (§5.4), motion (§5.5),
        and the token-to-surface mapping (§5.6)
  - [x] Domain rules and invariants are listed (DR-139 .. DR-168 — this spec's allocated block;
        DR-01..DR-138 belong to specs 001-009 and are cited, never redefined)
  - [x] Edge cases cover failure modes, not just happy paths (EC-1 .. EC-15)
  - [x] Non-functional requirements are specific and measurable (NFR-001 .. NFR-012 — every one is a
        ratio, a ΔE00 figure, a pixel count or a byte budget, all automatable)
  - [x] Arc42 references point to the right sections — noting all arc42 files are empty placeholders
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (exactly one remains, by design:
        the authoritative brand values, which this spec must not invent)
  - [x] Open questions are assigned and have a resolution path (11 recorded; 10 resolved,
        1 open with an owner and a stated unblocking path)
  - [x] Confirmed values and placeholder values are labelled distinctly everywhere and never blurred
-->
