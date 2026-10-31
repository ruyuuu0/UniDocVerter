namespace UniDocVerter.Models;

public sealed record MediaStream(string Type);

public sealed record MediaProbeResult(
    IReadOnlyList<MediaStream> Streams)
{
    public bool HasVideo => Streams.Any(stream => stream.Type == "video");
    public bool HasAudio => Streams.Any(stream => stream.Type == "audio");
}