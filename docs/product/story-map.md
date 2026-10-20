# User Story Map: InfoSupport Tetris

## Goal
A faithful, polished Tetris game wearing Info Support's brand, where colleagues drop in with no sign-up, play a quick round, and put their name on a shared all-time leaderboard — turning a five-minute coffee break into office bragging rights.

## Primary User
An Info Support colleague on a laptop with a keyboard, playing casually during a short break. They want zero friction to start, a proper game of Tetris, and the chance to stamp their name on the board if they do well.

## Story Map

### Play Tetris
| #   | Story                                                                                  |
| --- | -------------------------------------------------------------------------------------- |
| 001 | As a player, I can start a new game on a standard 10x20 board where pieces spawn and fall |
| 002 | As a player, I can move, rotate and soft-drop the falling piece with the keyboard         |
| 003 | As a player, I can hard drop a piece to slam it straight down                             |
| 004 | As a player, I can see the next piece coming so I can plan ahead                          |
| 005 | As a player, I can hold a piece and swap it back in on a later turn                       |
| 006 | As a player, I clear full lines and earn points, with a bonus for multi-line clears        |
| 007 | As a player, the game speeds up as my level climbs so it keeps getting harder             |
| 008 | As a player, the game ends when the stack tops out and I'm shown my final score           |

### Compete
| #   | Story                                                                                  |
| --- | -------------------------------------------------------------------------------------- |
| 009 | As a player, I can type my name at game over and submit my score to the leaderboard      |
| 010 | As a colleague, I can view the all-time leaderboard showing each name and high score      |
| 011 | As a player, my just-finished run is highlighted on the leaderboard so I can see where I landed |

### Look like Info Support
| #   | Story                                                                                  |
| --- | -------------------------------------------------------------------------------------- |
| 012 | As a visitor, I land on a branded Info Support page that tells me what this is           |
| 013 | As a player, the whole app uses Info Support colors and typography so it feels on-brand  |
| 014 | As a new player, I can see a controls / how-to-play panel so I know which keys do what   |

## Out of scope (deliberate)
- **Accounts and authentication** — no registration, no login, no sessions. A player is identified only by the name they type in at game over.
- **Ghost piece** — explicitly dropped; the user wants the challenge.
- **Pause** — explicitly dropped; games are short by design.
- **Weekly resets, challenges, streaks** — the leaderboard is all-time high score only.
- **Mobile / touch controls** — laptop + keyboard only.
