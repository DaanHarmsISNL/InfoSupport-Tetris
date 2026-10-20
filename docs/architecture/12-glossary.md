# 12. Glossary

<!-- Domain and technical terms used throughout the documentation -->

| Term | Definition |
| ---- | ---------- |
| GameSession | The aggregate root holding all per-player game state for a single Tetris run. |
| GameStatus | Enum (`Ready`, `Playing`, `GameOver`) describing the lifecycle state of a `GameSession`. |
| Playfield | The grid of cells pieces occupy, consisting of the 20 visible rows plus hidden buffer rows above them. |
| BufferRowCount / buffer rows | The hidden spawn rows above the 20 visible playfield rows, used so pieces can spawn off-screen. |
| ActivePiece | The tetromino currently falling and under player control. |
| Tetromino / TetrominoType | A four-cell piece shape; the closed set of seven one-sided shapes `I`, `O`, `T`, `S`, `Z`, `J`, `L` defined in `TetrominoDefinition.cs`. |
| Orientation | Enum of the four SRS rotation states (`Spawn`, `Right`, `Inverted`, `Left`) a tetromino can be in. |
| 7-bag (SevenBag) | The randomizer algorithm that deals all seven tetromino types once per "bag" before reshuffling, avoiding long droughts of any one piece. |
| PieceQueue | The upcoming sequence of pieces that feeds the Next Piece Preview. |
| Next Piece Preview | The domain/UI concept that surfaces the upcoming piece(s) from the `PieceQueue` to the player. |
| Hold / HoldDisplay | Stashing the active piece for later use, limited to once per piece drop. |
| Kick / Kick table (SRS kicks) | The Super Rotation System offset table tried in order to resolve wall/floor collisions during rotation. |
| Gravity | The automatic downward movement of the active piece over time. |
| Soft drop | Player-accelerated downward movement that still resolves through normal gravity/lock rules. |
| Hard drop | Instant drop-and-lock of the active piece to the lowest valid position. |
| Lock delay | The grace period after a piece lands before it locks, during which the player can still move or rotate it. |
| Line clear | The removal and scoring of a completed horizontal row. |
| ClearType | Enum classifying a lock resolution by rows cleared: `None`, `Single`, `Double`, `Triple`, `Tetris`. |
| ScoreAward / ScoreSource | A grant of points and the enum of why it was awarded (`LineClear`, `SoftDrop`, `HardDrop`); only `LineClear` is multiplied by the current level. |
| Level / gravity speed curve | The mechanic by which gravity accelerates as the player's level increases. |
| CompletedRun | The end-of-game summary record (final score, level, lines cleared, etc.) produced when a session reaches `GameOver`. |
| TopOutReason | Enum of the closed set of ways a session reaches `GameOver`: `BlockOut` (a piece could not be placed at spawn) and `LockOut` (a piece locked entirely within buffer rows). |
| InputCommand / InputState | The input abstraction representing move/rotate/soft-drop/hold player commands. |
| FR- (Functional Requirement) | Spec ID prefix for a required system behaviour, listed in each spec's Functional Requirements section. |
| DR- (Design Rule) | Spec ID prefix for a named domain rule underpinning the design, defined in each spec's Domain Model section. |
| EC- (Edge Case) | Spec ID prefix for an edge case or error scenario a spec explicitly addresses. |
| NFR- (Non-Functional Requirement) | Spec ID prefix for a quality-attribute requirement (performance, reliability, etc.) in a spec's Non-Functional Requirements section. |
| SC- (Success Criterion) | Spec ID prefix for a measurable condition used to judge whether a spec's feature is successfully delivered. |
