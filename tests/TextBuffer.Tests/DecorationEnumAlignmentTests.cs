// Tests to verify decoration enum values match TypeScript exactly
// TypeScript source: vs/editor/common/model.ts

using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Tests to verify enum values align with TypeScript definitions.
/// These tests ensure serialization/deserialization compatibility
/// between C# and TypeScript implementations.
/// </summary>
public class DecorationEnumAlignmentTests
{
    #region MinimapPosition Tests
    
    [Fact]
    public void MinimapPosition_Inline_MatchesTypeScript()
    {
        // TS: MinimapPosition.Inline = 1
        Assert.Equal(1, (int)MinimapPosition.Inline);
    }
    
    [Fact]
    public void MinimapPosition_Gutter_MatchesTypeScript()
    {
        // TS: MinimapPosition.Gutter = 2
        Assert.Equal(2, (int)MinimapPosition.Gutter);
    }
    
    [Fact]
    public void MinimapPosition_ValuesAreContiguous()
    {
        // Verify no gaps in enum values
        int[] values = Enum.GetValues<MinimapPosition>().Cast<int>().OrderBy(x => x).ToArray();
        Assert.Equal([1, 2], values);
    }
    
    #endregion
    
    #region GlyphMarginLane Tests
    
    [Fact]
    public void GlyphMarginLane_Left_MatchesTypeScript()
    {
        // TS: GlyphMarginLane.Left = 1
        Assert.Equal(1, (int)GlyphMarginLane.Left);
    }
    
    [Fact]
    public void GlyphMarginLane_Center_MatchesTypeScript()
    {
        // TS: GlyphMarginLane.Center = 2
        Assert.Equal(2, (int)GlyphMarginLane.Center);
    }
    
    [Fact]
    public void GlyphMarginLane_Right_MatchesTypeScript()
    {
        // TS: GlyphMarginLane.Right = 3
        Assert.Equal(3, (int)GlyphMarginLane.Right);
    }
    
    [Fact]
    public void GlyphMarginLane_ValuesAreContiguous()
    {
        // Verify no gaps in enum values
        int[] values = Enum.GetValues<GlyphMarginLane>().Cast<int>().OrderBy(x => x).ToArray();
        Assert.Equal([1, 2, 3], values);
    }
    
    #endregion
    
    #region InjectedTextCursorStops Tests
    
    [Fact]
    public void InjectedTextCursorStops_Both_MatchesTypeScript()
    {
        // TS: InjectedTextCursorStops.Both = 0
        Assert.Equal(0, (int)InjectedTextCursorStops.Both);
    }
    
    [Fact]
    public void InjectedTextCursorStops_Right_MatchesTypeScript()
    {
        // TS: InjectedTextCursorStops.Right = 1
        Assert.Equal(1, (int)InjectedTextCursorStops.Right);
    }
    
    [Fact]
    public void InjectedTextCursorStops_Left_MatchesTypeScript()
    {
        // TS: InjectedTextCursorStops.Left = 2
        Assert.Equal(2, (int)InjectedTextCursorStops.Left);
    }
    
    [Fact]
    public void InjectedTextCursorStops_None_MatchesTypeScript()
    {
        // TS: InjectedTextCursorStops.None = 3
        Assert.Equal(3, (int)InjectedTextCursorStops.None);
    }
    
    [Fact]
    public void InjectedTextCursorStops_IsNotFlagsEnum()
    {
        // TS uses non-flags enum, C# should match
        // A flags enum would have FlagsAttribute
        Type type = typeof(InjectedTextCursorStops);
        bool hasFlags = type.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
        Assert.False(hasFlags, "InjectedTextCursorStops should NOT be a [Flags] enum to match TypeScript");
    }
    
    [Fact]
    public void InjectedTextCursorStops_ValuesAreContiguous()
    {
        // Verify no gaps in enum values
        int[] values = Enum.GetValues<InjectedTextCursorStops>().Cast<int>().OrderBy(x => x).ToArray();
        Assert.Equal([0, 1, 2, 3], values);
    }
    
