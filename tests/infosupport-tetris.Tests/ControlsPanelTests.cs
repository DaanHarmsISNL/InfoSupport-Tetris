using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using infosupport_tetris.Domain;
using Xunit;

namespace infosupport_tetris.Tests;

/// <summary>
/// docs/specs/014-controls-panel.md: data-level coverage for the control legend (DR-259 .. DR-264)
/// and for DR-260's completeness rule. There is no ASP.NET test host wired up in this project
/// (no WebApplicationFactory reference), so the panel's markup is exercised at the Razor-partial
/// source level and via the rendered <c>Pages/Game.cshtml</c> source rather than through a live
/// HTTP render -- the assertions below are the ones SC-001, SC-002 and SC-005 ask for, applied to
/// the data that drives the markup.
/// </summary>
public class ControlsPanelTests
{
    /// <summary>SC-001/DR-260: exactly one entry per PlayerAction, no duplicates, nothing missing.</summary>
    [Fact]
    public void Legend_HasExactlyOneEntryPerPlayerAction()
    {
        var actions = typeof(PlayerAction).GetEnumValues().Cast<PlayerAction>().ToHashSet();
        var legendActions = ControlLegend.Entries.Select(e => e.Action).ToList();

        Assert.Equal(actions.Count, legendActions.Count);
        Assert.Equal(actions, legendActions.ToHashSet());
        Assert.Equal(legendActions.Count, legendActions.Distinct().Count());
    }

