using Tomlyn;
using Tomlyn.Serialization;

namespace CvGenerator.Model;

/// <summary>
/// The source-generated binding context. Tomlyn's reflection-based path would
/// work too, but a generated context is trim- and AOT-safe and keeps the
/// mapping declarative.
/// </summary>
[TomlSerializable(typeof(Document))]
internal sealed partial class CvSerializerContext : TomlSerializerContext;

internal static class TomlLoader
{
    public static Document Load(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new GeneratorException($"Input file not found: {path} ({error.Message})");
        }

        return Parse(text, path);
    }

    public static Document Parse(string text, string origin = "<input>")
    {
        try
        {
            return TomlSerializer.Deserialize<Document>(text, CvSerializerContext.Default)
                ?? new Document();
        }
        catch (TomlException error)
        {
            throw new GeneratorException($"Invalid TOML in {origin}: {error.Message}", error);
        }
    }
}
