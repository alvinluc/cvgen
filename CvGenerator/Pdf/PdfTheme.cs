using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CvGenerator.Pdf;

/// <summary>
/// The typographic constants of the PDF template. The design is deliberately
/// icon-free: polish lives in the typography and structure rather than in
/// decoration.
/// </summary>
internal static class PdfTheme
{
    public static readonly Color Accent = Color.FromHex("#165a72");
    public static readonly Color Ink = Color.FromHex("#17212b");
    public static readonly Color Muted = Color.FromHex("#5c6875");
    public static readonly Color Soft = Color.FromHex("#eef5f7");
    public static readonly Color Rule = Color.FromHex("#d8e4e8");

    /// <summary>Body size in points; every relative gap below is a multiple of it.</summary>
    public const float BaseSize = 9.55f;

    public const float PageMarginHorizontalCm = 1.32f;
    public const float PageMarginVerticalCm = 1.24f;

    /// <summary>
    /// Line height as a multiple of the font size: tight enough to keep a CV on
    /// as few pages as possible without the body text setting solid.
    /// </summary>
    public const float LineHeight = 1.22f;

    /// <summary>Gap below each paragraph of a cover letter body.</summary>
    public static float BodyGap => Em(1.05f);

    /// <summary>Room between the sign-off and the name, left for a signature.</summary>
    public static float SignOffGap => Em(2.6f);

    /// <summary>Gap between the lines of the recipient address block.</summary>
    public static float RecipientGap => Em(0.42f);

    /// <summary>Gap below the date, separating it from the recipient address block.</summary>
    public static float DateGap => Em(1.3f);

    /// <summary>The default gap below a body paragraph.</summary>
    public static float ParagraphGap => Em(0.48f);

    /// <summary>Width of the rule under the CV header, 38% of the text column.</summary>
    public static float HeaderRuleWidth => 0.38f * ContentWidth;

    /// <summary>A4 width less the side margins, in points.</summary>
    private static float ContentWidth =>
        PageSizes.A4.Width - (2f * PageMarginHorizontalCm * PointsPerCentimetre);

    private const float PointsPerCentimetre = 72f / 2.54f;

    public static float Em(float multiple) => BaseSize * multiple;
}
