namespace CvGenerator;

/// <summary>
/// A failure worth reporting to the user verbatim: a bad argument, a missing
/// input, malformed TOML, or a file that could not be written.
/// </summary>
internal sealed class GeneratorException : Exception
{
    public GeneratorException(string message)
        : base(message)
    {
    }

    public GeneratorException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
