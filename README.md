# ChessEngine — a from-scratch chess engine in C#

A complete chess engine written from first principles in C# (.NET 8), with no
chess libraries and no dependencies. It plays legal chess through the
[Universal Chess Interface (UCI)](https://en.wikipedia.org/wiki/Universal_Chess_Interface),
so you can plug it into any GUI (Arena, Cute Chess, lichess-bot, …) or drive it
from the command line.

## What it does

- **Legal move generation** — every rule of chess: castling (both sides),
  en passant (including the horizontal pin edge case), promotions, check,
  checkmate and stalemate detection. Verified against the standard perft
  suites (see below).
- **Search** — negamax with alpha-beta pruning, iterative deepening,
  quiescence search (so it never stops mid-exchange and misevaluates a hanging
  queen) and MVV-LVA capture ordering.
- **Evaluation** — material values plus piece-square tables, in centipawns.
- **UCI protocol** — `uci`, `isready`, `position`, `go` (depth / movetime /
  clock), `setoption name Depth`, plus `d` (print board), `perft` and `bench`
  helpers.

## Perft — proof the move generator is right

Perft counts the leaf nodes of the move tree; the counts are known constants,
so matching them proves the generator handles every rule correctly.

| Position | Depth | Nodes |
| --- | --- | --- |
| Start position | 5 | 4,865,609 |
| Kiwipete (castling, en passant, promotions) | 3 | 97,862 |
| En-passant pin position | 4 | 43,238 |
| Promotion-heavy position | 3 | 62,379 |

On this machine the engine runs at roughly 4.8M nodes/second in Release.

## How to run

```bash
dotnet run --project src/ChessEngine.Cli -c Release        # UCI loop (for a GUI)
dotnet run --project src/ChessEngine.Cli -c Release -- perft 4
dotnet run --project src/ChessEngine.Cli -c Release -- bestmove 3
dotnet run --project src/ChessEngine.Cli -c Release -- bench
```

Talking to it over UCI:

```
uci
isready
position startpos moves e2e4 e7e5
go depth 5
bestmove b1c3
```

## How to run the tests

```bash
dotnet test -c Release
```

The suite (44 tests) is built around perft: if any move-generation rule is
wrong, the counts stop matching. It also covers FEN round-trips, make/unmake
restoring the position exactly, castling rights, en-passant captures, pins,
mate-in-one search, and a scripted UCI session.

## Design decisions

1. **Mailbox board, not bitboards.** A plain 64-square array keeps the code
   readable and every rule explicit — the right trade-off for an engine whose
   job is to be understood, while still reaching ~5M nps.
2. **Make/unmake with an undo record instead of copying the board.**
   Search and perft make millions of moves; restoring from a small `Undo`
   struct is far cheaper than cloning the position each time, and the tests
   prove make/unmake is an exact inverse.
3. **Pseudo-legal generation + legality filter.** Generating all moves and
   discarding the ones that leave the king in check is simpler and less
   error-prone than generating only legal moves directly — and it is exactly
   what makes the en-passant pin case fall out for free.
4. **Quiescence search at the horizon.** Evaluating only quiet positions
   avoids the classic horizon effect where the engine thinks it won a queen
   because it stopped searching one ply before the recapture.

## Project layout

```
src/
  ChessEngine/        Library: Board, MoveGenerator, Perft, Evaluation, Search
  ChessEngine.Cli/    UCI loop + perft / bestmove / bench commands
tests/
  ChessEngine.Tests/  xUnit: perft suites, board rules, search, UCI sessions
```

## Tech stack

- C# 12 / .NET 8, zero third-party dependencies
- xUnit for tests
