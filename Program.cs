using infosupport_tetris.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Anonymous, cookie-based session id only (NFR-007: no accounts, no persisted identity) used
// to key the in-memory GameSession store so each browser tab/session keeps its own game.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddSingleton<IGameSessionStore, GameSessionStore>();

// docs/specs/009-submit-score.md DR-115/DR-121/DR-122: one shared leaderboard, global to the
// application. In-memory for now -- durability across a redeploy (FR-023) is deferred to an ADR
// (arc42 §09, open question 1); nothing above this interface changes when that is resolved.
builder.Services.AddSingleton<IScoreSubmissionService, ScoreSubmissionService>();
builder.Services.AddSingleton<ILeaderboardStore, LeaderboardStore>();
builder.Services.AddSingleton<ILeaderboardQueryService, LeaderboardQueryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
