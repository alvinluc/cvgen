using CommandLine;

namespace Application.Domain
{
    public sealed class Options
    {
        [Value(0)] public string? FileName { get; set; }

        [Value(1)] public string? FileFormat { get; set; }
    }
}