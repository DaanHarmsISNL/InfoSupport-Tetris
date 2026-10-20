using System.Collections.Concurrent;
using infosupport_tetris.Domain;

namespace infosupport_tetris.Services;

/// <summary>
/// Holds at most one in-progress <see cref="GameSession"/> per browser session (keyed by the
/// ASP.NET Core session cookie id). This is the server-side home for the "where does the game
/// loop/domain model live" question that spec 001 §9.3/§10 Open Question 1 leaves to the
/// architecture docs — nothing is persisted (NFR-007: no identity beyond an anonymous cookie),
/// and EC-12 ("start/New game activated repeatedly") is satisfied because a replace is always
/// a plain dictionary write, never an append.
/// </summary>
public interface IGameSessionStore
{
    GameSession? Get(string sessionId);

    GameSession Replace(string sessionId, GameSession newSession);
}

public sealed class GameSessionStore : IGameSessionStore
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public GameSession? Get(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) ? session : null;

    public GameSession Replace(string sessionId, GameSession newSession)
    {
        _sessions[sessionId] = newSession;
        return newSession;
    }
}
