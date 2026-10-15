using Nexora.MeetingAgent.Audio;

namespace Nexora.MeetingAgent.Tests;

public sealed class SpeechSegmenterTests
{
    [Fact]
    public void TwoTracksProduceIndependentStableSegmentIds()
    {
        var vietnamese = new SpeechSegmenter();
        var english = new SpeechSegmenter();
        var time = DateTimeOffset.UtcNow;
        var vi = new List<Nexora.MeetingAgent.Models.SpeechChunk>();
        var en = new List<Nexora.MeetingAgent.Models.SpeechChunk>();
        for (var step = 0; step < 9; step++)
        {
            time = time.AddMilliseconds(500);
            vi.AddRange(vietnamese.Advance(new float[8000], step < 7, time));
            en.AddRange(english.Advance(new float[8000], step is >= 2 and < 7, time));
        }

        var viId = Assert.Single(vi, chunk => chunk.Type == "start").SegmentId;
        var enId = Assert.Single(en, chunk => chunk.Type == "start").SegmentId;
        Assert.NotEqual(viId, enId);
        Assert.All(vi, chunk => Assert.Equal(viId, chunk.SegmentId));
        Assert.All(en, chunk => Assert.Equal(enId, chunk.SegmentId));
        Assert.Contains(vi, chunk => chunk.Type == "partial");
        Assert.Contains(vi, chunk => chunk.Type == "final");
        Assert.Contains(en, chunk => chunk.Type == "final");
    }
}
