namespace CvGenerator;

internal enum Format
{
    Pdf,
    Docx,

    /// <summary>An alias for <see cref="Docx"/>.</summary>
    Doc,

    /// <summary>Single-column, icon-free DOCX tuned for ATS parsers.</summary>
    Ats,
}

/// <summary>
/// The parsed command line: <c>cv-generator &lt;name&gt; [format] [-i dir] [-o dir]</c>.
/// </summary>
internal sealed record Cli(string Name, Format Format, string InputDir, string OutputDir)
{
    public const string Usage = """
        Generate a CV or cover letter from a TOML profile.

        Usage: cv-generator <name> [format] [options]

          <name>                 Input file name without .toml
          [format]               pdf (default), docx, doc, or ats

        Options:
          -i, --input-dir <dir>  Directory holding the TOML inputs (default: input)
          -o, --output-dir <dir> Directory to write documents into (default: output)
          -h, --help             Show this message
          -V, --version          Show the version
        """;

    /// <summary>
    /// <c>doc</c> is an alias for <c>docx</c>; the ATS variant gets its own suffix
    /// so it can live next to the styled DOCX.
    /// </summary>
    public string FileName() => Format switch
    {
        Format.Pdf => $"{Name}.pdf",
        Format.Doc or Format.Docx => $"{Name}.docx",
        Format.Ats => $"{Name}-ats.docx",
        _ => throw new GeneratorException($"Unsupported format: {Format}"),
    };

    public string InputPath => Path.Combine(InputDir, $"{Name}.toml");

    public string OutputPath => Path.Combine(OutputDir, FileName());

    /// <summary>
    /// Parses the argument list. Returns <c>null</c> when the caller asked for
    /// help or the version, both of which have already been written to stdout.
    /// </summary>
    public static Cli? Parse(string[] args)
    {
        string? name = null;
        Format? format = null;
        var inputDir = "input";
        var outputDir = "output";

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "-h" or "--help":
                    Console.WriteLine(Usage);
                    return null;

                case "-V" or "--version":
                    Console.WriteLine($"cv-generator {Version}");
                    return null;

                case "-i" or "--input-dir":
                    inputDir = Next(args, ref index, argument);
                    break;

                case "-o" or "--output-dir":
                    outputDir = Next(args, ref index, argument);
                    break;

                default:
                    if (argument.StartsWith('-'))
                    {
                        throw new GeneratorException($"unexpected option '{argument}'\n\n{Usage}");
                    }

                    if (name is null)
                    {
                        name = argument;
                    }
                    else if (format is null)
                    {
                        format = ParseFormat(argument);
                    }
                    else
                    {
                        throw new GeneratorException($"unexpected argument '{argument}'\n\n{Usage}");
                    }

                    break;
            }
        }

        if (name is null)
        {
            throw new GeneratorException($"missing input name\n\n{Usage}");
        }

        return new Cli(name, format ?? Format.Pdf, inputDir, outputDir);
    }

    private static string Version =>
        typeof(Cli).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private static Format ParseFormat(string value) => value.ToLowerInvariant() switch
    {
        "pdf" => Format.Pdf,
        "docx" => Format.Docx,
        "doc" => Format.Doc,
        "ats" => Format.Ats,
        _ => throw new GeneratorException(
            $"invalid format '{value}' (expected pdf, docx, doc, or ats)"),
    };

    private static string Next(string[] args, ref int index, string option)
    {
        index++;
        if (index >= args.Length)
        {
            throw new GeneratorException($"'{option}' needs a value");
        }

        return args[index];
    }
}
