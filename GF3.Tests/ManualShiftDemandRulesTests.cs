using BusinessLogicLayer.Common;
using WebApi.ShiftSwaps;
using static WebApi.ShiftSwaps.ShiftSwapRules;

namespace GF3.Tests;
public sealed class ManualShiftDemandRulesTests
{
    [Fact]
    public void Capacity_DuplicatesAndTouchingRanges()
    {
        var p = new ShiftSwapPeriod("09:00", "15:00");
        Assert.Equal(2, ManualShiftDemandRules.ParseCell("09:00-15:00,09:00-15:00").Count);
        Assert.True(ManualShiftDemandRules.CanReserve("09:00-15:00,09:00-15:00", [p], p));
        Assert.False(ManualShiftDemandRules.CanReserve("09:00-15:00,09:00-15:00", [p,p], p));
        Assert.True(ManualShiftDemandRules.CanReserve("09:00-15:00,15:00-21:00", [], new("09:00","21:00")));
        Assert.False(ManualShiftDemandRules.CanReserve("09:00-15:00,16:00-21:00", [], new("09:00","21:00")));
    }
    [Theory]
    [InlineData("09:00", "21:00", null)]
    [InlineData("09:00", "15:00", "15:00 - 21:00")]
    [InlineData("15:00", "21:00", "09:00 - 15:00")]
    [InlineData("12:00", "15:00", "09:00 - 12:00, 15:00 - 21:00")]
    public void Consume_SubtractsOnlyClaimedUnit(string from, string to, string? expected)
        => Assert.Equal(expected, ManualShiftDemandRules.Consume("09:00-21:00", new(from,to)));
    [Fact]
    public void Consume_DuplicateLeavesOtherUnit()
        => Assert.Equal("09:00 - 15:00", ManualShiftDemandRules.Consume("09:00-15:00,09:00-15:00", new("09:00","15:00")));
    [Theory]
    [InlineData("text")][InlineData("09:00-15:00,")][InlineData("25:00-26:00")]
    [InlineData("15:00-09:00")][InlineData("09:00-09:00")][InlineData("09:00-15:00,bad")]
    public void Invalid_RejectsEntireCell(string raw) => Assert.Throws<ValidationException>(() => ManualShiftDemandRules.ParseCell(raw));

    [Theory]
    [InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public void NoteConsumption_PreservesPrefixAndUnrelatedMetadata(bool legacy,bool encoded)
    {
        var meta=System.Text.Json.Nodes.JsonNode.Parse(legacy
            ? "{\"manualColumns\":[{\"id\":7,\"label\":\"Manual\",\"cells\":{\"1\":\"09:00-21:00\",\"2\":\"text\"}}],\"g\":[7],\"o\":[-7],\"s\":[[0,1,123]],\"t\":[[1,2,\"note\"]],\"u\":{\"1\":[\"1:2\"]},\"opaque\":42}"
            : "{\"m\":[[7,\"Manual\",{\"1\":\"09:00-21:00\",\"2\":\"text\"}]],\"g\":[7],\"o\":[-7],\"s\":[[0,1,123]],\"t\":[[1,2,\"note\"]],\"u\":{\"1\":[\"1:2\"]},\"opaque\":42}")!;
        var payload=encoded?"b64:"+EncodeGraphNoteMetaValue(meta.ToJsonString()):meta.ToJsonString();
        var note=ConsumeManualColumnPeriodFromNote("old text\n\n[[GF3_GRAPH_META:"+payload+"]]",7,1,new("12:00","15:00"));
        Assert.StartsWith("old text\n\n",note);Assert.Equal("09:00 - 12:00, 15:00 - 21:00",GraphManualColumnLabelResolver.ResolveCell(note,7,1));
        Assert.Equal("text",GraphManualColumnLabelResolver.ResolveCell(note,7,2));
        var after=ParseGraphNoteMeta(note!.Split("[[GF3_GRAPH_META:")[1][..^2])!;
        foreach(var key in new[]{"g","o","s","t","u","opaque"})Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(meta[key],after[key]));
    }
}
