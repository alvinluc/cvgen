using CommandLine;

namespace Application.Domain
{
    public sealed class Options
    {
        [Value(0)] public string? File { get; set; }

        [Value(1)] public string? Format { get; set; }
    }
}
