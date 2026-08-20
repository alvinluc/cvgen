using System.Reflection;

using QuestPDF.Drawing;

namespace CvGenerator.Pdf;

/// <summary>
/// Font registration for the PDF renderer.
/// <para>
/// <c>CvGenerator/Fonts/</c> ships Libertinus Serif, embedded at build time and
/// registered here, so the same input lays out identically anywhere. Any other
/// TrueType file dropped into that directory is picked up the same way, no code
/// change needed. Should the directory ever be emptied, the renderer falls back
/// through <see cref="Families"/> to whatever serif the host has and line breaks
/// start varying between machines — which is what
/// <c>EmbedsTheBundledSerifRatherThanAHostFont</c> guards against.
/// </para>
/// </summary>
internal static class PdfFonts
{
    private const string ResourcePrefix = "font-";

    /// <summary>
    /// The family chain handed to QuestPDF. "Libertinus Serif" wins when the
    /// embedded font is present; the rest are common host serifs, in descending
    /// order of how close they sit to the intended look.
    /// </summary>
    public static readonly string[] Families =
    [
        "Libertinus Serif",
        "Linux Libertine",
        "Georgia",
        "Times New Roman",
        "DejaVu Serif",
        "Liberation Serif",
        "serif",
    ];

    private static bool registered;

    /// <summary>True once an embedded face was found, i.e. output is host-independent.</summary>
    public static bool HasEmbeddedFonts { get; private set; }

    public static void Register()
    {
        if (registered)
        {
            return;
        }

        registered = true;

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null)
            {
                continue;
            }

            FontManager.RegisterFont(stream);
            HasEmbeddedFonts = true;
        }
    }
}
