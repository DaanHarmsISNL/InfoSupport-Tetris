using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// docs/specs/012-landing-page.md: a small, focused slice of the 27-scenario spec rather than
/// full coverage -- the routing split (landing page at `/`, game relocated to `/Game`) and the
/// core shape of the new front door (SC-001, SC-002, SC-004, SC-011).
/// </summary>
public class LandingPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LandingPageTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // SC-001/SC-002: the root address is the landing page, not the ASP.NET Core scaffold and not
    // the game board.
    [Fact]
    public async Task Get_Root_ReturnsLandingPage_NotScaffoldOrGameMarkup()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // FR-021/DR-254: the document title identifies the product out of context.
        Assert.Contains("<title>Info Support Tetris</title>", html);

        // SC-001: none of the ASP.NET Core scaffold's default welcome text remains.
        Assert.DoesNotContain("Learn about building Web apps with ASP.NET Core", html);

        // 012 ends where 001 begins -- the playfield markup lives on /Game, not here.
        Assert.DoesNotContain("tetris-playfield", html);

        // FR-002/FR-011: the three controls, in reading order, each with a meaningful name.
        Assert.Contains("Play", html);
        Assert.Contains("View the leaderboard", html);
        Assert.Contains("How to play", html);
    }

    // FR-007/FR-009/DR-229/DR-245: /Game is where the game itself now lives, at its own address.
    [Fact]
    public async Task Get_Game_ServesTheGame()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/Game");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("tetris-playfield", html);
    }

    // DR-229/SC-004: activating the primary action starts a new GameSession in status Playing
    // immediately, with no intermediate form -- the landing page's "Play" form posts straight to
    // /Game's Start handler.
    [Fact]
    public async Task PostStartOnGame_FromLanding_RedirectsIntoANewPlayingSession()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsync("/Game?handler=Start", content: null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Game", response.Headers.Location?.OriginalString);
    }

    // FR-015/DR-243: reachable with no precondition at all -- no game started first.
    [Fact]
    public async Task Get_Leaderboard_IsReachableWithoutHavingPlayed()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/Leaderboard");
        response.EnsureSuccessStatusCode();
    }
}
