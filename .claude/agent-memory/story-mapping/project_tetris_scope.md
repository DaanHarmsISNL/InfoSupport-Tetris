---
name: project-tetris-scope
description: InfoSupport-Tetris product scope — classic Tetris with a name-entry all-time leaderboard and Info Support branding; no accounts, no ghost piece, no pause
metadata:
  type: project
---

InfoSupport-Tetris is a faithful classic Tetris game with a single all-time high-score leaderboard (name + score only) and heavy Info Support branding. There is **no login and no account system** — the player types a name at game over and that name goes on the board.

**Why:** It is a casual office toy for five-minute coffee breaks among colleagues, not a serious competitive product. The user reversed an earlier username/password decision on 2026-10-20 specifically to remove friction before play.

**How to apply:** When proposing features, resist scope creep toward accounts or tournament mechanics. Confirmed scope decisions:

- In: next-piece preview, hold piece, hard drop, soft drop, increasing speed per level, line-clear scoring with multi-line bonus, top-out game over, name entry at game over.
- Out, explicitly: **accounts / authentication / sessions** (reversed decision — do not re-propose), **ghost piece** (user wants the difficulty), **pause** (games are short by design).
- Out: weekly resets, personal streaks, challenge-a-colleague, anything beyond name and score on the leaderboard.
- Out: mobile and touch controls — laptop with keyboard only.
- Because there is no persistent identity, "personal best" and "my rank" are not expressible. The equivalent payoff is highlighting the just-finished run on the leaderboard.
- Branding is a first-class requirement, not polish-later: the app "should really feel like Info Support" in appearance.

The story map lives at `docs/product/story-map.md` (stories 001-014). See [[project-tetris-story-map]].