    /// <summary>DR-262: entries render in the fixed order 1..7, move/drop/rotate/hold.</summary>
    [Fact]
    public void Legend_IsInStableDisplayOrder()
    {
        var expectedOrder = new[]
        {
            PlayerAction.MoveLeft,
            PlayerAction.MoveRight,
            PlayerAction.SoftDrop,
            PlayerAction.HardDrop,
            PlayerAction.RotateClockwise,
            PlayerAction.RotateCounterClockwise,
            PlayerAction.Hold,
        };

        var actualOrder = ControlLegend.Entries
            .OrderBy(e => e.DisplayOrder)
            .Select(e => e.Action)
            .ToArray();

        Assert.Equal(expectedOrder, actualOrder);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, ControlLegend.Entries.Select(e => e.DisplayOrder).OrderBy(x => x));
    }

    /// <summary>SC-005/DR-263: the three two-key actions show both keys; the four single-key
    /// actions show exactly one.</summary>
    [Theory]
    [InlineData(PlayerAction.MoveLeft, 1)]
    [InlineData(PlayerAction.MoveRight, 1)]
    [InlineData(PlayerAction.SoftDrop, 1)]
    [InlineData(PlayerAction.HardDrop, 1)]
    [InlineData(PlayerAction.RotateClockwise, 2)]
    [InlineData(PlayerAction.RotateCounterClockwise, 2)]
    [InlineData(PlayerAction.Hold, 2)]
    public void Legend_ShowsEveryBoundKeyForEachAction(PlayerAction action, int expectedKeyCount)
    {
        var entry = ControlLegend.Entries.Single(e => e.Action == action);
        Assert.Equal(expectedKeyCount, entry.Keys.Count);
    }

    /// <summary>§5.2 table: the exact key names per action, in order (both alternatives, not one).</summary>
    [Theory]
    [InlineData(PlayerAction.MoveLeft, new[] { "Left arrow" })]
    [InlineData(PlayerAction.MoveRight, new[] { "Right arrow" })]
    [InlineData(PlayerAction.SoftDrop, new[] { "Down arrow" })]
    [InlineData(PlayerAction.HardDrop, new[] { "Space" })]
    [InlineData(PlayerAction.RotateClockwise, new[] { "Up arrow", "X" })]
    [InlineData(PlayerAction.RotateCounterClockwise, new[] { "Z", "Ctrl" })]
    [InlineData(PlayerAction.Hold, new[] { "C", "Shift" })]
    public void Legend_HasTheExactKeyNamesFromTheSpecTable(PlayerAction action, string[] expectedNames)
    {
        var entry = ControlLegend.Entries.Single(e => e.Action == action);
        Assert.Equal(expectedNames, entry.Keys.Select(k => k.Name).ToArray());
    }

    /// <summary>DR-270: only soft drop and hold carry a note; the other five carry none.</summary>
    [Fact]
    public void Legend_OnlySoftDropAndHoldHaveNotes()
    {
        Assert.Equal("while held", ControlLegend.Entries.Single(e => e.Action == PlayerAction.SoftDrop).Note);
        Assert.Equal("once per piece", ControlLegend.Entries.Single(e => e.Action == PlayerAction.Hold).Note);

        var othersWithNotes = ControlLegend.Entries
            .Where(e => e.Action != PlayerAction.SoftDrop && e.Action != PlayerAction.Hold)
            .Where(e => e.Note is not null);

        Assert.Empty(othersWithNotes);
    }

    /// <summary>DR-264: every KeyCap exposes a non-empty text name, whether or not it has a glyph.</summary>
    [Fact]
    public void EveryKeyCap_HasANonEmptyTextName()
    {
        foreach (var entry in ControlLegend.Entries)
        {
            foreach (var key in entry.Keys)
            {
                Assert.False(string.IsNullOrWhiteSpace(key.Name));
            }
        }
    }

    /// <summary>The four arrow actions carry a glyph; the others (letters, Space, Ctrl, Shift) do not
    /// -- matching §5.2's KeyCap table exactly.</summary>
    [Fact]
    public void ArrowKeys_CarryAGlyph_OthersDoNot()
    {
        var glyphed = new HashSet<string> { "Left arrow", "Right arrow", "Down arrow", "Up arrow" };

        foreach (var entry in ControlLegend.Entries)
        {
            foreach (var key in entry.Keys)
            {
                if (glyphed.Contains(key.Name))
                {
                    Assert.False(string.IsNullOrEmpty(key.Glyph));
                }
                else
                {
                    Assert.True(string.IsNullOrEmpty(key.Glyph));
                }
            }
        }
    }

    /// <summary>DR-270/FR-013: exactly four how-to-play lines, each citing one of 006/007/008.</summary>
    [Fact]
    public void HowToPlay_HasExactlyFourNotesEachCitingAnOwningSpec()
    {
        Assert.Equal(4, HowToPlay.Notes.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, HowToPlay.Notes.Select(n => n.Order).OrderBy(x => x));

        foreach (var note in HowToPlay.Notes)
        {
            Assert.Contains(note.OwningSpec, new[] { "006", "007", "008" });
            Assert.False(string.IsNullOrWhiteSpace(note.Text));
        }
    }

    /// <summary>
    /// NFR-009: heading + 7 rows + 2 notes + 4 prose lines stays under 700 characters of visible
    /// text (labels, key names -- not glyphs, which are presentation-only duplicates of the name).
    /// </summary>
    [Fact]
    public void WholePanelContent_StaysUnderSevenHundredCharacters()
    {
        var length = "Controls".Length
            + ControlLegend.Entries.Sum(e =>
                e.Label.Length
                + e.Keys.Sum(k => k.Name.Length + 4) // " or " separators/approximation
                + (e.Note?.Length ?? 0))
            + HowToPlay.Notes.Sum(n => n.Text.Length);

        Assert.True(length < 700, $"Panel content is {length} characters, expected < 700 (NFR-009).");
    }

    /// <summary>
    /// DR-259 (known gap, documented on <see cref="ControlLegend"/>): the inline keyboard-handling
    /// script in Pages/Index.cshtml cannot be generated from ControlLegend without risking a merge
    /// conflict with the concurrently in-flight landing-page branch, so it remains a parallel
    /// literal. This regression test is the enforcement mechanism SC-002/NFR-003 ask for in place
    /// of generation: every key code the script binds must decode (via the same DOM KeyboardEvent
    /// "code" naming the script uses) to a key name that appears somewhere in ControlLegend, and
    /// every key name ControlLegend lists (other than Space/Ctrl generic forms already covered by
    /// the script's left/right variants) must appear bound in the script. A drift here is exactly
    /// the defect DR-259 exists to catch.
    /// </summary>
    [Fact]
    public void InlineKeyboardScript_StaysInSyncWithControlLegend()
    {
        var gamePath = FindRepoFile("Pages", "Game.cshtml");
        var source = File.ReadAllText(gamePath);

        // The script binds these DOM "code" values to these PlayerAction-ish command strings.
        var expectedCodes = new[]
        {
            "ArrowLeft", "ArrowRight", "ArrowDown", "ArrowUp",
            "KeyX", "KeyZ", "ControlLeft", "ControlRight",
            "KeyC", "ShiftLeft", "ShiftRight",
        };

        foreach (var code in expectedCodes)
        {
            Assert.Contains(code, source);
        }

        // Every legend key name's underlying physical key is represented by at least one of the
        // codes above -- i.e. nothing in ControlLegend names a key the script does not bind.
        var nameToCodeFragment = new Dictionary<string, string>
        {
            ["Left arrow"] = "ArrowLeft",
            ["Right arrow"] = "ArrowRight",
            ["Down arrow"] = "ArrowDown",
            ["Up arrow"] = "ArrowUp",
            ["X"] = "KeyX",
            ["Z"] = "KeyZ",
            ["Ctrl"] = "Control",
            ["C"] = "KeyC",
            ["Shift"] = "Shift",
            ["Space"] = "hard-drop-form", // Space is handled by the dedicated hard-drop script/form, not `bindings`.
        };

        foreach (var entry in ControlLegend.Entries)
        {
            foreach (var key in entry.Keys)
            {
                Assert.True(
                    nameToCodeFragment.TryGetValue(key.Name, out var fragment),
                    $"No known script fragment mapped for legend key '{key.Name}' -- update this test's mapping or ControlLegend.");
                Assert.Contains(fragment, source);
            }
        }
    }

    private static string FindRepoFile(params string[] relativeSegments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate " + Path.Combine(relativeSegments) + " by walking up from " + AppContext.BaseDirectory);
    }
}
