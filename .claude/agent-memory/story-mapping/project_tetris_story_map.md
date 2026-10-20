---
name: project-tetris-story-map
description: Location and structure of the InfoSupport-Tetris story map — 3 activities, stories numbered 001-014, numbers carry through to spec files
metadata:
  type: project
---

The approved user story map is at `docs/product/story-map.md`, created and then revised on 2026-10-20.

**Why:** Story numbers are the shared handle between the map and the `/spec <number>` workflow, so they must stay stable once spec files exist.

**How to apply:** When the user refers to a story by number, read the map rather than guessing. Current structure:

- **Play Tetris** (001-008): board and falling pieces, keyboard move/rotate/soft-drop, hard drop, next preview, hold, line clears and scoring, speed ramp, top-out game over.
- **Compete** (009-011): type a name at game over and submit, all-time leaderboard, just-finished run highlighted.
- **Look like Info Support** (012-014): branded landing page, brand colors and typography app-wide, controls/how-to-play panel.

Note: the map was renumbered once, when the "Get in" activity (register / log in / stay signed in) was removed after the user dropped accounts entirely. That was safe only because no spec files existed yet. Check `docs/` for spec files before any future renumber; otherwise append new numbers instead. Scope rationale is in [[project-tetris-scope]].
