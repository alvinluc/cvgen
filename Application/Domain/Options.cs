using CommandLine;

namespace Application.Domain
{
    public sealed class Options
    {
        [Value(0)] public string? File { get; set; }

        [Value(1)] public string? Format { get; set; }

        [Option('c', "cover-letter", Required = false, HelpText = "Generate a covering letter instead of a CV.")]
        public bool CoverLetter { get; set; }
    }
}