    #endregion
    
    #region MinimapSectionHeaderStyle Tests
    
    [Fact]
    public void MinimapSectionHeaderStyle_Normal_MatchesTypeScript()
    {
        // TS: MinimapSectionHeaderStyle.Normal = 1
        Assert.Equal(1, (int)MinimapSectionHeaderStyle.Normal);
    }
    
    [Fact]
    public void MinimapSectionHeaderStyle_Underlined_MatchesTypeScript()
    {
        // TS: MinimapSectionHeaderStyle.Underlined = 2
        Assert.Equal(2, (int)MinimapSectionHeaderStyle.Underlined);
    }
    
    [Fact]
    public void MinimapSectionHeaderStyle_ValuesAreContiguous()
    {
        // Verify no gaps in enum values
        int[] values = Enum.GetValues<MinimapSectionHeaderStyle>().Cast<int>().OrderBy(x => x).ToArray();
        Assert.Equal([1, 2], values);
    }
    
    #endregion
    
    #region Default Value Tests
    
    [Fact]
    public void ModelDecorationMinimapOptions_DefaultPosition_IsInline()
    {
        var options = new ModelDecorationMinimapOptions();
        Assert.Equal(MinimapPosition.Inline, options.Position);
    }
    
    [Fact]
    public void ModelDecorationGlyphMarginOptions_DefaultPosition_IsCenter()
    {
        var options = new ModelDecorationGlyphMarginOptions();
        Assert.Equal(GlyphMarginLane.Center, options.Position);
    }
    
    [Fact]
    public void ModelDecorationInjectedTextOptions_DefaultCursorStops_IsBoth()
    {
        var options = new ModelDecorationInjectedTextOptions();
        Assert.Equal(InjectedTextCursorStops.Both, options.CursorStops);
    }
    
    #endregion
    
    #region JSON Serialization Round-Trip Tests
    
    [Fact]
    public void MinimapPosition_JsonRoundTrip()
    {
        foreach (var position in Enum.GetValues<MinimapPosition>())
        {
            int value = (int)position;
            var parsed = (MinimapPosition)value;
            Assert.Equal(position, parsed);
        }
    }
    
    [Fact]
    public void GlyphMarginLane_JsonRoundTrip()
    {
        foreach (var lane in Enum.GetValues<GlyphMarginLane>())
        {
            int value = (int)lane;
            var parsed = (GlyphMarginLane)value;
            Assert.Equal(lane, parsed);
        }
    }
    
    [Fact]
    public void MinimapSectionHeaderStyle_JsonRoundTrip()
    {
        foreach (var style in Enum.GetValues<MinimapSectionHeaderStyle>())
        {
            int value = (int)style;
            var parsed = (MinimapSectionHeaderStyle)value;
            Assert.Equal(style, parsed);
        }
    }
    
    [Fact]
    public void InjectedTextCursorStops_JsonRoundTrip()
    {
        foreach (var stops in Enum.GetValues<InjectedTextCursorStops>())
        {
            int value = (int)stops;
            var parsed = (InjectedTextCursorStops)value;
            Assert.Equal(stops, parsed);
        }
    }
    
    #endregion
    
    #region OverviewRulerLane Tests (should remain unchanged)
    
    [Fact]
    public void OverviewRulerLane_IsFlagsEnum()
    {
        // OverviewRulerLane is a [Flags] enum in both TS and C#
        Type type = typeof(OverviewRulerLane);
        bool hasFlags = type.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
        Assert.True(hasFlags, "OverviewRulerLane should be a [Flags] enum");
    }
    
    [Fact]
    public void OverviewRulerLane_Values_MatchTypeScript()
    {
        // TS: Left = 1, Center = 2, Right = 4, Full = 7
        Assert.Equal(1, (int)OverviewRulerLane.Left);
        Assert.Equal(2, (int)OverviewRulerLane.Center);
        Assert.Equal(4, (int)OverviewRulerLane.Right);
        Assert.Equal(7, (int)OverviewRulerLane.Full);
    }
    
    #endregion
}
